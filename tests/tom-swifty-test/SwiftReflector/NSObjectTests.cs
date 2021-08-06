// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using Dynamo.CSLang;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class NSObjectTests {
		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void NSObjectIdentity (PlatformName platform)
		{
			string swiftCode =
				"import Foundation\n" +
				"public func NSIdentity (a: NSObject) -> NSObject {\n" +
				"    return a\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var nsID = new CSIdentifier ("ns");
			var nsresultID = new CSIdentifier ("nsresult");
			var nsdecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, nsID, new CSFunctionCall ("NSObject", true));
			var nsresultdecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, nsresultID,
									  new CSFunctionCall ("TopLevelEntities.NSIdentity", false, nsID));
			CSLine call = CSFunctionCall.ConsoleWriteLine (nsID == nsresultID);
			callingCode.Add (nsdecl);
			callingCode.Add (nsresultdecl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", platform: platform);

		}

		[Test]
		public void NSObjectSubTest1 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public class SubTest1 : NSObject {\n" +
				"    public override init () { }\n" +
				"    public func returnsThree () -> Int {\n" +
				"         return 3\n" +
				"    }\n" +
				"}\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();
			var objID = new CSIdentifier ("subTest");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, new CSFunctionCall ("SubTest1", true));
			var call = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("subTest.ReturnsThree"));

			callingCode.Add (objDecl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, "3\n", platform: PlatformName.macOS);
		}

		[Test]
		public void NSObjectSubTest2 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public class SubTest2 : NSObject {\n" +
				"    public override init () { }\n" +
				"    public func returnsThreePointOne () -> Double {\n" +
				"         return 3.1\n" +
				"    }\n" +
				"}\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();
			var objID = new CSIdentifier ("subTest");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, new CSFunctionCall ("SubTest2", true));
			var call = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("subTest.ReturnsThreePointOne"));

			callingCode.Add (objDecl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, "3.1\n", platform: PlatformName.macOS);
		}

		[Test]
		public void NSObjectSubTest3 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public class SubTest3 : NSObject {\n" +
				"    public override init () { }\n" +
				"    public func returnsTrue () -> Bool {\n" +
				"         return true\n" +
				"    }\n" +
				"}\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();
			var objID = new CSIdentifier ("subTest");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, new CSFunctionCall ("SubTest3", true));
			var call = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("subTest.ReturnsTrue"));

			callingCode.Add (objDecl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", platform: PlatformName.macOS);
		}

		[Test]
		public void NSObjectSubclassableMethodTest1 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"open class Subclassable1 : NSObject {\n" +
				"   public override init () { }\n" +
				"   open func returnsTrue () -> Bool {\n" +
				"       return true\n" +
				"   }\n" +
				"}\n";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			var objID = new CSIdentifier ("subTest");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, new CSFunctionCall ("Subclassable1", true));
			var call = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("subTest.ReturnsTrue"));

			callingCode.Add (objDecl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", platform: PlatformName.macOS);
		}

		[Test]
		public void NSObjectSubclassableMethodTest2 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"open class Subclassable2 : NSObject {\n" +
				"   public override init () { }\n" +
				"   open func returnsTrue () -> Bool {\n" +
				"       return true\n" +
				"   }\n" +
				"}\n" +
				"public func callIt (a: Subclassable2) -> Bool {\n" +
				"    return a.returnsTrue()\n" +
				"}\n";


			var theSub = new CSClass (CSVisibility.Public, "TheSub2");
			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, theSub.Name,
						 new CSParameterList (), new CSBaseExpression [0], true, new CSCodeBlock ());
			theSub.Constructors.Add (ctor);
			theSub.Inheritance.Add (new CSIdentifier ("Subclassable2"));

			var theBody = new CSCodeBlock ();
			theBody.Add (CSReturn.ReturnLine (CSConstant.Val (false)));

			var returnsFalse = new CSMethod (CSVisibility.Public, CSMethodKind.Override, CSSimpleType.Bool,
							 new CSIdentifier ("ReturnsTrue"), new CSParameterList (), theBody);
			theSub.Methods.Add (returnsFalse);


			var callingCode = new CodeElementCollection<ICodeElement> ();
			var objID = new CSIdentifier ("subTest");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, new CSFunctionCall ("TheSub2", true));
			var call = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("subTest.ReturnsTrue"));
			var call2 = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.CallIt", objID));
			callingCode.Add (objDecl);
			callingCode.Add (call);
			callingCode.Add (call2);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\nFalse\n", otherClass: theSub, platform: PlatformName.macOS);
		}

		[Test]
		public void NSObjectSubclassableMethodTest3 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"open class Subclassable3 : NSObject {\n" +
				"   public override init () { }\n" +
				"   open var returnsTrue:Bool {\n" +
				"       get { return true\n } " +
				"   }\n" +
				"}\n" +
				"public func callIt (a: Subclassable3) -> Bool {\n" +
				"    return a.returnsTrue\n" +
				"}\n";


			var theSub = new CSClass (CSVisibility.Public, "TheSub3");
			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, theSub.Name,
						 new CSParameterList (), new CSBaseExpression [0], true, new CSCodeBlock ());
			theSub.Constructors.Add (ctor);
			theSub.Inheritance.Add (new CSIdentifier ("Subclassable3"));

			var theBody = new CSCodeBlock ();
			theBody.Add (CSReturn.ReturnLine (CSConstant.Val (false)));

			LineCodeElementCollection<ICodeElement> getCode =
				new LineCodeElementCollection<ICodeElement> (
					new ICodeElement [] {
						CSReturn.ReturnLine (CSConstant.Val (false))
					}, false, true);
			CSProperty returnsFalse = new CSProperty (CSSimpleType.Bool, CSMethodKind.Override, new CSIdentifier ("ReturnsTrue"),
							  CSVisibility.Public, new CSCodeBlock (getCode),
							  CSVisibility.Public, null);

			theSub.Properties.Add (returnsFalse);


			var callingCode = new CodeElementCollection<ICodeElement> ();
			var objID = new CSIdentifier ("subTest");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, new CSFunctionCall ("TheSub3", true));
			var call = CSFunctionCall.ConsoleWriteLine (objID.Dot ((CSIdentifier)"ReturnsTrue"));
			var call2 = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.CallIt", objID));
			callingCode.Add (objDecl);
			callingCode.Add (call);
			callingCode.Add (call2);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\nFalse\n", otherClass: theSub, platform: PlatformName.macOS);
		}


		[Test]
		public void NSObjectObjCMethodTest0 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"public class ItsSimple : NSObject {\n" +
				"    public override init () { }\n" +
				"    @objc public func returns15() -> Int {\n" +
				"        return 15\n" +
				"    }\n" +
				"}\n";

			var objID = new CSIdentifier ("obj");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, CSFunctionCall.Ctor ("ItsSimple"));
			var printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function (objID.Name + ".Returns15"));
			var callingCode = CSCodeBlock.Create (objDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "15\n", platform: PlatformName.macOS);
		}



		[Test]
		public void NSObjectObjCMethodTest1 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"@objc\n" +
				"open class ItsLessSimple : NSObject {\n" +
				"    public override init () { }\n" +
				"    @objc open func returns15() -> Int {\n" +
				"        return 15\n" +
				"    }\n" +
				"}\n";

			var objID = new CSIdentifier ("obj");
			var objDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, objID, CSFunctionCall.Ctor ("ItsLessSimple"));
			var printer = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function (objID.Name + ".Returns15"));
			var callingCode = CSCodeBlock.Create (objDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "15\n", platform: PlatformName.macOS);
		}


		[Test]
		public void NSImageViewSmokeTest ()
		{
			string swiftCode =
				"import Foundation\n" +
				"import Cocoa\n" +
				"@IBDesignable\n" +
				"public class NSImageX : NSImageView {\n" +
				"    public override func draw (_ dirtyRect: NSRect) {\n" +
				"        super.draw(dirtyRect)\n" +
				"        let path = NSBezierPath ()\n" +
				"        path.lineWidth = 8.0\n" +
				"        path.move(to: NSPoint (x:frame.minX, y:frame.minY))\n" +
				"        path.line(to: NSPoint(x:frame.maxX, y:frame.maxY))\n" +
				"        path.move(to: NSPoint (x:frame.maxX, y:frame.minY))\n" +
				"        path.line(to: NSPoint (x:frame.minX, y: frame.maxY))\n" +
				"        NSColor.red.set()\n" +
				"        path.stroke()\n" +
				"    }\n" +
				"}\n";


			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n", platform: PlatformName.macOS);
		}


		[Test]
		[Ignore ("getting a conflict in the C# wrapping (check signature?) ")]
		public void NSImageViewSmokeTest1 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"import Cocoa\n" +
				"@IBDesignable\n" +
				"public class NSImageX : NSImageView {\n" +
				"    public override func display () {\n" +
				"        super.display()\n" +
				"    }\n" +
				"}\n";


			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n", platform: PlatformName.macOS);
		}


		[Test]
		public void NSImageViewSmokeTest2 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"import Cocoa\n" +
				"@IBDesignable\n" +
				"open class NSImageX : NSImageView {\n" +
				"    open override func draw (_ dirtyRect: NSRect) {\n" +
				"        super.draw(dirtyRect)\n" +
				"        let path = NSBezierPath ()\n" +
				"        path.lineWidth = 8.0\n" +
				"        path.move(to: NSPoint (x:frame.minX, y:frame.minY))\n" +
				"        path.line(to: NSPoint(x:frame.maxX, y:frame.maxY))\n" +
				"        path.move(to: NSPoint (x:frame.maxX, y:frame.minY))\n" +
				"        path.line(to: NSPoint (x:frame.minX, y: frame.maxY))\n" +
				"        NSColor.red.set()\n" +
				"        path.stroke()\n" +
				"    }\n" +
				"}\n";


			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n", platform: PlatformName.macOS);
		}


		[Test]
		public void NSImageViewSmokeTest3 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"import Cocoa\n" +
				"@IBDesignable\n" +
				"open class NSImageX : NSImageView {\n" +
				"    open override func display () {\n" +
				"        super.display()\n" +
				"    }\n" +
				"}\n";


			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n", platform: PlatformName.macOS);
		}


		[Test]
		[Ignore ("")]
		public void NSImageViewSmokeTest4 ()
		{
			string swiftCode =
				"import Foundation\n" +
				"import Cocoa\n" +
				"@IBDesignable\n" +
				"open class NSImageX : NSImageView {\n" +
				"    open override var wantsLayer: Bool {\n" +
				"        get { return super.wantsLayer }\n" +
				"        set { super.wantsLayer = newValue }\n" +
				"    }\n" +
				"}\n";


			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n", platform: PlatformName.macOS);
		}

		[Test]
		public void ChartDataEntryBaseTest ()
		{
			string swiftCode =
				"import Foundation\n" +
				"open class ChartDataEntryBase : NSObject {\n" +
				"    public override required init () {\n" +
				"        super.init()\n" +
				"    }\n" +
				"    public init (y: Double) {\n" +
				"        super.init()\n" +
				"    }\n" +
				"    @objc public init(y: Double, data: AnyObject?) {\n" +
				"        super.init()\n" +
				"    }\n" +
				"}\n";

			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("ok"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "ok\n", platform: PlatformName.macOS);
		}

		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void VirtualMethodStructArg (PlatformName platform)
		{
			string swiftCode =
$"import Foundation\nopen class SomeVirtualClass{platform} : NSObject {{\n\tpublic override init () {{ }}\n\topen func StringVersion (v: OperatingSystemVersion) -> String {{\n\t\treturn \"\\(v.majorVersion).\\(v.minorVersion)\"\n\t}}\n}}\n";
			// var vers = new NSOperatingSystemVersion();
			// var cl = new SomeVirtualClass ();
			// var str = cl.StringVersion (vers);
			// Console.WriteLine (str.ToString());
			var versID = new CSIdentifier ("vers");
			var versDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, versID, new CSFunctionCall ("NSOperatingSystemVersion", true));
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, "cl", new CSFunctionCall ($"SomeVirtualClass{platform}", true));
			var strID = new CSIdentifier ("str");
			var strDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, strID, new CSFunctionCall ("cl.StringVersion", false, versID));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{strID.Name}.ToString", false));
			var callingCode = CSCodeBlock.Create (versDecl, clDecl, strDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "0.0\n", platform: platform);
		}

		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void VirtualPropStruct (PlatformName platform)
		{
			string swiftCode =
$"import Foundation\nopen class AnotherVirtualClass{platform} {{\n\tpublic init () {{ }}\n\tpublic var OSVersion = OperatingSystemVersion (majorVersion: 1, minorVersion:2, patchVersion: 3)\n}}\n";

			// var cl = new AnotherVirtualClass ();
			// var vers = cl.OSVersion;
			// Console.WriteLine(vers);
			// vers.Major = 5;
			// cl.OSVersion = vers;
			// vers = cl.OSVersion;
			// Console.WriteLine(vers);
			var versID = new CSIdentifier ("vers");
			var clID = new CSIdentifier ("cl");
			var osverExpr = clID.Dot (new CSIdentifier ("OSVersion"));
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ($"AnotherVirtualClass{platform}", true));
			var versDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, versID, osverExpr);
			var printer = CSFunctionCall.ConsoleWriteLine (versID);
			var setMajor = CSAssignment.Assign (versID.Dot (new CSIdentifier ("Major")), CSConstant.Val (5));
			var setOSVer = CSAssignment.Assign (osverExpr, versID);
			var resetVer = CSAssignment.Assign (versID, osverExpr);

			var callingCode = CSCodeBlock.Create (clDecl, versDecl, printer, setMajor, setOSVer, resetVer, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "1.2.3\n5.2.3\n", platform: platform);
		}


		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		public void VirtualOpenPropStruct (PlatformName platform)
		{
			string swiftCode =
	$"import Foundation\nopen class AnotherOpenVirtualClass{platform} {{\n\tpublic init () {{ }}\n\topen var OSVersion = OperatingSystemVersion (majorVersion: 1, minorVersion:2, patchVersion: 3)\n}}\n";

			// var cl = new AnotherVirtualClass ();
			// var vers = cl.OSVersion;
			// Console.WriteLine(vers);
			// vers.Major = 5;
			// cl.OSVersion = vers;
			// vers = cl.OSVersion;
			// Console.WriteLine(vers);
			var versID = new CSIdentifier ("vers");
			var clID = new CSIdentifier ("cl");
			var osverExpr = clID.Dot (new CSIdentifier ("OSVersion"));
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall ($"AnotherOpenVirtualClass{platform}", true));
			var versDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, versID, osverExpr);
			var printer = CSFunctionCall.ConsoleWriteLine (versID);
			var setMajor = CSAssignment.Assign (versID.Dot (new CSIdentifier ("Major")), CSConstant.Val (5));
			var setOSVer = CSAssignment.Assign (osverExpr, versID);
			var resetVer = CSAssignment.Assign (versID, osverExpr);

			var callingCode = CSCodeBlock.Create (clDecl, versDecl, printer, setMajor, setOSVer, resetVer, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "1.2.3\n5.2.3\n", platform: platform);
		}


		[Test]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.iOS)]
		[Ignore ("Waiting on Apple issue https://bugs.swift.org/browse/SR-13832")]
		public void CallAVirtualInACtor (PlatformName platform)
		{
			string swiftCode =
@"import Foundation
open class VirtInInit : NSObject {
	private var theValue:Int = 0;
	public init (value:Int) {
		super.init()
		setValue (value: value)
	}
	open func setValue (value: Int) {
		theValue = value
	}
	open func getValue () -> Int {
		return theValue;
	}
}
";
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, "cl", new CSFunctionCall ("VirtInInit", true, CSConstant.Val (5)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ("cl.GetValue", false));
			var setter = CSFunctionCall.FunctionCallLine ("cl.SetValue", CSConstant.Val (7));

			var callingCode = CSCodeBlock.Create (clDecl, printer, setter, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "5\n7\n", platform: platform);
		}
	}
}
