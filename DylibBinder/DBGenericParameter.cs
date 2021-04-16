using System;
using System.Collections.Generic;
using System.Linq;
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

		public DBGenericParameters (SwiftBaseFunctionType signature)
			=> GenericParameters.AddRange (signature.GetGenericParameters ());

		public DBGenericParameters (SwiftType swiftType)
			=> GenericParameters.AddRange (swiftType.GetGenericParameters ());

		public DBGenericParameters (DBTypeDeclaration typeDeclaration)
			=> GenericParameters.AddRange (typeDeclaration.ParseTopLevelGenerics ());

		public HashSet<DBGenericParameter> GenericParameters { get; } = new HashSet<DBGenericParameter> (new DBGenericParameterComparer ());
	}

	internal static class DBGenericParameterExtensions {
		public static List<DBGenericParameter> GetGenericParameters (this SwiftBaseFunctionType signature)
			=> FilterGenericParameterSource (signature.Parameters, signature.ReturnType);

		public static List<DBGenericParameter> GetGenericParameters (this SwiftType swiftType)
			=> FilterGenericParameterSource (swiftType);

		static List<DBGenericParameter> FilterGenericParameterSource (params SwiftType [] types)
			=> types.SelectMany (t => GenericParameterSwitch (t)).ToList ();

		static List<DBGenericParameter> GenericParameterSwitch (object type) => type switch {
			SwiftFunctionType funcType => GetGenericParameters (funcType),
			SwiftBoundGenericType boundType => GetGenericParameters (boundType),
			SwiftTupleType tupleType => GetGenericParameters (tupleType),
			SwiftGenericArgReferenceType refType => GetGenericParameters (refType),
			_ => new List<DBGenericParameter> ()
		};

		static List<DBGenericParameter> GetGenericParameters (SwiftFunctionType funcType)
			=> GenericParameterSwitch (funcType.Parameters);

		static List<DBGenericParameter> GetGenericParameters (SwiftBoundGenericType boundType)
			=> boundType.BoundTypes.Where (t => t is SwiftGenericArgReferenceType).SelectMany (t => GetGenericParameters (t as SwiftGenericArgReferenceType)).ToList ();

		static List<DBGenericParameter> GetGenericParameters (SwiftTupleType tupleType)
			=> tupleType.Contents.SelectMany (t => GenericParameterSwitch (t)).ToList ();

		static List<DBGenericParameter> GetGenericParameters (SwiftGenericArgReferenceType refType)
			=> new () { new DBGenericParameter (refType.Depth, refType.Index) };
	}

	class DBGenericParameterComparer : EqualityComparer<DBGenericParameter> {
		public override bool Equals (DBGenericParameter gp1, DBGenericParameter gp2)
			=> gp1.Name == gp2.Name;

		public override int GetHashCode (DBGenericParameter gp)
			=> gp.Name.GetHashCode ();
	}
}
