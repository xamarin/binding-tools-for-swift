// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Mono.Options;
using SwiftReflector;


namespace typeomatic {
	public class TypeOMaticOptions {
		// This implementation of RealPath is here because we need specific behavior
		// that other implementations do not give or do not have the proper granularity
		// to allow us to wrap them.
		// First, we need RealPath to be able to work on paths that might not exist (yet).
		// Second, we need them to work on null.
		// In the Mono.Posix assembly, there is an implementation of RealPath which fails on
		// non-existent paths and on null.
		// The implementation of GetCanonicalPath is private, so I can't call it to handle
		// path elements that are . or ..
		public static string RealPath (string path)
		{
			// intentional: return null on null so that
			// var foo = ReadPath(somethingNull) works
			if (String.IsNullOrEmpty (path))
				return path;

			// A custom implementation for inexistent paths, since realpath doesn't support those
			if (!File.Exists (path) && !Directory.Exists (path)) {
				// Path.GetFullPath will resolve '.' and '..', and it works on non-existent paths.
				path = Path.GetFullPath (path);
				// A containing directory will exist, so run RealPath recursively on parent directories.
				return Path.Combine (RealPath (Path.GetDirectoryName (path)), Path.GetFileName (path));
			}

			// The path exists, so we can use realpath.
			// Mono.Unix.UnixPath.GetRealPath doesn't resolve symlinks unless it's the last component.
			// Mono.Unix.UnixPath.GetCompleteRealPath doesn't resolve relative paths in symlinks.
			// So P/Invoke the platforms's realpath method instead.
			return GetRealPath (path);
		}

		[DllImport ("/usr/lib/libSystem.dylib", SetLastError = true)]
		static extern string realpath (string path, IntPtr zero);

		internal static string GetRealPath (string path)
		{
			var rv = realpath (path, IntPtr.Zero);
			if (rv != null)
				return rv;

			var errno = Marshal.GetLastWin32Error ();
			if (errno != 0)
				throw new Exception ($"Unable to canonicalize the path '{path}': {strerror (errno)} ({errno}).");
			return path;
		}

		[DllImport ("/usr/lib/libSystem.dylib", SetLastError = true, EntryPoint = "strerror")]
		static extern IntPtr _strerror (int errno);

		internal static string strerror (int errno)
		{
			return Marshal.PtrToStringAuto (_strerror (errno));
		}

		static string FindPathFromEnvVariable (string pathSuffix)
		{
			string path = Environment.GetEnvironmentVariable ("SOM_PATH");
			if (path != null) {
				var fullPath = Path.Combine (path.Replace ("\n", ""), pathSuffix);
				if (Directory.Exists (fullPath))
					return fullPath;
			}
			return null;
		}

		static string GetExecutableDirectory ()
		{
			return Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
		}

		static string FindPathRelativeToExecutable (string pathSuffix)
		{
			var path = Path.Combine (GetExecutableDirectory (), pathSuffix);
			return Directory.Exists (path) ? path : null;
		}

		public static string FindSwiftLibPath ()
		{
			return FindPathFromEnvVariable ("bin/swift/lib/swift/") ?? FindPathRelativeToExecutable ("../../bin/swift/lib/swift/");
		}

		public void PrintUsage (TextWriter writer)
		{
			var location = Assembly.GetEntryAssembly ()?.Location;
			string exeName = (location != null) ? Path.GetFileName (location) : "";
			writer.WriteLine ($"Usage:");
			writer.WriteLine ($"\t{exeName} [options]");
			writer.WriteLine ("Options:");
			optionsSet.WriteOptionDescriptions (writer);
			return;
		}

		OptionSet optionsSet;
		public string SwiftLibPath { get; set; }
		public bool PrintHelp { get; set; }
		public PlatformName Platform { get; set; }
		public List<string> Namespaces { get; private set; }
		public string Framework { get; set; }
		public TextWriter OutputWriter { get; set; }


		public TypeOMaticOptions ()
		{
			Namespaces = new List<string> ();
			Platform = PlatformName.iOS;
			OutputWriter = Console.Out;

			// create an option set that will be used to parse the different
			// options of the command line.
			optionsSet = new OptionSet {
				{ "platform=", "target platform, one of: macOS|mac, iOS|iphone, watchOS|watch, tvOS|appletv, default is iOS", platform => {
					Platform = ToPlatformName (platform);
				}},
				{ "swift-lib-path=", "swift library directory path.", p => {
					if (!string.IsNullOrEmpty (p))
						SwiftLibPath = Path.GetFullPath (p);

				}},
				{ "namespace=", "name space to include (not specifying namespaces will default to all, can be used multiple times)", @namespace => {
					Namespaces.Add (@namespace);
				}},
				{ "xamglue-framework=", "/path/to/XamGlue.framework", framework => {
					Framework = framework;
				}},
				{ "output=", "optional output file", filename => {
					OutputWriter = new StreamWriter (filename);
				}},
				{ "h|?|help", "prints this message", h => {
					PrintHelp |=h != null;
				}}
			};
		}


		static PlatformName ToPlatformName(string platform)
		{
			switch (platform) {
			case "macOS":
			case "mac":
				return PlatformName.macOS;
			case "iOS":
			case "iphone":
				return PlatformName.iOS;
			case "watchOS":
			case "watch":
				return PlatformName.watchOS;
			case "tvOS":
			case "appletv":
				return PlatformName.tvOS;
			default:
				return PlatformName.None;
			}
		}

		public List<String> ParseCommandLine (string [] args)
		{
			// set the default values for the option

			var extra = optionsSet.Parse (args);

			if (SwiftLibPath == null) {
				SwiftLibPath = FindSwiftLibPath ();
			}
			SwiftLibPath = RealPath (SwiftLibPath);
			return extra;
		}
	}
}
