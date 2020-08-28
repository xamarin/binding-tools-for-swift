// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector.IOUtils;
using System.Diagnostics;
using System.Text;
using SwiftReflector.ExceptionTools;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;
using Xamarin;
using ObjCRuntime;

namespace SwiftReflector {
	public class WrappingCompiler {
		const string kXamWrapPrefix = "XamarinSwiftWrapper";
		string outputDirectory;
		SwiftCompilerLocation CompilerLocation;
		bool retainSwiftFiles;
		TypeMapper typeMapper;
		bool verbose;
		ErrorHandling errors;

		Dictionary<string, Dictionary<string, List<string>>> wrappers = new Dictionary<string, Dictionary<string, List<string>>> ();

		public WrappingCompiler (string outputDirectory, SwiftCompilerLocation compilerLocation,
		                         bool retainSwiftFiles, TypeMapper typeMapper, bool verbose, ErrorHandling errors)
		{
			this.outputDirectory = Ex.ThrowOnNull (outputDirectory, "outputDirectory");
			CompilerLocation = Ex.ThrowOnNull (compilerLocation, "compilerLocation");
			this.retainSwiftFiles = retainSwiftFiles;
			this.typeMapper = Ex.ThrowOnNull (typeMapper, "typeMapper");
			this.verbose = verbose;
			this.errors = errors;
		}

		public Tuple<string, HashSet<string>> CompileWrappers (string [] inputLibraryDirectories, string [] inputModuleDirectories,
			IEnumerable<ModuleDeclaration> modulesToCompile, ModuleInventory modInventory,
			List<string> targets, string wrappingModuleName, bool outputIsFramework)
		{
			wrappingModuleName = wrappingModuleName ?? kXamWrapPrefix;

			string outputLibraryName = BuildLibraryName (wrappingModuleName, outputIsFramework);

			string outputLibraryPath = Path.Combine (outputDirectory, outputLibraryName);

			string outputFrameworkPath = Path.Combine (outputDirectory, Path.GetFileName (outputLibraryName) + ".framework");
			string outputFrameworkLibPath = Path.Combine (outputFrameworkPath, outputLibraryName);


			if (File.Exists (outputLibraryPath)) {
				File.Delete (outputLibraryPath);
			}

			using (TempDirectorySwiftClassFileProvider fileProvider =
				new TempDirectorySwiftClassFileProvider (Ex.ThrowOnNull (wrappingModuleName, "wrappingModuleName"), true)) {
				var allReferencedModules = new HashSet<string> ();
				foreach (ModuleDeclaration module in modulesToCompile) {
					HashSet<string> referencedModules = null;
					var wrappedClasses = Wrap (module, modInventory, fileProvider, typeMapper, wrappingModuleName, out referencedModules, errors);
					this.wrappers.Add (module.Name, wrappedClasses);
					allReferencedModules.Merge (referencedModules);
				}
				var inModuleNamesList = modulesToCompile.Select (mod => mod.Name).ToList ();
				inModuleNamesList.Add ("XamGlue");


				if (fileProvider.IsEmpty) {
					return new Tuple<string, HashSet<string>> (null, null);
				}

				var targetOutDirs = new List<string> ();
				for (int i = 0; i < targets.Count; i++) {
					// each file goes to a unique output directory.
					// first compile into the fileProvider, then move to
					// fileProvider/tar-get-arch
					string targetoutdir = Path.Combine (fileProvider.DirectoryPath, targets [i]);
					targetOutDirs.Add (targetoutdir);
					Directory.CreateDirectory (targetoutdir);

					var locations = SwiftModuleFinder.GatherAllReferencedModules (allReferencedModules,
												      inputModuleDirectories, targets [i]);
					try {
						string [] inputModDirs = locations.Select (loc => loc.DirectoryPath).ToArray ();
						CompileAllFiles (fileProvider, wrappingModuleName, outputLibraryName, outputLibraryPath,
						                 inputModDirs, inputLibraryDirectories, inModuleNamesList.ToArray (),
						                 targets [i], outputIsFramework);
					} catch (Exception e) {
						throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 66, e, $"Failed to compile the generated swift wrapper code.");
					} finally {
						locations.DisposeAll ();
					}

					// move to arch directory
					File.Copy (Path.Combine (fileProvider.DirectoryPath, outputLibraryName),
							  Path.Combine (targetoutdir, outputLibraryName), true);
					File.Delete (Path.Combine (fileProvider.DirectoryPath, outputLibraryName));

					File.Copy (Path.Combine (fileProvider.DirectoryPath, wrappingModuleName + ".swiftmodule"),
							  Path.Combine (targetoutdir, wrappingModuleName + ".swiftmodule"));
					File.Delete (Path.Combine (fileProvider.DirectoryPath, wrappingModuleName + ".swiftmodule"));

					File.Copy (Path.Combine (fileProvider.DirectoryPath, wrappingModuleName + ".swiftdoc"),
							  Path.Combine (targetoutdir, wrappingModuleName + ".swiftdoc"));
					File.Delete (Path.Combine (fileProvider.DirectoryPath, wrappingModuleName + ".swiftdoc"));
				}
				if (targets.Count > 1) {
					// lipo all the outputs back into the fileProvider
					Lipo (targetOutDirs, fileProvider.DirectoryPath, outputLibraryName);
					File.Copy (Path.Combine (fileProvider.DirectoryPath, outputLibraryName),
							  outputLibraryPath, true);
					if (!IsMacOSLib (outputLibraryPath)) {
						if (!Directory.Exists (outputFrameworkPath))
							Directory.CreateDirectory (outputFrameworkPath);
						File.Copy (outputLibraryPath, outputFrameworkLibPath, true);
						InfoPList.MakeInfoPList (outputFrameworkLibPath, Path.Combine (outputFrameworkPath, "Info.plist"));
					}
				} else {
					File.Copy (Path.Combine (targetOutDirs [0], outputLibraryName),
										  outputLibraryPath, true);
					if (!IsMacOSLib(outputLibraryPath)) {
						if (!Directory.Exists (outputFrameworkPath))
							Directory.CreateDirectory (outputFrameworkPath);
						File.Copy (outputLibraryPath, outputFrameworkLibPath, true);
						InfoPList.MakeInfoPList (outputFrameworkLibPath, Path.Combine (outputFrameworkPath, "Info.plist"));
					}
				}
				for (int i = 0; i < targets.Count; i++) {
					string arch = targets [i].ClangTargetCpu ();
					string targetDir = Path.Combine (outputDirectory, arch);
					Directory.CreateDirectory (targetDir);

					File.Copy (Path.Combine (targetOutDirs [i], wrappingModuleName + ".swiftmodule"),
							  Path.Combine (targetDir, wrappingModuleName + ".swiftmodule"), true);
					File.Copy (Path.Combine (targetOutDirs [i], wrappingModuleName + ".swiftdoc"),
										  Path.Combine (targetDir, wrappingModuleName + ".swiftdoc"), true);
				}
				foreach (string dirname in targetOutDirs) {
					Directory.Delete (dirname, true);
				}
				if (retainSwiftFiles) {
					CopySwiftFiles (fileProvider, Path.Combine (outputDirectory, wrappingModuleName + "Source"));
				}
				return new Tuple<string, HashSet<string>> (outputLibraryPath, allReferencedModules);
			}
		}

