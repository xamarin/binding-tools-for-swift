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
using SwiftRuntimeLibrary;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class ProtocolTests {
		void WrapSingleMethod (string type, string csType, string csReplacement, string expected)
		{
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
			    $"public protocol MontyWSM{type} {{ func val() -> {type}\n  }}\n" +
				       $"public class TestMontyWSM{type} {{\npublic init() {{ }}\npublic func doIt(m:MontyWSM{type}) {{\nvar s = \"\"\nprint(m.val(), to:&s)\nwriteToFile(s, \"WrapSingleMethod{type}\")\n}}\n}}\n";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSM{type}");
			overCS.Inheritance.Add (new CSIdentifier ($"IMontyWSM{type}"));
			CSCodeBlock overBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSIdentifier (csReplacement)));
			CSMethod overMeth = new CSMethod (CSVisibility.Public, CSMethodKind.None, new CSSimpleType (csType),
			    new CSIdentifier ("Val"), new CSParameterList (), overBody);
			overCS.Methods.Add (overMeth);
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSM{type}"), "myOver", new CSFunctionCall ($"OverWSM{type}", true));
			CSLine decl1 = CSVariableDeclaration.VarLine (new CSSimpleType ($"TestMontyWSM{type}"), "tester", new CSFunctionCall ($"TestMontyWSM{type}", true));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("tester.DoIt", false, new CSIdentifier ("myOver"));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, decl1, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapSingleMethod{type}", otherClass: overCS, platform: PlatformName.macOS);
		}

		[Test]
		public void WrapSingleMethodBool ()
		{
			WrapSingleMethod ("Bool", "bool", "false", "false\n");
		}

		[Test]
		public void WrapSingleMethodUInt64 ()
		{
			WrapSingleMethod ("UInt64", "ulong", "42", "42\n");
		}

		[Test]
		public void WrapSingleMethodInt64 ()
		{
			WrapSingleMethod ("Int64", "long", "42", "42\n");
		}

		[Test]
		public void WrapSingleMethodFloat ()
		{
			WrapSingleMethod ("Float", "float", "42.1f", "42.1\n");
		}

		[Test]
		public void WrapSingleMethodDouble ()
		{
			WrapSingleMethod ("Double", "double", "42.1", "42.1\n");
		}

		[Test]
		public void WrapSingleMethodString ()
		{
			WrapSingleMethod ("String", "SwiftString", "SwiftString.FromString(\"Hi mom\")", "Hi mom\n");
		}

		void WrapSingleSubscriptGetOnly (string type, string csType, string csReplacement, string csAlt, string expected)
		{
			string swiftCode =
		TestRunningCodeGenerator.kSwiftFileWriter +
		$"public protocol MontyWSGO{type} {{ subscript(i:Int32) -> {type} {{ get }} \n  }}\n" +
			   $"public class TestMontyWSGO{type} {{\npublic init() {{ }}\npublic func doIt(m:MontyWSGO{type}) {{\nvar s = \"\", t=\"\"\nprint(m[0], to:&s)\nprint(m[1], to:&t)\nwriteToFile(s+t, \"WrapSingleSubscriptGetOnly{type}\")\n}}\n}}\n";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSGO{type}");
			overCS.Inheritance.Add (new CSIdentifier ($"IMontyWSGO{type}"));
			CSParameterList overParams = new CSParameterList ();
			overParams.Add (new CSParameter (CSSimpleType.Int, "i"));
			CSCodeBlock overBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSTernary(new CSIdentifier("i") == CSConstant.Val(0),
		    	new CSIdentifier(csReplacement), new CSIdentifier(csAlt), false)));
			CSProperty overProp = new CSProperty (new CSSimpleType (csType), CSMethodKind.None, CSVisibility.Public,
			    overBody, CSVisibility.Public, null, overParams);

			overCS.Properties.Add (overProp);

			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSGO{type}"), "myOver", new CSFunctionCall ($"OverWSGO{type}", true));
			CSLine decl1 = CSVariableDeclaration.VarLine (new CSSimpleType ($"TestMontyWSGO{type}"), "tester", new CSFunctionCall ($"TestMontyWSGO{type}", true));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("tester.DoIt", false, new CSIdentifier ("myOver"));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, decl1, invoker);


			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapSingleSubscriptGetOnly{type}", otherClass : overCS);
		}

		[Test]
		public void WrapSingleSubscriptGetOnlyBool ()
		{
			WrapSingleSubscriptGetOnly ("Bool", "bool", "false", "true", "false\ntrue\n");
		}

		[Test]
		public void WrapSingleSubscriptGetOnlyInt ()
		{
			WrapSingleSubscriptGetOnly ("Int32", "int", "43", "-40", "43\n-40\n");
		}

		[Test]
		public void WrapSingleSubscriptGetOnlyUInt ()
		{
			WrapSingleSubscriptGetOnly ("UInt32", "uint", "(uint)43", "(uint)40", "43\n40\n");
		}

		[Test]
		public void WrapSingleSubscriptGetOnlyDouble ()
		{
			WrapSingleSubscriptGetOnly ("Double", "double", "43.0", "40.0", "43.0\n40.0\n");
		}

		[Test]
		public void WrapSingleSubscriptGetOnlyFloat ()
		{
			WrapSingleSubscriptGetOnly ("Float", "float", "43.0f", "40.0f", "43.0\n40.0\n");
		}

		[Test]
		public void WrapSingleSubscriptGetOnlyString ()
		{
			WrapSingleSubscriptGetOnly ("String", "SwiftString", "SwiftString.FromString(\"one\")",
							   "SwiftString.FromString(\"two\")", "one\ntwo\n");
		}

		void WrapSinglePropertyGetOnly (string appendage, string type, string csType, string csReplacement, string expected)
		{
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
				       $"public protocol MontyWSPGO{type} {{ var prop : {type} {{ get }} \n  }}\n" +
				       $"public class TestMontyWSPGO{type} {{\npublic init() {{ }}\npublic func doIt(m:MontyWSPGO{type}) {{\nvar s = \"\"\nprint(m.prop, to:&s)\nwriteToFile(s, \"WrapSinglePropertyGetOnly{appendage}\")\n}}\n}}\n";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSPGO{type}");
			overCS.Inheritance.Add (new CSIdentifier ($"IMontyWSPGO{type}"));
			CSCodeBlock overBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSIdentifier (csReplacement)));
			CSProperty overProp = new CSProperty (new CSSimpleType (csType), CSMethodKind.None, new CSIdentifier ("Prop"),
						CSVisibility.Public, overBody, CSVisibility.Public, null);

			overCS.Properties.Add (overProp);

			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSPGO{type}"), "myOver", new CSFunctionCall ($"OverWSPGO{type}", true));
			CSLine decl1 = CSVariableDeclaration.VarLine (new CSSimpleType ($"TestMontyWSPGO{type}"), "tester", new CSFunctionCall ($"TestMontyWSPGO{type}", true));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("tester.DoIt", false, new CSIdentifier ("myOver"));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, decl1, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapSinglePropertyGetOnly{appendage}", otherClass : overCS, platform: PlatformName.macOS);
		}

		[Test]
		public void WrapSinglePropGetOnlyBool ()
		{
			WrapSinglePropertyGetOnly ("Bool", "Bool", "bool", "false", "false\n");
		}

		[Test]
		public void WrapSinglePropGetOnlyInt32 ()
		{
			WrapSinglePropertyGetOnly ("Int", "Int32", "int", "5", "5\n");
		}

		[Test]
		public void WrapSinglePropGetOnlyUInt32 ()
		{
			WrapSinglePropertyGetOnly ("UInt", "UInt32", "uint", "5", "5\n");
		}

		[Test]
		public void WrapSinglePropGetOnlyFloat ()
		{
			WrapSinglePropertyGetOnly ("Float", "Float", "float", "4f", "4.0\n");
		}

		[Test]
		public void WrapSinglePropGetOnlyDouble ()
		{
			WrapSinglePropertyGetOnly ("Double", "Double", "double", "4.0", "4.0\n");
		}

		[Test]
		public void WrapSinglePropGetOnlyString ()
		{
			WrapSinglePropertyGetOnly ("String", "String", "SwiftString", "SwiftString.FromString(\"Hi mom\")", "Hi mom\n");
		}

		void WrapSinglePropertyGetSetOnly (string type, string csType, string csVal, string swiftReplacement, string expected)
		{
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
			    $"public protocol MontyWSPGSO{type} {{ var prop : {type} {{ get set }} \n  }}\n" +
			    $"public class TestMontyWSPGSO{type} {{\npublic init() {{ }}\npublic func doIt(m:MontyWSPGSO{type}) {{\nvar x = m\nvar s = \"\", t = \"\"\nprint(x.prop, to:&s)\nx.prop = {swiftReplacement}\nprint(x.prop, to:&t)\nwriteToFile(s + t, \"WrapSinglePropertyGetSetOnly{type}\")\n}}\n}}\n";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSPGSO{type}");
			overCS.Inheritance.Add (new CSIdentifier ($"IMontyWSPGSO{type}"));
			CSProperty overProp = new CSProperty (new CSSimpleType (csType), CSMethodKind.None, new CSIdentifier ("Prop"),
			    CSVisibility.Public, new CSCodeBlock (), CSVisibility.Public, new CSCodeBlock ());

			overCS.Properties.Add (overProp);

			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSPGSO{type}"), "myOver", new CSFunctionCall ($"OverWSPGSO{type}", true));
			CSLine decl1 = CSVariableDeclaration.VarLine (new CSSimpleType ($"TestMontyWSPGSO{type}"), "tester", new CSFunctionCall ($"TestMontyWSPGSO{type}", true));
			CSLine initer = CSAssignment.Assign ("myOver.Prop", new CSIdentifier (csVal));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("tester.DoIt", false, new CSIdentifier ("myOver"));
			CSCodeBlock callingCode = CSCodeBlock.Create (decl, decl1, initer, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapSinglePropertyGetSetOnly{type}", otherClass : overCS, platform: PlatformName.macOS);
		}

		[Test]
		public void WrapSinglePropGetSetInt ()
		{
			WrapSinglePropertyGetSetOnly ("Int32", "int", "42", "43", "42\n43\n");
		}

		[Test]
		public void WrapSinglePropGetSetUInt ()
		{
			WrapSinglePropertyGetSetOnly ("UInt32", "uint", "42", "43", "42\n43\n");
		}

		[Test]
		public void WrapSinglePropGetSetFloat ()
		{
			WrapSinglePropertyGetSetOnly ("Float", "float", "42.0f", "43.0", "42.0\n43.0\n");
		}

		[Test]
		public void WrapSinglePropGetSetDouble ()
		{
			WrapSinglePropertyGetSetOnly ("Double", "double", "42.0", "43.0", "42.0\n43.0\n");
		}

		[Test]
		public void WrapSinglePropGetSetString ()
		{
			WrapSinglePropertyGetSetOnly ("String", "SwiftString", "SwiftString.FromString(\"hi\")", "\"mom\"", "hi\nmom\n");
		}

		[Test]
		public void WrapSinglePropGetSetBool ()
		{
			WrapSinglePropertyGetSetOnly ("Bool", "bool", "true", "false", "true\nfalse\n");
		}

		void WrapSubscriptGetSetOnly (string type, string csType, string csVal, string swiftReplacement, string expected)
		{
			string swiftCode =
			    TestRunningCodeGenerator.kSwiftFileWriter +
				       $"public protocol MontyWSubSGO{type} {{ subscript(i:Int32) -> {type} {{ get set }}\n  }}\n" +
				       $"public class TestMontyWSubSGO{type} {{\npublic init() {{ }}\npublic func doIt(m:MontyWSubSGO{type}) {{\nvar x = m\nvar s = \"\", t = \"\"\nprint(x[0], to:&s)\nx[0] = {swiftReplacement}\nprint(x[0], to:&t)\nwriteToFile(s + t, \"WrapSubscriptGetSetOnly{type}\")\n}}\n}}\n";

			CSClass overCS = new CSClass (CSVisibility.Public, $"OverWSubSGO{type}");
			overCS.Inheritance.Add (new CSIdentifier ($"IMontyWSubSGO{type}"));
			overCS.Fields.Add (new CSLine (new CSFieldDeclaration (new CSSimpleType (csType), new CSIdentifier ("_x"), new CSIdentifier (csVal))));

			CSParameterList overParams = new CSParameterList ();
			overParams.Add (new CSParameter (CSSimpleType.Int, "i"));
			CSCodeBlock getBody = CSCodeBlock.Create (CSReturn.ReturnLine ((CSIdentifier)"_x"));
			CSCodeBlock setBody = CSCodeBlock.Create (CSAssignment.Assign ("_x", (CSIdentifier)"value"));
			CSProperty overProp = new CSProperty (new CSSimpleType (csType), CSMethodKind.None, CSVisibility.Public,
			    getBody, CSVisibility.Public, setBody, overParams);

			overCS.Properties.Add (overProp);

			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ($"OverWSubSGO{type}"), "myOver", new CSFunctionCall ($"OverWSubSGO{type}", true));
			CSLine decl1 = CSVariableDeclaration.VarLine (new CSSimpleType ($"TestMontyWSubSGO{type}"), "tester", new CSFunctionCall ($"TestMontyWSubSGO{type}", true));
			CSLine invoker = CSFunctionCall.FunctionCallLine ("tester.DoIt", false, new CSIdentifier ("myOver"));
			CSCodeBlock callingCode = CSCodeBlock .Create (decl, decl1, invoker);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapSubscriptGetSetOnly{type}", otherClass : overCS);
		}

		[Test]
		public void WrapSubscriptPropGetSetBool ()
		{
			WrapSubscriptGetSetOnly ("Bool", "bool", "true", "false", "true\nfalse\n");
		}

		[Test]
		public void WrapSubscriptPropGetSetInt ()
		{
			WrapSubscriptGetSetOnly ("Int32", "int", "5", "6", "5\n6\n");
		}

		[Test]
		public void WrapSubscriptPropGetSetUInt ()
		{
			WrapSubscriptGetSetOnly ("UInt32", "uint", "5", "6", "5\n6\n");
		}

		[Test]
		public void WrapSubscriptPropGetSetFloat ()
		{
			WrapSubscriptGetSetOnly ("Float", "float", "5.0f", "6.0", "5.0\n6.0\n");
		}

		[Test]
		public void WrapSubscriptPropGetSetDouble ()
		{
			WrapSubscriptGetSetOnly ("Double", "double", "5.0", "6.0", "5.0\n6.0\n");
		}

		[Test]
		public void WrapSubscriptPropGetSetString ()
		{
			WrapSubscriptGetSetOnly ("String", "SwiftString", "SwiftString.FromString(\"hi\")", "\"mom\"", "hi\nmom\n");
		}

		[Test]
		[Ignore("Taking offline until protocols are redone")]
		public void CustomStringConvertibleTest ()
		{
			var swiftCode = TestRunningCodeGenerator.kSwiftFileWriter +
				"public func printIt(a: CustomStringConvertible) {\n" +
				"   writeToFile(a.description, \"CustomStringConvertibleTest\")\n" +
				"}\n";

			var convertible = new CSClass (CSVisibility.Public, "MyConvert");
			convertible.Inheritance.Add (new CSIdentifier ("ICustomStringConvertible"));
			var getBody = CSCodeBlock.Create (CSReturn.ReturnLine (new CSFunctionCall("SwiftString.FromString", false, CSConstant.Val ("I did it!"))));
			var declProp = new CSProperty (new CSSimpleType (typeof (SwiftString)), CSMethodKind.None, new CSIdentifier ("Description"),
							     CSVisibility.Public, getBody, CSVisibility.Public, null);
			convertible.Properties.Add (declProp);
			var caller = CSFunctionCall.FunctionCallLine ("TopLevelEntities.PrintIt", false, new CSFunctionCall ("MyConvert", true));
			var callingCode = CSCodeBlock.Create (caller);

			TestRunning.TestAndExecute (swiftCode, callingCode, "I did it!", otherClass : convertible);
		}

		[Test]
		public void ObjCFunctionSmokeTest ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol SmokeFunc0 {\n" +
				"    func intOnBool (a: Int) -> Bool\n" +
				"}\n";

			var caller = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("did it"));
			var callingCode = CSCodeBlock.Create (caller);

			TestRunning.TestAndExecute (swiftCode, callingCode, "did it\n", platform: PlatformName.macOS);
		}

		[Test]
		public void ObjCPropertySmokeTest ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol SmokeProp0 {\n" +
				"    var X: Bool { get set }\n" +
				"}\n";

			var caller = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("did it"));
			var callingCode = CSCodeBlock.Create (caller);

			TestRunning.TestAndExecute (swiftCode, callingCode, "did it\n", platform: PlatformName.macOS);
		}

		[Test]
		public void ObjCSubscriptSmokeTest ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol SmokeSub0 {\n" +
				"    @objc subscript (index:Int) -> Bool { get set }\n" +
				"}\n";

			var caller = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("did it"));
			var callingCode = CSCodeBlock.Create (caller);
      
      TestRunning.TestAndExecute (swiftCode, callingCode, "did it\n", platform: PlatformName.macOS);
		}



		[Test]
		public void ObjCInvokeProtoFunc ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Hand {\n" +
				"   @objc func whichHand()\n" +
				"}\n" +
				"public func doWhichHand(a:Hand) {\n" +
				"    a.whichHand()\n" +
				"}\n";
			var altClass = new CSClass (CSVisibility.Public, "MyHandsProtoFunc");
			altClass.Inheritance.Add (new CSIdentifier ("NSObject"));
			altClass.Inheritance.Add (new CSIdentifier ("IHand"));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("lefty"));

			var whichHandMethod = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("WhichHand"), new CSParameterList (),
							    CSCodeBlock.Create (printIt));
			altClass.Methods.Add (whichHandMethod);

			var altCtor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), new CSBaseExpression [0], true, new CSCodeBlock ());
			altClass.Constructors.Add (altCtor);

			var caller = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("did it"));
			var callingCode = CSCodeBlock.Create (caller);

			TestRunning.TestAndExecute (swiftCode, callingCode, "did it\n", otherClass: altClass, platform: PlatformName.macOS);
		}

		[Test]
		public void ObjCReturnProto ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Picker {\n" +
				"    @objc func pick () -> Int\n" +
				"}\n" +
				"@objc\n" +
				"private class hiddenPicker : NSObject, Picker {\n" +
				"    @objc public func pick () -> Int {\n" +
				"        return 42\n" +
				"    }\n" +
				"}\n" +
				"public func getPicker () -> Picker {\n" +
				"    return hiddenPicker ()\n" +
				"}\n";

			var varDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, "picker", new CSFunctionCall ("TopLevelEntities.GetPicker", false));
			var printIt = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("picker.Pick", false));
			var callingCode = CSCodeBlock.Create (varDecl, printIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n", platform: PlatformName.macOS);
		}


		[Test]
		public void ObjCInvokeProp ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol HandProp {\n" +
				"   @objc var whichHand:Int { get }\n" +
				"}\n" +
				"public func doWhichHand(a:HandProp) -> Int {\n" +
				"    return a.whichHand\n" +
				"}\n";
			var altClass = new CSClass (CSVisibility.Public, "MyHandsProp");
			altClass.Inheritance.Add (new CSIdentifier ("NSObject"));
			altClass.Inheritance.Add (new CSIdentifier ("IHandProp"));

			var whichHandProp = CSProperty.PublicGetBacking (new CSSimpleType ("nint"), new CSIdentifier ("WhichHand"), new CSIdentifier ("42"));

			altClass.Properties.Add (whichHandProp);

			var altCtor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), new CSBaseExpression [0], true, new CSCodeBlock ());
			altClass.Constructors.Add (altCtor);


			var altInst = CSVariableDeclaration.VarLine (CSSimpleType.Var, "lefty", new CSFunctionCall ("MyHandsProp", true));

			var caller = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.DoWhichHand", new CSIdentifier ("lefty")));
			var callingCode = CSCodeBlock.Create (altInst, caller);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n", otherClass: altClass, platform: PlatformName.macOS);
		}


		[Test]
		public void ObjCInvokePropInSwift ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol HandProp1 {\n" +
				"   @objc var whichHand:Int { get }\n" +
				"}\n" +
				"@objc\n" +
				"internal class HandPropImpl : NSObject, HandProp1 {\n" +
				"    @objc public var whichHand:Int {\n" +
				"        get {\n" +
				"            return 42\n" +
				"        }\n" +
				"    }\n" +
				"}\n" +
				"public func makeHandProp1 () -> HandProp1 {\n" +
				"    return HandPropImpl ()\n" +
				"}\n";

			var inst = CSVariableDeclaration.VarLine (CSSimpleType.Var, "lefty", new CSFunctionCall ("TopLevelEntities.MakeHandProp1", false));

			var caller = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ("lefty.WhichHand"));
			var callingCode = CSCodeBlock.Create (inst, caller);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n", platform: PlatformName.macOS);
		}


		[Test]
		public void ObjCRefProtocolArg ()
		{
			var swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol LifeTheUniverseAnd {\n" +
				"   @objc func Everything () -> Int\n" +
				"}\n" +
				"@objc\n" +
				"internal class Liff : NSObject, LifeTheUniverseAnd {\n" +
				"   private var x: Int\n" +
				"   public init (z: Int) {\n" +
				"      x = z\n" +
				"   }\n" +
				"   @objc func Everything () -> Int {\n" +
				"       return x\n" +
				"   }\n" +
				"}\n"+
				"public func makeIt (a: Int) -> LifeTheUniverseAnd {\n" +
				"    return Liff(z: a)\n" +
				"}\n" +
				"public func setIt (a:inout LifeTheUniverseAnd) {\n" +
				"    a = Liff(z: 42)\n" +
				"}\n";

			var inst = CSVariableDeclaration.VarLine (CSSimpleType.Var, "liff", new CSFunctionCall ("TopLevelEntities.MakeIt", false, CSConstant.Val (17)));
			var morphIt = CSFunctionCall.FunctionCallLine ("TopLevelEntities.SetIt", false, new CSIdentifier ("ref liff"));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("liff.Everything"));
			var callingCode = CSCodeBlock.Create (inst, morphIt, printIt);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n", platform: PlatformName.macOS);

		}


		[Test]
		public void EmptyProtocolTest ()
		{
			var swiftCode = @"
public protocol ThisServesAsATrait { }
public func isThisATrait (a: ThisServesAsATrait) -> Bool {
    return true;
}
";

			var auxClass = new CSClass (CSVisibility.Public, "Traitor");
			auxClass.Inheritance.Add (new CSIdentifier ("IThisServesAsATrait"));
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, "trait", new CSFunctionCall ("Traitor", true));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.IsThisATrait", new CSIdentifier ("trait")));
			var callingCode = CSCodeBlock.Create (decl, printIt);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", otherClass: auxClass);

		}

		[Test]
		public void EqConstraintSmokeTest ()
		{
			var swiftCode = @"
public protocol Interpolatable {
	associatedtype ValueType
	func interpolateFrom(from: ValueType, to: ValueType)
}
public class FilmStrip<T: Interpolatable> where T.ValueType == T {
	public init () { }
}
";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("No smoke"));
			var callingCode = CSCodeBlock.Create (printIt);
			// expected errors:
	    		// associated type
			// equality constraint
	    		// skipping FilmString (due to previous errors)
			TestRunning.TestAndExecute (swiftCode, callingCode, "No smoke\n", expectedErrorCount: 1, platform:PlatformName.macOS);
		}

		[Test]
		public void TestProtocolTypeAttribute ()
		{
			var swiftCode = @"
public protocol Useless {
	func doNothing ()
}
";
			// this will throw on fail
			// SwiftProtocolTypeAttribute.DescriptorForType (typeof (Useless));
			var getter = CSFunctionCall.FunctionCallLine ("SwiftProtocolTypeAttribute.DescriptorForType", false,
				new CSSimpleType ("IUseless").Typeof ());
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
		
			var callingCode = CSCodeBlock.Create (getter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n", platform: PlatformName.macOS);
		}

		[Test]
		public void TestReturnsAny ()
		{
			var swiftCode = @"
public func returnsAny () -> Any {
	return 7;
}
";

			var any = new CSIdentifier ("any");
			var anyDecl = CSVariableDeclaration.VarLine (any, new CSFunctionCall ("TopLevelEntities.ReturnsAny", false));
			var unbox = new CSFunctionCall ("SwiftExistentialContainer0.Unbox<nint>", false, any);
			var printer = CSFunctionCall.ConsoleWriteLine (unbox);
			var callingCode = CSCodeBlock.Create (anyDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}
	}
}
