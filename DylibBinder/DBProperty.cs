﻿using System;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBProperty {
		public DBProperty (PropertyContents propertyContents)
		{
			Name = propertyContents.Name.Name;
			IsStatic = propertyContents.Getter.IsStatic;
			Type = SwiftTypeToString.MapSwiftTypeToString (propertyContents.Getter.ReturnType, propertyContents.Class.ClassName.Module.Name);
			GenericParameters = new DBGenericParameters (propertyContents.Getter.ReturnType);

			Getter = new DBFunc (this, "Getter");
			if (propertyContents.Setter != null) 
				Setter = new DBFunc (this, "Setter");
		}

		public string Name { get; private set; }
		public bool IsStatic { get; }
		public string Type { get; }
		public DBFunc Getter { get; }
		public DBFunc Setter { get; }
		public DBGenericParameters GenericParameters { get; }

		public TypeAccessibility Accessibility { get; } = TypeAccessibility.Public;
		public bool IsPossiblyIncomplete { get; } = false;
		public bool IsDeprecated { get; } = false;
		public bool IsUnavailable { get; } = false;
		public bool IsOptional { get; } = false;
		public Storage Storage { get; } = Storage.Addressed;
	}

	internal class DBProperties {
		public DBProperties (ClassContents classContents)
		{
			var properties = SortedSetExtensions.CreatePropertySortedSet ();
			properties.AddRange (classContents.Properties.Values, classContents.StaticProperties.Values);
			foreach (var property in properties) {
				if (property.Name.Name.IsPublic () && !IsMetaClass (property.Getter.ReturnType))
					Properties.Add (new DBProperty (property));
			}
		}

		public List<DBProperty> Properties { get; } = new List<DBProperty> ();

		bool IsMetaClass (SwiftType swiftType)
		{
			return swiftType.Type == CoreCompoundType.MetaClass;
		}
	}
}