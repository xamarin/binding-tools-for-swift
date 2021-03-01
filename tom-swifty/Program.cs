// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using SwiftReflector;
using SwiftReflector.Demangling;
using SwiftReflector.ExceptionTools;
using System.Text;
using SwiftReflector.IOUtils;
using SwiftReflector.TypeMapping;
using System.Reflection;
using System.Linq;

using Mono.Options;

namespace tomswifty {
	class MainClass {
		public static int Main (string [] args)
		{
			ErrorHandling errors = new ErrorHandling ();
			// create the default options before we parse the cmd
			SwiftyOptions options = new SwiftyOptions ();

			var extra = options.ParseCommandLine (args);

			// TJ hardcoding options to debug
			// library at: ../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/appletvos//libswiftCore.dylib
			options.SwiftBinPath = Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin";
			options.SwiftLibPath = Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib";
			//options.SwiftLibPath = Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/iphoneos";
			options.ModuleName = "swiftCore";
			options.OutputDirectory = Directory.GetCurrentDirectory () + "/../../Modules";
			options.RetainXmlReflection = true;
			options.RetainSwiftWrappingCode = true;
			options.ModulePaths.Clear ();
			options.DylibPaths.Clear ();
			options.DylibPaths.Add (Directory.GetCurrentDirectory () + "/../../../SwiftToolchain-v1-28e007252c2ec7217c7d75bd6f111b9c682153e3/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/iphoneos//");
			options.SwiftGluePath = Directory.GetCurrentDirectory () + "/../../../swiftglue/bin/Debug";
			options.TypeDatabasePaths.Add (Directory.GetCurrentDirectory () + "/../../../bindings");



			// deal with those options that do not care about the extra params, 
			// then check if we have some and print.
			if (options.Demangle)
			{
				Demangle (args);
				return 0;
			}

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return 0;
			}

			if (options.PrintVersion) {
				PrintVersion ();
				return 0;
			}

			if (extra.Count > 0) {
				// Warn about extra params that are ignored.
				Console.WriteLine ($"WARNING: The following extra parameters will be ignored: '{ String.Join (",", extra) }'");
			}

			// in the following checks, the options will make sure that, if not provided, we find the path.
			// if we are missing the path, we have an issue.
			if (string.IsNullOrEmpty (options.SwiftBinPath)) {
				Console.WriteLine ("Unable to find the custom swift compiler. Try using --swift-bin-path.");
				return 1;
			}

			if (string.IsNullOrEmpty (options.SwiftLibPath)) {
				Console.WriteLine ("Unable to find the custom swift compiler libraries. Try using --swift-lib-path.");
				return 1;
			}

			if (errors.AnyErrors)
				return HandleErrors (options, errors);

			options.CheckForOptionErrors (errors, true);
			if (errors.AnyErrors)
				return HandleErrors (options, errors);

			try {
				if (options.ModuleName == null) {
					Console.WriteLine ("-module-name option is required.");
					return 1;
				}
			} catch (Exception e) {

			}
			

			var unicodeMapper = new UnicodeMapper ();
			if (options.UnicodeMappingFile != null) {
				if (!File.Exists (options.UnicodeMappingFile)) {
					Console.WriteLine ($"Unable to find the unicode mapping file {options.UnicodeMappingFile}.");
					return 1;
				}
				unicodeMapper.AddMappingsFromFile (options.UnicodeMappingFile);
			}

			Compile (options, unicodeMapper, errors);
			if (errors.AnyMessages) {
				Console.WriteLine ("{0}{1} warnings and {2} errors{0}", Environment.NewLine, errors.WarningCount, errors.ErrorCount);
				return HandleErrors (options, errors);
			}
			return 0;
		}

		static void Compile (SwiftyOptions options, UnicodeMapper unicodeMapper, ErrorHandling errors)
		{
			try {
				using (DisposableTempDirectory temp = new DisposableTempDirectory (null, true)) {
					SwiftCompilerLocation compilerLocation = new SwiftCompilerLocation (options.SwiftBinPath, options.SwiftLibPath);
					ClassCompilerOptions compilerOptions = new ClassCompilerOptions (options.TargetPlatformIs64Bit, options.Verbose, options.RetainXmlReflection, options.RetainSwiftWrappingCode);
					NewClassCompiler classCompiler = new NewClassCompiler (compilerLocation, compilerOptions, unicodeMapper);

					ClassCompilerNames compilerNames = new ClassCompilerNames (options.ModuleName, options.WrappingModuleName);
					ClassCompilerLocations classCompilerLocations = new ClassCompilerLocations (options.ModulePaths, options.DylibPaths, options.TypeDatabasePaths);
					var compileErrors = classCompiler.CompileToCSharp (classCompilerLocations, compilerNames, options.Targets, options.OutputDirectory, true);
					errors.Add (compileErrors);
				}
			} catch (Exception err) {
				errors.Add (err);
			}
		}

		static int HandleErrors (SwiftyOptions options, ErrorHandling errors)
		{
			return errors.Show (options.Verbosity + (options.PrintStackTrace ? 4 : 0)); 
		}

		static void PrintVersion ()
		{
			AssemblyName name = Assembly.GetExecutingAssembly ().GetName ();
			Console.WriteLine ($"{name.Name} {Constants.Version} ({Constants.Branch}: {Constants.Hash})");
		}

		static void Demangle(string[] names)
		{
			for (int i = 1; i < names.Length; i++) {
				if (!names [i].StartsWith (Decomposer.kSwift4ID, StringComparison.Ordinal)) {
					Console.WriteLine ($"Symbol '{names[i]}' is not a Swift 4 (or later) symbol.");
					continue;
				}				       
				Swift4Demangler demangler = new Swift4Demangler (names [i], 0);
				var demangling = demangler.ExplodedView ();
				if (demangling == null) {
					Console.WriteLine ($"Unable to demangle symbol '{names [i]}'");
				}
				Console.WriteLine ($"{names [i]}:\n{demangling}");
			}
		}
	}
}
