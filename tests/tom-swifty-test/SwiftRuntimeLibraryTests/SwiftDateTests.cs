// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SwiftRuntimeLibrary;

using NUnit.Framework;
using Dynamo.CSLang;
using tomwiftytest;
using SwiftReflector;

namespace SwiftRuntimeLibraryTests {
	[TestFixture]
	public class SwiftDateTests {

		[TestCase (PlatformName.macOS)]
		public void BasicTest (PlatformName platform)
		{
			var swiftCode = @"
public func dateIsNotUsed () { }
";

			var clID = new CSIdentifier ("date");
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ("SwiftDate.SwiftDate_TimeIntervalSince1970", false, CSConstant.Val (0.0)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{clID.Name}.ToNSDate", false));
			var disposer = CSFunctionCall.FunctionCallLine ($"{clID.Name}.Dispose");

			var callingCode = CSCodeBlock.Create (clDecl, printer, disposer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "1970-01-01 00:00:00 +0000\n", testName: $"BasicTest{platform}", platform: platform);
		}
	}
}
