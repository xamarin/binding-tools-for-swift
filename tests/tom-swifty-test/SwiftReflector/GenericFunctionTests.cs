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
	public class GenericFunctionTests {
		void WrapGenFunc (string appendage, string cstype, string val1, string expected, string iosExpected = null)
		{
			string swiftCode =
		TestRunningCodeGenerator.kSwiftFileWriter +
		$"public struct BarWGF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
			   $"open class FooWGF{appendage} {{\npublic init() {{ }}\n }}\npublic func globalFunc{appendage}<T>(a:T) {{\nvar s = \"\"\nprint(\"\\(a)\", to:&s);\nwriteToFile(s, \"WrapGenFunc{appendage}\")\n}}";

			CSLine printer = CSFunctionCall.FunctionCallLine ($"TopLevelEntities.GlobalFunc{appendage}<{cstype}>", false,
								     new CSIdentifier (val1));

			CSCodeBlock callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenFunc{appendage}", iosExpectedOutput: iosExpected);
		}

		[Test]
		public void TestsingleGenericBool ()
		{
			WrapGenFunc ("BoolTrue", "bool", "true", "true\n");
			WrapGenFunc ("BoolFalse", "bool", "false", "false\n");
		}

		[Test]
		public void TestsingleGenericInt ()
		{
			WrapGenFunc ("IntPos", "int", "42", "42\n");
			WrapGenFunc ("IntNeg", "int", "-37", "-37\n");
		}

		[Test]
		public void TestsingleGenericUInt ()
		{
			WrapGenFunc ("UInt", "uint", "42", "42\n");
		}

		[Test]
		public void TestsingleGenericFloat ()
		{
			WrapGenFunc ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestsingleGenericDouble ()
		{
			WrapGenFunc ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void TestsingleGenericString ()
		{
			WrapGenFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}

		[Test]
		public void TestsingleGenericClass ()
		{
			WrapGenFunc ("Class", "FooWGFClass", "new FooWGFClass()", "XamWrapping.xam_sub_FooWGFClass\n", "GenericFunctionTestsWrapping.xam_sub_FooWGFClass\n");
		}

		[Test]
		public void TestsingleGenericStruct ()
		{
			WrapGenFunc ("Struct", "BarWGFStruct", "new BarWGFStruct(5)", "BarWGFStruct(X: 5)\n");
		}

		void WrapGenFunc2 (string appendage, string cstype1, string cstype2, string val1, string val2, string expected)
		{
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
				       $"public func globalFuncWGF2{appendage}<T, U>(a:T, b:U) {{\nvar s = \"\"\nprint(\"\\(a) \\(b)\", to: &s)\nwriteToFile(s, \"WrapGenFunc2{appendage}\")\n }}";
			CSLine printer = CSFunctionCall.FunctionCallLine ($"TopLevelEntities.GlobalFuncWGF2{appendage}<{cstype1}, {cstype2}>", false,
								     new CSIdentifier (val1), new CSIdentifier (val2));

			CSCodeBlock callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenFunc2{appendage}");
		}

		[Test]
		public void TestsingleGenericBoolBool ()
		{
			WrapGenFunc2 ("BoolBoolTF", "bool", "bool", "true", "false", "true false\n");
			WrapGenFunc2 ("BoolBoolFT", "bool", "bool", "false", "true", "false true\n");
		}

		[Test]
		public void TestsingleGenericBoolInt ()
		{
			WrapGenFunc2 ("BoolInt", "bool", "int", "true", "42", "true 42\n");
			WrapGenFunc2 ("IntBool", "int", "bool", "42", "true", "42 true\n");
		}

		[Test]
		public void TestsingleGenericBoolFloat ()
		{
			WrapGenFunc2 ("BoolFloat", "bool", "float", "true", "42.1f", "true 42.1\n");
			WrapGenFunc2 ("FloatBool", "float", "bool", "42.1f", "true", "42.1 true\n");
		}

		[Test]
		public void TestsingleGenericBoolDouble ()
		{
			WrapGenFunc2 ("BoolDouble", "bool", "double", "true", "42.1", "true 42.1\n");
			WrapGenFunc2 ("DoubleBool", "double", "bool", "42.1", "true", "42.1 true\n");
		}

		[Test]
		public void TestsingleGenericBoolString ()
		{
			WrapGenFunc2 ("BoolString", "bool", "SwiftString", "true", "SwiftString.FromString(\"hi mom\")", "true hi mom\n");
			WrapGenFunc2 ("StringBool", "SwiftString", "bool", "SwiftString.FromString(\"hi mom\")", "true", "hi mom true\n");
		}


		[Test]
		public void TestsingleGenericIntFloat ()
		{
			WrapGenFunc2 ("IntFloat", "int", "float", "42", "42.1f", "42 42.1\n");
			WrapGenFunc2 ("FloatInt", "float", "int", "42.1f", "42", "42.1 42\n");
		}

		[Test]
		public void TestsingleGenericIntDouble ()
		{
			WrapGenFunc2 ("IntDouble", "int", "double", "42", "42.1", "42 42.1\n");
			WrapGenFunc2 ("DoubleInt", "double", "int", "42.1", "42", "42.1 42\n");
		}

		[Test]
		public void TestsingleGenericIntString ()
		{
			WrapGenFunc2 ("IntString", "int", "SwiftString", "42", "SwiftString.FromString(\"hi mom\")", "42 hi mom\n");
			WrapGenFunc2 ("StringInt", "SwiftString", "int", "SwiftString.FromString(\"hi mom\")", "42", "hi mom 42\n");
		}

		[Test]
		public void TestsingleGenericFloatDouble ()
		{
			WrapGenFunc2 ("FloatDouble", "float", "double", "44.1f", "42.1", "44.1 42.1\n");
			WrapGenFunc2 ("DoubleFloat", "double", "float", "42.1", "44.1f", "42.1 44.1\n");
		}

		[Test]
		public void TestsingleGenericFloatString ()
		{
			WrapGenFunc2 ("FloatString", "float", "SwiftString", "44.1f", "SwiftString.FromString(\"hi mom\")", "44.1 hi mom\n");
			WrapGenFunc2 ("StringFloat", "SwiftString", "float", "SwiftString.FromString(\"hi mom\")", "44.1f", "hi mom 44.1\n");
		}

		void WrapGenReturnFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGRF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public class FooWGRF{appendage} {{\npublic init() {{ }}\n }}\npublic func globalFuncWGRF{appendage}<T>(a:T)-> T {{\nreturn a;\n}}";

			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"TopLevelEntities.GlobalFuncWGRF{appendage}<{cstype}>", (CSIdentifier)val1));
			CSCodeBlock callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, $"WrapGenReturnFunc{appendage}");
		}

		[Test]
		public void TestGenReturnBool ()
		{
			WrapGenReturnFunc ("BoolT", "bool", "true", "True\n");
			WrapGenReturnFunc ("BoolF", "bool", "false", "False\n");
		}

		[Test]
		public void TestGenReturnInt ()
		{
			WrapGenReturnFunc ("IntPos", "int", "42", "42\n");
			WrapGenReturnFunc ("IntNeg", "int", "-73", "-73\n");
		}

		[Test]
		public void TestGenReturnFloat ()
		{
			WrapGenReturnFunc ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestGenReturnDouble ()
		{
			WrapGenReturnFunc ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void TestGenReturnString ()
		{
			WrapGenReturnFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}


		[Test]
		public void TestGenReturnClass ()
		{
			WrapGenReturnFunc ("Class", "FooWGRFClass", "new FooWGRFClass()", "GenericFunctionTests.FooWGRFClass\n");
		}

		[Test]
		public void TestGenReturnStruct ()
		{
			WrapGenReturnFunc ("Struct", "BarWGRFStruct", "new BarWGRFStruct(5)", "GenericFunctionTests.BarWGRFStruct\n");
		}

		void WrapGenNonVirtClassReturnFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGNVCRF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public final class FooWGNVCRF{appendage}<T> {{\npublic init() {{ }}\npublic func Func(a:T)-> T {{\nreturn a;\n}}\n}}\n";
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGNVCRF{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGNVCRF{appendage}<{cstype}>", true));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", (CSIdentifier)val1));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenNonVirtClassReturnFunc{appendage}");
		}

		[Test]
		public void TestGenClassReturnBool ()
		{
			WrapGenNonVirtClassReturnFunc ("BoolT", "bool", "true", "True\n");
			WrapGenNonVirtClassReturnFunc ("BoolF", "bool", "false", "False\n");
		}

		[Test]
		public void TestGenClassReturnInt ()
		{
			WrapGenNonVirtClassReturnFunc ("IntPos", "int", "42", "42\n");
			WrapGenNonVirtClassReturnFunc ("IntNeg", "int", "-37", "-37\n");
		}

		[Test]
		public void TestGenClassReturnUint ()
		{
			WrapGenNonVirtClassReturnFunc ("UInt", "uint", "42", "42\n");
		}

		[Test]
		public void TestGenClassReturnFloat ()
		{
			WrapGenNonVirtClassReturnFunc ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestGenClassReturnDouble ()
		{
			WrapGenNonVirtClassReturnFunc ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void TestGenClassReturnString ()
		{
			WrapGenNonVirtClassReturnFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}
		[Test]
		public void TestGenClassReturnStruct ()
		{
			WrapGenNonVirtClassReturnFunc ("Struct", "BarWGNVCRFStruct", "new BarWGNVCRFStruct(12)", "GenericFunctionTests.BarWGNVCRFStruct\n");
		}

		void WrapGenReallyVirtClassReturnFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarRYWGVCR{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"open class FooRYWGVCR{appendage}<T> {{\npublic init() {{ }}\nopen func Func(a:T)-> T {{\nreturn a;\n}}\n}}\n";

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooRYWGVCR{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooRYWGVCR{appendage}<{cstype}>", true));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", (CSIdentifier)val1));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenReallyVirtClassReturnFunc{appendage}");
		}

		[Test]
		[Ignore ("Error in swift code generation")]
		public void TestGenReallyVirtClassReturnBool ()
		{
			WrapGenReallyVirtClassReturnFunc ("BoolT", "bool", "true", "True\n");
			WrapGenReallyVirtClassReturnFunc ("BoolF", "bool", "false", "False\n");
		}

		void WrapGenVirtClassReturnFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGVCR{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public class FooWGVCR{appendage}<T> {{\npublic init() {{ }}\npublic func Func(a:T)-> T {{\nreturn a;\n}}\n}}\n";

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGVCR{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGVCR{appendage}<{cstype}>", true));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", (CSIdentifier)val1));

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenVirtClassReturnFunc{appendage}");
		}

		[Test]
		public void TestGenVirtClassReturnBool ()
		{
			WrapGenVirtClassReturnFunc ("BoolT", "bool", "true", "True\n");
			WrapGenVirtClassReturnFunc ("BoolF", "bool", "false", "False\n");
		}


		[Test]
		public void TestGenVirtClassReturnInt ()
		{
			WrapGenVirtClassReturnFunc ("IntPos", "nint", "42", "42\n");
			WrapGenVirtClassReturnFunc ("IntNeg", "nint", "-37", "-37\n");
		}

		[Test]
		public void TestGenVirtClassReturnUInt ()
		{
			WrapGenVirtClassReturnFunc ("UInt", "nuint", "42", "42\n");
		}

		[Test]
		public void TestGenVirtClassReturnFloat ()
		{
			WrapGenVirtClassReturnFunc ("FloatPos", "float", "42.1f", "42.1\n");
			WrapGenVirtClassReturnFunc ("FloatNeg", "float", "-37.2f", "-37.2\n");
		}

		[Test]
		public void TestGenVirtClassReturnDouble ()
		{
			WrapGenVirtClassReturnFunc ("DoublePos", "double", "42.1", "42.1\n");
			WrapGenVirtClassReturnFunc ("DoubleNeg", "double", "-37.2", "-37.2\n");
		}


		[Test]
		public void TestGenVirtClassReturnString ()
		{
			WrapGenVirtClassReturnFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}

		[Test]
		public void TestGenVirtClassReturnsTuple ()
		{
			WrapGenVirtClassReturnFunc ("Tuple", "Tuple<bool, bool>", "new Tuple<bool, bool>(true, false)", "(True, False)\n");
		}

		[Test]
		public void TestGenVirtClassReturnsStruct ()
		{
			WrapGenVirtClassReturnFunc ("Struct", "BarWGVCRStruct", "new BarWGVCRStruct(7)", "GenericFunctionTests.BarWGVCRStruct\n");
		}

		[Test]
		public void TestGenVirtClassReturnsgenClass ()
		{
			WrapGenVirtClassReturnFunc ("Class", "FooWGVCRClass<bool>", "new FooWGVCRClass<bool>()", "GenericFunctionTests.FooWGVCRClass`1[System.Boolean]\n");
		}

		[Test]
		public void WrapGenClassConstraint ()
		{
			string swiftCode =
				"public class BarWGCC {\npublic init() {\n }\n public func getVal() -> Int\n{\nreturn 17;\n}\n}\n" +
		"public final class FooWGCC<T : BarWGCC> {\npublic init() { }\npublic func Func(a:T)-> Int {\nreturn a.getVal();\n}\n}\n";

			CSLine barDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("BarWGCC", false), "bar",
								   new CSFunctionCall ("BarWGCC", true));
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("FooWGCC", false,
										    new CSSimpleType ("BarWGCC")),
								   "foo", new CSFunctionCall ("FooWGCC<BarWGCC>", true));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", (CSIdentifier)"bar"));

			CSCodeBlock callingCode = CSCodeBlock.Create (barDecl, fooDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n");
		}

		[Test]
		public void WrapGenVirtClassConstraint ()
		{
			string swiftCode =
				"public class BarWGVCC {\npublic init() {\n }\n public func getVal() -> Int\n{\nreturn 17;\n}\n}\n" +
		"public class FooWGVCC<T : BarWGVCC> {\npublic init() { }\npublic func Func(a:T)-> Int {\nreturn a.getVal();\n}\n}\n";

			CSLine barDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("BarWGVCC", false), "bar",
								   new CSFunctionCall ("BarWGVCC", true));
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("FooWGVCC", false,
										    new CSSimpleType ("BarWGVCC")),
								   "foo", new CSFunctionCall ("FooWGVCC<BarWGVCC>", true));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", (CSIdentifier)"bar"));

			CSCodeBlock callingCode = CSCodeBlock.Create (barDecl, fooDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n");
		}

		[Test]
		public void WrapGenClassConstraintIncorrectUsage ()
		{
			string swiftCode =
				"public class Bar {\npublic init() {\n }\n public func getVal() -> Int\n{\nreturn 17;\n}\n}\n" +
				"public final class Foo<T : Bar> {\npublic init() { }\npublic func Func(a:T)-> Int {\nreturn a.getVal();\n}\n}\n";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {
				Utils.CompileSwift (swiftCode, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");

				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = CSFile.Create (use, ns);

				CSLine barDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Bar", false), "bar",
														   new CSFunctionCall ("Bar", true));
				CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Foo", false,
																			new CSSimpleType ("int")),
														   "foo", new CSFunctionCall ("Foo<int>", true));


				CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", CSConstant.Val (23)));

				CSCodeBlock mainBody = CSCodeBlock.Create (barDecl, fooDecl, printer);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
					new CSIdentifier ("Main"), new CSParameterList (new CSParameter (new CSSimpleType ("string", true), "args")),
					mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);

				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));
				var exeOutFilename = provider.UniquePath (null, "CSWrap", "exe");
				CodeWriter.WriteToFile (csOutFilename, csfile);

				Assert.Throws <Exception> (() => {
					Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), exeOutFilename, platform: PlatformName.macOS);
				});
			}
		}

		[Test]
		public void WrapGenVirtClassConstraintIncorrectUsage ()
		{
			string swiftCode =
				"public class Bar {\npublic init() {\n }\n public func getVal() -> Int\n{\nreturn 17;\n}\n}\n" +
				"public class Foo<T : Bar> {\npublic init() { }\npublic func Func(a:T)-> Int {\nreturn a.getVal();\n}\n}\n";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {

				Utils.CompileSwift (swiftCode, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");

				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = new CSFile (use, new CSNamespace [] { ns });

				CSLine barDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Bar", false), "bar",
														   new CSFunctionCall ("Bar", true));
				CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Foo", false,
																			new CSSimpleType ("int")),
														   "foo", new CSFunctionCall ("Foo<int>", true));


				CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("foo.Func", CSConstant.Val (23)));

				CSCodeBlock mainBody = CSCodeBlock.Create (barDecl, fooDecl, printer);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
					new CSIdentifier ("Main"), new CSParameterList (new CSParameter (new CSSimpleType ("string", true), "args")),
					mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);

				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));
				var exeOutFilename = provider.UniquePath (null, "CSWrap", "exe");

				CodeWriter.WriteToFile (csOutFilename, csfile);

				Assert.Throws<Exception> (() => {
					Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), exeOutFilename, platform: PlatformName.macOS);
				});
			}
		}

		[Test]
		public void WrapGenClassProtocolConstraintCorrectUsage ()
		{
			string swiftCode =
				"public protocol UpperWGCPCCU {\n func uppy()\n }\n" +
		"public class FooWGCPCCU<T : UpperWGCPCCU> {\npublic init() { }\npublic func uppy(a:T) {\n a.uppy()\n}\n}\n";

			CSClass cl = new CSClass (CSVisibility.Public, "IntUpperWGCPCCU");
			cl.Inheritance.Add (new CSIdentifier ("IUpperWGCPCCU"));
			CSIdentifier valId = new CSIdentifier ("Value");
			cl.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.Int, valId, CSConstant.Val (0), CSVisibility.Public));
			CSCodeBlock body = new CSCodeBlock ();
			CSMethod uppy = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("Uppy"),
						 new CSParameterList (), body);
			body.Add (CSAssignment.Assign (valId, CSAssignmentOperator.Assign, valId + CSConstant.Val (1)));
			cl.Methods.Add (uppy);

			CSLine intUpperDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("IntUpperWGCPCCU", false), "upper",
									new CSFunctionCall ("IntUpperWGCPCCU", true));
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("FooWGCPCCU", false,
										    new CSSimpleType ("IntUpperWGCPCCU")),
								   "foo", new CSFunctionCall ("FooWGCPCCU<IntUpperWGCPCCU>", true));
			CSLine doUpper = CSFunctionCall.FunctionCallLine ("foo.Uppy", false, new CSIdentifier ("upper"));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("upper").Dot (CSIdentifier.Create ("Value")));

			CSCodeBlock callingCode = CSCodeBlock.Create (intUpperDecl, fooDecl, doUpper, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "1\n", otherClass : cl, platform: PlatformName.macOS);
		}


		[Test]
		public void WrapGenClassProtocolConstraintIncorrectUsage ()
		{
			string swiftCode =
				"public protocol Upper {\n func uppy()\n }\n" +
				"public class Foo<T : Upper> {\npublic init() { }\npublic func uppy(a:T) {\n a.uppy()\n}\n}\n";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {

				Utils.CompileSwift (swiftCode, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");

				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = new CSFile (use, new CSNamespace [] { ns });

				CSClass cl = new CSClass (CSVisibility.Public, "IntUpper");
				//				cl.Inheritance.Add(new Identifier("IUpper"));
				CSIdentifier valId = new CSIdentifier ("Value");
				cl.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.Int, valId, CSConstant.Val (0), CSVisibility.Public));
				CSCodeBlock body = new CSCodeBlock ();
				CSMethod uppy = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("Uppy"),
										 new CSParameterList (), body);
				body.Add (CSAssignment.Assign (valId, CSAssignmentOperator.Assign, valId + CSConstant.Val (1)));
				cl.Methods.Add (uppy);
				ns.Block.Add (cl);


				CSLine intUpperDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("IntUpper", false), "upper",
														   new CSFunctionCall ("IntUpper", true));
				CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Foo", false,
																			new CSSimpleType ("IntUpper")),
														   "foo", new CSFunctionCall ("Foo<IntUpper>", true));
				CSLine doUpper = CSFunctionCall.FunctionCallLine ("foo.Uppy", false, new CSIdentifier ("upper"));


				CSLine printer = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("upper").Dot (CSIdentifier.Create ("Value")));

				CSCodeBlock mainBody = CSCodeBlock.Create (intUpperDecl, fooDecl, doUpper, printer);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
					new CSIdentifier ("Main"), new CSParameterList (new CSParameter (new CSSimpleType ("string", true), "args")),
					mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);

				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));

				CodeWriter.WriteToFile (csOutFilename, csfile);

				Assert.Throws<Exception> (() => {
					Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), csOutFilename);
				});
			}
		}

		[Test]
		public void WrapGenClassMultiProtocolConstraintIncorrectUsage ()
		{
			string swiftCode =
				"public protocol Upper {\n func uppy()\n }\n" +
				"public protocol Downer {\n func downy()\n }\n" +
				"public class Foo<T > where T:Upper, T:Downer {\npublic init() { }\n" +
				"public func uppy(a:T) {\n a.uppy()\n}\n" +
				"public func downy(a:T) {\n a.downy()\n}\n" +
				"}\n";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {

				Utils.CompileSwift (swiftCode, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");

				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = new CSFile (use, new CSNamespace [] { ns });

				CSClass cl = new CSClass (CSVisibility.Public, "IntUpper");
				cl.Inheritance.Add (new CSIdentifier ("IUpper"));
				cl.Inheritance.Add (new CSIdentifier ("IDowner"));
				CSIdentifier valId = new CSIdentifier ("Value");
				cl.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.Int, valId, CSConstant.Val (0), CSVisibility.Public));

				CSCodeBlock uppybody = new CSCodeBlock ();
				CSMethod uppy = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("Uppy"),
										 new CSParameterList (), uppybody);
				uppybody.Add (CSAssignment.Assign (valId, CSAssignmentOperator.Assign, valId + CSConstant.Val (1)));
				cl.Methods.Add (uppy);

				CSCodeBlock downybody = new CSCodeBlock ();
				CSMethod downy = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("Downy"),
										 new CSParameterList (), downybody);
				downybody.Add (CSAssignment.Assign (valId, CSAssignmentOperator.Assign, valId - CSConstant.Val (1)));
				cl.Methods.Add (downy);

				ns.Block.Add (cl);


				CSLine intUpperDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("IntUpper", false), "upper",
														   new CSFunctionCall ("IntUpper", true));
				CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Foo", false,
																			new CSSimpleType ("IntUpper")),
														   "foo", new CSFunctionCall ("Foo<IntUpper>", true));
				CSLine doUpper = CSFunctionCall.FunctionCallLine ("foo.Uppy", false, new CSIdentifier ("upper"));
				CSLine doDowner = CSFunctionCall.FunctionCallLine ("foo.Downy", false, new CSIdentifier ("upper"));


				CSLine printer = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("upper").Dot (CSIdentifier.Create ("Value")));

				CSCodeBlock mainBody = CSCodeBlock.Create (intUpperDecl, fooDecl, doUpper, doUpper, doDowner, printer);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
					new CSIdentifier ("Main"), new CSParameterList (new CSParameter (new CSSimpleType ("string", true), "args")),
					mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);



				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));
				string exeFilename = Path.ChangeExtension (csOutFilename, "exe");

				CodeWriter.WriteToFile (csOutFilename, csfile);
				Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), exeFilename, platform: PlatformName.macOS);

				Exception e = Assert.Throws<Exception> (() => {
					TestRunning.CopyTestReferencesTo (provider.DirectoryPath);
					string output = Compiler.RunWithMono (exeFilename, provider.DirectoryPath, platform: PlatformName.macOS);
					Assert.AreEqual ("1\n", output);
				});
				Assert.True (e.Message.Contains ("NotSupportedException"));
			}
		}

		[Test]
		public void WrapGenClassMultiProtocolConstraintCorrectUsage ()
		{
			string swiftCode =
				"public protocol UpperWGCMPCCU {\n func uppy()\n }\n" +
		"public protocol DownerWGCMPCCU {\n func downy()\n }\n" +
		"public class FooWGCMPCCU<T>  where T:UpperWGCMPCCU, T:DownerWGCMPCCU {\npublic init() { }\n" +
				"public func uppy(a:T) {\n a.uppy()\n}\n" +
				"public func downy(a:T) {\n a.downy()\n}\n" +
				"}\n" +
		"public class BarWGCMPCCU : UpperWGCMPCCU, DownerWGCMPCCU {\n" +
				"public var value:Int = 0\n" +
				"public init() { }\n" +
				"public func uppy() {\nvalue = value + 1;\n}\n" +
				"public func downy() {\nvalue = value - 1;\n}\n" +
				"}\n";
			CSLine intUpperDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("BarWGCMPCCU", false), "upper",
									new CSFunctionCall ("BarWGCMPCCU", true));
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("FooWGCMPCCU", false,
										    new CSSimpleType ("BarWGCMPCCU")),
								   "foo", new CSFunctionCall ("FooWGCMPCCU<BarWGCMPCCU>", true));
			CSLine doUpper = CSFunctionCall.FunctionCallLine ("foo.Uppy", false, new CSIdentifier ("upper"));
			CSLine doDowner = CSFunctionCall.FunctionCallLine ("foo.Downy", false, new CSIdentifier ("upper"));


			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("upper").Dot (CSIdentifier.Create ("Value")));

			CSCodeBlock callingCode = CSCodeBlock.Create (intUpperDecl, fooDecl, doUpper, doUpper, doDowner, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "1\n", platform: PlatformName.macOS);
		}

		void WrapGenNonVirtClassPropFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
			    $"public final class FooWGNVCPF{appendage}<T> {{\npublic var x:T\npublic init(a:T) {{ x = a\n }}\n}}\n";

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGNVCPF{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGNVCPF{appendage}<{cstype}>", true,
											   new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo.X");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenNonVirtClassPropFunc{appendage}");
		}

		[Test]
		public void TestFinalGenPropBool ()
		{
			WrapGenNonVirtClassPropFunc ("BoolTrue", "bool", "true", "True\n");
			WrapGenNonVirtClassPropFunc ("BoolFalse", "bool", "false", "False\n");
		}


		[Test]
		public void TestFinalGenPropInt ()
		{
			WrapGenNonVirtClassPropFunc ("NIntPos", "nint", "14", "14\n");
			WrapGenNonVirtClassPropFunc ("NIntNeg", "nint", "-25", "-25\n");
		}

		[Test]
		public void TestFinalGenPropNUint ()
		{
			WrapGenNonVirtClassPropFunc ("NUInt", "nuint", "14", "14\n");
		}

		[Test]
		public void TestFinalGenPropFloat ()
		{
			WrapGenNonVirtClassPropFunc ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestFinalGenPropDouble ()
		{
			WrapGenNonVirtClassPropFunc ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void TestFinalGenPropString ()
		{
			WrapGenNonVirtClassPropFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}


		void WrapGenVirtClassPropFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGVCPF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public class FooWGVCPF{appendage}<T> {{\npublic var x:T\npublic init(a:T) {{ x = a\n }}\n}}\n";

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGVCPF{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGVCPF{appendage}<{cstype}>", true,
											   new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo.X");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenVirtClassPropFunc{appendage}");
		}

		[Test]
		public void TestVirtGenPropBool ()
		{
			WrapGenVirtClassPropFunc ("BoolT", "bool", "true", "True\n");
			WrapGenVirtClassPropFunc ("BoolF", "bool", "false", "False\n");
		}

		[Test]
		public void TestVirtGenPropInt ()
		{
			WrapGenVirtClassPropFunc ("NIntPos", "nint", "14", "14\n");
			WrapGenVirtClassPropFunc ("NIntNeg", "nint", "-32", "-32\n");
		}

		[Test]
		public void TestVirtGenPropUInt ()
		{
			WrapGenVirtClassPropFunc ("UInt", "nuint", "14", "14\n");
		}

		[Test]
		public void TestVirtGenPropFloat ()
		{
			WrapGenVirtClassPropFunc ("Float", "float", "14.1f", "14.1\n");
		}

		[Test]
		public void TestVirtGenPropDouble ()
		{
			WrapGenVirtClassPropFunc ("Double", "double", "14.1", "14.1\n");
		}

		[Test]
		public void TestVirtGenPropString ()
		{
			WrapGenVirtClassPropFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}

		void WrapGenNonVirtClassGetIndexerFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGNVCGIF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public final class FooWGNVCGIF{appendage}<T> {{\nprivate var x:T\npublic init(a:T) {{ x = a\n }}\npublic subscript(int:Int) -> T\n{{\nreturn x\n}}\n}}\n";

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGNVCGIF{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGNVCGIF{appendage}<{cstype}>", true,
											   new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo[0]");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenNonVirtClassGetIndexerFunc{appendage}");
		}

		[Test]
		public void TestFinalSubscriptGetBool ()
		{
			WrapGenNonVirtClassGetIndexerFunc ("BoolT", "bool", "true", "True\n");
			WrapGenNonVirtClassGetIndexerFunc ("BoolF", "bool", "false", "False\n");
		}

		[Test]
		public void TestFinalSubscriptGetInt ()
		{
			WrapGenNonVirtClassGetIndexerFunc ("IntNeg", "int", "-18", "-18\n");
			WrapGenNonVirtClassGetIndexerFunc ("IntPos", "int", "42", "42\n");
		}

		[Test]
		public void TestFinalSubscriptGetUInt ()
		{
			WrapGenNonVirtClassGetIndexerFunc ("UInt", "uint", "42", "42\n");
		}

		[Test]
		public void TestFinalSubscriptGetFloat ()
		{
			WrapGenNonVirtClassGetIndexerFunc ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestFinalSubscriptGetDouble ()
		{
			WrapGenNonVirtClassGetIndexerFunc ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void TestFinalSubscriptGetString ()
		{
			WrapGenNonVirtClassGetIndexerFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}

		void WrapGenNonVirtClassGetSetIndexerFunc (string cstype, string firstVal, string secondVal, string expected)
		{
			string swiftCode =
		$"public struct BarWGNVCGSIF{cstype} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public final class FooWGNVCGSIF{cstype}<T> {{\nprivate var x:T\npublic init(a:T) {{ x = a\n }}\npublic subscript(int:Int) -> T\n{{\nget {{\nreturn x\n}}\n set(newValue) {{\n x=newValue\n}}\n}}\n}}\n";
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGNVCGSIF{cstype}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGNVCGSIF{cstype}<{cstype}>", true,
											   new CSIdentifier (firstVal)));

			CSLine assign = CSAssignment.Assign ("foo[0]", CSAssignmentOperator.Assign, new CSIdentifier (secondVal));

			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo[0]");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, assign, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenNonVirtClassGetSetIndexerFunc{cstype}");
		}

		[Test]
		public void TestFinalSubscriptGetSetBool ()
		{
			WrapGenNonVirtClassGetSetIndexerFunc ("bool", "false", "true", "True\n");
		}

		[Test]
		public void TestFinalSubscriptGetSetInt ()
		{
			WrapGenNonVirtClassGetSetIndexerFunc ("int", "42", "-18", "-18\n");
		}

		[Test]
		public void TestFinalSubscriptGetSetUInt ()
		{
			WrapGenNonVirtClassGetSetIndexerFunc ("uint", "42", "75", "75\n");
		}

		[Test]
		public void TestFinalSubscriptGetSetFloat ()
		{
			WrapGenNonVirtClassGetSetIndexerFunc ("float", "42.0f", "-18.1f", "-18.1\n");
		}

		[Test]
		public void TestFinalSubscriptGetSetDouble ()
		{
			WrapGenNonVirtClassGetSetIndexerFunc ("double", "42.0", "-18.1", "-18.1\n");
		}

		[Test]
		public void TestFinalSubscriptGetSetString ()
		{
			WrapGenNonVirtClassGetSetIndexerFunc ("SwiftString", "SwiftString.FromString(\"nothing\")", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}
	
		void WrapGenVirtClassGetIndexerFunc (string appendage, string cstype, string val1, string expected)
		{
			string swiftCode =
		$"public struct BarWGVCGIF{appendage} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public class FooWGVCGIF{appendage}<T> {{\nprivate var x:T\npublic init(a:T) {{ x = a\n }}\npublic subscript(int:Int) -> T\n{{\nreturn x\n}}\n}}\n";
			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGVCGIF{appendage}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGVCGIF{appendage}<{cstype}>", true,
											   new CSIdentifier (val1)));


			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo[0]");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenVirtClassGetIndexerFunc{appendage}");
		}

		[Test]
		public void TestSubscriptGetBool ()
		{
			WrapGenVirtClassGetIndexerFunc ("BoolT", "bool", "true", "True\n");
			WrapGenVirtClassGetIndexerFunc ("BoolF", "bool", "false", "False\n");
		}

		[Test]
		public void TestSubscriptGetInt ()
		{
			WrapGenVirtClassGetIndexerFunc ("IntNeg", "int", "-18", "-18\n");
			WrapGenVirtClassGetIndexerFunc ("IntPos", "int", "42", "42\n");
		}

		[Test]
		public void TestSubscriptGetUInt ()
		{
			WrapGenVirtClassGetIndexerFunc ("UInt", "uint", "42", "42\n");
		}

		[Test]
		public void TestSubscriptGetFloat ()
		{
			WrapGenVirtClassGetIndexerFunc ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void TestSubscriptGetDouble ()
		{
			WrapGenVirtClassGetIndexerFunc ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void TestSubscriptGetString ()
		{
			WrapGenVirtClassGetIndexerFunc ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}

		void WrapGenVirtClassGetSetIndexerFunc (string cstype, string firstVal, string secondVal, string expected)
		{
			string swiftCode =
		$"public struct BarWGVCGSIF{cstype} {{\npublic var X:Int32 = 0;\npublic init(x:Int32) {{\n X = x;\n}}\n}}\n" +
		$"public class FooWGVCGSIF{cstype}<T> {{\nprivate var x:T\npublic init(a:T) {{ x = a\n }}\npublic subscript(int:Int) -> T\n{{\nget {{\nreturn x\n}}\n set(newValue) {{\n x=newValue\n}}\n}}\n}}\n";

			CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWGVCGSIF{cstype}", false,
										    new CSSimpleType (cstype)),
								   "foo", new CSFunctionCall ($"FooWGVCGSIF{cstype}<{cstype}>", true,
											   new CSIdentifier (firstVal)));

			CSLine assign = CSAssignment.Assign ("foo[0]", CSAssignmentOperator.Assign, new CSIdentifier (secondVal));

			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo[0]");

			CSCodeBlock callingCode = CSCodeBlock.Create (fooDecl, assign, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapGenVirtClassGetSetIndexerFunc{cstype}");
		}

		[Test]
		public void TestSubscriptGetSetBool ()
		{
			WrapGenVirtClassGetSetIndexerFunc ("bool", "false", "true", "True\n");
		}


		[Test]
		public void TestSubscriptGetSetInt ()
		{
			WrapGenVirtClassGetSetIndexerFunc ("int", "-18", "75", "75\n");
		}

		[Test]
		public void TestSubscriptGetSetUInt ()
		{
			WrapGenVirtClassGetSetIndexerFunc ("int", "18", "75", "75\n");
		}

		[Test]
		public void TestSubscriptGetSetFloat ()
		{
			WrapGenVirtClassGetSetIndexerFunc ("float", "3.14f", "42.1f", "42.1\n");
		}

		[Test]
		public void TestSubscriptGetSetDouble ()
		{
			WrapGenVirtClassGetSetIndexerFunc ("double", "3.14", "42.1", "42.1\n");
		}

		[Test]
		public void TestSubscriptGetSetString ()
		{
			WrapGenVirtClassGetSetIndexerFunc ("SwiftString", "SwiftString.FromString(\"hello\")", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}


		[Test]
		public void TestGenFuncInClass()
		{
			string swiftCode =
				"public final class Key<ValueType> {\n" +
				"    public let theKey: String\n" +
				"    public init(_ key: String) {\n" +
				"        theKey = key\n" +
				"    }\n" +
				"}\n" +
				"public final class Defaults {\n" +
				"    public init() { }\n" +
				"    public func set<ValueType>(_ value: ValueType, for key: Key<ValueType>) {\n" +
				"    }\n" +
				"}\n"
				;
			CSLine keyDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Key<SwiftString>"), "myKey",
			                                                new CSFunctionCall ("Key<SwiftString>", true,
			                                                                    new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("nothing"))));
			CSLine defDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Defaults"), "myDefaults",
			                                                new CSFunctionCall ("Defaults", true));
			CSLine callLine = CSFunctionCall.FunctionCallLine ("myDefaults.Set<SwiftString>", false,
									  new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("nothing")),
									  new CSIdentifier ("myKey"));
			CSCodeBlock callingCode = CSCodeBlock.Create (keyDecl, defDecl, callLine);

			TestRunning.TestAndExecute (swiftCode, callingCode, "");
		}


		string WrapPropertyBag (string cstype, string toAdd)
		{
			string swiftCode =
"public class PropertyBag<T>\n" +
"{\n" +
"	private var bag: [String: T]\n" +
"\n" +
"	public init()\n" +
"	{\n" +
"		bag = [String: T]()\n" +
"	}\n" +
"\n" +
"	public func add(key:String, val: T)\n" +
"	{\n" +
"		bag[key] = val\n" +
"	}\n" +
"\n" +
//"	public func get(key: String) -> T? {\n" +
//"		return bag[key]\n" +
//"   }\n" +
"\n" +
"   public func contains(key:String) -> Bool {\n" +
"		return bag[key] != nil\n" +
"	}\n" +
"	\n" +
"	public func contents() -> [(String, T)] {\n" +
"		return Array(bag.lazy)\n" +
"	}\n" +
"}\n";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {

				Utils.CompileSwift (swiftCode, provider);

				var libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");

				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = new CSFile (use, new CSNamespace [] { ns });

				CSLine fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("PropertyBag", false,
																			new CSSimpleType (cstype)),
														   "foo", new CSFunctionCall (String.Format ("PropertyBag<{0}>", cstype), true));

				CSLine add = CSFunctionCall.FunctionCallLine ("foo.Add", false,
														 new CSIdentifier ("SwiftString.FromString(\"key\")"),
														 new CSIdentifier (toAdd));

				CSCodeBlock mainBody = CSCodeBlock.Create (fooDecl, add);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
					new CSIdentifier ("Main"), new CSParameterList (new CSParameter (new CSSimpleType ("string", true), "args")),
					mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);



				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));
				var exeOutFilename = provider.UniquePath (null, "CSWrap", "exe");

				CodeWriter.WriteToFile (csOutFilename, csfile);

				Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), exeOutFilename);
				TestRunning.CopyTestReferencesTo (provider.DirectoryPath);
				return Compiler.RunWithMono (exeOutFilename, provider.DirectoryPath);
			}
		}

		[Test]
		public void TestPropBag ()
		{
			Assert.AreEqual ("", WrapPropertyBag ("nint", "5"));
		}

		[Test]
		public void TestGenericFuncInClass()
		{
			string swiftCode =
				"public final class TGFICFoo<T> {\n" +
				"    public var x:T\n" +
				"    public init(a: T) {\n" +
				"        x = a\n" +
				"    }\n" +
				"}\n" +
				"public final class TGFICBar {\n" +
				"    public init() { }\n" +
				"    public func doIt<T>(val: TGFICFoo<T>) {\n" +
				"    }\n" +
				"}\n";

			var fooDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("TGFICFoo<nint>"), "foo", new CSFunctionCall ("TGFICFoo<nint>", true, CSConstant.Val (7)));
			var barDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("TGFICBar"), "bar", new CSFunctionCall ("TGFICBar", true));
			var callIt = CSFunctionCall.FunctionCallLine ("bar.DoIt", false, new CSIdentifier("foo"));
			var callingCode = new CodeElementCollection<ICodeElement> { fooDecl, barDecl, callIt };

			TestRunning.TestAndExecute (swiftCode, callingCode, "");			                            
		}

		[Test]
		public void TestRedundantConstraintInFunc ()
		{
			string swiftCode =
				"public func getSetCount<T>(a: Set<T>) -> Int {\n" +
				"    return a.count\n" +
				"}\n";

			var setDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftSet<bool>"), "mySet", new CSFunctionCall ("SwiftSet<bool>", true));
			var popSet = CSFunctionCall.FunctionCallLine ("mySet.Insert", false, CSConstant.Val (true));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.GetSetCount", (CSIdentifier)"mySet"));
			var callingCode = new CodeElementCollection<ICodeElement> { setDecl, popSet, printIt };

			TestRunning.TestAndExecute (swiftCode, callingCode, "1\n");
		}

		[Test]
		public void TestRedundantConstraintInMethod ()
		{
			string swiftCode =
				"public class RedClass {\n" +
				"    public init () { }\n" +
				"    public func getSetCount<T>(a: Set<T>) -> Int {\n" +
				"        return a.count\n" +
				"    }\n" +
				"}\n";

			var setDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftSet<bool>"), "mySet", new CSFunctionCall ("SwiftSet<bool>", true));
			var popSet = CSFunctionCall.FunctionCallLine ("mySet.Insert", false, CSConstant.Val (true));
			var classDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("RedClass"), "cl", new CSFunctionCall ("RedClass", true));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("cl.GetSetCount", (CSIdentifier)"mySet"));
			var callingCode = new CodeElementCollection<ICodeElement> { setDecl, popSet, classDecl, printIt };

			TestRunning.TestAndExecute (swiftCode, callingCode, "1\n");
		}

		[Test]
		public void TestEmbeddedGenericSmokeTest ()
		{
			string swiftCode =
				"public func itsATuple<T, U> (a: Optional<(T, U)>) {\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("compiled"));
			var callingCode = new CodeElementCollection<ICodeElement> { printIt };

			TestRunning.TestAndExecute (swiftCode, callingCode, "compiled\n");
		}

		[Test]
		public void TestSwiftMessagesWeak ()
		{
			var swiftCode = @"
import Foundation

public class Weak<T: AnyObject> {
    public weak var value : T?
    public init(value: T?) {
        self.value = value
    }
}";
			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("compiled"));
			var callingCode = CSCodeBlock.Create (printIt);

			TestRunning.TestAndExecute (swiftCode, callingCode, "compiled\n");
}

	}
}
