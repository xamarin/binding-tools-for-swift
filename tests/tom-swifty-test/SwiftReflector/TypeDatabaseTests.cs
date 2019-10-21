// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class TypeDatabaseTests {
		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void HasCGFloat (PlatformName platform)
		{
			var swiftCode = @"
import Foundation
import CoreGraphics

public func returnsFloat() -> CGFloat {
	return 42.5
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("TopLevelEntities.ReturnsFloat", false));
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42.5\n", platform: platform);
		}


		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void CGFloatIdentity (PlatformName platform)
		{
			var swiftCode = @"
import Foundation
import CoreGraphics

public func identityCrisis (f: CGFloat) -> CGFloat {
	return f
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("TopLevelEntities.IdentityCrisis", false,
			                                                                   new CSCastExpression (new CSSimpleType ("nfloat"), CSConstant.Val(42.5))));
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42.5\n", platform: platform);
		}


		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void CGFloatVirtual (PlatformName platform)
		{
			var swiftCode = @"
import Foundation
import CoreGraphics

open class ItsACGFloat {
    open var value:CGFloat = 0
    public init (with: CGFloat) {
        value = with
    }
    open func getValue () -> CGFloat {
        return value
    }
}
";

			var cgfID = new CSIdentifier ("cgf");
			var cgfDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, cgfID, new CSFunctionCall ("ItsACGFloat", true,
														  new CSCastExpression (new CSSimpleType ("nfloat"), CSConstant.Val (42.5))));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("cgf.GetValue", false));
			var callingCode = CSCodeBlock.Create (cgfDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42.5\n", platform: platform);

		}
	}
}
