using System;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class DylibBinderReflector {
		public static void Reflect (string dylibPath, string outputPath, string ignoreListPath, string swiftVersion = "5.0")
		{
			var errors = new ErrorHandling ();
			var mi = ModuleInventory.FromFile (dylibPath, errors);
			var dBTopLevel = new DBTopLevel (mi, ignoreListPath, swiftVersion);
			XmlGenerator.WriteDBToFile (dBTopLevel, outputPath);
		}
	}
}
