using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;

namespace DylibBinder {
	class Program {
		static int Main (string [] args)
		{
			DylibBinderOptions options = new DylibBinderOptions ();
			var extra = options.optionsSet.Parse (args);

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return 1;
			}

			if (string.IsNullOrEmpty (options.DylibPath)) {
				Console.WriteLine ("The path to the Dylib was not included. Try using --dylibPath.");
				return 1;
			}

			if (string.IsNullOrEmpty (options.OutputPath)) {
				Console.WriteLine ("The path for the output xml was not included. Try using --outputPath.");
				return 1;
			}

			if (!File.Exists (options.DylibPath)) {
				Console.WriteLine ($"Unable to find the path to the Dylib: {options.DylibPath}.");
				return 1;
			}

			if (!string.IsNullOrEmpty (options.IgnoreListPath)) {
				if (!File.Exists (options.IgnoreListPath)) {
					Console.WriteLine ($"Unable to find the path to the IgnoreList: {options.IgnoreListPath}.");
					return 1;
				}
			}

			var errors = new ErrorHandling ();
			var mi = ModuleInventory.FromFile (options.DylibPath, errors);
			var dBTopLevel = new DBTopLevel (mi, options.IgnoreListPath, options.SwiftVersion);
			XmlGenerator.WriteDBToFile (dBTopLevel, options.OutputPath);
			return 0;
		}
	}
}
