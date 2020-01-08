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

	}
}
