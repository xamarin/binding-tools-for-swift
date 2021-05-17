using System;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class DBAssociatedType {
		public string Name { get; }

		public DBAssociatedType (string name)
		{
			Name = Exceptions.ThrowOnNull (name, nameof (name));
		}
	}

	internal class DBAssociatedTypes {
		public HashSet<DBAssociatedType> AssociatedTypeCollection { get; } = new HashSet<DBAssociatedType> (new DBAssociatedTypeComparer ());

		public DBAssociatedTypes ()
		{
		}

		public DBAssociatedTypes (DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			AssociatedTypeCollection.UnionWith (typeDeclaration.GetAssociatedTypes ());
		}
	}

	// interface for having an AssociatedTypes Property
	internal interface IAssociatedTypes {
		public DBAssociatedTypes AssociatedTypes { get; }
	}

	internal static class DBAssociatedTypesExtensions {
		public static List<DBAssociatedType> GetAssociatedTypes (this SwiftBaseFunctionType signature)
		{
			Exceptions.ThrowOnNull (signature, nameof (signature));
			return signature.EachParameter.SelectMany (t => AssociatedTypesSwitch (t)).ToList ();
		}

		public static List<DBAssociatedType> GetAssociatedTypes (this SwiftType swiftType)
		{
			Exceptions.ThrowOnNull (swiftType, nameof (swiftType));
			return AssociatedTypesSwitch (swiftType);
		}

		static List<DBAssociatedType> AssociatedTypesSwitch (object type) => type switch {
			SwiftGenericArgReferenceType genericArgType => GetAssociatedTypes (genericArgType),
			SwiftFunctionType funcType => GetAssociatedTypes (funcType),
			_ => new List<DBAssociatedType> ()
		};

		static List<DBAssociatedType> GetAssociatedTypes (SwiftFunctionType funcType)
		{
			Exceptions.ThrowOnNull (funcType, nameof (funcType));
			return funcType.Parameters is SwiftGenericArgReferenceType genericArgType ? GetAssociatedTypes (genericArgType) : new List<DBAssociatedType> ();
		}

		static List<DBAssociatedType> GetAssociatedTypes (SwiftGenericArgReferenceType genericArgType)
		{
			Exceptions.ThrowOnNull (genericArgType, nameof (genericArgType));
			return genericArgType.AssociatedTypePath.Select (t => new DBAssociatedType (t)).ToList ();
		}

		public static List<DBAssociatedType> GetChildrenAssociatedTypes<T> (this IEnumerable<T> iEnumerableInstance)
		{
			Exceptions.ThrowOnNull (iEnumerableInstance, nameof (iEnumerableInstance));
			return iEnumerableInstance.Where (t => t is IAssociatedTypes).Cast<IAssociatedTypes> ().SelectMany (t => t.AssociatedTypes.AssociatedTypeCollection).ToList ();
		}
	}

	internal class DBAssociatedTypeComparer : EqualityComparer<DBAssociatedType> {
		public override bool Equals (DBAssociatedType at1, DBAssociatedType at2)
		{
			if (at1 == null && at2 == null)
				return true;
			else if (at1 == null || at2 == null)
				return false;
			return at1.Name == at2.Name;
		}

		public override int GetHashCode (DBAssociatedType at)
			=> at == null ? 1 : at.Name.GetHashCode ();
	}
}
