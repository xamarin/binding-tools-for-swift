// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	public class SwiftTypeRegistryTests {

		[TestCase ("bool")]
		[TestCase ("byte")]
		[TestCase ("sbyte")]
		[TestCase ("short")]
		[TestCase ("ushort")]
		[TestCase ("int")]
		[TestCase ("uint")]
		[TestCase ("long")]
		[TestCase ("ulong")]
		[TestCase ("float")]
		[TestCase ("double")]
		[TestCase ("nint")]
		[TestCase ("nuint")]
		[TestCase ("SwiftString")]
		[TestCase ("OpaquePointer")]
		[TestCase ("UnsafeRawPointer")]
		[TestCase ("UnsafeRawBufferPointer")]
		public void HasBuiltInTypes(string csType)
		{
			var swiftCode = $"public func doesNothing{csType}() {{\n}}\n";

			// var ocsty = typeof (csType);
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue(mt, out csty);
			// Console.WriteLine (csty == ocsty);

			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType (csType).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var printer = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", testName: "HashBuiltInTypes" + csType);
		}

		[Test]
		public void HasAddedClass ()
		{
			var swiftCode = @"
public class RegistryAddedClass {
    public init () { }
}
";
			// var ocsty = typeof (RegistryAddedClass);
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue(mt, out csty);
			// Console.WriteLine (csty == ocsty);

			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("RegistryAddedClass").Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var printer = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

		[Test]
		public void HasTuple ()
		{
			var swiftCode = @"
public func tupleTestDoesNothing () {
}";
			// var ocsty = typeof (Tuple<bool, float>)
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue(mt, out csty);
			// Console.WriteLine (csty == ocsty);
			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("Tuple", false, CSSimpleType.Bool, CSSimpleType.Float).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var printer = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

		[Test]
		public void HasClosureAction ()
		{
			var swiftCode = @"
public func closureTestDoesNothing () {
}";
			// var ocsty = typeof (Action<bool>)
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue(mt, out csty);
			// Console.WriteLine (csty.Name);
			// var genargs = csty.GetGenericArguments());
			// Console.WriteLine (genargs.Length);
			// Console.WriteLine (genargs[1].Name);
			// Console.WriteLine (csty == ocsty);
			//
			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("Action", false, CSSimpleType.Bool).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var print1 = CSFunctionCall.ConsoleWriteLine (ocsTypeID.Dot (new CSIdentifier ("Name")));
			var genargsID = new CSIdentifier ("genargs");
			var genArgsDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, genargsID, new CSFunctionCall ($"{csTypeID.Name}.GetGenericArguments", false));
			var print2 = CSFunctionCall.ConsoleWriteLine (genargsID.Dot (new CSIdentifier ("Length")));
			var print3 = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{genargsID.Name} [0].Name"));
			var print4 = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, print1, genArgsDecl, print2, print3, print4);

			TestRunning.TestAndExecute (swiftCode, callingCode, "Action`1\n1\nBoolean\nTrue\n");
		}

		[Test]
		public void HasClosureFunc ()
		{
			var swiftCode = @"
public func closureFuncTestDoesNothing () {
}";
			// var ocsty = typeof (Func<bool, int>)
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue(mt, out csty);
			// Console.WriteLine (csty.Name);
			// var genargs = csty.GetGenericArguments());
			// Console.WriteLine (genargs.Length);
			// Console.WriteLine (genargs[1].Name);
			// Console.WriteLine (csty == ocsty);
			//
			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("Func", false, CSSimpleType.Bool, CSSimpleType.Int).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var print1 = CSFunctionCall.ConsoleWriteLine (ocsTypeID.Dot (new CSIdentifier ("Name")));
			var genargsID = new CSIdentifier ("genargs");
			var genArgsDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, genargsID, new CSFunctionCall ($"{csTypeID.Name}.GetGenericArguments", false));
			var print2 = CSFunctionCall.ConsoleWriteLine (genargsID.Dot (new CSIdentifier ("Length")));
			var print3 = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{genargsID.Name} [0].Name"));
			var print4 = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, print1, genArgsDecl, print2, print3, print4);

			TestRunning.TestAndExecute (swiftCode, callingCode, "Func`2\n2\nBoolean\nTrue\n");
		}

		[Test]
		public void TestOptional ()
		{
			var swiftCode = @"
public func optionalTestDoesNothing () {
}";

			// var ocsty = typeof (SwiftOptional<bool>);
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue (mt, out csty);
			// Console.WriteLine (csty.Name);
			// Console.WriteLine (genargs.Length);
			// Console.WriteLine (genargs[1].Name);
			// Console.WriteLine (csty == ocsty);
			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("SwiftOptional", false, CSSimpleType.Bool).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var print1 = CSFunctionCall.ConsoleWriteLine (ocsTypeID.Dot (new CSIdentifier ("Name")));
			var genargsID = new CSIdentifier ("genargs");
			var genArgsDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, genargsID, new CSFunctionCall ($"{csTypeID.Name}.GetGenericArguments", false));
			var print2 = CSFunctionCall.ConsoleWriteLine (genargsID.Dot (new CSIdentifier ("Length")));
			var print3 = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{genargsID.Name} [0].Name"));
			var print4 = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, print1, genArgsDecl, print2, print3, print4);

			TestRunning.TestAndExecute (swiftCode, callingCode, "SwiftOptional`1\n1\nBoolean\nTrue\n");
		}

		[Test]
		public void TestDictionary ()
		{
			var swiftCode = @"
public func dictionaryTestDoesNothing () {
}";

			// var ocsty = typeof (SwiftOptional<bool>);
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue (mt, out csty);
			// Console.WriteLine (csty.Name);
			// Console.WriteLine (genargs.Length);
			// Console.WriteLine (genargs[1].Name);
			// Console.WriteLine (csty == ocsty);
			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("SwiftDictionary", false, CSSimpleType.Bool, CSSimpleType.Int).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var print1 = CSFunctionCall.ConsoleWriteLine (ocsTypeID.Dot (new CSIdentifier ("Name")));
			var genargsID = new CSIdentifier ("genargs");
			var genArgsDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, genargsID, new CSFunctionCall ($"{csTypeID.Name}.GetGenericArguments", false));
			var print2 = CSFunctionCall.ConsoleWriteLine (genargsID.Dot (new CSIdentifier ("Length")));
			var print3 = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{genargsID.Name} [0].Name"));
			var print4 = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, print1, genArgsDecl, print2, print3, print4);

			TestRunning.TestAndExecute (swiftCode, callingCode, "SwiftDictionary`2\n2\nBoolean\nTrue\n");
		}

		[Test]
		public void TestGenericClass ()
		{
			var swiftCode = @"
public class GenClass<T> {
	public var x: T
	public init (a:T) {
		x = a
	}
}
";
			// var ocsty = typeof (GenClass<bool>);
			// var mt = StructMarshal.Marshaler.Metatypeof (ocsty);
			// Type csty;
			// SwiftTypeRegistry.Registry.TryGetValue (mt, out csty);
			// Console.WriteLine (csty.Name);
			// Console.WriteLine (genargs.Length);
			// Console.WriteLine (genargs[1].Name);
			// Console.WriteLine (csty == ocsty);
			var ocsTypeID = new CSIdentifier ("ocsty");
			var csTypeID = new CSIdentifier ("csty");
			var mtID = new CSIdentifier ("mt");

			var ocstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ocsTypeID, new CSSimpleType ("GenClass", false, CSSimpleType.Bool).Typeof ());
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID,
				new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, ocsTypeID));
			var cstyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Type, csTypeID);
			var tryGetLine = CSFunctionCall.FunctionCallLine ("SwiftTypeRegistry.Registry.TryGetValue", false,
				mtID, new CSUnaryExpression (CSUnaryOperator.Out, csTypeID));
			var print1 = CSFunctionCall.ConsoleWriteLine (ocsTypeID.Dot (new CSIdentifier ("Name")));
			var genargsID = new CSIdentifier ("genargs");
			var genArgsDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, genargsID, new CSFunctionCall ($"{csTypeID.Name}.GetGenericArguments", false));
			var print2 = CSFunctionCall.ConsoleWriteLine (genargsID.Dot (new CSIdentifier ("Length")));
			var print3 = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{genargsID.Name} [0].Name"));
			var print4 = CSFunctionCall.ConsoleWriteLine (csTypeID == ocsTypeID);
			var callingCode = CSCodeBlock.Create (ocstyDecl, mtDecl, cstyDecl, tryGetLine, print1, genArgsDecl, print2, print3, print4);

			TestRunning.TestAndExecute (swiftCode, callingCode, "GenClass`1\n1\nBoolean\nTrue\n");
		}
	}
}
