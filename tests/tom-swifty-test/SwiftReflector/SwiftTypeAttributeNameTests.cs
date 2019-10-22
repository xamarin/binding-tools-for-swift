using System;
using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	public class SwiftTypeAttributeNameTests {

		[Test]
		public void AttributeOnClass ()
		{
			var swiftCode = @"
public class BoringClass {
    public init () {
    }
}
";
			var callingCode = PrintTypeName ("BoringClass");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringClass\n");
		}


		[Test]
		public void AttributeOnOpenClass ()
		{
			var swiftCode = @"
open class BoringOpenClass {
    public init () {
    }
}
";
			var callingCode = PrintTypeName ("BoringOpenClass");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringOpenClass\n");
		}

		[Test]
		public void AttributeStruct ()
		{
			var swiftCode = @"
open class BoringStruct {
    public init () {
    }
    public var X: Int = 0
}
";
			var callingCode = PrintTypeName ("BoringStruct");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringStruct\n");
		}


		[Test]
		public void AttributeProtocol ()
		{
			var swiftCode = @"
public protocol BoringProtocol {
    func Add (a: Int, b: Int) -> Int
}
";
			var callingCode = PrintTypeName ("IBoringProtocol");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringProtocol\n", platform: PlatformName.macOS);
		}


		[Test]
		public void BoringEZEnum ()
		{
			var swiftCode = @"
import Foundation

@objc
public enum BoringEnum1 : Int {
    case a, b, c
}
";
			var callingCode = PrintTypeName ("BoringEnum1");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringEnum1\n", platform: PlatformName.macOS);
		}


		[Test]
		public void BoringEnum ()
		{
			var swiftCode = @"
import Foundation

public enum BoringEnum2 {
    case a, b, c
}
";
			var callingCode = PrintTypeName ("BoringEnum2");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringEnum2\n");
		}


		[Test]
		public void BoringEnumCompoud ()
		{
			var swiftCode = @"
import Foundation

public enum BoringEnum3 {
    case a (Int)
    case b (Float)
    case c (Bool)
}
";
			var callingCode = PrintTypeName ("BoringEnum3");
			TestRunning.TestAndExecute (swiftCode, callingCode, ".BoringEnum3\n");
		}


		CSCodeBlock PrintTypeName (string csTypeName)
		{
			// string name;
			// SwiftTypeNameAttribute.TryGetSwiftName (typeof (csTypeName), out name);
			// Console.WriteLine (name.Substring (name.IndexOf ('.')));

			var nameID = "name";
			var nameDecl = CSVariableDeclaration.VarLine (new CSSimpleType (typeof (string)), nameID);


			var tryGet = CSFunctionCall.FunctionCallLine ("SwiftTypeNameAttribute.TryGetSwiftName", false,
				new CSFunctionCall ("typeof", false, new CSIdentifier (csTypeName)),
				new CSIdentifier ($"out {nameID}"));


			var printer = CSFunctionCall.ConsoleWriteLine (
				new CSFunctionCall ($"{nameID}.Substring", false,
					new CSFunctionCall ($"{nameID}.IndexOf", false, CSConstant.Val ('.'))));

			return CSCodeBlock.Create (nameDecl, tryGet, printer);
		}
	}
}
