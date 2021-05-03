using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;

namespace DylibBinder {
	class Program {
		static void Main (string [] args)
		{
			DylibBinderOptions options = new DylibBinderOptions ();
			var extra = options.optionsSet.Parse (args);

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return;
			}

			if (string.IsNullOrEmpty (options.DylibPath)) {
				Console.WriteLine ("The path to the Dylib was not included. Try using --dylibPath.");
				return;
			}

			if (string.IsNullOrEmpty (options.OutputPath)) {
				Console.WriteLine ("The path for the output xml was not included. Try using --outputPath.");
				return;
			}

			if (!File.Exists (options.DylibPath)) {
				Console.WriteLine ($"Unable to find the path to the Dylib: {options.DylibPath}.");
				return;
			}

			var errors = new ErrorHandling ();
			var mi = ModuleInventory.FromFile (options.DylibPath, errors);
			var dBTopLevel = new DBTopLevel (mi, options.SwiftVersion);
			using var xmlGenerator = new XmlGenerator (dBTopLevel, options.OutputPath);
		}
	}
}
