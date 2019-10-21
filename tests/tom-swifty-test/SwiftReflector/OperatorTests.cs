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
using System.Reflection;
using SwiftReflector.TypeMapping;


namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class OperatorTests {


		[Test]
		public void OperatorSmokeTest0 ()
		{
			var swiftCode =
				"import Darwin\n" +
				"infix operator ** : MultiplicationPrecedence\n" +
				"public func ** (left: Double, right: Double) -> Double {\n" +
				"    return pow(left, right)\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.InfixOperatorStarStar", CSConstant.Val (2.0), CSConstant.Val (8.0)));
			var callingCode = new CodeElementCollection<ICodeElement> {
				printIt
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "256\n");

		}

		[Test]
		public void OperatorSmokeTest1 ()
		{
			var swiftCode =
				"prefix operator *-\n" +
				"public prefix func *- (arg: Int) -> Int {\n" +
				"    return -1 * arg\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.PrefixOperatorStarMinus", CSConstant.Val (47)));
			var callingCode = new CodeElementCollection<ICodeElement> {
				printIt
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "-47\n");
		}


		[Test]
		public void OperatorSmokeTest2 ()
		{
			var swiftCode =
				"postfix operator -*\n" +
				"public postfix func -* (arg: Int) -> Int {\n" +
				"    return -1 * arg\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.PostfixOperatorMinusStar", CSConstant.Val (88)));
			var callingCode = new CodeElementCollection<ICodeElement> {
				printIt
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "-88\n");

		}


		[Test]
		public void OperatorSmokeTest3 ()
		{
			var swiftCode =
				"postfix operator ^^\n" +
				"public postfix func ^^ (arg: String) -> String {\n" +
				"    return \"//\" + arg + \"//\"\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("TopLevelEntities.PostfixOperatorHatHat",
												CSFunctionCall.Function ("SwiftString.FromString", CSConstant.Val ("nothing"))));
			var callingCode = new CodeElementCollection<ICodeElement> {
				printIt
			};
			TestRunning.TestAndExecute (swiftCode, callingCode, "//nothing//\n");

		}

		[Test]
		public void OperatorCompositionNoInvoke ()
		{
			var swiftCode =
				"infix operator ∘\n" +
				"    public func ∘<T>(left: @escaping (T) -> (T), right: @escaping (T) -> (T)) -> (T) -> (T) {\n" +
				"        return { (x) in\n" +
				"            left (right(x))\n" +
				"        }\n" +
				"}\n";

			var lbody1 = new CSCodeBlock ();
			var lid = new CSIdentifier ("d");
			lbody1.Add (CSReturn.ReturnLine (lid * CSConstant.Val (2.0)));
			var pl = new CSParameterList ();
			pl.Add (new CSParameter (CSSimpleType.Double, lid));
			var lam1 = new CSLambda (pl, lbody1);
			var lbody2 = new CSCodeBlock ();
			lbody2.Add (CSReturn.ReturnLine (lid * CSConstant.Val (3.0)));
			var lam2 = new CSLambda (pl, lbody2);

			var compFunc = CSFunctionCall.FunctionCallLine ("TopLevelEntities.InfixOperatorRing", false, lam1, lam2);
			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val (12.0));

			var callingCode = new CodeElementCollection<ICodeElement> {
				compFunc, printIt
			};

			TestRunning.TestAndExecute (swiftCode, callingCode, "12\n");
		}


		[Test]
		[Ignore ("bizarre failure in calling back")]
		public void OperatorComposition ()
		{
			var swiftCode =
				"infix operator ∘\n" +
				"    public func ∘<T>(left: @escaping (T) -> (T), right: @escaping (T) -> (T)) -> (T) -> (T) {\n" +
				"        return { (x) in\n" +
				"            left (right(x))\n" +
				"        }\n" +
				"}\n";

			var lbody1 = new CSCodeBlock ();
			var lid = new CSIdentifier ("d");
			lbody1.Add (CSReturn.ReturnLine (lid * CSConstant.Val (2.0)));
			var pl = new CSParameterList ();
			pl.Add (new CSParameter (CSSimpleType.Double, lid));
			var lam1 = new CSLambda (pl, lbody1);
			var lbody2 = new CSCodeBlock ();
			lbody2.Add (CSReturn.ReturnLine (lid * CSConstant.Val (3.0)));
			var lam2 = new CSLambda (pl, lbody2);

			var compFunc = CSVariableDeclaration.VarLine (new CSSimpleType ("Func", false, CSSimpleType.Double, CSSimpleType.Double),
								      "compLam", new CSFunctionCall ("TopLevelEntities.InfixOperatorRing", false,
												     lam1, lam2));
			var printIt = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("compLam", CSConstant.Val (2.0)));

			var callingCode = new CodeElementCollection<ICodeElement> {
				compFunc, printIt
			};

			TestRunning.TestAndExecute (swiftCode, callingCode, "12\n");
		}


		[Test]
		public void MultiplicationConflict ()
		{
			var swiftCode =
				"infix operator × : MultiplicationPrecedence\n" +
				"public func × (left: Double, right: Double) -> Double {\n" +
				"    return left * right\n" +
				"}\n" +
				"public func × (left: (Double, Double, Double), right: (Double, Double, Double)) -> (Double, Double, Double) {\n" +
				"    let a = left.1 * right.2 - left.2 * right.1\n" +
				"    let b = left.2 * right.0 - left.0 * right.2\n" +
				"    let c = left.0 * right.1 - left.1 * right.0\n" +
				"    return (a, b, c)\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("success"));
			var callingCode = new CodeElementCollection<ICodeElement> { printIt };

			TestRunning.TestAndExecute (swiftCode, callingCode, "success\n");
		}


		[Test]
		public void StructInfixOpTest ()
		{
			var swiftCode = @"
public struct IntRep {
    public init (with: Int32) {
		val = with
	}
    public var val:Int32 = 0
}

infix operator %^^% : AdditionPrecedence
public func %^^% (left:IntRep, right: IntRep) -> IntRep {
    return IntRep (with: left.val + right.val)
}
";

			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSFunctionCall ("IntRep", true, CSConstant.Val (3)));
			var rightID = new CSIdentifier ("right");
			var rightDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, rightID, new CSFunctionCall ("IntRep", true, CSConstant.Val (4)));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.InfixOperatorPercentHatHatPercent", false, leftID, rightID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID.Dot (new CSIdentifier ("Val")));

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, rightDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");
		}

		[Test]
		public void StructPrefixOpTest ()
		{
			var swiftCode = @"
public struct IntRep1 {
    public init (with: Int32) {
		val = with
	}
    public var val:Int32 = 0
}

prefix operator %++% 
public prefix func %++% (left:IntRep1) -> IntRep1 {
    return IntRep1 (with: left.val + 1)
}
";

			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSFunctionCall ("IntRep1", true, CSConstant.Val (3)));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.PrefixOperatorPercentPlusPlusPercent", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID.Dot (new CSIdentifier ("Val")));

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "4\n");
		}


		[Test]
		public void StructPostfixOpTest ()
		{
			var swiftCode = @"
public struct IntRep2 {
    public init (with: Int32) {
		val = with
	}
    public var val:Int32 = 0
}

prefix operator %--% 
public prefix func %--% (left:IntRep2) -> IntRep2 {
    return IntRep2 (with: left.val - 1)
}
";

			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSFunctionCall ("IntRep2", true, CSConstant.Val (3)));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.PrefixOperatorPercentMinusMinusPercent", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID.Dot (new CSIdentifier ("Val")));

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "2\n");
		}


		[Test]
		public void EnumInfixOpTest ()
		{
			var swiftCode = @"
public enum IntOrFloat {
	case IntVal(Int)
	case FloatVal(Float)
}
infix operator ^++^ : AdditionPrecedence
public func ^++^ (left: IntOrFloat, right: IntOrFloat) -> IntOrFloat {
    switch left {
    case .IntVal(let x):
        switch right {
        case .IntVal(let y):
            return .IntVal(x + y)
        case .FloatVal(let y):
            return .FloatVal(Float(x) + y)
        }
    case .FloatVal(let x):
        switch right {
        case .IntVal(let y):
            return .FloatVal(x + Float(y))
        case .FloatVal(let y):
            return .FloatVal(x + y)
        }
    }
}
";
			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSFunctionCall ("IntOrFloat.NewIntVal", false, CSConstant.Val (3)));
			var rightID = new CSIdentifier ("right");
			var rightDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, rightID, new CSFunctionCall ("IntOrFloat.NewIntVal", false, CSConstant.Val (4)));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.InfixOperatorHatPlusPlusHat", false, leftID, rightID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID.Dot (new CSIdentifier ("ValueIntVal")));

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, rightDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "7\n");

		}

		[Test]
		public void SimpleEnumInfixOpTest ()
		{
			var swiftCode = @"
public enum CompassPoints {
	case North, East, South, West
}
prefix operator ^-^
public prefix func ^-^(val: CompassPoints) -> CompassPoints {
	switch val {
	case .North: return .South
	case .East: return .West
	case .South: return .North
	case .West: return .East
	}
}
";
			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSIdentifier ("CompassPoints.North"));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.PrefixOperatorHatMinusHat", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID);

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "South\n");

		}

		[Test]
		public void SimpleEnumPostfixOpTest ()
		{
			var swiftCode = @"
public enum CompassPoints1 {
	case North, East, South, West
}
postfix operator ^+^
public postfix func ^+^(val: CompassPoints1) -> CompassPoints1 {
	switch val {
	case .North: return .East
	case .East: return .South
	case .South: return .West
	case .West: return .North
	}
}
";
			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSIdentifier ("CompassPoints1.North"));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.PostfixOperatorHatPlusHat", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID);

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "East\n");

		}



		[Test]
		public void SuperSimpleEnumTest ()
		{
			var swiftCode = @"
import Foundation

@objc
public enum CompassPoints2 : Int {
    case North = 0, East, South, West
}

prefix operator ^*^
public prefix func ^*^(val: CompassPoints2) -> CompassPoints2 {
    switch val {
    case .North: return .South
    case .East: return .West
    case .South: return .North
    case .West: return .East
    }
}";
			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSIdentifier ("CompassPoints2.North"));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("TopLevelEntities.PrefixOperatorHatStarHat", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID);

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "South\n");

		}


		[Test]
		public void StructInlinePrefixOperator ()
		{
			var swiftCode = @"
prefix operator ^*-*^

public struct NumRep0 {
    public init (a: Int) {
        val = a
    }
    public var val: Int
    public static prefix func ^*-*^(val: NumRep0) -> NumRep0 {
        return NumRep0 (a: -val.val)
    }
}";

			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSFunctionCall ("NumRep0", true, CSConstant.Val(4)));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("NumRep0.PrefixOperatorHatStarMinusStarHat", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID.Dot (new CSIdentifier ("Val")));

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "-4\n");

		}

		[Test]
		public void ClassInlinePostfixOperator ()
		{
			var swiftCode = @"
postfix operator ^*--*^

public class NumRep1 {
    public init (a: Int) {
        val = a
    }
    public var val: Int
    public static postfix func ^*--*^(val: NumRep1) -> NumRep1 {
        return NumRep1 (a: -val.val)
    }
}";

			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSFunctionCall ("NumRep1", true, CSConstant.Val (4)));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("NumRep1.PostfixOperatorHatStarMinusMinusStarHat", false, leftID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID.Dot (new CSIdentifier ("Val")));

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "-4\n");
		}

		[Test]
		public void EnumInlineInfixOperator ()
		{
			var swiftCode = @"
infix operator ^*=*^
public enum CompassPoints3 {
	case North, East, South, West

	public static func ^*=*^(left: CompassPoints3, right: CompassPoints3) -> Bool {
		return left == right
	}
}
";
			var leftID = new CSIdentifier ("left");
			var leftDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, leftID, new CSIdentifier ("CompassPoints3.North"));
			var rightID = new CSIdentifier ("right");
			var rightDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, rightID, new CSIdentifier ("CompassPoints3.East"));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("CompassPoints3Extensions.InfixOperatorHatStarEqualsStarHat", false, leftID, rightID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID);

			var callingCode = new CodeElementCollection<ICodeElement> { leftDecl, rightDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n");
		}


		[Test]
		public void EnumInlinePrefixOperator ()
		{
			var swiftCode = @"
prefix operator ^+-+^
public enum CompassPoints4 {
	case North, East, South, West

	public static prefix func ^+-+^ (item: CompassPoints4) -> CompassPoints4 {
		switch item {
		case .North: return .West
		case .East: return .North
		case .South: return .East
		case .West: return .South
		}
	}
}
";
			var itemID = new CSIdentifier ("item");
			var itemDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, itemID, new CSIdentifier ("CompassPoints4.North"));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, resultID, new CSFunctionCall ("CompassPoints4Extensions.PrefixOperatorHatPlusMinusPlusHat", false, itemID));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID);

			var callingCode = new CodeElementCollection<ICodeElement> { itemDecl, resultDecl, printer };
			TestRunning.TestAndExecute (swiftCode, callingCode, "West\n");
		}

	}

}
