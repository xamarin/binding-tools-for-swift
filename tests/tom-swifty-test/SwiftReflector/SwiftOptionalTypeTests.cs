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
	public class SwiftOptionalTypeTests {
		void WrapOptionalArg (string type1, string cstype1, string val1, string expected, bool isStatic = true, string distinguisher = "")
		{
			var typeString = type1 + distinguisher;
			string finalDecl = (isStatic ? "final" : "");
			string statDecl = (isStatic ? "static" : "");

			string swiftCode = TestRunningCodeGenerator.kSwiftFileWriter +
						      $"public {finalDecl} class MontyWOA{typeString} {{ public init() {{ }}\n public {statDecl} func printOpt(a:{type1}?)\n {{\n var s = \"\"\nif a != nil {{\n print(\"Optional(\\(a!))\", to:&s)\n }}\n else {{ print(\"nil\", to:&s)\n }}\nwriteToFile(s, \"WrapOptionalArg{typeString}\")\n }}\n}}\n";

			CSBaseExpression optValue = null;
			if (val1 != null) {
				optValue = new CSFunctionCall (String.Format ("SwiftOptional<{0}>", cstype1), true, new CSIdentifier (val1));
			} else {
				optValue = new CSFunctionCall (String.Format ("SwiftOptional<{0}>.None", cstype1), false);
			}

			CSLine csopt = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional", false, new
			    CSSimpleType (cstype1)), "opt", optValue);

			CSLine printer = CSFunctionCall.FunctionCallLine ((isStatic ? $"MontyWOA{typeString}.PrintOpt" : $"new MontyWOA{typeString}().PrintOpt"), false, new CSIdentifier ("opt"));

			CSCodeBlock callingCode = CSCodeBlock.Create (csopt, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapOptionalArg{typeString}");
		}

		[Test]
		public void TestOptBool ()
		{
			WrapOptionalArg ("Bool", "bool", "true", "Optional(true)\n");
		}

		[Test]
		public void TestOptBool1 ()
		{
			WrapOptionalArg ("Bool", "bool", null, "nil\n", distinguisher: "1");
		}

		[Test]
		public void TestOptInt32 ()
		{
			WrapOptionalArg ("Int32", "int", "47", "Optional(47)\n");
		}

		[Test]
		public void TestOptInt321 ()
		{
			WrapOptionalArg ("Int32", "int", null, "nil\n", distinguisher: "1");
		}

		[Test]
		public void TestOptUInt32 ()
		{
			WrapOptionalArg ("UInt32", "uint", "47", "Optional(47)\n");
		}

		[Test]
		public void TestOptUInt321 ()
		{
			WrapOptionalArg ("UInt32", "uint", null, "nil\n", distinguisher: "1");
		}

		[Test]
		public void TestOptFloat ()
		{
			WrapOptionalArg ("Float", "float", "47.5f", "Optional(47.5)\n");
		}

		[Test]
		public void TestOptFloat1 ()
		{
			WrapOptionalArg ("Float", "float", null, "nil\n", distinguisher: "1");
		}


		[Test]
		public void TestOptDouble ()
		{
			WrapOptionalArg ("Double", "double", "47.5", "Optional(47.5)\n");
		}

		[Test]
		public void TestOptDouble1 ()
		{
			WrapOptionalArg ("Double", "double", null, "nil\n", distinguisher: "1");
		}

		[Test]
		public void TestOptVirtBool ()
		{
			WrapOptionalArg ("Bool", "bool", "true", "Optional(true)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtBool1 ()
		{
			WrapOptionalArg ("Bool", "bool", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtInt32 ()
		{
			WrapOptionalArg ("Int32", "int", "47", "Optional(47)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtInt321 ()
		{
			WrapOptionalArg ("Int32", "int", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtUInt32 ()
		{
			WrapOptionalArg ("UInt32", "uint", "47", "Optional(47)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtUInt321 ()
		{
			WrapOptionalArg ("UInt32", "uint", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtInt ()
		{
			WrapOptionalArg ("Int", "nint", "47", "Optional(47)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtInt1 ()
		{
			WrapOptionalArg ("Int", "nint", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtUInt ()
		{
			WrapOptionalArg ("UInt", "nuint", "47", "Optional(47)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtUInt1 ()
		{
			WrapOptionalArg ("UInt", "nuint", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtFloat ()
		{
			WrapOptionalArg ("Float", "float", "47.5f", "Optional(47.5)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtFloat1 ()
		{
			WrapOptionalArg ("Float", "float", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtDouble ()
		{
			WrapOptionalArg ("Double", "double", "47.5", "Optional(47.5)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtDouble1 ()
		{
			WrapOptionalArg ("Double", "double", null, "nil\n", false, distinguisher: "Virt1");
		}

		[Test]
		public void TestOptVirtString ()
		{
			WrapOptionalArg ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")", "Optional(hi mom)\n", false, distinguisher: "Virt");
		}

		[Test]
		public void TestOptVirtString1 ()
		{
			WrapOptionalArg ("String", "SwiftString", null, "nil\n", false, distinguisher: "Virt1");
		}

		void WrapOptionalReturn (string type1, string cstype1, string val1, string expected, bool isFinal = true, string distinguisher = "")
		{
			string finalStr = isFinal ? "final" : "";
			string staticStr = isFinal ? "static" : "";
			string appendage = (type1 + expected).Replace ('.', '_').Replace ('\n', '_').Replace (' ', '_') + distinguisher;

			string swiftCode = $"public {finalStr} class MontyWOR{appendage} {{ public init() {{ }}\n public {staticStr} func makeOpt() -> {type1}?\n {{\n return {val1};\n }}\n }}\n";

			CSLine csopt = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional", false, new
										  CSSimpleType (cstype1)), "opt",
								 new CSFunctionCall (isFinal ? $"MontyWOR{appendage}.MakeOpt" : $"new MontyWOR{appendage}().MakeOpt", false));

			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("opt.ToString"));

			CSCodeBlock callingCode = CSCodeBlock.Create (csopt, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapOptionalReturn{type1}{distinguisher}");
		}

		[Test]
		public void TestReturnOptBool ()
		{
			WrapOptionalReturn ("Bool", "bool", "true", "True\n");
			WrapOptionalReturn ("Bool", "bool", "false", "False\n", distinguisher: "B");
			WrapOptionalReturn ("Bool", "bool", "nil", "\n", distinguisher: "C");
		}

		[Test]
		public void TestReturnOptInt ()
		{
			WrapOptionalReturn ("Int", "nint", "5", "5\n");
			WrapOptionalReturn ("Int", "nint", "nil", "\n", distinguisher: "B");
		}

		[Test]
		public void TestReturnOptUInt ()
		{
			WrapOptionalReturn ("UInt", "nuint", "5", "5\n");
			WrapOptionalReturn ("UInt", "nuint", "nil", "\n", distinguisher: "B");
		}

		[Test]
		public void TestReturnOptFloat ()
		{
			WrapOptionalReturn ("Float", "float", "5.2", "5.2\n");
			WrapOptionalReturn ("Float", "float", "nil", "\n", distinguisher: "B");
		}

		[Test]
		public void TestReturnOptDouble ()
		{
			WrapOptionalReturn ("Double", "double", "5.2", "5.2\n");
			WrapOptionalReturn ("Double", "double", "nil", "\n", distinguisher: "B");
		}

		[Test]
		public void TestReturnOptString ()
		{
			WrapOptionalReturn ("String", "SwiftString", "\"hi mom\"", "hi mom\n");
			WrapOptionalReturn ("String", "SwiftString", "nil", "\n", distinguisher: "B");
		}

		[Test]
		public void TestVirtReturnOptBool ()
		{
			WrapOptionalReturn ("Bool", "bool", "true", "True\n", false, distinguisher: "Virt");
			WrapOptionalReturn ("Bool", "bool", "false", "False\n", false, distinguisher: "VirtB");
			WrapOptionalReturn ("Bool", "bool", "nil", "\n", false, distinguisher: "VirtC");
		}

		[Test]
		public void TestVirtReturnOptInt ()
		{
			WrapOptionalReturn ("Int", "nint", "52", "52\n", false, distinguisher: "Virt");
			WrapOptionalReturn ("Int", "nint", "nil", "\n", false, distinguisher: "VirtB");
		}

		[Test]
		public void TestVirtReturnOptUInt ()
		{
			WrapOptionalReturn ("UInt", "nuint", "52", "52\n", false, distinguisher: "Virt");
			WrapOptionalReturn ("UInt", "nuint", "nil", "\n", false, distinguisher: "VirtB");
		}

		[Test]
		public void TestVirtReturnOptFloat ()
		{
			WrapOptionalReturn ("Float", "float", "52.5", "52.5\n", false, distinguisher: "Virt");
			WrapOptionalReturn ("Float", "float", "nil", "\n", false, distinguisher: "VirtB");
		}

		[Test]
		public void TestVirtReturnOptString ()
		{
			WrapOptionalReturn ("String", "SwiftString", "\"hi mom\"", "hi mom\n", false, distinguisher: "Virt");
			WrapOptionalReturn ("String", "SwiftString", "nil", "\n", false, distinguisher: "VirtB");
		}

		void WrapOptionalClassArg (string type1, string cstype1, string val1, string expected)
		{
			string appendage = (type1 + expected).Replace ('.', '_').Replace ('\n', '_').Replace (' ', '_');
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
			    $"public class FooWOCA{appendage} {{\n private var x:{type1}\n public init(a:{type1}) {{\n x = a;\n}}\n public func getX() -> {type1} {{\n return x;\n}} }}\n" +
				       $"public final class MontyWOCA{appendage} {{ public init() {{ }}\n public static func printOpt(a:FooWOCA{appendage}?)\n {{\nvar s = \"\"\n if a != nil {{\n print(a!.getX(), to:&s)\n }}\n else {{ print(\"nil\", to:&s)\n }}\nwriteToFile(s, \"WrapOptionalClassArg{appendage}\")\n }}\n}}\n";

			CSLine csopt = CSVariableDeclaration.VarLine (new CSSimpleType ($"SwiftOptional<FooWOCA{appendage}>"), "opt",
								      val1 != null ? (CSBaseExpression)new CSFunctionCall ($"SwiftOptional<FooWOCA{appendage}>", true, new CSFunctionCall ($"FooWOCA{appendage}", true, new CSIdentifier (val1))) :
								      (CSBaseExpression)new CSFunctionCall ($"SwiftOptional<FooWOCA{appendage}>.None", false));

			CSLine printer = CSFunctionCall.FunctionCallLine ($"MontyWOCA{appendage}.PrintOpt", false, new CSIdentifier ("opt"));

			CSCodeBlock callingCode = CSCodeBlock.Create (csopt, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapOptionalClassArg{appendage}");
		}

		[Test]
		public void TestOptClassBool ()
		{
			WrapOptionalClassArg ("Bool", "bool", "true", "true\n");
			WrapOptionalClassArg ("Bool", "bool", "false", "false\n");
			WrapOptionalClassArg ("Bool", "bool", null, "nil\n");
		}

		[Test]
		public void TestOptClassInt ()
		{
			WrapOptionalClassArg ("Int", "nint", "57", "57\n");
			WrapOptionalClassArg ("Int", "nint", null, "nil\n");
		}

		[Test]
		public void TestOptClassUInt ()
		{
			WrapOptionalClassArg ("UInt", "nuint", "57", "57\n");
			WrapOptionalClassArg ("UInt", "nuint", null, "nil\n");
		}

		[Test]
		public void TestOptClassFloat ()
		{
			WrapOptionalClassArg ("Float", "float", "57.5f", "57.5\n");
			WrapOptionalClassArg ("Float", "float", null, "nil\n");
		}

		[Test]
		public void TestOptClassDouble ()
		{
			WrapOptionalClassArg ("Double", "double", "57.5", "57.5\n");
			WrapOptionalClassArg ("Double", "double", null, "nil\n");
		}

		[Test]
		public void WrapOptionalClassReturn ()
		{
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
				       $"public class FooWOCR {{\n public init() {{\n}}\n public func doIt() {{\nvar s = \"\"\n print(\"hi mom\", to:&s);\nwriteToFile(s, \"WrapOptionalClassReturn\")\n }} }}\n" +
			    "public final class MontyWOCR { public init() { }\n public static func getFoo(a:Bool)\n->FooWOCR? {\n if a {\n return FooWOCR();\n }\n else { return nil;\n }\n }\n}\n";

			CSLine csopt1 = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional<FooWOCR>"), "foo1",
					  new CSFunctionCall ("MontyWOCR.GetFoo", false, CSConstant.Val (true)));
			CSLine csopt2 = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional<FooWOCR>"), "foo2",
								  new CSFunctionCall ("MontyWOCR.GetFoo", false, CSConstant.Val (false)));

			CSLine printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("{0}, {1}"), (CSIdentifier)"foo1", (CSIdentifier)"foo2");

			CSCodeBlock callingCode = CSCodeBlock.Create (csopt1, csopt2, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "SwiftOptionalTypeTests.FooWOCR, \n");
		}

		[TestCase("Int", "3", "nint", "3\n")]
		[TestCase ("Float", "-42.5", "float", "-42.5\n")]
		[TestCase ("Double", "-42.5", "double", "-42.5\n")]
		[TestCase ("Bool", "true", "bool", "True\n")]
		[TestCase ("String", "\"hi mom\"", "SwiftString", "hi mom\n")]
		public void WrapImplicitlyUnwrappedOptional(string swiftType, string swiftVal, string csType, string expected)
		{
			string swiftCode = $"public func returnBang{swiftType}()->{swiftType}! {{\n" +
				$"   return {swiftVal}\n" +
				"}";
			CSLine csopt = CSVariableDeclaration.VarLine (new CSSimpleType ($"SwiftOptional<{csType}>"), "foo",
			                                              new CSFunctionCall ($"TopLevelEntities.ReturnBang{swiftType}", false));
			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"foo.Value");
			CSCodeBlock callingCode = CSCodeBlock.Create (csopt, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapImplicitlyUnwrappedOptional{swiftType}");
		}

		[Test]
		public void WrapOptionalVirtualProp ()
		{
			string swiftCode =
				"open class OptPropClass {\n" +
				"    open var x:Int? = 17\n" +
				"    public init () {\n" +
				"    }\n" +
				"    open func referenceFunc () -> Int? {\n" +
				"        return x\n" +
				"    }" +
				"}\n";

			var classID = new CSIdentifier ("cl");
			var classDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("OptPropClass"), classID,
			                                               new CSFunctionCall ("OptPropClass", true));
			var printer = CSFunctionCall.ConsoleWriteLine (classID.Dot ((CSIdentifier)"X").Dot ((CSIdentifier)"Value"));

			var callingCode = CSCodeBlock.Create (classDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n", platform: PlatformName.macOS);
		}

		[Test]
		public void WrapOptionalVirtMethod ()
		{
			string swiftCode =
				"open class OptMethClass {\n" +
				"    private var x:Int? = 17\n" +
				"    public init () {\n" +
				"    }\n" +
				"    open func referenceFunc () -> Int? {\n" +
				"        return x\n" +
				"    }" +
				"}\n";
			var classID = new CSIdentifier ("cl");
			var classDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("OptMethClass"), classID,
			                                               new CSFunctionCall ("OptMethClass", true));
			var printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("cl.ReferenceFunc"));

			var callingCode = CSCodeBlock.Create (classDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n", platform: PlatformName.macOS);
		}


		[Test]
		public void TestUnwrappedOptional ()
		{
			string swiftCode =
				"public class FooAny {\n" +
				"    public init() { }\n" +
				"}\n" +
				"public class AnyBang {\n" +
				"    public var x: AnyObject!\n" +
				"    public init (ix: AnyObject!) {\n" +
				"        x = ix\n" +
				"    }\n" +
				"}\n";

			var fooAnyID = new CSIdentifier ("fooAny");
			var fooAnyDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("FooAny"), fooAnyID,
			                                                new CSFunctionCall ("FooAny", true));

			var anyObjID = new CSIdentifier ("anyObj");
			var anyObjDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftAnyObject"), anyObjID, new CSFunctionCall ("SwiftAnyObject.FromISwiftObject", false, fooAnyID));

			var anyBangID = new CSIdentifier ("anyBang");
			var anyBangDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("AnyBang"), anyBangID,
									 new CSFunctionCall ("AnyBang", true,
			                                                                     new CSFunctionCall ("SwiftOptional<SwiftAnyObject>", true, anyObjID)));

			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("success."));

			var callingCode = CSCodeBlock.Create (fooAnyDecl, anyObjDecl, anyBangDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "success.\n");
		}	
	}
}

