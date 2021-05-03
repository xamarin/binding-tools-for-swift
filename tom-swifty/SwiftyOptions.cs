// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using Mono.Options;

using SwiftReflector;
using SwiftReflector.Demangling;
using SwiftReflector.IOUtils;
using SwiftReflector.ExceptionTools;
using System.Text;
using Dynamo;

namespace tomswifty {
	public class SwiftyOptions {

		OptionSet optionsSet;

		public SwiftyOptions ()
		{
			TypeDatabasePaths = new List<string> ();
			DylibPaths = new List<string> ();
			ModulePaths = new List<string> ();
			Targets = new List<string> ();

			// create an option set that will be used to parse the different
			// options of the command line.
			optionsSet = new OptionSet { 
				{ "demangle", "Demangles the given swift symbols, printing human readable trees of each symbol.", p => {
					Demangle |=p != null;
				}},
				{ "swift-lib-path=", "swift library directory path.", p => {
					if (!string.IsNullOrEmpty (p))
						SwiftLibPath = Path.GetFullPath (p);
					
				}},
				{ "swift-bin-path=", "uses 'path' as the directory to search for the swift compiler", p => {
					if (!string.IsNullOrEmpty (p))
						SwiftBinPath = Path.GetFullPath (p);
				}},
				{ "retain-xml-reflection", "keeps the xml reflection files generated from the swift module.", p =>  { 
					RetainXmlReflection |=p != null;
				}},
				{ "retain-swift-wrappers", "keeps the swift wrapper source code in the output directory.", p => {
					RetainSwiftWrappingCode |=p != null;
				}},
				{ "pinvoke-class-prefix=", "use 'name' as a prefix for classes to hold PInvokes. Default is 'NativeMethods'", p => {
					PInvokeClassPrefix = p;
				}},
				{ "print-stack-trace", "prints a stack trace with each error.", p => {
					PrintStackTrace |=p != null;
				}},
				{ "wrapping-module-name=", "sets the swift wrapper module name to 'wrap-name'.", p => {
					if (p != null)
						WrappingModuleName = p;
				}},
				{ "module-name=", "sets the name of the module that will be processed.", p => {
					if (p != null)
						ModuleName = p;
				}},
				{ "global-class-name=", "use 'name' as the name of a class to hold global functions and properties.", p => {
					if (p != null)
						GlobalClassName = p;
				}},
				{ "arch=", "set the architecture to target. Default is 64.", (int arch) => {
					TargetPlatformIs64Bit = arch == 64;
				}},
				{ "L|library-directory=", "searches in directory for dylib files; can be used multiple times.", p => {
					if (!string.IsNullOrEmpty (p))
						DylibPaths.Add (Path.GetFullPath (p));
				}},
				{ "M=", "[module-directory] searches in directory for swiftmodule files; can be used multiple times", p => {
					if (!string.IsNullOrEmpty (p))
						ModulePaths.Add (Path.GetFullPath (p));
				}},
				{ "C=", "[combined-directory] searches in directory for both dylib and swiftmodule files; can be used multiple times", p => {
					if (!string.IsNullOrEmpty (p)) {
						DylibPaths.Add (Path.GetFullPath (p));
						ModulePaths.Add (Path.GetFullPath (p));
					}
				}},
				{ "type-database-path=", "searches in directory for type database files; can be used multiple times", p => {
					if (!string.IsNullOrEmpty (p))
						TypeDatabasePaths.Add (Path.GetFullPath (p));
				}},
				{ "o=", "[directory] write all output files to directory", p => {
					if (!string.IsNullOrEmpty (p))
						OutputDirectory = Path.GetFullPath (p);
				}},
				{ "unicode-mapping=", "XML file describing mapping from swift unicode identifiers to C# identifiers", p => {
					UnicodeMappingFile = p;
				}},
				{ "v|verbose", "prints information about work in process.", v => {
					Verbosity++;;
				}},
				{ "version", "print version information.", v => {
					PrintVersion |=v != null;
				}},
				{ "dylibXmlPath=", "path to the xml when using a Dylib.", dylibXmlPath => {
					DylibXmlPath = dylibXmlPath;
				}},
				{ "h|?|help", "prints this message", h => {
					PrintHelp |=h != null;
				}},
			};
		}