		void Lipo (List<string> sourcePaths, string outputPath, string libraryName)
		{
			var sb = new StringBuilder ();
			foreach (string s in sourcePaths) {
				sb.Append ($" {Path.Combine (s, libraryName)}");
			}
			ExecAndCollect.Run ("/usr/bin/lipo", $"-create {sb.ToString ()} -output {Path.Combine (outputPath, libraryName)}", verbose: verbose);
		}


		void CopySwiftFiles (TempDirectorySwiftClassFileProvider provider, string outputDirectory)
		{
			if (!Directory.Exists (outputDirectory))
				Directory.CreateDirectory (outputDirectory);
			foreach (string file in provider.CompletedFileNames) {
				string targetFile = Path.Combine (outputDirectory, file);
				string sourceFile = Path.Combine (provider.DirectoryPath, file);
				File.Copy (sourceFile, targetFile, true);
			}
		}

		public bool TryGetClassesForModule (string module, out Dictionary<string, List<string>> classes)
		{
			return wrappers.TryGetValue (module, out classes);
		}

		void CompileAllFiles (TempDirectorySwiftClassFileProvider fileProvider, string moduleName, string outputLibraryName,
			string outputLibraryPath, string [] inputModulePaths, string [] inputLibraryPaths, string [] inputModuleNames, string target,
				    bool outputIsFramework)
		{
			SwiftTargetCompilerInfo compilerInfo = CompilerLocation.GetTargetInfo (target);
			using (CustomSwiftCompiler compiler = new CustomSwiftCompiler (compilerInfo, fileProvider, false)) {
				compiler.Verbose = verbose;
				string [] sourceFiles = fileProvider.CompletedFileNames.ToArray ();
				SwiftCompilerOptions options = new SwiftCompilerOptions (moduleName, inputModulePaths, inputLibraryPaths, inputModuleNames);
				compiler.Compile (options, outputIsFramework, sourceFiles);
			}
		}

		Dictionary<string, List<string>> Wrap (ModuleDeclaration module,
		                                              ModuleInventory modInventory, TempDirectorySwiftClassFileProvider fileProvider,
		                                              TypeMapper typeMapper, string wrappingModuleName, out HashSet<string> referencedModules,
		                                              ErrorHandling errors)
		{
			var wrapper = new MethodWrapping (fileProvider, typeMapper, wrappingModuleName, errors);
			referencedModules = wrapper.WrapModule (module, modInventory);
			FunctionReferenceCodeMap = wrapper.FunctionReferenceCodeMap;

			Dictionary<string, List<string>> wrappedResults = null;
			if (!wrapper.TryGetClassesForModule (module.Name, out wrappedResults)) {
				return new Dictionary<string, List<string>> ();
			}
			return wrappedResults;
		}

		string BuildLibraryName (string wrappingModuleName, bool outputIsFramework)
		{
			return outputIsFramework ? wrappingModuleName : String.Format ("lib{0}.dylib", wrappingModuleName);
		}
		string BuildSwiftmoduleName (string wrappingModuleName)
		{
			return String.Format ("{0}.swiftmodule", wrappingModuleName);
		}

		static bool IsMacOSLib (string pathToLibrary)
		{
			using (FileStream stm = new FileStream (pathToLibrary, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				foreach (MachOFile file in MachO.Read (stm)) {
					var osmin = file.MinOS;
					if (osmin == null)
						throw new NotSupportedException ("dylib files without a minimum supported operating system load command are not supported.");
					if (osmin.Platform != MachO.Platform.MacOS)
						return false;
				}
				return true;
			}
		}

		public FunctionReferenceCodeMap FunctionReferenceCodeMap { get; private set; }

	}
}

