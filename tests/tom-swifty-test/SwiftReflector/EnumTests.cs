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
		[Ignore ("apple bug - https://bugs.swift.org/browse/SR-13798")]
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

		[Test]
		[Ignore ("apple bug - https://bugs.swift.org/browse/SR-13798")]
		public void TrivialEnumCtor ()
		{
			var swiftCode = @"
public enum TheForce {
    case `do`, doNot
    public init (yoda: Bool) {
        self = yoda ? .do : .doNot // there is no try
    }
}
";

			// var force = TheForceExtensions.Init (true);
			// Console.WriteLine (force);

			var forceID = new CSIdentifier ("force");
			var forceDecl = CSVariableDeclaration.VarLine (forceID, new CSFunctionCall ("TheForceExtensions.Init", false, CSConstant.Val (true)));
			var printer = CSFunctionCall.ConsoleWriteLine (forceID);

			var callingCode = CSCodeBlock.Create (forceDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Do\n");
		}

		[Test]
		public void TrivialOptionalEnumCtor ()
		{
			var swiftCode = @"
public enum SomeForce {
	case `do`, doNot
	public init? (a: Int32) {
		if a == 0 {
			self = .do
		} else if a == 1 {
			self = .doNot
		}
		return nil
	}
}
";
			// var force = SomeForceExtensions.InitOptional (2);
			// Console.WriteLine (force.HasValue);

			var forceID = new CSIdentifier ("force");
			var forceDecl = CSVariableDeclaration.VarLine (forceID, new CSFunctionCall ("SomeForceExtensions.InitOptional", false, CSConstant.Val (7)));
			var printer = CSFunctionCall.ConsoleWriteLine (forceID.Dot (new CSIdentifier ("HasValue")));

			var callingCode = CSCodeBlock.Create (forceDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}

		[Test]
		[Ignore ("apple bug - https://bugs.swift.org/browse/SR-13798")]
		public void NestedEnum ()
		{
			var swiftCode = @"
public struct System {
    public enum LOAD_AVG {
        case short
        case long
    }    
    public static func loadAverage(type: LOAD_AVG = .long) -> Int {
        return 42
    }
}";
			var getVal = new CSFunctionCall ("System.LoadAverage", false, new CSIdentifier ("System.LOAD_AVG.Short"));
			var printer = CSFunctionCall.ConsoleWriteLine (getVal);

			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n");
		}
	}
}
