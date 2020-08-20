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
using SwiftRuntimeLibrary;

namespace SwiftReflector {

	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ExceptionTests {
		void TLExceptionSimple (string toAdd, string output)
		{
			string swiftCode = $"public enum MyErrorTLES{toAdd} : Error {{\ncase itFailed\n}}\n" +
			    $"public func throwItTLES{toAdd}(doThrow: Bool) throws -> Int\n {{\n if doThrow {{\n throw MyErrorTLES{toAdd}.itFailed\n}}\nelse {{\n return 5\n}}\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock tryBlock = new CSCodeBlock ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
						      new CSFunctionCall ($"TopLevelEntities.ThrowItTLES{toAdd}", false, new CSIdentifier (toAdd)));
			tryBlock.Add (call);
			CSCodeBlock catchBlock = new CSCodeBlock ();
			catchBlock.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val ("exception thrown")));
			CSTryCatch catcher = new CSTryCatch (tryBlock, typeof (SwiftException), null, catchBlock);
			callingCode.Add (catcher);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : $"TLExceptionSimple{toAdd}");
		}

		[Test]
		public void TestTLExceptionNotThrown ()
		{
			TLExceptionSimple ("false", "5");
		}

		[Test]
		public void TestTLExceptionThrown ()
		{
			TLExceptionSimple ("true", "exception thrown");
		}


		void TLExceptionVoid (string toAdd, string output)
		{
			string swiftCode = $"public enum MyErrorTLEV{toAdd} : Error {{\ncase itFailed\n}}\n" +
			    $"public func throwItTLEV{toAdd}(doThrow: Bool) throws \n {{\n if doThrow {{\n throw  MyErrorTLEV{toAdd}.itFailed\n}}\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock tryBlock = new CSCodeBlock ();
			CSLine call = CSFunctionCall.FunctionCallLine ($"TopLevelEntities.ThrowItTLEV{toAdd}", false, new CSIdentifier (toAdd));
			CSLine writer = CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val ("no exception"));
			tryBlock.Add (call);
			tryBlock.Add (writer);
			CSCodeBlock catchBlock = new CSCodeBlock ();
			catchBlock.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val ("exception thrown")));
			CSTryCatch catcher = new CSTryCatch (tryBlock, typeof (SwiftException), null, catchBlock);
			callingCode.Add (catcher);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : $"TLExceptionVoid{toAdd}");
		}

		public void TestTLExceptionVoidNotThrown ()
		{
			TLExceptionVoid ("false", "no exception");
		}

		[Test]
		public void TestTLExceptionVoidThrown ()
		{
			TLExceptionVoid ("true", "exception thrown");
		}

		void TLExceptionSimpleOptional (string name, string toAdd, bool toReturn, string output)
		{
			string swiftCode = $"public enum MyErrorTLSO{name} : Error {{\ncase itFailed\n}}\n" +
			    $"public func throwItTLSO{name}(doThrow: Bool, doReturn: Bool) throws -> Int?\n {{\n if doThrow {{\n throw MyErrorTLSO{name}.itFailed\n}}\nelse {{\n if doReturn {{\n return 5\n}} else {{\nreturn nil\n}}\n}}\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock tryBlock = new CSCodeBlock ();
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
						      new CSFunctionCall ($"TopLevelEntities.ThrowItTLSO{name}", false, new CSIdentifier (toAdd),
							 CSConstant.Val (toReturn)).Dot (new CSFunctionCall ("ToString", false)));
			tryBlock.Add (call);
			CSCodeBlock catchBlock = new CSCodeBlock ();
			catchBlock.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val ("exception thrown")));
			CSTryCatch catcher = new CSTryCatch (tryBlock, typeof (SwiftException), null, catchBlock);
			callingCode.Add (catcher);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLExceptionSimpleOptional{name}");
		}

		[Test]
		public void TestTLExceptionOptionalNotThrownHasValue ()
		{
			TLExceptionSimpleOptional ("ONTHV", "false", true, "5");
		}

		[Test]
		public void TestTLExceptionOptionalNotThrownNoValue ()
		{
			TLExceptionSimpleOptional ("ONTNV", "false", false, "");
		}

		[Test]
		public void TestTLExceptionOptionalThrownHasValue ()
		{
			TLExceptionSimpleOptional ("OTHV", "true", true, "exception thrown");
		}
		[Test]
		public void TestTLExceptionOptionalThrownHasNoValue ()
		{
			TLExceptionSimpleOptional ("OTNV", "true", false, "exception thrown");
		}


		void MethodExceptionSimple (string toAdd, string output)
		{
			string swiftCode = $"public enum MyErrorMES{toAdd} : Error {{\ncase itFailed\n}}\n" +
			    $"public class FooMES{toAdd} {{\npublic init() {{\n}}\n public func throwItMES{toAdd}(doThrow: Bool) throws -> Int\n {{\n if doThrow {{\n throw MyErrorMES{toAdd}.itFailed\n}}\nelse {{\n return 5\n}}\n}}\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock tryBlock = new CSCodeBlock ();
			CSLine varLine = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooMES{toAdd}"), "foo", new CSFunctionCall ($"FooMES{toAdd}", true));
			tryBlock.Add (varLine);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
						      new CSFunctionCall ($"foo.ThrowItMES{toAdd}", false, new CSIdentifier (toAdd)));
			tryBlock.Add (call);
			CSCodeBlock catchBlock = new CSCodeBlock ();
			catchBlock.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val ("exception thrown")));
			CSTryCatch catcher = new CSTryCatch (tryBlock, typeof (SwiftException), null, catchBlock);
			callingCode.Add (catcher);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"MethodExceptionSimple{toAdd}");
		}

		[Test]
		public void MethodExceptionNotThrown ()
		{
			MethodExceptionSimple ("false", "5");
		}


		[Test]
		public void MethodExceptionThrown ()
		{
			MethodExceptionSimple ("true", "exception thrown");
		}



		void VirtualMethodExceptionSimple (string toAdd, string output)
		{
			string swiftCode = $"public enum MyErrorVMES{toAdd} : Error {{\ncase itFailed\n}}\n" +
			    $"open class FooVMES{toAdd} {{\npublic init() {{\n}}\n open func throwIt(doThrow: Bool) throws -> Int\n {{\n if doThrow {{\n throw MyErrorVMES{toAdd}.itFailed\n}}\nelse {{\n return 5\n}}\n}}\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock tryBlock = new CSCodeBlock ();
			CSLine varLine = CSVariableDeclaration.VarLine (new CSSimpleType ($"FooVMES{toAdd}"), "foo", new CSFunctionCall ($"FooVMES{toAdd}", true));
			tryBlock.Add (varLine);
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false,
							new CSFunctionCall ("foo.ThrowIt", false, new CSIdentifier (toAdd)));
			tryBlock.Add (call);
			CSCodeBlock catchBlock = new CSCodeBlock ();
			catchBlock.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("e").Dot (new CSIdentifier ("Message"))));
			CSTryCatch catcher = new CSTryCatch (tryBlock, typeof (SwiftException), "e", catchBlock);
			callingCode.Add (catcher);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"VirtualMethodExceptionSimple{toAdd}");
		}

		[Test]
		public void VirtualMethodExceptionNotThrown ()
		{
			VirtualMethodExceptionSimple ("false", "5");
		}


		[Test]
		public void VirtualMethodExceptionThrown ()
		{
			VirtualMethodExceptionSimple ("true", "Swift exception thrown: itFailed");
		}


		void VirtualMethodException (string toAdd, string output)
		{
			string swiftCode = $"public enum MyErrorVME{toAdd} : Error {{\ncase itFailed\n}}\n" +
			    $"open class FooVME{toAdd} {{\npublic init() {{\n}}\n public final func doIt(doThrow:Bool) throws {{\n let _ = try throwIt(doThrow:doThrow)\n}}\n open func throwIt(doThrow: Bool) throws -> Int\n {{\n if doThrow {{\n throw MyErrorVME{toAdd}.itFailed\n}}\nelse {{\n return 5\n}}\n}}\n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();

			CSClass fooOver = new CSClass (CSVisibility.Public, $"SubFooVME{toAdd}");
			fooOver.Inheritance.Add (new CSIdentifier ($"FooVME{toAdd}"));
			CSCodeBlock overBody = new CSCodeBlock ();
			CSCodeBlock ifblock = new CSCodeBlock ();
			CSCodeBlock elseblock = new CSCodeBlock ();
			ifblock.Add (new CSLine (new CSThrow (new CSFunctionCall ("ArgumentException", true, CSConstant.Val ("gotcha.")))));
			elseblock.Add (CSReturn.ReturnLine (CSConstant.Val (6)));
			CSIfElse ifElse = new CSIfElse (new CSIdentifier ("doThrow"), ifblock, elseblock);
			overBody.Add (ifElse);

			CSMethod myThrowIt = new CSMethod (CSVisibility.Public,
						      CSMethodKind.Override,
						      new CSSimpleType ("nint"),
						      new CSIdentifier ("ThrowIt"),
						      new CSParameterList (new CSParameter (CSSimpleType.Bool, new CSIdentifier ("doThrow"))), overBody);
			fooOver.Methods.Add (myThrowIt);

			CSCodeBlock tryBlock = new CSCodeBlock ();
			CSLine varLine = CSVariableDeclaration.VarLine (new CSSimpleType ($"SubFooVME{toAdd}"), "foo", new CSFunctionCall ($"SubFooVME{toAdd}", true));
			tryBlock.Add (varLine);
			CSLine call = CSFunctionCall.FunctionCallLine ("foo.DoIt", false, new CSIdentifier (toAdd));
			tryBlock.Add (call);
			CSCodeBlock catchBlock = new CSCodeBlock ();
			catchBlock.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("e").Dot (new CSIdentifier ("Message"))));
			CSTryCatch catcher = new CSTryCatch (tryBlock, typeof (SwiftException), "e", catchBlock);
			callingCode.Add (catcher);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"VirtualMethodException{toAdd}", otherClass: fooOver);
		}


		[Test]
		public void VirtualSubMethodExceptionNotThrown ()
		{
			VirtualMethodException ("false", "");
		}


		[Test]
		public void VirtualSubMethodExceptionThrown ()
		{
			VirtualMethodException ("true", "Swift exception thrown: gotcha.");
		}

	}
}
