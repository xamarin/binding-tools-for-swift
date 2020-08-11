// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftReflector;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	public class EnumTests {
		[Test]
		public void PropOnTrivialEnum ()
		{
			var swiftCode = @"
public enum Rocks {
    case igneous, sedimentary, metamorphic
    public var Rocks: String { return ""Pile Of Rocks"" }
}
";


			// Console.WriteLine (Rocks.Igneous.GetRocks ());

			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("Rocks.Igneous.Rocks", false));
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "Pile Of Rocks\n");
		}
	}
}
