using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

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

			options.SwiftLibPath = Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib";
			options.ModuleName = "swiftCore";

			string bashString = $"find {options.SwiftLibPath}/swift/*/ -type f -iname \"lib{options.ModuleName}.dylib\"";
			var libraries = GetLibraries (bashString);

			WriteXml.CreateXmlFile (libraries);
		}


		static Dictionary<string, string> GetLibraries (string bashString)
		{
			Dictionary<string, string> libraries = new Dictionary<string, string> ();
			var shellOutput = Shell.RunBash (bashString);
			var libraryPaths = shellOutput.Split ('\n');

			foreach (var lib in libraryPaths) {
				var libName = lib.Split ('/').Last ().Split ('.').First ();
				if (libName != "" && !libraries.ContainsKey (libName))
					libraries.Add (libName, lib);
			}
			return libraries;
		}
	}
}