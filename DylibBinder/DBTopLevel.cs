using System;
using SwiftRuntimeLibrary;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBTopLevel {
		public string SwiftVersion { get; }
		public ModuleInventory Mi { get; }
		public DBModules Modules { get; }
		public DBTypeDeclarations DBTypeDeclarations { get; }
		public DBFuncs GlobalFunctions { get; }

		public DBTopLevel (ModuleInventory moduleInventory, string ignoreListPath, string swiftVersion)
		{
			SwiftVersion = Exceptions.ThrowOnNull (swiftVersion, nameof (swiftVersion));
			Mi = Exceptions.ThrowOnNull (moduleInventory, nameof (moduleInventory));
			Modules = new DBModules (Mi, ignoreListPath);
		}
	}
}
