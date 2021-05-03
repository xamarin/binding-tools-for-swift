using System;
using System.Collections.Generic;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBInnerTypes {
		public List<DBTypeDeclaration> InnerTypeCollection { get; } = new List<DBTypeDeclaration> ();

		public DBInnerTypes (SwiftClassName swiftClassName)
		{
			var name = swiftClassName.ToFullyQualifiedName ();
			if (DBTypeDeclarations.InnerXDictionary.TryGetValue (name, out List<ClassContents> innerTypeList)) {
				foreach (var innerType in innerTypeList) {
					InnerTypeCollection.Add (new DBTypeDeclaration (innerType));
				}
			}
		}
	}
}
