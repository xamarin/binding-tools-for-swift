// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using SwiftReflector.IOUtils;
using NUnit.Framework;
using System.IO;
using tomwiftytest;
using System.Xml.Linq;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;
using System.Linq;
using SwiftReflector;
using SwiftReflector.SwiftInterfaceReflector;
using SwiftReflector.TypeMapping;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class DynamicXmlTests {

		public enum ReflectorMode {
			Compiler,
			Parser,
			Comparator,
		}
		static TypeDatabase typeDatabase;

		static DynamicXmlTests ()
		{
			typeDatabase = new TypeDatabase ();
			foreach (var dbPath in Compiler.kTypeDatabases) {
				if (!Directory.Exists (dbPath))
					continue;
				foreach (var dbFile in Directory.GetFiles (dbPath, "*.xml")) {
					typeDatabase.Read (dbFile);
				}
			}
		}

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

		XDocument ParserToXDocument (string directory, string moduleName)
		{
			var parser = new SwiftInterfaceReflector (typeDatabase, new NoLoadLoader ());
			return parser.Reflect (Path.Combine (directory, moduleName + ".swiftinterface"));
		}

		List<ModuleDeclaration> ParserToModule (string directory, string moduleName)
		{
			var decls = new List<ModuleDeclaration> ();
			var doc = ParserToXDocument (directory, moduleName);
			return Reflector.FromXml (doc);
		}

		List<ModuleDeclaration> ComparatorToModule (CustomSwiftCompiler compiler, string moduleName)
		{
			var compilerDoc = compiler.ReflectToXDocument (null, null, null, moduleName);
			var parserDoc = ParserToXDocument (compiler.DirectoryPath, moduleName);

			var diffs = XmlComparator.Compare (compilerDoc, parserDoc);
			var sb = new StringBuilder ();
			foreach (var s in diffs) {
				sb.Append ('\n').Append (s);
			}
			Assert.AreEqual (0, diffs.Count, "Diffs in xml: " + sb.ToString ());
			return Reflector.FromXml (compilerDoc);
		}

		List<ModuleDeclaration> ReflectToModules (string code, string moduleName, ReflectorMode mode = ReflectorMode.Compiler)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: moduleName);


			switch (mode) {
			case ReflectorMode.Compiler:
				return compiler.ReflectToModules (null, null, null, moduleName);
			case ReflectorMode.Parser:
				return ParserToModule (compiler.DirectoryPath, moduleName);
			case ReflectorMode.Comparator:
				return ComparatorToModule (compiler, moduleName);
			default:
				throw new ArgumentOutOfRangeException (nameof (mode));
			}

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


		void TestFuncReturning (string declaredType, string value, string expectedType, ReflectorMode mode = ReflectorMode.Compiler)
		{
			string code = String.Format ("public func foo() -> {0} {{ return {1} }}", declaredType, value);
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningBool (ReflectorMode mode)
		{
			TestFuncReturning ("Bool", "true", "Swift.Bool", mode);
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningInt (ReflectorMode mode)
		{
			TestFuncReturning ("Int", "42", "Swift.Int", mode);
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningUInt (ReflectorMode mode)
		{
			TestFuncReturning ("UInt", "43", "Swift.UInt", mode);
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningFloat (ReflectorMode mode)
		{
			TestFuncReturning ("Float", "2.0", "Swift.Float", mode);
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningDouble (ReflectorMode mode)
		{
			TestFuncReturning ("Double", "3.0", "Swift.Double", mode);
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningString (ReflectorMode mode)
		{
			TestFuncReturning ("String", "\"nothing\"", "Swift.String", mode);
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestEmptyClass (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.AreEqual (1, module.Classes.Count (), "wrong classes count");
			Assert.AreEqual (0, module.Functions.Count (), "wrong function count");
			Assert.AreEqual (0, module.Structs.Count (), "wrong structs count");
			Assert.AreEqual ("Foo", module.Classes.First ().Name, "wrong name");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestEmptyStruct (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			Assert.AreEqual (0, module.Classes.Count (), "wrong classes count");
			Assert.AreEqual (0, module.Functions.Count (), "wrong function count");
			Assert.AreEqual (1, module.Structs.Count (), "wrong structs count");
			Assert.AreEqual ("Foo", module.Structs.First ().Name, "wrong name");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestStructLayout (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { public var X:Int;\n public var Y:Bool; public var Z: Float; }", "SomeModule", mode)
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "incorrect parameter name")]
		public void TestClassWithConstructor (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule", mode)
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestClassHasDestructor (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.NotNull (theClass, "not a class");
			FunctionDeclaration dtor = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".dtor");
			Assert.NotNull (dtor, "not a destructor");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void FuncReturningTuple (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public func returnTuple()->(Int,Float) { return (0, 3.0); }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnTuple");
			var result = func.ReturnTypeName.Replace (" ", "");
			Assert.AreEqual ("(Swift.Int,Swift.Float)", result, "wrong type");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void FuncReturningDictionary (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public func returnDict()->[Int:Float] { return [Int:Float](); }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnDict");
			var returnType = func.ReturnTypeName.Replace (" ", "");
			Assert.AreEqual ("Swift.Dictionary<Swift.Int,Swift.Float>", returnType, "wrong type");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void FuncReturningIntThrows (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public enum MathError : Error {\ncase divZero\n}\n" +
								    "public func returnInt(a:Int) throws ->Int { if a < 1\n{\n throw MathError.divZero\n }\n else {\n return a\n}\n}", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnInt");
			Assert.AreEqual ("Swift.Int", func.ReturnTypeName, "wrong type");
			Assert.AreEqual (true, func.HasThrows, "doesn't throw");
		}



		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void FuncReturningIntOption (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public func returnIntOpt()->Int? { return 3; }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnIntOpt");
			Assert.AreEqual ("Swift.Optional<Swift.Int>", func.ReturnTypeName, "wrong type");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void GlobalBool (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public var aGlobal:Bool = true", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			Assert.NotNull (module, "not module");
			PropertyDeclaration decl = module.TopLevelProperties.FirstOrDefault (f => f.Name == "aGlobal");
			Assert.IsNotNull (decl, "no declaration");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest1 (ReflectorMode mode)
		{
			string code = "public enum foo { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			Assert.AreEqual (edecl.Name, "foo", "wrong name");
			Assert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			Assert.IsTrue (edecl.IsTrivial, "wrong triviality");
			Assert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			Assert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest2 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(Int), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest3 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest4 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(Int), b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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




		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest5 (ReflectorMode mode)
		{
			string code = "public enum foo:Int { case a=1, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest6 (ReflectorMode mode)
		{
			string code = "public enum foo:Int { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void EnumSmokeTest7 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Bool), d(Float) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void OptionalSmokeTest1 (ReflectorMode mode)
		{
			string code = "public func optInt(x:Int) -> Int? { if (x >= 0) { return x; }\nreturn nil; }\n";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "Need to desugar typealiases")]
		public void TypeAliasTest (ReflectorMode mode)
		{
			string code = "public typealias Foo = OpaquePointer\n" +
				"public typealias Bar = Foo\n" +
				"public func aliased(a: Bar, b: (Bar)->()) {\n}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DeprecatedFunction (ReflectorMode mode)
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.IsDeprecated, "deprecated");
			Assert.IsFalse (func.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DeprecatedClass (ReflectorMode mode)
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "no class");
			Assert.IsTrue (cl.IsDeprecated, "not deprecated");
			Assert.IsFalse (cl.IsUnavailable, "available");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ObsoletedFunction (ReflectorMode mode)
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "func");
			Assert.IsFalse (func.IsDeprecated, "deprecated");
			Assert.IsTrue (func.IsUnavailable, "unavilable");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ObsoletedClass (ReflectorMode mode)
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "no class");
			Assert.IsFalse (cl.IsDeprecated, "deprecated");
			Assert.IsTrue (cl.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void UnavailableFunction (ReflectorMode mode)
		{
			string code =
				"@available(*, unavailable)" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no func");
			Assert.IsFalse (func.IsDeprecated, "deprecated");
			Assert.IsTrue (func.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void UnavailableClass (ReflectorMode mode)
		{
			string code =
				"@available(*, unavailable)" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "class");
			Assert.IsFalse (cl.IsDeprecated, "deprecated");
			Assert.IsTrue (cl.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "returning wrong unavailble")]
		public void MethodInStruct (ReflectorMode mode)
		{
			string code =

				"public struct CommandEvaluation {\n" +
				"	@available(*, deprecated, message: \"Please use parameter (at) instead\")\n" +
 				"       public func retrieveParameter (at index: Int) throws->String {\n" +
				"               return \"foo\"\n" +
				"       }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var st = module.Structs.FirstOrDefault (f => f.Name == "CommandEvaluation");
			Assert.IsNotNull (st, "no struct");
			Assert.IsFalse (st.IsDeprecated, "deprecated");
			Assert.IsFalse (st.IsUnavailable, "unavailable");
			var func = st.AllMethodsNoCDTor ().Where (fn => fn.Name == "retrieveParameter").FirstOrDefault ();
			Assert.IsNotNull (func, "no func");
			Assert.IsTrue (func.IsDeprecated, "deprecated");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void UnavailableProperty (ReflectorMode mode)
		{
			string code =
				"public struct JSON {\n" +
				"    @available(*, unavailable, renamed:\"null\")\n" +
    				"    public static var nullJSON: Int { return 3 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var st = module.Structs.FirstOrDefault (f => f.Name == "JSON");
			var prop = st.AllProperties ().Where (fn => fn.Name == "nullJSON").FirstOrDefault ();
			Assert.IsNotNull (prop, "no prop");
			Assert.IsTrue (prop.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ExtensionProperty (ReflectorMode mode)
		{
			string code =
				"public extension Double {\n" +
				"    public var millisecond: Double  { return self / 1000 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			Assert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			Assert.AreEqual (2, ext.Members.Count, $"Expected 2 members but got {ext.Members.Count}");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ExtensionFunc (ReflectorMode mode)
		{
			string code =
				"public extension Double {\n" +
				"    public func DoubleIt() -> Double  { return self * 2; }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			Assert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			Assert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			Assert.AreEqual (1, ext.Members.Count, $"Expected 1 member but got {ext.Members.Count}");
			var func = ext.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (func, $"Expected a FunctionDeclaration but got {ext.Members [0].GetType ().Name}");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ExtensionProto (ReflectorMode mode)
		{
			string code =
				"public protocol Printer {\n" +
				"    func printIt()\n" +
				"}\n" +
				"extension Double : Printer {\n" +
				"    public func printIt() { print(self) }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ObjCOptionalMember (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc optional func foo()\n" +
				"    func bar()\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "need to handle optional props/methods")]
		public void ObjCOptionalProp (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc optional var X:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "need to handle optional props/methods")]
		public void ObjCOptionalSubsript (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc optional subscript (index:Int) ->Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "NRE parsing XML")]
		public void PropertyVisibility (ReflectorMode mode)
		{
			PropertyVisibilityCore ("open", Accessibility.Open, mode);
			PropertyVisibilityCore ("public", Accessibility.Public, mode);
			PropertyVisibilityCore ("internal", Accessibility.Internal, mode);
			PropertyVisibilityCore ("private", Accessibility.Private, mode);
		}

		void PropertyVisibilityCore (string swiftVisibility, Accessibility accessibility, ReflectorMode mode)
		{
			string code = $@"open class Foo {{
			open {swiftVisibility} (set) weak var parent: Foo?
}}";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.AllClasses.Count (), "Expected a class.");
			var fooClass = module.AllClasses.First ();
			Assert.AreEqual (1, fooClass.AllProperties ().Count (), "Expected one property.");
			Assert.AreEqual (accessibility, fooClass.AllProperties () [0].GetSetter ().Access, "Unexpected Visibility.");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ObjCMemberSelector (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc func foo()\n" +
				"    @objc func bar(a:Int)\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "property selector incorrect")]
		public void ObjCPropSelector (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc var X:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "property selector incorrect")]
		public void ObjCPropSelectorLower (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc var x:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			Assert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			Assert.IsTrue (proto.IsObjC, "not objc protocol");
			Assert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			Assert.AreEqual (3, proto.Members.Count (), "incorrect number of members");
			var xProp = proto.Members.OfType<PropertyDeclaration> ().Where (f => f.Name == "x").FirstOrDefault ();
			Assert.IsNotNull (xProp, "No prop named x");
			var getter = xProp.GetGetter ();
			Assert.IsNotNull (getter, "Null getter");
			Assert.AreEqual ("x", getter.ObjCSelector, $"incorrect get X selector name {getter.ObjCSelector}");
			var setter = xProp.GetSetter ();
			Assert.IsNotNull (setter, "Null setter");
			Assert.AreEqual ("setX:", setter.ObjCSelector, $"incorrect set X selector name {setter.ObjCSelector}");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void ObjCSubsriptSelector (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc subscript (index:Int) ->Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void RequiredInitTest (ReflectorMode mode)
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

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void NotRequiredInitTest (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestPublicPrivateParamNames (ReflectorMode mode)
		{
			string code = "public func foo(seen notseen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PublicName, "wrong public name");
			Assert.AreEqual ("notseen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			Assert.IsTrue (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "Wrong name")]
		public void TestOnlyPublicParamNames (ReflectorMode mode)
		{
			string code = "public func foo(seen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PublicName, "wrong public name");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			Assert.IsTrue (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestNotRequiredParamName (ReflectorMode mode)
		{
			string code = "public func foo(_ seen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.AreEqual ("", func.ParameterLists [0] [0].PublicName, "wrong public name");
			Assert.AreEqual ("seen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			Assert.IsFalse (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestSimpleVariadicFunc (ReflectorMode mode)
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestSimpleNotVariadicFunc (ReflectorMode mode)
		{
			string code = "public func itemsAsArray (a:Int) -> [Int] {\n return [a]\n}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itemsAsArray");
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			Assert.IsFalse (func.ParameterLists [0] [0].IsVariadic, "Parameter item is not marked variadic");
			Assert.IsFalse (func.IsVariadic, "Func is not mared variadic");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestReturnsOptionalProtocol (ReflectorMode mode)
		{
			var code = @"
public protocol Foo {
	func itsABool() -> Bool
}
public func itMightBeAProtocol () -> Foo? {
	return nil
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestPropReturnsOptionalProtocol (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestConvenienceCtor (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestProtocolListType (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (fn => fn.Name == "joe");
			Assert.IsNotNull (func, "no func");

			var parm = func.ParameterLists [0] [0];
			var protoList = parm.TypeSpec as ProtocolListTypeSpec;
			Assert.IsNotNull (protoList, "not a proto list");
			Assert.AreEqual (2, protoList.Protocols.Count, "wrong protocol list count");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestFuncReturningAny (ReflectorMode mode)
		{
			var code = @"
public func returnsAny() -> Any {
    return 7
}";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (fn => fn.Name == "returnsAny");
			Assert.IsNotNull (func, "no func");

			var returnType = func.ReturnTypeSpec as NamedTypeSpec;
			Assert.IsNotNull (returnType, "no return type");
			Assert.AreEqual ("Swift.Any", returnType.Name, $"Wrong type: {returnType.Name}");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void AssocTypeSmoke (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing
	func getThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "grammar error for associated types")]
		public void AssocTypeTimesTwo (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype ThingOne
	associatedtype ThingTwo
	func doThing(a: ThingOne) -> ThingTwo
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void AssocTypeDefaultType (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing = Int
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "Don't have IteratorProtocol in type database")]
		public void AssocTypeConformance (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing : IteratorProtocol
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "Hang on parse")]
		public void AssocTypeSuper (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
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

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void FindsAssocTypeByName (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			Assert.IsNotNull (protocol, "no protocol");
			Assert.IsTrue (protocol.HasAssociatedTypes, "no associated types?!");
			var assoc = protocol.AssociatedTypeNamed ("Thing");
			Assert.IsNotNull (assoc, "couldn't find associated type");
			assoc = protocol.AssociatedTypeNamed ("ThroatwarblerMangrove");
			Assert.IsNull (assoc, "Found a non-existent associated type");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncNoArgsNoReturnOutput (ReflectorMode mode)
		{
			var code = @"public func SomeFunc () { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc () -> ()", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncNoArgsReturnsIntOutput (ReflectorMode mode)
		{
			var code = @"public func SomeFunc () -> Int { return 3; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc () -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncNoArgsReturnsIntThrowsOutput (ReflectorMode mode)
		{
			var code = @"public func SomeFunc () throws -> Int { return 3; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc () throws -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncOneArgSamePubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncOneArgDiffPubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (b a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (b a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncOneArgNoPubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (_ a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (_ a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestTLFuncTwoArgSamePubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (a: Int, b: Int) -> Int { return a + b; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.SomeFunc (a: Swift.Int, b: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestPropGetFunc (ReflectorMode mode)
		{
			var code = @"
public class Foo {
	public init () { }
	public var prop: Int = 0
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "get_prop").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public var SomeModule.Foo.prop: Swift.Int { get }", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "Prop should be get/set not get only")]
		public void TestPropGetSet (ReflectorMode mode)
		{
			var code = @"
public class Foo {
	public init () { }
	public var prop: Int = 0
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllProperties ().Where (p => p.Name == "prop").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public var SomeModule.Foo.prop: Swift.Int { get set }", output, "wrong signature");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void TestSubscriptGetSet (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "get_subscript").FirstOrDefault ();
			Assert.IsNotNull (func, "null func");
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.Foo.subscript [_ Index: Swift.Int] -> Swift.String { get }", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "missing class generic in signature")]
		public void TestGenericMethodInGenericClass (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "printIt").FirstOrDefault ();
			var output = func.ToString ();
			Assert.AreEqual ("Public SomeModule.Foo.printIt<T, U> (a: U) -> ()", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DetectsSelfEasy (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> Self
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DetectsSelfEasy1 (ReflectorMode mode)
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
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DetectsSelfInTuple (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> (Int, Bool, Self)
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DetectsSelfInOptional (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> Self?
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DetectsSelfInBoundGeneric (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> UnsafeMutablePointer<Self>
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void DetectsSelfInClosure (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami(a: (Self)->()) -> ()
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			Assert.IsNotNull (proto, "no protocol");
			Assert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "not coming through as a let")]
		public void TopLevelLet (ReflectorMode mode)
		{
			var code = "public let myVar:Int = 42";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var prop = module.Properties.Where (p => p.Name == "myVar").FirstOrDefault ();
			Assert.IsNotNull (prop, "no prop");
			Assert.IsTrue (prop.IsLet, "not a let");
			Assert.IsNull (prop.GetSetter (), "why is there a setter");
		}


		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "not coming through as a let")]
		public void TheEpsilonIssue (ReflectorMode mode)
		{
			string code = "public let 𝑒 = 2.718\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var prop = module.Properties.Where (p => p.Name == "𝑒").FirstOrDefault ();
			Assert.IsNotNull (prop, "no prop");
			Assert.IsTrue (prop.IsLet, "not a let");
			Assert.IsNull (prop.GetSetter (), "why is there a setter");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser, Ignore = "Colon coming through on precedence")]
		public void InfixOperatorDecl (ReflectorMode mode)
		{
			var code = @"infix operator *^* : AdditionPrecedence
extension Int {
	public static func *^* (lhs: Int, rhs: Int) -> Int {
		return 2 * lhs + 2 * rhs
	}
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var opDecl = module.Operators.Where (op => op.Name == "*^*").FirstOrDefault ();
			Assert.AreEqual ("*^*", opDecl.Name, "name mismatch");
			Assert.AreEqual ("AdditionPrecedence", opDecl.PrecedenceGroup, "predence group mismatch");
			Assert.AreEqual (OperatorType.Infix, opDecl.OperatorType, "operator type mismatch");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void PrefixOperatorDecl (ReflectorMode mode)
		{
			var code = @"prefix operator *^^*
extension Int {
	public static prefix func *^^* (lhs: Int) -> Int {
		return 2 * lhs
	}
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var opDecl = module.Operators.Where (op => op.Name == "*^^*").FirstOrDefault ();
			Assert.AreEqual ("*^^*", opDecl.Name, "name mismatch");
			Assert.IsNull (opDecl.PrecedenceGroup, "predence group mismatch");
			Assert.AreEqual (OperatorType.Prefix, opDecl.OperatorType, "operator type mismatch");
		}

		[TestCase (ReflectorMode.Compiler)]
		[TestCase (ReflectorMode.Parser)]
		public void PostfixOperatorDecl (ReflectorMode mode)
		{
			var code = @"postfix operator *^&^*
extension Int {
	public static postfix func *^&^* (lhs: Int) -> Int {
		return 2 * lhs
	}
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module is null");
			var opDecl = module.Operators.Where (op => op.Name == "*^&^*").FirstOrDefault ();
			Assert.AreEqual ("*^&^*", opDecl.Name, "name mismatch");
			Assert.IsNull (opDecl.PrecedenceGroup, "predence group mismatch");
			Assert.AreEqual (OperatorType.Postfix, opDecl.OperatorType, "operator type mismatch");
		}
	}
}
