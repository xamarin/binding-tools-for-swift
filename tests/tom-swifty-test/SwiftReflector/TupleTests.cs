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


namespace SwiftReflector.Demangling {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class TupleTests {
		void Wrap2Tuple (string type1, string type2, string cstype1, string cstype2, string val1, string val2, string expected)
		{
			string typetype = type1 + type2;
			string swiftCode = $"public final class MontyW2T{typetype} {{ public init() {{ }}\n public static func tupe(a:{type1}, b: {type2}) -> ({type1}, {type2})\n {{\n return (a, b);\n }}\n}}\n";

			CSLine cstupe = CSVariableDeclaration.VarLine (new CSSimpleType ("Tuple", false, new CSSimpleType (cstype1),
										   new CSSimpleType (cstype2)), "tupe", new CSFunctionCall ($"MontyW2T{typetype}.Tupe", false,
					  new CSIdentifier (val1), new CSIdentifier (val2)));

			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"tupe");

			CSCodeBlock callingCode = CSCodeBlock.Create (cstupe, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"Wrap2Tuple{typetype}");
		}

		[Test]
		public void Test2TupeBoolBool ()
		{
			Wrap2Tuple ("Bool", "Bool", "bool", "bool", "false", "true", "(False, True)\n");
		}

		[Test]
		public void Test2TupeBoolInt ()
		{
			Wrap2Tuple ("Bool", "Int", "bool", "nint", "false", "42", "(False, 42)\n");
		}

		[Test]
		public void Test2TupeIntBool ()
		{
			Wrap2Tuple ("Int", "Bool", "nint", "bool", "42", "true", "(42, True)\n");
		}

		[Test]
		public void Test2TupeIntString ()
		{
			Wrap2Tuple ("Int", "String", "nint", "SwiftString", "42", "SwiftString.FromString(\"hi mom\")", "(42, hi mom)\n");
		}

		[Test]
		public void Test2TupeDoubleInt ()
		{
			Wrap2Tuple ("Double", "Int", "double", "nint", "42.1", "37", "(42.1, 37)\n");
		}

		[Test]
		public void Test2TupeIntDouble ()
		{
			Wrap2Tuple ("Int", "Double", "nint", "double", "37", "42.1", "(37, 42.1)\n");
		}


		void WrapMultiTuple (string type, int count, string appendage, string expected, int nothing, params string [] values)
		{
			StringBuilder sbargs = new StringBuilder ();
			StringBuilder sbtype = new StringBuilder ();
			StringBuilder sbret = new StringBuilder ();
			for (int i = 0; i < count; i++) {
				if (i > 0) {
					sbargs.Append (", ");
					sbtype.Append (", ");
					sbret.Append (", ");
				}
				sbargs.Append (String.Format ("a{0}: {1}", i, type));
				sbtype.Append (type);
				sbret.Append (String.Format ("a{0}", i));
			}

			string swiftCode =
			    $"public final class MontyWMT{appendage} {{ public init() {{ }}\n public static func tupe({sbargs.ToString ()}) -> ({sbtype.ToString ()})\n {{\n return ({sbret.ToString ()});\n }}\n}}\n";

			CSBaseExpression [] args = values.Select (str => (CSBaseExpression)new CSIdentifier (str)).ToArray ();

			CSLine cstupe = CSVariableDeclaration.VarLine (new CSSimpleType ("var"), "tupe",
								  new CSFunctionCall ($"MontyWMT{appendage}.Tupe", false, args));

			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"tupe");

