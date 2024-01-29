﻿using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using Xamarin;
using System.Linq;
using tomwiftytest;
using NUnit.Framework.Legacy;

namespace SwiftReflector {
	[TestFixture]
	public class CompilationTargetTests {
		[Test]
		public void MacOSTest ()
		{
			var target = new CompilationTarget (PlatformName.macOS, TargetCpu.X86_64,
				TargetEnvironment.Device, new Version ("14.3"));
			ClassicAssert.AreEqual ("x86_64-apple-macosx14.3", target.ToString ());
		}

		[Test]
		public void CantHaveNullTarget ()
		{
			ClassicAssert.Throws (typeof (ArgumentNullException), () => new CompilationTarget (PlatformName.iOS, TargetCpu.Arm64,
				TargetEnvironment.Simulator, null));
		}

		[Test]
		public void CantHaveNonePlatform ()
		{
			ClassicAssert.Throws (typeof (ArgumentOutOfRangeException), () => new CompilationTarget (PlatformName.None, TargetCpu.Arm64,
					    TargetEnvironment.Simulator, null));
		}

		[Test]
		public void SimulatorOnWatchOS ()
		{
			var target = new CompilationTarget (PlatformName.watchOS, TargetCpu.I386,
				TargetEnvironment.Simulator, new Version ("3.2"));
			ClassicAssert.AreEqual ("i386-apple-watchos3.2-simulator", target.ToString ());
		}

		[Test]
		public void BasicLibraryTest ()
		{
			var swiftCode = @"public func sumIt(a: Int, b: Int) -> Int {
    return a + b
}";

			using (var provider = new DisposableTempDirectory ()) {
				var moduleName = "BasicLibrary";
				Utils.SystemCompileSwift (swiftCode, provider, moduleName);
				var errors = new ErrorHandling ();

				var target = UniformTargetRepresentation.FromPath (moduleName,
					new List<string> () { provider.DirectoryPath }, errors);

				ClassicAssert.IsNotNull (target, "Didn't get a target");
				ClassicAssert.IsNotNull (target.Library);
				ClassicAssert.AreEqual (TargetEnvironment.Device, target.Library.Environment, "wrong environment");
				ClassicAssert.AreEqual (1, target.Library.Targets.Count, "more targets than we wanted");
				ClassicAssert.AreEqual (TargetCpu.X86_64, target.Library.Targets [0].Cpu, "cpu mismatch");
				ClassicAssert.AreEqual (PlatformName.macOS, target.Library.OperatingSystem, "operating system mismatch");
				ClassicAssert.AreEqual (new Version ("10.9"), target.Library.Targets [0].MinimumOSVersion, "os version mismatch");
				ClassicAssert.AreEqual (TargetManufacturer.Apple, target.Library.Targets [0].Manufacturer, "wrong manufacturer");
			}
		}

		CompilationTargetCollection BuildCompilationTargetCollection (PlatformName platform, TargetEnvironment env, List<TargetCpu> cpus, string minVersion)
		{
			var version = new Version (minVersion);

			var collection = new CompilationTargetCollection ();
			foreach (var cpu in cpus) {
				collection.Add (new CompilationTarget (platform, cpu, env, version));
			}
			return collection.Count > 0 ? collection : null;
		}

		CompilationSettings BuildCompilationSettings (List<string> swiftFiles, PlatformName platform,
			List<TargetCpu> simArchs, List<TargetCpu> deviceArchs, string minVersion, string outputDirectory,
			bool isLibrary)
		{
			var moduleName = "NoNameModule";
			CompilationTargetCollection simTargets = null;
			CompilationTargetCollection devTargets = null;
			UniformTargetRepresentation targetRepresentation = null;
			if (simArchs != null) {
				simTargets = BuildCompilationTargetCollection (platform, TargetEnvironment.Simulator, simArchs, minVersion);
			}
			if (deviceArchs != null) {
				devTargets = BuildCompilationTargetCollection (platform, TargetEnvironment.Device, deviceArchs, minVersion);
			}

			if (isLibrary) {
				var library = new LibraryRepresentation (outputDirectory, moduleName);
				library.Targets.AddRange (simTargets ?? devTargets);
				targetRepresentation = new UniformTargetRepresentation (library);
			} else {
				if (simTargets != null && devTargets != null) {
					var devFm = new FrameworkRepresentation (outputDirectory, moduleName);
					devFm.Targets.AddRange (devTargets);
					var simFm = new FrameworkRepresentation (outputDirectory, moduleName);
					simFm.Targets.AddRange (simTargets);
					var xcFramework = new XCFrameworkRepresentation (outputDirectory, moduleName);
					xcFramework.Frameworks.Add (devFm);
					xcFramework.Frameworks.Add (simFm);
					targetRepresentation = new UniformTargetRepresentation (xcFramework);
				} else {
					var framework = new FrameworkRepresentation ("/path/to/nowhere", moduleName);
					framework.Targets.AddRange (simTargets ?? devTargets);
					targetRepresentation = new UniformTargetRepresentation (framework);
				}
			}
			var compilationSettings = new CompilationSettings (outputDirectory, moduleName, targetRepresentation);
			compilationSettings.SwiftFilePaths.AddRange (swiftFiles);
			return compilationSettings;
		}

