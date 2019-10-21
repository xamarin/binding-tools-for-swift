using System;
using NUnit.Framework;
using tomwiftytest;
using Dynamo;
using Dynamo.CSLang;
using SwiftRuntimeLibrary;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ComparableTests {
		void SimpleLess (string typeLabel, CSBaseExpression lhs, CSBaseExpression rhs, string expected)
		{
			var swiftCode =
				$"public func isLess{typeLabel}<T:Comparable>(a: T, b: T) -> Bool {{\n" +
				"    return a <= b\n" +
				"}\n";

			var isEqual = new CSFunctionCall ($"TopLevelEntities.IsLess{typeLabel}", false, lhs, rhs);
			var printer = CSFunctionCall.ConsoleWriteLine (isEqual);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"SimpleLess{typeLabel}");
		}


		[Test]
		public void SimpleLessInt()
		{
			SimpleLess("Int32", CSConstant.Val (3), CSConstant.Val (4), "True\n");
		}

		[Test]
		public void SimpleLessFloat ()
		{
			SimpleLess ("Float", CSConstant.Val (3.1f), CSConstant.Val (4.1f), "True\n");
		}

		[Test]
		public void SimpleLessDouble ()
		{
			SimpleLess ("Double", CSConstant.Val (3.1), CSConstant.Val (4.1), "True\n");
		}

		[Test]
		public void SimpleLessString ()
		{
			SimpleLess ("String", new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("aa")),
			            new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("bb")), "True\n");
		}


		[Test]
		public void SimpleNLessInt ()
		{
			SimpleLess ("NInt32", CSConstant.Val (4), CSConstant.Val (3), "False\n");
		}

		[Test]
		public void SimpleNLessFloat ()
		{
			SimpleLess ("NFloat", CSConstant.Val (4.1f), CSConstant.Val (3.1f), "False\n");
		}

		[Test]
		public void SimpleNLessDouble ()
		{
			SimpleLess ("NDouble", CSConstant.Val (4.1), CSConstant.Val (3.1), "False\n");
		}

		[Test]
		public void SimpleNLessString ()
		{
			SimpleLess ("NString", new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("bb")),
			            new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("aa")), "False\n");
		}


		[Test]
		public void ClassLess ()
		{
			// Note for future Steve
			// If an argument is a protocol with associated types, the calling conventions are different.
			// Typically, if a function accepts a protocol type as its argument it gets passed in as an existential
			// container.
			// It appears that if a function is generic with a protocol constraint then the argument gets
			// passed in as a pointer to the type then with the attendant metadata and protocol witness table
			// As a result, this code crashes since we're passing in the wrong type as far as I can tell.
			// This clearly needs more investigation

			var swiftCode =
				$"public func isLessClass<T:Comparable>(a: T, b: T) -> Bool {{\n" +
				"    return a < b\n" +
				"}\n";

			var lessClass = new CSClass (CSVisibility.Public, "LessClass");
			lessClass.Inheritance.Add (new CSIdentifier ("ISwiftComparable"));
			var field = CSVariableDeclaration.VarLine (CSSimpleType.Int, "X");
			lessClass.Fields.Add (field);
			var ctorParms = new CSParameterList ();
			ctorParms.Add (new CSParameter (CSSimpleType.Int, new CSIdentifier ("x")));
			var assignLine = CSAssignment.Assign ("X", new CSIdentifier ("x"));
			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, new CSIdentifier ("LessClass"), ctorParms,
						 CSCodeBlock.Create (assignLine));
			lessClass.Constructors.Add (ctor);


			var eqParms = new CSParameterList ();
			eqParms.Add (new CSParameter (new CSSimpleType ("ISwiftEquatable"), new CSIdentifier ("other")));
			var castLine = CSVariableDeclaration.VarLine (new CSSimpleType ("LessClass"), "otherLessClass",
								      new CSBinaryExpression (CSBinaryOperator.As, new CSIdentifier ("other"), new CSIdentifier ("LessClass")));
			var nonNull = new CSBinaryExpression (CSBinaryOperator.NotEqual, new CSIdentifier ("otherLessClass"), CSConstant.Null);
			var valsEq = new CSBinaryExpression (CSBinaryOperator.Equal, new CSIdentifier ("otherLessClass.X"), new CSIdentifier ("X"));
			var returnLine = CSReturn.ReturnLine (new CSBinaryExpression (CSBinaryOperator.And, nonNull, valsEq));
			var opEquals = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Bool, new CSIdentifier ("OpEquals"), eqParms,
						     CSCodeBlock.Create (castLine, returnLine));
			lessClass.Methods.Add (opEquals);


			var lessParms = new CSParameterList ();
			lessParms.Add (new CSParameter (new CSSimpleType ("ISwiftComparable"), new CSIdentifier ("other")));
			var lessCastLine = CSVariableDeclaration.VarLine (new CSSimpleType ("LessClass"), "otherLessClass",
								      new CSBinaryExpression (CSBinaryOperator.As, new CSIdentifier ("other"), new CSIdentifier ("LessClass")));
			nonNull = new CSBinaryExpression (CSBinaryOperator.NotEqual, new CSIdentifier ("otherLessClass"), CSConstant.Null);
			valsEq = new CSBinaryExpression (CSBinaryOperator.Less, new CSIdentifier ("otherLessClass.X"), new CSIdentifier ("X"));
			returnLine = CSReturn.ReturnLine (new CSBinaryExpression (CSBinaryOperator.And, nonNull, valsEq));
			var opLess = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Bool, new CSIdentifier ("OpLess"), lessParms,
						   CSCodeBlock.Create (lessCastLine, returnLine));
			lessClass.Methods.Add (opLess);


			var newClass = new CSFunctionCall ("LessClass", true, CSConstant.Val (5));
			var isEqual = new CSFunctionCall ($"TopLevelEntities.IsLessClass", false, newClass, newClass);
			var printer = CSFunctionCall.ConsoleWriteLine (isEqual);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\n", otherClass : lessClass, platform: PlatformName.macOS);

		}

	}
}
