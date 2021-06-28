using System;
using System.Collections.Generic;
using SwiftRuntimeLibrary;
using System.Linq;
using System.IO;

namespace SwiftReflector {
	public class FrameworkRepresentation {
		CompilationTargetCollection compilationTargets = new CompilationTargetCollection ();

		public FrameworkRepresentation (string pathToFramework)
		{
			Path = Exceptions.ThrowOnNull (pathToFramework, nameof (pathToFramework));
		}

		public CompilationTargetCollection Targets { get => compilationTargets; }
		public PlatformName OperatingSystem { get => compilationTargets.OperatingSystem; }
		public string OperatingSystemString { get => compilationTargets.OperatingSystemString; }
		public TargetEnvironment Environment { get => compilationTargets.Environment; }
		public string Path { get; private set; }
	}

	public class XCFrameworkRepresentation {
		List<FrameworkRepresentation> frameworks = new List<FrameworkRepresentation> ();

		public XCFrameworkRepresentation (string pathToXCFramework)
		{
			Path = Exceptions.ThrowOnNull (pathToXCFramework, nameof (pathToXCFramework));
		}

		public List<FrameworkRepresentation> Frameworks { get => frameworks; }
		public string Path { get; private set; }
	}

	public class LibraryRepresentation : FrameworkRepresentation {
		public LibraryRepresentation (string pathToLibrary)
			: base (pathToLibrary)
		{
		}
	}

	public class UniformTargetRepresentation {
		public UniformTargetRepresentation (FrameworkRepresentation framework)
		{
			Framework = framework;
		}

		public UniformTargetRepresentation (XCFrameworkRepresentation xCFramework)
		{
			XCFramework = xCFramework;
		}

		public UniformTargetRepresentation (LibraryRepresentation library)
		{
			Library = library;
		}

		public XCFrameworkRepresentation XCFramework { get; private set; }
		public FrameworkRepresentation Framework { get; private set; }
		public LibraryRepresentation Library { get; private set; }

		public static UniformTargetRepresentation FromPath (string moduleName, List<string> directoriesToSearch, ErrorHandling errors)
		{
			string path = null;
			try {
				if (TryGetFrameworkPath (moduleName, directoriesToSearch, out path)) {
					return FrameworkFromPath (moduleName, path, errors);
				} else if (TryGetXCFrameworkPath (moduleName, directoriesToSearch, out path)) {
					return XCFrameworkFromPath (moduleName, path, errors);
				} else if (TryGetLibraryPath (moduleName, directoriesToSearch, out path)) {
					return LibraryFromPath (moduleName, path, errors);
				}
				return null;
			} catch (Exception e) {
				errors.Add (e);
				return null;
			}
		}

		static bool TryGetAnyDirectory (string targetFrameworkName, List<string> directories, out string path)
		{
			path = directories.FirstOrDefault (d => d.EndsWith (targetFrameworkName) && Directory.Exists (d));
			if (path != null)
				return true;
			path = directories.FirstOrDefault (d => Directory.Exists (Path.Combine (d, targetFrameworkName)));
			if (path != null) {
				path = Path.Combine (path, targetFrameworkName);
				return true;
			}
			return false;
		}

		static bool TryGetFrameworkPath (string moduleName, List<string> directories, out string path)
		{
			var targetFrameworkName = $"{moduleName}.framework";
			return TryGetAnyDirectory (targetFrameworkName, directories, out path);
		}

		static bool TryGetXCFrameworkPath (string moduleName, List<string> directories, out string path)
		{
			var targetFrameworkName = $"{moduleName}.xcframework";
			return TryGetAnyDirectory (targetFrameworkName, directories, out path);
		}

		static bool TryGetLibraryPath (string moduleName, List<string> directories, out string path)
		{
			var targetLibrary = $"lib{moduleName}.dylib";
			path = directories.FirstOrDefault (d => File.Exists (Path.Combine (d, targetLibrary)));
			if (path != null) path = Path.Combine (path, targetLibrary);
			return path != null;
		}

		static UniformTargetRepresentation LibraryFromPath (string moduleName, string path, ErrorHandling errors)
		{
			var libraryRep = new LibraryRepresentation (path);
			try {
				ReadCompilationTargets (path, libraryRep.Targets);
			} catch (Exception e) {
				errors.Add (e);
				return null;
			}
			return new UniformTargetRepresentation (libraryRep);
		}

		static void ReadCompilationTargets (string libFile, CompilationTargetCollection targets)
		{
			using (FileStream stm = new FileStream (libFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				var fileTargets = MachOHelpers.CompilationTargetsFromDylib (stm);
				FixMinOSIssue (fileTargets);
				targets.AddRange (fileTargets);
			}
		}

		static void FixMinOSIssue (List<CompilationTarget> targets)
		{
			// We know that in some targets that there might be an Arm64 CPU which
			// has an OS MinVersion that doesn't match any of the others. This is something
			// that gets changed by the linker when you compile for Arm64 on some platforms.
			// This code makes sure that any Arm64 platform has a version number that matches
			// all the rest.
			// There is probably a better algorithm for this, but let's be honest, we're looking
			// at no more than 5 or 6 targets, so if we can't do this task in a microsecond with
			// this code, we've got much bigger issues.
			for (int i = 0; i < targets.Count; i++) {
				if (IsArm64 (targets [i])) {
					for (int j = 0; j < targets.Count; j++) {
						if (IsArm64 (targets [j]))
							continue;
						if (targets [j].MinimumOSVersion != targets [i].MinimumOSVersion) {
							targets [i] = targets [i].WithMinimumOSVersion (targets [j].MinimumOSVersion);
							break;
						}
					}
				}
			}

		}

		static bool IsArm64 (CompilationTarget t)
		{
			return t.Cpu == TargetCpu.Arm64 || t.Cpu == TargetCpu.Arm64e || t.Cpu == TargetCpu.Arm64_32;
		}

		static UniformTargetRepresentation FrameworkFromPath (string moduleName, string frameworkPath, ErrorHandling errors)
		{
			var frameworkRep = new FrameworkRepresentation (frameworkPath);
			try {
				var libraryPath = Path.Combine (frameworkPath, moduleName);
				if (!File.Exists (libraryPath)) {
					errors.Add (new FileNotFoundException (libraryPath));
					return null;
				}
				ReadCompilationTargets (libraryPath, frameworkRep.Targets);
			} catch (Exception e) {
				errors.Add (e);
				return null;
			}
			return new UniformTargetRepresentation (frameworkRep);
		}

		static UniformTargetRepresentation XCFrameworkFromPath (string moduleName, string pathToXCFramework, ErrorHandling errors)
		{
			var xcframeworkRep = new XCFrameworkRepresentation (pathToXCFramework);
			foreach (var candidateFrameworkPath in CandidateFrameworkDirectories (pathToXCFramework, moduleName))
			{
				var targetRep = FrameworkFromPath (moduleName, candidateFrameworkPath, errors);
				if (targetRep != null)
					xcframeworkRep.Frameworks.Add (targetRep.Framework);
			}
			if (xcframeworkRep.Frameworks.Count == 0)
				return null;
			return new UniformTargetRepresentation (xcframeworkRep);
		}

		static IEnumerable<string> CandidateFrameworkDirectories (string pathToXCFramework, string moduleName)
		{
			foreach (var path in Directory.GetDirectories (pathToXCFramework))
			{
				var candidate = Path.Combine (path, $"{moduleName}.framework");
				var candidateModule = Path.Combine (candidate, moduleName);
				if (Directory.Exists (candidate) && File.Exists (candidateModule))
					yield return candidate;
			}
		}
	}
}