		DisposableTempDirectory CompileStringToResult (string swiftCode, PlatformName platform,
			string minVersion, List<TargetCpu> simArchs, List<TargetCpu> deviceArchs, bool isLibrary)
		{
			var outputDirectory = new DisposableTempDirectory ();
			try {
				using (var codeDirectory = new DisposableTempDirectory ()) {
					var sourceFile = Path.Combine (codeDirectory.DirectoryPath, "noname.swift");
					using (var sourceCode = new StreamWriter (sourceFile))
						sourceCode.Write (swiftCode);
					var compilationSettings = BuildCompilationSettings (new List<string> () { sourceFile }, platform, simArchs, deviceArchs,
						minVersion, outputDirectory.DirectoryPath, isLibrary);
					var result = compilationSettings.CompileTarget ();
				}
			} catch {
				outputDirectory.Dispose ();
				throw;
			}
			return outputDirectory;
		}

		[Test]
		public void SimpleLibraryTest ()
		{
			var swiftCode = @"public func sum (a: Int, b: Int) -> Int {
    return a + b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.macOS, "11.0", null,
				new List<TargetCpu> () { TargetCpu.X86_64 }, true)) {

				var outputFile = Path.Combine (output.DirectoryPath, "libNoNameModule.dylib");
				ClassicAssert.IsTrue (File.Exists (outputFile), "we didn't get a file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					ClassicAssert.AreEqual (1, macho.Count, "wrong contents");
					ClassicAssert.AreEqual (MachO.Architectures.x86_64, macho [0].Architecture, "wrong arch");
				}
			}
		}

		[Test]
		public void IphoneDeviceFramework ()
		{
			var swiftCode = @"public func sum (a: Int, b: Int) -> Int {
    return a + b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.iOS, "10.2", null,
				new List<TargetCpu> () { TargetCpu.Arm64 }, false)) {

				var outputFile = Path.Combine (output.DirectoryPath, "NoNameModule.framework", "NoNameModule");
				ClassicAssert.IsTrue (File.Exists (outputFile), "we didn't get a file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					ClassicAssert.AreEqual (1, macho.Count, "wrong contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					ClassicAssert.IsNotNull (file, "no arm64");
				}
			}
		}

		[Test]
		public void IphoneSimulatorFramework ()
		{
			var swiftCode = @"public func sum (a: Int, b: Int) -> Int {
    return a + b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.iOS, "10.2",
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.X86_64 }, null, false)) {

				var outputFile = Path.Combine (output.DirectoryPath, "NoNameModule.framework", "NoNameModule");
				ClassicAssert.IsTrue (File.Exists (outputFile), "we didn't get a file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					ClassicAssert.AreEqual (2, macho.Count, "wrong contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					ClassicAssert.IsNotNull (file, "no arm64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.x86_64);
					ClassicAssert.IsNotNull (file, "no x86_64");
				}
			}
		}


		[Test]
		public void IphoneXCFramework ()
		{
			var swiftCode = @"public func sum (a: Int, b: Int) -> Int {
    return a + b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.iOS, "10.2",
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.X86_64 },
				new List<TargetCpu> () { TargetCpu.Arm64 }, false)) {

				var outputDirectory = Path.Combine (output.DirectoryPath, "NoNameModule.xcframework");
				ClassicAssert.IsTrue (Directory.Exists (outputDirectory), "no xcframework");

				var deviceFM = Path.Combine (outputDirectory, "ios-arm64", "NoNameModule.framework");
				ClassicAssert.IsTrue (Directory.Exists (deviceFM), "no device directory");

				var simFM = Path.Combine (outputDirectory, "ios-arm64_x86_64-simulator", "NoNameModule.framework");
				ClassicAssert.IsTrue (Directory.Exists (simFM), "no simulator directory");

				var outputFile = Path.Combine (deviceFM, "NoNameModule");
				ClassicAssert.IsTrue (File.Exists (outputFile), "we didn't get a device file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					ClassicAssert.AreEqual (1, macho.Count, "wrong device contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					ClassicAssert.IsNotNull (file, "device: no arm64");
				}

				outputFile = Path.Combine (simFM, "NoNameModule");
				ClassicAssert.IsTrue (File.Exists (outputFile), "we didn't get a simulator file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					ClassicAssert.AreEqual (2, macho.Count, "wrong simulator contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					ClassicAssert.IsNotNull (file, "simulator: no arm64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.x86_64);
					ClassicAssert.IsNotNull (file, "simulator: no x86_64");
				}
			}
		}


		[Test]
		public void RoundTripMacLibrary ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.macOS, "11.0",
				null, new List<TargetCpu> () { TargetCpu.X86_64 }, true)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Library, "wasn't a library");
				var lib = rep.Library;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "libNoNameModule.dylib"), lib.Path, "wrong lib path");
				ClassicAssert.AreEqual (1, lib.Targets.Count, "wrong number of targets");
				var target = lib.Targets [0];
				ClassicAssert.AreEqual (TargetCpu.X86_64, target.Cpu, "wrong cpu");
				ClassicAssert.AreEqual (PlatformName.macOS, target.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual ("11.0", target.MinimumOSVersion.ToString (), "wrong version");
				ClassicAssert.AreEqual (TargetEnvironment.Device, target.Environment, "wrong environment");
			}
		}


		[Test]
		public void RoundTripMacFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.macOS, "11.0",
				null, new List<TargetCpu> () { TargetCpu.X86_64 }, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.macOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Device, fwk.Environment, "wrong environment");
				var target = fwk.Targets [0];
				ClassicAssert.AreEqual (TargetCpu.X86_64, target.Cpu, "wrong cpu");
				ClassicAssert.AreEqual ("11.0", target.MinimumOSVersion.ToString (), "wrong version");
			}
		}


		[Test]
		public void RoundTripiOSDeviceFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.iOS, "10.9",
				null, new List<TargetCpu> () { TargetCpu.Arm64 }, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.iOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Device, fwk.Environment, "wrong environment");

				foreach (var target in fwk.Targets) {
					ClassicAssert.AreEqual ("10.9", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
					ClassicAssert.IsTrue (target.Cpu == TargetCpu.Arm64, $"wrong cpu in {target}");
				}
			}
		}


		[Test]
		public void RoundTripiOSSimulatorFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.iOS, "10.9",
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.X86_64 }, null, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (2, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.iOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Simulator, fwk.Environment, "wrong environment");

				foreach (var target in fwk.Targets) {
					ClassicAssert.AreEqual ("10.9", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
					ClassicAssert.IsTrue (target.Cpu == TargetCpu.Arm64 || target.Cpu == TargetCpu.X86_64, $"wrong cpu in {target}");
				}
			}
		}


		[Test]
		public void RoundTripiOSXCFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.iOS, "10.9",
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.X86_64 },
				new List<TargetCpu> () { TargetCpu.Arm64 }, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.XCFramework, "wasn't a xcframework");
				var xcfwk = rep.XCFramework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.xcframework"), xcfwk.Path, "wrong xcfwk path");
				ClassicAssert.AreEqual (2, xcfwk.Frameworks.Count, "wrong number of frameworks");

				var fwk = xcfwk.Frameworks.FirstOrDefault (fw => fw.Environment == TargetEnvironment.Device);
				ClassicAssert.IsNotNull (fwk, "not a device framwwork");

				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.iOS, fwk.OperatingSystem, "wrong os");

				foreach (var target in fwk.Targets) {
					ClassicAssert.AreEqual ("10.9", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
					ClassicAssert.IsTrue (target.Cpu == TargetCpu.Arm64 || target.Cpu == TargetCpu.X86_64, $"wrong cpu in {target}");
				}
			}
		}


		[Test]
		public void RoundTriptvOSDeviceFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.tvOS, "11.0",
				null, new List<TargetCpu> () { TargetCpu.Arm64 }, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.tvOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Device, fwk.Environment, "wrong environment");

				var target = fwk.Targets [0];
				ClassicAssert.AreEqual ("11.0", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
				ClassicAssert.AreEqual (TargetCpu.Arm64, target.Cpu, $"wrong cpu in {target}");
			}

		}


		[Test]
		public void RoundTriptvOSSimulatorFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.tvOS, "11.0",
				new List<TargetCpu> () { TargetCpu.X86_64 }, null, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.tvOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Simulator, fwk.Environment, "wrong environment");

				var target = fwk.Targets [0];
				ClassicAssert.AreEqual ("11.0", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
				ClassicAssert.AreEqual (TargetCpu.X86_64, target.Cpu, $"wrong cpu in {target}");
			}

		}


		[Test]
		[Ignore ("MachO isn't reading this for me.")]
		public void RoundTripwatchOSDeviceFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.watchOS, "7.0",
				null, new List<TargetCpu> () { TargetCpu.Arm64_32 }, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.watchOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Device, fwk.Environment, "wrong environment");

				var target = fwk.Targets [0];
				ClassicAssert.AreEqual ("7.0", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
				ClassicAssert.AreEqual (TargetCpu.Arm64_32, target.Cpu, $"wrong cpu in {target}");
			}

		}


		[Test]
		public void RoundTripwatchOSSimulatorFramework ()
		{
			var swiftCode = @"public func diff (a: Int, b: Int) -> Int {
    return a - b
}";
			using (var output = CompileStringToResult (swiftCode, PlatformName.watchOS, "7.0",
				new List<TargetCpu> () { TargetCpu.X86_64 }, null, false)) {

				var errors = new ErrorHandling ();
				var rep = UniformTargetRepresentation.FromPath ("NoNameModule", new List<string> () { output.DirectoryPath }, errors);
				ClassicAssert.IsNotNull (rep, "no representation");
				ClassicAssert.IsFalse (errors.AnyErrors, "had errors");
				ClassicAssert.IsNotNull (rep.Framework, "wasn't a framework");
				var fwk = rep.Framework;

				ClassicAssert.AreEqual (Path.Combine (output.DirectoryPath, "NoNameModule.framework"), fwk.Path, "wrong fwk path");
				ClassicAssert.AreEqual (1, fwk.Targets.Count, "wrong number of targets");
				ClassicAssert.AreEqual (PlatformName.watchOS, fwk.OperatingSystem, "wrong os");
				ClassicAssert.AreEqual (TargetEnvironment.Simulator, fwk.Environment, "wrong environment");

				var target = fwk.Targets [0];
				ClassicAssert.AreEqual ("7.0", target.MinimumOSVersion.ToString (), $"wrong minimum os in {target}");
				ClassicAssert.AreEqual (TargetCpu.X86_64, target.Cpu, $"wrong cpu in {target}");
			}

		}

		[TestCase ("ios", PlatformName.iOS)]
		[TestCase ("macosx", PlatformName.macOS)]
		[TestCase ("watchos", PlatformName.watchOS)]
		[TestCase ("tvos", PlatformName.tvOS)]
		public void FromStringOSSuccess (string os, PlatformName platform)
		{
			var testString = $"i386-apple-{os}10.1";
			var compilationTarget = new CompilationTarget (testString);
			ClassicAssert.AreEqual (platform, compilationTarget.OperatingSystem, $"wrong os {os}");
			ClassicAssert.AreEqual (new Version ("10.1"), compilationTarget.MinimumOSVersion, $"wrong version");
		}

		[Test]
		public void FromStringOSFail ()
		{
			var testString = $"i386-apple-steveos3.7";
			ClassicAssert.Throws (typeof (ArgumentOutOfRangeException), () => new CompilationTarget (testString));
		}

		[Test]
		public void FromStringManufacturerSuccess ()
		{
			var compilationTarget = new CompilationTarget ("i386-apple-ios10.1");
			ClassicAssert.AreEqual (TargetManufacturer.Apple, compilationTarget.Manufacturer);
		}

		[Test]
		public void FromStringManufacturerFail ()
		{
			ClassicAssert.Throws (typeof (ArgumentOutOfRangeException), () => new CompilationTarget ("i386-banana-ios10.1"));
		}

		[TestCase ("arm64", TargetCpu.Arm64)]
		[TestCase ("x86_64", TargetCpu.X86_64)]
		[TestCase ("arm64_32", TargetCpu.Arm64_32)]
		public void FromStringCpuSuccess (string cpu, TargetCpu targetCpu)
		{
			var testString = $"{cpu}-apple-ios10.1";
			var compilationTarget = new CompilationTarget (testString);
			ClassicAssert.AreEqual (targetCpu, compilationTarget.Cpu);
		}

		[Test]
		public void FromStringCpuFail ()
		{
			var testString = $"blah-apple-ios10.1";
			ClassicAssert.Throws(typeof (ArgumentOutOfRangeException), () => new CompilationTarget (testString));
		}
	}
}
