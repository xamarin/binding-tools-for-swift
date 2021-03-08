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
	public class DBFunc {
		public DBFunc (TLFunction tlf, bool isMutating = false, bool isProtocol = false)
		{
			Name = tlf.Name.Name;
			IsStatic = GetStaticStatus (tlf.Signature);
			IsProperty = "False";
			HasThrows = tlf.Signature.CanThrow.ToString ();
			IsMutating = isMutating.ToString ();
			OperatorKind = tlf.Operator.ToString ();
			HasInstance = (!tlf.Signature.IsConstructor && IsStatic == "False").ToString ();
			// TODO not sure how to handle instances with protocols yet
			if (isProtocol)
				HasInstance = "False";

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
			IsProperty = "True";
			HasThrows = "False";
			IsMutating = isMutating.ToString ();
			HasInstance = IsStatic == "True" ? "False" : "True";
			ReturnType = dbProperty.Type;
			ParameterLists = new DBParameterLists (ReturnType, HasInstance, propertyType);
			GenericParameters = dbProperty.GenericParameters;
		}

		public string Name { get; }
		public string IsStatic { get; }
		public string IsProperty { get; }
		public string ReturnType { get; }
		public string HasThrows { get; }
		public string IsMutating { get; }
		public string HasInstance { get; }

		public string PropertyType { get; } = "";
		public string IsPossiblyIncomplete { get; } = "False";
		public string OperatorKind { get; } = "None";
		public string Accessibility { get; } = "Public";
		public string IsFinal { get; } = "False";
		public string IsDeprecated { get; } = "False";
		public string IsUnavailable { get; } = "False";
		public string IsOptional { get; } = "False";
		public string IsRequired { get; } = "False";
		public string IsConvenienceInit { get; } = "False";
		public string ObjcSelector { get; } = "";

		public DBParameterLists ParameterLists { get; }
		public DBGenericParameters GenericParameters { get; }
		public List<DBAssociatedType> AssociatedTypes { get; } = new List<DBAssociatedType> ();

		string GetStaticStatus (SwiftBaseFunctionType signature)
		{
			if (signature is SwiftStaticFunctionType)
				return "True";
			else if (signature is SwiftStaticFunctionThunkType)
				return "True";
			else if (signature is SwiftPropertyType propType)
				return propType.IsStatic.ToString ();
			else if (signature is SwiftPropertyThunkType propThunkType)
				return propThunkType.IsStatic.ToString ();
			else if (signature is SwiftUncurriedFunctionThunkType)
				return "False";
			else if (signature is SwiftUncurriedFunctionType)
				return "False";

			return "False";
		}
	}

	public class DBFuncs {
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
			if (matches.Count > 1)
				return false;
			return true;
		}
	}
}
