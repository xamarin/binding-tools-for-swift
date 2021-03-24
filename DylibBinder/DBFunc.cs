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
	internal class DBFunc {
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
		}

		public DBFunc (DBProperty dbProperty, string propertyType, bool isMutating = false)
		{
			if (propertyType == "Getter")
				Name = $"get_{dbProperty.Name}";
			else
				Name = $"set_{dbProperty.Name}";
			PropertyType = propertyType;
			IsStatic = dbProperty.IsStatic;
			IsProperty = true;
			HasThrows = false;
			IsMutating = isMutating;
			HasInstance = !IsStatic;
			ReturnType = dbProperty.Type;
			ParameterLists = new DBParameterLists (ReturnType, HasInstance, propertyType);
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
		public List<DBAssociatedType> AssociatedTypes { get; } = new List<DBAssociatedType> ();

		bool GetStaticStatus (SwiftBaseFunctionType signature)
		{
			switch (signature) {
			case SwiftStaticFunctionType:
				return true;
			case SwiftPropertyType propType:
				return propType.IsStatic;
			case SwiftUncurriedFunctionThunkType:
				return false;
			case SwiftUncurriedFunctionType:
				return false;
			default:
				return false;
			}
		}
	}

	internal class DBFuncs {
		public DBFuncs (ClassContents classContents)
		{
			var functions = classContents.Methods.Values.ToList ();
			functions.AddRange (classContents.StaticFunctions.Values.ToList ());
			functions.AddRange (classContents.Constructors.Values.ToList ());
			functions.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.Name, type2.Name.Name));
			foreach (var function in functions) {
				foreach (var overloadFunction in function.Functions) {
					if (overloadFunction.Name.Name.IsPublic () && !IsMetaClass (overloadFunction.Signature)
						&& DoesNotDoubleThrow (overloadFunction.Signature.ToString ())) {
						Funcs.Add (new DBFunc (overloadFunction));
					}
				}
			}
		}

		public DBFuncs (ProtocolContents protocolContents)
		{
			var functions = protocolContents.FunctionsOfUnknownDestination;
			functions.Sort ((type1, type2) => String.CompareOrdinal (type1.Module.Name, type2.Module.Name));
			foreach (var function in functions) {
				if (function.Name.Name.IsPublic () && !IsMetaClass (function.Signature)
					&& DoesNotDoubleThrow (function.Signature.ToString ())) {
					Funcs.Add (new DBFunc (function, isProtocol: true));
				}
			}
		}

		public List<DBFunc> Funcs { get; } = new List<DBFunc> ();

		bool IsMetaClass (SwiftBaseFunctionType funcType)
		{
			if (funcType.ReturnType.Type == SwiftReflector.CoreCompoundType.MetaClass)
				return true;
			foreach (var param in funcType.EachParameter) {
				if (param.Type == SwiftReflector.CoreCompoundType.MetaClass)
					return true;
			}
			return false;
		}

		bool DoesNotDoubleThrow (string s)
		{
			var matches = Regex.Matches (s, "->");
			return matches.Count <= 1;
		}
	}
}
