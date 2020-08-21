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

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ExtensionTests {

		[Test]
		public void ExtensionSmokeTest ()
		{
			var swiftCode =
				"public extension Double {\n" +
				"    public func DoubleIt() -> Double  { return self * 2; }\n" +
				"}\n";
			;
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("success"));
			CSCodeBlock callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "success\n");
		}

		[Test]
		public void ExtensionOnInt32 ()
		{
			var swiftCode =
				"public extension Int32 {\n" +
				"    public func TripleIt() -> Int32  { return self * 3; }\n" +
				"}\n";
			var extendIt = new CSFunctionCall ("3.TripleIt", false);
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "9\n");
		}

		[Test]
		public void ExtensionPropOnInt32 ()
		{
			var swiftCode =
				"public extension Int32 {\n" +
				"    public var times3 : Int32  { return self * 3; }\n" +
				"}\n";
			var extendIt = new CSFunctionCall ("3.GetTimes3", false);
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecuteNoDevice (swiftCode, callingCode, "9\n");
		}

		[Test]
		public void StaticExtensionFuncOnBool ()
		{
			var swiftCode =
				"public extension Bool {\n" +
				"    public static func getIt() -> Int { return 3; }\n" +
				"}\n";

			var extendIt = new CSFunctionCall ("ExtensionsForSystemDotbool0.GetIt", false);
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecuteNoDevice (swiftCode, callingCode, "3\n");
		}

		[Test]
		public void StaticExtensionPropOnBool ()
		{
			var swiftCode =
				"public extension Bool {\n" +
				"    public static var truthy: Int { return 4; }\n" +
				"}\n";
			var extendIt = new CSFunctionCall ("ExtensionsForSystemDotbool0.GetTruthy", false);
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecuteNoDevice (swiftCode, callingCode, "4\n");
		}



		[Test]
		[TestCase ("Int", "((nint)3)", "3\n")]
		[TestCase ("Float", "((float)42.1)", "42.1\n")]
		[TestCase ("Double", "(-42.1)", "-42.1\n")]
		[TestCase ("Bool", "(true)", "True\n")]
		[TestCase ("String", "(SwiftString.FromString(\"nothing\"))", "nothing\n")]
		public void ExtensionIdentityFunc(string swiftType, string value, string expected)
		{
			var swiftCode =
				$"public extension {swiftType} {{\n" +
				$"    public func Identity{swiftType}() -> {swiftType} {{ return self; }}\n" +
				"}\n";
			var valGetter = new CSFunctionCall ($"{value}.Identity{swiftType}", false);
			var printer = CSFunctionCall.ConsoleWriteLine (valGetter);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecuteNoDevice (swiftCode, callingCode, expected, testName: $"ExtensionIdentityFunc{swiftType}");
		}


		[Test]
		[TestCase ("Int", "((nint)3)", "3\n")]
		[TestCase ("Float", "((float)42.1)", "42.1\n")]
		[TestCase ("Double", "(-42.1)", "-42.1\n")]
		[TestCase ("Bool", "(true)", "True\n")]
		[TestCase ("String", "(SwiftString.FromString(\"nothing\"))", "nothing\n")]
		public void ExtensionIdentityProp (string swiftType, string value, string expected)
		{
			var swiftCode =
				$"public extension {swiftType} {{\n" +
				$"    public var IdentityProp{swiftType} : {swiftType} {{ return self; }}\n" +
				"}\n";
			var valGetter = new CSFunctionCall ($"{value}.GetIdentityProp{swiftType}", false);
			var printer = CSFunctionCall.ConsoleWriteLine (valGetter); 
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecuteNoDevice (swiftCode, callingCode, expected, testName: $"ExtensionIdentityProp{swiftType}");
		}



		[Test]
		public void GenericExtensionFuncOnBool ()
		{
			var swiftCode =
				"public protocol Truthy {\n" +
				"   func truthy(b:Bool) -> String\n" +
				"}\n" +
				"public class CTruthy : Truthy {\n" +
				"    public init() { }\n" +
				"    public func truthy(b: Bool) -> String { return b ? \"truthy\" : \"falsish\"; }\n" +
				"}\n" +
				"extension Bool {\n" +
				"    public func truth<T: Truthy>(a: T) -> String {\n" +
				"        return a.truthy(b: self)\n" +
				"    }\n" +
				"}\n";
			var truthyVar = CSVariableDeclaration.VarLine (new CSSimpleType ("CTruthy"), "truthy",
								       new CSFunctionCall ("CTruthy", true));
			var extendIt = new CSFunctionCall ("true.Truth", false, new CSIdentifier ("truthy"));
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var callingCode = CSCodeBlock.Create (truthyVar, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "truthy\n", platform: PlatformName.macOS);

		}

		[Test]
		public void TestExtensionSubscriptOnUInt ()
		{
			var swiftCode =
				"private func tenToThe (_ i: Int) -> Int{\n" +
				"   var exp = 1\n" +
				"   for _ in 0..<i {\n" +
				"      exp *= 10\n" +
				"   }\n" +
				"   return exp\n" +
				"}\n" +
				"public extension UInt {\n" +
				"   public subscript (digitIndex: Int) -> Int {\n" +
				"       return (Int(self) / tenToThe (digitIndex)) % 10;\n" +
				"   }\n" +
				"}\n";
			var extendIt = new CSFunctionCall ("((nuint)321).GetSubscript", false, CSConstant.Val(1));
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "2\n");
		}

		[Test]
		public void TestExtenstionGetSetSubscriptOnUInt16 ()
		{
			var swiftCode =
				"public extension UInt16 {\n" +
				"    subscript (index:Int) -> UInt16 {\n" +
				"        get { return self & UInt16(1 << index); }\n" +
				"        set {\n" +
				"            if newValue != 0 {\n"+
				"                self = self | (1 << index)\n" +
				"            }\n" +
				"            else {\n" +
				"                self = self & ~UInt16(1 << index)\n" +
				"            }\n" +
				"         }\n" +
				"    }\n" +
				"}\n";
			var extendIt = new CSFunctionCall ("((ushort)321).GetSubscript", false, CSConstant.Val (1));
			var printer = CSFunctionCall.ConsoleWriteLine (extendIt);
			var decl = CSVariableDeclaration.VarLine (CSSimpleType.UShort, "ashort", CSConstant.Val ((ushort)0));

			var changeIt = CSFunctionCall.FunctionCallLine ("ExtensionsForSystemDotushort0.SetSubscript", false, new CSIdentifier ("ref ashort"),
									CSConstant.Val (1), CSConstant.Val (3));
			var printAgain = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"ashort");
			var callingCode = CSCodeBlock.Create (printer, decl, changeIt, printAgain);
			TestRunning.TestAndExecuteNoDevice (swiftCode, callingCode, "0\n8\n");
		}

		[Test]
		public void ExtensionOnOptional ()
		{
			var swiftCode =
				"public extension Optional {\n" +
				"    func passOn<T>(a:T) -> T? {\n" +
				"        return self != nil ? a : nil\n" +
				"    }\n" +
				"}\n";


			var myOpt = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional", false, new CSType [] { CSSimpleType.Bool }), "optBool",
								   new CSFunctionCall ("SwiftOptional<bool>.Some", false, CSConstant.Val (true)));
			var result = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftOptional", false, new CSType [] { CSSimpleType.Int }), "optInt",
								    new CSFunctionCall ("optBool.PassOn", false, CSConstant.Val (42)));
			var printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"optInt.Value");

			var callingCode = CSCodeBlock.Create (myOpt, result, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "42\n");
		}


		[Test]
		public void ExtensionOnClassSmoke ()
		{
			var swiftCode =
				"public class SmokedHotDog {\n" +
				"    public init () { }\n" +
				"    public func AddOnions () {}\n" +
				"}\n" +
				"public extension SmokedHotDog {" +
				"    public func Price () -> Double {\n" +
				"        return 2.99\n" +
				"    }\n" +
				"}\n";

			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Made it"));

			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Made it\n");
		}


		[Test]
		public void ExtensionOnClassTest ()
		{
			var swiftCode =
				"public class Hamburger {\n" +
				"    public init () { }\n" +
				"    public func AddOnions () {}\n" +
				"}\n" +
				"public extension Hamburger {" +
				"    public func Price () -> Double {\n" +
				"        return 2.99\n" +
				"    }\n" +
				"}\n";

			var myDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, "burg", CSFunctionCall.Ctor ("Hamburger"));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("burg.Price", false));

			var callingCode = CSCodeBlock.Create (myDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "2.99\n");
		}


		[Test]
		public void ExtensionOnUserTypeTest ()
		{
			var swiftCode =
				@"public class HotDogOnUserType
{
    public init ()
    {

    }
}

public extension HotDogOnUserType
{
    public func Price () -> Double { return 2.99 }
}";

			var myDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, "dawg", CSFunctionCall.Ctor ("HotDogOnUserType"));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("dawg.Price", false));

			var callingCode = CSCodeBlock.Create (myDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "2.99\n");
		}


		[Test]
		public void NonPublicExtensionOnNotification ()
		{
			var swiftCode = @"import Foundation
	extension Notification {
	
	    func myFrame () -> CGRect? {
	        return CGRect.init ();
	    }
	}";

			var callingCode = CSCodeBlock.Create ();
			TestRunning.TestAndExecute (swiftCode, callingCode, "");
		}

		[Test]
		public void GenericExtensionOnDictionary ()
		{
			var swiftCode = @"
public extension Dictionary {
    func property<T>(_ name: String) -> T? {
        guard let key = name as? Key, let value = self[key] else { return nil }        
        return value as? T
    }
}";
			// var foo = new SwiftDictionary <SwiftString, nint> ();
			// foo.Add (SwiftString.FromString ("key", 43);
			// var bar = foo.Property<SwiftString, nint, nint>(SwiftString.FromString ("key"));
			// Console.WriteLine (bar.HasValue);
			// Console.WriteLine (bar.Value);

			var fooID = new CSIdentifier ("foo");
			var barID = new CSIdentifier ("bar");
			var stringExpr = new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("key"));
			var fooDecl = CSVariableDeclaration.VarLine (fooID, new CSFunctionCall ("SwiftDictionary<SwiftString, nint>", true));
			var fooAdd = CSFunctionCall.FunctionCallLine ($"{fooID.Name}.Add", false, stringExpr, CSConstant.Val (43));
			var barDecl = CSVariableDeclaration.VarLine (barID, new CSFunctionCall ($"{fooID.Name}.Property<SwiftString, nint, nint>", false, stringExpr));
			var printHasIt = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{barID.Name}.HasValue"));
			var printValue = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{barID.Name}.Value"));

			var callingCode = CSCodeBlock.Create (fooDecl, fooAdd, barDecl, printHasIt, printValue);
			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n43\n");
		}

		[Test]
		public void GenericExtensionWithTypes ()
		{
			var swiftCode = @"
public extension Dictionary {
    func value<T>(forKey: String, ofType: T.Type) -> T? {
        return nil
    }
}";

			// var foo = new SwiftDictionary <SwiftString, nint> ();
			// foo.Add (SwiftString.FromString ("key", 43);
			// var bar = foo.Value<SwiftString, nint, nint> (SwiftString.FromString ("key"), StructMarshal.Marshaler.Metatypeof (typeof (nint)));
			// Console.WriteLine (bar.HasValue);

			var fooID = new CSIdentifier ("foo");
			var barID = new CSIdentifier ("bar");
			var stringExpr = new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("key"));
			var fooDecl = CSVariableDeclaration.VarLine (fooID, new CSFunctionCall ("SwiftDictionary<SwiftString, nint>", true));
			var fooAdd = CSFunctionCall.FunctionCallLine ($"{fooID.Name}.Add", false, stringExpr, CSConstant.Val (43));
			var barDecl = CSVariableDeclaration.VarLine (barID, new CSFunctionCall ($"{fooID.Name}.Value<SwiftString, nint, nint>", false, stringExpr,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, new CSSimpleType ("nint").Typeof ())));
			var printHasIt = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{barID.Name}.HasValue"));

			var callingCode = CSCodeBlock.Create (fooDecl, fooAdd, barDecl, printHasIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}
	}
}
