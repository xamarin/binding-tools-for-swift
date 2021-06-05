using System;
using System.Collections.Generic;
using SwiftReflector;
using SwiftReflector.Inventory;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class DBModule {
		public string Name { get; set; }
		public DBTypeDeclarations TypeDeclarations { get; set; }
		public DBFuncs GlobalFuncs { get; set; }

		public DBModule (ModuleInventory mi, SwiftName module, string ignoreListPath)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			Name = Exceptions.ThrowOnNull (module.Name, nameof (module.Name));
			TypeDeclarations = new DBTypeDeclarations (mi, module, ignoreListPath);
			GlobalFuncs = new DBFuncs (mi, module);
		}
	}

	internal class DBModules {
		public List<DBModule> ModuleCollection { get; set; } = new List<DBModule> ();

		public DBModules (ModuleInventory mi, string ignoreListPath)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			foreach (var module in mi.ModuleNames) {
				ModuleCollection.Add (new DBModule (mi, module, ignoreListPath));
			}
		}
	}
}
