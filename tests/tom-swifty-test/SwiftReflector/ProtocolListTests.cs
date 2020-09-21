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
    func constantA () -> Int
}
public protocol ProtoRB {
    func constantB () -> Int
}
public class ImplRARB : ProtoRA, ProtoRB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}

public func getDual () -> ProtoRA & ProtoRB {
	return ImplRARB ()
}
";

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("(ImplRARB)TopLevelEntities.GetDual", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void TestPropDualProtocol ()
		{
			var swiftCode = @"
public protocol ProtoPA {
    func constantA () -> Int
}
public protocol ProtoPB {
    func constantB () -> Int
}
public class ImplPAPB : ProtoPA, ProtoPB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}

public var DualProp : ProtoPA & ProtoPB = ImplPAPB ()
";

			var thingID = new CSIdentifier ("due");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("(ImplPAPB)TopLevelEntities.GetDualProp", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void TestNonOverrideMethodParameter ()
		{
			var swiftCode = @"
public protocol ProtoMPA {
    func constantA () -> Int
}
public protocol ProtoMPB {
    func constantB () -> Int
}
public class ImplMPAMPB : ProtoMPA, ProtoMPB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }

    public func infoOn(a: ProtoMPA & ProtoMPB) -> String
    {
        let x = a.constantA()
        let y = a.constantB()
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
    func constantA () -> Int
}
public protocol ProtoMRB {
    func constantB () -> Int
}
public class ImplMRAMRB : ProtoMRA, ProtoMRB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}


		[Test]
		public void TestNonOverrideProperty ()
		{
			var swiftCode = @"
public protocol ProtoMPRA {
    func constantA () -> Int
}
public protocol ProtoMPRB {
    func constantB () -> Int
}
public class ImplMPRAMPRB : ProtoMPRA, ProtoMPRB {
    public init () {
    }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}



		[Test]
		public void TestNonOverrideSubscript ()
		{
			var swiftCode = @"
public protocol ProtoMSRA {
    func constantA () -> Int
}
public protocol ProtoMSRB {
    func constantB () -> Int
}
public class ImplMSRAMSRB : ProtoMSRA, ProtoMSRB {
    public init () {
    }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestEnumPayload ()
		{
			var swiftCode = @"
public protocol ProtoMEA {
    func constantA () -> Int
}
public protocol ProtoMEB {
    func constantB () -> Int
}
public enum NotParticularlyUsefulPayload {
    case intValue(Int)
    case protoValue(ProtoMEA & ProtoMEB)
}
public class ImplMEAMEB : ProtoMEA, ProtoMEB {
    public init () {
    }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{quaID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, quaDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestEnumFactory ()
		{
			var swiftCode = @"
public protocol ProtoMEFA {
    func constantA () -> Int
}
public protocol ProtoMEFB {
    func constantB () -> Int
}
public enum NotParticularlyUseful {
    case intValue(Int)
    case protoValue(ProtoMEFA & ProtoMEFB)
}
public class ImplMEFAMFEB : ProtoMEFA, ProtoMEFB {
    public init () {
    }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
    func constantA () -> Int
}
public protocol ProtoERB {
    func constantB () -> Int
}
public class ImplERAERB : ProtoERA, ProtoERB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void TestVirtualFunc ()
		{
			var swiftCode = @"
public protocol ProtoVA {
    func constantA () -> Int
}
public protocol ProtoVB {
    func constantB () -> Int
}
public class ImplVAVB : ProtoVA, ProtoVB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}

open class UsingClass {
    public init () { }
    open func doAThing (a: ProtoVA & ProtoVB) -> Int {
        return a.constantA() + a.constantB()
    }
}
";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplVAVB", true));

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
    func constantA () -> Int
}
public protocol ProtoVPB {
    func constantB () -> Int
}
public class ImplVAVB : ProtoVPA, ProtoVPB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}

open class UsingClassP {
    private var thing: ProtoVPA & ProtoVPB;
    public init () {
        thing = ImplVAVB ()
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
        return thing.constantA() + thing.constantB()
    }
}";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("UsingClassP", true));

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"(ImplVAVB){thingID}.GetImpl", false));
			var resetter = CSFunctionCall.FunctionCallLine ($"{thingID}.SetImpl", false, new CSFunctionCall ("ImplVAVB", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.DoAThing", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, resetter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void TestVirtualIndexer ()
		{
			var swiftCode = @"
public protocol ProtoVIPA {
    func constantA () -> Int
}
public protocol ProtoVIPB {
    func constantB () -> Int
}
public class ImplVIPAVIPB : ProtoVIPA, ProtoVIPB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
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
        return thing.constantA() + thing.constantB()
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
    func constantA () -> Int
}
public protocol ProtoProtoFB {
    func constantB () -> Int
}
public class ImplProtoFAProtoFB : ProtoProtoFA, ProtoProtoFB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}
public protocol UsingProto {
    func tryItOut (a: ProtoProtoFA & ProtoProtoFB) -> Int
}
public class UsingClassPP : UsingProto {
    public init () { }
    public func tryItOut (a: ProtoProtoFA & ProtoProtoFB) -> Int {
        return a.constantA() + a.constantB()
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
    func constantA () -> Int
}
public protocol ProtoProtoPB {
    func constantB () -> Int
}
public class ImplProtoPropFAProtoFB : ProtoProtoPA, ProtoProtoPB {
    public init () { }
    public func constantA () -> Int {
        return 3
    }
    public func constantB () -> Int {
        return 4
    }
}
public protocol UsingProto {
    var prop : ProtoProtoPA & ProtoProtoPB  { get }
}
public class UsingClassPProp : UsingProto {
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
    return p.constantA() + p.constantB()
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
