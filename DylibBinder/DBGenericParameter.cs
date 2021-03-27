using System;
using System.Collections.Generic;
using Dynamo.SwiftLang;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBGenericParameter {
		public DBGenericParameter (int depth, int index)
		{
			Depth = depth;
			Index = index;
			Name = SLGenericReferenceType.DefaultNamer (depth, index);
		}

		public int Depth { get; }
		public int Index { get; }
		public string Name { get; }
	}

	internal class DBGenericParameters {

		public DBGenericParameters (SwiftBaseFunctionType signature) {
			AssignGenericParameters (signature);
		}

		public DBGenericParameters (SwiftType swiftType)
		{
			FilterGenericParameterSource (swiftType);
		}

		public DBGenericParameters (DBTypeDeclaration typeDeclaration)
		{
			ParseTopLevelGenerics (typeDeclaration.Funcs, typeDeclaration.Properties);
		}

		public HashSet<DBGenericParameter> GenericParameters { get; } = new HashSet<DBGenericParameter> (new DBGenericParameterComparer ());

		void AssignGenericParameters (SwiftBaseFunctionType signature)
		{
			FilterGenericParameterSource (signature.Parameters, signature.ReturnType);
		}

		void FilterGenericParameterSource (params SwiftType [] types)
		{
			foreach (var type in types) {
				switch (type) {
				case SwiftFunctionType funcType:
					HandleType (funcType);
					break;
				case SwiftBoundGenericType boundType:
					HandleType (boundType);
					break;
				case SwiftTupleType tupleType:
					HandleType (tupleType);
					break;
				case SwiftGenericArgReferenceType refType:
					HandleType (refType);
					break;
				default:
					break;
				}
			}
		}

		void HandleType (SwiftFunctionType funcType)
		{
			switch (funcType.Parameters) {
			case SwiftBoundGenericType boundType:
				HandleType (boundType);
				break;
			case SwiftTupleType tupleType:
				HandleType (tupleType);
				break;
			case SwiftGenericArgReferenceType refType:
				HandleType (refType);
				break;
			default:
				break;
			}
		}

		void HandleType (SwiftBoundGenericType boundType)
		{
			foreach (var bound in boundType.BoundTypes) {
				if (bound is SwiftGenericArgReferenceType refType)
					HandleType (refType);
			}
		}

		void HandleType (SwiftTupleType tupleType)
		{
			foreach (var content in tupleType.Contents) {
				switch (content) {
				case SwiftBoundGenericType boundGType:
					HandleType (boundGType);
					break;
				case SwiftFunctionType cFuncType:
					HandleType (cFuncType);
					break;
				case SwiftGenericArgReferenceType refType:
					HandleType (refType);
					break;
				case SwiftTupleType innerTupleType:
					HandleType (innerTupleType);
					break;
				default:
					break;
				}
			}
		}

		void HandleType (SwiftGenericArgReferenceType refType)
		{
			GenericParameters.Add (new DBGenericParameter (refType.Depth, refType.Index));
		}

		void ParseTopLevelGenerics (DBFuncs funcs, DBProperties properties)
		{
			foreach (var func in funcs.Funcs) {
				GrabTopLevelGenerics (func.GenericParameters);
			}

			foreach (var prop in properties.Properties) {
				GrabTopLevelGenerics (prop.GenericParameters);
			}
		}

		void GrabTopLevelGenerics (DBGenericParameters genericParameters) {
			foreach (var gp in genericParameters.GenericParameters) {
				if (gp.Depth == 0)
					GenericParameters.Add (gp);
			}
		}
	}

	class DBGenericParameterComparer : EqualityComparer<DBGenericParameter> {
		public override bool Equals (DBGenericParameter gp1, DBGenericParameter gp2)
		{
			return gp1.Name == gp2.Name;
		}

		public override int GetHashCode (DBGenericParameter gp)
		{
			var maxDepthLevel = 18;
			return gp.Index * maxDepthLevel + gp.Depth;
		}
	}

}
