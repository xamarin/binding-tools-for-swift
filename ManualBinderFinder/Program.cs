using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace ManualBinderFinder {
	class Program {
		static void Main (string [] args)
		{
			ManualBinderFinderOptions options = new ManualBinderFinderOptions ();
			var extra = options.optionsSet.Parse (args);
			//options.dylibLibraryList = new List<string> () { "libswiftCore" };

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return;
			}

			if (options.dylibLibraryList.Count == 0){
				Console.WriteLine ("library is empty. Try --library=<dylib name>");
				return;
			}

			if (string.IsNullOrEmpty (options.platform)) {
				options.platform = "all";
			} else if (options.validPlatform.Contains (options.platform.ToLower ())) {
				Console.WriteLine ("Platform was not recognized");
				return;
			}

			if (string.IsNullOrEmpty (options.architecture)) {
				options.architecture = "all";
			} else if (options.validArchitecture.Contains (options.architecture.ToLower ())) {
				Console.WriteLine ("Architecture was not recognized");
				return;
			}

			Console.WriteLine ($"{options.platform}");
			Console.WriteLine ($"{options.architecture}");

			foreach (string lib in options.dylibLibraryList) {
				string libName = lib;
				if (!libName.Contains (".dylib")) {
					libName += ".dylib";
				}
				var shellOutput = Shell.RunBash ($"find ../SwiftToolchain*/ -iname {libName}");
				//var shellOutput = Shell.RunBash ($"find ../../../../SwiftToolchain*/ -iname {libName}");

				if (!shellOutput.Contains (".dylib")) {
					Console.WriteLine ($"{libName} was not valid");
					return;
				}
				string dylibPath = shellOutput.Substring (0, shellOutput.IndexOf (Environment.NewLine));
				string dylibName = dylibPath.Split ('/').Last ().Split ('.').First ();
				var errors = new ErrorHandling ();

				var mi = ModuleInventory.FromFile (dylibPath, errors);

				WriteXml.CreateXmlFile (dylibName, mi);
			}
			
		}
	}
}