		public string SwiftBinPath { get; set; }
		public string SwiftLibPath { get; set; }
		public string DylibXmlPath { get; set; }
		public List<string> TypeDatabasePaths { get; private set; }
		public string SwiftGluePath { get; set; }
		public List<string> ModulePaths { get; private set; }
		public List<string> DylibPaths { get; private set; }
		public List<string> Targets { get; private set; }
		public string ModuleName { get; set; }
		public string WrappingModuleName { get; set; }
		public string OutputDirectory { get; set; }
		public bool RetainSwiftWrappingCode { get; set; }
		public bool RetainXmlReflection { get; set; }
		public bool PrintStackTrace { get; set; }
		public string GlobalClassName { get; set; }
		public bool TargetPlatformIs64Bit { get; set; }
		public string PInvokeClassPrefix { get; set; }
		public string UnicodeMappingFile { get; set; }
		public bool PrintVersion { get; set; }
		public bool Verbose { get { return Verbosity > 0; } }
		public int Verbosity { get; set; }
		public bool PrintHelp { get; set; }
		public bool Demangle { get; set; }

		#region Parsing Command Line

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

		public static string FindSwiftBinPath ()
		{
			return FindPathFromEnvVariable ("bin/swift/bin") ?? FindPathRelativeToExecutable ("../../bin/swift/bin");
		}

		public static string FindSwiftLibPath ()
		{
			return FindPathFromEnvVariable ("bin/swift/lib/swift/") ?? FindPathRelativeToExecutable ("../../bin/swift/lib/swift/");
		}

		string FindTypeDatabasePath ()
		{
			return FindPathFromEnvVariable ("bindings") ?? FindPathRelativeToExecutable ("../../bindings");
		}

		public static string FindSwiftGluePath ()
		{
			return FindPathFromEnvVariable ("lib/SwiftInterop") ?? FindPathRelativeToExecutable ("../SwiftInterop");
		}

		/// <summary>
		/// Prints the usage of the tool using the provider writer.
		/// </summary>
		/// <param name="writer">Writer that will be used to print the usage.</param>
		public void PrintUsage (TextWriter writer)
		{
			var location = Assembly.GetEntryAssembly ()?.Location;
			string exeName = (location != null)? Path.GetFileName (location) : "";
			writer.WriteLine ($"Usage:");
			writer.WriteLine ($"\t{exeName} [options] -o=output-directory -module-name=ModuleName");
			writer.WriteLine ($"\t{exeName} --demangle symbol [symbol...]");
			writer.WriteLine ("Options:");
			optionsSet.WriteOptionDescriptions (writer);
			return;
		}

		/// <summary>
		/// Parses a list of string that represent the command line
		/// parameters used by the user.
		/// </summary>
		/// <returns>The command line.</returns>
		/// <param name="args">A list of arguments not defined by
		/// the tool and that were passed by the user.</param>
		public List<String> ParseCommandLine (string [] args)
		{
			// set the default values for the option
			RetainXmlReflection = false;
			RetainSwiftWrappingCode = false;
			PrintStackTrace = false;
			WrappingModuleName = null;
			GlobalClassName = "TopLevelEntities";
			TargetPlatformIs64Bit = true;
			SwiftGluePath = PosixHelpers.RealPath (FindSwiftGluePath ());

			var extra = optionsSet.Parse (args);

			if (SwiftBinPath == null) {
				SwiftBinPath = FindSwiftBinPath ();
			}
			SwiftBinPath = PosixHelpers.RealPath (SwiftBinPath);

			if (SwiftLibPath == null) {
				SwiftLibPath = FindSwiftLibPath ();
			}
			SwiftLibPath = PosixHelpers.RealPath (SwiftLibPath);

			var tdbPath = PosixHelpers.RealPath (FindTypeDatabasePath ());
			if (tdbPath != null && !TypeDatabasePaths.Contains (tdbPath))
				TypeDatabasePaths.Add (tdbPath);

			return extra;
		}

