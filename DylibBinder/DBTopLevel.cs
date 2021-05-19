using System;
using SwiftRuntimeLibrary;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBTopLevel {
		public string SwiftVersion { get; }
		public ModuleInventory Mi { get; }
		public DBTypeDeclarations DBTypeDeclarations { get; }

		public DBTopLevel (ModuleInventory moduleInventory, string ignoreListPath, string swiftVersion)
		{
			SwiftVersion = Exceptions.ThrowOnNull (swiftVersion, nameof (swiftVersion));
			Mi = Exceptions.ThrowOnNull (moduleInventory, nameof (moduleInventory));
			DBTypeDeclarations = new DBTypeDeclarations (Mi, ignoreListPath);
		}
	}
}
