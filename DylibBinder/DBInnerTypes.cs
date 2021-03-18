using System;
using System.Collections.Generic;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBInnerTypes {
		public DBInnerTypes (SwiftClassName swiftClassName)
		{
			var name = swiftClassName.ToString ();
			if (DBTypeDeclarations.InnerXDictionary.ContainsKey (name)) {
				foreach (var innerType in DBTypeDeclarations.InnerXDictionary[name]) {
					InnerTypes.Add (new DBTypeDeclaration (innerType));
				}
			}
		}

		public List<DBTypeDeclaration> InnerTypes { get; } = new List<DBTypeDeclaration> ();
	}
}
