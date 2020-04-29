// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftReflector;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class DynamicSelfTests {
		[Ignore ("Still smoking")]
		[Test]
		public void SmokeTestSimplest ()
		{
			var swiftCode = @"
public protocol Identity0 {
	func whoAmI () -> Self
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Got here."));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here.\n", platform: PlatformName.macOS);

		}
	}
}
