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

			TestRunning.TestAndExecute (swiftCode, callingCode, "3 4 7\n", platform: PlatformName.macOS);
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
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("TopLevelEntities.GetDual<ImplRARB>", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("TopLevelEntities.GetDualProp<ImplPAPB>", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
			TestRunning.TestAndExecute (swiftCode, callingCode, "3 4 7\n", platform: PlatformName.macOS);
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
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID.Name}.GetMeA<ImplMRAMRB>", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID.Name}.GetPropStuff<ImplMPRAMPRB>", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID.Name}.GetSubscript<ImplMSRAMSRB>", false, CSConstant.Val (7)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{anotherID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
public enum NotParticularlyUseful {
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
    public func getPayload() -> NotParticularlyUseful {
        return .protoValue(self)
    }
}
";
			var thingID = new CSIdentifier ("due");
			var anotherID = new CSIdentifier ("tre");
			var quaID = new CSIdentifier ("qua");
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("ImplMEAMEB", true));
			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID.Name}.GetPayload", false));
			var quaDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, quaID, new CSFunctionCall ($"{anotherID.Name}.GetValueProtoValue<ImplMEAMEB>", false));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{quaID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, quaDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
			TestRunning.TestAndExecute (swiftCode, callingCode, "ProtoValue\n", platform: PlatformName.macOS);
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
			var thingDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, thingID, new CSFunctionCall ("TopLevelEntities.GetDual<ImplERAERB>", false, CSConstant.Val (false)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.ConstantA", false));
			var callingCode = CSCodeBlock.Create (thingDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
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
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n", platform: PlatformName.macOS);
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

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID}.GetImpl<ImplVAVB>", false));
			var resetter = CSFunctionCall.FunctionCallLine ($"{thingID}.SetImpl", false, new CSFunctionCall ("ImplVAVB", true));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.DoAThing", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, resetter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n", platform: PlatformName.macOS);
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

			var anotherDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, anotherID, new CSFunctionCall ($"{thingID}.GetSubscript<ImplVIPAVIPB>", false, CSConstant.Val (4)));
			var resetter = CSFunctionCall.FunctionCallLine ($"{thingID}.SetSubscript", false, new CSFunctionCall ("ImplVIPAVIPB", true), CSConstant.Val (18));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{thingID.Name}.DoAThing", false));
			var callingCode = CSCodeBlock.Create (thingDecl, anotherDecl, resetter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n", platform: PlatformName.macOS);
		}
	}
}
