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

			// these will eventually come from tom-swifty
			options.SwiftLibPath = Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib";
			options.ModuleName = "swiftCore";
			options.TypeDatabasePaths.Add (Directory.GetCurrentDirectory () + "/../../../bindings");
			options.DylibPath = Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/appletvos//libswiftCore.dylib";
			SwiftTypeToString.TypeDatabasePaths = options.TypeDatabasePaths;

			var errors = new ErrorHandling ();
			var mi = ModuleInventory.FromFile (options.DylibPath, errors);
			var dBTopLevel = new DBTopLevel ("1.0", "Swift", "5.0", mi);
			var xmlGenerator = new XmlGenerator (dBTopLevel, "../../Modules/NewXml.xml");
		}
	}
}