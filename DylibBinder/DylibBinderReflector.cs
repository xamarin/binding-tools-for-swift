using System;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class DylibBinderReflector {
		public static void Reflect (string dylibPath, string outputPath, string swiftVersion = "5.0")
		{
			var errors = new ErrorHandling ();
			var mi = ModuleInventory.FromFile (dylibPath, errors);
			var dBTopLevel = new DBTopLevel (mi, swiftVersion);
			using var xmlGenerator = new XmlGenerator (dBTopLevel, outputPath);
		}
	}
}
