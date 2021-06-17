using System;
using System.Collections.Generic;
using NUnit.Framework;
using SwiftReflector.IOUtils;
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
	}
}
