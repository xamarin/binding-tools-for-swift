// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
	public class EquatableTests {
		
		void SimpleEquals (string typeLabel, CSBaseExpression lhs, CSBaseExpression rhs, string expected)
		{
			var swiftCode =
				$"public func areEqual{typeLabel}<T:Equatable>(a: T, b: T) -> Bool {{\n" +
				"    return a == b\n" +
				"}\n";
			
			var isEqual = new CSFunctionCall ($"TopLevelEntities.AreEqual{typeLabel}", false, lhs, rhs);
			var printer = CSFunctionCall.ConsoleWriteLine (isEqual);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName: $"SimpleEquals{typeLabel}");
			
		}

		[Test]
		public void SimpleEqualsInt ()
		{
			SimpleEquals ("Int32", CSConstant.Val (4), CSConstant.Val (4), "True\n");
		}


		[Test]
		public void SimpleEqualsFloat ()
		{
			SimpleEquals ("Float", CSConstant.Val (4.1f), CSConstant.Val (4.1f), "True\n");
		}


		[Test]
		public void SimpleEqualsDouble ()
		{
			SimpleEquals ("Double", CSConstant.Val (4.1), CSConstant.Val (4.1), "True\n");
		}

		[Test]
		public void SimpleEqualsBool ()
		{
			SimpleEquals ("Bool", CSConstant.Val (true), CSConstant.Val (true), "True\n");
		}

		[Test]
		public void SimpleEqualsString ()
		{
			SimpleEquals ("String", new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("hi mom")),
				      new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("hi mom")), "True\n");
		}

		[Test]
		public void SimpleNEqualsInt ()
		{
			SimpleEquals ("NInt32", CSConstant.Val (7), CSConstant.Val (4), "False\n");
		}


		[Test]
		public void SimpleNEqualsFloat ()
		{
			SimpleEquals ("NFloat", CSConstant.Val (7.1f), CSConstant.Val (4.1f), "False\n");
		}


		[Test]
		public void SimpleNEqualsDouble ()
		{
			SimpleEquals ("NDouble", CSConstant.Val (7.1), CSConstant.Val (4.1), "False\n");
		}

		[Test]
		public void SimpleNEqualsBool ()
		{
			SimpleEquals ("NBool", CSConstant.Val (false), CSConstant.Val (true), "False\n");
		}

		[Test]
		public void SimpleNEqualsString ()
		{
			SimpleEquals ("NString", new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("hi mom")),
			              new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("bye mom")), "False\n");
		}

		[Test]
		public void ClassEquals ()
		{
			// Note for future Steve
			// If an argument is a protocol with associated types, the calling conventions are different.
			// Typically, if a function accepts a protocol type as its argument it gets passed in as an existential
			// container.
			// It appears that if a function is generic with a protocol constraint then the argument gets
			// passed in as a pointer to the type then with the attendant metadata and protocol witness table
			// As a result, this code crashes since we're passing in the wrong type as far as I can tell.
			// This clearly needs more investigation

			// note to current or future Steve:
			// The calling conventions for this are:
			// rdi - pointer to value containing a
			// rsi - pointer to value containing b
			// rdx - metadata for type T
			// rcx - protocol witness table for T
			//
			// in pinvoke terms, this is
			// extern static void areEqualClass(IntPtr a, IntPtr b, SwiftMetatype mt, IntPtr protowitness);

			var swiftCode =
				$"public func areEqualClass<T:Equatable>(a: T, b: T) -> Bool {{\n" +
				"    return a == b\n" +
				"}\n" +
				"public protocol XXEquals {\n" +
				"    func Equals() -> Bool\n" +
				"}\n"
				;

			var eqClass = new CSClass (CSVisibility.Public, "EqClass");
			var field = CSVariableDeclaration.VarLine (CSSimpleType.Int, "X");
			eqClass.Fields.Add (field);
			var ctorParms = new CSParameterList ();
			ctorParms.Add (new CSParameter (CSSimpleType.Int, new CSIdentifier ("x")));
			var assignLine = CSAssignment.Assign ("X", new CSIdentifier ("x"));
			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, new CSIdentifier ("EqClass"), ctorParms,
						 CSCodeBlock.Create (assignLine));
			eqClass.Constructors.Add (ctor);
			eqClass.Inheritance.Add (typeof (ISwiftEquatable));

			var eqParms = new CSParameterList ();
			eqParms.Add (new CSParameter (new CSSimpleType ("ISwiftEquatable"), new CSIdentifier ("other")));
			var castLine = CSVariableDeclaration.VarLine (new CSSimpleType ("EqClass"), "otherEqClass",
								      new CSBinaryExpression (CSBinaryOperator.As, new CSIdentifier ("other"), new CSIdentifier ("EqClass")));
			var nonNull = new CSBinaryExpression (CSBinaryOperator.NotEqual, new CSIdentifier ("otherEqClass"), CSConstant.Null);
			var valsEq = new CSBinaryExpression (CSBinaryOperator.Equal, new CSIdentifier ("otherEqClass.X"), new CSIdentifier ("X"));
			var returnLine = CSReturn.ReturnLine (new CSBinaryExpression (CSBinaryOperator.And, nonNull, valsEq));
			var opEquals = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Bool, new CSIdentifier ("OpEquals"), eqParms,
						     CSCodeBlock.Create (castLine, returnLine));
			eqClass.Methods.Add (opEquals);


			var newClass = new CSFunctionCall ("EqClass", true, CSConstant.Val (5));
			var isEqual = new CSFunctionCall ($"TopLevelEntities.AreEqualClass", false, newClass, newClass);
			var printer = CSFunctionCall.ConsoleWriteLine (isEqual);
			var callingCode = CSCodeBlock.Create (printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", otherClass : eqClass, platform: PlatformName.macOS);
		}


		public class EqClass : ISwiftEquatable {
			public int X;
			public EqClass (int x)
			{
				X = x;
			}
			public bool OpEquals (ISwiftEquatable other)
			{
				var otherEqClass = other as EqClass;
				return otherEqClass != null && otherEqClass.X == X;
			}
		}
	}
}
