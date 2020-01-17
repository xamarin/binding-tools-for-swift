// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SwiftReflector.Exceptions;
using System.Linq;
using ObjCRuntime;

namespace SwiftReflector.IOUtils {
	public interface ISwiftModuleLocation : IDisposable {
		string DirectoryPath { get; }
		string ModuleName { get; }
	}

	public partial class SwiftModuleFinder {
		class SimplePathModule : ISwiftModuleLocation {
			public SimplePathModule (string dirPath, string moduleName)
			{
				if (dirPath == null)
					throw new ArgumentNullException (nameof (dirPath));
				if (moduleName == null)
					throw new ArgumentNullException (nameof (moduleName));
				DirectoryPath = dirPath;
				ModuleName = moduleName;
			}
			public string DirectoryPath {
				get; private set;
			}

			public string ModuleName {
				get; private set;
			}

			public void Dispose ()
			{
				GC.SuppressFinalize (this);
			}

			~SimplePathModule () { }
		}

		class AppleFrameworkModule : ISwiftModuleLocation {
			bool disposed = false;
			DisposableTempDirectory tempDir;

			public AppleFrameworkModule (string dir, string moduleName, string target)
			{
				if (dir == null)
					throw new ArgumentNullException (nameof (dir));
				if (moduleName == null)
					throw new ArgumentNullException (nameof (moduleName));
				if (target == null)
					throw new ArgumentNullException (nameof (target));
				ModuleName = moduleName;
				string cpu = target.ClangTargetCpu ();
				string pathSourceDirectory = Path.Combine (Path.Combine (dir, "Modules"), moduleName);

				// hack -
				// apple puts armv?.* files into arm.*
				// so if the file doesn't exist and the name starts with armv, then we
				// check for an arm shorthand.
				if (!File.Exists (Path.Combine (pathSourceDirectory, cpu + ".swiftmodule")) && cpu.StartsWith ("armv") &&
				    File.Exists (Path.Combine (pathSourceDirectory, "arm.swiftmodule"))) {
					cpu = "arm";
				}
				string moduleSourceFile = Path.Combine (pathSourceDirectory, cpu + ".swiftmodule");
				string docSourceFile = Path.Combine (pathSourceDirectory, cpu + ".swiftdoc");
				if (!File.Exists (moduleSourceFile))
					throw new FileNotFoundException ($"Unable to find Swift module for target {target}.", moduleSourceFile);
				if (!File.Exists (docSourceFile))
					throw new FileNotFoundException ($"Unable to find Swift module for target {target}.", docSourceFile);
				tempDir = new DisposableTempDirectory (null, false);
				if (moduleName.EndsWith (".swiftmodule"))
					moduleName = moduleName.Substring (0, moduleName.Length - ".swiftmodule".Length);
				string moduleDestFile = Path.Combine (tempDir.DirectoryPath, moduleName + ".swiftmodule");
				string docDestFile = Path.Combine (tempDir.DirectoryPath, moduleName + ".swiftdoc");
				File.Copy (moduleSourceFile, moduleDestFile);
				File.Copy (docSourceFile, docDestFile);
			}

			public string DirectoryPath {
				get {
					return tempDir.DirectoryPath;
				}
			}

			public string ModuleName {
				get; private set;
			}

			~AppleFrameworkModule ()
			{
				Dispose (false);
			}
			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			protected void Dispose (bool disposing)
			{
				if (!disposed) {
					if (disposing) {
						DisposeManagedResources ();
					}
					DisposeUnmanagedResources ();
					disposed = true;
				}
			}

			protected void DisposeManagedResources ()
			{
				tempDir.Dispose ();
			}

			protected void DisposeUnmanagedResources ()
			{
			}
		}

		static bool IsSystemModule (string target, string module)
		{
			var os = target.ClangTargetOS ();
			string[] modules = null;
			if (os.StartsWith ("macos", StringComparison.InvariantCulture))
				modules = systemModuleNamesMac;
			else if (os.StartsWith ("ios", StringComparison.InvariantCulture))
				modules = systemModuleNamesIPhone;
			else if (os.StartsWith ("tvos"))
				modules = systemModuleNamesTV;
			else if (os.StartsWith ("watchos"))
				modules = systemModuleNamesWatch;

			return modules.Contains (module);
		}

