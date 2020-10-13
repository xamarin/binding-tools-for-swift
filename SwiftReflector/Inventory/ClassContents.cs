// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftReflector.ExceptionTools;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector.Inventory {
	public class ClassContents {
		int sizeofMachinePointer;
		public ClassContents (SwiftClassName className, int sizeofMachiinePointer)
		{
			this.sizeofMachinePointer = sizeofMachiinePointer;
			Constructors = new FunctionInventory (sizeofMachinePointer);
			ClassConstructor = new FunctionInventory (sizeofMachinePointer);
			Methods = new FunctionInventory (sizeofMachinePointer);
			Properties = new PropertyInventory (sizeofMachinePointer);
			StaticProperties = new PropertyInventory (sizeofMachiinePointer);
			PrivateProperties = new PropertyInventory (sizeofMachinePointer);
			StaticPrivateProperties = new PropertyInventory (sizeofMachinePointer);
			Subscripts = new List<TLFunction> (sizeofMachinePointer);
			PrivateSubscripts = new List<TLFunction> (sizeofMachinePointer);
			StaticFunctions = new FunctionInventory (sizeofMachinePointer);
			Destructors = new FunctionInventory (sizeofMachinePointer);
			WitnessTable = new WitnessInventory (sizeofMachinePointer);
			FunctionsOfUnknownDestination = new List<TLFunction> ();
			DefinitionsOfUnknownDestination = new List<TLDefinition> ();
			Variables = new VariableInventory (sizeofMachinePointer);
			Initializers = new FunctionInventory (sizeofMachinePointer);
			Name = className;
			ProtocolConformanceDescriptors = new List<TLProtocolConformanceDescriptor> ();
			MethodDescriptors = new FunctionInventory (sizeofMachinePointer);
			PropertyDescriptors = new List<TLPropertyDescriptor> ();
		}

		public void Add (TLDefinition tld, Stream srcStm)
		{
			TLFunction tlf = tld as TLFunction;
			if (tlf != null) {
				if (IsConstructor (tlf.Signature, tlf.Class)) {
					Constructors.Add (tlf, srcStm);
				} else if (tlf.Signature is SwiftClassConstructorType) {
					if (ClassConstructor.Values.Count () == 0)
						ClassConstructor.Add (tlf, srcStm);
					else
						throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 12, $"multiple type metadata accessors for {tlf.Class.ClassName.ToFullyQualifiedName ()}");
				} else if (IsDestructor (tlf.Signature, tlf.Class)) {
					Destructors.Add (tlf, srcStm);
				} else if (IsProperty (tlf.Signature, tlf.Class)) {
					if (IsSubscript (tlf.Signature, tlf.Class)) {
						if (IsPrivateProperty (tlf.Signature, tlf.Class))
							PrivateSubscripts.Add (tlf);
						else
							Subscripts.Add (tlf);
					} else {
						if (IsStaticProperty (tlf.Signature, tlf.Class)) {
							if (IsPrivateProperty (tlf.Signature, tlf.Class))
								StaticPrivateProperties.Add (tlf, srcStm);
							else
								StaticProperties.Add (tlf, srcStm);
						} else {
							if (IsPrivateProperty (tlf.Signature, tlf.Class))
								PrivateProperties.Add (tlf, srcStm);
							else
								Properties.Add (tlf, srcStm);
						}
					}
				} else if (IsMethodOnClass (tlf.Signature, tlf.Class)) {
					if (tlf is TLMethodDescriptor)
						MethodDescriptors.Add (tlf, srcStm);
		    			else
						Methods.Add (tlf, srcStm);
				} else if (IsStaticMethod (tlf.Signature, tlf.Class)) {
					var oldTLF = StaticFunctions.ContainsEquivalentFunction (tlf);
					if (oldTLF == null)
						StaticFunctions.Add (tlf, srcStm);
					else {
						var newSig = tlf.Signature as SwiftStaticFunctionType;
						var oldSig = oldTLF.Signature as SwiftStaticFunctionType;
						// if the old sig is the thunk, chain it into the new and
						// replace the old with the new.
						// Else chain the new (thunk) into the old.
						if (oldSig is SwiftStaticFunctionThunkType thunk) {
							newSig.Thunk = thunk;
							StaticFunctions.ReplaceFunction (oldTLF, tlf);
						} else {
							oldSig.Thunk = newSig as SwiftStaticFunctionThunkType;
						}
					}
				} else if (IsWitnessTable (tlf.Signature, tlf.Class)) {
					WitnessTable.Add (tlf, srcStm);
				} else if (IsInitializer (tlf.Signature, tlf.Class)) {
					Initializers.Add (tlf, srcStm);
				} else {
					FunctionsOfUnknownDestination.Add (tlf);
				}
				return;
			}
			var meta = tld as TLDirectMetadata;
			if (meta != null) {
				if (DirectMetadata != null)
					throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 13, $"duplicate direct metadata in class {DirectMetadata.Class.ClassName.ToFullyQualifiedName ()}");
				DirectMetadata = meta;
				return;
			}
			var lazy = tld as TLLazyCacheVariable;
			if (lazy != null) {
				if (LazyCacheVariable != null)
					throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 14, $"duplicate lazy cache variable in class {LazyCacheVariable.Class.ClassName.ToFullyQualifiedName ()}");
				LazyCacheVariable = lazy;
				return;
			}
			var mc = tld as TLMetaclass;
			if (mc != null) {
				if (Metaclass != null) {
					throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 15, $"duplicate type meta data descriptor in class {Metaclass.Class.ClassName.ToFullyQualifiedName ()}");
				}
				Metaclass = mc;
				return;
			}
			var nom = tld as TLNominalTypeDescriptor;
			if (nom != null) {
				if (TypeDescriptor != null) {
					throw ErrorHelper.CreateError (ReflectorError.kInventoryBase + 16, $"duplicate nominal type descriptor in class {TypeDescriptor.Class.ClassName.ToFullyQualifiedName ()}");
				}
				TypeDescriptor = nom;
				return;
			}
			var tlvar = tld as TLVariable;
			if (tlvar != null) {
				if (tlvar is TLPropertyDescriptor tlpropDesc)
					PropertyDescriptors.Add (tlpropDesc);
				else
					Variables.Add (tlvar, srcStm);
				return;
			}
			var tlprot = tld as TLProtocolConformanceDescriptor;
			if (tlprot != null) {
				ProtocolConformanceDescriptors.Add (tlprot);
				return;
			}
			DefinitionsOfUnknownDestination.Add (tld);
		}

		public bool IsFinal (TLFunction func)
		{
			string funcMangledSuffix = func.MangledName.Substring (3); // drop the __T

			foreach (string mangledWitnessEntry in WitnessTable.MangledNames) {
				if (mangledWitnessEntry.EndsWith (funcMangledSuffix))
					return false;
			}
			return true;
		}

		static bool IsConstructor (SwiftType signature, SwiftClassType cl)
		{
			if (cl == null)
				return false;
			SwiftConstructorType des = signature as SwiftConstructorType;
			if (des == null)
				return false;
			return des.Name.Equals (Decomposer.kSwiftAllocatingConstructorName)
				|| des.Name.Equals (Decomposer.kSwiftNonAllocatingConstructorName);
		}

		static bool IsDestructor (SwiftType signature, SwiftClassType cl)
		{
			if (cl == null)
				return false;
			SwiftDestructorType des = signature as SwiftDestructorType;
			if (des == null)
				return false;
			return des.Name.Equals (Decomposer.kSwiftDeallocatingDestructorName)
				|| des.Name.Equals (Decomposer.kSwiftNonDeallocatingDestructorName);
		}

		public static bool IsWitnessTable (SwiftType signature, SwiftClassType cl)
		{
			if (cl == null)
				return false;
			return signature is SwiftWitnessTableType;
		}

		public static bool IsInitializer (SwiftType signature, SwiftClassType cl)
		{
			if (cl == null)
				return false;
			return signature is SwiftInitializerType;
		}

		static bool IsProperty (SwiftType signature, SwiftClassType cl)
		{
			return signature is SwiftPropertyType;
		}

		static bool IsSubscript (SwiftType signature, SwiftClassType cl)
		{
			SwiftPropertyType prop = signature as SwiftPropertyType;
			return prop != null && prop.IsSubscript;
		}

		static bool IsPrivateProperty (SwiftType signature, SwiftClassType cl)
		{
			SwiftPropertyType pt = signature as SwiftPropertyType;
			return pt != null && pt.IsPrivate;
		}

		static bool IsStaticProperty(SwiftType signature, SwiftClassType classType)
		{
			SwiftPropertyType pt = signature as SwiftPropertyType;
			return pt != null && pt.IsStatic;
		}

		static bool IsMethodOnClass (SwiftType signature, SwiftClassType cl)
		{
			if (cl == null)
				return false;
			if (signature is SwiftWitnessTableType)
				return false;
			SwiftUncurriedFunctionType ucf = signature as SwiftUncurriedFunctionType;
			if (ucf == null)
				return false;
			return ucf.UncurriedParameter is SwiftClassType && ucf.UncurriedParameter.Equals (cl);
		}

		static bool IsStaticMethod (SwiftType signature, SwiftClassType cl)
		{
			return signature is SwiftStaticFunctionType;
		}

		public IEnumerable<PropertyContents> AllPropertiesWithName(string name)
		{
			PropertyContents prop = null;
			if (Properties.TryGetValue (name, out prop))
				yield return prop;
			if (PrivateProperties.TryGetValue (name, out prop))
				yield return prop;
			if (StaticProperties.TryGetValue (name, out prop))
				yield return prop;
			if (StaticPrivateProperties.TryGetValue (name, out prop))
				yield return prop;
		}


		public SwiftClassName Name { get; private set; }
		public FunctionInventory Constructors { get; private set; }
		public FunctionInventory ClassConstructor { get; private set; }
		public FunctionInventory Destructors { get; private set; }
		public PropertyInventory Properties { get; private set; }
		public PropertyInventory StaticProperties { get; private set; }
		public PropertyInventory PrivateProperties { get; private set; }
		public PropertyInventory StaticPrivateProperties { get; private set; }
		public List<TLFunction> Subscripts { get; private set; }
		public List<TLFunction> PrivateSubscripts { get; private set; }
		public FunctionInventory Methods { get; private set; }
		public FunctionInventory StaticFunctions { get; private set; }
		public WitnessInventory WitnessTable { get; private set; }
		public TLLazyCacheVariable LazyCacheVariable { get; private set; }
		public TLDirectMetadata DirectMetadata { get; private set; }
		public TLMetaclass Metaclass { get; private set; }
		public TLNominalTypeDescriptor TypeDescriptor { get; private set; }
		public List<TLFunction> FunctionsOfUnknownDestination { get; private set; }
		public List<TLDefinition> DefinitionsOfUnknownDestination { get; private set; }
		public VariableInventory Variables { get; private set; }
		public List<TLPropertyDescriptor> PropertyDescriptors { get; private set; }
		public FunctionInventory Initializers { get; private set; }
		public List<TLProtocolConformanceDescriptor> ProtocolConformanceDescriptors { get; private set; }
		public FunctionInventory MethodDescriptors { get; private set; }

		public int SizeInBytes {
			get {
				ValueWitnessTable vat = WitnessTable.ValueWitnessTable;
				return vat != null ? (int)vat.Size : 0;
			}
		}
		public int StrideInBytes {
			get {
				ValueWitnessTable vat = WitnessTable.ValueWitnessTable;
				return vat != null ? (int)vat.Stride : 0;
			}
		}
	}

}

