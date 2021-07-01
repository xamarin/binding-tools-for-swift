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
using SwiftRuntimeLibrary;

namespace SwiftReflector {
	public class WrappingCompiler {
		const string kXamWrapPrefix = "XamarinSwiftWrapper";
		string outputDirectory;
		SwiftCompilerLocation CompilerLocation;
		bool retainSwiftFiles;
		TypeMapper typeMapper;
		bool verbose;
		ErrorHandling errors;
		UniformTargetRepresentation inputTarget;

		Dictionary<string, Dictionary<string, List<string>>> wrappers = new Dictionary<string, Dictionary<string, List<string>>> ();

		public WrappingCompiler (string outputDirectory, SwiftCompilerLocation compilerLocation,
			bool retainSwiftFiles, TypeMapper typeMapper, bool verbose, ErrorHandling errors,
			UniformTargetRepresentation inputTarget)
		{
			this.outputDirectory = Exceptions.ThrowOnNull (outputDirectory, "outputDirectory");
			CompilerLocation = Exceptions.ThrowOnNull (compilerLocation, "compilerLocation");
			this.retainSwiftFiles = retainSwiftFiles;
			this.typeMapper = Exceptions.ThrowOnNull (typeMapper, "typeMapper");
			this.verbose = verbose;
			this.errors = errors;
			this.inputTarget = Exceptions.ThrowOnNull (inputTarget, nameof (inputTarget));
		}

		public Tuple<string, HashSet<string>> CompileWrappers (string [] inputLibraryDirectories, string [] inputModuleDirectories,
			IEnumerable<ModuleDeclaration> modulesToCompile, ModuleInventory modInventory,
			List<string> targets, string wrappingModuleName, bool outputIsFramework,
			string minimumOSVersion = null, bool isLibrary = false)
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
				new TempDirectorySwiftClassFileProvider (Exceptions.ThrowOnNull (wrappingModuleName, "wrappingModuleName"), true)) {
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

				var filesToCompile = fileProvider.CompletedFileNames.Select (file => Path.Combine (fileProvider.DirectoryPath, file)).ToList ();

				var targetCompiler = new CompilationSettings (outputDirectory, wrappingModuleName, inputTarget,
					inputModuleDirectories, inputLibraryDirectories, filesToCompile, referencedModules: inModuleNamesList);

				// useful for debugging a build
				targetCompiler.SuperVerbose = true;

				try {
					targetCompiler.CompileTarget ();
				} catch (Exception e) {
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 66, e, $"Failed to compile the generated swift wrapper code: {e.Message}");
				}

				if (retainSwiftFiles) {
					CopySwiftFiles (fileProvider, Path.Combine (outputDirectory, wrappingModuleName + "Source"));
				}
				return new Tuple<string, HashSet<string>> (outputLibraryPath, allReferencedModules);
			}
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

