using System;
using System.Collections.Generic;
using Dynamo.SwiftLang;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBGenericParameter : IEquatable<DBGenericParameter> {
		public DBGenericParameter (int depth, int index)
		{
			Depth = depth;
			Index = index;
			Name = SLGenericReferenceType.DefaultNamer (depth, index);
		}

		public int Depth { get; }
		public int Index { get; }
		public string Name { get; }

		public bool Equals (DBGenericParameter other)
		{
			return this.Depth == other.Depth && this.Index == other.Index;
		}
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

		public List<DBGenericParameter> GenericParameters { get; } = new List<DBGenericParameter> ();


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
					return;
				case SwiftBoundGenericType boundType:
					HandleType (boundType);
					return;
				case SwiftTupleType tupleType:
					HandleType (tupleType);
					return;
				case SwiftGenericArgReferenceType refType:
					HandleType (refType);
					return;
				default:
					return;
				}
			}
		}

		void HandleType (SwiftFunctionType funcType)
		{
			switch (funcType.Parameters) {
			case SwiftBoundGenericType boundType:
				HandleType (boundType);
				return;
			case SwiftTupleType tupleType:
				HandleType (tupleType);
				return;
			case SwiftGenericArgReferenceType refType:
				HandleType (refType);
				return;
			default:
				return;
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
					return;
				case SwiftFunctionType cFuncType:
					HandleType (cFuncType);
					return;
				case SwiftGenericArgReferenceType refType:
					HandleType (refType);
					return;
				case SwiftTupleType innerTupleType:
					HandleType (innerTupleType);
					return;
				default:
					return;
				}
			}
		}

		void HandleType (SwiftGenericArgReferenceType refType)
		{
			AddIfNotPresent (new DBGenericParameter (refType.Depth, refType.Index));
		}

		void AddIfNotPresent (DBGenericParameter newGP)
		{
			if (GenericParameters.Count == 0) {
				GenericParameters.Add (newGP);
				return;
			}

			if (!GenericParameters.Contains (newGP))
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
