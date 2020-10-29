// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class UnicodeTests {
		[Test]
		public void TopLevelFunctionAllUnicode ()
		{
			string swiftCode =
				"public func \x0200() -> Int {\n" +
				"    return 3\n" +
				"}\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.\x0200")));
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TopLevelFunctionAllUnicode1 ()
		{
			string swiftCode =
				"public func f\x3004() -> Int {\n" +
				"    return 3\n" +
				"}\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.FU3004")));
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TopLevelPropertyAllUnicode1 ()
		{
			string swiftCode =
				"public var v\x3004:Int = 3\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"TopLevelEntities.VU3004"));
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void UnicodeInClassName ()
		{
			string swiftCode =
				"public class c\x1800 {\n" +
				"   public init() { }\n" +
				"   public func doIt() -> Int {\n" +
				"       return 3\n" +
				"   }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"CU1800", "cl", CSFunctionCall.Ctor ("CU1800")),
				CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("cl.DoIt"))
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void UnicodeInClassNameAndMethod ()
		{
			string swiftCode =
				"public class cc1\x1800 {\n" +
				"   public init() { }\n" +
				"   public func doIt\x3004() -> Int {\n" +
				"       return 3\n" +
				"   }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"Cc1U1800", "cl", CSFunctionCall.Ctor ("Cc1U1800")),
				CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("cl.DoItU3004"))
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void UnicodeInClassNameAndProperty ()
		{
			string swiftCode =
				"public class cd1\x1800 {\n" +
				"   public init() { }\n" +
				"   public var v\x3004:Int = 3\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"Cd1U1800", "cl", CSFunctionCall.Ctor ("Cd1U1800")),
				CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"cl.VU3004")
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void UnicodeInStructName ()
		{
			string swiftCode =
				"public struct s\x1800 {\n" +
				"   public init() { }\n" +
				"   public func doIt() -> Int {\n" +
				"       return 3\n" +
				"   }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"SU1800", "st", CSFunctionCall.Ctor ("SU1800")),
				CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("st.DoIt"))
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void UnicodeInStructNameAndMethod ()
		{
			string swiftCode =
				"public struct s2\x1800 {\n" +
				"   public init() { }\n" +
				"   public func doIt\x3004() -> Int {\n" +
				"       return 3\n" +
				"   }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"S2U1800", "st", CSFunctionCall.Ctor ("S2U1800")),
				CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("st.DoItU3004"))
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void UnicodeInStructNameAndProperty ()
		{
			string swiftCode =
				"public struct s3\x1800 {\n" +
				"   public init() { }\n" +
				"   public var v\x3004:Int = 3\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"S3U1800", "st", CSFunctionCall.Ctor ("S3U1800")),
				CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"st.VU3004")
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void UnicodeInOperatorName ()
		{
			string swiftCode =
				"infix operator \x2295\x2295\n" +
				"public func \x2295\x2295 (left:Int, right: Int) -> Int {\n" +
				"    return left + right\n" +
				"}\n";
			var callingCode = new CodeElementCollection<ICodeElement> {
				CSVariableDeclaration.VarLine ((CSSimpleType)"nint", "x", CSFunctionCall.Function ("TopLevelEntities.InfixOperatorCirclePlusCirclePlus", CSConstant.Val (3), CSConstant.Val (4))),
				CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"x")
			};

			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}


		[Test]
		[Ignore ("apple's top level let issue")]
		public void TheEpsilonIssue ()
		{
			string swiftCode =
				"public let 𝑒 = 2.718\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();

			callingCode.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"TopLevelEntities.LittleEpsilon"));

			TestRunning.TestAndExecute (swiftCode, callingCode, "2.718\n");
		}
	}
}
