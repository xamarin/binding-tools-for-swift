using System;
using System.Collections.Generic;
using SwiftRuntimeLibrary;
using System.Linq;
using System.IO;

namespace SwiftReflector {
	public interface ICompiledCodeRepresentation {
		string Path { get; }
		string ParentPath { get; }
		CompilationTargetCollection Targets { get; }
		string PathToDylib (CompilationTarget target);
		string PathToSwiftModule (CompilationTarget target);
		string PathToSwiftInterface (CompilationTarget target);
	}

	public static class CompiledCodeRepresentationExtensions {
		public static string PathToDylib (this ICompiledCodeRepresentation rep, string target)
		{
			var compTarget = new CompilationTarget (target);
			return rep.PathToDylib (compTarget);
		}

		public static string PathToSwiftModule (this ICompiledCodeRepresentation rep, string target)
		{
			var compTarget = new CompilationTarget (target);
			return rep.PathToSwiftModule (compTarget);
		}
		public static string PathToSwiftInterface (this ICompiledCodeRepresentation rep, string target)
		{
			var compTarget = new CompilationTarget (target);
			return rep.PathToSwiftInterface (compTarget);
		}
	}


	public class FrameworkRepresentation : ICompiledCodeRepresentation {
		CompilationTargetCollection compilationTargets = new CompilationTargetCollection ();
		protected string pathToDylib, pathToSwiftModules, pathToSwiftInterface;
		string moduleName;

		public FrameworkRepresentation (string pathToFramework, string moduleName)
		{
			Path = Exceptions.ThrowOnNull (pathToFramework, nameof (pathToFramework));
			ParentPath = Directory.GetParent (Path).ToString ();
			pathToDylib = System.IO.Path.Combine (Path, moduleName);
			this.moduleName = Exceptions.ThrowOnNull (moduleName, nameof (moduleName));
			pathToSwiftModules = System.IO.Path.Combine (Path, "Modules", $"{moduleName}.swiftmodule");
			pathToSwiftInterface = System.IO.Path.Combine (Path, "Modules", $"{moduleName}.swiftmodule");
		}

		public string Path { get; private set; }
		public string ParentPath { get; private set; }
		public CompilationTargetCollection Targets { get => compilationTargets; }

		public PlatformName OperatingSystem { get => compilationTargets.OperatingSystem; }
		public string OperatingSystemString { get => compilationTargets.OperatingSystemString; }
		public TargetEnvironment Environment { get => compilationTargets.Environment; }

		public virtual string PathToDylib (CompilationTarget target)
		{
			return pathToDylib;
		}

		public virtual string PathToSwiftModule (CompilationTarget target)
		{
			var filePath = System.IO.Path.Combine (pathToSwiftModules, $"{target.CpuToString ()}.swiftmodule");
			return filePath;
		}

		public virtual string PathToSwiftInterface (CompilationTarget target)
		{
			var filePath = System.IO.Path.Combine (pathToSwiftInterface, $"{target.CpuToString ()}.swiftinterface");
			return filePath;
		}
	}

	public class XCFrameworkRepresentation : ICompiledCodeRepresentation {
		List<FrameworkRepresentation> frameworks = new List<FrameworkRepresentation> ();

		public XCFrameworkRepresentation (string pathToXCFramework, string moduleName)
		{
			Path = Exceptions.ThrowOnNull (pathToXCFramework, nameof (pathToXCFramework));
			ParentPath = Directory.GetParent (Path).ToString ();
		}

		public List<FrameworkRepresentation> Frameworks { get => frameworks; }

		public string Path { get; private set; }
		public string ParentPath { get; private set; }
		public CompilationTargetCollection Targets {
			get {
				// we could cache this, but the interfacing for the cache is
				// uncomfortable since Frameworks could change at any time.
				var targets = new CompilationTargetCollection ();
				foreach (var framework in Frameworks) {
					foreach (var target in framework.Targets)
						targets.Add (target);
				}
				return targets;
			}
		}

		public string PathToDylib (CompilationTarget target)
		{
			var framework = Frameworks.First (fm => fm.Targets.First (target.Equals) != null);
			return framework.PathToDylib (target);
		}

		public string PathToSwiftModule (CompilationTarget target)
		{
			var framework = Frameworks.First (fm => fm.Targets.First (target.Equals) != null);
			return framework.PathToSwiftModule (target);
		}

		public string PathToSwiftInterface (CompilationTarget target)
		{
			var fm = FrameworkForTarget (target);
			return fm?.PathToSwiftInterface (target);
		}

