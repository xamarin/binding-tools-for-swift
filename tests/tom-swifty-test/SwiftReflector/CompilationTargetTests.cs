using System;
using NUnit.Framework;

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
	}
}
