using System;
using SwiftReflector.IOUtils;
using NUnit.Framework;
using System.IO;
using tomwiftytest;
using System.Xml.Linq;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;
using System.Linq;
using SwiftReflector;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class DynamicXmlTests {

		void CompileStringToModule (string code, string moduleName)
		{
			Utils.CompileSwift (code, moduleName: moduleName);
		}

		Stream ReflectToXml (string code, string moduleName)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: moduleName);
			return compiler.ReflectToStream (null, null, null, moduleName);
		}

		XDocument ReflectToXDocument (string code, string moduleName)
		{
			return XDocument.Load (ReflectToXml (code, moduleName));
		}

		List<ModuleDeclaration> ReflectToModules (string code, string moduleName)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: moduleName);
			return compiler.ReflectToModules (null, null, null, moduleName);
		}

		// these two tests are smoke tests for module compilation
		// and XML reflection. Both use the custom swift compiler and both
		// exist as a canary in the coal mine. If these don't run, then nothing else will.
		[Test]
		public void HelloModuleTest ()
		{
			CompileStringToModule (Compiler.kHelloSwift, "MyModule");
		}

		[Test]
		public void HelloXmlTest ()
		{
			ReflectToXml (Compiler.kHelloSwift, "MyModule");
		}


		void TestFuncReturning (string declaredType, string value, string expectedType)
		{
			string code = String.Format ("public func foo() -> {0} {{ return {1} }}", declaredType, value);
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.Functions.Count ());
			FunctionDeclaration func = module.Functions.First ();
			Assert.IsNotNull (func);
			Assert.AreEqual (func.Name, "foo");
			Assert.AreEqual (expectedType, func.ReturnTypeName);
			Assert.AreEqual (1, func.ParameterLists.Count);
			Assert.AreEqual (0, func.ParameterLists [0].Count);
			NamedTypeSpec ns = func.ReturnTypeSpec as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual (expectedType, ns.Name);
		}

		[Test]
		public void TestFuncReturningBool ()
		{
			TestFuncReturning ("Bool", "true", "Swift.Bool");
		}
		[Test]
		public void TestFuncReturningInt ()
		{
			TestFuncReturning ("Int", "42", "Swift.Int");
		}
		[Test]
		public void TestFuncReturningUInt ()
		{
			TestFuncReturning ("UInt", "43", "Swift.UInt");
		}
		[Test]
		public void TestFuncReturningFloat ()
		{
			TestFuncReturning ("Float", "2.0", "Swift.Float");
		}
		[Test]
		public void TestFuncReturningDouble ()
		{
			TestFuncReturning ("Double", "3.0", "Swift.Double");
		}
		[Test]
		public void TestFuncReturningString ()
		{
			TestFuncReturning ("String", "\"nothing\"", "Swift.String");
		}

		[Test]
		public void TestEmptyClass ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.AreEqual (1, module.Classes.Count ());
			Assert.AreEqual (0, module.Functions.Count ());
			Assert.AreEqual (0, module.Structs.Count ());
			Assert.AreEqual ("Foo", module.Classes.First ().Name);
		}

		[Test]
		public void TestEmptyStruct ()
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.AreEqual (0, module.Classes.Count ());
			Assert.AreEqual (0, module.Functions.Count ());
			Assert.AreEqual (1, module.Structs.Count ());
			Assert.AreEqual ("Foo", module.Structs.First ().Name);
		}

		[Test]
		public void TestStructLayout ()
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { public var X:Int;\n public var Y:Bool; public var Z: Float; }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			StructDeclaration theStruct = module.Structs.FirstOrDefault (s => s.Name == "Foo");
			Assert.NotNull (theStruct);
			List<PropertyDeclaration> props = theStruct.Members.OfType<PropertyDeclaration> ().ToList ();
			Assert.AreEqual (3, props.Count);
			Assert.AreEqual ("X", props [0].Name);
			Assert.AreEqual ("Y", props [1].Name);
			Assert.AreEqual ("Z", props [2].Name);
			Assert.AreEqual ("Swift.Int", props [0].TypeName);
			Assert.AreEqual ("Swift.Bool", props [1].TypeName);
			Assert.AreEqual ("Swift.Float", props [2].TypeName);
		}

		[Test]
		public void TestClassWithConstructor ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.NotNull (theClass);
			FunctionDeclaration cons = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".ctor");
			Assert.NotNull (cons);
			Assert.AreEqual (2, cons.ParameterLists.Count);
			Assert.AreEqual (1, cons.ParameterLists [1].Count);
			Assert.AreEqual ("Swift.Int", cons.ParameterLists [1] [0].TypeName);
			Assert.AreEqual ("y", cons.ParameterLists [1] [0].PublicName);
		}

		[Test]
		public void TestClassHasDestructor ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.NotNull (theClass);
			FunctionDeclaration dtor = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".dtor");
			Assert.NotNull (dtor);
		}

		[Test]
		public void FuncReturningTuple ()
		{
			ModuleDeclaration module = ReflectToModules ("public func returnTuple()->(Int,Float) { return (0, 3.0); }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnTuple");
			Assert.AreEqual ("(Swift.Int, Swift.Float)", func.ReturnTypeName);
		}

		[Test]
		public void FuncReturningDictionary ()
		{
			ModuleDeclaration module = ReflectToModules ("public func returnDict()->[Int:Float] { return [Int:Float](); }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnDict");
			Assert.AreEqual ("Swift.Dictionary<Swift.Int, Swift.Float>", func.ReturnTypeName);
		}


		[Test]
		public void FuncReturningIntThrows ()
		{
			ModuleDeclaration module = ReflectToModules ("public enum MathError : Error {\ncase divZero\n}\n" +
								    "public func returnInt(a:Int) throws ->Int { if a < 1\n{\n throw MathError.divZero\n }\n else {\n return a\n}\n}", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnInt");
			Assert.AreEqual ("Swift.Int", func.ReturnTypeName);
			Assert.AreEqual (true, func.HasThrows);
		}



		[Test]
		public void FuncReturningIntOption ()
		{
			ModuleDeclaration module = ReflectToModules ("public func returnIntOpt()->Int? { return 3; }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnIntOpt");
			Assert.AreEqual ("Swift.Optional<Swift.Int>", func.ReturnTypeName);
		}

		[Test]
		public void GlobalBool ()
		{
			ModuleDeclaration module = ReflectToModules ("public var aGlobal:Bool = true", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module);
			PropertyDeclaration decl = module.TopLevelProperties.FirstOrDefault (f => f.Name == "aGlobal");
			Assert.IsNotNull (decl);
		}

		[Test]
		public void EnumSmokeTest1 ()
		{
			string code = "public enum foo { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			Assert.IsTrue (edecl.IsTrivial);
			Assert.IsTrue (edecl.IsHomogenous);
			Assert.IsFalse (edecl.HasRawType);
		}

		[Test]
		public void EnumSmokeTest2 ()
		{
			string code = "public enum foo { case a(Int), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			foreach (EnumElement elem in edecl.Elements) {
				Assert.IsTrue (elem.HasType);
			}
			Assert.IsFalse (edecl.IsTrivial);
			Assert.IsTrue (edecl.IsIntegral);
			Assert.IsTrue (edecl.IsHomogenous);
			Assert.IsFalse (edecl.HasRawType);
		}

		[Test]
		public void EnumSmokeTest3 ()
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			foreach (EnumElement elem in edecl.Elements) {
				Assert.IsTrue (elem.HasType);
			}
			Assert.IsFalse (edecl.IsTrivial);
			Assert.IsTrue (edecl.IsIntegral);
			Assert.IsFalse (edecl.IsHomogenous);
			Assert.IsFalse (edecl.HasRawType);
		}

		[Test]
		public void EnumSmokeTest4 ()
		{
			string code = "public enum foo { case a(Int), b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			Assert.IsFalse (edecl.IsTrivial);
			Assert.IsTrue (edecl.IsIntegral);
			Assert.IsFalse (edecl.IsHomogenous);
			Assert.IsFalse (edecl.HasRawType);
		}




		[Test]
		public void EnumSmokeTest5 ()
		{
			string code = "public enum foo:Int { case a=1, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			Assert.IsFalse (edecl.IsTrivial);
			Assert.IsTrue (edecl.IsIntegral);
			Assert.IsTrue (edecl.IsHomogenous);
			Assert.IsTrue (edecl.HasRawType);
			Assert.AreEqual ("Swift.Int", edecl.RawTypeName);
		}

		[Test]
		public void EnumSmokeTest6 ()
		{
			string code = "public enum foo:Int { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			Assert.IsFalse (edecl.IsTrivial);
			Assert.IsTrue (edecl.IsIntegral);
			Assert.IsTrue (edecl.IsHomogenous);
			Assert.IsTrue (edecl.HasRawType);
			Assert.AreEqual ("Swift.Int", edecl.RawTypeName);
		}

		[Test]
		public void EnumSmokeTest7 ()
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Bool), d(Float) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.AllEnums.Count);
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo");
			Assert.AreEqual (4, edecl.Elements.Count);
			Assert.IsFalse (edecl.IsTrivial);
			Assert.IsFalse (edecl.IsIntegral);
			Assert.IsFalse (edecl.IsHomogenous);
			Assert.IsFalse (edecl.HasRawType);

			Assert.AreEqual ("Swift.UInt", edecl ["a"].TypeName);
			Assert.AreEqual ("Swift.Int", edecl ["b"].TypeName);
			Assert.AreEqual ("Swift.Bool", edecl ["c"].TypeName);
			Assert.AreEqual ("Swift.Float", edecl ["d"].TypeName);
		}


		[Test]
		public void OptionalSmokeTest1 ()
		{
			string code = "public func optInt(x:Int) -> Int? { if (x >= 0) { return x; }\nreturn nil; }\n";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
		}

		[Test]
		public void TypeAliasTest()
		{
			string code = "public typealias Foo = OpaquePointer\n" +
				"public typealias Bar = Foo\n" +
				"public func aliased(a: Bar, b: (Bar)->()) {\n}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "aliased");
			Assert.IsNotNull (func);
			var named = func.ParameterLists [0] [0].TypeSpec as NamedTypeSpec;
			Assert.IsNotNull (named);
			Assert.AreEqual ("Swift.OpaquePointer", named.Name);
			var closType = func.ParameterLists [0] [1].TypeSpec as ClosureTypeSpec;
			Assert.IsNotNull (closType);
			named = closType.Arguments as NamedTypeSpec;
			Assert.IsNotNull (named);
			Assert.AreEqual ("Swift.OpaquePointer", named.Name);

		}

		[Test]
		public void DeprecatedFunction()
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.IsDeprecated, "deprecated");
			Assert.IsFalse (func.IsUnavailable, "unavailable");
		}


		[Test]
		public void DeprecatedClass ()
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl);
			Assert.IsTrue (cl.IsDeprecated);
			Assert.IsFalse (cl.IsUnavailable);
		}

		[Test]
		public void ObsoletedFunction ()
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "func");
			Assert.IsFalse (func.IsDeprecated, "deprecated");
			Assert.IsTrue (func.IsUnavailable, "unavilable");
		}


		[Test]
		public void ObsoletedClass ()
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl);
			Assert.IsFalse (cl.IsDeprecated, "deprecated");
			Assert.IsTrue (cl.IsUnavailable, "unavailable");
		}


		[Test]
		public void UnavailableFunction ()
		{
			string code =
				"@available(*, unavailable)" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func);
			Assert.IsFalse (func.IsDeprecated);
			Assert.IsTrue (func.IsUnavailable);
		}


		[Test]
		public void UnavailableClass ()
		{
			string code =
				"@available(*, unavailable)" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl);
			Assert.IsFalse (cl.IsDeprecated);
			Assert.IsTrue (cl.IsUnavailable);
		}


		[Test]
		public void MethodInStruct()
		{
			string code =

				"public struct CommandEvaluation {\n" +
				"	@available(*, deprecated, message: \"Please use parameter (at) instead\")\n" +
 				"       public func retrieveParameter (at index: Int) throws->String {\n" +
				"               return \"foo\"\n" +
				"       }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var st = module.Structs.FirstOrDefault (f => f.Name == "CommandEvaluation");
			Assert.IsNotNull (st);
			Assert.IsFalse (st.IsDeprecated);
			Assert.IsFalse (st.IsUnavailable);
			var func = st.AllMethodsNoCDTor ().Where (fn => fn.Name == "retrieveParameter").FirstOrDefault ();
			Assert.IsNotNull (func);
			Assert.IsTrue (func.IsDeprecated);
		}

		[Test]
		public void UnavailableProperty()
		{
			string code =
				"public struct JSON {\n" +
				"    @available(*, unavailable, renamed:\"null\")\n" +
    				"    public static var nullJSON: Int { return 3 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			var st = module.Structs.FirstOrDefault (f => f.Name == "JSON");
			var prop = st.AllProperties ().Where (fn => fn.Name == "nullJSON").FirstOrDefault ();
			Assert.IsNotNull (prop);
			Assert.IsTrue (prop.IsUnavailable);
		}


		[Test]
		public void ExtensionProperty ()
		{
			string code =
				"public extension Double {\n" +
				"    public var millisecond: Double  { return self / 1000 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			Assert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			Assert.AreEqual (2, ext.Members.Count, $"Expected 2 members but got {ext.Members.Count}");
		}

		[Test]
		public void ExtensionFunc ()
		{
			string code =
				"public extension Double {\n" +
				"    public func DoubleIt() -> Double  { return self * 2; }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			Assert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			Assert.AreEqual (1, ext.Members.Count, $"Expected 1 member but got {ext.Members.Count}");
			var func = ext.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (func, $"Expected a FunctionDeclaration but got {ext.Members[0].GetType ().Name}");
		}

		[Test]
		public void ExtensionProto ()
		{
			string code =
				"public protocol Printer {\n" +
				"    func printIt()\n" +
				"}\n" +
				"extension Double : Printer {\n" +
				"    public func printIt() { print(self) }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module);
			Assert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			Assert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			Assert.AreEqual (1, ext.Members.Count, $"Expected 1 member but got {ext.Members.Count}");
			var func = ext.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (func, $"Expected a FunctionDeclaration but got {ext.Members [0].GetType ().Name}");
			Assert.AreEqual (1, ext.Inheritance.Count, $"Expected 1 inheritance but had {ext.Inheritance.Count}");
			var inh = ext.Inheritance [0];
			Assert.AreEqual ("SomeModule.Printer", inh.InheritedTypeName, $"Incorrect type name {inh.InheritedTypeName}");
			Assert.AreEqual (InheritanceKind.Protocol, inh.InheritanceKind, $"Should always be protocol inheritance");
		}

		[Test]
		public void ObjCOptionalMember ()
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc optional func foo()\n" +
				"    func bar()\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var fooFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "foo").FirstOrDefault ();
			Assert.IsNotNull (fooFunc, "No func named foo");
			Assert.IsTrue (fooFunc.IsOptional, "should be optional");
			var barFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "bar").FirstOrDefault ();
			Assert.IsNotNull (barFunc, "No func named bar");
			Assert.IsFalse (barFunc.IsOptional, "should not be optional");
		}

		[Test]
		public void ObjCOptionalProp ()
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc optional var X:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto1", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (3, proto.Members.Count (), "incorrect number of members");
			var xProp = proto.Members.OfType<PropertyDeclaration> ().Where (f => f.Name == "X").FirstOrDefault ();
			Assert.IsNotNull (xProp, "No prop named X");
			Assert.IsTrue (xProp.IsOptional, "prop is not optional");
			var getter = xProp.GetGetter ();
			Assert.IsNotNull (getter, "Null getter");
			Assert.IsTrue (getter.IsOptional, "getter is not optional");
			var setter = xProp.GetSetter ();
			Assert.IsNotNull (setter, "Null setter");
			Assert.IsTrue (setter.IsOptional, "setter is not optional");
		}


		[Test]
		public void ObjCOptionalSubsript ()
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc optional subscript (index:Int) ->Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto1", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var func0 = proto.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (func0, "expected a function declaration at index 0");
			Assert.IsTrue (func0.IsOptional, "func 0 should be optional");

			var func1 = proto.Members [1] as FunctionDeclaration;
			Assert.IsNotNull (func1, "expected a function declaration at index 1");
			Assert.IsTrue (func1.IsOptional, "func 1 should be optional");
		}

		[Test]
		public void PropertyVisibility ()
		{
			PropertyVisibilityCore ("open", Accessibility.Open);
			PropertyVisibilityCore ("public", Accessibility.Public);
			PropertyVisibilityCore ("internal", Accessibility.Internal);
			PropertyVisibilityCore ("private", Accessibility.Private);
		}

		void PropertyVisibilityCore (string swiftVisibility, Accessibility accessibility)
		{
			string code = $@"open class Foo {{
			open {swiftVisibility} (set) weak var parent: Foo?
}}";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.AllClasses.Count (), "Expected a class.");
			var fooClass = module.AllClasses.First ();
			Assert.AreEqual (1, fooClass.AllProperties ().Count (), "Expected one property.");
			Assert.AreEqual (accessibility, fooClass.AllProperties () [0].GetSetter ().Access, "Unexpected Visibility.");
		}

		[Test]
		public void ObjCMemberSelector ()
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc func foo()\n" +
				"    @objc func bar(a:Int)\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var fooFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "foo").FirstOrDefault ();
			Assert.IsNotNull (fooFunc, "No func named foo");
			Assert.AreEqual ("foo", fooFunc.ObjCSelector, $"Incorrect foo selector name {fooFunc.ObjCSelector}");
			var barFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "bar").FirstOrDefault ();
			Assert.IsNotNull (barFunc, "No func named bar");
			Assert.AreEqual ("barWithA:", barFunc.ObjCSelector, $"Incorrect bar selector name {barFunc.ObjCSelector}");
		}


		[Test]
		public void ObjCPropSelector ()
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc var X:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (3, proto.Members.Count (), "incorrect number of members");
			var xProp = proto.Members.OfType<PropertyDeclaration> ().Where (f => f.Name == "X").FirstOrDefault ();
			Assert.IsNotNull (xProp, "No prop named X");
			var getter = xProp.GetGetter ();
			Assert.IsNotNull (getter, "Null getter");
			Assert.AreEqual ("X", getter.ObjCSelector, $"incorrect get X selector name {getter.ObjCSelector}");
			var setter = xProp.GetSetter ();
			Assert.IsNotNull (setter, "Null setter");
			Assert.AreEqual ("setX:", setter.ObjCSelector, $"incorrect set X selector name {setter.ObjCSelector}");
		}

		[Test]
		public void ObjCSubsriptSelector ()
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc subscript (index:Int) ->Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto1", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var func0 = proto.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (func0, "expected a function declaration at index 0");
			Assert.AreEqual ("objectAtIndexedSubscript:", func0.ObjCSelector, $"Incorrect selector for getter {func0.ObjCSelector}");

			var func1 = proto.Members [1] as FunctionDeclaration;
			Assert.IsNotNull (func1, "expected a function declaration at index 1");
			Assert.AreEqual ("setObject:atIndexedSubscript:", func1.ObjCSelector, $"Incorrect selector for setter {func1.ObjCSelector}");
		}

		[Test]
		public void RequiredInitTest ()
		{
			string code =
				"open class BaseWithReq {\n" +
				"    public var x: String\n" +
				"    public required init (s: String) {\n" +
				"        x = s\n" +
				"    }\n" +
				"}\n" +
				"open class SubOfBase : BaseWithReq {\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (2, module.Classes.Count (), "Expected 2 classes");
			var baseClass = module.Classes.FirstOrDefault (cl => cl.Name == "BaseWithReq");
			Assert.IsNotNull (baseClass, "didn't find base class");
			var subClass = module.Classes.FirstOrDefault (cl => cl.Name == "SubOfBase");
			Assert.IsNotNull (subClass, "didn't find sub class");

			var baseInit = baseClass.AllConstructors ().FirstOrDefault ();
			Assert.IsNotNull (baseInit, "no constructors in base class");
			Assert.IsTrue (baseInit.IsRequired, "incorrect IsRequired on base class");

			var subInit = subClass.AllConstructors ().FirstOrDefault ();
			Assert.IsNotNull (subInit, "no constructors in sub class");
			Assert.IsTrue (subInit.IsRequired, "incorrect IsRequired on sub class");
		}

		[Test]
		public void NotRequiredInitTest ()
		{
			string code =
				"open class BaseWithoutReq {\n" +
				"    public var x: String\n" +
				"    public init (s: String) {\n" +
				"        x = s\n" +
				"    }\n" +
				"}\n" +
				"open class SubOfBase : BaseWithoutReq {\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (2, module.Classes.Count (), "Expected 2 classes");
			var baseClass = module.Classes.FirstOrDefault (cl => cl.Name == "BaseWithoutReq");
			Assert.IsNotNull (baseClass, "didn't find base class");
			var subClass = module.Classes.FirstOrDefault (cl => cl.Name == "SubOfBase");
			Assert.IsNotNull (subClass, "didn't find sub class");

			var baseInit = baseClass.AllConstructors ().FirstOrDefault ();
			Assert.IsNotNull (baseInit, "no constructors in base class");
			Assert.IsFalse (baseInit.IsRequired, "incorrect IsRequired on base class");

			var subInit = subClass.AllConstructors ().FirstOrDefault ();
			Assert.IsNotNull (subInit, "no constructors in sub class");
			Assert.IsFalse (subInit.IsRequired, "incorrect IsRequired on sub class");

		}

		[Test]
		public void TestPublicPrivateParamNames ()
		{
			string code = "public func foo(seen notseen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PublicName, "wrong public name");
			Assert.AreEqual ("notseen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			Assert.IsTrue (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[Test]
		public void TestOnlyPublicParamNames ()
		{
			string code = "public func foo(seen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PublicName, "wrong public name");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			Assert.IsTrue (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[Test]
		public void TestNotRequiredParamName ()
		{
			string code = "public func foo(_ seen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.AreEqual ("", func.ParameterLists [0] [0].PublicName, "wrong public name");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			Assert.IsFalse (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[Test]
		public void TestSimpleVariadicFunc ()
		{
			string code = "public func itemsAsArray (a:Int ...) -> [Int] {\n return a\n}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itemsAsArray");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.IsTrue (func.ParameterLists [0] [0].IsVariadic, "Parameter item is not marked variadic");
			Assert.IsTrue (func.IsVariadic, "Func is not mared variadic");
		}
		[Test]
		public void TestSimpleNotVariadicFunc ()
		{
			string code = "public func itemsAsArray (a:Int) -> [Int] {\n return [a]\n}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itemsAsArray");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.IsFalse (func.ParameterLists [0] [0].IsVariadic, "Parameter item is not marked variadic");
			Assert.IsFalse (func.IsVariadic, "Func is not mared variadic");
		}

		[Test]
		public void TestReturnsOptionalProtocol ()
		{
			var code = @"
public protocol Foo {
	func itsABool() -> Bool
}
public func itMightBeAProtocol () -> Foo? {
	return nil
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itMightBeAProtocol");
			Assert.IsNotNull (func, "no function");
			var returnType = func.ReturnTypeSpec as NamedTypeSpec;
			Assert.IsNotNull (returnType, "not a named type spec");
			Assert.IsTrue (returnType.GenericParameters.Count == 1, "Expected 1 generic parameter");
			var boundType = returnType.GenericParameters [0] as NamedTypeSpec;
			Assert.IsNotNull (boundType, "wrong kind of bound type");
			Assert.AreEqual ("SomeModule.Foo", boundType.Name, "Wrong bound type name");

		}

		[Test]
		public void TestPropReturnsOptionalProtocol ()
		{
			var code = @"
public protocol Foo {
	func itsABool() -> Bool
}
public class Container {
	public init () { }
	public var itMightBeAProtocol : Foo? = nil
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.FirstOrDefault (c => c.Name == "Container");
			Assert.IsNotNull (cl, "no class");
			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "itMightBeAProtocol");
			Assert.IsNotNull (prop, "no prop");
			var returnType = prop.TypeSpec as NamedTypeSpec;
			Assert.IsNotNull (returnType, "not a named type spec");
			Assert.IsTrue (returnType.GenericParameters.Count == 1, "Expected 1 generic parameter");
			var boundType = returnType.GenericParameters [0] as NamedTypeSpec;
			Assert.IsNotNull (boundType, "wrong kind of bound type");
			Assert.AreEqual ("SomeModule.Foo", boundType.Name, "Wrong bound type name");

		}


		[Test]
		public void TestConvenienceCtor ()
		{
			var code = @"
open class Foo {
	private var x: String
	public init (val: String) {
		x = val
	}
	public convenience init (intval: Int) {
		self.init (val: String(intval))
	}
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			foreach (var ctor in cl.AllConstructors()) {
				var item = ctor.ParameterLists.Last ().Last ();
				if (item.PublicName == "val")
					Assert.IsFalse (ctor.IsConvenienceInit, "designated ctor marked convenience");
				else
					Assert.IsTrue (ctor.IsConvenienceInit, "convenience ctor marked designated");
			}
		}

		[Test]
		public void TestProtocolListType ()
		{
			var code = @"
public protocol FooA {
    func A()
}
public protocol FooB {
    func B()
}
public func joe (a: FooA & FooB) {
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (fn => fn.Name == "joe");
			Assert.IsNotNull (func, "no func");

			var parm = func.ParameterLists [0] [0];
			var protoList = parm.TypeSpec as ProtocolListTypeSpec;
			Assert.IsNotNull (protoList, "not a proto list");
			Assert.AreEqual (2, protoList.Protocols.Count);
		}
	}
}