			CSCodeBlock callingCode = CSCodeBlock.Create (cstupe, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"WrapMultiTuple{appendage}");
		}

		[Test]
		public void Tuple8Bools ()
		{
			WrapMultiTuple ("Bool", 8, "Tuple8Bools", "(False, True, True, False, True, False, False, True)\n", 0,
				       "false", "true", "true", "false", "true", "false", "false", "true");
		}

		[Test]
		public void Tuple10Ints ()
		{
			WrapMultiTuple ("Int32", 10, "Tuple10Ints", "(1, -2, 3, -4, 5, -6, 7, -8, 9, -10)\n", 0,
				       "1", "-2", "3", "-4", "5", "-6", "7", "-8", "9", "-10");
		}

		[Test]
		public void Tuple10Strings ()
		{
			WrapMultiTuple ("String", 10, "Tuple10Strings", "(one, two, three, four, five, six, seven, eight, nine, ten)\n", 0,
			    "SwiftString.FromString(\"one\")", "SwiftString.FromString(\"two\")",
			    "SwiftString.FromString(\"three\")", "SwiftString.FromString(\"four\")",
			    "SwiftString.FromString(\"five\")", "SwiftString.FromString(\"six\")",
			    "SwiftString.FromString(\"seven\")", "SwiftString.FromString(\"eight\")",
			    "SwiftString.FromString(\"nine\")", "SwiftString.FromString(\"ten\")");
		}

		[Test]
		public void Test10Doubles ()
		{
			WrapMultiTuple ("Double", 10, "Test10Doubles", "(0, 1.1, 2.2, 3.3, 4.4, 5.5, 6.6, 7.7, 8.8, 9.9)\n", 0,
			    "0", "1.1", "2.2", "3.3", "4.4", "5.5", "6.6", "7.7", "8.8", "9.9");
		}

		void TupleSwap (string type1, string type2, string cstype1, string cstype2, string val1, string val2, string expected)
		{
			string appendage = type1 + type2;
			string swiftCode = $"public final class MontyTupSwap{appendage} {{ public init() {{ }}\n public static func tupe(a:{type1}, b: {type2}) -> ({type2}, {type1})\n {{\nreturn (b, a);\n }}\n}}\n";

			CSLine cstupe = CSVariableDeclaration.VarLine (new CSSimpleType ("Tuple", false, new CSSimpleType (cstype2),
										   new CSSimpleType (cstype1)),
								  "tupe", new CSFunctionCall ($"MontyTupSwap{appendage}.Tupe", false,
											   new CSIdentifier (val1), new CSIdentifier (val2)));

			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"tupe");

			CSCodeBlock callingCode = CSCodeBlock.Create (cstupe, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"TupleSwap{appendage}");
		}

		[Test]
		public void SwapBoolBool ()
		{
			TupleSwap ("Bool", "Bool", "bool", "bool", "true", "false", "(False, True)\n");
		}

		[Test]
		public void SwapBoolInt ()
		{
			TupleSwap ("Bool", "Int32", "bool", "int", "true", "1", "(1, True)\n");
		}

		[Test]
		public void SwapIntBool ()
		{
			TupleSwap ("Int32", "Bool", "int", "bool", "12", "true", "(True, 12)\n");
		}

		[Test]
		public void SwapBoolString ()
		{
			TupleSwap ("Bool", "String", "bool", "SwiftString", "true", "SwiftString.FromString(\"hi mom\")", "(hi mom, True)\n");
		}

		[Test]
		public void SwapStringBool ()
		{
			TupleSwap ("String", "Bool", "SwiftString", "bool", "SwiftString.FromString(\"hi mom\")", "true", "(True, hi mom)\n");
		}

		void TupleProp (string type1, string type2, string cstype1, string cstype2, string val1, string val2, string expected)
		{
			string appendage = type1 + type2.Replace ('(', '_').Replace (')', '_').Replace (',', '_').Replace (' ', '_');
			string swiftCode = $"public final class MontyTupleProp{appendage} {{ public init() {{ }}\n public var prop:({type1}, {type2}) {{\n return ({val1}, {val2});\n }}\n}}\n";
			CSLine cstupe = CSVariableDeclaration.VarLine (new CSSimpleType ("Tuple", false, new CSSimpleType (cstype1),
										   new CSSimpleType (cstype2)),
								  "tupe", new CSFunctionCall ($"MontyTupleProp{appendage}", true).Dot (new CSIdentifier ("Prop")));

			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"tupe");

			CSCodeBlock callingCode = CSCodeBlock.Create (cstupe, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, expected, testName : $"TupleProp{appendage}");
		}

		[Test]
		public void TuplePropBoolBool ()
		{
			TupleProp ("Bool", "Bool", "bool", "bool", "true", "false", "(True, False)\n");
		}

		[Test]
		public void TuplePropBoolInt ()
		{
			TupleProp ("Bool", "Int32", "bool", "int", "true", "37", "(True, 37)\n");
		}

		[Test]
		public void TuplePropIntBool ()
		{
			TupleProp ("Int32", "Bool", "int", "bool", "37", "true", "(37, True)\n");
		}

		[Test]
		public void TuplePropIntDouble ()
		{
			TupleProp ("Int32", "Double", "int", "double", "37", "42.1", "(37, 42.1)\n");
		}

		[Test]
		public void TuplePropDoubleInt ()
		{
			TupleProp ("Double", "Int32", "double", "int", "42.1", "37", "(42.1, 37)\n");
		}

		[Test]
		public void TuplePropBoolString ()
		{
			TupleProp ("Bool", "String", "bool", "SwiftString", "true", "\"hi mom\"", "(True, hi mom)\n");
		}

		[Test]
		public void TuplePropStringBool ()
		{
			TupleProp ("String", "Bool", "SwiftString", "bool", "\"hi mom\"", "true", "(hi mom, True)\n");
		}

		[Test]
		public void TuplePropBoolTupleBoolBool ()
		{
			TupleProp ("Bool", "(Bool, Bool)", "bool", "Tuple<bool, bool>", "true",
								   "(true, false)", "(True, (True, False))\n");
		}

		void WrapVirtualTuple (string type1, string type2, string cstype1, string cstype2, string val1, string val2, string expected)
		{
			string appendage = type1 + type2;

			string swiftCode = TestRunningCodeGenerator.kSwiftFileWriter +
						      $"public class MontyWVT{appendage} {{ public init() {{ }}\n public func tupe(a:({type1}, {type2}))\n {{\nvar s = \"\"\n print(a, to:&s)\nwriteToFile(s, \"WrapVirtualTuple{appendage}\")\n }}\n}}\n";

			CSLine cstupe = CSVariableDeclaration.VarLine (new CSSimpleType ($"MontyWVT{appendage}"), "m", new CSFunctionCall ($"MontyWVT{appendage}", true));

			CSLine printer = CSFunctionCall.FunctionCallLine ("m.Tupe", false,
					   new CSFunctionCall (String.Format ("Tuple<{0},{1}>", cstype1, cstype2), true, new CSIdentifier (val1),
					       new CSIdentifier (val2)));

			CSCodeBlock callingCode = CSCodeBlock.Create (cstupe, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, expected, $"WrapVirtualTuple{appendage}");
		}

		[Test]
		public void TestVirtualTupleBoolBool ()
		{
			WrapVirtualTuple ("Bool", "Bool", "bool", "bool", "true", "false", "(true, false)\n");
		}

		[Test]
		public void TestVirtualTupleBoolInt ()
		{
			WrapVirtualTuple ("Bool", "Int32", "bool", "int", "true", "37", "(true, 37)\n");
		}

		[Test]
		public void TestVirtualTupleIntBool ()
		{
			WrapVirtualTuple ("Int32", "Bool", "int", "bool", "37", "true", "(37, true)\n");
		}

		[Test]
		public void TestVirtualTupleBoolDouble ()
		{
			WrapVirtualTuple ("Bool", "Double", "bool", "double", "true", "37.5", "(true, 37.5)\n");
		}

		[Test]
		public void TestVirtualTupleBoolString ()
		{
			WrapVirtualTuple ("Bool", "String", "bool", "SwiftString", "true", "SwiftString.FromString(\"hi mom\")", "(true, \"hi mom\")\n");
		}
	}
}

