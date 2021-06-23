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
			string workingDirectory = null)
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
		}

		public string OutputDirectory { get; private set; }
		public string ModuleName { get; private set; }
		public List<string> FrameworkPaths { get; private set; }
		public List<string> LibraryPaths { get; private set; }
		public List<string> SwiftFilePaths { get; private set; }
		public UniformTargetRepresentation TargetRepresentation { get; private set; }
		public string MakeFrameworkScript { get; private set; }
		public string WorkingDirectory { get; private set; }


		public string CompileTarget ()
		{
			var scriptPath = GetScriptPath ();
			var args = BuildCommandArgs ();
			var output = ExecAndCollect.Run (scriptPath, args, WorkingDirectory);
			if (TargetRepresentation.Library != null) {
				var frameworkDir = Path.Combine (OutputDirectory, $"{ModuleName}.framework");
				var libraryPath = Path.Combine (OutputDirectory, $"lib{ModuleName}.dylib");
				File.Copy (Path.Combine (frameworkDir, ModuleName), libraryPath);
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

			if (SwiftFilePaths.Count == 0)
				throw new Exception ("No files provided to compile.");
			CheckAllFilesExist ();
			var args = new StringBuilder ();

			// useful for debugging:
			// args.Append (" --verbose --verbose ");

			args.Append ("--output-path ").Append (StringUtils.Quote (OutputDirectory));
			args.Append (" --module-name ").Append (ModuleName);
			if (FrameworkPaths.Count > 0)
				AppendFileList (args.Append (" --frameworks"), FrameworkPaths);
			if (LibraryPaths.Count > 0)
				AppendFileList (args.Append (" --libraries"), LibraryPaths);
			// no need to check for empty, this is mandatory
			AppendFileList (args.Append (" --swift-files"), SwiftFilePaths);

			if (TargetRepresentation.Library != null)
				BuildLibraryArgs (TargetRepresentation.Library, args);
			else if (TargetRepresentation.Framework != null)
				BuildFrameworkArgs (TargetRepresentation.Framework, args);
			else if (TargetRepresentation.XCFramework != null)
				BuildFrameworkArgs (TargetRepresentation.XCFramework, args);
			else throw new Exception ("TargetRepresentation has none of Library/Framework/XCFramework in it.");
			return args.ToString ();
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
			// ../../../../tools/make-framwork
			// from Pack-Man installation:
			// ../make-framework/make-framework

			var assemblyPath = AssemblyLocation ();
			var parent = Directory.GetParent (assemblyPath).ToString ();
			var pacMacPath = Path.Combine (parent, "make-framework/make-framwork");
			if (File.Exists (pacMacPath))
				return pacMacPath;
			var grandParent = Directory.GetParent (parent).ToString ();
			var idePath = Path.Combine (grandParent, "tools/make-framework");
			if (File.Exists (idePath))
				return idePath;
			var greatGrandParent = Directory.GetParent (grandParent).ToString ();
			var greatGreatGrandParent = Directory.GetParent (greatGrandParent).ToString ();
			var unitTestsPath = Path.Combine (greatGreatGrandParent, "tools/make-framework");
			return File.Exists (unitTestsPath) ? unitTestsPath : null;
		}

		string AssemblyLocation ()
		{
			// reference code: https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
			var codeBase = Assembly.GetExecutingAssembly ().CodeBase;
			var uri = new UriBuilder (codeBase);
			var path = Uri.UnescapeDataString (uri.Path);
			return Path.GetDirectoryName (path);
		}
	}
}
