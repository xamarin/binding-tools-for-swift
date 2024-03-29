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
			Targets = new CompilationTargetCollection ();

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
		public CompilationTargetCollection Targets { get; private set; }
		public string MinimumOSVersion { get; private set; }
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
		public UniformTargetRepresentation InputTargetRepresentation { get; private set; }

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
			{ "macosx", "mac" }, { "macos", "mac" }, { "ios", "iphone" }, { "tvos", "appletv" }, {"watchos", "watch" }
		};

		static Dictionary<string, string> targetOSToTargetLibrary = new Dictionary<string, string> {
			{ "macosx", "macosx" }, { "macos", "macosx" }, { "ios", "iphoneos" }, { "tvos", "appletvos" }, {"watchos", "watchos" }
		};

		public void CheckForOptionErrors (ErrorHandling errors, bool isLibrary = false)
		{
			CheckPath (SwiftBinPath, "path to swift binaries", errors);
			CheckPath (SwiftLibPath, "path to swift libraries", errors);
			TypeDatabasePaths.ForEach (path => CheckPath (path, "path to type database files", errors));
			var swiftTypeDatabase = FindSwiftTypeDatabase (errors);
			if (swiftTypeDatabase != null)
				TypeDatabasePaths.Add (swiftTypeDatabase);

			EnsureFileExists ("swiftc", new string [] { SwiftBinPath }, errors);
			EnsureFileExists ("swift", new string [] { SwiftBinPath }, errors);
			ModulePaths.ForEach (path => CheckPath (path, "module path", errors));
			DylibPaths.ForEach (path => CheckPath (path, "library path", errors));

			if (ModuleName != null) {
				InputTargetRepresentation = UniformTargetRepresentation.FromPath (ModuleName, DylibPaths, errors);
				if (InputTargetRepresentation == null)
					return;
			}

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
						var targets = MachOHelpers.CompilationTargetsFromDylib (stm);

						Targets = FilterTargetsIfNeeded (targets, libFile);

						MinimumOSVersion = Targets.MinimumOSVersion.ToString ();

						if (Targets.Count > 0) {
							var targetOSOnly = Targets.OperatingSystemString;

							var targetXamGluePath = FindXamGluePathForOS (targetOSOnly, errors);
							if (targetXamGluePath == null) {
								throw new ArgumentException ($"Unable to find XamGlue for target operating system {targetOSOnly}");
							}
							ModulePaths.Add (targetXamGluePath);
							DylibPaths.Add (targetXamGluePath);

							string targetOSLibraryPathPart;
							if (!targetOSToTargetLibrary.TryGetValue (targetOSOnly, out targetOSLibraryPathPart)) {
								throw new ArgumentException ("Target not found", nameof (targetOSOnly));
							}
							// The SwiftLibPath may have a 'swift' directory inside the path
							var swiftLibPath = SwiftLibPath;
							SwiftLibPath = Path.Combine (swiftLibPath, targetOSLibraryPathPart);
							if (!Directory.Exists (SwiftLibPath))
								SwiftLibPath = Path.Combine (swiftLibPath, $"swift/{targetOSLibraryPathPart}");

							// filter the targets here
							foreach (var target in Targets) {
								StringBuilder sb = new StringBuilder ();
								foreach (string s in ModulePaths.Interleave (", ")) {
									sb.Append (s);
								}
								// If we are looking at a dylib file, it will not have the swiftmodule file so skip these
								if (isLibrary)
									continue;

								var targetRep = UniformTargetRepresentation.FromPath (ModuleName, ModulePaths, errors);
								if (targetRep == null)
									errors.Add (new ReflectorError (new FileNotFoundException ($"Unable to find swift module file for {ModuleName}. Searched in {sb.ToString ()}.")));
								else if (!targetRep.HasTarget (target))
									errors.Add (new ReflectorError (new FileNotFoundException ($"Unable to find swift module file for {ModuleName} in target {target}. Searched in {sb.ToString ()}.")));
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

		string FindSwiftTypeDatabase (ErrorHandling errors)
		{
			// where to look:
			// we run in two basic configurations: development and release
			// In development, the path to the executable is:
			// /path/to/binding-tools-for-swift/tom-swifty/bin/[Debug,Release]/tom-swifty.exe
			// The path to the typedatabase is
			// /path/to/binding-tools-for-swift/bindings
			// So this directory is
			// ../../../bindings
			// In release, the path to the executable is:
			// /path/to/lib/binding-tools-for-swift/tom-swifty.exe
			// so the path to the library is going to be:
			// ../../bindings
			var tomSwiftyPath = CompilationSettings.AssemblyLocation ();

			var devPathRoot = Parentize (tomSwiftyPath, 3);
			var releasePathRoot = Parentize (tomSwiftyPath, 2);

			var devPath = Path.Combine (devPathRoot, "bindings");
			if (Directory.Exists (devPath))
				return devPath;

			var releasePath = Path.Combine (releasePathRoot, "bindings");
			if (Directory.Exists (releasePath))
				return releasePath;
			errors.Add (new FileNotFoundException ($"Unable to find bindings for system types. Looked in {devPath} and {releasePath}"));
			return null;
		}

		string FindXamGluePathForOS (string targetOS, ErrorHandling errors)
		{
			// where to look:
			// we run in two basic configurations: development and release
			// In development, the path to the executable is:
			// /path/to/binding-tools-for-swift/tom-swifty/bin/[Debug,Release]/tom-swifty.exe
			// the path to the XamGlue is:
			// /path/to/binding-tools-for-swift/swiftglue/bin/[Debug,Release]/<targetOSDirectory>/FinalProduct/XamGlue.[framework,xcframework]
			// so the path to the library is going to be:
			// ../../../swiftglue/bin/[Debug,Relase]/<targetOSDirectory>/FinalProduct/XamGlue.[framework,xcframework]
			// In release:
			// /path/to/lib/binding-tools-for-swift/tom-swifty.exe
			// the path to XamGlue is going to be:
			// /path/to/lib/SwiftInterop/<targetOSDirectory>/XamGlue.[framework,xcframework]
			// so the path to the library is going to be:
			// ../SwiftInterop/<targetOSDirectory>/XamGlue.[framework,xcframework]

			// So to do this, we're going to get the path to the assembly and modify it to be
			// either of the two cases then let UniformTargetRepresentation do the hard work
			var tomSwiftyPath = CompilationSettings.AssemblyLocation ();

			var devPathRoot = Parentize (tomSwiftyPath, 3);
			var releasePathRoot = Parentize (tomSwiftyPath, 1);

			string targetOSDirectory;
			if (!targetOSToTargetDirectory.TryGetValue (targetOS, out targetOSDirectory)) {
				errors.Add (new FileNotFoundException ($"In looking for XamGlue, asked to look for target operating system {targetOS} but I don't know about it"));
			}

			var devDebug = Path.Combine (devPathRoot, "swiftglue/bin/Debug", targetOSDirectory, "FinalProduct");
			var devRelease = Path.Combine (devPathRoot, "swiftglue/bin/Release", targetOSDirectory, "FinalProduct");
			var release = Path.Combine (releasePathRoot, "SwiftInterop", targetOSDirectory);
			var searchPaths = new List<string> () { devDebug, devRelease, release };
			if (SwiftGluePath != null) {
				searchPaths.Insert (0, SwiftGluePath); // give it priority at 0
			}
			var uniformTargetRep = UniformTargetRepresentation.FromPath ("XamGlue", searchPaths, errors);

			return uniformTargetRep?.ParentPath;
		}

		static string Parentize (string path, int numberOfParents)
		{
			while (numberOfParents-- > 0) {
				path = Directory.GetParent (path).ToString ();
			}
			return path;
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

		static CompilationTargetCollection FilterTargetsIfNeeded (List<CompilationTarget> targets, string lib)
		{
			using (FileStream stm = new FileStream (lib, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				var availableTargets = MachOHelpers.CompilationTargetsFromDylib (stm);
				if (availableTargets.Count == 0) {
					throw new RuntimeException (ReflectorError.kCantHappenBase + 55, $"library {lib} contains no target architectures.");
				} else {
					if (targets == null || targets.Count == 0) {
						targets = availableTargets;
					} else {
						var sectionalTargets = MachOHelpers.CommonTargets (targets, availableTargets);
						if (sectionalTargets.Count == 0) {
							StringBuilder sbsrc = new StringBuilder ();
							foreach (var s in targets.Select (t => t.ToString ()).Interleave (", ")) {
								sbsrc.Append (s);
							}
							StringBuilder sbdst = new StringBuilder ();
							foreach (var s in availableTargets.Select (t => t.ToString ()).Interleave (", ")) {
								sbdst.Append (s);
							}
							throw new RuntimeException (ReflectorError.kCantHappenBase + 56, $"No specified target ({sbsrc.ToString ()}) was found in the available targets for {lib} ({sbdst.ToString ()}).");
						} else {
							targets = sectionalTargets;
						}
					}
				}
				var result = new CompilationTargetCollection ();
				result.AddRange (targets);
				return result;
			}
		}
	}
}

