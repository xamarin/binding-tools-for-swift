using System;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector;

namespace DylibBinder {
	internal class DBAssociatedType {
		public string Name { get; }

		public DBAssociatedType (string name)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentException ("DBAssociateType name cannot be null or empty");
			Name = name;
		}
	}

	internal class DBAssociatedTypes {
		public HashSet<DBAssociatedType> AssociatedTypeCollection { get; } = new HashSet<DBAssociatedType> (new DBAssociatedTypeComparer ());

		public DBAssociatedTypes ()
		{
		}

		public DBAssociatedTypes (DBTypeDeclaration typeDeclaration)
		{
			AssociatedTypeCollection.UnionWith (typeDeclaration.GetAssociatedTypes ());
		}
	}

	// interface for having an AssociatedTypes Property
	internal interface IAssociatedTypes {
		public DBAssociatedTypes AssociatedTypes { get; }
	}

	internal static class DBAssociatedTypesExtensions {
		public static List<DBAssociatedType> GetAssociatedTypes (this SwiftBaseFunctionType signature)
			=> signature.EachParameter.SelectMany (t => AssociatedTypesSwitch (t)).ToList ();

		public static List<DBAssociatedType> GetAssociatedTypes (this SwiftType swiftType)
			=> AssociatedTypesSwitch (swiftType);

		static List<DBAssociatedType> AssociatedTypesSwitch (object type) => type switch {
			SwiftGenericArgReferenceType genericArgType => GetAssociatedTypes (genericArgType),
			SwiftFunctionType funcType => GetAssociatedTypes (funcType),
			_ => new ()
		};

		static List<DBAssociatedType> GetAssociatedTypes (SwiftFunctionType funcType)
			=> funcType.Parameters is SwiftGenericArgReferenceType genericArgType ? GetAssociatedTypes (genericArgType) : new ();

		static List<DBAssociatedType> GetAssociatedTypes (SwiftGenericArgReferenceType genericArgType)
			=> genericArgType.AssociatedTypePath.Select (t => new DBAssociatedType (t)).ToList ();

		public static List<DBAssociatedType> GetChildrenAssociatedTypes<T> (this IEnumerable<T> iEnumerableInstance)
			=> iEnumerableInstance.Where (t => t is IAssociatedTypes).Cast<IAssociatedTypes> ().SelectMany (t => t.AssociatedTypes.AssociatedTypeCollection).ToList ();
	}

	internal class DBAssociatedTypeComparer : EqualityComparer<DBAssociatedType> {
		public override bool Equals (DBAssociatedType at1, DBAssociatedType at2)
			=> at1.Name == at2.Name;

		public override int GetHashCode (DBAssociatedType at)
			=> at.Name.GetHashCode ();
	}
}
