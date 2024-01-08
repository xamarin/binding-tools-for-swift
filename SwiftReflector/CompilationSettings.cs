using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector.IOUtils;
using SwiftRuntimeLibrary;
using System.Reflection;
using Xamarin.Utils;

namespace SwiftReflector {
	public class CompilationSettings {
		public CompilationSettings (string outputDirectory, string moduleName, UniformTargetRepresentation targetRepresentation,
			IEnumerable<string> frameworkPaths = null, IEnumerable<string> libraryPaths = null,
			IEnumerable<string> swiftFilePaths = null, string makeFrameworkScript = null,
			string workingDirectory = null, IEnumerable<string> referencedModules = null)
		{
			OutputDirectory = Exceptions.ThrowOnNull (outputDirectory, nameof (outputDirectory));
			ModuleName = Exceptions.ThrowOnNull (moduleName, nameof (moduleName));

			FrameworkPaths = new List<string> ();
			if (frameworkPaths != null)
				FrameworkPaths.AddRange (frameworkPaths);

			LibraryPaths = new List<string> ();
			if (libraryPaths != null)
				LibraryPaths.AddRange (libraryPaths);

			SwiftFilePaths = new List<string> ();
			if (swiftFilePaths != null)
				SwiftFilePaths.AddRange (swiftFilePaths);

			MakeFrameworkScript = makeFrameworkScript;
			WorkingDirectory = workingDirectory;
			TargetRepresentation = Exceptions.ThrowOnNull (targetRepresentation, nameof (targetRepresentation));

			SwiftModuleReferences = new List<string> ();
			if (referencedModules != null)
				SwiftModuleReferences.AddRange (referencedModules);
		}

		public string OutputDirectory { get; private set; }
		public string ModuleName { get; private set; }
		public List<string> FrameworkPaths { get; private set; }
		public List<string> LibraryPaths { get; private set; }
		public List<string> SwiftFilePaths { get; private set; }
		public List<string> SwiftModuleReferences { get; private set; }
		public UniformTargetRepresentation TargetRepresentation { get; private set; }
		public string MakeFrameworkScript { get; private set; }
		public string WorkingDirectory { get; private set; }
		public bool Verbose { get; set; }
		public bool SuperVerbose { get; set; }


		static string [] handySwiftModuleExtensions = new string [] {
			"swiftdoc", "swiftinterface", "swiftmodule", "swiftsourceinfo"
		};

		public string CompileTarget ()
		{
			var scriptPath = GetScriptPath ();
			var args = BuildCommandArgs ();
			WorkingDirectory = WorkingDirectory ?? Path.GetDirectoryName (SwiftFilePaths [0]);
			var output = ExecAndCollect.Run (scriptPath, args, WorkingDirectory);
			if (TargetRepresentation.Library != null) {
				if (TargetRepresentation.Library.Targets.Count > 1)
					throw new NotSupportedException ("fat libraries are not supported");
				var frameworkDir = Path.Combine (OutputDirectory, $"{ModuleName}.framework");
				var libraryPath = Path.Combine (OutputDirectory, $"lib{ModuleName}.dylib");
				File.Copy (Path.Combine (frameworkDir, ModuleName), libraryPath);
				var arch = TargetRepresentation.Library.Targets [0].CpuToString ();

				var swiftModuleSource = Path.Combine (frameworkDir, "Modules", $"{ModuleName}.swiftmodule");
				foreach (var extension in handySwiftModuleExtensions) {
					var handyFile = Path.Combine (swiftModuleSource, $"{arch}.{extension}");
					if (File.Exists (handyFile))
						File.Copy (handyFile, Path.Combine (OutputDirectory, $"{ModuleName}.{extension}"));
				}
				Directories.DeleteContentsAndSelf (frameworkDir);
				ChangeInstallName (libraryPath);
			}
			return output;
		}

		void ChangeInstallName (string libraryPath)
		{
			var args = $"install_name_tool -id @rpath/lib{ModuleName}.dylib {libraryPath}";
			var output = ExecAndCollect.Run ("xcrun", args);
		}

