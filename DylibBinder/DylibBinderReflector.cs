using System;
using SwiftReflector;
using SwiftReflector.Inventory;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	public class DylibBinderReflector {
		public static void Reflect (string dylibPath, string outputPath, string ignoreListPath = null, string swiftVersion = "5.0")
		{
			var errors = new ErrorHandling ();
			var mi = ModuleInventory.FromFile (Exceptions.ThrowOnNull (dylibPath, nameof (dylibPath)), errors);
			var dBTopLevel = new DBTopLevel (mi, ignoreListPath, swiftVersion);
			XmlGenerator.WriteDBToFile (Exceptions.ThrowOnNull (dBTopLevel, nameof (dBTopLevel)), Exceptions.ThrowOnNull (outputPath, nameof (outputPath)));
		}
	}
}
