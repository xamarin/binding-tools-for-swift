﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	public class ProtocolConformanceTests {
		[Test]
		public void CanGetProtocolConformanceDesc ()
		{
			// var nomDesc = SwiftProtocolTypeAttribute.DescriptorForType (typeof (IIteratorProtocol<>));
			// var witTable = SwiftCore.ConformsToSwiftProtocol (StructMarshal.Marshaler.Metatypeof (typeof (SwiftIteratorProtocolProxy<nint>)),
			//                                          nomDesc);
			// Console.WriteLine (confDesc != IntPtr.Zero);

			var swiftCode = @"
public func canGetProtocolConfDesc () {
}
";
			var nomDescID = new CSIdentifier ("nomDesc");
			var witTableID = new CSIdentifier ("witTable");

			var nomDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, nomDescID,
				new CSFunctionCall ("SwiftProtocolTypeAttribute.DescriptorForType", false, new CSSimpleType ("IIteratorProtocol<>").Typeof ()));

			var metaTypeCall = new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, new CSSimpleType ("SwiftIteratorProtocolProxy<nint>").Typeof ());
			var confDescDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, witTableID,
				new CSFunctionCall ("SwiftCore.ConformsToSwiftProtocol", false, metaTypeCall, nomDescID));
			var printer = CSFunctionCall.ConsoleWriteLine (witTableID.Dot (new CSIdentifier ("Handle")) != new CSIdentifier ("IntPtr.Zero"));

			var callingCode = CSCodeBlock.Create (nomDecl, confDescDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void CanGetProtocolConformanceDescMarshal ()
		{
			// var confDesc = StructMarshal.Marshaler.ProtocolConformanceof (typeof (IIteratorProtocol<>),
			//		typeof (SwiftIteratorProtocolProxy<nint>);
			// Console.WriteLine (confDesc != IntPtr.Zero);

			var swiftCode = @"
public func canGetProtocolConfDescMarshal () {
}
";
			var confDescID = new CSIdentifier ("confDesc");

			var concreteType = new CSSimpleType ("SwiftIteratorProtocolProxy<nint>").Typeof ();
			var ifaceType = new CSSimpleType ("IIteratorProtocol<>").Typeof ();
			var confDescDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, confDescID,
				new CSFunctionCall ("StructMarshal.Marshaler.ProtocolConformanceof", false, ifaceType, concreteType));
			var printer = CSFunctionCall.ConsoleWriteLine (confDescID.Dot (new CSIdentifier ("Handle")) != new CSIdentifier ("IntPtr.Zero"));

			var callingCode = CSCodeBlock.Create (confDescDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

		[Test]
		[Ignore ("Needs a type database entry, probably")]
		public void CanIterateAdapting ()
		{
			var swiftCode = @"
import XamGlue

public func iterateThings (this: iteratorprotocol_xam_helper<Int>) -> String
{
	var s = """";
	while let i = this.next () {
	    let isFirst = s == """"
	    if !isFirst {
	    	s = s + "" ""
	    }
		s = s + String (i)
	}
	return s
}
";
			//var list = new List<nint> () { 13, -4, 2 };
			//var iter = new EnumerableIterator<nint> (list);
			//var adapt = new SwiftIteratorProtocolProxy<nint> (iter);
			//var s = TopLevelEntities.IterateThings (adapt);
			//Console.WriteLine (s.ToString)

			var listID = new CSIdentifier ("list");
			var iterID = new CSIdentifier ("iter");
			var adaptID = new CSIdentifier ("adapt");
			var sID = new CSIdentifier ("s");
			var listDecl = CSVariableDeclaration.VarLine (listID, new CSListInitialized (new CSSimpleType ("nint"),
				CSConstant.Val (13), CSConstant.Val (-4), CSConstant.Val (2)));
			var iterDecl = CSVariableDeclaration.VarLine (iterID, new CSFunctionCall ("EnumerableIterator<nint>", true, listID));
			var adaptDecl = CSVariableDeclaration.VarLine (adaptID, new CSFunctionCall ("SwiftIteratorProtocolProxy<nint>", true, iterID));
			var sDecl = CSVariableDeclaration.VarLine (sID, new CSFunctionCall ("TopLevelEntities.IterateThings", false, adaptID));
			var printer = CSFunctionCall.ConsoleWriteLine (sID);

			var callingCode = CSCodeBlock.Create (listDecl, iterDecl, sDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "13 -4 2\n");
		}


		[TestCase ("Bool", "Boolean", "[true, false, true]")]
		[TestCase ("Int", "nint", "[0, 1, 2]")]
		[TestCase ("UInt", "nuint", "[0, 1, 2]")]
		[TestCase ("Int32", "Int32", "[0, 1, 2]")]
		[TestCase ("UInt32", "UInt32", "[0, 1, 2]")]
		[TestCase ("Float", "Single", "[0.1, 1.1, 2.1]")]
		[TestCase ("Int?", "SwiftOptional`1", "[0, 1,  nil]", "OptInt")]
		[TestCase ("String", "SwiftString", "[\"a\", \"b\",  \"c\"]")]
		public void CanGetAssocTypesParameterized (string swiftType, string csType, string initData, string nameSuffix = null)
		{
			nameSuffix = nameSuffix ?? swiftType;
			var swiftCode = $@"
private class Foo{nameSuffix} : IteratorProtocol {{
	public init () {{
	}}
	private var data: [{swiftType}] = {initData}
	private var x = -1
	public func next () -> {swiftType}? {{
		if x < data.count {{
			x = x + 1
			return data[x]
		}}
		else {{
			return nil
		}}
	}}
}}
public func blindAssocFunc{nameSuffix} () -> Any.Type {{
	return Foo{nameSuffix}.self
}}
";
			// var any = TopLevelEntities.BlindAssocFunc ();
			// var types = StructMarshal.Marshaler.GetAssociatedTypes (any, typeof (IIteratorProtocol<>), 1);
			// Console.WriteLine (types[0].Name);

			var anyID = new CSIdentifier ("any");
			var anyDecl = CSVariableDeclaration.VarLine (anyID, new CSFunctionCall ($"TopLevelEntities.BlindAssocFunc{nameSuffix}", false));
			var assocTypesID = new CSIdentifier ("assoc");
			var typesID = new CSIdentifier ("types");
			var typesDecl = CSVariableDeclaration.VarLine (typesID, new CSFunctionCall ("StructMarshal.Marshaler.GetAssociatedTypes", false,
				anyID, new CSSimpleType ("IIteratorProtocol<>").Typeof (), CSConstant.Val (1)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIndexExpression (typesID, false, CSConstant.Val (0)).Dot (new CSIdentifier ("Name")));

			var callingCode = CSCodeBlock.Create (anyDecl, typesDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, $"{csType}\n", testName: $"CanGetAssocTypesParams{nameSuffix}");
		}


		[TestCase ("Bool", "Boolean", "[true, false, true]")]
		[TestCase ("Int", "nint", "[0, 1, 2]")]
		[TestCase ("UInt", "nuint", "[0, 1, 2]")]
		[TestCase ("Int32", "Int32", "[0, 1, 2]")]
		[TestCase ("UInt32", "UInt32", "[0, 1, 2]")]
		[TestCase ("Float", "Single", "[0.1, 1.1, 2.1]")]
		[TestCase ("Int?", "SwiftOptional`1", "[0, 1,  nil]", "OptInt")]
		[TestCase ("String", "SwiftString", "[\"a\", \"b\",  \"c\"]")]
		public void CanGetAssociatedTypeFromAny (string swiftType, string csType, string initData, string nameSuffix = null)
		{
			nameSuffix = nameSuffix ?? swiftType;
			var swiftCode = $@"
private class FooAny{nameSuffix} : IteratorProtocol {{
	public init () {{ }}
	private var data : [{swiftType}] = {initData}
	private var x = -1
	public func next () -> {swiftType}? {{
		if x < data.count {{
			x = x + 1
			return data[x]
		}}
		else {{
			return nil
		}}
	}}
}}
public func blindAssocFuncAny{nameSuffix} () -> Any {{
	return FooAny{nameSuffix} ()
}}
";
			// var any = TopLevelEntities.BlindAssocFuncAny ();
			// var types = StructMarshal.Marshaler.GetAssociateTypes (any.ObjectMetadata, typeof (IIteratorProtocol<>), 1);
			// Console.WriteLine (types[0].Name);

			var anyID = new CSIdentifier ("any");
			var anyDecl = CSVariableDeclaration.VarLine (anyID, new CSFunctionCall ($"TopLevelEntities.BlindAssocFuncAny{nameSuffix}", false));
			var assocTypesID = new CSIdentifier ("assoc");
			var typesID = new CSIdentifier ("tyypes");
			var typesDecl = CSVariableDeclaration.VarLine (typesID, new CSFunctionCall ("StructMarshal.Marshaler.GetAssociatedTypes", false,
				anyID.Dot (new CSIdentifier ("ObjectMetadata")), new CSSimpleType ("IIteratorProtocol<>").Typeof (), CSConstant.Val (1)));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIndexExpression (typesID, false, CSConstant.Val (0)).Dot (new CSIdentifier ("Name")));

			var callingCode = CSCodeBlock.Create (anyDecl, typesDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, $"{csType}\n", testName: $"CanGetAssociatedTypeFromAny{nameSuffix}");
		}


		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SmokeProtocolAssoc ()
		{
			var swiftCode = @"
public protocol Iterator0 {
	associatedtype Elem
	func next () -> Elem
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SmokeProtocolAssocGetProp ()
		{
			var swiftCode = @"
public protocol Iterator1 {
	associatedtype Elem
	var next:Elem { get }
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SmokeProtocolAssocGetSetProp ()
		{
			var swiftCode = @"
public protocol Iterator2 {
	associatedtype Elem
	var item: Elem { get set }
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SmokeProtocolAssocFuncArg ()
		{
			var swiftCode = @"
public protocol Iterator3 {
	associatedtype Elem
	func ident (a:Elem)
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SomeProtocolAssocSubscriptGet ()
		{
			var swiftCode = @"
public protocol Iterator4 {
	associatedtype Elem
	subscript (index: Int) -> Elem {
		get
	}
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SomeProtocolAssocSubscriptGetSet ()
		{
			var swiftCode = @"
public protocol Iterator5 {
	associatedtype Elem
	subscript (index: Int) -> Elem {
		get set
	}
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SomeProtocolAssocSubscriptGetSetParams ()
		{
			var swiftCode = @"
public protocol Iterator6 {
	associatedtype Elem
	subscript (index: Elem) -> Elem {
		get set
	}
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("OK"));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "OK\n");
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SimplestProtocolAssocTest ()
		{
			var swiftCode = @"
public protocol Simplest0 {
	associatedtype Item
	func printAndGetIt () -> Item
}
public func doPrintSimplest0<T>(a:T) where T:Simplest0 {
	let _ = a.printAndGetIt ()
}
";
			var altClass = new CSClass (CSVisibility.Public, "Simple0Impl");
			altClass.Inheritance.Add (new CSIdentifier ("ISimplest0<SwiftString>"));
			var strID = new CSIdentifier ("theStr");
			var strDecl = CSVariableDeclaration.VarLine (strID, CSConstant.Val ("Got here!"));
			var printPart = CSFunctionCall.ConsoleWriteLine (strID);
			var returnPart = CSReturn.ReturnLine (new CSFunctionCall ("SwiftString.FromString", false, strID));
			var printBody = CSCodeBlock.Create (strDecl, printPart, returnPart);
			var speak = new CSMethod (CSVisibility.Public, CSMethodKind.None, new CSSimpleType ("SwiftString"), new CSIdentifier ("PrintAndGetIt"), new CSParameterList (), printBody);
			altClass.Methods.Add (speak);

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), CSCodeBlock.Create ());
			altClass.Methods.Add (ctor);


			var instID = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instID, new CSFunctionCall ("Simple0Impl", true));
			var doPrint = CSFunctionCall.FunctionCallLine ("TopLevelEntities.DoPrintSimplest0<Simple0Impl, SwiftString>", false, instID);
			var callingCode = CSCodeBlock.Create (instDecl, doPrint);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here!\n", otherClass: altClass);
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SimplestProtocolPropGetAssocTest ()
		{
			var swiftCode = @"
public protocol Simplest1 {
	associatedtype Item
	var printThing: Item { get }
}
public func doPrintSimplest1<T>(a:T) where T:Simplest1 {
	let _ = a.printThing
}
";
			var altClass = new CSClass (CSVisibility.Public, "Simple1Impl");
			altClass.Inheritance.Add (new CSIdentifier ("ISimplest1<SwiftString>"));
			var strID = new CSIdentifier ("theStr");
			var strDecl = CSVariableDeclaration.VarLine (strID, CSConstant.Val ("Got here!"));
			var printPart = CSFunctionCall.ConsoleWriteLine (strID);
			var returnPart = CSReturn.ReturnLine (new CSFunctionCall ("SwiftString.FromString", false, strID));
			var printBody = CSCodeBlock.Create (strDecl, printPart, returnPart);
			var speak = new CSProperty (new CSSimpleType ("SwiftString"), CSMethodKind.None, new CSIdentifier ("PrintThing"),
				CSVisibility.Public, printBody, CSVisibility.Public, null);
			altClass.Properties.Add (speak);

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), CSCodeBlock.Create ());
			altClass.Methods.Add (ctor);

			var instID = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instID, new CSFunctionCall ("Simple1Impl", true));
			var doPrint = CSFunctionCall.FunctionCallLine ("TopLevelEntities.DoPrintSimplest1<Simple1Impl, SwiftString>", false, instID);
			var callingCode = CSCodeBlock.Create (instDecl, doPrint);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here!\n", otherClass: altClass);
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SimpleProtocolPropGetSetAssocTest ()
		{
			var swiftCode = @"
public protocol Simplest2 {
	associatedtype Item
	var thing: Item { get set }
}
public func doSetProp<T, U> (a: inout T, b:U) where T:Simplest2, U==T.Item {
	a.thing = b
}
";
			var altClass = new CSClass (CSVisibility.Public, "Simple2Impl");
			altClass.Inheritance.Add (new CSIdentifier ("ISimplest2<SwiftString>"));
			var thingProp = CSProperty.PublicGetSet (new CSSimpleType ("SwiftString"), "Thing");
			altClass.Properties.Add (thingProp);

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), CSCodeBlock.Create ());
			altClass.Methods.Add (ctor);

			var instID = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instID, new CSFunctionCall ("Simple2Impl", true));
			var doSetProp = CSFunctionCall.FunctionCallLine ("TopLevelEntities.DoSetProp<Simple2Impl, SwiftString>", false, instID,
				new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("Got here!")));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{instID.Name}.Thing"));
			var callingCode = CSCodeBlock.Create (instDecl, doSetProp, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here!\n", otherClass: altClass);
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SimpleProtocolProGetSetAssocTestAltSyntax ()
		{
			var swiftCode = @"
public protocol Simplest3 {
	associatedtype Item
	var thing: Item { get set }
}
public func doSetProp<T> (a: inout T, b:T.Item) where T:Simplest3 {
	a.thing = b
}
";
			var altClass = new CSClass (CSVisibility.Public, "Simple3Impl");
			altClass.Inheritance.Add (new CSIdentifier ("ISimplest3<SwiftString>"));
			var thingProp = CSProperty.PublicGetSet (new CSSimpleType ("SwiftString"), "Thing");
			altClass.Properties.Add (thingProp);

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), CSCodeBlock.Create ());
			altClass.Methods.Add (ctor);

			var instID = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instID, new CSFunctionCall ("Simple3Impl", true));
			var doSetProp = CSFunctionCall.FunctionCallLine ("TopLevelEntities.DoSetProp<Simple3Impl, SwiftString>", false, instID,
				new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("Got here!")));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{instID.Name}.Thing"));
			var callingCode = CSCodeBlock.Create (instDecl, doSetProp, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here!\n", otherClass: altClass);
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SimpleProtocolProGetIndexer ()
		{
			var swiftCode = @"
public protocol Simplest4 {
	associatedtype Item
	subscript (index: Int) -> Item {
		get
	}
}
public func doGetIt<T:Simplest4> (a: T, i: Int) -> T.Item {
	return a[i]
}
";
			var altClass = new CSClass (CSVisibility.Public, "Simple4Impl");
			altClass.Inheritance.Add (new CSIdentifier ("ISimplest4<SwiftString>"));

			var getBlock = CSCodeBlock.Create (CSReturn.ReturnLine (new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("Got here!"))));
			var parameters = new CSParameterList (new CSParameter (new CSSimpleType ("nint"), new CSIdentifier ("index")));
			var thingIndex = new CSProperty (new CSSimpleType ("SwiftString"), CSMethodKind.None,
				CSVisibility.Public, getBlock, CSVisibility.Public, null, parameters);
			altClass.Properties.Add (thingIndex);

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), CSCodeBlock.Create ());
			altClass.Methods.Add (ctor);
			var instID = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instID, new CSFunctionCall ("Simple4Impl", true));
			var resultID = new CSIdentifier ("result");
			var resultDecl = CSVariableDeclaration.VarLine (resultID, new CSFunctionCall ("TopLevelEntities.DoGetIt<Simple4Impl, SwiftString>", false, instID, CSConstant.Val (3)));
			var printer = CSFunctionCall.ConsoleWriteLine (resultID);
			var callingCode = CSCodeBlock.Create (instDecl, resultDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here!\n", otherClass: altClass);
		}

		[Test]
		[Ignore ("vtable should be fileprivate")]
		public void SimpleProtocolProGetSetIndexer ()
		{
			var swiftCode = @"
public protocol Simplest5 {
	associatedtype Item
	subscript (index: Int) -> Item {
		get set
	}
}
public func doSetIt<T:Simplest5> (a: inout T, i: Int, v: T.Item) {
	a[i] = v
}
";
			var altClass = new CSClass (CSVisibility.Public, "Simple5Impl");
			altClass.Inheritance.Add (new CSIdentifier ("ISimplest5<SwiftString>"));

			var fieldName = new CSIdentifier ("v");
			altClass.Fields.Add (CSFieldDeclaration.FieldLine (new CSSimpleType ("SwiftString"), fieldName));
			var getBlock = CSCodeBlock.Create (CSReturn.ReturnLine (fieldName));
			var setBlock = CSCodeBlock.Create (CSAssignment.Assign (fieldName, new CSIdentifier ("value")));

			var parameters = new CSParameterList (new CSParameter (new CSSimpleType ("nint"), new CSIdentifier ("index")));
			var thingIndex = new CSProperty (new CSSimpleType ("SwiftString"), CSMethodKind.None,
				CSVisibility.Public, getBlock, CSVisibility.Public, setBlock, parameters);
			altClass.Properties.Add (thingIndex);

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, altClass.Name, new CSParameterList (), CSCodeBlock.Create ());
			altClass.Methods.Add (ctor);
			var instID = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instID, new CSFunctionCall ("Simple5Impl", true));
			var callSetter = CSFunctionCall.FunctionCallLine ("TopLevelEntities.DoSetIt", false, instID, CSConstant.Val (3), new CSFunctionCall ("SwiftString.FromString", false, CSConstant.Val ("Got here!")));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{instID.Name}[3]"));
			var callingCode = CSCodeBlock.Create (instDecl, callSetter, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here!\n", otherClass: altClass);
		}
	}
}