		string BuildCommandArgs ()
		{
			// Here are the args to the script that we need:
			//--frameworks fw1 fw2 ...
			//   adds framework directories to swift compilation (-F) (optional)
			//--libraries lb1 lb2...
			//   adds library directories to swift compilation (-L) (optional)
			//--swift-library-references lib1 lib2 ...
			//   adds references to the libraries (-llib1 -llib2 ...)
			//--swift-framework-references fm1 fm1 ...
			//   adds references to the frameworks (-framework fm1 -framework -fm2
			//--swift-files sf1 sf2 ...
			//   adds swift files to be compiled (required)
			//--target-os os - name, one of ios, tvos, watchos, macosx (required)
			//   sets the target operating system for the build.
			//--minimum-os-version version (required)
			//   sets the minimum operating system version for the compilation.
			//--simulator-archs arch1 arch2...
			//   sets the architectures for a simulator build.
			//--device-archs arch1 arch2...
			//   sets the architectures for a device build.
			//--module-name name (required)
			//   sets the name of the output module.
			//--extra-swift-args arg1 arg2...
			//   sets extra arguments to pass to the swift compiler.
			//--output-path path (required)
			//   sets the directory where there final output will live.
			//--make-xcframework (optional)
			//   if present, puts both device and simulator builds into an xcframework
			//   if present, both--simulator - archs and--device - archs must be present.
			//--install-name-tool args
                        //   if present, runs the install-name-tool command as part of the swift compilation

			if (SwiftFilePaths.Count == 0)
				throw new Exception ("No files provided to compile.");
			CheckAllFilesExist ();
			var args = new StringBuilder ();

			args.Append ("--output-path ").Append (StringUtils.Quote (OutputDirectory));
			args.Append (" --module-name ").Append (ModuleName);
			if (FrameworkPaths.Count > 0) {
				// for framework paths, drop the end directory if it ends with .framework or .xcframework
				var framePaths = FrameworkPaths.Select (p => p.EndsWith (".framework") || p.EndsWith (".xcframework") ? Directory.GetParent (p).ToString () : p);
				AppendFileList (args.Append (" --frameworks"), framePaths);
			}
			if (LibraryPaths.Count > 0)
				AppendFileList (args.Append (" --libraries"), LibraryPaths);

			var combinedDirectories = new List<string> (FrameworkPaths.Count + LibraryPaths.Count);
			combinedDirectories.AddRange (FrameworkPaths);
			combinedDirectories.AddRange (LibraryPaths);

			BuildLibraryAndFrameworkReferences (args, combinedDirectories);

			// no need to check for empty, this is mandatory
			AppendFileList (args.Append (" --swift-files"), SwiftFilePaths.Select (Path.GetFileName));

			if (TargetRepresentation.Library != null)
				BuildLibraryArgs (TargetRepresentation.Library, args);
			else if (TargetRepresentation.Framework != null)
				BuildFrameworkArgs (TargetRepresentation.Framework, args);
			else if (TargetRepresentation.XCFramework != null)
				BuildFrameworkArgs (TargetRepresentation.XCFramework, args);
			else throw new Exception ("TargetRepresentation has none of Library/Framework/XCFramework in it.");
			if (Verbose) {
				args.Append (" --verbose");
			} else if (SuperVerbose) {
				args.Append (" --verbose --verbose");
			}
			if (TargetRepresentation.Library != null) {
				args.Append (" --extra-swift-args -Xlinker -install_name -Xlinker @rpath/lib")
					.Append (ModuleName).Append (".dylib");
			}
			args.Append (" --install-name-tool -change XamGlue @rpath/XamGlue");
			return args.ToString ();
		}

		void BuildLibraryAndFrameworkReferences (StringBuilder args, List<string> directoryPaths)
		{
			foreach (var moduleName in SwiftModuleReferences) {
				string path;
				var kind = UniformTargetRepresentation.TargetRepresentationKindFromPath (moduleName, directoryPaths, out path);
				if (path != null)
					path = Directory.GetParent (path).ToString ();
				switch (kind) {
				case TargetRepresentationKind.Framework:
				case TargetRepresentationKind.XCFramework:
					args.Append (" --swift-framework-references ").Append (moduleName);
					break;
				case TargetRepresentationKind.Library:
					args.Append (" --swift-library-references ").Append (moduleName);
					break;
				default:
					break;
				}
			}
		}

		void BuildLibraryArgs (LibraryRepresentation library, StringBuilder args)
		{
			CommonFrameworkArgs (library, args);
		}

