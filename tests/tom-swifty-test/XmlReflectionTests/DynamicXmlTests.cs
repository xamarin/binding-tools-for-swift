// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
			Assert.IsNotNull (module, "no module");
			Assert.AreEqual (1, module.Functions.Count (), "wrong func count");
			FunctionDeclaration func = module.Functions.First ();
			Assert.IsNotNull (func, "no func");
			Assert.AreEqual (func.Name, "foo", "bad name");
			Assert.AreEqual (expectedType, func.ReturnTypeName, "wrong return type");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong parameter list count");
			Assert.AreEqual (0, func.ParameterLists [0].Count, "wrong parameter count");
			NamedTypeSpec ns = func.ReturnTypeSpec as NamedTypeSpec;
			Assert.NotNull (ns, "not a named type spec");
			Assert.AreEqual (expectedType, ns.Name, "wrong name");
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
			Assert.AreEqual (1, module.Classes.Count (), "wrong classes count");
			Assert.AreEqual (0, module.Functions.Count (), "wrong function count");
			Assert.AreEqual (0, module.Structs.Count (), "wrong structs count");
			Assert.AreEqual ("Foo", module.Classes.First ().Name, "wrong name");
		}

		[Test]
		public void TestEmptyStruct ()
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.AreEqual (0, module.Classes.Count (), "wrong classes count");
			Assert.AreEqual (0, module.Functions.Count (), "wrong function count");
			Assert.AreEqual (1, module.Structs.Count (), "wrong structs count");
			Assert.AreEqual ("Foo", module.Structs.First ().Name, "wrong name");
		}

		[Test]
		public void TestStructLayout ()
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { public var X:Int;\n public var Y:Bool; public var Z: Float; }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "no module");
			StructDeclaration theStruct = module.Structs.FirstOrDefault (s => s.Name == "Foo");
			Assert.NotNull (theStruct, "no struct");
			List<PropertyDeclaration> props = theStruct.Members.OfType<PropertyDeclaration> ().ToList ();
			Assert.AreEqual (3, props.Count, "wrong props count");
			Assert.AreEqual ("X", props [0].Name, "not x");
			Assert.AreEqual ("Y", props [1].Name, "not y");
			Assert.AreEqual ("Z", props [2].Name, "not z");
			Assert.AreEqual ("Swift.Int", props [0].TypeName, "not int");
			Assert.AreEqual ("Swift.Bool", props [1].TypeName, "not bool");
			Assert.AreEqual ("Swift.Float", props [2].TypeName, "not float");
		}

		[Test]
		public void TestClassWithConstructor ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.NotNull (theClass, "not class");
			FunctionDeclaration cons = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".ctor");
			Assert.NotNull (cons, "no constructor");
			Assert.AreEqual (2, cons.ParameterLists.Count, "wrong parameterlist count");
			Assert.AreEqual (1, cons.ParameterLists [1].Count, "wrong arg count");
			Assert.AreEqual ("Swift.Int", cons.ParameterLists [1] [0].TypeName, "wrong type");
			Assert.AreEqual ("y", cons.ParameterLists [1] [0].PublicName, "wrong name");
		}

		[Test]
		public void TestClassHasDestructor ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.NotNull (theClass, "not a class");
			FunctionDeclaration dtor = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".dtor");
			Assert.NotNull (dtor, "not a destructor");
		}

		[Test]
		public void FuncReturningTuple ()
		{
			ModuleDeclaration module = ReflectToModules ("public func returnTuple()->(Int,Float) { return (0, 3.0); }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnTuple");
			Assert.AreEqual ("(Swift.Int, Swift.Float)", func.ReturnTypeName, "wrong type");
		}

		[Test]
		public void FuncReturningDictionary ()
		{
			ModuleDeclaration module = ReflectToModules ("public func returnDict()->[Int:Float] { return [Int:Float](); }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnDict");
			Assert.AreEqual ("Swift.Dictionary<Swift.Int, Swift.Float>", func.ReturnTypeName, "wrong type");
		}


		[Test]
		public void FuncReturningIntThrows ()
		{
			ModuleDeclaration module = ReflectToModules ("public enum MathError : Error {\ncase divZero\n}\n" +
								    "public func returnInt(a:Int) throws ->Int { if a < 1\n{\n throw MathError.divZero\n }\n else {\n return a\n}\n}", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnInt");
			Assert.AreEqual ("Swift.Int", func.ReturnTypeName, "wrong type");
			Assert.AreEqual (true, func.HasThrows, "doesn't throw");
		}



		[Test]
		public void FuncReturningIntOption ()
		{
			ModuleDeclaration module = ReflectToModules ("public func returnIntOpt()->Int? { return 3; }", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnIntOpt");
			Assert.AreEqual ("Swift.Optional<Swift.Int>", func.ReturnTypeName, "wrong type");
		}

		[Test]
		public void GlobalBool ()
		{
			ModuleDeclaration module = ReflectToModules ("public var aGlobal:Bool = true", "SomeModule")
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			PropertyDeclaration decl = module.TopLevelProperties.FirstOrDefault (f => f.Name == "aGlobal");
			Assert.IsNotNull (decl, "no declaration");
		}

		[Test]
		public void EnumSmokeTest1 ()
		{
			string code = "public enum foo { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			Assert.IsTrue (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[Test]
		public void EnumSmokeTest2 ()
		{
			string code = "public enum foo { case a(Int), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			foreach (EnumElement elem in edecl.Elements) {
				Assert.IsTrue (elem.HasType, "no type");
			}
			Assert.IsFalse (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsIntegral, "wrong integral");
			Assert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[Test]
		public void EnumSmokeTest3 ()
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			foreach (EnumElement elem in edecl.Elements) {
				Assert.IsTrue (elem.HasType, "no type");
			}
			Assert.IsFalse (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsIntegral, "wrong integral");
			Assert.IsFalse (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[Test]
		public void EnumSmokeTest4 ()
		{
			string code = "public enum foo { case a(Int), b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			Assert.IsFalse (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsIntegral, "wrong integral");
			Assert.IsFalse (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}




		[Test]
		public void EnumSmokeTest5 ()
		{
			string code = "public enum foo:Int { case a=1, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			Assert.IsFalse (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsIntegral, "wrong integral");
			Assert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsTrue (edecl.HasRawType, "wrong has raw type");
			Assert.AreEqual ("Swift.Int", edecl.RawTypeName, "wrong raw type name");
		}

		[Test]
		public void EnumSmokeTest6 ()
		{
			string code = "public enum foo:Int { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			Assert.IsFalse (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsIntegral, "wrong integral");
			Assert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsTrue (edecl.HasRawType, "wrong has raw type");
			Assert.AreEqual ("Swift.Int", edecl.RawTypeName, "wrong raw type name");
		}

		[Test]
		public void EnumSmokeTest7 ()
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Bool), d(Float) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			Assert.IsFalse (edecl.IsTrivial, "wrong triviality");
			Assert.IsFalse (edecl.IsIntegral, "wrong integral");
			Assert.IsFalse (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsFalse (edecl.HasRawType, "wrong has raw type");

			Assert.AreEqual ("Swift.UInt", edecl ["a"].TypeName, "wrong raw type name uint");
			Assert.AreEqual ("Swift.Int", edecl ["b"].TypeName, "wrong raw type name int");
			Assert.AreEqual ("Swift.Bool", edecl ["c"].TypeName, "wrong raw type name bool");
			Assert.AreEqual ("Swift.Float", edecl ["d"].TypeName, "wrong raw type name float");
		}


		[Test]
		public void OptionalSmokeTest1 ()
		{
			string code = "public func optInt(x:Int) -> Int? { if (x >= 0) { return x; }\nreturn nil; }\n";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
		}

		[Test]
		public void TypeAliasTest ()
		{
			string code = "public typealias Foo = OpaquePointer\n" +
				"public typealias Bar = Foo\n" +
				"public func aliased(a: Bar, b: (Bar)->()) {\n}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "aliased");
			Assert.IsNotNull (func, "no func");
			var named = func.ParameterLists [0] [0].TypeSpec as NamedTypeSpec;
			Assert.IsNotNull (named, "not a named type spec");
			Assert.AreEqual ("Swift.OpaquePointer", named.Name, "wrong name");
			var closType = func.ParameterLists [0] [1].TypeSpec as ClosureTypeSpec;
			Assert.IsNotNull (closType, "not a closure");
			named = closType.Arguments as NamedTypeSpec;
			Assert.IsNotNull (named, "not a named type spec 2");
			Assert.AreEqual ("Swift.OpaquePointer", named.Name, "wrong name 2");

		}

		[Test]
		public void DeprecatedFunction ()
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
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "no class");
			Assert.IsTrue (cl.IsDeprecated, "not deprecated");
			Assert.IsFalse (cl.IsUnavailable, "available");
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
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "no class");
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
			Assert.IsNotNull (module, "not a module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no func");
			Assert.IsFalse (func.IsDeprecated, "deprecated");
			Assert.IsTrue (func.IsUnavailable, "unavailable");
		}


		[Test]
		public void UnavailableClass ()
		{
			string code =
				"@available(*, unavailable)" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "class");
			Assert.IsFalse (cl.IsDeprecated, "deprecated");
			Assert.IsTrue (cl.IsUnavailable, "unavailable");
		}


		[Test]
		public void MethodInStruct ()
		{
			string code =

				"public struct CommandEvaluation {\n" +
				"	@available(*, deprecated, message: \"Please use parameter (at) instead\")\n" +
 				"       public func retrieveParameter (at index: Int) throws->String {\n" +
				"               return \"foo\"\n" +
				"       }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var st = module.Structs.FirstOrDefault (f => f.Name == "CommandEvaluation");
			Assert.IsNotNull (st, "no struct");
			Assert.IsFalse (st.IsDeprecated, "deprecated");
			Assert.IsFalse (st.IsUnavailable, "unavailable");
			var func = st.AllMethodsNoCDTor ().Where (fn => fn.Name == "retrieveParameter").FirstOrDefault ();
			Assert.IsNotNull (func, "no func");
			Assert.IsTrue (func.IsDeprecated, "deprecated");
		}

		[Test]
		public void UnavailableProperty ()
		{
			string code =
				"public struct JSON {\n" +
				"    @available(*, unavailable, renamed:\"null\")\n" +
    				"    public static var nullJSON: Int { return 3 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var st = module.Structs.FirstOrDefault (f => f.Name == "JSON");
			var prop = st.AllProperties ().Where (fn => fn.Name == "nullJSON").FirstOrDefault ();
			Assert.IsNotNull (prop, "no prop");
			Assert.IsTrue (prop.IsUnavailable, "unavailable");
		}


		[Test]
		public void ExtensionProperty ()
		{
			string code =
				"public extension Double {\n" +
				"    public var millisecond: Double  { return self / 1000 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
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
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			Assert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			Assert.AreEqual (1, ext.Members.Count, $"Expected 1 member but got {ext.Members.Count}");
			var func = ext.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (func, $"Expected a FunctionDeclaration but got {ext.Members [0].GetType ().Name}");
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
			Assert.IsNotNull (module, "not a module");
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

			foreach (var ctor in cl.AllConstructors ()) {
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
			Assert.AreEqual (2, protoList.Protocols.Count, "wrong protocol list count");
		}

		[Test]
		public void TestFuncReturningAny ()
		{
			var code = @"
public func returnsAny() -> Any {
    return 7
}";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (fn => fn.Name == "returnsAny");
			Assert.IsNotNull (func, "no func");

			var returnType = func.ReturnTypeSpec as NamedTypeSpec;
			Assert.IsNotNull (returnType, "no return type");
			Assert.AreEqual ("Swift.Any", returnType.Name, $"Wrong type: {returnType.Name}");
		}

		[Test]
		public void AssocTypeSmoke ()
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing
	func getThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			Assert.AreEqual ("Thing", assoc.Name, "wrong name");
			Assert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			Assert.IsNull (assoc.SuperClass, "non-null superclass");
			Assert.IsNull (assoc.DefaultType, "non-null default type");
		}

		[Test]
		public void AssocTypeTimesTwo ()
		{
			var code = @"
public protocol HoldsThing {
	associatedtype ThingOne
	associatedtype ThingTwo
	func doThing(a: ThingOne) -> ThingTwo
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.AreEqual (2, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			Assert.AreEqual ("ThingOne", assoc.Name, "wrong name");
			assoc = protocol.AssociatedTypes [1];
			Assert.AreEqual ("ThingTwo", assoc.Name, "wrong name");
			Assert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			Assert.IsNull (assoc.SuperClass, "non-null superclass");
			Assert.IsNull (assoc.DefaultType, "non-null default type");
		}

		[Test]
		public void AssocTypeDefaultType ()
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing = Int
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			Assert.AreEqual ("Thing", assoc.Name, "wrong name");
			Assert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			Assert.IsNull (assoc.SuperClass, "non-null superclass");
			Assert.IsNotNull (assoc.DefaultType, "null default type");
			Assert.AreEqual ("Swift.Int", assoc.DefaultType.ToString (), "wrong type");
		}

		[Test]
		public void AssocTypeConformance ()
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing : IteratorProtocol
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			Assert.AreEqual ("Thing", assoc.Name, "wrong name");
			Assert.AreEqual (1, assoc.ConformingProtocols.Count, "wrong number of conf");
			Assert.AreEqual ("Swift.IteratorProtocol", assoc.ConformingProtocols [0].ToString (), "wrong protocol name");
			Assert.IsNull (assoc.SuperClass, "non-null superclass");
			Assert.IsNull (assoc.DefaultType, "non-null default type");
		}


		[Test]
		public void AssocTypeSuper ()
		{
			var code = @"
open class Foo {
	public init () { }
}

public protocol HoldsThing {
	associatedtype Thing : Foo
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			Assert.AreEqual ("Thing", assoc.Name, "wrong name");
			Assert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			Assert.IsNotNull (assoc.SuperClass, "null superclass");
			Assert.AreEqual ("SomeModule.Foo", assoc.SuperClass.ToString (), "wrong superclass name");
			Assert.IsNull (assoc.DefaultType, "non-null default type");
		}

		[Test]
		public void FindsAssocTypeByName ()
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.IsTrue (protocol.HasAssociatedTypes, "no associated types?!");
			var assoc = protocol.AssociatedTypeNamed ("Thing");
			Assert.IsNotNull (assoc, "couldn't find associated type");
			assoc = protocol.AssociatedTypeNamed ("ThroatwarblerMangrove");
			Assert.IsNull (assoc, "Found a non-existent associated type");
		}

		[Test]
		public void TestTLFuncNoArgsNoReturnOutput ()
		{
			var code = @"public func SomeFunc () { }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc () -> ()", output, "wrong signature");
		}

		[Test]
		public void TestTLFuncNoArgsReturnsIntOutput ()
		{
			var code = @"public func SomeFunc () -> Int { return 3; }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc () -> Swift.Int", output, "wrong signature");
		}

		[Test]
		public void TestTLFuncNoArgsReturnsIntThrowsOutput ()
		{
			var code = @"public func SomeFunc () throws -> Int { return 3; }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc () throws -> Swift.Int", output, "wrong signature");
		}

		[Test]
		public void TestTLFuncOneArgSamePubPrivReturnsInt ()
		{
			var code = @"public func SomeFunc (a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[Test]
		public void TestTLFuncOneArgDiffPubPrivReturnsInt ()
		{
			var code = @"public func SomeFunc (b a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (b a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[Test]
		public void TestTLFuncOneArgNoPubPrivReturnsInt ()
		{
			var code = @"public func SomeFunc (_ a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (_ a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[Test]
		public void TestTLFuncTwoArgSamePubPrivReturnsInt ()
		{
			var code = @"public func SomeFunc (a: Int, b: Int) -> Int { return a + b; }";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (a: Swift.Int, b: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[Test]
		public void TestPropGetFunc ()
		{
			var code = @"
public class Foo {
	public init () { }
	public var prop: Int = 0
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "get_prop").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public var SomeModule.Foo.prop: Swift.Int { get }", output, "wrong signature");
		}

		[Test]
		public void TestPropGetSet ()
		{
			var code = @"
public class Foo {
	public init () { }
	public var prop: Int = 0
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllProperties ().Where (p => p.Name == "prop").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public var SomeModule.Foo.prop: Swift.Int { get set }", output, "wrong signature");
		}


		[Test]
		public void TestSubscriptGetSet ()
		{
			var code = @"
public class Foo {
	public init () { }
	public subscript (Index: Int) -> String {
		get {
			return ""nothing""
		}
		set { }
	}
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "get_subscript").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.Foo.subscript [_ Index: Swift.Int] -> Swift.String { get }", output, "wrong signature");
		}

		[Test]
		public void TestGenericMethodInGenericClass ()
		{
			var code = @"
public class Foo<T> {
private var x: T
	public init (a: T) { x = a; }
	public func printIt<U>(a: U) {
		print(x)
		print(a)
	}
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "printIt").FirstOrDefault ();
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.Foo.printIt<T, U> (a: U) -> ()", output, "wrong signature");
		}

		[Test]
		public void DetectsSelfEasy ()
		{
			var code = @"
public protocol Simple {
	func whoami() -> Self
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[Test]
		public void DetectsSelfEasy1 ()
		{
			var code = @"
public protocol NoSelf {
	func whoami (a: NoSelf)
}
public protocol Simple {
	associatedtype Thing
	func whoami(a: Self) -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[Test]
		public void DetectsSelfInTuple ()
		{
			var code = @"
public protocol Simple {
	func whoami() -> (Int, Bool, Self)
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[Test]
		public void DetectsSelfInOptional ()
		{
			var code = @"
public protocol Simple {
	func whoami() -> Self?
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[Test]
		public void DetectsSelfInBoundGeneric ()
		{
			var code = @"
public protocol Simple {
	func whoami() -> UnsafeMutablePointer<Self>
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[Test]
		public void DetectsSelfInClosure ()
		{
			var code = @"
public protocol Simple {
	func whoami(a: (Self)->()) -> ()
}
";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[Test]
		public void TopLevelLet ()
		{
			var code = "public let myVar:Int = 42";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var prop = module.Properties.Where (p => p.Name == "myVar").FirstOrDefault ();
			Assert.IsNotNull (prop, "no prop");
			Assert.IsTrue (prop.IsLet, "not a let");
			Assert.IsNull (prop.GetSetter (), "why is there a setter");
		}


		[Test]
		public void TheEpsilonIssue ()
		{
			string code = "public let 𝑒 = 2.718\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var prop = module.Properties.Where (p => p.Name == "𝑒").FirstOrDefault ();
			Assert.IsNotNull (prop, "no prop");
			Assert.IsTrue (prop.IsLet, "not a let");
			Assert.IsNull (prop.GetSetter (), "why is there a setter");
		}
	}
}
