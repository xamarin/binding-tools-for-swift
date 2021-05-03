using System;
using Dynamo;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBTopLevel {
		public string SwiftVersion { get; }
		public ModuleInventory Mi { get; }
		public DBTypeDeclarations DBTypeDeclarations { get; }

		public DBTopLevel (ModuleInventory moduleInventory, string swiftVersion)
		{
			SwiftVersion = swiftVersion;
			Mi = Exceptions.ThrowOnNull (moduleInventory, "moduleInventory");
			DBTypeDeclarations = new DBTypeDeclarations (Mi);
		}
	}
}