		FrameworkRepresentation FrameworkForTarget (CompilationTarget target)
		{
			foreach (var framework in Frameworks) {
				var match = framework.Targets.Any (target.Equals);
				if (match)
					return framework;
			}
			return null;
		}

	}

	public class LibraryRepresentation : FrameworkRepresentation {
		public LibraryRepresentation (string pathToLibrary, string moduleName)
			: base (pathToLibrary, moduleName)
		{
			pathToDylib = pathToLibrary;
			var parent = Directory.GetParent (pathToDylib).ToString ();
			pathToSwiftModules = System.IO.Path.Combine (parent, $"{moduleName}.swiftmodule");
			pathToSwiftInterface = System.IO.Path.Combine (parent, $"{moduleName}.swiftinterface");
		}

		public override string PathToSwiftModule (CompilationTarget target)
		{
			return pathToSwiftModules;
		}

		public override string PathToSwiftInterface (CompilationTarget target)
		{
			return pathToSwiftInterface;
		}
	}

	public class UniformTargetRepresentation : ICompiledCodeRepresentation {
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

		ICompiledCodeRepresentation CompiledCodeRepresentationAdapter {
			get {
				return (ICompiledCodeRepresentation)Library ??
					(ICompiledCodeRepresentation)Framework ??
					XCFramework;
			}
		}

		public string Path {
			get { return CompiledCodeRepresentationAdapter?.Path; }
		}
		public string ParentPath {
			get { return CompiledCodeRepresentationAdapter?.ParentPath; }
		}
		public CompilationTargetCollection Targets {
			get {
				return CompiledCodeRepresentationAdapter?.Targets;
			}
		}

		public XCFrameworkRepresentation XCFramework { get; private set; }
		public FrameworkRepresentation Framework { get; private set; }
		public LibraryRepresentation Library { get; private set; }

		public string PathToDylib (CompilationTarget target)
		{
			return CompiledCodeRepresentationAdapter?.PathToDylib (target);
		}

		public string PathToSwiftModule (CompilationTarget target)
		{
			return CompiledCodeRepresentationAdapter?.PathToSwiftModule (target);
		}

		public string PathToSwiftInterface (CompilationTarget target)
		{
			return CompiledCodeRepresentationAdapter?.PathToSwiftInterface (target);
		}

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

		public static TargetRepresentationKind TargetRepresentationKindFromPath (string moduleName, List<string> directoriesToSearch, out string path)
		{
			path = null;
			try {
				if (TryGetFrameworkPath (moduleName, directoriesToSearch, out path)) {
					return TargetRepresentationKind.Framework;
				} else if (TryGetXCFrameworkPath (moduleName, directoriesToSearch, out path)) {
					return TargetRepresentationKind.XCFramework;
				} else if (TryGetLibraryPath (moduleName, directoriesToSearch, out path)) {
					return TargetRepresentationKind.Library;
				}
				return TargetRepresentationKind.None;
			} catch {
				return TargetRepresentationKind.None;
			}
		}

		static bool TryGetAnyDirectory (string targetFrameworkName, List<string> directories, out string path)
		{
			path = directories.FirstOrDefault (d => d.EndsWith (targetFrameworkName) && Directory.Exists (d));
			if (path != null)
				return true;
			path = directories.FirstOrDefault (d => Directory.Exists (System.IO.Path.Combine (d, targetFrameworkName)));
			if (path != null) {
				path = System.IO.Path.Combine (path, targetFrameworkName);
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
			path = directories.FirstOrDefault (d => File.Exists (System.IO.Path.Combine (d, targetLibrary)));
			if (path != null) path = System.IO.Path.Combine (path, targetLibrary);
			return path != null;
		}

		static UniformTargetRepresentation LibraryFromPath (string moduleName, string path, ErrorHandling errors)
		{
			var libraryRep = new LibraryRepresentation (path, moduleName);
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
			var frameworkRep = new FrameworkRepresentation (frameworkPath, moduleName);
			try {
				var libraryPath = System.IO.Path.Combine (frameworkPath, moduleName);
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
			var xcframeworkRep = new XCFrameworkRepresentation (pathToXCFramework, moduleName);
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
				var candidate = System.IO.Path.Combine (path, $"{moduleName}.framework");
				var candidateModule = System.IO.Path.Combine (candidate, moduleName);
				if (Directory.Exists (candidate) && File.Exists (candidateModule))
					yield return candidate;
			}
		}
	}
}
