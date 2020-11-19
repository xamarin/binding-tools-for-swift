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
	public class ProtocolListTests {


		[Test]
		public void TestBasicDualProtocol ()
		{
			var swiftCode = @"
public protocol ProtoA {
    func constantA () -> Int
}
public protocol ProtoB {
    func constantB () -> Int
}
public class ImplAB : ProtoA, ProtoB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}

public func infoOn(a: ProtoA & ProtoB) -> String {
    let x = a.constantA()
    let y = a.constantB()
    let z = x + y
    return ""\(x) \(y) \(z)""
}
";

			var clID = new CSIdentifier ("cl");
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ("ImplAB", true));
			var clCall = new CSFunctionCall ("TopLevelEntities.InfoOn", false, clID);
			var printer = CSFunctionCall.ConsoleWriteLine (clCall);
			var callingCode = CSCodeBlock.Create (clDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "3 4 7\n");
		}


		[Test]
		public void TestReturnDualProtocol ()
		{
			var swiftCode = @"
public protocol ProtoRA {
    func constantRA () -> Int
}
public protocol ProtoRB {
    func constantRB () -> Int
}
public class ImplRARB : ProtoRA, ProtoRB {
    public init () { }
    public func constantRA () -> Int {
        return 3
    }
    public func constantRB () -> Int {
        return 4
    }
}

public func getDual () -> ProtoRA & ProtoRB {
	return ImplRARB ()
}
";

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("(ImplRARB)TopLevelEntities.GetDual", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantRA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void TestPropDualProtocol ()
		{
			var swiftCode = @"
public protocol ProtoPA {
    func constantPA () -> Int
}
public protocol ProtoPB {
    func constantPB () -> Int
}
public class ImplPAPB : ProtoPA, ProtoPB {
    public init () { }
    public func constantPA () -> Int {
        return 3
    }
    public func constantPB () -> Int {
        return 4
    }
}

public var DualProp : ProtoPA & ProtoPB = ImplPAPB ()
";

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("(ImplPAPB)TopLevelEntities.GetDualProp", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantPA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void TestNonOverrideMethodParameter ()
		{
			var swiftCode = @"
public protocol ProtoMPA {
    func constantMPA () -> Int
}
public protocol ProtoMPB {
    func constantMPB () -> Int
}
public class ImplMPAMPB : ProtoMPA, ProtoMPB {
    public init () { }
    public func constantMPA () -> Int {
        return 3
    }
    public func constantMPB () -> Int {
        return 4
    }

    public func infoOn(a: ProtoMPA & ProtoMPB) -> String
    {
        let x = a.constantMPA()
        let y = a.constantMPB()
        let z = x + y
        return ""\(x) \(y) \(z)""
    }
}
";

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMPAMPB", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.InfoOn", false, thingID));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3 4 7\n");
		}


		[Test]
		public void TestNonOverrideMethodReturn ()
		{
			var swiftCode = @"
public protocol ProtoMRA {
    func constantMRA () -> Int
}
public protocol ProtoMRB {
    func constantMRB () -> Int
}
public class ImplMRAMRB : ProtoMRA, ProtoMRB {
    public init () { }
    public func constantMRA () -> Int {
        return 3
    }
    public func constantMRB () -> Int {
        return 4
    }

    public func getMeA() -> ProtoMRA & ProtoMRB
    {
        return ImplMRAMRB ()
    }
}
";

			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMRAMRB", true));
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"(ImplMRAMRB){thingID.Name}.GetMeA", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantMRA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void TestNonOverrideProperty ()
		{
			var swiftCode = @"
public protocol ProtoMPRA {
    func constantMPRA () -> Int
}
public protocol ProtoMPRB {
    func constantMPRB () -> Int
}
public class ImplMPRAMPRB : ProtoMPRA, ProtoMPRB {
    public init () {
    }
    public func constantMPRA () -> Int {
        return 3
    }
    public func constantMPRB () -> Int {
        return 4
    }

    public func setPropStuff ()
    {
        propStuff = self
    }
   
    public var propStuff: ProtoMPRA & ProtoMPRB {
        get {
             return self
        }
        set {
        }
    }

}
";

			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMPRAMPRB", true));
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSIdentifier ($"(ImplMPRAMPRB){thingID.Name}").Dot (new CSIdentifier ("PropStuff")));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantMPRA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}



		[Test]
		public void TestNonOverrideSubscript ()
		{
			var swiftCode = @"
public protocol ProtoMSRA {
    func constantMSRA () -> Int
}
public protocol ProtoMSRB {
    func constantMSRB () -> Int
}
public class ImplMSRAMSRB : ProtoMSRA, ProtoMSRB {
    public init () {
    }
    public func constantMSRA () -> Int {
        return 3
    }
    public func constantMSRB () -> Int {
        return 4
    }

    public subscript (index: Int) -> ProtoMSRA & ProtoMSRB {
	get {
	    return self
	}
	set {
	}
    }

}
";

			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMSRAMSRB", true));
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"(ImplMSRAMSRB){thingID.Name}.GetSubscript", false, CSConstant.Val (7)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantMSRA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestEnumPayload ()
		{
			var swiftCode = @"
public protocol ProtoMEA {
    func constantMEA () -> Int
}
public protocol ProtoMEB {
    func constantMEB () -> Int
}
public enum NotParticularlyUsefulPayload {
    case intValue(Int)
    case protoValue(ProtoMEA & ProtoMEB)
}
public class ImplMEAMEB : ProtoMEA, ProtoMEB {
    public init () {
    }
    public func constantMEA () -> Int {
        return 3
    }
    public func constantMEB () -> Int {
        return 4
    }
    public func getPayload() -> NotParticularlyUsefulPayload {
        return .protoValue(self)
    }
}
";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var quaID = new CSIdentifier ("qua");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMEAMEB", true));
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID.Name}.GetPayload", false));
			var quaDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, quaID, new CSFunctionCall ($"(ImplMEAMEB){anotherID.Name}.GetValueProtoValue", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{quaID.Name}.ConstantMEA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, quaDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestEnumFactory ()
		{
			var swiftCode = @"
public protocol ProtoMEFA {
    func constantMEFA () -> Int
}
public protocol ProtoMEFB {
    func constantMEFB () -> Int
}
public enum NotParticularlyUseful {
    case intValue(Int)
    case protoValue(ProtoMEFA & ProtoMEFB)
}
public class ImplMEFAMFEB : ProtoMEFA, ProtoMEFB {
    public init () {
    }
    public func constantMEFA () -> Int {
        return 3
    }
    public func constantMEFB () -> Int {
        return 4
    }
    public func getPayload() -> NotParticularlyUseful {
        return .protoValue(self)
    }
}
";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMEFAMFEB", true));

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"NotParticularlyUseful.NewProtoValue", false, thingID));
			var printer = CSFunctionCall.ConsoleWriteLine (anotherID.Dot (new CSIdentifier ("Case")));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ProtoValue\n");
		}


		[Test]
		[Ignore ("This is partly there. To get the rest of the way there will be a much larger undertaking: https://github.com/xamarin/maccore/issues/1984.")]
		public void TestFuncThrows ()
		{
			var swiftCode = @"
public protocol ProtoERA {
    func constantERA () -> Int
}
public protocol ProtoERB {
    func constantERB () -> Int
}
public class ImplERAERB : ProtoERA, ProtoERB {
    public init () { }
    public func constantERA () -> Int {
        return 3
    }
    public func constantERB () -> Int {
        return 4
    }
}

internal struct someError : Error {
    public init () { }
}

public func getDual (doThrow: Bool) throws -> ProtoERA & ProtoERB {
	if doThrow {
            throw someError ()
        }
	return ImplERAERB ()
}
";

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("(ImplERAERB)TopLevelEntities.GetDual", false, CSConstant.Val (false)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantERA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestVirtualFunc ()
		{
			var swiftCode = @"
public protocol ProtoVA {
    func constantVA () -> Int
}
public protocol ProtoVB {
    func constantVB () -> Int
}
public class ImplVAVBFunc : ProtoVA, ProtoVB {
    public init () { }
    public func constantVA () -> Int {
        return 3
    }
    public func constantVB () -> Int {
        return 4
    }
}

open class UsingClass {
    public init () { }
    open func doAThing (a: ProtoVA & ProtoVB) -> Int {
        return a.constantVA() + a.constantVB()
    }
}
";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplVAVBFunc", true));

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"UsingClass", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.DoAThing", false, thingID));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void TestVirtualProp ()
		{
			var swiftCode = @"
public protocol ProtoVPA {
    func constantVPA () -> Int
}
public protocol ProtoVPB {
    func constantVPB () -> Int
}
public class ImplVAVBProp : ProtoVPA, ProtoVPB {
    public init () { }
    public func constantVPA () -> Int {
        return 3
    }
    public func constantVPB () -> Int {
        return 4
    }
}

open class UsingClassP {
    private var thing: ProtoVPA & ProtoVPB;
    public init () {
        thing = ImplVAVBProp ()
    }
    open var impl: ProtoVPA & ProtoVPB {
        get {
            return thing
        }
        set {
            thing = newValue
        }
    }
    public func doAThing () -> Int {
        return thing.constantVPA() + thing.constantVPB()
    }
}";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("UsingClassP", true));

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"(ImplVAVBProp){thingID}.GetImpl", false));
			var resetter = CSFunctionCall.FunctionCallLine ($"{thingID}.SetImpl", false, new CSFunctionCall ("ImplVAVBProp", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.DoAThing", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, resetter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void TestVirtualIndexer ()
		{
			var swiftCode = @"
public protocol ProtoVIPA {
    func constantVIPA () -> Int
}
public protocol ProtoVIPB {
    func constantVIPB () -> Int
}
public class ImplVIPAVIPB : ProtoVIPA, ProtoVIPB {
    public init () { }
    public func constantVIPA () -> Int {
        return 3
    }
    public func constantVIPB () -> Int {
        return 4
    }
}

open class UsingClassPI {
    private var thing: ProtoVIPA & ProtoVIPB;
    public init () {
        thing = ImplVIPAVIPB ()
    }

    open subscript (index: Int) ->  ProtoVIPA & ProtoVIPB {
        get {
            return thing;
        }
        set {
            thing = newValue
        }
    }

    public func doAThing () -> Int {
        return thing.constantVIPA() + thing.constantVIPB()
    }
}";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("UsingClassPI", true));

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"(ImplVIPAVIPB){thingID}.GetSubscript", false, CSConstant.Val (4)));
			var resetter = CSFunctionCall.FunctionCallLine ($"{thingID}.SetSubscript", false, new CSFunctionCall ("ImplVIPAVIPB", true), CSConstant.Val (18));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.DoAThing", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, resetter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void TestProtoProtoList ()
		{
			var swiftCode = @"
public protocol ProtoProtoFA {
    func constantFA () -> Int
}
public protocol ProtoProtoFB {
    func constantFB () -> Int
}
public class ImplProtoFAProtoFB : ProtoProtoFA, ProtoProtoFB {
    public init () { }
    public func constantFA () -> Int {
        return 3
    }
    public func constantFB () -> Int {
        return 4
    }
}
public protocol UsingProto {
    func tryItOut (a: ProtoProtoFA & ProtoProtoFB) -> Int
}
public class UsingClassPP : UsingProto {
    public init () { }
    public func tryItOut (a: ProtoProtoFA & ProtoProtoFB) -> Int {
        return a.constantFA() + a.constantFB()
    }
}
";
			// var due = new ImplProtoFAProtoFB ();
			// var tre = new UsingClassPP ();
			// Console.WriteLine (tre.TryItOut (due));

			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplProtoFAProtoFB", true));
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ("UsingClassPP", true));
			var invoke = new CSFunctionCall ($"{anotherID.Name}.TryItOut", false, thingID);
			var printer = CSFunctionCall.ConsoleWriteLine (invoke);
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void TestProtoPropProtoList ()
		{
			var swiftCode = @"
public protocol ProtoProtoPA {
    func constantProtoPA () -> Int
}
public protocol ProtoProtoPB {
    func constantProtoPB () -> Int
}
public class ImplProtoPropFAProtoFB : ProtoProtoPA, ProtoProtoPB {
    public init () { }
    public func constantProtoPA () -> Int {
        return 3
    }
    public func constantProtoPB () -> Int {
        return 4
    }
}
public protocol UsingProtoProp {
    var prop : ProtoProtoPA & ProtoProtoPB  { get }
}
public class UsingClassPProp : UsingProtoProp {
    public init () {
        x = ImplProtoPropFAProtoFB ()
    }
    private var x: ProtoProtoPA & ProtoProtoPB
    public var prop : ProtoProtoPA & ProtoProtoPB {
        get {
            return x
        }
    }
}

public func tryItOut (a: UsingClassPProp) -> Int {
    let p = a.prop
    return p.constantProtoPA() + p.constantProtoPB()
}
";
			// var tre = new UsingClassPProp ();
			// Console.WriteLine (TopLevelEntities.TryItOut(due));

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("UsingClassPProp", true));
			var invoke = new CSFunctionCall ($"TopLevelEntities.TryItOut", false, thingID);
			var printer = CSFunctionCall.ConsoleWriteLine (invoke);
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

	}
}
