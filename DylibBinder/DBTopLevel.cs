using System;
using System.Collections.Generic;
using Dynamo;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class DBTopLevel {
		public DBTopLevel (string xmlVersion, string moduleName, string swiftVersion, ModuleInventory moduleInventory)
		{
			XmlVersion = Exceptions.ThrowOnNull (xmlVersion, "xmlVersion");
			ModuleName = Exceptions.ThrowOnNull (moduleName, "moduleName");
			SwiftVersion = Exceptions.ThrowOnNull (swiftVersion, "swiftVersion");
			Mi = Exceptions.ThrowOnNull (moduleInventory, "moduleInventory");
			DBTypeDeclarations = new DBTypeDeclarations (Mi);
		}

		public string XmlVersion { get; }
		public string ModuleName { get; }
		public string SwiftVersion { get; }
		public ModuleInventory Mi { get; }
		public DBTypeDeclarations DBTypeDeclarations { get; }
	}
}
