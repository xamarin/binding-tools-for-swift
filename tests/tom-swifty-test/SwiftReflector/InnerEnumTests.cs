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
	public class InnerEnumTests {

		[Test]
		public void StructBadInnerEnumSmokeTest ()
		{
			string swiftCode =
				"public struct CommandCougar {\n" +
				"    public enum Errors : Error { \n" +
				"        case validate(String)\n" +
				"        case callback(String)\n" +
				"        case parse(String)\n" +
				"    }\n" +
				"}";


			// typeof (CommandCougar.Errors).FullName
			var innerTypeName = new CSSimpleType ("CommandCougar.Errors").Typeof ().Dot (new CSIdentifier ("FullName"));
			var printer = CSFunctionCall.ConsoleWriteLine (innerTypeName);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "InnerEnumTests.CommandCougar+Errors\n");
		}

		[Test]
		public void ClassBadInnerEnumSmokeTest ()
		{
			string swiftCode =
				"public class CommandCougar1 {\n" +
				"    public init () { }\n" +
				"    public enum Errors : Error { \n" +
				"        case validate(String)\n" +
				"        case callback(String)\n" +
				"        case parse(String)\n" +
				"    }\n" +
				"}";


			// typeof (CommandCougar.Errors).FullName
			var innerTypeName = new CSSimpleType ("CommandCougar1.Errors").Typeof ().Dot (new CSIdentifier ("FullName"));
			var printer = CSFunctionCall.ConsoleWriteLine (innerTypeName);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "InnerEnumTests.CommandCougar1+Errors\n");
		}

		[Test]
		public void EnumBadInnerEnumSmokeTest ()
		{
			string swiftCode =
				"public enum CommandCougar2 {\n" +
				"    case Foo(String)\n" +
				"    case Bar(String)\n" +
				"    public enum Errors : Error { \n" +
				"        case validate(String)\n" +
				"        case callback(String)\n" +
				"        case parse(String)\n" +
				"    }\n" +
				"}";


			// typeof (CommandCougar.Errors).FullName
			var innerTypeName = new CSSimpleType ("CommandCougar2.Errors").Typeof ().Dot (new CSIdentifier ("FullName"));
			var printer = CSFunctionCall.ConsoleWriteLine (innerTypeName);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "InnerEnumTests.CommandCougar2+Errors\n");
		}
	}
}