		void BuildFrameworkArgs (FrameworkRepresentation framework, StringBuilder args)
		{
			CommonFrameworkArgs (framework, args);
		}

		void CommonFrameworkArgs (FrameworkRepresentation framework, StringBuilder args, bool includeOS = true)
		{
			if (includeOS) {
				args.Append (" --minimum-os-version ").Append (framework.Targets.MinimumOSVersion);
				args.Append (" --target-os ").Append (framework.OperatingSystemString);
			}
			if (framework.Environment == TargetEnvironment.Device) {
				AppendArchitectures (" --device-archs", args, framework.Targets);
			} else {
				AppendArchitectures (" --simulator-archs", args, framework.Targets);
			}
		}

		void BuildFrameworkArgs (XCFrameworkRepresentation xcFramework, StringBuilder args)
		{
			if (xcFramework.Frameworks.Count != 2) {
				throw new Exception ("Right now, compilation of xcframewords only supports exactly two sub-frameworks: one for device and one for simulator.");
			}
			CommonFrameworkArgs (xcFramework.Frameworks [0], args, true);
			CommonFrameworkArgs (xcFramework.Frameworks [1], args, false);
			args.Append (" --make-xcframework");
		}

		void AppendArchitectures (string flag, StringBuilder sb, CompilationTargetCollection compilationTargets)
		{
			sb.Append (flag);
			foreach (var target in compilationTargets) {
				sb.Append (" ").Append (target.CpuToString ());
			}
		}

		StringBuilder AppendFileList (StringBuilder sb, IEnumerable<string> list)
		{
			foreach (var elem in list) {
				sb.Append (" ").Append (StringUtils.Quote (elem));
			}
			return sb;
		}

		void CheckAllFilesExist ()
		{
			foreach (var file in SwiftFilePaths) {
				if (!File.Exists (file))
					throw new FileNotFoundException ("Couldn't find swift source file", file);
			}
		}

		string GetScriptPath ()
		{
			var path = MakeFrameworkScript ?? Environment.GetEnvironmentVariable ("BTFS_MAKE_FRAMEWORK") ??
				SearchForScript ();
			if (path == null)
				throw new FileNotFoundException ("Unable to find the make-framework script.\n");
			if (!File.Exists (path))
				throw new FileNotFoundException ($"The specified script '{path}' does not exist at that location.");
			return path;
		}

		string SearchForScript ()
		{
			// locations to look relative to the running assembly:
			// from code main code base as run from the IDE:
			// ../../tools/make-framework
			// from unit tests:
			// ../../../../../tools/make-framwork
			// from Pack-Man installation:
			// ../make-framework/make-framework

			var assemblyPath = AssemblyLocation ();
			var parent = Directory.GetParent (assemblyPath).ToString ();
			var pacMacPath = Path.Combine (parent, "make-framework/make-framework");
			if (File.Exists (pacMacPath))
				return pacMacPath;
			var grandParent = Directory.GetParent (parent).ToString ();
			var idePath = Path.Combine (grandParent, "tools/make-framework");
			if (File.Exists (idePath))
				return idePath;
			var greatGrandParent = Directory.GetParent (grandParent).ToString ();
			var backupIDEPath = Path.Combine (greatGrandParent, "tools/make-framework");
			if (File.Exists (backupIDEPath))
				return backupIDEPath;
			var greatGreatGrandParent = Directory.GetParent (greatGrandParent).ToString ();
			var unitTestsPath = Path.Combine (greatGreatGrandParent, "tools/make-framework");
			if (File.Exists (unitTestsPath))
				return unitTestsPath;
			var greatGreatGreatGrandParent = Directory.GetParent (greatGreatGrandParent).ToString ();
			unitTestsPath = Path.Combine (greatGreatGreatGrandParent, "tools/make-framework");
			return File.Exists (unitTestsPath) ? unitTestsPath : null;
		}

		public static string AssemblyLocation ()
		{
			// reference code: https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
			var codeBase = Assembly.GetExecutingAssembly ().CodeBase;
			var uri = new UriBuilder (codeBase);
			var path = Uri.UnescapeDataString (uri.Path);
			return Path.GetDirectoryName (path);
		}
	}
}
