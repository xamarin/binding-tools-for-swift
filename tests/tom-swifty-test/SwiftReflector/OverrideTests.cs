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
using NUnit.Framework.Legacy;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class OverrideTests {

		List<ClassDeclaration> ReflectClassDeclarations (string code)
		{
			using (TempDirectoryFilenameProvider fileProvider = new TempDirectoryFilenameProvider (null, false)) {

				CustomSwiftCompiler compiler = Utils.DefaultSwiftCompiler (fileProvider);
				SwiftCompilerOptions options = new SwiftCompilerOptions ("NameNotImportant", null, null, null);
				compiler.CompileString (options, code);

				List<ModuleDeclaration> modules = compiler.ReflectToModules (new string [] { compiler.DirectoryPath },
					new string [] { compiler.DirectoryPath }, "", "NameNotImportant");
				ClassicAssert.AreEqual (1, modules.Count);
				return modules [0].AllClasses;
			}
		}

		[Test]
		[Ignore ("need to refactor RelflectClassDeclarations")]
		public void SmokeTestOverride0 ()
		{
			string code = "open class Foo { public init() { }\nopen func doSomething() { }\n}\n";
			List<ClassDeclaration> classes = ReflectClassDeclarations (code);
			ClassicAssert.AreEqual (1, classes.Count);
			ClassDeclaration theClass = classes [0].MakeUnrooted () as ClassDeclaration;

			TypeMapper typeMapper = new TypeMapper (Compiler.kTypeDatabases);
			typeMapper.RegisterClass (theClass);

			OverrideBuilder overrider = new OverrideBuilder (typeMapper, theClass, null, new ModuleDeclaration ("OverrideModule"));

			ClassicAssert.IsNotNull (overrider.OverriddenClass);
			ClassicAssert.AreEqual (1, overrider.ClassImplementations.Count);
			ClassicAssert.IsNotNull (overrider.OverriddenVirtualMethods);
			ClassicAssert.AreEqual (1, overrider.OverriddenVirtualMethods.Count);

			using (TempDirectoryFilenameProvider temp = new TempDirectoryFilenameProvider (null, false)) {
				string file = temp.ProvideFileFor ("output.swift");
				SLFile swiftFile = new SLFile (overrider.Imports);
				swiftFile.Classes.AddRange (overrider.ClassImplementations);
				CodeWriter.WriteToFile (file, swiftFile);
			}
		}


		void WrapSingleMethod (string type, string returnVal, string csType, string csReplacement, string expected)
		{
			string swiftCode = $"open class MontyWSM{type} {{ public init() {{}}\n open func val() -> {type} {{ return {returnVal}; }} }}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSM{type}");
			overCS.Inheritance.Add (new CSIdentifier ($"MontyWSM{type}"));
			CSCodeBlock overBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSIdentifier (csReplacement)));
			CSMethod overMeth = new CSMethod (CSVisibility.Public, CSMethodKind.Override, new CSSimpleType (csType),
					      new CSIdentifier ("Val"), new CSParameterList (), overBody);
			overCS.Methods.Add (overMeth);

			CSCodeBlock printBody = CSCodeBlock.Create (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("{0}, {1}"),
												     CSFunctionCall.Function ("base.Val"), CSFunctionCall.Function ("Val")));
			CSMethod printIt = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("PrintIt"), new CSParameterList (), printBody);
			overCS.Methods.Add (printIt);
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSM{type}"), "printer", new CSFunctionCall ($"OverWSM{type}", true)));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("printer.PrintIt", false));
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapSingleMethod{type}", otherClass: overCS);
		}

		[Test]
		public void WrapSingleMethodBool ()
		{
			WrapSingleMethod ("Bool", "true", "bool", "false", "True, False\n");
		}

		[Test]
		public void WrapSingleMethodInt ()
		{
			WrapSingleMethod ("Int64", "3", "long", "4", "3, 4\n");
		}

		[Test]
		public void WrapSingleMethodUInt ()
		{
			WrapSingleMethod ("UInt64", "3", "ulong", "4", "3, 4\n");
		}

		[Test]
		public void WrapSingleMethodFloat ()
		{
			WrapSingleMethod ("Float", "3", "float", "4", "3, 4\n");
		}

		[Test]
		public void WrapSingleMethodDouble ()
		{
			WrapSingleMethod ("Double", "3", "double", "4", "3, 4\n");
		}

		[Test]
		public void WrapSingleMethodString ()
		{
			WrapSingleMethod ("String", "\"Hi\"", "SwiftString", "SwiftString.FromString(\"Mom\")", "Hi, Mom\n");
		}

		void WrapClassCallsVirtual (string type, string returnVal, string csType, string csReplacement, string expected)
		{
			string safeType = type.Replace ('.', '_');
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
			    $"open class HolderWCCV{safeType} {{ public init() {{}}\n open func val() -> {type} {{ return {returnVal}; }} }}\n" +
				       $"public final class FooWVVC{safeType} {{ var holder:HolderWCCV{safeType}\npublic init(h:HolderWCCV{safeType}) {{ holder = h\n}}\npublic func doIt() {{ var s = \"\"\nprint(holder.val(), to:&s);\nwriteToFile(s, \"WrapClassCallsVirtual{safeType}\")\n }} }}";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWCCV{safeType}");
			overCS.Inheritance.Add (new CSIdentifier ($"HolderWCCV{safeType}"));
			CSCodeBlock overBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSIdentifier (csReplacement)));
			CSMethod overMeth = new CSMethod (CSVisibility.Public, CSMethodKind.Override, new CSSimpleType (csType),
			    new CSIdentifier ("Val"), new CSParameterList (), overBody);
			overCS.Methods.Add (overMeth);

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ($"FooWVVC{safeType}"), "foo", new CSFunctionCall ($"FooWVVC{safeType}", true, new CSFunctionCall ($"OverWCCV{safeType}", true))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("foo.DoIt", false));
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapClassCallsVirtual{safeType}", otherClass: overCS);
		}

		[Test]
		public void WrapClassUsesClassBool ()
		{
			WrapClassCallsVirtual ("Bool", "true", "bool", "false", "false\n");
		}

		[Test]
		public void WrapClassUsesClassInt64 ()
		{
			WrapClassCallsVirtual ("Swift.Int64", "3", "long", "4", "4\n");
		}

		[Test]
		public void WrapClassUsesClassUInt64 ()
		{
			WrapClassCallsVirtual ("Swift.UInt64", "3", "ulong", "4", "4\n");
		}

		[Test]
		public void WrapClassUsesClassFloat ()
		{
			WrapClassCallsVirtual ("Float", "3", "float", "4", "4.0\n");
		}

		[Test]
		public void WrapClassUsesClassDouble ()
		{
			WrapClassCallsVirtual ("Double", "3", "double", "4", "4.0\n");
		}

		[Test]
		public void WrapClassUsesClassString ()
		{
			WrapClassCallsVirtual ("String", "\"Hi\"", "SwiftString", "SwiftString.FromString(\"Mom\")", "Mom\n");
		}

		void WrapSingleProperty (string type, string returnVal, string csType, string csReplacement, string expected)
		{
			string appendage = type.Replace ('.', '_');
			string swiftCode =
			    $"open class MontyWSP{appendage} {{ public init() {{}}\n open var val: {type} {{\nget {{ return {returnVal}\n}} }} }}";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSP{appendage}");
			overCS.Inheritance.Add (new CSIdentifier ($"MontyWSP{appendage}"));
			CSCodeBlock getterBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSIdentifier (csReplacement)));

			CSProperty overProp = new CSProperty (new CSSimpleType (csType), CSMethodKind.Override, new CSIdentifier ("Val"),
			    CSVisibility.Public, getterBody, CSVisibility.Public, null);
			overCS.Properties.Add (overProp);

			CSCodeBlock printBody = CSCodeBlock.Create (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("{0}, {1}"), (CSIdentifier)"base.Val", (CSIdentifier)"Val"));
			CSMethod printIt = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("PrintIt"), new CSParameterList (), printBody);
			overCS.Methods.Add (printIt);

			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSP{appendage}"), "printer", new CSFunctionCall ($"OverWSP{appendage}", true));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("printer.PrintIt", false);
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapSingleProperty{appendage}", otherClass: overCS);
		}

		[Test]
		public void WrapSinglePropBool ()
		{
			WrapSingleProperty ("Bool", "true", "bool", "false", "True, False\n");
		}

		[Test]
		public void WrapSinglePropInt64 ()
		{
			WrapSingleProperty ("Swift.Int64", "3", "long", "4", "3, 4\n");
		}

		[Test]
		public void WrapSinglePropUInt64 ()
		{
			WrapSingleProperty ("Swift.UInt64", "3", "ulong", "4", "3, 4\n");
		}

		[Test]
		public void WrapSinglePropFloat ()
		{
			WrapSingleProperty ("Float", "3.0", "float", "4.0f", "3, 4\n");
		}

		[Test]
		public void WrapSinglePropDouble ()
		{
			WrapSingleProperty ("Double", "3.0", "double", "4.0", "3, 4\n");
		}

		[Test]
		public void WrapSinglePropString ()
		{
			WrapSingleProperty ("String", "\"Hi\"", "SwiftString", "SwiftString.FromString(\"Mom\")", "Hi, Mom\n");
		}

		void WrapSingleGetSetProperty (string type, string returnVal, string csType, string csReplacement, string expectedOutput)
		{
			string appendage = type.Replace ('.', '_');
			var swiftCode = String.Format ("public class Monty {{ public init() {{}}\n private var _x:{0} = {1}\npublic var val: {0} {{\nget {{ return _x\n}}\nset {{ _x = newValue\n}} }} }}", type, returnVal);
			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("Monty"), "monty", new CSFunctionCall ("Monty", true));
			var invoker = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("monty.Val"));
			var setter = CSAssignment.Assign ("monty.Val", new CSIdentifier (csReplacement));
			var callingCode = CSCodeBlock.Create (decl, invoker, setter, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expectedOutput, testName: $"WrapSingleProperty{appendage}");
		}

		[Test]
		public void WrapSingleGetSetPropBool ()
		{
			WrapSingleGetSetProperty ("Bool", "true", "bool", "false", "True\nFalse\n");
		}

		[Test]
		public void WrapSingleGetSetPropInt64 ()
		{
			WrapSingleGetSetProperty ("Swift.Int64", "3", "long", "4", "3\n4\n");
		}

		[Test]
		public void WrapSingleGetSetPropUInt64 ()
		{
			WrapSingleGetSetProperty ("Swift.UInt64", "3", "ulong", "4", "3\n4\n");
		}

		[Test]
		public void WrapSingleGetSetPropFloat ()
		{
			WrapSingleGetSetProperty ("Float", "3.0", "float", "4.0f", "3\n4\n");
		}

		[Test]
		public void WrapSingleGetSetPropDouble ()
		{
			WrapSingleGetSetProperty ("Double", "3.0", "double", "4.0", "3\n4\n");
		}

		[Test]
		public void WrapSingleGetSetPropString ()
		{
			WrapSingleGetSetProperty ("String", "\"Hi\"", "SwiftString", "SwiftString.FromString(\"Mom\")", "Hi\nMom\n");
		}

		void WrapSingleGetSetSubscript0 (string type, string returnVal, string csType, string csReplacement, string expected)
		{
			string swiftCode = $"public class MontyWSGSSub{type} {{ public init() {{}}\n private var _x:{type} = {returnVal}\npublic subscript(i:Int32) -> {type} {{\nget {{ return _x\n}}\nset {{ _x = newValue\n}} }} }}";
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"MontyWSGSSub{type}"), "monty", new CSFunctionCall ($"MontyWSGSSub{type}", true));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"monty[0]");
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, $"WrapSingleGetSetSubscript0{type}");
		}

		[Test]
		public void WrapSingleGetSubscriptBool ()
		{
			WrapSingleGetSetSubscript0 ("Bool", "true", "bool", "false", "True\n");
		}

		[Test]
		public void WrapSingleGetSubscriptInt ()
		{
			WrapSingleGetSetSubscript0 ("Int32", "42", "int", "37", "42\n");
		}

		[Test]
		public void WrapSingleGetSubscriptUInt ()
		{
			WrapSingleGetSetSubscript0 ("UInt32", "42", "uint", "37", "42\n");
		}

		[Test]
		public void WrapSingleGetSubscriptFloat ()
		{
			WrapSingleGetSetSubscript0 ("Float", "42", "float", "37", "42\n");
		}

		[Test]
		public void WrapSingleGetSubscriptDouble ()
		{
			WrapSingleGetSetSubscript0 ("Double", "42", "double", "37", "42\n");
		}

		[Test]
		public void WrapSingleGetSubscriptString ()
		{
			WrapSingleGetSetSubscript0 ("String", "\"Hi mom.\"", "SwiftString", "37", "Hi mom.\n");
		}

		void WrapSingleGetSetSubscript1 (string type, string returnVal, string csType, string csReplacement, string expected)
		{
			string swiftCode = $"public class MontyWSGSSub1{type} {{ public init() {{}}\n private var _x:{type} = {returnVal}\npublic subscript(i:Int32) -> {type} {{\nget {{ return _x\n}}\nset {{ _x = newValue\n}} }} }}";
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"MontyWSGSSub1{type}"), "monty", new CSFunctionCall ($"MontyWSGSSub1{type}", true));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"monty[0]");
			CSLine setter = CSAssignment.Assign ("monty[0]", new CSIdentifier (csReplacement));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, invoker, setter, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapSingleGetSetSubscript1{type}");
		}

		[Test]
		public void WrapSingleGetSetSubscriptBool ()
		{
			WrapSingleGetSetSubscript1 ("Bool", "true", "bool", "false", "True\nFalse\n");
		}

		[Test]
		public void WrapSingleGetSetSubscriptInt ()
		{
			WrapSingleGetSetSubscript1 ("Int32", "42", "int", "37", "42\n37\n");
		}

		[Test]
		public void WrapSingleGetSetSubscriptUInt ()
		{
			WrapSingleGetSetSubscript1 ("UInt32", "42", "uint", "37", "42\n37\n");
		}

		[Test]
		public void WrapSingleGetSetSubscriptFloat ()
		{
			WrapSingleGetSetSubscript1 ("Float", "42", "float", "37f", "42\n37\n");
		}

		[Test]
		public void WrapSingleGetSetSubscriptDouble ()
		{
			WrapSingleGetSetSubscript1 ("Double", "42", "double", "37.0", "42\n37\n");
		}

		[Test]
		public void WrapSingleGetSetSubscriptString ()
		{
			WrapSingleGetSetSubscript1 ("String", "\"Hi\"", "SwiftString", "SwiftString.FromString(\"Mom\")", "Hi\nMom\n");
		}

		[Test]
		public void WrapSingleGetSetSubscript2 ()
		{
			string swiftCode = $"public final class FooWSGSub2 {{ public let X:Int\npublic init(i:Int) {{ X = i\n }} }}\n public class MontyWSGSub2 {{ public init() {{}}\n private var _x:FooWSGSub2 = FooWSGSub2(i:42);\npublic subscript(i:Int32) -> FooWSGSub2 {{\nget {{ return _x\n}}\nset {{ _x = newValue\n}} }} }}";
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("MontyWSGSub2"), "monty", new CSFunctionCall ("MontyWSGSub2", true));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"monty[0].X");
			CSLine setter = CSAssignment.Assign ("monty[0]", new CSFunctionCall ("FooWSGSub2", true, CSConstant.Val (37L)));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, invoker, setter, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n37\n");
		}

		[Test]
		public void WrapSingleGetSetSubscript3 ()
		{
			string swiftCode = $"public struct FooWSGSub3 {{ public let X:Int\npublic init(i:Int) {{ X = i\n }} }}\n public class MontyWSGSub3 {{ public init() {{}}\n private var _x:FooWSGSub3 = FooWSGSub3(i:42);\npublic subscript(i:Int32) -> FooWSGSub3 {{\nget {{ return _x\n}}\nset {{ _x = newValue\n}} }} }}";
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("MontyWSGSub3"), "monty", new CSFunctionCall ("MontyWSGSub3", true));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"monty[0].X");
			CSLine setter = CSAssignment.Assign ("monty[0]", new CSFunctionCall ("FooWSGSub3", true, CSConstant.Val (37L)));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, invoker, setter, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n37\n");
		}



		[Test]
		public void WrapVirtSubscriptGetInt ()
		{
			string swiftCode =
				"open class ScriptGetInt {\n" +
				"   public init () { } \n" +
				"   open subscript (index:Int32) -> Int32 {\n" +
				"       get {\n" +
				"           return index\n" +
				"       }\n" +
				"   }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("ScriptGetInt"), "sub", new CSFunctionCall ("ScriptGetInt", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIndexExpression ("sub", false, CSConstant.Val (7)));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void WrapVirtualSubscriptProtocol ()
		{
			string swiftCode =
				"public protocol Thingy {\n" +
				"    func whoAmI () -> String\n" +
				"}\n" +
				"public class Popeye : Thingy {\n" +
				"    public init() { }\n" +
				"    public func whoAmI () -> String\n {" +
				"        return \"who I yam\"\n" +
				"    }\n" +
				"}\n" +
				"open class Scripto {\n" +
				"   private var x:Thingy = Popeye()\n" +
				"   public init() { }\n" +
				"   open subscript (index: Int) -> Thingy {\n" +
				"        get {\n" +
				"            return x\n" +
				"        }\n" +
				"        set(newValue) {\n" +
				"            x = newValue\n" +
				"        }\n" +
				"   }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("Scripto"), "sub", new CSFunctionCall ("Scripto", true));
			var decl2 = CSVariableDeclaration.VarLine (CSSimpleType.Var, "pop", new CSIndexExpression ("sub", false, CSConstant.Val (0)));
			var printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("pop.WhoAmI"));
			var callingCode = CSCodeBlock.Create (decl, decl2, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "who I yam\n");
		}

		[Test]
		public void WrapVirtClassNonVirtMethod ()
		{
			string swiftCode =
				"open class VirtClassNVM {\n" +
				"    public init() { }\n" +
				"    public func returns17() -> Int {\n" +
				"        return 17" +
				"    }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("VirtClassNVM"), "cl", new CSFunctionCall ("VirtClassNVM", true));
			var printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("cl.Returns17"));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n");
		}

		[Test]
		public void WrapVirtClassNonVirtProp ()
		{
			string swiftCode =
				"open class VirtClassNVP {\n" +
				"    public init() { }\n" +
				"    public var x:Int = 17" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("VirtClassNVP"), "cl", new CSFunctionCall ("VirtClassNVP", true));
			var printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"cl.X");
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n");
		}

		[Test]
		public void WrapVirtClassNonVirtIndex ()
		{
			string swiftCode =
				"open class VirtClassNVS {\n" +
				"    private var X:[Bool] = [false, true]\n" +
				"    public init() { }\n" +
				"    public subscript (index: Int) -> Bool {\n" +
				"        get {\n" +
				"            return X[index & 1]\n" +
				"        }\n" +
				"        set(newValue) {\n" +
				"            X[index & 1] = newValue\n" +
				"        }\n" +
				"    }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("VirtClassNVS"), "cl", new CSFunctionCall ("VirtClassNVS", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIndexExpression (new CSIdentifier ("cl"), false, CSConstant.Val (0)));
			var printer1 = CSFunctionCall.ConsoleWriteLine (new CSIndexExpression (new CSIdentifier ("cl"), false, CSConstant.Val (1)));
			var callingCode = CSCodeBlock.Create (decl, printer, printer1);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\nTrue\n");
		}


		[Test]
		public void RequiredInitTest ()
		{
			string swiftCode =
				"open class BaseWithReq {\n" +
				"    public var x: String\n" +
				"    public required init (s: String) {\n" +
				"        x = s\n" +
				"    }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("BaseWithReq"), "bs", new CSFunctionCall ("BaseWithReq", true, new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("got it"))));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("bs").Dot (new CSIdentifier ("X")));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "got it\n");
		}

		[Test]
		public void RequiredInitTestWithSubclass ()
		{
			string swiftCode =
				"open class BaseWithReqWithSubclass {\n" +
				"    public var x: String\n" +
				"    public required init (s: String) {\n" +
				"        x = s\n" +
				"    }\n" +
				"}\n" +
				"open class SubOfBase : BaseWithReqWithSubclass {\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SubOfBase"), "sub", new CSFunctionCall ("SubOfBase", true, new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("got it"))));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("sub").Dot (new CSIdentifier ("X")));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "got it\n");
		}

		[Test]
		public void OptionalInitTest ()
		{
			string swiftCode =
				"open class OptionalInit {\n" +
				"    public init? (b: Bool) {\n" +
				"        if !b { return nil; }\n" +
				"    }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional<OptionalInit>"), "opt", new CSFunctionCall ("OptionalInit.OptionalInitOptional", false, CSConstant.Val (true)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("opt.HasValue"));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

		[Test]
		public void OptionalInitFailTest ()
		{
			string swiftCode =
				"open class OptionalInitFail {\n" +
				"    public init? (b: Bool) {\n" +
				"        if !b { return nil; }\n" +
				"    }\n" +
				"}\n";

			var decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional<OptionalInitFail>"), "opt", new CSFunctionCall ("OptionalInitFail.OptionalInitFailOptional", false, CSConstant.Val (false)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("opt.HasValue"));
			var callingCode = CSCodeBlock.Create (decl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}

		[Test]
		public void VirtualPropIsVirtual ()
		{
			string swiftCode =
				"open class ItsAVirtProp {\n" +
				"     open var x = 0\n" +
				"     public init (ix: Int) {\n" +
				"         x = ix\n" +
				"     }\n" +
				"}\n";

			// var pi = typeof (ItsAVirtProp).GetProperty ("X");
			// Console.WriteLine(pi.GetGetMethod().IsVirtual);
			var piID = new CSIdentifier ("pi");
			var getProp = new CSFunctionCall ("GetProperty", false, CSConstant.Val ("X"));
			var propInfo = CSVariableDeclaration.VarLine (CSSimpleType.Var, piID, new CSSimpleType ("ItsAVirtProp").Typeof ().Dot (getProp));
			var getGet = new CSFunctionCall ("GetGetMethod", false);
			var printer = CSFunctionCall.ConsoleWriteLine (piID.Dot (getGet.Dot (new CSIdentifier ("IsVirtual"))));
			var callingCode = CSCodeBlock.Create (propInfo, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void BadArgumentName ()
		{
			string swiftCode =
				"open class AEXMLElement {\n" +
				"    open func allDescendants (where predicate: @escaping (AEXMLElement) -> Bool) -> [AEXMLElement] {\n" +
				"        return []\n" +
				"    }\n" +
				"}\n";

			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n");
		}

		[Test]
		public void MoreArgumentNames ()
		{
			string swiftCode =
				"open class AEXMLElement1 {\n" +
				"    public init() {\n" +
				"    }\n" +
				"    @discardableResult open func addChild(_ child: AEXMLElement1) -> AEXMLElement1 {\n" +
				"        return child\n" +
				"    }\n" +
				"    @discardableResult open func addChild(name: String, value: String? = nil, attributes: [String: String] = [String: String] ()) -> AEXMLElement1 {\n" +
				"        let child = AEXMLElement1()\n" +
				"        return addChild(child)\n" +
				"    }\n" +
				"    @discardableResult open func addChildren(_ children: [AEXMLElement1]) -> [AEXMLElement1] {\n" +
				"        return []\n" +
				"    }\n" +
				"}\n";

			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n");
		}

		[Test]
		public void SubscriptMarshaling ()
		{
			string swiftCode =
				"open class AEXMLElement2 {\n" +
				"    public init (name:String) {\n" +
				"    }\n" +
				"    open subscript (key: String) -> AEXMLElement2 {\n" +
				"        return AEXMLElement2 (name: \"\")\n" +
				"    }\n" +
				"}\n";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n");
		}

		[Test]
		public void TrivialEnumMarshalProp ()
		{
			var swiftCode = @"
import Foundation
@objc
public enum TrivEnum0 : Int {
	case None, Blind, Free
}
open class TrivClass0 {
	public init () { }
	open var X : TrivEnum0 = .Blind
}";
			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("TrivClass0", true));
			var printer = CSFunctionCall.ConsoleWriteLine (declID.Dot (new CSIdentifier ("X")));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Blind\n");
		}


		[Test]
		public void TrivialEnumMethod ()
		{
			var swiftCode = @"
import Foundation
@objc
public enum TrivEnum1 : Int {
	case None, Blind, Free
}
open class TrivClass1 {
	public init () { }
	open func foo () -> TrivEnum1 {
		return .Blind
	}
}
";
			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("TrivClass1", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{declID.Name}.Foo", false));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Blind\n");

		}



		[Test]
		public void ConvenienceCtor ()
		{
			var swiftCode = @"
open class InconvenienceClass {
	private var x: Double
	public init (f: Double) {
		x = f
	}
	public convenience init (g: Int) {
		self.init (f: Double(g))
	}
	public func getX () -> Double {
		return x
	}
}
";

			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("InconvenienceClass", true, CSConstant.Val (3.14)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{declID.Name}.GetX", false));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3.14\n");
		}


		[Test]
		public void TestAnonymousNameInOverride ()
		{
			var swiftCode = @"
open class TheBaseClass {
	public init () { }
	open func getValue (_ : Int) -> Int {
		return 3;
	}
}
";

			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("TheBaseClass", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{declID.Name}.GetValue", false, CSConstant.Val (42)));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestMultiOverride ()
		{
			
			var swiftCode = @"
open class FirstClass {
	public init () { }
	open func firstFunc () -> Int {
	    return 42
	}
}

open class SecondClass : FirstClass {
	public override init () { }

	open func secondFunc () -> Int {
	    return 17
	}
}
";
			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("SecondClass", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{declID.Name}.FirstFunc", false));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n", "TestMultiOverride");
		}

		[Test]
		public void TestClosureProp ()
		{
			var swiftCode = @"
open class ClosureFunc {
	public init () { }
	open var animationDidStartClosure = {(onAnimation: Bool) -> Void in }
	public func returns5() -> Int {
	    return 5
	}
}
";
			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("ClosureFunc", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{declID.Name}.Returns5", false));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "5\n", testName: "TestClosureProp");
		}

		[Test]
		public void TestClosureParameter ()
		{
			var swiftCode = @"
public class ClosureArg {
	public init () { }
	public func caller (clos: @escaping (Int32)->Bool, x: Int32) -> Bool {
	    return clos(x)
	}
}
";
			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("ClosureArg", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{declID.Name}.Caller", false, new CSIdentifier ("x => (x & 1) != 0"),
				CSConstant.Val (7)));
			var callingCode = CSCodeBlock.Create (decl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

		[Test]
		public void TestClosureReturn ()
		{
			var swiftCode = @"
public class ClosureReturn {
	public init () { }
	public func caller () -> (Int32)->Bool {
	    return { x in
		    return x % 2 == 1
	    }
	}
}
";
			var declID = new CSIdentifier ("cl");
			var funcID = new CSIdentifier ("x");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("ClosureReturn", true));
			var xdecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, funcID, new CSFunctionCall ($"{declID.Name}.Caller", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall (funcID.Name, false, CSConstant.Val (7)));
			var callingCode = CSCodeBlock.Create (decl, xdecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void TestClosureVirtualReturn ()
		{
			var swiftCode = @"
open class ClosureVirtualReturn {
	public init () { }
	open func caller () -> (Int32)->Bool {
	    return { x in
		    return x % 2 == 1
	    }
	}
}
";
			var declID = new CSIdentifier ("cl");
			var funcID = new CSIdentifier ("x");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("ClosureVirtualReturn", true));
			var xdecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, funcID, new CSFunctionCall ($"{declID.Name}.Caller", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall (funcID.Name, false, CSConstant.Val (7)));
			var callingCode = CSCodeBlock.Create (decl, xdecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void TestClosureSimpleProp ()
		{
			var swiftCode = @"
open class ClosureSimpleProp {
	public init () { }
	open var x: ()->() = { () in }
}
";

			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("ClosureSimpleProp", true));
			var setter = CSAssignment.Assign ($"{declID}.X", new CSIdentifier ("() => { Console.WriteLine (\"here\"); }"));
			var execIt = CSFunctionCall.FunctionCallLine ($"{declID}.X");
			var callingCode = CSCodeBlock.Create (decl, setter, execIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "here\n");
		}


		[Test]
		public void TestSimpleSetClosure ()
		{
			var swiftCode = @"
open class SimpleSetClosure {
	public init () { }
	open func setValue (a: @escaping ()->()) {
	    a()
	}
}
";

			var declID = new CSIdentifier ("cl");
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, declID, new CSFunctionCall ("SimpleSetClosure", true));
			var execIt = CSFunctionCall.FunctionCallLine ($"{declID}.SetValue", new CSIdentifier ("() => { Console.WriteLine (\"here\"); }"));
			var callingCode = CSCodeBlock.Create (decl, execIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "here\n");
		}
	}
}

