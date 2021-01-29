// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftReflector.TypeMapping;
using SwiftReflector;
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

		[Test]
		[TestCase ("!", OperatorType.Prefix, null)]
		[TestCase ("~", OperatorType.Prefix, null)]
		[TestCase ("+", OperatorType.Prefix, null)]
		[TestCase ("-", OperatorType.Prefix, null)]
		[TestCase ("..<", OperatorType.Prefix, null)]
		[TestCase ("...", OperatorType.Prefix, null)]
		[TestCase ("...", OperatorType.Postfix, null)]
		[TestCase ("<<", OperatorType.Infix, "BitWiseShiftPrecedence")]
		[TestCase (">>", OperatorType.Infix, "BitWiseShiftPrecedence")]
		[TestCase ("*", OperatorType.Infix, "MultiplicationPrecedence")]
		[TestCase ("/", OperatorType.Infix, "MultiplicationPrecedence")]
		[TestCase ("%", OperatorType.Infix, "MultiplicationPrecedence")]
		[TestCase ("&*", OperatorType.Infix, "MultiplicationPrecedence")]
		[TestCase ("&", OperatorType.Infix, "MultiplicationPrecedence")]
		[TestCase ("+", OperatorType.Infix, "AdditionPrecedence")]
		[TestCase ("-", OperatorType.Infix, "AdditionPrecedence")]
		[TestCase ("&+", OperatorType.Infix, "AdditionPrecedence")]
		[TestCase ("&-", OperatorType.Infix, "AdditionPrecedence")]
		[TestCase ("|", OperatorType.Infix, "AdditionPrecedence")]
		[TestCase ("^", OperatorType.Infix, "AdditionPrecedence")]
		[TestCase ("..<", OperatorType.Infix, "RangeFormationPrecedence")]
		[TestCase ("...", OperatorType.Infix, "RangeFormationPrecedence")]
		[TestCase ("is", OperatorType.Infix, "CastingPrecedence")]
		[TestCase ("as", OperatorType.Infix, "CastingPrecedence")]
		[TestCase ("as?", OperatorType.Infix, "CastingPrecedence")]
		[TestCase ("as!", OperatorType.Infix, "CastingPrecedence")]
		[TestCase ("<", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("<=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (">", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (">=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("==", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("!=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("===", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("!==", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("~=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (".==", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (".!=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (".<", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (".<=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (".>", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase (".>=", OperatorType.Infix, "ComparisonPrecedence")]
		[TestCase ("&&", OperatorType.Infix, "LogicalConjunctionPrecedence")]
		[TestCase ("||", OperatorType.Infix, "LogicalConjunctionPrecedence")]
		[TestCase ("?:", OperatorType.Infix, "TernaryPrecedence")]
		[TestCase ("=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("*=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("/=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("%=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("+=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("-=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("<<=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase (">>=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("&=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("|=", OperatorType.Infix, "AssignmentPrecedence")]
		[TestCase ("^=", OperatorType.Infix, "AssignmentPrecedence")]
		public void TestSwiftCoreOperator (string opName, OperatorType opType, string precedenceGroup)
		{
			var td = new TypeDatabase ();
			var path = GetSwiftCoreDB ();
			Assert.IsNotNull (path, "couldn't find SwiftCore.xml!");
			td.Read (path);

			var operators = td.OperatorsForModule ("Swift");
			Assert.AreNotEqual (0, operators.Count (), "no operators?!");

			var opWithName = operators.Where (op => op.Name == opName);
			Assert.IsTrue (opWithName.Any (), $"no operators named {opName}");

			var opWithType = opWithName.Where (op => op.OperatorType == opType);
			Assert.IsTrue (opWithType.Any (), $"no operator named {opName} with type {opType}");

			if (precedenceGroup != null) {
				Assert.IsNotNull (opWithType.FirstOrDefault (op => op.PrecedenceGroup == precedenceGroup), $"precendence mismatch on {opName} of {opType} with {precedenceGroup}");
			}
		}


		string GetSwiftCoreDB ()
		{
			var dblocs = Compiler.kTypeDatabases;
			foreach (var path in dblocs) {
				var target = Path.Combine (path, "SwiftCore.xml");
				if (File.Exists (target))
					return target;
			}
			return null;
		}
	}
}
