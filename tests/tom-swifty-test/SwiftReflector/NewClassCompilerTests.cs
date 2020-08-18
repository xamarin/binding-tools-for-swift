// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using SwiftReflector.TypeMapping;
using tomwiftytest;


namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class NewClassCompilerTests {
		[Test]
		public void SmokeTest ()
		{
			string code = "public final class Bar { }";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (code, provider);
				Utils.CompileToCSharp (provider);
				AssertBindingsCreated (provider.DirectoryPath, "Xython");
			}
		}

		[Test]
		public void SmokeTestExecute ()
		{
			string swiftCode = "public final class Bar {\npublic func hello() {\nprint(\"hello!\");\n}\n}\n";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (swiftCode, provider);
				Utils.CompileToCSharp (provider);
			}
		}


		[Test]
		public void SmokeTestStruct ()
		{
			string code = "public struct Bar {\n public var X:Int; \n }\n";
			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (code, provider);
				Utils.CompileToCSharp (provider);
			}

		}
		[Test]
		public void SmokeTestStruct1 ()
		{
			string code = "public struct Bar {\n public var X:Int; \n public init(x:Int) {\n X = x;\n }\n}\n";
			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (code, provider);
				Utils.CompileToCSharp (provider);
			}
		}

		[Test]
		public void SmokeTestStruct2 ()
		{
			string code = "open class Foo { public init() { }\n}\n" +
				"public struct Bar {\n public var x:Foo; \n public init(y:Foo) {\n x = y;\n }\n public func getIt() -> Foo { return x;\n }\n}\n";
			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (code, provider);
				Utils.CompileToCSharp (provider);
			}
		}


		[Test]
		public void OneWithEverythingOnItPlease ()
		{
			string swiftCode = @"import Foundation;
public enum Bread {
    case Wheat
    case White
}

public protocol Food {
    func AddOnions ()
}

public struct Bun
{
    var MadeOf : Bread;
    var Length : Int;
}

public class HotDog : Food
{
    public init (k : Int, b : Bun)
    {
        Ketchup = k;
    }
    
    public var Ketchup: Int;
    
    public func AddOnions () {}
}

public extension Int
{
    public func Price () -> Double { return 2.99 }
}

public func Eat () { }
";
			var print = CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)"42");
			var callingCode = CSCodeBlock.Create (print);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42", platform: PlatformName.macOS);
		}

		void TestCtor (string testName, string type, string value, string output = null)
		{
			string className = $"{testName}Bar";
			output = output ?? value;
			string swiftCode = String.Format ("public struct {0} {{\n public var X:{1}; \n public init(x:{1}) {{\n X = x;\n }}\n}}\n",
							 className, type);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType (className), "bar", new CSFunctionCall (className, true, new CSIdentifier (value))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar").Dot (new CSIdentifier ("X"))));

			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: testName);
		}

		[Test]
		public void TestMutatingMethod ()
		{
			string swiftCode =
				"public struct Bar {\n" +
				"   private var x:Int = 0\n" +
				"   public init() { }\n" +
				"   public mutating func up() {\n" +
				"       x = x + 1;\n" +
				"   }\n" +
				"   public func getX() -> Int {\n" +
				"       return x\n" +
				"   }\n" +
				"}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Bar"), "bar", new CSFunctionCall ("Bar", true)));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("bar.Up", false));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false,
														  new CSFunctionCall ("bar.GetX", false)));

			TestRunning.TestAndExecute (swiftCode, callingCode, "1");

		}

		[Test]
		public void TestEmbeddedProtocol ()
		{
			string swiftCode =
		TestRunningCodeGenerator.kSwiftFileWriter +
		"public protocol Proto {\n func doIt()\n }\n" +
			    "public struct BarTEP {\n public var X:Proto;\npublic var Y:Int32;\npublic init(x:Proto, y:Int32) {\nX=x;\nY=y\n}\n}\n" +
			   "public class Thing : Proto {\n public init() { }\npublic func doIt() {\nvar s = \"\"\nprint(\"hi mom\", to:&s)\nwriteToFile(s, \"TestEmbeddedProtocol\")\n}\n}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Thing"), "thing", new CSFunctionCall ("Thing", true)));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("BarTEP"), "bar", new CSFunctionCall ("BarTEP", true, new CSIdentifier ("thing"), CSConstant.Val (42))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("bar.X.DoIt", false));
			callingCode.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"bar.Y"));


			TestRunning.TestAndExecute (swiftCode, callingCode, "42\nhi mom\n", platform: PlatformName.macOS);
		}

		[Test]
		public void StructWithInt ()
		{
			TestCtor ("StructWithInt", "Int", "42");
		}

		[Test]
		public void StructWithUInt ()
		{
			TestCtor ("StructWithUInt", "UInt", "42");
		}

		[Test]
		public void StructWithBool ()
		{
			TestCtor ("StructWithBool", "Bool", "true", "True");
		}

		[Test]
		public void StructWithFloat ()
		{
			TestCtor ("StructWithFloat", "Float", "42");
		}

		[Test]
		public void StructWithDouble ()
		{
			TestCtor ("StructWithDouble", "Double", "42.1");
		}

		[Test]
		public void StructWithString ()
		{
			TestCtor ("StructWithUInt", "String", "SwiftString.FromString(\"word\")", "word");
		}


		[Test]
		public void InitSimpleClass ()
		{
			string swiftCode = "public struct BarISC {\n public var X:Int; \n public init(x:Int) {\n X = x;\n }\n}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("BarISC"), "bar", new CSFunctionCall ("BarISC", true, new CSIdentifier ("3"))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar").Dot (new CSIdentifier ("X"))));
			TestRunning.TestAndExecute (swiftCode, callingCode, "3");
		}

		void TestMethod (string testName, string type, string value, string valueToAdd, string output)
		{
			output = output ?? value;
			string swiftCode = String.Format ("public struct Bar{1} {{\n public var X:{0}; \n public init(x:{0}) {{\n X = x;\n }}\n public func add(x:{0}) -> {0} {{ return X + x;\n}}\n }}\n", type,
							testName);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Bar" + testName), "bar", new CSFunctionCall ("Bar" + testName, true, new CSIdentifier (value))));
			CSFunctionCall call = new CSFunctionCall ("bar.Add", false, new CSIdentifier (valueToAdd));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, call));
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : testName);
		}
	
		[Test]
		public void StructMethodInt ()
		{
			TestMethod ("StructMethodInt", "Int", "3", "1", "4");
		}

		[Test]
		public void StructMethodUInt ()
		{
			TestMethod ("StructMethodUInt", "UInt", "3", "1", "4");
		}

		[Test]
		public void StructMethodFloat ()
		{
			TestMethod ("StructMethodFloat", "Float", "3", "1", "4");
		}

		[Test]
		public void StructMethodDouble ()
		{
			TestMethod ("StructMethodDouble", "Double", "3", "1", "4");
		}

		[Test]
		public void StructMethodString ()
		{
			TestMethod ("StructMethodString", "String", "SwiftString.FromString(\"word\")", "SwiftString.FromString(\" up\")", "word up");
		}

		[Test]
		public void SmokeTestClassInStruct ()
		{
			string code = "public final class Bar { }\n public struct Foo { public var X: Bar;\npublic init(x:Bar) { X = x; }\n }";

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (code, provider);
				Utils.CompileToCSharp (provider);
			}
		}

		void TestPropSet (string testName, string type, string value, string valueToSet, string output)
		{
			output = output ?? value;
			string swiftCode = String.Format ("public struct Bar{1} {{\n public var X:{0}; \n public init(x:{0}) {{\n X = x;\n }}\n}}\n", type,
							type);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Bar" + type), "bar", new CSFunctionCall ("Bar" + type, true, new CSIdentifier (value))));
			CSLine assign = CSAssignment.Assign ("bar.X", new CSIdentifier (valueToSet));
			callingCode.Add (assign);
			callingCode.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar.X")));
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : testName);
		}

		[Test]
		public void PropSetInt ()
		{
			TestPropSet ("PropSetInt", "Int", "3", "5", "5");
		}

		[Test]
		public void PropSetFloat ()
		{
			TestPropSet ("PropSetFloat", "Float", "3.1f", "4.1f", "4.1");
		}

		[Test]
		public void PropSetDouble ()
		{
			TestPropSet ("PropSetDouble", "Double", "3.1", "4.1", "4.1");
		}

		[Test]
		public void PropSetBool ()
		{
			TestPropSet ("PropSetBool", "Bool", "false", "true", "True");
		}

		[Test]
		public void PropSetString ()
		{
			TestPropSet ("PropSetString", "String", "SwiftString.FromString(\"abc\")", "SwiftString.FromString(\"def\")", "def");
		}

		void TestGlobalFunc (string testName, string type, string value, string toAdd, string output)
		{
			string swiftCode = String.Format ("public func callMe{2}(x:{0}) -> {0} {{ return {1} + x; }}",
							       type, value, type);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
					    new CSFunctionCall ("TopLevelEntities.CallMe" + type, false, new CSIdentifier (toAdd)));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : testName);
		}

		[Test]
		public void TestGlobalFuncInt ()
		{
			TestGlobalFunc ("TestGlobalFuncInt", "Int", "3", "4", "7");
		}

		[Test]
		public void TestGlobalFuncFloat ()
		{
			TestGlobalFunc ("TestGlobalFuncFloat", "Float", "3", "4", "7");
		}

		[Test]
		public void TestGlobalFuncDouble ()
		{
			TestGlobalFunc ("TestGlobalFuncDouble", "Double", "3", "4", "7");
		}

		[Test]
		public void TestGlobalFuncUInt ()
		{
			TestGlobalFunc ("TestGlobalFuncUInt", "UInt", "3", "4", "7");
		}

		[Test]
		public void TestGlobalFuncString ()
		{
			TestGlobalFunc ("TestGlobalFuncString", "String", "\"word\"", "SwiftString.FromString(\" up\")", "word up");
		}

		[TestCase ("TestGlobalAnonFuncInt", "Int", "3", null, "3\n")]
		[TestCase ("TestGlobalAnonFuncUInt", "UInt", "3", null, "3\n")]
		[TestCase ("TestGlobalAnonFuncFloat", "Float", "34.2", "34.2f", "34.2\n")]
		[TestCase ("TestGlobalAnonFuncDouble", "Double", "34.2", null, "34.2\n")]
		[TestCase ("TestGlobalAnonFuncBool", "Bool", "true", null, "True\n")]
		[TestCase ("TestGlobalAnonFuncString", "String", "\"hi mom\"", "SwiftString.FromString(\"nothing\")", "hi mom\n")]
		public void TestGlobalAnonFunc (string testName, string type, string value, string csValue, string output)
		{
			var swiftCode = $"public func callMeAnon{type} (_ : {type}) -> {type} {{\n return {value}\n}}";
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"TopLevelEntities.CallMeAnon{type}", false, new CSIdentifier (csValue ?? value)));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: testName);
		}

		void TestGlobalPropGet (string type, string initValue, string output)
		{
			string testName = "TestGlobalPropGet" + type;
			output = output ?? initValue;
			string swiftCode = String.Format ("public var aGlobal{2} : {0} = {1}", type, initValue, type);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("TopLevelEntities.AGlobal" + type));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : testName);
		}

		[Test]
		public void TestGlocalPropGetBool ()
		{
			TestGlobalPropGet ("Bool", "true", "True");
		}

		[Test]
		public void TestGlocalPropGetInt ()
		{
			TestGlobalPropGet ("Int", "3", "3");
		}

		[Test]
		public void TestGlocalPropGetUInt ()
		{
			TestGlobalPropGet ("UInt", "3", "3");
		}

		[Test]
		public void TestGlocalPropGetFloat ()
		{
			TestGlobalPropGet ("Float", "42", "42");
		}

		[Test]
		public void TestGlocalPropGetDouble ()
		{
			TestGlobalPropGet ("Double", "42", "42");
		}

		[Test]
		public void TestGlocalPropGetString ()
		{
			TestGlobalPropGet ("String", "\"word\"", "word");
		}

		void TestIndexedPropGet (string type, string initValue, string output)
		{
			string testName = "TestIndexedPropGet" + type;
			output = output ?? initValue;
			string swiftCode = String.Format ("public final class Bar{2} {{\n public init() {{}}\nprivate var arr:[{0}] = Array(repeating:{1}, count:3)\npublic subscript(i:Int) -> {0} {{\nget{{ return arr[i]\n}}\nset (value) {{ arr[i] = value\n}}\n}}\n}}\n",
							  type, initValue, testName);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Bar" + testName), "bar", new CSFunctionCall ("Bar" + testName, true));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar[0]"));
			callingCode.Add (classDecl);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : testName);
		}

		[Test]
		public void TestIndexedPropGetInt ()
		{
			TestIndexedPropGet ("Int", "4", "4");
		}

		[Test]
		public void TestIndexedPropGetUInt ()
		{
			TestIndexedPropGet ("UInt", "4", "4");
		}

		[Test]
		public void TestIndexedPropGetFloat ()
		{
			TestIndexedPropGet ("Float", "4", "4");
		}

		[Test]
		public void TestIndexedPropGetDouble ()
		{
			TestIndexedPropGet ("Double", "4", "4");
		}

		[Test]
		public void TestIndexedPropGetBool ()
		{
			TestIndexedPropGet ("Bool", "true", "True");
		}

		[Test]
		public void TestIndexedPropGetString ()
		{
			TestIndexedPropGet ("String", "\"word\"", "word");
		}

		void TestIndexedPropSet (string type, string initValue, string setValue, string output)
		{
			string testName = "TestIndexedPropSet" + type;
			output = output ?? initValue;
			string swiftCode = String.Format ("public final class Bar{2} {{\n public init() {{ }}\n private var arr:[{0}] = Array(repeating:{1}, count:3)\npublic subscript(i:Int) -> {0} {{\nget{{ return arr[i]\n}}\nset (value) {{ arr[i] = value\n}}\n}}\n}}\n",
					    type, initValue, testName);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("Bar" + testName), "bar", new CSFunctionCall ("Bar" + testName, true));
			CSLine setCall = CSAssignment.Assign ("bar[0]", new CSIdentifier (setValue));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar[0]"));
			callingCode.Add (classDecl);
			callingCode.Add (setCall);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : testName);
		}

		[Test]
		public void TestIndexedPropSetInt ()
		{
			TestIndexedPropSet ("Int", "4", "2", "2");
		}

		[Test]
		public void TestIndexedPropSetUnt ()
		{
			TestIndexedPropSet ("UInt", "4", "2", "2");
		}

		[Test]
		public void TestIndexedPropSetFloat ()
		{
			TestIndexedPropSet ("Float", "4", "2", "2");
		}

		[Test]
		public void TestIndexedPropSetDouble ()
		{
			TestIndexedPropSet ("Double", "4", "2", "2");
		}

		[Test]
		public void TestIndexedPropSetBool ()
		{
			TestIndexedPropSet ("Bool", "false", "true", "True");
		}

		[Test]
		public void TestIndexedPropSetString ()
		{
			TestIndexedPropSet ("String", "\"word\"", "SwiftString.FromString(\"up\")", "up");
		}

		[Test]
		public void TestIndexedOnClassPropGet ()
		{
			string swiftCode = "public final class FooTIOCPG {\npublic var X:Int;\npublic init(x:Int) {\n X = x; }\n }\n" +
		"public final class BarTIOCPG {\n public init() { }\nprivate var arr:[Int] = [1,2,3]\npublic subscript(i:FooTIOCPG) -> Int {\nget { return arr[i.X]\n}\nset (value) { arr[i.X] = value\n}\n}\n}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType ("FooTIOCPG"), "foo", new CSFunctionCall ("FooTIOCPG", true, CSConstant.Val (1L)));
			CSLine classDecl1 = CSVariableDeclaration.VarLine (new CSSimpleType ("BarTIOCPG"), "bar", new CSFunctionCall ("BarTIOCPG", true));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar[foo]"));
			callingCode.Add (classDecl0);
			callingCode.Add (classDecl1);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "2");
		}

		[Test]
		public void TestIndexedOnStructPropGet ()
		{
			string swiftCode = "public struct FooTIOSPG {\npublic var X:Int;\npublic init(x:Int) {\n X = x; }\n }\n" +
		"public final class BarTIOSPG {\n public init(){ }\n private var arr:[Int] = [1,2,3]\npublic subscript(i:FooTIOSPG) -> Int {\nget { return arr[i.X]\n}\nset (value) { arr[i.X] = value\n}\n}\n}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType ("FooTIOSPG"), "foo", new CSFunctionCall ("FooTIOSPG", true, CSConstant.Val (1L)));
			CSLine classDecl1 = CSVariableDeclaration.VarLine (new CSSimpleType ("BarTIOSPG"), "bar", new CSFunctionCall ("BarTIOSPG", true));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("bar[foo]"));
			callingCode.Add (classDecl0);
			callingCode.Add (classDecl1);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "2");
		}

		void EnumSmokeTestImpl (string testName, string enumToPrint, string expected)
		{
			string swiftCode = $"import Foundation\n @objc public enum {testName} : Int {{\ncase a, b, c\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType (testName), "foo", new CSIdentifier (enumToPrint));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("(int)foo"));
			callingCode.Add (classDecl0);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : testName);
		}

		[Test]
		public void EnumSmokeTest0 ()
		{
			EnumSmokeTestImpl ("FooA", "FooA.A", "0");
			EnumSmokeTestImpl ("FooB", "FooB.B", "1");
			EnumSmokeTestImpl ("FooC", "FooC.C", "2");
		}

		void EnumSmokeTest1RawValueImpl (string typeName, string enumToPrint, string expected)
		{
			string swiftCode = $"import Foundation\n @objc public enum {typeName} : Int {{\ncase a=2, b, c\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType (typeName), "foo", new CSIdentifier (enumToPrint));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("(int)foo"));
			callingCode.Add (classDecl0);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"EnumSmokeTest1RawValueImpl{typeName}");
		}

		[Test]
		public void EnumSmokeTest1 ()
		{
			EnumSmokeTest1RawValueImpl ("FooAST1", "FooAST1.A", "2");
			EnumSmokeTest1RawValueImpl ("FooBST1", "FooBST1.B", "3");
			EnumSmokeTest1RawValueImpl ("FooCST1", "FooCST1.C", "4");
		}

		void EnumSmokeTest2RawValueImpl (string typeName, string enumToPrint, string expected)
		{
			string swiftCode = $"import Foundation\n @objc public enum {typeName} : Int {{\ncase a=2, b=17, c=34\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType (typeName), "foo", new CSIdentifier (enumToPrint));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("(int)foo"));
			callingCode.Add (classDecl0);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"EnumSmokeTest2RawValueImpl{typeName}");
		}

		[Test]
		public void EnumSmokeTest2 ()
		{
			EnumSmokeTest2RawValueImpl ("FooAST2", "FooAST2.A", "2");
			EnumSmokeTest2RawValueImpl ("FooBST2", "FooBST2.B", "17");
			EnumSmokeTest2RawValueImpl ("FooCST2", "FooCST2.C", "34");
		}

		void EnumSmokeTest3RawValueImpl (string typeName, string enumToPrint, string expected)
		{
			string swiftCode = $"import Foundation\n @objc public enum {typeName} : Int {{\ncase a=2, b=17, c=34\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType (typeName), "foo", new CSIdentifier (enumToPrint));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("foo.RawValue()"));
			callingCode.Add (classDecl0);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"EnumSmokeTest3RawValueImpl{typeName}");
		}

		[Test]
		public void EnumSmokeTest3 ()
		{
			EnumSmokeTest3RawValueImpl ("FooAST3", "FooAST3.A", "2");
			EnumSmokeTest3RawValueImpl ("FooBST3", "FooBST3.B", "17");
			EnumSmokeTest3RawValueImpl ("FooCST3", "FooCST3.C", "34");
		}

		void EnumSmokeTest4RawValueImpl (string typeName, string enumToPrint, string expected)
		{
			string swiftCode = $"import Foundation\n public enum {typeName} : Int {{\ncase a=2, b=17, c=34\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine classDecl0 = CSVariableDeclaration.VarLine (new CSSimpleType (typeName), "foo", new CSIdentifier (enumToPrint));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("foo.RawValue()"));
			callingCode.Add (classDecl0);
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"EnumSmokeTest4RawValueImpl{typeName}");
		}

		[Test]
		public void EnumSmokeTest4 ()
		{
			EnumSmokeTest4RawValueImpl ("FooAST4", "FooAST4.A", "2");
			EnumSmokeTest4RawValueImpl ("FooBST4", "FooBST4.B", "17");
			EnumSmokeTest4RawValueImpl ("FooCST4", "FooCST4.C", "34");
		}

		//[Test]
		//public void TrivialObjcEnumHasNoAttr()
		//{
		//	string swiftCode = "import Foundation\n @objc public enum FooTOEHNA : Int {\ncase a=2, b=17, c=34\n}\n";
		//	CodeElemCollection<ICodeElem> callingCode = new CodeElemCollection<ICodeElem> ();
		//	Line call = FunctionCall.FunctionCallLine ("Console.Write", false, new FunctionCall ("SwiftEnumMapper.EnumHasRawValue",
		//                                                                                               false, new Identifier ("typeof(FooTOEHNA)")));
		//	callingCode.Add (call);
		//          TestRunning.TestAndExecute(swiftCode, callingCode, "False", "TrivialObjcEnumHasNoAttr", "NewClassCompilerTests", "TomTestTrivialObjcEnumHasNoAttr");
		//}

		//[Test]
		//public void TrivialEnumHasAttr()
		//{
		//	string swiftCode = "import Foundation\n public enum FooTEHA : Int {\ncase a=2, b=17, c=34\n}\n";
		//	CodeElemCollection<ICodeElem> callingCode = new CodeElemCollection<ICodeElem> ();
		//	Line call = FunctionCall.FunctionCallLine ("Console.Write", false, new FunctionCall ("SwiftEnumMapper.EnumHasRawValue",
		//                                                                                               false, new Identifier ("typeof(FooTEHA)")));
		//	callingCode.Add (call);
		//          TestRunning.TestAndExecute(swiftCode, callingCode, "True", "TrivialEnumHasAttr", "NewClassCompilerTests", "TomTestTrivialEnumHasAttr");
		//}

		[Test]
		public void EnumSmokeTest5 ()
		{
			string swiftCode = "public final class BarEST5 { init() { } }\n public enum FooEST5 {\n case a(BarEST5)\n case b(BarEST5)\n}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val ("Hi"));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Hi");
		}

		public void EnumIsRightTypeImpl (string enumName)
		{
			string swiftCode = $"public final class BarEIRTI{enumName} {{ public init() {{ }} }}\n public enum FooEIRTI{enumName} {{\n case a(BarEIRTI{enumName})\n case b(BarEIRTI{enumName})\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine makeBar = CSAssignment.Assign ($"BarEIRTI{enumName} b", new CSFunctionCall ($"BarEIRTI{enumName}", true));
			callingCode.Add (makeBar);
			CSLine makeFoo = CSAssignment.Assign ($"FooEIRTI{enumName} f", new CSFunctionCall ($"FooEIRTI{enumName}.New" + enumName, false, new CSIdentifier ("b")));
			callingCode.Add (makeFoo);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("f.Case"));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, enumName, testName: $"EnumIsRightType{enumName}");
		}

		[Test]
		public void EnumIsRightType ()
		{
			EnumIsRightTypeImpl ("A");
			EnumIsRightTypeImpl ("B");
		}

		public void EnumCompoundTypesImpl (string enumCase, string value, string expected)
		{
			string swiftCode = $"public enum FooECTI{enumCase} {{\n case a(Int)\n case b(Double)\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine makeFoo = CSAssignment.Assign ($"FooECTI{enumCase} f", new CSFunctionCall ($"FooECTI{enumCase}.New{enumCase}", false, new CSIdentifier (value)));
			callingCode.Add (makeFoo);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("f.Value" + enumCase));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"EnumCompoundType{enumCase}");
		}

		[Test]
		public void EnumCompoundTypes ()
		{
			EnumCompoundTypesImpl ("A", "42", "42");
			EnumCompoundTypesImpl ("B", "37.5", "37.5");
		}

		public void EnumCompoundTypesNoBImpl (string enumCase, string value, string expected)
		{
			string swiftCode = $"public enum FooECTNBI{enumCase} {{\n case a(Int)\n case b\n case c(Double)\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine makeFoo = CSAssignment.Assign ($"FooECTNBI{enumCase} f", new CSFunctionCall ($"FooECTNBI{enumCase}.New" + enumCase, false, new CSIdentifier (value)));
			callingCode.Add (makeFoo);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("f.Value" + enumCase));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"EnumCompoundBlankTypeCase{enumCase}");
		}

		[Test]
		public void EnumCompoundTypesBlankCase ()
		{
			EnumCompoundTypesNoBImpl ("A", "42", "42");
			EnumCompoundTypesNoBImpl ("C", "37.5", "37.5");
		}

		[Test]
		public void EnumRoundTripTest ()
		{
			string swiftCode = "public enum FooERTT {\n case a(Int)\n case b\n case c(Double)\n}\n" +
		"public final class BarERTT {\npublic init() { }\n public func ident(arg:FooERTT) -> FooERTT {\nreturn arg;\n}\n}\n";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine makeFoo = CSAssignment.Assign ("FooERTT f", new CSFunctionCall ("FooERTT.NewC", false, CSConstant.Val (3.1)));
			callingCode.Add (makeFoo);
			CSLine makeBar = CSAssignment.Assign ("BarERTT b", new CSFunctionCall ("BarERTT", true));
			callingCode.Add (makeBar);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
													  new CSFunctionCall ("b.Ident", false, new CSIdentifier ("f")).Dot (new CSIdentifier ("ValueC")));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3.1");
		}


		[Test]
		public void EnumHonorsLabel ()
		{
			string swiftCode = @"
public enum UselessEnum {
case something(x: Int)
case nothing
}
";
			var enID = new CSIdentifier ("useless");
			var enDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, enID, new CSFunctionCall ("UselessEnum.NewSomething", false, CSConstant.Val (17)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{enID.Name}.Case"));
			var callingCode = new CodeElementCollection<ICodeElement> () { enDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "Something\n");
		}

		[Test]
		public void EnumPrivateAccessor ()
		{
			var swiftCode = @"
public enum PrivAccess {
case iVal(x: Int)
case fVal(y: Float)
}
";
			var miID = new CSIdentifier ("mi");
			var miDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, miID, new CSFunctionCall ("typeof(PrivAccess).GetMethod", false, CSConstant.Val ("__GetValueIVal"),
				new CSIdentifier ("System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance")));
			var printer = CSFunctionCall.ConsoleWriteLine (miID.Dot (new CSIdentifier ("IsPublic")));
			var callingCode = new CodeElementCollection<ICodeElement> () { miDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}


		[Test]
		public void TopLevelTuple ()
		{
			string swiftCode = "public func weightAndSize() -> (Float, Int)\n{\n\treturn (3.1, 4)\n}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine tupleBuilt = CSAssignment.Assign ("Tuple<float, nint> val", new CSFunctionCall ("TopLevelEntities.WeightAndSize", false));
			callingCode.Add (tupleBuilt);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("val"));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "(3.1, 4)");
		}



		[Test]
		public void StaticVarTest ()
		{
			string swiftCode = "public final class AFinalClassSVT {\n\tpublic static var aStaticProp: Bool = true\n}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("AFinalClassSVT.AStaticProp"));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "True");
		}


		[Test]
		public void IndexerTest ()
		{
			string swiftCode = "public final class AFinalClassIT {\npublic init() { }\n\tprivate static var _names:[String] = [ \"one\", \"two\", \"three\" ]\n" +
		"public subscript(i:Int) -> String { return AFinalClassIT._names[i]; }\n}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("new AFinalClassIT()[0]"));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "one");
		}

		[Test]
		public void SwiftKeywordForParameterName ()
		{
			string swiftCode = "public func keywordFunc(for name: String) -> String {\n" +
				"    return name\n" +
				"}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
								       new CSFunctionCall ("TopLevelEntities.KeywordFunc", false,
											  new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("nothing"))));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "nothing");
		}


		[Test]
		public void SwiftEmptyConstructorName ()
		{
			string swiftCode =
				"public final class Key<ValueType> {\n" +
				"    public let thing:String\n" +
				"    public init(_ key: String) {\n" +
				"        self.thing = key\n" +
				"    }\n" +
				"}\n";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("Key<nint>"), "aThing",
								     new CSFunctionCall ("Key<nint>", true,
											 new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("nothing"))));
			CSLine printer = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("aThing.Thing"));
			callingCode.Add (decl);
			callingCode.Add (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "nothing");
		}

		[Test]
		public void SwiftEmptyParameterInFunction ()
		{
			string swiftCode =
				"public func emptyParamFunc(_ val: Int) -> Int {\n" +
				"    return val\n" +
				"}\n";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();

			CSLine printer = CSFunctionCall.FunctionCallLine ("Console.Write", false,
									 new CSFunctionCall ("TopLevelEntities.EmptyParamFunc", false,
											    CSConstant.Val (17)));
			callingCode.Add (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17");
		}

		[Test]
		[RunWithLeaks]
		public void EnumHasProperty ()
		{
			string swiftCode =
				"public enum Parameter {\n" +
				"    case required(String)\n" +
				"    case optional(String)\n" +
				"    public var formattedValue: String {\n" +
				"        switch self {\n" +
				"        case .required (let value):\n" +
				"            return \"<\\(value)>\"\n" +
				"        case .optional (let value):\n" +
				"            return \"[<\\(value)>]\"\n" +
				"        }\n" +
				"    }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var makeFoo = CSAssignment.Assign ("Parameter pr", new CSFunctionCall ("Parameter.NewRequired", false, new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("nothing"))));
			callingCode.Add (makeFoo);
			var printit = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"pr.FormattedValue");
			callingCode.Add (printit);
			TestRunning.TestAndExecute (swiftCode, callingCode, "<nothing>\n");
		}

		[Test]
		public void EnumHasInternalProperty ()
		{
			string swiftCode = @"
public enum Position {

    case top, bottom

    var opposite: Position {
        switch self {
        case .top:
            return .bottom
        case .bottom:
            return .top
        }
    }

}";

			var enumID = new CSIdentifier ("pos");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, enumID, new CSIdentifier ("Position.Top"));
			var printer = CSFunctionCall.ConsoleWriteLine (enumID);
			var callingCode = new CodeElementCollection<ICodeElement> () { decl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "Top\n");
		}

		[Test]
		public void TestMissingUsingOnStruct ()
		{
			string swiftCode =
				"public struct testLib {\n" +
				" public init () { }\n" +
				" public var text = \"Hello, World!\"\n" +
				"}\n" +
				"public func sayHello() -> testLib {\n" +
				"   return testLib();\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var makeFoo = CSAssignment.Assign ("TestLib foo", new CSFunctionCall ("TopLevelEntities.SayHello", false));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("foo").Dot (CSIdentifier.Create ("Text")));
			callingCode.Add (makeFoo);
			callingCode.Add (printIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Hello, World!\n");
		}

		[Test]
		public void TestPropMatchesClassName ()
		{
			string swiftCode =
				"public class KeyPropMatchesClassName {\n" +
				"    public init() { } \n" +
				"    public var keyPropMatchesClassName: String = \"keyPropMatchesClassName\"\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var makeKey = CSAssignment.Assign ("KeyPropMatchesClassName key", new CSFunctionCall ("KeyPropMatchesClassName", true));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("key").Dot (CSIdentifier.Create ("KeyPropMatchesClassName0")));
			callingCode.Add (makeKey);
			callingCode.Add (printIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "keyPropMatchesClassName\n");
		}

		[Test]
		public void TestPropMatchesClassEnumName ()
		{
			string swiftCode =
				"public enum RocksPropMatchesClassEnumName {\n" +
				"    case igneous, sedimentary, metamorphic\n" +
				"}\n" +
				"public class KeyPropMatchesClassEnumName {\n" +
				"    public init() { } \n" +
				"    public var keyPropMatchesClassEnumName: RocksPropMatchesClassEnumName = .igneous\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var makeKey = CSAssignment.Assign ("KeyPropMatchesClassEnumName key", new CSFunctionCall ("KeyPropMatchesClassEnumName", true));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("key").Dot (CSIdentifier.Create ("KeyPropMatchesClassEnumName0")));
			callingCode.Add (makeKey);
			callingCode.Add (printIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Igneous\n");
		}

		[Test]
		public void TestPropMatchesTrivialEnumName ()
		{
			string swiftCode =
				"public enum RocksPropMatchesTrivialEnumName {\n" +
				"    case igneous, sedimentary, metamorphic\n" +
				"    public var RocksPropMatchesTrivialEnumName:Int { return 5; }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("RocksPropMatchesTrivialEnumName.Igneous.RocksPropMatchesTrivialEnumName"));
			callingCode.Add (printIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "5\n");
		}

		[Test]
		public void TestSimpleOptionalCtor ()
		{
			string swiftCode =
				"public class FirstOptCtor {\n" +
				"    public init?(fail: Bool) {\n" +
				"        if fail {\n" +
				"            return nil\n" +
				"        }\n" +
				"    }\n" +
				"}\n";

			var varDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional<FirstOptCtor>"), "optVal",
								    new CSFunctionCall ("FirstOptCtor.FirstOptCtorOptional", false, CSConstant.Val (true)));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("optVal").Dot (CSIdentifier.Create ("HasValue")));
			var callingCode = new CodeElementCollection<ICodeElement> {
				varDecl, printIt
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}

		[Test]
		public void TestSimpleStructOptionalCtor ()
		{
			string swiftCode =
				"public struct FirstOptStructCtor {\n" +
				"    public init?(fail: Bool) {\n" +
				"        if fail {\n" +
				"            return nil\n" +
				"        }\n" +
				"    }\n" +
				"}\n";

			var varDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional<FirstOptStructCtor>"), "optVal",
			                                             new CSFunctionCall ("FirstOptStructCtor.FirstOptStructCtorOptional", false, CSConstant.Val (true)));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("optVal").Dot (CSIdentifier.Create ("HasValue")));
			var callingCode = new CodeElementCollection<ICodeElement> {
				varDecl, printIt
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}

		[TestCase ("IntPos", "Int", "42", "42\n")]
		[TestCase ("IntNeg", "Int", "-42", "-42\n")]
		[TestCase ("UInt", "UInt", "42", "42\n")]
		[TestCase ("BoolTrue", "Bool", "true", "True\n")]
		[TestCase ("BoolFalse", "Bool", "false", "False\n")]
		[TestCase ("Float", "Float", "42.1", "42.1\n")]
		[TestCase ("Double", "Double", "42.1", "42.1\n")]
		[TestCase ("String", "String", "\"nothing\"", "nothing\n")]
		public void PublicClassOpenClassMethod (string testCase, string type, string value, string expected)
		{
			string SwiftCode =
				$"public class PublicClassOpenClassMethod{testCase} {{\n" +
				$"    open class func thing() -> {type} {{\n" +
				$"        return {value}\n" +
				"    }\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"PublicClassOpenClassMethod{testCase}.Thing"));
			var callingCode = new CodeElementCollection<ICodeElement> { printIt };
			TestRunning.TestAndExecute (SwiftCode, callingCode, expected, testName : $"OpenClassMethod{testCase}");
		}

		[TestCase ("IntPos", "Int", "42", "42\n")]
		[TestCase ("IntNeg", "Int", "-42", "-42\n")]
		[TestCase ("UInt", "UInt", "42", "42\n")]
		[TestCase ("BoolTrue", "Bool", "true", "True\n")]
		[TestCase ("BoolFalse", "Bool", "false", "False\n")]
		[TestCase ("Float", "Float", "42.1", "42.1\n")]
		[TestCase ("Double", "Double", "42.1", "42.1\n")]
		[TestCase ("String", "String", "\"nothing\"", "nothing\n")]
		public void OpenClassOpenClassMethod (string testCase, string type, string value, string expected)
		{
			string SwiftCode =
				$"open class OpenClassOpenClassMethod{testCase} {{\n" +
				$"    open class func thing() -> {type} {{\n" +
				$"        return {value}\n" +
				"    }\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"OpenClassOpenClassMethod{testCase}.Thing"));
			var callingCode = new CodeElementCollection<ICodeElement> { printIt };
			TestRunning.TestAndExecute (SwiftCode, callingCode, expected, $"OpenClassOpenClassMethod{testCase}");
		}

		[Test]
		public void TopLevelProp ()
		{
			string code = "public var Answer: Int { get { return 42 } }";
			using (TempDirectoryFilenameProvider libProvider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (code, libProvider);
				Utils.CompileToCSharp (libProvider);
			}
		}

		[TestCase ("public final class Bar { public init () {}; public func GetAnswer () -> Int { return 42 } }", "Xython.Bar", "Xython", "Bar")]
		[TestCase ("public struct Str { public init () {}; public func GetAnswer () -> Int { return 42 } }", "Xython.Str", "Xython", "Str")]
		[TestCase ("public enum Direction { case north; case south; case east; case west; }", "Xython.Direction", "Xython", "Direction")]
		//Both of these fail due to empty XML - https://github.com/xamarin/maccore/issues/943
		//[TestCase ("public func GetAnswer () -> Int { return 42 }", "Xython.GetAnswer", "Xython", "GetAnswer")]
		//[TestCase ("public var Answer: Int = 42", "Xython.Answer", "Xython", "Answer")]
		public void ImportBindingSmokeTest (string swiftCode, string swiftName, string ns, string csharpName)
		{
			using (TempDirectoryFilenameProvider libProvider = new TempDirectoryFilenameProvider ()) {
				// First compile the example code
				{
					Utils.CompileSwift (swiftCode, libProvider);
					Utils.CompileToCSharp (libProvider);
					AssertBindingsCreated (libProvider.DirectoryPath, "Xython");
				}

				// Now read out the type database XML and verify
				{
					List<string> typeDatabasePaths = new List<string> { Path.Combine (libProvider.DirectoryPath, "bindings") };
					TypeMapper typeMapper = new TypeMapper (typeDatabasePaths, UnicodeMapper.Default);
					var entity = typeMapper.TypeDatabase.EntityForSwiftName (swiftName);
					Assert.AreEqual (ns, entity.SharpNamespace);
					Assert.AreEqual (csharpName, entity.SharpTypeName);
				}
			}
		}

		[Test]
		public void MultipleImports ()
		{
			string libCode = "public final class Bar { public init () {}; public func DoIt () -> Bar { return self; } }";
			string consumerCode = "import Lib\npublic final class BarUser { public func Main () -> Bar { return (Bar ()).DoIt() } }";

			using (TempDirectoryFilenameProvider libProvider = new TempDirectoryFilenameProvider ()) {
				// First compile the library with the base types we need to import
				{
					Utils.CompileSwift (libCode, libProvider, moduleName: "Lib");
					Utils.CompileToCSharp (libProvider, moduleName: "Lib");
					AssertBindingsCreated (libProvider.DirectoryPath, "Lib");
				}

				// Now compile the consumer in a new directory and import from Lib
				using (TempDirectoryFilenameProvider consumerProvider = new TempDirectoryFilenameProvider ()) {
					CustomSwiftCompiler consumerCompiler = Utils.DefaultSwiftCompiler (consumerProvider);
					SwiftCompilerOptions options = new SwiftCompilerOptions ("Consumer", new string [] { consumerCompiler.DirectoryPath, libProvider.DirectoryPath }, new string [] { libProvider.DirectoryPath }, new string [] { "Lib" });

					consumerCompiler.CompileString (options, consumerCode);

					NewClassCompiler ncc = Utils.DefaultCSharpCompiler ();

					var searchPath = new List<string> { consumerCompiler.DirectoryPath, libProvider.DirectoryPath, Compiler.kSwiftRuntimeGlueDirectory };
					List<String> typeDataBasePaths = new List<String> { Path.Combine (libProvider.DirectoryPath, "bindings/Lib") };
					typeDataBasePaths.AddRange (Compiler.kTypeDatabases);

					ClassCompilerLocations classCompilerLocations = new ClassCompilerLocations (searchPath, searchPath, typeDataBasePaths);
					ClassCompilerNames compilerNames = new ClassCompilerNames ("Consumer", null);

					ErrorHandling errors = ncc.CompileToCSharp (classCompilerLocations, compilerNames, new List<string> { "x86_64-apple-macosx10.9" }, consumerCompiler.DirectoryPath);

					Utils.CheckErrors (errors);

					AssertBindingsCreated (consumerCompiler.DirectoryPath, "Consumer");
				}
			}
		}

		[Test]
		public void TopLevelComputedProperties_PublicGet ()
		{
			const string propertyName = "TopLevelComputedProperties_PublicGet_Answer";
			string swiftCode = $"public var {propertyName}: Int {{ get {{ return 42 }} }}";
			var callingCode = new CodeElementCollection<ICodeElement> { CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)$"TopLevelEntities.{propertyName}") };
			TestRunning.TestAndExecute (swiftCode, callingCode, "42");
		}

		[Test]
		public void TopLevelComputedProperties_PrivateGet ()
		{
			const string propertyName = "TopLevelComputedProperties_PrivateGet_Answer";

			// We want to confirm that wrapping doesn't blow up with a private here
			// However, without _something_ to call it doesn't go well, so just echo
			string swiftCode = $@"public func Echo (s : String) -> String {{ return s }}
private var {propertyName}: Int {{ get {{ return 42 }} }}";
			var echo = CSFunctionCall.Function ("TopLevelEntities.Echo", CSFunctionCall.Function ("SwiftString.FromString", (CSIdentifier)@"""Foo"""));
			var callingCode = new CodeElementCollection<ICodeElement> { CSFunctionCall.FunctionLine ("Console.Write", echo) };
			TestRunning.TestAndExecute (swiftCode, callingCode, "Foo");
		}

		[Test]
		public void TopLevelComputedProperties_PublicGetPrivateSet ()
		{
			const string propertyName = "TopLevelComputedProperties_PublicGetPrivateSet_Answer";

			string swiftCode = $"public var {propertyName}: Int {{ get {{ return 42 }} set {{ }} }}";
			var callingCode = new CodeElementCollection<ICodeElement> { CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)$"TopLevelEntities.{propertyName}") };
			TestRunning.TestAndExecute (swiftCode, callingCode, "42");
		}

		[Test]
		public void TopLevelComputedProperties_PublicGetSet ()
		{
			const string propertyName = "TopLevelComputedProperties_PublicGetSet_Answer";

			string swiftCode = $@"private var AnswerBacking: Int = 0
public var {propertyName}: Int {{ get {{ return AnswerBacking }} set {{ AnswerBacking = newValue }} }}";
			var set = CSAssignment.Assign ((CSIdentifier)$"TopLevelEntities.{propertyName}", (CSIdentifier)"42");
			var read = CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)$"TopLevelEntities.{propertyName}");
			TestRunning.TestAndExecute (swiftCode, new CodeElementCollection<ICodeElement> { set, read }, "42");
		}

		[Test]
		public void TopLevelSetProperty ()
		{
			const string propertyName = "TopLevelSetProperty_Answer";

			string swiftCode = $@"public var {propertyName}: Int = 0";
			var set = CSAssignment.Assign ((CSIdentifier)$"TopLevelEntities.{propertyName}", (CSIdentifier)"42");
			var read = CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)$"TopLevelEntities.{propertyName}");
			TestRunning.TestAndExecute (swiftCode, new CodeElementCollection<ICodeElement> { set, read }, "42");
		}

		[Test]
		public void TopLevelComputedProperties_PublicGetInstanced ()
		{
			string swiftCode = @"public class Math
{
    var Value : Int;
    public init (a : Int)
    {
        Value = a;
    }
    
    public var Answer: Int { get { return Value } }
}";
			var instance = CSAssignment.Assign ("var foo", CSFunctionCall.Ctor ("Math", (CSIdentifier)"42"));
			var print = CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)"foo.Answer");
			var callingCode = new CodeElementCollection<ICodeElement> { instance, print };
			TestRunning.TestAndExecute (swiftCode, callingCode, "42");
		}

		[Test]
		public void LazyVariable ()
		{
			string swiftCode = @"import Foundation;
open class LazyVariable
{
    private lazy var lazyVariable: NSObject = NSObject()
    public init ()
    {
    }
    public var Answer: String { get { return type(of: lazyVariable).description () } }
}";
			var instance = CSAssignment.Assign ("var foo", CSFunctionCall.Ctor ("LazyVariable"));
			var print = CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)"foo.Answer");
			var callingCode = new CodeElementCollection<ICodeElement> { instance, print };
			TestRunning.TestAndExecute (swiftCode, callingCode, "NSObject");

		}

		public static void AssertBindingsCreated (string ouputDirectory, string moduleName)
		{
			string bindingDir = Path.Combine (ouputDirectory, "bindings");
			Assert.IsTrue (Directory.Exists (bindingDir), "Binding directory was not created?");
			Assert.IsTrue (File.Exists (Path.Combine (bindingDir, moduleName)), "Module type database not written out?");
		}



		[Test]
		public void TrivialEnumMember ()
		{
			var swiftCode = @"
public enum Counters {
	case One, Two, Three, Four
	public func toInt() -> Int {
        switch self {
        case .One: return 1
        case .Two: return 2
        case .Three: return 3
        case .Four: return 4
        }
    }
}
";

			var oneID = new CSIdentifier ("one");
			var oneDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, oneID, new CSIdentifier ("Counters.One"));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{oneID.Name}.ToInt", false));
			var callingCode = new CodeElementCollection<ICodeElement> { oneDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "1\n");
		}

		static void XmlOutputExists(string directory)
		{
			var xmlDir = Path.Combine (directory, "XmlReflection");
			Assert.IsTrue (Directory.Exists (xmlDir), "reflection directory doesn't exist");
			var file = Path.Combine (xmlDir, "Swift_XamReflect.xml");
			Assert.IsTrue (File.Exists (file), "reflection file doesn't exist");
		}


		[Test]
		public void TestXmlReflectionRetained ()
		{
			string swiftCode = "public func reflectExists(x:Int32) -> Int32 { return x; }";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSFunctionCall ("TopLevelEntities.ReflectExists" , false, CSConstant.Val (17)));
			callingCode.Add (call);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17", postCompileCheck: XmlOutputExists);

		}

		[Test]
		public void WatchThoseConstructors ()
		{
			var swiftCode = @"
open class Bass {
	private var x:Int32;
	public init (a: Int32) {
		x = a
	}
	public init (a: Int32, b: Int32) {
		x = a + b
	}
	public func getX() -> Int32 {
		return x
	}
}
open class Mid : Bass {
	public override init (a: Int32, b: Int32) {
		super.init (a:a, b: b)
	}
}
";
			var midID = new CSIdentifier ("mid");
			var midDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, midID, new CSFunctionCall ("Mid", true, CSConstant.Val (3), CSConstant.Val (4)));
			var valCall = new CSFunctionCall ($"{midID.Name}.GetX", false);
			var printer = CSFunctionCall.ConsoleWriteLine (valCall);
			var callingCode = CSCodeBlock.Create (midDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void TestNoStructCtor ()
		{
			var swiftCode = @"
public struct WhyMakeMe {
	public var someField:Int = 42
}
public func MakeMe () -> WhyMakeMe {
	return WhyMakeMe ()
}
";
			var meID = new CSIdentifier ("me");
			var meDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("WhyMakeMe"), meID, new CSFunctionCall ("TopLevelEntities.MakeMe", false));
			var printer = CSFunctionCall.ConsoleWriteLine (meID.Dot (new CSIdentifier ("SomeField")));
			var callingCode = CSCodeBlock.Create (meDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n");
		}

		[Test]
		public void TestAnonParamInMethod ()
		{
			var swiftCode = @"
public class NoNeckJoe {
	public init () { }
	public func getValue (_ : Int) -> Int {
		return 73
	}
}
";
			var clID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ("NoNeckJoe", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{clID.Name}.GetValue", false, CSConstant.Val (17)));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "73\n");
		}

		[Test]
		public void TestAnontParamInCtor ()
		{
			var swiftCode = @"
public class NoSpleenJoe {
	public init (_ : Int) { }
	public func getValue (_ : Int) -> Int {
		return 73
	}
}";
			var clID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ("NoSpleenJoe", true, CSConstant.Val (17)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{clID.Name}.GetValue", false, CSConstant.Val (17)));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "73\n");
		}

		[Test]
		public void TestLizardman ()
		{
			var swiftCode = @"
public func lizardman (lizardman: Double) -> Double {
	return lizardman;
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("TopLevelEntities.Lizardman", false, CSConstant.Val (42.0)));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n");
		}

		[Test]
		public void TestEveryProtocol ()
		{
			var swiftCode = @"
public func thisFuncIsNotUsedInThisTest () { }
";
			var clID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ("EveryProtocol", true));
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("nothing here"));
			var disposer = CSFunctionCall.FunctionCallLine ($"{clID.Name}.Dispose", false);
			var callingCode = CSCodeBlock.Create (decl, printer, disposer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "nothing here\n");

		}

		[Test]
		public void TestInitializedVariable ()
		{
			var swiftCode = @"
public var Answer: Int = 42
";

			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("TopLevelEntities.Answer"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n");
		}

		[Test]
		public void TestTypeAliasUsage ()
		{
			var swiftCode = @"
public struct Harmony {
    
    public static func create(_ intervals: [Float]) -> Harmonizer {
        return { firstPitch in
            let pitchSet = PitchSet()
            return intervals.reduce(pitchSet) {
                (ps, interval) -> PitchSet in
                return ps
            }
        }
    }
}


public typealias Harmonizer = ((Pitch) -> PitchSet)


public struct Pitch {
    public init() {}
}

public struct PitchSet
{
    public init() {}
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("got here"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "got here\n");
		}
	}
}

