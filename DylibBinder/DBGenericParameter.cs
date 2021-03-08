using System;
using System.Collections.Generic;
using Dynamo.SwiftLang;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class DBGenericParameter {
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

	public class DBGenericParameters {

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

		public List<DBGenericParameter> GenericParameters { get; } = new List<DBGenericParameter> ();


		void AssignGenericParameters (SwiftBaseFunctionType signature)
		{
			FilterGenericParameterSource (signature.Parameters, signature.ReturnType);
		}

		void FilterGenericParameterSource (params SwiftType [] types)
		{
			foreach (var type in types) {
				if (type is SwiftFunctionType funcType) {
					HandleFunctionType (funcType);
				} else if (type is SwiftBoundGenericType boundType) {
					HandleBoundGenericType (boundType);
				} else if (type is SwiftTupleType tupleType) {
					HandleTupleType (tupleType);
				} else if (type is SwiftGenericArgReferenceType refType) {
					HandleGenericArgReferenceType (refType);
				}
			}
		}

		void HandleFunctionType (SwiftFunctionType funcType)
		{
			if (funcType.Parameters is SwiftBoundGenericType boundType) {
				HandleBoundGenericType (boundType);
			} else if (funcType.Parameters is SwiftTupleType tupleType) {
				HandleTupleType (tupleType);
			} else if (funcType.Parameters is SwiftGenericArgReferenceType refType) {
				HandleGenericArgReferenceType (refType);
			}
		}

		void HandleBoundGenericType (SwiftBoundGenericType boundType)
		{
			foreach (var bound in boundType.BoundTypes) {
				if (bound is SwiftGenericArgReferenceType refType)
					HandleGenericArgReferenceType (refType);
			}
		}

		void HandleTupleType (SwiftTupleType tupleType)
		{
			foreach (var content in tupleType.Contents) {
				if (content is SwiftBoundGenericType boundGType) {
					HandleBoundGenericType (boundGType);
				} else if (content is SwiftFunctionType cFuncType) {
					HandleFunctionType (cFuncType);
				} else if (content is SwiftGenericArgReferenceType refType) {
					HandleGenericArgReferenceType (refType);
				} else if (content is SwiftTupleType innerTupleType) {
					HandleTupleType (innerTupleType);
				}
			}
		}

		void HandleGenericArgReferenceType (SwiftGenericArgReferenceType refType)
		{
			AddIfNotPresent (new DBGenericParameter (refType.Depth, refType.Index));
		}

		void AddIfNotPresent (DBGenericParameter newGP)
		{
			if (GenericParameters.Count == 0) {
				GenericParameters.Add (newGP);
				return;
			}

			foreach (var oldGP in GenericParameters) {
				if (oldGP.Depth == newGP.Depth && oldGP.Index == newGP.Index) {
					return;
				}
			}
			GenericParameters.Add (newGP);
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
					AddIfNotPresent (gp);
			}
		}
	}
}
