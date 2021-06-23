﻿using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using Xamarin;
using System.Linq;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	public class CompilationTargetTests {
		[Test]
		public void MacOSTest ()
		{
			var target = new CompilationTarget (PlatformName.macOS, TargetCpu.X86_64,
				TargetEnvironment.Device, new Version ("14.3"));
			Assert.AreEqual ("x86_64-apple-macosx14.3", target.ToString ());
		}

		[Test]
		public void CantHaveNullTarget ()
		{
			Assert.Throws (typeof (ArgumentNullException), () => new CompilationTarget (PlatformName.iOS, TargetCpu.Arm64,
				TargetEnvironment.Simulator, null));
		}

		[Test]
		public void CantHaveNonePlatform ()
		{
			Assert.Throws (typeof (ArgumentOutOfRangeException), () => new CompilationTarget (PlatformName.None, TargetCpu.Arm64,
					    TargetEnvironment.Simulator, null));
		}

		[Test]
		public void SimulatorOnWatchOS ()
		{
			var target = new CompilationTarget (PlatformName.watchOS, TargetCpu.I386,
				TargetEnvironment.Simulator, new Version ("3.2"));
			Assert.AreEqual ("i386-apple-watchos3.2-simulator", target.ToString ());
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

				Assert.IsNotNull (target, "Didn't get a target");
				Assert.IsNotNull (target.Library);
				Assert.AreEqual (TargetEnvironment.Device, target.Library.Environment, "wrong environment");
				Assert.AreEqual (1, target.Library.Targets.Count, "more targets than we wanted");
				Assert.AreEqual (TargetCpu.X86_64, target.Library.Targets [0].Cpu, "cpu mismatch");
				Assert.AreEqual (PlatformName.macOS, target.Library.OperatingSystem, "operating system mismatch");
				Assert.AreEqual (new Version ("10.9"), target.Library.Targets [0].MinimumOSVersion, "os version mismatch");
				Assert.AreEqual (TargetManufacturer.Apple, target.Library.Targets [0].Manufacturer, "wrong manufacturer");
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
				var library = new LibraryRepresentation ("");
				library.Targets.AddRange (simTargets ?? devTargets);
				targetRepresentation = new UniformTargetRepresentation (library);
			} else {
				if (simTargets != null && devTargets != null) {
					var devFm = new FrameworkRepresentation ("");
					devFm.Targets.AddRange (devTargets);
					var simFm = new FrameworkRepresentation ("");
					simFm.Targets.AddRange (simTargets);
					var xcFramework = new XCFrameworkRepresentation ("");
					xcFramework.Frameworks.Add (devFm);
					xcFramework.Frameworks.Add (simFm);
					targetRepresentation = new UniformTargetRepresentation (xcFramework);
				} else {
					var framework = new FrameworkRepresentation ("");
					framework.Targets.AddRange (simTargets ?? devTargets);
					targetRepresentation = new UniformTargetRepresentation (framework);
				}
			}
			var compilationSettings = new CompilationSettings (outputDirectory, "NoNameModule", targetRepresentation);
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
				Assert.IsTrue (File.Exists (outputFile), "we didn't get a file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					Assert.AreEqual (1, macho.Count, "wrong contents");
					Assert.AreEqual (MachO.Architectures.x86_64, macho [0].Architecture, "wrong arch");
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
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.Armv7s }, false)) {

				var outputFile = Path.Combine (output.DirectoryPath, "NoNameModule.framework", "NoNameModule");
				Assert.IsTrue (File.Exists (outputFile), "we didn't get a file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					Assert.AreEqual (2, macho.Count, "wrong contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					Assert.IsNotNull (file, "no arm64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARMv7s);
					Assert.IsNotNull (file, "no arm7s");
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
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.X86_64, TargetCpu.I386 }, null, false)) {

				var outputFile = Path.Combine (output.DirectoryPath, "NoNameModule.framework", "NoNameModule");
				Assert.IsTrue (File.Exists (outputFile), "we didn't get a file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					Assert.AreEqual (3, macho.Count, "wrong contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					Assert.IsNotNull (file, "no arm64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.x86_64);
					Assert.IsNotNull (file, "no x86_64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.i386);
					Assert.IsNotNull (file, "no i386");
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
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.X86_64, TargetCpu.I386 },
				new List<TargetCpu> () { TargetCpu.Arm64, TargetCpu.Armv7s }, false)) {

				var outputDirectory = Path.Combine (output.DirectoryPath, "NoNameModule.xcframework");
				Assert.IsTrue (Directory.Exists (outputDirectory), "no xcframework");

				var deviceFM = Path.Combine (outputDirectory, "ios-arm64_armv7s", "NoNameModule.framework");
				Assert.IsTrue (Directory.Exists (deviceFM), "no device directory");

				var simFM = Path.Combine (outputDirectory, "ios-arm64_i386_x86_64-simulator", "NoNameModule.framework");
				Assert.IsTrue (Directory.Exists (simFM), "no simulator directory");

				var outputFile = Path.Combine (deviceFM, "NoNameModule");
				Assert.IsTrue (File.Exists (outputFile), "we didn't get a device file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					Assert.AreEqual (2, macho.Count, "wrong device contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					Assert.IsNotNull (file, "device: no arm64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARMv7s);
					Assert.IsNotNull (file, "device: no armv7s");
				}

				outputFile = Path.Combine (simFM, "NoNameModule");
				Assert.IsTrue (File.Exists (outputFile), "we didn't get a simulator file!");

				using (var macho = MachO.Read (outputFile, ReadingMode.Immediate)) {
					Assert.AreEqual (3, macho.Count, "wrong simulator contents");
					var file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.ARM64);
					Assert.IsNotNull (file, "simulator: no arm64");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.i386);
					Assert.IsNotNull (file, "simulator: no i386");
					file = macho.FirstOrDefault (f => f.Architecture == MachO.Architectures.x86_64);
					Assert.IsNotNull (file, "simulator: no x86_64");
				}
			}
		}
	}
}
