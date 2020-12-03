using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace BindingNemo {
	class Program {
		static void Main (string [] args)
		{
			BindingNemoOptions options = new BindingNemoOptions ();
			var extra = options.optionsSet.Parse (args);
			//options.dylibLibraryList = new List<string> () { "libswiftCore" };

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return;
			}

			//if (options.dylibLibraryList.Count == 0){
			//	Console.WriteLine ("library is empty. Try --library=<dylib name>");
			//	return;
			//}

			//options.platform = "iphoneos";
			//options.architecture = "arm64";

			if (string.IsNullOrEmpty (options.platform)) {
				options.platform = "all";
			} else if (!options.validPlatform.Contains (options.platform.ToLower ())) {
				Console.WriteLine ("Platform was not recognized");
				return;
			}

			if (string.IsNullOrEmpty (options.architecture)) {
				options.architecture = "all";
			} else if (!options.validArchitecture.Contains (options.architecture.ToLower ())) {
				Console.WriteLine ("Architecture was not recognized");
				return;
			}

			Console.WriteLine ($"{options.platform}");
			Console.WriteLine ($"{options.architecture}");

			string bashString = BuildBashString ("all", options.platform, options.architecture);
			Console.WriteLine (bashString);
			var libraries = GetLibraries (bashString);

			WriteXml.CreateXmlFile (libraries);

			//foreach (var lib in libraries) {
			//	if (string.IsNullOrEmpty (lib.Value))
			//		continue;
			//	try {
			//		var errors = new ErrorHandling ();
			//		var mi = ModuleInventory.FromFile (lib.Value, errors);
			//		WriteXml.CreateXmlFile (lib.Key, mi);
			//	} catch (Exception e) {
			//		Console.WriteLine ($"Could not create xml for {lib.Key} - {lib.Value}. {e.Message}");
			//	}
			//}
		}

		static string BuildBashString (string name, string platform, string architecture)
		{
			string n;
			string p;
			if (name == "all")
				n = "*.dylib";
			else
				n = name;

			if (platform == "all")
				p = "*";
			else
				p = platform;

			if (architecture == "all") {
				//return $"find ../SwiftToolchain*/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/{p}/ -type f -iname \"{n}\"";
				return $"find ../../../SwiftToolchain*/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/{p}/ -type f -iname \"{n}\"";
			} else {
				//return $"find ../SwiftToolchain*/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/{p}/{architecture}/ -type f -iname \"{n}\"";
				return $"find ../../../SwiftToolchain*/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/{p}/{architecture}/ -type f -iname \"{n}\"";
			}
		}

		static Dictionary<string, string> GetLibraries (string bashString)
		{
			Dictionary<string, string> libraries = new Dictionary<string, string> ();
			var shellOutput = Shell.RunBash (bashString);
			var libraryPaths = shellOutput.Split ('\n');

			foreach (var lib in libraryPaths) {
				// Question for Steve, are the dylibs different for different architectures & platforms
				// I think probably, but here I am ignoring them
				var libName = lib.Split ('/').Last ().Split ('.').First ();
				if (!libraries.ContainsKey (libName))
					libraries.Add (libName, lib);
			}
			return libraries;
		}
	}
}