		public static List<ISwiftModuleLocation> GatherAllReferencedModules (IEnumerable<string> allReferencedModules,
										    IEnumerable<string> inputModuleDirectories,
										    string target)
		{
			var locations = new List<ISwiftModuleLocation> ();
			var locationErrors = new List<string> ();

			try {
				foreach (string moduleName in allReferencedModules) {
					if (moduleName == "Swift" || moduleName == "Self")
						continue;
					ISwiftModuleLocation loc = SwiftModuleFinder.Find (inputModuleDirectories, moduleName, target);
					if (loc == null) {
						if (IsSystemModule (target, moduleName))
							continue;
						locationErrors.Add (moduleName);
						continue;
					}
					locations.Add (loc);
				}
			} catch {
				locations.DisposeAll ();
				throw;
			}
			if (locationErrors.Count > 0) {
				if (locationErrors.Count == 1)
					throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 6, $"Unable to find swiftmodule file for {locationErrors [0]} for target {target}.");
				throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 7, $"Unable to find swiftmodules for {locationErrors.InterleaveCommas ()} for target {target}.");
			}
			return locations;
		}

		public static ISwiftModuleLocation Find (IEnumerable<string> searchPaths, string moduleName, string target)
		{
			foreach (string path in searchPaths) {
				try {
					ISwiftModuleLocation location = FindOrDefault (path, moduleName, target);
					if (location != null)
						return location;
				} catch {
					continue;
				}
			}
			return null;
		}

		public static ISwiftModuleLocation FindOrDefault (string targetDir, string moduleName, string target)
		{
			if (moduleName.EndsWith (".swiftmodule", StringComparison.Ordinal))
				throw new ArgumentOutOfRangeException (nameof (moduleName), $"{nameof (moduleName)} can't end in .swiftmodule.");

			string moduleFileName = moduleName + ".swiftmodule";

			if (IsAppleFramework (targetDir, moduleFileName)) {
				return new AppleFrameworkModule (targetDir, moduleFileName, target);
			}

			if (IsXamarinLayout (targetDir, moduleFileName, target)) {
				return new SimplePathModule (Path.Combine (targetDir, target.ClangTargetCpu ()), moduleFileName);
			}

			if (IsDirectLayout (targetDir, moduleFileName)) {
				return new SimplePathModule (targetDir, moduleFileName);
			}

			return null;
		}

		public static IEnumerable<string> FindModuleNames (IEnumerable<string> paths, string target)
		{
			foreach (var path in paths)
				foreach (var item in FindModuleNames (path, target))
					yield return item;
		}

		public static IEnumerable<string> FindModuleNames (string path, string target)
		{
			foreach (string dir in Directory.EnumerateDirectories (path))
			{
				string appleName = GetAppleModuleName (dir);
				if (appleName != null)
					yield return appleName;
				string xamarinModuleName = GetXamarinModuleName (path, target);
				if (xamarinModuleName != null)
					yield return xamarinModuleName;
			}
			foreach (string file in Directory.EnumerateFiles (path))
			{
				string directName = GetDirectLayoutModuleName (file);
				if (directName != null)
					yield return directName;
			}
		}

		public static string GetAppleModuleName (string dir)
		{
			if (dir.EndsWith (".framework", StringComparison.Ordinal) && Directory.Exists (Path.Combine (dir, "Modules"))) {
				string name = (new DirectoryInfo (dir)).Name;
				name = name.Substring (0, name.Length - ".framework".Length);
				if (name.Length > 0 && File.Exists (Path.Combine (dir, "Modules", name + ".swiftmodule")))
					return name;
			}

			return null;
		}

		public static string GetXamarinModuleName (string dir, string target)
		{
			string name = (new DirectoryInfo (dir)).Name;
			if (File.Exists (Path.Combine (dir, target, name + ".swiftmodule")))
				return name;
			return null;
		}

		public static string GetDirectLayoutModuleName (string file)
		{
			if (File.Exists (file) && file.EndsWith (".swiftmodule", StringComparison.Ordinal)) {
				string name = (new DirectoryInfo (file)).Name;
				return name.Substring (0, name.Length - ".swiftmodule".Length);
			}
			return null;
		}

		public static bool IsAppleFramework (string dir, string modfileName)
		{
			string modulesDir = Path.Combine (dir, "Modules");
			string swiftModuleDir = Path.Combine (modulesDir, modfileName);
			return Directory.Exists (modulesDir) && Directory.Exists (swiftModuleDir);
		}

		static bool IsXamarinLayout (string dir, string moduleFileName, string target)
		{
			string moduleDir = Path.Combine (dir, target.ClangTargetCpu ());
			string modulePath = Path.Combine (moduleDir, moduleFileName);
			return Directory.Exists (moduleDir) && File.Exists (modulePath);
		}

		static bool IsDirectLayout (string dir, string moduleFileName)
		{
			string modulePath = Path.Combine (dir, moduleFileName);
			return File.Exists (modulePath);
		}
	}
}
