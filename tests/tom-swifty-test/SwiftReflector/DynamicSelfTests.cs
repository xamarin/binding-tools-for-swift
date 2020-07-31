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

		[Test]
		public void TestSelfArg ()
		{
			var swiftCode = @"
public protocol Identity2 {
	func whoAmI (s: Self)
}

public func whoWho<T> (a: T) where T : Identity2 {
    a.whoAmI (s: a)
}
";

			// public class Foo1 : Identity2<Foo1>
			// {
			//     public Foo1 () { }
			//     public void WhoAmI (Foo1 who) {
			//         Console.WriteLine ("Got here.");
			//     }
			// }

			var auxClass = new CSClass (CSVisibility.Public, "Foo1");
			var ifaceType = new CSSimpleType ("IIdentity2", false, new CSSimpleType ("Foo1"));
			auxClass.Inheritance.Add (new CSIdentifier (ifaceType.ToString ()));

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, auxClass.Name,
				new CSParameterList (), new CSCodeBlock ());

			var paramList = new CSParameterList ();
			paramList.Add (new CSParameter (new CSSimpleType ("Foo1"), new CSIdentifier ("self")));

			var whoAmI = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void,
				new CSIdentifier ("WhoAmI"), paramList, CSCodeBlock.Create (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Got here."))));

			auxClass.Constructors.Add (ctor);
			auxClass.Methods.Add (whoAmI);

			var instName = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instName, new CSFunctionCall (auxClass.Name, new Dynamo.CommaListElementCollection<CSBaseExpression> (), true));
			var methodCall = CSFunctionCall.FunctionCallLine ("TopLevelEntities.WhoWho", false, instName);
			var callingCode = CSCodeBlock.Create (instDecl, methodCall);

			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here.\n", otherClass: auxClass, platform: PlatformName.macOS);
		}

		[Test]
		public void TestSelfPropGet ()
		{
			var swiftCode = @"
public protocol Identity3 {
	var whoAmI: Self { get }
}

public func whoProp<T> (a: T) where T: Identity3 {
	a.whoAmI
}
";

			// public class Foo2 : IIdentity3<Foo2>
			// {
			//	public Foo2 () { }
			//	public Foo2 WhoAmI {
			//		get {
			//			Console.WriteLine("Got here.");
			//			return this;
			//		}
			//	}
			// }
			var auxClass = new CSClass (CSVisibility.Public, "Foo2");
			var ifaceType = new CSSimpleType ("IIdentity3", false, new CSSimpleType ("Foo2"));
			auxClass.Inheritance.Add (new CSIdentifier (ifaceType.ToString ()));

			var ctor = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, auxClass.Name,
				new CSParameterList (), new CSCodeBlock ());

			var getBody = CSCodeBlock.Create (CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("Got here.")),
				CSReturn.ReturnLine (CSIdentifier.This));

			var whoAmI = new CSProperty (auxClass.ToCSType (), CSMethodKind.None, new CSIdentifier ("WhoAmI"), CSVisibility.Public, getBody, CSVisibility.Public, null);

			auxClass.Constructors.Add (ctor);
			auxClass.Properties.Add (whoAmI);

			var instName = new CSIdentifier ("inst");
			var instDecl = CSVariableDeclaration.VarLine (instName, new CSFunctionCall (auxClass.Name, new Dynamo.CommaListElementCollection<CSBaseExpression> (), true));
			var methodCall = CSFunctionCall.FunctionCallLine ("TopLevelEntities.WhoProp", false, instName);
			var callingCode = CSCodeBlock.Create (instDecl, methodCall);

			TestRunning.TestAndExecute (swiftCode, callingCode, "Got here.\n", otherClass: auxClass, platform: PlatformName.macOS);
		}

	}
}
