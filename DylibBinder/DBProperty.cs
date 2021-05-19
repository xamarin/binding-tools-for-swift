using System;
using System.Collections.Generic;
using SwiftReflector;
using SwiftReflector.Inventory;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class DBProperty : IAssociatedTypes {
		public string Name { get; private set; }
		public bool IsStatic { get; }
		public string Type { get; }
		public DBFunc Getter { get; }
		public DBFunc Setter { get; }
		public DBGenericParameters GenericParameters { get; }
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		public TypeAccessibility Accessibility { get; } = TypeAccessibility.Public;
		public bool IsPossiblyIncomplete { get; } = false;
		public bool IsDeprecated { get; } = false;
		public bool IsUnavailable { get; } = false;
		public bool IsOptional { get; } = false;
		public Storage Storage { get; } = Storage.Addressed;

		public DBProperty (PropertyContents propertyContents)
		{
			Exceptions.ThrowOnNull (propertyContents, nameof (propertyContents));
			Name = propertyContents.Name.Name;
			IsStatic = propertyContents.Getter.IsStatic;
			AssociatedTypes.AssociatedTypeCollection.UnionWith (propertyContents.Getter.ReturnType.GetAssociatedTypes ());
			Type = SwiftTypeToString.MapSwiftTypeToString (propertyContents.Getter.ReturnType, propertyContents.Class.ClassName.Module.Name);
			GenericParameters = new DBGenericParameters (propertyContents.Getter.ReturnType);

			Getter = new DBFunc (this, "Getter");
			Setter = propertyContents.Setter != null ? new DBFunc (this, "Setter") : null;
		}
	}

	internal class DBProperties : IAssociatedTypes {
		public List<DBProperty> PropertyCollection { get; } = new List<DBProperty> ();
		public DBAssociatedTypes AssociatedTypes { get; } = new DBAssociatedTypes ();

		public DBProperties (ClassContents classContents)
		{
			Exceptions.ThrowOnNull (classContents, nameof (classContents));
			var properties = SortedSetExtensions.Create<PropertyContents> ();
			properties.AddRange (classContents.Properties.Values, classContents.StaticProperties.Values);
			foreach (var property in properties) {
				if (property.Name.Name.IsPublic () && !IsMetaClass (property.Getter.ReturnType))
					PropertyCollection.Add (new DBProperty (property));
			}
			AssociatedTypes.AssociatedTypeCollection.UnionWith (PropertyCollection.GetChildrenAssociatedTypes ());
		}

		bool IsMetaClass (SwiftType swiftType)
		{
			Exceptions.ThrowOnNull (swiftType, nameof (swiftType));
			return swiftType.Type == CoreCompoundType.MetaClass;
		}
	}
}
