// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using SwiftRuntimeLibrary;

namespace Symbolicator {
	public class Program {
		static void Main (string[] args) {

			var tasks = new Dictionary<Type, string> {
				// Type holding the constants / Path to library to look for symbols.
				[typeof (SwiftCoreConstants)] = "/usr/lib/swift/libswiftCore.dylib",
				[typeof (XamGlueConstants)] = "../../swiftglue/bin/Debug/iphone/FinalProduct/XamGlue",
			};

			Console.WriteLine ("Building binding-tools-for-swift...");
			Tools.Exec ("/usr/bin/make", "-C", "../..", "all");
			Console.WriteLine ("Done building binding-tools-for-swift...");

			foreach (var task in tasks) {
				var code = Tools.UpdateMangledConstants (task.Key, task.Value);
				Tools.WriteToFile (task.Key, code);
				Console.WriteLine ($"{task.Key.Name} updated!");
			}
		}
	}
}

