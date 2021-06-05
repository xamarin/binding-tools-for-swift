using System;
using SwiftReflector;
using SwiftReflector.Demangling;
using SwiftReflector.Inventory;
using System.Collections.Generic;
using System.Linq;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class DBFunc : IAssociatedTypes {
		public string Name { get; }
		public bool IsStatic { get; }
		public bool IsProperty { get; }
		public string ReturnType { get; }
		public bool HasThrows { get; }
		public bool IsMutating { get; }
		public bool HasInstance { get; }

		public string PropertyType { get; } = string.Empty;
		public bool IsPossiblyIncomplete { get; } = false;
		public OperatorType OperatorKind { get; } = OperatorType.None;
		public TypeAccessibility Accessibility { get; } = TypeAccessibility.Public;
		public bool IsFinal { get; } = false;
		public bool IsDeprecated { get; } = false;
		public bool IsUnavailable { get; } = false;
		public bool IsOptional { get; } = false;
		public bool IsRequired { get; } = false;
		public bool IsConvenienceInit { get; } = false;
		public string ObjcSelector { get; } = string.Empty;

		public DBParameterLists ParameterLists { get; }
		public DBGenericParameters GenericParameters { get; }
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		public DBFunc (TLFunction tlf, bool isMutating = false, bool isProtocol = false, bool isGlobal = false)
		{
			Exceptions.ThrowOnNull (tlf, nameof (tlf));
			Name = tlf.Name.Name;
			IsStatic = GetStaticStatus (tlf.Signature);
			IsProperty = false;
			HasThrows = tlf.Signature.CanThrow;
			IsMutating = isMutating;
			OperatorKind = tlf.Operator;
			HasInstance = !tlf.Signature.IsConstructor && !IsStatic && !isProtocol && !isGlobal;
			ReturnType = SwiftTypeToString.MapSwiftTypeToString (tlf.Signature.ReturnType, tlf.Module.Name);
			ParameterLists = new DBParameterLists (tlf.Signature, HasInstance, isGlobal);
			GenericParameters = new DBGenericParameters (tlf.Signature);
			AssociatedTypes.AssociatedTypeCollection.UnionWith (tlf.Signature.ReturnType.GetAssociatedTypes ());
			AssociatedTypes.AssociatedTypeCollection.UnionWith (ParameterLists.AssociatedTypes.AssociatedTypeCollection);
		}

		public DBFunc (DBProperty dbProperty, string propertyType, bool isMutating = false)
		{
			Exceptions.ThrowOnNull (dbProperty, nameof (dbProperty));
			Name = propertyType == "Getter" ? $"get_{dbProperty.Name}" : $"set_{dbProperty.Name}";
			PropertyType = Exceptions.ThrowOnNull (propertyType, nameof (propertyType));
			IsStatic = dbProperty.IsStatic;
			IsProperty = true;
			HasThrows = false;
			IsMutating = isMutating;
			HasInstance = !IsStatic;
			ReturnType = dbProperty.Type;
			ParameterLists = new DBParameterLists (HasInstance, propertyType);
			GenericParameters = dbProperty.GenericParameters;
		}

		bool GetStaticStatus (SwiftBaseFunctionType signature) => signature switch {
			SwiftStaticFunctionType funcType => true,
			SwiftPropertyType propType => propType.IsStatic,
			_ => false,
		};
	}

	internal class DBFuncs : IAssociatedTypes {
		public List<DBFunc> FuncCollection { get; } = new List<DBFunc> ();
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		public DBFuncs (ClassContents classContents)
		{
			Exceptions.ThrowOnNull (classContents, nameof (classContents));
			var functions = SortedSetExtensions.Create<OverloadInventory> ();
			functions.AddRange (classContents.Methods.Values, classContents.StaticFunctions.Values, classContents.Constructors.Values);

			foreach (var function in functions) {
				foreach (var overloadFunction in function.Functions) {
					if (overloadFunction.Name.Name.IsPublic () && !IsMetaClass (overloadFunction.Signature))
						FuncCollection.Add (new DBFunc (overloadFunction));
				}
			}
			AssociatedTypes.AssociatedTypeCollection.UnionWith (FuncCollection.GetChildrenAssociatedTypes ());
		}

		public DBFuncs (ProtocolContents protocolContents)
		{
			Exceptions.ThrowOnNull (protocolContents, nameof (protocolContents));
			var functions = SortedSetExtensions.Create<TLFunction> ();
			functions.AddRange (protocolContents.FunctionsOfUnknownDestination);
			foreach (var function in functions) {
				if (function.Name.Name.IsPublic () && !IsMetaClass (function.Signature))
					FuncCollection.Add (new DBFunc (function, isProtocol: true));
			}
			AssociatedTypes.AssociatedTypeCollection.UnionWith (FuncCollection.GetChildrenAssociatedTypes ());
		}

		public DBFuncs (ModuleInventory mi, SwiftName module)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			var functions = SortedSetExtensions.Create<OverloadInventory> ();
			functions.AddRange (CheckInventoryDictionary.GetGlobalFunctions (mi, module));

			foreach (var function in functions) {
				foreach (var overloadFunction in function.Functions) {
					if (overloadFunction.Name.Name.IsPublic () && !IsMetaClass (overloadFunction.Signature))
						FuncCollection.Add (new DBFunc (overloadFunction, isGlobal: true));
				}
			}
			AssociatedTypes.AssociatedTypeCollection.UnionWith (FuncCollection.GetChildrenAssociatedTypes ());
		}

		bool IsMetaClass (SwiftBaseFunctionType funcType)
		{
			Exceptions.ThrowOnNull (funcType, nameof (funcType));
			return funcType.ReturnType.Type == CoreCompoundType.MetaClass ||
			    funcType.EachParameter.Any (t => t.Type == CoreCompoundType.MetaClass);
		}
	}
}