		#endregion

		static Dictionary<string, string> targetOSToTargetDirectory = new Dictionary<string, string> {
			{ "macos", "mac" }, { "ios", "iphone" }, { "tvos", "appletv" }, {"watchos", "watch" }
		};

		static Dictionary<string, string> targetOSToTargetLibrary = new Dictionary<string, string> {
			{ "macos", "macosx" }, { "ios", "iphoneos" }, { "tvos", "appletvos" }, {"watchos", "watchos" }
		};

		// TJ
		// adding optional parameter to check if we are dealing with a library
		// if so, we can skip swiftmodule specific things
		public void CheckForOptionErrors (ErrorHandling errors, bool isLibrary = false)
		{
			CheckPath (SwiftBinPath, "path to swift binaries", errors);
			CheckPath (SwiftLibPath, "path to swift libraries", errors);
			TypeDatabasePaths.ForEach (path => CheckPath (path, "path to type database files", errors));

			EnsureFileExists ("swiftc", new string [] { SwiftBinPath }, errors);
			EnsureFileExists ("swift", new string [] { SwiftBinPath }, errors);
			ModulePaths.ForEach (path => CheckPath (path, "module path", errors));
			DylibPaths.ForEach (path => CheckPath (path, "library path", errors));
			if (ModuleName != null) {
				string wholeModule = "lib" + ModuleName + ".dylib";
				string libDir = DylibPaths.FirstOrDefault (path => File.Exists (Path.Combine (path, wholeModule)));
				if (libDir == null) {
					wholeModule = ModuleName;
					libDir = DylibPaths.FirstOrDefault (path => File.Exists (Path.Combine (path, wholeModule)));
				}
				if (libDir == null) {
					errors.Add (new ReflectorError (new FileNotFoundException ($"Couldn't find module {ModuleName}")));
					return;
				}
				string libFile = Path.Combine (libDir, wholeModule);
				using (FileStream stm = new FileStream (libFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					try {
						List<string> targets = MachOHelpers.TargetsFromDylib (stm);

						Targets = FilterTargetsIfNeeded (targets, libFile);

						if (Targets.Count > 0) {
							var targetOS = targets [0].ClangTargetOS ();
							var targetOSAlpha = new string (targetOS.Where (Char.IsLetter).ToArray ());

							if (SwiftGluePath != null) {
								string path = null;
								string targetOSPathPart;
								if (!targetOSToTargetDirectory.TryGetValue (targetOSAlpha, out targetOSPathPart)) {
									throw new ArgumentException ("Target not found", nameof (targetOSAlpha));
								}
								// TJ - The SwiftGluePath may have a 'FinalProduct' directory inside the path
								path = Path.Combine (SwiftGluePath, $"{targetOSPathPart}/XamGlue.framework");
								if (!Directory.Exists (path))
									path = Path.Combine (SwiftGluePath, $"{targetOSPathPart}/FinalProduct/XamGlue.framework");

								ModulePaths.Add (path);
								DylibPaths.Add (path);
							}

							string targetOSLibraryPathPart;
							if (!targetOSToTargetLibrary.TryGetValue (targetOSAlpha, out targetOSLibraryPathPart)) {
								throw new ArgumentException ("Target not found", nameof (targetOSAlpha));
							}
							// TJ - The SwiftLibPath may have a 'swift' directory inside the path
							var swiftLibPath = SwiftLibPath;
							SwiftLibPath = Path.Combine (swiftLibPath, targetOSLibraryPathPart);
							if (!Directory.Exists (SwiftLibPath))
								SwiftLibPath = Path.Combine (swiftLibPath, $"swift/{targetOSLibraryPathPart}");

							// filter the targets here
							foreach (string target in Targets) {
								StringBuilder sb = new StringBuilder ();
								foreach (string s in ModulePaths.Interleave (", ")) {
									sb.Append (s);
								}
								// Added by TJ
								// If we are looking at a dylib file, it will not have the swiftmodule file so skip these
								if (isLibrary)
									continue;

								using (ISwiftModuleLocation loc = SwiftModuleFinder.Find (ModulePaths, ModuleName, target)) {
									if (loc == null) {
										errors.Add (new ReflectorError (new FileNotFoundException ($"Unable to find swift module file for {ModuleName} in target {target}. Searched in {sb.ToString ()}.")));
									}
								}

								using (ISwiftModuleLocation loc = SwiftModuleFinder.Find (ModulePaths, "XamGlue", target)) {
									if (loc == null) {
										errors.Add (new ReflectorError (new FileNotFoundException ($"Unable to find swift module file for XamGlue in target {target}. Did you forget to refer to it with -M or -C? Searched in {sb.ToString ()}.")));
									}
								}
							}
						}
					} catch (Exception err) {
						errors.Add (new RuntimeException (ReflectorError.kCantHappenBase + 63, true, err, $"{libFile}: {err.Message}"));
					}
				}
			}
			if (String.IsNullOrEmpty (OutputDirectory)) {
				ArgumentException ex = new ArgumentException ("need to specify an output directory with -o");
				errors.Add (ex);
			} else {
				if (File.Exists (OutputDirectory) && !File.GetAttributes (OutputDirectory).HasFlag (FileAttributes.Directory)) {
					errors.Add (new ReflectorError (new ArgumentException ($"output directory {OutputDirectory} is a file. It needs to be a directory.")));
					return;
				}
				if (!Directory.Exists (OutputDirectory))
					Directory.CreateDirectory (OutputDirectory);
			}
		}

		void CheckPath (string path, string identifier, ErrorHandling errors)
		{
			if (!Directory.Exists (path)) {
				FileNotFoundException fnf = new FileNotFoundException ($"Error in {identifier}, directory '{path}' does not exist.");
				errors.Add (fnf);
				return;
			}
			FileAttributes attributes = File.GetAttributes (path);
			if (!attributes.HasFlag(FileAttributes.Directory)) {
				FileNotFoundException fnf = new FileNotFoundException ($"Error in {identifier}, file '{path}' needs to be a directory.");
				errors.Add (fnf);
			}
		}

		bool EnsureFileExists (string name, IEnumerable<string> possiblePaths, ErrorHandling errors)
		{
			string location = possiblePaths.FirstOrDefault (path => File.Exists (Path.Combine (path, name)));
			if (location == null) {
				FileNotFoundException fnf = new FileNotFoundException (
					$"Unable to find file '{name}' (searched {possiblePaths.Count ()} location(s).)");
				errors.Add (fnf);
				return false;
			}
			return true;
		}

		static List<string> FilterTargetsIfNeeded (List<string> targets, string lib)
		{
			using (FileStream stm = new FileStream (lib, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				List<string> availableTargets = MachOHelpers.TargetsFromDylib (stm);
				if (availableTargets.Count == 0) {
					throw new RuntimeException (ReflectorError.kCantHappenBase + 55, $"library {lib} contains no target architectures.");
				} else {
					if (targets == null || targets.Count == 0) {
						targets = availableTargets;
					} else {
						List<string> sectionalTargets = MachOHelpers.CommonTargets (targets, availableTargets);
						if (sectionalTargets.Count == 0) {
							StringBuilder sbsrc = new StringBuilder ();
							foreach (var s in targets.Interleave (", ")) {
								sbsrc.Append (s);
							}
							StringBuilder sbdst = new StringBuilder ();
							foreach (var s in availableTargets.Interleave (", ")) {
								sbdst.Append (s);
							}
							throw new RuntimeException (ReflectorError.kCantHappenBase + 56, $"No specified target ({sbsrc.ToString ()}) was found in the available targets for {lib} ({sbdst.ToString ()}).");
						} else {
							targets = sectionalTargets;
						}
					}
				}
				return targets;
			}
		}

	}
}

