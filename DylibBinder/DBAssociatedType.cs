using System;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector;
using SwiftReflector.SwiftXmlReflection;

namespace DylibBinder {
	internal class DBAssociatedType {
		public DBAssociatedType (string name)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentException ("DBAssociateType name cannot be null or empty");
			Name = name;
		}

		public string Name { get; }
	}

	internal class DBAssociatedTypes {
		public DBAssociatedTypes ()
		{
		}

		public DBAssociatedTypes (HashSet<DBAssociatedType> hashset)
		{
			AssociatedTypes = hashset;
		}

		public DBAssociatedTypes (DBTypeDeclaration typeDeclaration)
		{
			AssociatedTypes.AddRange (typeDeclaration.GetAssociatedTypes ());
		}

		public HashSet<DBAssociatedType> AssociatedTypes { get; } = new HashSet<DBAssociatedType> (new DBAssociatedTypeComparer ());
	}

	// interface for having an AssociatedTypes Property
	internal interface IAssociatedTypes {
		public DBAssociatedTypes AssociatedTypes { get; }
	}

	internal static class DBAssociatedTypesExtensions {
		public static void Add (this DBAssociatedTypes type1, DBAssociatedTypes type2)
		{
			foreach (var associatedType in type2.AssociatedTypes) {
				type1.AssociatedTypes.Add (associatedType);
			}
		}

		public static void Add (this DBAssociatedTypes dBAssociatedTypes, List<DBAssociatedType> list)
		{
			foreach (var item in list) {
				dBAssociatedTypes.AssociatedTypes.Add (item);
			}
		}

		public static List<DBAssociatedType> GetAssociatedTypes (this DBTypeDeclaration typeDeclaration)
			=> typeDeclaration.Kind == TypeKind.Protocol ? GetAssociatedTypes(typeDeclaration.Funcs) : GetAssociatedTypes (typeDeclaration.Funcs, typeDeclaration.Properties);

		static List<DBAssociatedType> GetAssociatedTypes (params IAssociatedTypes [] items)
			=> items.SelectMany (t => t.AssociatedTypes.AssociatedTypes).ToList ();

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
			=> genericArgType.AssociatedTypePath.Select (t => CreateAssociatedType (t)).ToList ();

		static DBAssociatedType CreateAssociatedType (string name)
			=> new (name);

		public static List<DBAssociatedType> GetChildrenAssociatedTypes<T> (this IEnumerable<T> iEnumerableInstance)
			=> iEnumerableInstance.Where (t => t is IAssociatedTypes).Cast<IAssociatedTypes> ().SelectMany (t => t.AssociatedTypes.AssociatedTypes).ToList ();
	}

	internal class DBAssociatedTypeComparer : EqualityComparer<DBAssociatedType> {
		public override bool Equals (DBAssociatedType at1, DBAssociatedType at2)
			=> at1.Name == at2.Name;

		public override int GetHashCode (DBAssociatedType at)
			=> at.Name.GetHashCode ();
	}
}
