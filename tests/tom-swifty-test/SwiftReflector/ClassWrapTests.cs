using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using SwiftReflector.Inventory;
using tomwiftytest;
using SwiftReflector.IOUtils;
using Dynamo.CSLang;
using SwiftReflector.Demangling;


namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ClassWrapTests {

		[Test]
		public void TestForFinality ()
		{
			string simpleClass = "public class Garble { public final func finalmethod() { }; public func virtmethod() { } }";
			using (DisposableTempFile montyLib = new DisposableTempFile ("libXython.dylib", false)) {

				Compiler.CompileStringToFileUsing (null, XCodeCompiler.SwiftcCustom, simpleClass, " -emit-library -module-name Xython", montyLib);
				var errors = new ErrorHandling ();
				ModuleInventory inventory = ModuleInventory.FromFile (montyLib.Filename, errors);
				Utils.CheckErrors (errors);
				ClassContents cl = inventory.ClassesForName (new SwiftName ("Xython", false)).FirstOrDefault ();
				Assert.IsNotNull (cl);

				if (cl.WitnessTable == null || cl.WitnessTable.MangledNames == null ||
				    cl.WitnessTable.MangledNames.Count () == 0)
					return;

				foreach (var oi in cl.Methods.Values) {
					foreach (TLFunction f in oi.Functions) {
						if (f.MangledName.Contains ("finalmethod")) {
							Assert.IsTrue (cl.IsFinal (f));
						} else if (f.MangledName.Contains ("virtmethod")) {
							Assert.IsFalse (cl.IsFinal (f));
						}
					}
				}
			}

		}



		void WrapSingleMethod (string type, string returnVal, string expected)
		{
			string swiftCode = String.Format ("public final class MontyWSM{2} {{ class InnerMonty {{\n}}\npublic init() {{}}\n public func val() -> {0} {{ return {1}; }} }}",
							 type, returnVal, type);


			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Ctor ($"MontyWSM{type}").Dot (CSFunctionCall.Function ("Val"))));

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapSingleMethod{type}");
		}

		[Test]
		public void WrapSingleMethodBool ()
		{
			WrapSingleMethod ("Bool", "true", "True\n");
		}

		[Test]
		public void WrapSingleMethodInt ()
		{
			WrapSingleMethod ("Int", "42", "42\n");
		}

		[Test]
		public void WrapSingleMethodUInt ()
		{
			WrapSingleMethod ("UInt", "42", "42\n");
		}

		[Test]
		public void WrapSingleMethodFloat ()
		{
			WrapSingleMethod ("Float", "42.1", "42.1\n");
		}

		[Test]
		public void WrapSingleMethodDouble ()
		{
			WrapSingleMethod ("Double", "42.1", "42.1\n");
		}

		[Test]
		public void WrapSingleMethodString ()
		{
			WrapSingleMethod ("String", "\"a string\"", "a string\n");
		}

		[Test]
		public void WrapSingleMethodAnyObject()
		{
			WrapSingleMethod ("AnyObject", "InnerMonty()", "SwiftRuntimeLibrary.SwiftAnyObject\n");
		}

		void WrapMultipleMethod (string type, string return1, string return2, string expected)
		{
			string swiftCode = String.Format ("public final class MontyWMM{3} {{ public init() {{ }}\n public func val() -> {0} {{ return {1}; }}; public func val1() -> {0} {{ return {2}; }}}}",
						 type, return1, return2, type);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("{0} {1}"),
			                                                  CSFunctionCall.Ctor ($"MontyWMM{type}").Dot (CSFunctionCall.Function ("Val")), 
			                                                  CSFunctionCall.Ctor ($"MontyWMM{type}").Dot (CSFunctionCall.Function ("Val1"))));
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapMultiMethod{type}");
		}


		[Test]
		public void WrapMultiMethodBool ()
		{
			WrapMultipleMethod ("Bool", "true", "false", "True False\n");
		}


		[Test]
		public void WrapMultiMethodInt ()
		{
			WrapMultipleMethod ("Int", "42", "43", "42 43\n");
		}

		[Test]
		public void WrapMultiMethodUInt ()
		{
			WrapMultipleMethod ("UInt", "42", "43", "42 43\n");
		}


		[Test]
		public void WrapMultiMethodFloat ()
		{
			WrapMultipleMethod ("Float", "42.5", "43.5", "42.5 43.5\n");
		}


		[Test]
		public void WrapMultiMethodDouble ()
		{
			WrapMultipleMethod ("Double", "42.5", "43.5", "42.5 43.5\n");
		}




		void WrapSingleProperty (string type, string returnVal, string reassignVal, string expected)
		{
			string swiftCode = String.Format ("public final class MontyWSP{2} {{ public init() {{ }}\npublic var val:{0} = {1}; }}",
							 type, returnVal, type);

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> {
				CSAssignment.Assign ("var nothing", CSFunctionCall.Ctor ($"MontyWSP{type}")),
				CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("nothing").Dot ((CSIdentifier)"Val")),
				CSAssignment.Assign ("nothing.Val", new CSConstant (reassignVal)),
				CSFunctionCall.ConsoleWriteLine (CSIdentifier.Create ("nothing").Dot ((CSIdentifier)"Val"))
			};

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"WrapSingleProperty{type}");
		}


		[Test]
		public void SinglePropGetterInt ()
		{
			WrapSingleProperty ("Int", "42", "43", "42\n43\n");
		}


		[Test]
		public void SinglePropGetterUInt ()
		{
			WrapSingleProperty ("UInt", "42", "43", "42\n43\n");
		}



		[Test]
		public void SinglePropGetterBool ()
		{
			WrapSingleProperty ("Bool", "true", "false", "True\nFalse\n");
		}

		[Test]
		public void SinglePropGetterFloat ()
		{
			WrapSingleProperty ("Float", "42.5", "14.5f", "42.5\n14.5\n");
		}

		[Test]
		public void SinglePropGetterDouble ()
		{
			WrapSingleProperty ("Double", "42.5", "14.5", "42.5\n14.5\n");
		}


		[Test]
		public void WrapClassReturningClass ()
		{
			string classOne = "public final class GarbleWCRC { public init() { }\npublic func success() { var s = \"\"\n print(\"success\", to:&s)\nwriteToFile(s, \"WrapClassReturningClass\")\n  } }\n";
			string classTwo = "public final class MontyWCRC { public init() { }\npublic func doIt() -> GarbleWCRC { return GarbleWCRC(); } }";
			string swiftCode = TestRunningCodeGenerator.kSwiftFileWriter + classOne + classTwo;

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSAssignment.Assign ("var monty", new CSFunctionCall ("MontyWCRC", true)));
			callingCode.Add (CSAssignment.Assign ("var garb", new CSFunctionCall ("monty.DoIt", false)));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("garb.Success", false));

			TestRunning.TestAndExecute (swiftCode, callingCode, "success\n");
		}

	}
}

