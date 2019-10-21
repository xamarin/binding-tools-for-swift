using System;
using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class HomonymTests {

		[Test]
		public void TopLevelFunctionHomonym ()
		{
			var swiftCode =
				"public func same(a:Int) -> Int { return a }\n" +
				"public func same(b:Int) -> Int { return b }\n" +
				"public func same(b:Int) -> Bool { return true }\n";

			var call = new CSFunctionCall ("TopLevelEntities.Same_a_nint", false, CSConstant.Val (14));
			var call1 = new CSFunctionCall ("TopLevelEntities.Same_b_bool", false, CSConstant.Val (14));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call);
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1);
			CSCodeBlock callingCode = CSCodeBlock.Create (printer, printer1);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\nTrue\n");
		}


		[Test]
		public void TestNonVirtualClassHomonym()
		{
			var swiftCode =
				"public class FooNVClass {\n" +
				"   public init() {}\n" +
				"   public func same(a:Int) -> Int { return a }\n" +
				"   public func same(b:Int) -> Int { return b + b }\n" +
				"   public func same(b:Int) -> Bool { return true }\n" +
				"}\n";
			var foo = new CSLine (new CSVariableDeclaration (new CSSimpleType ("FooNVClass"), "foo", new CSFunctionCall ("FooNVClass", true)));
			var call = new CSFunctionCall ("foo.Same_a_nint", false, CSConstant.Val (14));
			var call1 = new CSFunctionCall ("foo.Same_b_nint", false, CSConstant.Val (14));
			var call2 = new CSFunctionCall ("foo.Same_b_bool", false, CSConstant.Val (14));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call);
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1);
			CSLine printer2 = CSFunctionCall.ConsoleWriteLine (call2);
			CSCodeBlock callingCode = CSCodeBlock.Create (foo, printer, printer1, printer2);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\n28\nTrue\n");
		}

		[Test]
		public void TestStructHomonym ()
		{
			var swiftCode =
				"public struct FooStruct {\n" +
				"   public init() {}\n" +
				"   public func same(a:Int) -> Int { return a }\n" +
				"   public func same(b:Int) -> Int { return b + b }\n" +
				"   public func same(b:Int) -> Bool { return true }\n" +
				"}\n";
			var foo = new CSLine (new CSVariableDeclaration (new CSSimpleType ("FooStruct"), "foo", new CSFunctionCall ("FooStruct", true)));
			var call = new CSFunctionCall ("foo.Same_a_nint", false, CSConstant.Val (14));
			var call1 = new CSFunctionCall ("foo.Same_b_nint", false, CSConstant.Val (14));
			var call2 = new CSFunctionCall ("foo.Same_b_bool", false, CSConstant.Val (14));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call);
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1);
			CSLine printer2 = CSFunctionCall.ConsoleWriteLine (call2);
			CSCodeBlock callingCode = CSCodeBlock.Create (foo, printer, printer1, printer2);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\n28\nTrue\n");
		}

		[Test]
		public void TestConstructorClassHomonym ()
		{
			var swiftCode =
				"public class FooClassCtor {\n" +
				"   public var x:Int\n" +
				"   public init(a:Int) { x = a\n }\n" +
				"   public init(b:Int) { x = b + b\n }\n" +
				"}\n";
			var call = new CSFunctionCall ("FooClassCtor.FooClassCtor_a", false, CSConstant.Val (14));
			var call1 = new CSFunctionCall ("FooClassCtor.FooClassCtor_b", false, CSConstant.Val (14));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call.Dot(new CSIdentifier("X")));
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1.Dot (new CSIdentifier ("X")));
			CSCodeBlock callingCode = CSCodeBlock.Create (printer, printer1);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\n28\n");
		}

		[Test]
		public void TestGenericConstructorClassHomonym ()
		{
			var swiftCode =
				"public class FooGeneric<T> {\n" +
				"   public var x:Int\n" +
				"   public var t:T\n" +
				"   public init(a:Int, c:T) { x = a\n t = c\n}\n" +
				"   public init(b:Int, c:T) { x = b + b\n t = c\n}\n" +
				"}\n";
			var call = new CSFunctionCall ("FooGeneric<bool>.FooGeneric_a_c", false, CSConstant.Val (14), CSConstant.Val(true));
			var call1 = new CSFunctionCall ("FooGeneric<bool>.FooGeneric_b_c", false, CSConstant.Val (14), CSConstant.Val (true));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call.Dot (new CSIdentifier ("X")));
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1.Dot (new CSIdentifier ("X")));
			CSCodeBlock callingCode = CSCodeBlock.Create (printer, printer1);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\n28\n");
		}

		[Test]
		public void TestConstructorStructHomonym ()
		{
			var swiftCode =
				"public struct FooStructCtor {\n" +
				"   public var x:Int\n" +
				"   public init(a:Int) { x = a\n }\n" +
				"   public init(b:Int) { x = b + b\n }\n" +
				"}\n";
			var call = new CSFunctionCall ("FooStructCtor.FooStructCtor_a", false, CSConstant.Val (14));
			var call1 = new CSFunctionCall ("FooStructCtor.FooStructCtor_b", false, CSConstant.Val (14));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call.Dot (new CSIdentifier ("X")));
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1.Dot (new CSIdentifier ("X")));
			CSCodeBlock callingCode = CSCodeBlock.Create (printer, printer1);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\n28\n");
		}

		[Test]
		public void TestVirtualClassHomonym()
		{
			var swiftCode =
				"open class FooVClass {\n" +
				"   public init() {}\n" +
				"   open func same(a:Int) -> Int { return a }\n" +
				"   open func same(b:Int) -> Int { return b + b }\n" +
				"   open func same(b:Int) -> Bool { return true }\n" +
				"}\n";
			var foo = new CSLine (new CSVariableDeclaration (new CSSimpleType ("FooVClass"), "foo", new CSFunctionCall ("FooVClass", true)));
			var call = new CSFunctionCall ("foo.Same_a_nint", false, CSConstant.Val (14));
			var call1 = new CSFunctionCall ("foo.Same_b_nint", false, CSConstant.Val (14));
			var call2 = new CSFunctionCall ("foo.Same_b_bool", false, CSConstant.Val (14));
			CSLine printer = CSFunctionCall.ConsoleWriteLine (call);
			CSLine printer1 = CSFunctionCall.ConsoleWriteLine (call1);
			CSLine printer2 = CSFunctionCall.ConsoleWriteLine (call2);
			CSCodeBlock callingCode = CSCodeBlock.Create (foo, printer, printer1, printer2);
			TestRunning.TestAndExecute (swiftCode, callingCode, "14\n28\nTrue\n", platform: PlatformName.macOS);
		}

		[Test]
		public void TestOperatorHomonymSmokeTest ()
		{
			var swiftCode =
				"infix operator ∪\n" +
				"public func ∪<T: Equatable> (left: [T], right: [T]) -> [T] {\n" +
				"    var union: [T] = []\n" +
				"    for value in left + right {\n" +
				"        if !union.contains(value) {\n" +
				"            union.append(value)\n" +
				"        }\n" +
				"    }\n" +
				"    return union\n" +
				"}\n" +
				"public func ∪<T> (left: Set<T>, right: Set<T>) -> Set<T> {\n" +
				"    return left.union(right)\n" +
				"}\n";

			var printIt = CSFunctionCall.ConsoleWriteLine (CSConstant.Val ("success"));
			var callingCode = CSCodeBlock.Create (printIt);
			TestRunning.TestAndExecute (swiftCode, callingCode, "success\n");
		}
	}
}
