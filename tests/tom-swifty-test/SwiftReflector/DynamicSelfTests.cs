// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftReflector;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class DynamicSelfTests {
		[Test]
		public void SmokeTestSimplest ()
		{
			var swiftCode = @"
public protocol Identity0 {
	func whoAmI () -> Self
}
";
			var printer = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Got here."));
			var callingCode = CSCodeBlock.Create (printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here.\n", platform: PlatformName.macOS);

		}

		[Test]
		public void TestSimplest ()
		{
			var swiftCode = @"
public protocol Identity1 {
	func whoAmI () -> Self
}

public func getName (a: Identity1) -> String {
	let o = a.whoAmI
	let t = type(of: o)
	return String(describing: t)
}
";

			// public class Foo : Identity1<Foo>
			// {
			//    public Foo () { }
			//    public Foo WhoAmI () {
			//        return this;
			//    }
			// }

			var auxClass = new CSClass (CSVisibility.Public, "Foo");
			auxClass.Inheritance.Add (new CSIdentifier ("IIdentity1<Foo>"));

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, auxClass.Name,
				new CSParameterList (), new CSCodeBlock ());
			var whoAmI = new CSMethod (CSVisibility.Public, CSMethodKind.None, new CSSimpleType (auxClass.Name.Name),
				new CSIdentifier ("WhoAmI"), new CSParameterList (),
				CSCodeBlock.Create (CSReturn.ReturnLine (CSIdentifier.This)));

			auxClass.Constructors.Add (ctor);
			auxClass.Methods.Add (whoAmI);

			var instName = new CSIdentifier ("inst");
			var nameName = new CSIdentifier ("name");
			var instDecl = CSVariableDeclaration.VarLine (instName, new CSFunctionCall (auxClass.Name, new Dynamo.CommaListElementCollection<CSBaseExpression> (), true));
			var nameDecl = CSVariableDeclaration.VarLine (nameName, new CSFunctionCall ("TopLevelEntities.GetName", false, instName));
			var printer = CSFunctionCall.ConsoleWriteLine (nameName);
			var callingCode = CSCodeBlock.Create (instDecl, nameDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "() -> Identity1\n", otherClass: auxClass, platform: PlatformName.macOS);
		}
	}
}
