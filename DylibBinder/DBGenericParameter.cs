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
			GenericParameters.AddRange (signature.AssignGenericParameters ());
		}

		public DBGenericParameters (SwiftType swiftType)
		{
			GenericParameters.AddRange (swiftType.AssignGenericParameters ());
		}

		public DBGenericParameters (DBTypeDeclaration typeDeclaration)
		{
			GenericParameters.AddRange (typeDeclaration.ParseTopLevelGenerics ());
		}

		public HashSet<DBGenericParameter> GenericParameters { get; } = new HashSet<DBGenericParameter> (new DBGenericParameterComparer ());

		public static List<DBGenericParameter> FilterGenericParameterSource (params SwiftType [] types)
		{
			var genericParameters = new List<DBGenericParameter> ();
			foreach (var type in types) {
				genericParameters.AddRange (GenericParameterSwitch (type));
			}
			return genericParameters;
		}

		public static List<DBGenericParameter> GetGenericParameters (SwiftFunctionType funcType)
		{
			var genericParameters = new List<DBGenericParameter> ();
			genericParameters.AddRange (GenericParameterSwitch (funcType.Parameters));
			return genericParameters;
		}

		public static List<DBGenericParameter> GetGenericParameters (SwiftBoundGenericType boundType)
		{
			var genericParameters = new List<DBGenericParameter> ();
			foreach (var bound in boundType.BoundTypes) {
				if (bound is SwiftGenericArgReferenceType refType)
					genericParameters.AddRange (GetGenericParameters (refType));
			}
			return genericParameters;
		}

		public static List<DBGenericParameter> GetGenericParameters (SwiftTupleType tupleType)
		{
			var genericParameters = new List<DBGenericParameter> ();
			foreach (var content in tupleType.Contents) {
				genericParameters.AddRange (GenericParameterSwitch (content));
			}
			return genericParameters;
		}

		public static List<DBGenericParameter> GetGenericParameters (SwiftGenericArgReferenceType refType)
		{
			return new List<DBGenericParameter> () { new DBGenericParameter (refType.Depth, refType.Index)};
		}

		public static List<DBGenericParameter> GenericParameterSwitch (object type) => type switch {
			SwiftFunctionType funcType => GetGenericParameters (funcType),
			SwiftBoundGenericType boundType => GetGenericParameters (boundType),
			SwiftTupleType tupleType => GetGenericParameters (tupleType),
			SwiftGenericArgReferenceType refType => GetGenericParameters (refType),
			_ => new List<DBGenericParameter> ()
		};
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

	internal static class SwiftBaseFunctionTypeExtensions {
		public static List<DBGenericParameter> AssignGenericParameters (this SwiftBaseFunctionType signature)
		{
			return DBGenericParameters.FilterGenericParameterSource (signature.Parameters, signature.ReturnType);
		}
	}

	internal static class SwiftTypeExtensions {
		public static List<DBGenericParameter> AssignGenericParameters (this SwiftType swiftType)
		{
			return DBGenericParameters.FilterGenericParameterSource (swiftType);
		}
	}

}
