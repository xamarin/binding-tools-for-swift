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
using System.Runtime.InteropServices;
using System.Reflection;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class ClosureTests {


		[Test]
		public void ClosureSmokeTest ()
		{
			string swiftCode =
				"public func callClosureCST(f:@escaping()->()) {\n" +
				"    f()\n" +
				"}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("C# output")));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("TopLevelEntities.CallClosureCST", false,
														 new CSLambda (new CSParameterList (), body));
			callingCode.Add (invoker);
			TestRunning.TestAndExecute (swiftCode, callingCode, "C# output\n");
		}


		void ClosureSmokeTest1 (string appendage, string swiftType, string csVal, string output)
		{
			string swiftCode =
				$"public func callClosureCST1{appendage}(a:{swiftType}, f:@escaping({swiftType})->()) {{\n" +
				"    f(a)\n" +
				"}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"val"));
			CSLine invoker = CSFunctionCall.FunctionCallLine ($"TopLevelEntities.CallClosureCST1{appendage}", false,
															     new CSIdentifier (csVal),
															     new CSLambda (body, "val"));
			callingCode.Add (invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureSmokeTest1{appendage}");
		}

		[Test]
		public void BoolActionClosure ()
		{
			ClosureSmokeTest1 ("BoolT", "Bool", "true", "True\n");
			ClosureSmokeTest1 ("BoolF", "Bool", "false", "False\n");
		}


		[Test]
		public void IntActionClosure ()
		{
			ClosureSmokeTest1 ("IntPos", "Int", "42", "42\n");
			ClosureSmokeTest1 ("IntNeg", "Int", "-42", "-42\n");
		}




		[Test]
		public void FloatActionClosure ()
		{
			ClosureSmokeTest1 ("FloatPos", "Float", "42.1f", "42.1\n");
			ClosureSmokeTest1 ("FloatNeg", "Float", "-42.1f", "-42.1\n");
		}


		[Test]
		public void DoubleActionClosure ()
		{
			ClosureSmokeTest1 ("DoublePos", "Double", "42.1", "42.1\n");
			ClosureSmokeTest1 ("DoubleNeg", "Double", "-42.1", "-42.1\n");
		}



		[Test]
		public void StringActionClosure ()
		{
			ClosureSmokeTest1 ("String", "String", "SwiftString.FromString(\"hi mom\")", "hi mom\n");
		}


		void ClosureIdentityFunc (string appendage, string swiftType, string csVal, string output, bool escaping)
		{
			appendage += escaping.ToString ();
			string escapingAttribute = escaping ? "@escaping" : "";
			string swiftCode =
				$"public func callClosureCIF{appendage}(a:{swiftType}, f:{escapingAttribute}({swiftType})->{swiftType}) -> {swiftType} {{\n" +
				"    return f(a)\n" +
				"}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSReturn.ReturnLine (new CSIdentifier ("val")));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"TopLevelEntities.CallClosureCIF{appendage}", (CSIdentifier)csVal, 
			                                                                           new CSLambda (body, "val")));
			callingCode.Add (invoker);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureIdentityFunc{appendage}");
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void IntIdentFunc (bool escaping)
		{
			ClosureIdentityFunc ("IntPos", "Int", "42", "42\n", escaping);
			ClosureIdentityFunc ("IntNeg", "Int", "-42", "-42\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void UIntIdentFunc (bool escaping)
		{
			ClosureIdentityFunc ("UInt", "UInt", "42", "42\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void BoolIdentFunc (bool escaping)
		{
			ClosureIdentityFunc ("BoolT", "Bool", "true", "True\n", escaping);
			ClosureIdentityFunc ("BoolF", "Bool", "false", "False\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FloatIdentFunc (bool escaping)
		{
			ClosureIdentityFunc ("FloatPos", "Float", "42.1f", "42.1\n", escaping);
			ClosureIdentityFunc ("FloatNeg", "Float", "-42.1f", "-42.1\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void DoubleIdentFunc (bool escaping)
		{
			ClosureIdentityFunc ("DoublePos", "Double", "42.1", "42.1\n", escaping);
			ClosureIdentityFunc ("DoubleNeg", "Double", "-42.1", "-42.1\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void StringIdentFunc (bool escaping)
		{
			ClosureIdentityFunc ("String", "String", "SwiftString.FromString(\"hi mom\")", "hi mom\n", escaping);
		}


		void ClosureConstantFunc (string appendage, string swiftType, string csVal, string output, bool escaping)
		{
			appendage += escaping.ToString ();
			string escapingAttribute = escaping ? "@escaping" : "";
			string swiftCode =
				$"public func callClosureCCF{appendage}(f:{escapingAttribute}()->{swiftType}) -> {swiftType} {{\n" +
				"    return f()\n" +
				"}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSReturn.ReturnLine (new CSIdentifier (csVal)));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"TopLevelEntities.CallClosureCCF{appendage}", new CSLambda (body)));
			callingCode.Add (invoker);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureConstantFunc{appendage}");
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void IntConstFunc (bool escaping)
		{
			ClosureConstantFunc ("IntPos", "Int", "42", "42\n", escaping);
			ClosureConstantFunc ("IntNeg", "Int", "-42", "-42\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void UIntConstFunc (bool escaping)
		{
			ClosureConstantFunc ("UInt", "UInt", "42", "42\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void BoolConstFunc (bool escaping)
		{
			ClosureConstantFunc ("BoolT", "Bool", "true", "True\n", escaping);
			ClosureConstantFunc ("BoolF", "Bool", "false", "False\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FloatConstFunc (bool escaping)
		{
			ClosureConstantFunc ("FloatPos", "Float", "42.1f", "42.1\n", escaping);
			ClosureConstantFunc ("FloatNeg", "Float", "-42.1f", "-42.1\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void DoubleConstFunc (bool escaping)
		{
			ClosureConstantFunc ("DoublePos", "Double", "42.1", "42.1\n", escaping);
			ClosureConstantFunc ("DoubleNeg", "Double", "-42.1", "-42.1\n", escaping);
		}



		void ClosureActionTwofer (string swiftType, string csVal1, string csVal2, string output, bool escaping)
		{
			string escapingAttribute = escaping ? "@escaping" : "";
			string swiftCode =
				$"public func callClosureCAT{swiftType}{escaping}(a: {swiftType}, b: {swiftType}, f:{escapingAttribute}({swiftType}, {swiftType})->()) -> () {{\n" +
				"    f(a, b)\n" +
				"}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("{0}, {1}"), (CSIdentifier)"val1", (CSIdentifier)"val2"));
			CSLine invoker = CSFunctionCall.FunctionCallLine ($"TopLevelEntities.CallClosureCAT{swiftType}{escaping}", false,
															     new CSIdentifier (csVal1),
															     new CSIdentifier (csVal2),
															     new CSLambda (body, "val1", "val2"));
			callingCode.Add (invoker);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureActionTwofer{swiftType}{escaping}");
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ActionTwoferInt (bool escaping)
		{
			ClosureActionTwofer ("Int", "42", "-42", "42, -42\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ActionTwoferUInt (bool escaping)
		{
			ClosureActionTwofer ("UInt", "42", "52", "42, 52\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ActionTwoferBool (bool escaping)
		{
			ClosureActionTwofer ("Bool", "true", "false", "True, False\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ActionTwoferFloat (bool escaping)
		{
			ClosureActionTwofer ("Float", "42.1f", "-42.1f", "42.1, -42.1\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ActionTwoferDouble (bool escaping)
		{
			ClosureActionTwofer ("Double", "42.1", "-42.1", "42.1, -42.1\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ActionTwoferString (bool escaping)
		{
			ClosureActionTwofer ("String", "SwiftString.FromString(\"hello mudda\")",
			                     "SwiftString.FromString(\"hello fadda\")", "hello mudda, hello fadda\n", escaping);
		}


		void ClosureSumFunc (string swiftType, string csVal1, string csVal2, string output, bool escaping)
		{
			string escapingAttribute = escaping ? "@escaping" : "";
			string swiftCode =
				$"public func sumIt{swiftType}{escaping}(a:{swiftType}, b:{swiftType}, f:{escapingAttribute}({swiftType},{swiftType})->{swiftType}) -> {swiftType} {{\n" +
				"    return f(a, b)\n" +
				"}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSReturn.ReturnLine (new CSIdentifier ("val1") + new CSIdentifier ("val2")));
			CSLine invoker = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"TopLevelEntities.SumIt{swiftType}{escaping}", 
			                                                                           (CSIdentifier)csVal1, (CSIdentifier)csVal2, new CSLambda (body, "val1", "val2")));
			callingCode.Add (invoker);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureSumFunc{swiftType}{escaping}");
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FuncTwoferSumInt (bool escaping)
		{
			ClosureSumFunc ("Int", "42", "-42", "0\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FuncTwoferSumUInt (bool escaping)
		{
			ClosureSumFunc ("UInt", "42", "42", "84\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FuncTwoferSumFloat (bool escaping)
		{
			ClosureSumFunc ("Float", "42.1f", "42.1f", "84.2\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void FuncTwoferSumDouble (bool escaping)
		{
			ClosureSumFunc ("Double", "42.1", "42.1", "84.2\n", escaping);
		}

		void MakeHandler (CSClass cl, string name, string propName)
		{
			CSSimpleType evtType = new CSSimpleType (typeof (EventArgs));
			CSProperty prop = CSProperty.PublicGetSet (evtType, propName);
			cl.Properties.Add (prop);
			CSParameterList pl = new CSParameterList ();
			pl.Add (new CSParameter (CSSimpleType.Object, "sender"));
			pl.Add (new CSParameter (evtType, "args"));
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSAssignment.Assign (propName, CSAssignmentOperator.Assign, new CSIdentifier ("args")));
			CSMethod meth = new CSMethod (CSVisibility.Public, CSMethodKind.None,
									 CSSimpleType.Void, new CSIdentifier (name), pl, body);
			cl.Methods.Add (meth);
		}

		void MakeCapsuleAccessor (CSClass cl)
		{
			CSParameterList pl = new CSParameterList ();
			pl.Add (new CSParameter (new CSSimpleType (typeof (EventArgs)), "evt"));
			CSCodeBlock body = new CSCodeBlock ();
			// System.Reflection.PropertyInfo pi = evt.GetType().GetProperty("Capsule");
			// return pi.GetValue(evt);
			body.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("System.Reflection.PropertyInfo"),
												 "pi",
												 new CSFunctionCall ("evt.GetType().GetProperty", false,
									      CSConstant.Val ("Capsule"))));
			body.Add (CSReturn.ReturnLine (new CSFunctionCall ("pi.GetValue", false, new CSIdentifier ("evt"))));


			CSMethod meth = new CSMethod (CSVisibility.Public, CSMethodKind.None,
									 CSSimpleType.Object, new CSIdentifier ("GetCapsule"), pl, body);
			cl.Methods.Add (meth);
		}

		void ClosureEventTest (string swiftType, string csVal, string output, bool escaping)
		{
			string escapingAttribute = escaping ? "@escaping" : "";
			string swiftCode =
				$"public func callClosure_{escaping}(a:{swiftType}, f:{escapingAttribute}({swiftType})->()) {{\n" +
				"    f(a)\n" +
				"}";

			var eventerName = $"Eventer_{escaping}";
			CSClass eventer = new CSClass (CSVisibility.Public, eventerName);
			MakeHandler (eventer, "AllocHandler", "AllocArgs");
			MakeHandler (eventer, "DeInitHandler", "DeInitArgs");
			MakeCapsuleAccessor (eventer);

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			// SwiftDotNetCapsule.CapsuleTrackArgs allocArgs;
			// SwiftDotNetCapsule.CapsuleTrackArgs deinitArgs;
			// EventInfo ei = typeof(SwiftDotNetCapsule).GetEvent("AllocCalled", System.Reflection.BindingFlags.Static);
			// ei.AddEventHandler(null, et.AllocHandler);
			// SwiftDotNetCapsule.AllocCalled += (s, e) => {
			//     allocArgs = e;
			// };
			// SwiftDotNetCapsule.DeInitCalled += (s, e) => {
			//     deinitArgs = e;
			// };
			// ...
			// Console.WriteLine((allocArgs != null) && (allocArgs == deinitArgs));

			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"val"));

			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType (eventerName), new CSIdentifier ("et"),
														new CSFunctionCall (eventerName, true)));


			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Action<object, EventArgs>"), "allocDel",
														new CSIdentifier ("et.AllocHandler")));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("System.Reflection.EventInfo"), new CSIdentifier ("allocEI"),
														new CSFunctionCall ("typeof(SwiftDotNetCapsule).GetEvent", false,
										     CSConstant.Val ("AllocCalled"),
										     new CSIdentifier ("System.Reflection.BindingFlags.Static") |
										     new CSIdentifier ("System.Reflection.BindingFlags.NonPublic"))));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType (typeof (Delegate)), "allocDelCvt",
								    new CSFunctionCall ("Delegate.CreateDelegate", false,
										     new CSIdentifier ("allocEI.EventHandlerType"),
										     new CSIdentifier ("allocDel.Target"),
										     new CSIdentifier ("allocDel.Method"))));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("System.Reflection.MethodInfo"), new CSIdentifier ("allocMI"),
														new CSFunctionCall ("allocEI.GetAddMethod", false, CSConstant.Val (true))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("allocMI.Invoke", false,
														  CSConstant.Null,
														  new CSArray1DInitialized ("object", new CSIdentifier ("allocDelCvt"))));


			//callingCode.Add(FunctionCall.FunctionCallLine("allocEI.AddEventHandler", false, Constant.Null,
			//                                              new Identifier("allocDelCvt")));


			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Action<object, EventArgs>"), "deInitDel",
														new CSIdentifier ("et.DeInitHandler")));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("System.Reflection.EventInfo"), new CSIdentifier ("deInitEI"),
														new CSFunctionCall ("typeof(SwiftDotNetCapsule).GetEvent", false,
																		 CSConstant.Val ("DeInitCalled"),
										     new CSIdentifier ("System.Reflection.BindingFlags.Static") |
										     new CSIdentifier ("System.Reflection.BindingFlags.NonPublic"))));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType (typeof (Delegate)), "deInitDelCvt",
								    new CSFunctionCall ("Delegate.CreateDelegate", false,
																		 new CSIdentifier ("deInitEI.EventHandlerType"),
										     new CSIdentifier ("deInitDel.Target"),
										     new CSIdentifier ("deInitDel.Method"))));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("System.Reflection.MethodInfo"), new CSIdentifier ("deInitMI"),
														new CSFunctionCall ("allocEI.GetAddMethod", false, CSConstant.Val (true))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("deInitMI.Invoke", false,
														  CSConstant.Null,
														  new CSArray1DInitialized ("object", new CSIdentifier ("deInitDelCvt"))));

			//callingCode.Add(FunctionCall.FunctionCallLine("deInitEI.AddEventHandler", false, Constant.Null,
			//                                              new Identifier("deInitDelCvt")));




			//callingCode.Add(Assignment.Assign("SwiftDotNetCapsule.AllocCalled", AssignmentOp.AddAssign,
			//								  new Identifier("et.AllocHandler")));
			//callingCode.Add(Assignment.Assign("SwiftDotNetCapsule.DeInitCalled", AssignmentOp.AddAssign,
			//								  new Identifier("et.DeInitHandler")));


			CSLine invoker = CSFunctionCall.FunctionCallLine ($"TopLevelEntities.CallClosure_{escaping}", false,
														 new CSIdentifier (csVal),
														 new CSLambda (body, "val"));
			callingCode.Add (invoker);
			callingCode.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"et.GetCapsule(et.AllocArgs)" == (CSIdentifier)"et.GetCapsule(et.DeInitArgs)"));
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureEventTest{escaping}", otherClass: eventer);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ClosureEventCalledBalanceBool (bool escaping)
		{
			ClosureEventTest ("Bool", "false", "False\nTrue\n", escaping);
		}


		void ClosureClassTest (string appendage, string swiftType, string csVal, string output, bool escaping)
		{
			appendage += escaping.ToString ();
			string escapingAttribute = escaping ? "@escaping" : "";
			string swiftCode =
		$"public final class FooCCT{appendage} {{\n" +
				" public init() { }\n" +
				$" public func runIt(f:{escapingAttribute}()->{swiftType}) -> {swiftType}\n" +
				"{ return f(); }\n" +
				"}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ($"FooCCT{appendage}"), new CSIdentifier ("x"),
								    new CSFunctionCall ($"FooCCT{appendage}", true)));

			CSLine invoker = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("x.RunIt", new CSLambda ((CSIdentifier)csVal)));
			callingCode.Add (invoker);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"ClosureClassTest{appendage}");
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ClosureClassBool (bool escaping)
		{
			ClosureClassTest ("BoolT", "Bool", "true", "True\n", escaping);
			ClosureClassTest ("BoolF", "Bool", "false", "False\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ClosureClassInt (bool escaping)
		{
			ClosureClassTest ("Int32Pos", "Int32", "42", "42\n", escaping);
			ClosureClassTest ("Int32Neg", "Int32", "-42", "-42\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ClosureClassUInt (bool escaping)
		{
			ClosureClassTest ("UInt32", "UInt32", "42", "42\n", escaping);
		}


		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ClosureClassFloat (bool escaping)
		{
			ClosureClassTest ("Float", "Float", "42.1f", "42.1\n", escaping);
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void ClosureClassDouble (bool escaping)
		{
			ClosureClassTest ("Double", "Double", "42.1", "42.1\n", escaping);
		}

		[Test]
		[TestCase(true)]
		[TestCase (false)]
		public void ClosureClassString (bool escaping)
		{
			ClosureClassTest ("String", "String", "SwiftString.FromString(\"hi mom\")", "hi mom\n", escaping);
		}


		[Test]
		public void ClosureIdentity ()
		{
			var swiftCode =
				"public func closureIdentity(a: @escaping () -> ()) -> () -> () {\n" +
				"    return a\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Success")));
			var lambda = new CSLambda (body);
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Action"), "lam", lambda));
			callingCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Action"), "lamreturn",
									new CSFunctionCall ("TopLevelEntities.ClosureIdentity", false, new CSIdentifier ("lam"))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("lamreturn", false));

			TestRunning.TestAndExecute (swiftCode, callingCode, "Success\n");
		}

		[Test]
		public void ClosureActionStringString ()
		{
			var swiftCode =
				"public func closureIdentityStringString (a: @escaping (String, String)->()) -> (String, String) -> () {\n" +
				" return a\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("stra.ToString") + CSFunctionCall.Function ("strb.ToString")));
			var lambda = new CSLambda (body, "stra", "strb");
			var returnType = new CSSimpleType ("Action", false, new CSSimpleType ("SwiftString"), new CSSimpleType ("SwiftString"));
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lam", lambda));
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lamreturn",
			                                                new CSFunctionCall ("TopLevelEntities.ClosureIdentityStringString", false, new CSIdentifier ("lam"))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("lamreturn", false, new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("hi ")),
			                                                  new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("mom"))));

			TestRunning.TestAndExecute (swiftCode, callingCode, "hi mom\n");
		}


		[Test]
		public void ClosureIdentityBool ()
		{
			var swiftCode =
				"public func closureIdentityBool(a: @escaping () -> (Bool)) -> () -> (Bool) {\n" +
				"    return a\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("True")));
			body.Add (CSReturn.ReturnLine (CSConstant.Val (true)));
			var lambda = new CSLambda (body);
			var returnType = new CSSimpleType ("Func", false, CSSimpleType.Bool);
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lam", lambda));
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lamreturn",
									new CSFunctionCall ("TopLevelEntities.ClosureIdentityBool", false, new CSIdentifier ("lam"))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("lamreturn", false));

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

	
		[Test]
		public void ClosureIdentityIntBool ()
		{
			var swiftCode =
				"public func closureIdentityIntBool(a: @escaping (Int) -> Bool) -> (Int) -> (Bool) {\n" +
				"    return a\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var body = new CSCodeBlock ();
			body.Add (CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"i"));
			body.Add (CSReturn.ReturnLine (CSConstant.Val (true)));
			var lambda = new CSLambda (body, "i");
			var returnType = new CSSimpleType ("Func", false, new CSSimpleType ("nint"), CSSimpleType.Bool);
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lam", lambda));
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lamreturn",
			                                                new CSFunctionCall ("TopLevelEntities.ClosureIdentityIntBool", false, new CSIdentifier ("lam"))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("lamreturn", false, CSConstant.Val (17)));

			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n");
		}

		[Test]
		public void ClosureIdentityDoubleDoubleDouble ()
		{
			var swiftCode =
				"public func closureIdentityDoubleDoubleDouble(a: @escaping (Double, Double) -> Double) -> (Double, Double) -> Double {\n" +
				"    return a\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var body = new CSCodeBlock ();
			var aId = new CSIdentifier ("a");
			var bId = new CSIdentifier ("b");
			body.Add (CSFunctionCall.ConsoleWriteLine (aId + bId));
			body.Add (CSReturn.ReturnLine (aId + bId));
			var lambda = new CSLambda (body, "a", "b");
			var returnType = new CSSimpleType ("Func", false, CSSimpleType.Double, CSSimpleType.Double, CSSimpleType.Double);
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lam", lambda));
			callingCode.Add (CSVariableDeclaration.VarLine (returnType, "lamreturn",
			                                                new CSFunctionCall ("TopLevelEntities.ClosureIdentityDoubleDoubleDouble", false, new CSIdentifier ("lam"))));
			callingCode.Add (CSFunctionCall.FunctionCallLine ("lamreturn", false, CSConstant.Val (3.1), CSConstant.Val (4.0)));

			TestRunning.TestAndExecute (swiftCode, callingCode, "7.1\n");
		}

		[Test]
		public void HandlesAutoclosure ()
		{
			var swiftCode =
				"public func autoClosureCheck(a: @autoclosure ()->Bool) -> Bool {\n" +
				"    return a()\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();

			var body = new CSCodeBlock ();
			body.Add (CSReturn.ReturnLine (CSConstant.Val (true)));
			var lambda = new CSLambda (body);

			var printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.AutoClosureCheck", lambda));
			callingCode.Add (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void HandlesSimpleClosureWithNoArgs ()
		{
			var swiftCode = @"
public func simpleReturnClosure () -> (Int32)->Bool {
	return { (a:Int32) in
	    return a % 2 == 0
	}
}
";

			var closID = new CSIdentifier ("cl");
			var closDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, closID, new CSFunctionCall ("TopLevelEntities.SimpleReturnClosure", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall (closID.Name, false, CSConstant.Val (42)));
			var callingCode = CSCodeBlock.Create (closDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void HandlesSimpleClosureInClassWithNoArgs ()
		{
			var swiftCode = @"
public class SimpleReturnClosureClass {
	public init () { }
	public func getClosure () -> (Int32)->Bool {
		return { (a: Int32) in
			return a % 2 == 0
		}
	}
}
";
			var classID = new CSIdentifier ("cl");
			var classDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, classID, new CSFunctionCall ("SimpleReturnClosureClass", true));
			var closID = new CSIdentifier ("clos");
			var closDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, closID, new CSFunctionCall ($"{classID.Name}.GetClosure", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall (closID.Name, false, CSConstant.Val (42)));
			var callingCode = CSCodeBlock.Create (classDecl, closDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}
	}
}
