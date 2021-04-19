using System;
using SwiftReflector;
using SwiftReflector.Demangling;
using SwiftReflector.Inventory;
using System.Collections.Generic;
using Dynamo.SwiftLang;
using SwiftReflector.TypeMapping;
using System.Linq;
using System.Text.RegularExpressions;

namespace DylibBinder {
	internal class DBFunc : IAssociatedTypes {
		public DBFunc (TLFunction tlf, bool isMutating = false, bool isProtocol = false)
		{
			Name = tlf.Name.Name;
			IsStatic = GetStaticStatus (tlf.Signature);
			IsProperty = false;
			HasThrows = tlf.Signature.CanThrow;
			IsMutating = isMutating;
			OperatorKind = tlf.Operator;
			// TODO not sure if/how to handle instances with protocols yet
			HasInstance = !tlf.Signature.IsConstructor && !IsStatic && !isProtocol;
			ReturnType = SwiftTypeToString.MapSwiftTypeToString (tlf.Signature.ReturnType, tlf.Module.Name);
			ParameterLists = new DBParameterLists (tlf.Signature, HasInstance);
			GenericParameters = new DBGenericParameters (tlf.Signature);
			AssociatedTypes.Add (tlf.Signature.ReturnType.GetAssociatedTypes ());
			AssociatedTypes.Add (ParameterLists.AssociatedTypes);
		}

		public DBFunc (DBProperty dbProperty, string propertyType, bool isMutating = false)
		{
			Name = propertyType == "Getter" ? $"get_{dbProperty.Name}" : $"set_{dbProperty.Name}";
			PropertyType = propertyType;
			IsStatic = dbProperty.IsStatic;
			IsProperty = true;
			HasThrows = false;
			IsMutating = isMutating;
			HasInstance = !IsStatic;
			ReturnType = dbProperty.Type;
			ParameterLists = new DBParameterLists (HasInstance, propertyType);
			GenericParameters = dbProperty.GenericParameters;
		}

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

		bool GetStaticStatus (SwiftBaseFunctionType signature) => signature switch {
			SwiftStaticFunctionType => true,
			SwiftPropertyType propType => propType.IsStatic,
			_ => false,
		};
	}

	internal class DBFuncs : IAssociatedTypes {
		public DBFuncs (ClassContents classContents)
		{
			var functions = SortedSetExtensions.Create<OverloadInventory> ();
			functions.AddRange (classContents.Methods.Values, classContents.StaticFunctions.Values, classContents.Constructors.Values);

			foreach (var function in functions) {
				foreach (var overloadFunction in function.Functions) {
					if (overloadFunction.Name.Name.IsPublic () && !IsMetaClass (overloadFunction.Signature))
						Funcs.Add (new DBFunc (overloadFunction));
				}
			}
			AssociatedTypes.Add (Funcs.GetChildrenAssociatedTypes ());
		}

		public DBFuncs (ProtocolContents protocolContents)
		{
			var functions = SortedSetExtensions.Create<TLFunction> ();
			functions.AddRange (protocolContents.FunctionsOfUnknownDestination);
			foreach (var function in functions) {
				if (function.Name.Name.IsPublic () && !IsMetaClass (function.Signature))
					Funcs.Add (new DBFunc (function, isProtocol: true));
			}
			AssociatedTypes.Add (Funcs.GetChildrenAssociatedTypes ());
		}

		public List<DBFunc> Funcs { get; } = new List<DBFunc> ();
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		bool IsMetaClass (SwiftBaseFunctionType funcType)
		{
			if (funcType.ReturnType.Type == CoreCompoundType.MetaClass)
				return true;
			foreach (var param in funcType.EachParameter) {
				if (param.Type == CoreCompoundType.MetaClass)
					return true;
			}
			return false;
		}
	}
}
