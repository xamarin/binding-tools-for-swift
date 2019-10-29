// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using tomwiftytest;
using SwiftReflector.IOUtils;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;
using System.IO;
using Dynamo;
using Dynamo.SwiftLang;
using SwiftReflector.Inventory;
using System.Linq;
using Dynamo.CSLang;
using System.Text;


namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class GenericStructTests {
		void WrapGenStructReturnFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGSRF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public struct FooWGSRF{appendage}<T> {{\nprivate var x:Int = 5\npublic init() {{ }}\npublic func Func(a:T)-> T {{\nreturn a;\n}}\n}}\n";


			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGSRF{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGSRF{appendage}<{cstype}>", true,
											   new CSFunctionCall ("SwiftNominalCtorArgument", true)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", (CSIdentifier)val1));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);


			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapGenStructReturnFunc{appendage}");
		}


		[Test]
		public void TestGenClassReturnBool ()
		{
			WrapGenStructReturnFunc ("BoolT", "bool", "true", "True\n");
			WrapGenStructReturnFunc ("BoolF", "bool", "false", "False\n");
		}

		[Test]
		public void TestGenClassReturnInt ()
		{
			WrapGenStructReturnFunc ("IntPos", "int", "14", "14\n");
			WrapGenStructReturnFunc ("IntNeg", "int", "-87", "-87\n");
		}


		[Test]
		public void TestGenClassReturnUInt ()
		{
			WrapGenStructReturnFunc ("UInt", "uint", "14", "14\n");
		}

		[Test]
		public void TestGenClassReturnFloat ()
		{
			WrapGenStructReturnFunc ("Float", "float", "14.1f", "14.1\n");
		}

		[Test]
		public void TestGenClassReturnDouble ()
		{
			WrapGenStructReturnFunc ("Double", "double", "14.1", "14.1\n");
		}

		[Test]
		public void TestGenClassReturnString ()
		{
			WrapGenStructReturnFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}

		void WrapGenPrivField (string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGPV{cstype} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public struct FooWGPV{cstype}<T> {{\nprivate var x:T\npublic init(a:T) {{\nx = a\n }}\n}}\n";

			CSSimpleType fooType = new CSSimpleType ($"FooWGPV{cstype}", false, cstype);

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGPV{cstype}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall (fooType.ToString (),
											   true, new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("StructMarshal.Marshaler.Sizeof", CSFunctionCall.Function ("foo.GetType")));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenPrivField{cstype}");
		}

		[Test]
		public void PrivateGenFieldBool ()
		{
			WrapGenPrivField ("bool", "true", "1\n");
		}

		[Test]
		public void PrivateGenFieldInt ()
		{
			WrapGenPrivField ("int", "5", "4\n");
		}

		[Test]
		public void PrivateGenFieldUInt ()
		{
			WrapGenPrivField ("uint", "5", "4\n");
		}

		[Test]
		public void PrivateGenFieldNInt ()
		{
			WrapGenPrivField ("nint", "5", "8\n");
		}

		[Test]
		public void PrivateGenFieldNUInt ()
		{
			WrapGenPrivField ("nuint", "5", "8\n");
		}

		[Test]
		public void PrivateGenFieldFloat ()
		{
			WrapGenPrivField ("float", "5.1f", "4\n");
		}

		[Test]
		public void PrivateGenFieldDouble ()
		{
			WrapGenPrivField ("double", "5.1", "8\n");
		}

		void WrapGenFieldGetOnly (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
			    $"public struct BarWGFGO{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
			    $"public struct FooWGFGO{appendage}<T> {{\npublic private(set) var x:T\npublic init(a:T) {{\nx = a\n }}\n}}\n";


			CSSimpleType fooType = new CSSimpleType ($"FooWGFGO{appendage}", false, cstype);

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGFGO{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall (fooType.ToString (),
											   true, new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo.X");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenFieldGetOnly{appendage}");
		}

		[Test]
		public void GenStructGetBool ()
		{
			WrapGenFieldGetOnly ("BoolT", "bool", "true", "True\n");
			WrapGenFieldGetOnly ("BoolF", "bool", "false", "False\n");
		}


		[Test]
		public void GenStructGetInt ()
		{
			WrapGenFieldGetOnly ("IntPos", "int", "-18", "-18\n");
			WrapGenFieldGetOnly ("IntNeg", "int", "871", "871\n");
		}

		[Test]
		public void GenStructGetNInt ()
		{
			WrapGenFieldGetOnly ("NIntPos", "nint", "-18", "-18\n");
			WrapGenFieldGetOnly ("NIntNeg", "nint", "871", "871\n");
		}

		[Test]
		public void GenStructGetUInt ()
		{
			WrapGenFieldGetOnly ("UInt", "uint", "18", "18\n");
		}

		[Test]
		public void GenStructGetNUInt ()
		{
			WrapGenFieldGetOnly ("NUInt", "nuint", "18", "18\n");
		}

		[Test]
		public void GenStructGetFloat ()
		{
			WrapGenFieldGetOnly ("Float", "float", "18.1f", "18.1\n");
		}

		[Test]
		public void GenStructGetDouble ()
		{
			WrapGenFieldGetOnly ("Double", "double", "18.1", "18.1\n");
		}

		[Test]
		public void GenStructGetString ()
		{
			WrapGenFieldGetOnly ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}


		void WrapGenFieldGetSet (string cstype, string val1, string val2, string expected)
		{
			string swiftCode =
		$"public struct BarWGFGS{cstype} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public struct FooWGFGS{cstype}<T> {{\npublic var x:T\npublic init(a:T) {{\nx = a\n }}\n}}\n";

			CSSimpleType fooType = new CSSimpleType ($"FooWGFGS{cstype}", false, cstype);

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGFGS{cstype}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall (fooType.ToString (),
											   true, new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo.X");

			CSLine setter = CSAssignment.Assign ("foo.X", CSAssignmentOperator.Assign, new CSIdentifier (val2));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer, setter, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenFieldGetSet{cstype}");
		}

		[Test]
		public void GenStructGetSetBool ()
		{
			WrapGenFieldGetSet ("bool", "true", "false", "True\nFalse\n");
		}

		[Test]
		public void GenStructGetSetInt ()
		{
			WrapGenFieldGetSet ("int", "-18", "451", "-18\n451\n");
		}

		[Test]
		public void GenStructGetSetUInt ()
		{
			WrapGenFieldGetSet ("uint", "18", "451", "18\n451\n");
		}

		[Test]
		public void GenStructGetSetNInt ()
		{
			WrapGenFieldGetSet ("nint", "-18", "451", "-18\n451\n");
		}

		[Test]
		public void GenStructGetSetNUInt ()
		{
			WrapGenFieldGetSet ("nuint", "18", "451", "18\n451\n");
		}

		[Test]
		public void GenStructGetSetFloat ()
		{
			WrapGenFieldGetSet ("float", "18.1f", "451.2f", "18.1\n451.2\n");
		}

		[Test]
		public void GenStructGetSetDouble ()
		{
			WrapGenFieldGetSet ("double", "18.1", "451.2", "18.1\n451.2\n");
		}

		[Test]
		public void GenStructGetSetString ()
		{
			WrapGenFieldGetSet ("SwiftString", "SwiftString.FromString(\"hi mom\")", "SwiftString.FromString(\"frobozz\")", "hi mom\nfrobozz\n");
		}

		void WrapGenSubscriptGetSet (string cstype, string val1, string val2, string expected)
		{
			string swiftCode =
		$"public struct BarWGSbGS{cstype} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public struct FooWGSbGS{cstype}<T> {{\nprivate var x:T\npublic init(a:T) {{\nx = a\n }}\n" +
		$"public subscript(index:T) -> T {{\nget {{\nreturn x\n}}\nset {{\n x = newValue\n}}\n}}\n}}\n";

			CSSimpleType fooType = new CSSimpleType ($"FooWGSbGS{cstype}", false, cstype);

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGSbGS{cstype}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall (fooType.ToString (),
											   true, new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)$"foo[{val1}]");


			CSLine setter = CSAssignment.Assign ($"foo[{val1}]", CSAssignmentOperator.Assign, new CSIdentifier (val2));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer, setter, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenSubscriptGetSet{cstype}");
		}

		[Test]
		public void WrapGenSubscriptGetSetBool ()
		{
			WrapGenSubscriptGetSet ("bool", "true", "false", "True\nFalse\n");
		}

		[Test]
		public void WrapGenSubscriptGetSetInt ()
		{
			WrapGenSubscriptGetSet ("int", "-18", "451", "-18\n451\n");
		}

		[Test]
		public void WrapGenSubscriptGetSetUInt ()
		{
			WrapGenSubscriptGetSet ("uint", "18", "451", "18\n451\n");
		}
	
		[Test]
		public void WrapGenSubscriptGetSetNInt ()
		{
			WrapGenSubscriptGetSet ("nint", "-18", "451", "-18\n451\n");
		}

		[Test]
		public void WrapGenSubscriptGetSetNUInt ()
		{
			WrapGenSubscriptGetSet ("nuint", "18", "451", "18\n451\n");
		}

		[Test]
		public void WrapGenSubscriptGetSetFloat ()
		{
			WrapGenSubscriptGetSet ("float", "18.1f", "451.2f", "18.1\n451.2\n");
		}

		[Test]
		public void WrapGenSubscriptGetSetDouble ()
		{
			WrapGenSubscriptGetSet ("double", "18.1", "451.2", "18.1\n451.2\n");
		}

		[Test]
		public void WrapGenSubscriptGetSetString ()
		{
			WrapGenSubscriptGetSet ("SwiftString", "SwiftString.FromString(\"hi mom\")", "SwiftString.FromString(\"frobozz\")", "hi mom\nfrobozz\n");
		}

		void WrapGenEnum (string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGE{cstype} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public enum MyOptionWGE{cstype}<T> {{\ncase Some(T)\ncase None\n}}\n";

			CSSimpleType fooType = new CSSimpleType ($"MyOptionWGE{cstype}", false, cstype);

			CSLine fooDecl = CSVariableDeclaration.VarLine (fooType, "foo",
								   new CSFunctionCall ($"{fooType.ToString ()}.NewSome",
										    false, new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)$"foo.ValueSome");


			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenEnum{cstype}");
		}

		[Test]
		public void TestGenEnumBool ()
		{
			WrapGenEnum ("bool", "true", "True\n");
		}

		[Test]
		public void TestGenEnumInt ()
		{
			WrapGenEnum ("int", "-18", "-18\n");
		}

		[Test]
		public void TestGenEnumUInt ()
		{
			WrapGenEnum ("uint", "(uint)753", "753\n");
		}

		[Test]
		public void TestGenEnumFloat ()
		{
			WrapGenEnum ("float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestGenEnumDouble ()
		{
			WrapGenEnum ("double", "42.1", "42.1\n");
		}

		[Test]
		public void TestGenEnumString ()
		{
			WrapGenEnum ("SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}
	}
}
