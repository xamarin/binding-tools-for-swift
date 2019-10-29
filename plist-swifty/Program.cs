// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using SwiftReflector.IOUtils;

using Mono.Options;

namespace plistswifty {
	class MainClass {
		public static int Main (string [] args)
		{
			var shouldShowHelp = false;
			string libPath = null;
			string outputPath = null;
			var options = new OptionSet { 
				{ "lib|l=", "Path of the library for which the plist will be generated.", p => libPath = p }, 
				{ "output|o=", "Path to which the generated plist will be written.", p => outputPath = p }, 
				{ "h|?|help", "Show this message and exit", h => shouldShowHelp = h != null },
			};

			var extra = options.Parse (args);
			if (extra.Count > 0) {
				// Warn about extra params that are ignored.
				Console.WriteLine ($"WARNING: The following extra parameters will be ignored: '{ String.Join (",", extra) }'");
			}
			

			if (shouldShowHelp) {
				Console.Out.WriteLine ("plist-swifty generates a basic Info.plist file for a given library.");
				Console.Out.WriteLine ("Most of the information for the file is pulled from the library itself.");
				Console.Out.WriteLine ("Some of the information is taken from Xcode itself.");
				Console.Out.WriteLine ("The output is a text version. If you need a binary, use plutil");
				Console.Out.WriteLine ("Usage:");
				options.WriteOptionDescriptions (Console.Out);
				return 0;
			}
			if (string.IsNullOrEmpty (libPath)) {
				Console.Out.WriteLine ("Missing required option -l=PATH");
				return 1;
			}
			
			if (string.IsNullOrEmpty (outputPath)) {
				Console.Out.WriteLine ("Missing required option -o=PATH");
				return 1;
			}

			if (!File.Exists (libPath)) {
				throw new FileNotFoundException ("unable to locate input library", args [0]);
			}
			if (!Directory.Exists (Path.GetDirectoryName (outputPath)))
				Directory.CreateDirectory (Path.GetDirectoryName (outputPath));
			InfoPList.MakeInfoPList (libPath, outputPath);
			return 0;
		}
	}
}
