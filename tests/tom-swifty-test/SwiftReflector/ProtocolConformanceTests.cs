// Copyright (c) Microsoft Corporation.
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
			// var nomDesc = SwiftProtocolTypeAttribute.DescriptorForType (typeof (ISwiftIterator<>));
			// var confDesc = SwiftCore.ConformsToSwiftProtocol (StructMarshal.Marshaler.Metatypeof (typeof (SwiftIteratorProtocolProxy<nint>)),
			//                                          nomDesc);
			// Console.WriteLine (confDesc != IntPtr.Zero);

			var swiftCode = @"
public func canGetProtocolConfDesc () {
}
";
			var nomDescID = new CSIdentifier ("nomDesc");
			var confDescID = new CSIdentifier ("confDesc");

			var nomDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, nomDescID,
				new CSFunctionCall ("SwiftProtocolTypeAttribute.DescriptorForType", false, new CSSimpleType ("ISwiftIterator<>").Typeof ()));

			var metaTypeCall = new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, new CSSimpleType ("SwiftIteratorProtocolProxy<nint>").Typeof ());
			var confDescDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, confDescID,
				new CSFunctionCall ("SwiftCore.ConformsToSwiftProtocol", false, metaTypeCall, nomDescID));
			var printer = CSFunctionCall.ConsoleWriteLine (confDescID != new CSIdentifier ("IntPtr.Zero"));

			var callingCode = CSCodeBlock.Create (nomDecl, confDescDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}


		[Test]
		public void CanGetProtocolConformanceDescMarshal ()
		{
			// var confDesc = StructMarshal.Marshaler.ProtocolConformanceof (typeof (ISwiftIterator<>),
			//		typeof (SwiftIteratorProtocolProxy<nint>);
			// Console.WriteLine (confDesc != IntPtr.Zero);

			var swiftCode = @"
public func canGetProtocolConfDescMarshal () {
}
";
			var confDescID = new CSIdentifier ("confDesc");

			var concreteType = new CSSimpleType ("SwiftIteratorProtocolProxy<nint>").Typeof ();
			var ifaceType = new CSSimpleType ("ISwiftIterator<>").Typeof ();
			var confDescDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, confDescID,
				new CSFunctionCall ("StructMarshal.Marshaler.ProtocolConformanceof", false, ifaceType, concreteType));
			var printer = CSFunctionCall.ConsoleWriteLine (confDescID != new CSIdentifier ("IntPtr.Zero"));

			var callingCode = CSCodeBlock.Create (confDescDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

		[Test]
		public void CanIterateAdapting ()
		{
			var swiftCode = @"
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
			//var iter = new SwiftListIterator<nint> (list);
			//var adapt = new SwiftIteratorProtocolProxy<nint> (iter);
			//var s = TopLevelEntities.IterateThings (adapt);
			//Console.WriteLine (s.ToString)

			var listID = new CSIdentifier ("list");
			var iterID = new CSIdentifier ("iter");
			var adaptID = new CSIdentifier ("adapt");
			var sID = new CSIdentifier ("s");
			var listDecl = CSVariableDeclaration.VarLine (listID, new CSListInitialized (new CSSimpleType ("nint"),
				CSConstant.Val (13), CSConstant.Val (-4), CSConstant.Val (2)));
			var iterDecl = CSVariableDeclaration.VarLine (iterID, new CSFunctionCall ("SwiftListIterator<nint>", true, listID));
			var adaptDecl = CSVariableDeclaration.VarLine (adaptID, new CSFunctionCall ("SwiftIteratorProtocolProxy<nint>", true, iterID));
			var sDecl = CSVariableDeclaration.VarLine (sID, new CSFunctionCall ("TopLevelEntities.IterateThings", false, adaptID));
			var printer = CSFunctionCall.ConsoleWriteLine (sID);

			var callingCode = CSCodeBlock.Create (listDecl, iterDecl, sDecl, printer);
		}
	}
}
