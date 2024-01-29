using System;
using SwiftReflector;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace SwiftReflector {

	[TestFixture]
	public class ClangTargetTests {
		[TestCase ("armv7-apple-ios10.3", false)]
		[TestCase ("armv7s-apple-ios10.3", false)]
		[TestCase ("arm64-apple-ios10.3", false)]
		[TestCase ("i386-apple-ios10.3-simulator", true)]
		[TestCase ("x86_64-apple-macosx10.9", false)]
		[TestCase ("x86_64-apple-ios10.3-simulator", true)]
		[TestCase ("armv7k-apple-watchos3.2", false)]
		[TestCase ("i386-apple-watchos3.2-simulator", true)]
		public void SimulatorTests (string target, bool expected)
		{
			ClassicAssert.AreEqual (expected, target.ClangTargetIsSimulator (), $"wrong simulator state for {target}");
		}
	}
}
