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
using DylibBinder;
using NUnit.Framework.Legacy;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class DynamicXmlTests {

		public enum ReflectorMode {
			Parser,
			DylibBinder,
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

		XDocument ParserToXDocument (string directory, string moduleName)
		{
			var parser = new SwiftInterfaceReflector (typeDatabase, new NoLoadLoader ());
			return parser.Reflect (Path.Combine (directory, moduleName + ".swiftinterface"));
		}

		List<ModuleDeclaration> ParserToModule (string directory, string moduleName)
		{
			var decls = new List<ModuleDeclaration> ();
			var doc = ParserToXDocument (directory, moduleName);
			return Reflector.FromXml (doc, typeDatabase);
		}

		List<ModuleDeclaration> DylibBinderToModule (string directory, string moduleName)
		{
			var outputPath = Path.Combine (directory, "lib" + moduleName + ".xml");
			DylibBinderReflector.Reflect (Path.Combine (directory, "lib" + moduleName + ".dylib"), outputPath);
			return Reflector.FromXmlFile (outputPath, typeDatabase);
		}

		List<ModuleDeclaration> ReflectToModules (string code, string moduleName, ReflectorMode mode = ReflectorMode.Parser)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: moduleName);


			switch (mode) {
			case ReflectorMode.Parser:
				return ParserToModule (compiler.DirectoryPath, moduleName);
			case ReflectorMode.DylibBinder:
				return DylibBinderToModule (compiler.DirectoryPath, moduleName);
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


		void TestFuncReturning (string declaredType, string value, string expectedType, ReflectorMode mode = ReflectorMode.Parser)
		{
			string code = String.Format ("public func foo() -> {0} {{ return {1} }}", declaredType, value);
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "no module");
			ClassicAssert.AreEqual (1, module.Functions.Count (), "wrong func count");
			FunctionDeclaration func = module.Functions.First ();
			ClassicAssert.IsNotNull (func, "no func");
			ClassicAssert.AreEqual (func.Name, "foo", "bad name");
			ClassicAssert.AreEqual (expectedType, func.ReturnTypeName, "wrong return type");
			ClassicAssert.AreEqual (1, func.ParameterLists.Count, "wrong parameter list count");
			ClassicAssert.AreEqual (0, func.ParameterLists [0].Count, "wrong parameter count");
			NamedTypeSpec ns = func.ReturnTypeSpec as NamedTypeSpec;
			ClassicAssert.NotNull (ns, "not a named type spec");
			ClassicAssert.AreEqual (expectedType, ns.Name, "wrong name");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningBool (ReflectorMode mode)
		{
			TestFuncReturning ("Bool", "true", "Swift.Bool", mode);
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningInt (ReflectorMode mode)
		{
			TestFuncReturning ("Int", "42", "Swift.Int", mode);
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningUInt (ReflectorMode mode)
		{
			TestFuncReturning ("UInt", "43", "Swift.UInt", mode);
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningFloat (ReflectorMode mode)
		{
			TestFuncReturning ("Float", "2.0", "Swift.Float", mode);
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningDouble (ReflectorMode mode)
		{
			TestFuncReturning ("Double", "3.0", "Swift.Double", mode);
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningString (ReflectorMode mode)
		{
			TestFuncReturning ("String", "\"nothing\"", "Swift.String", mode);
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestEmptyClass (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			ClassicAssert.AreEqual (1, module.Classes.Count (), "wrong classes count");
			ClassicAssert.AreEqual (0, module.Functions.Count (), "wrong function count");
			ClassicAssert.AreEqual (0, module.Structs.Count (), "wrong structs count");
			ClassicAssert.AreEqual ("Foo", module.Classes.First ().Name, "wrong name");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestEmptyStruct (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			ClassicAssert.AreEqual (0, module.Classes.Count (), "wrong classes count");
			ClassicAssert.AreEqual (0, module.Functions.Count (), "wrong function count");
			ClassicAssert.AreEqual (1, module.Structs.Count (), "wrong structs count");
			ClassicAssert.AreEqual ("Foo", module.Structs.First ().Name, "wrong name");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestStructLayout (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo { public var X:Int;\n public var Y:Bool; public var Z: Float; }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "no module");
			StructDeclaration theStruct = module.Structs.FirstOrDefault (s => s.Name == "Foo");
			ClassicAssert.NotNull (theStruct, "no struct");
			List<PropertyDeclaration> props = theStruct.Members.OfType<PropertyDeclaration> ().ToList ();
			ClassicAssert.AreEqual (3, props.Count, "wrong props count");
			ClassicAssert.AreEqual ("X", props [0].Name, "not x");
			ClassicAssert.AreEqual ("Y", props [1].Name, "not y");
			ClassicAssert.AreEqual ("Z", props [2].Name, "not z");
			ClassicAssert.AreEqual ("Swift.Int", props [0].TypeName, "not int");
			ClassicAssert.AreEqual ("Swift.Bool", props [1].TypeName, "not bool");
			ClassicAssert.AreEqual ("Swift.Float", props [2].TypeName, "not float");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestClassWithConstructor (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.NotNull (theClass, "not class");
			FunctionDeclaration cons = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".ctor");
			ClassicAssert.NotNull (cons, "no constructor");
			ClassicAssert.AreEqual (2, cons.ParameterLists.Count, "wrong parameterlist count");
			ClassicAssert.AreEqual (1, cons.ParameterLists [1].Count, "wrong arg count");
			ClassicAssert.AreEqual ("Swift.Int", cons.ParameterLists [1] [0].TypeName, "wrong type");
			ClassicAssert.AreEqual ("y", cons.ParameterLists [1] [0].PublicName, "wrong name");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Does not yet include destructors https://github.com/xamarin/binding-tools-for-swift/issues/700")]
		public void TestClassHasDestructor (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { public var x:Int; public init(y:Int) { x = y; } }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			ClassDeclaration theClass = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.NotNull (theClass, "not a class");
			FunctionDeclaration dtor = theClass.Members.OfType<FunctionDeclaration> ().FirstOrDefault (s => s.Name == ".dtor");
			ClassicAssert.NotNull (dtor, "not a destructor");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Naming convention. Tries to give the tuple names https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void FuncReturningTuple (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public func returnTuple()->(Int,Float) { return (0, 3.0); }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnTuple");
			var result = func.ReturnTypeName.Replace (" ", "");
			ClassicAssert.AreEqual ("(Swift.Int,Swift.Float)", result, "wrong type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void FuncReturningDictionary (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public func returnDict()->[Int:Float] { return [Int:Float](); }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnDict");
			var returnType = func.ReturnTypeName.Replace (" ", "");
			ClassicAssert.AreEqual ("Swift.Dictionary<Swift.Int,Swift.Float>", returnType, "wrong type");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void FuncReturningIntThrows (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public enum MathError : Error {\ncase divZero\n}\n" +
								    "public func returnInt(a:Int) throws ->Int { if a < 1\n{\n throw MathError.divZero\n }\n else {\n return a\n}\n}", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnInt");
			ClassicAssert.AreEqual ("Swift.Int", func.ReturnTypeName, "wrong type");
			ClassicAssert.AreEqual (true, func.HasThrows, "doesn't throw");
		}



		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void FuncReturningIntOption (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public func returnIntOpt()->Int? { return 3; }", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			FunctionDeclaration func = module.Functions.FirstOrDefault (f => f.Name == "returnIntOpt");
			ClassicAssert.AreEqual ("Swift.Optional<Swift.Int>", func.ReturnTypeName, "wrong type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle global variables https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void GlobalBool (ReflectorMode mode)
		{
			ModuleDeclaration module = ReflectToModules ("public var aGlobal:Bool = true", "SomeModule", mode)
				.Find (m => m.Name == "SomeModule");
			ClassicAssert.NotNull (module, "not module");
			PropertyDeclaration decl = module.TopLevelProperties.FirstOrDefault (f => f.Name == "aGlobal");
			ClassicAssert.IsNotNull (decl, "no declaration");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest1 (ReflectorMode mode)
		{
			string code = "public enum foo { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			ClassicAssert.IsTrue (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest2 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(Int), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			foreach (EnumElement elem in edecl.Elements) {
				ClassicAssert.IsTrue (elem.HasType, "no type");
			}
			ClassicAssert.IsFalse (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsTrue (edecl.IsIntegral, "wrong integral");
			ClassicAssert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest3 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Int), d(Int) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			foreach (EnumElement elem in edecl.Elements) {
				ClassicAssert.IsTrue (elem.HasType, "no type");
			}
			ClassicAssert.IsFalse (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsTrue (edecl.IsIntegral, "wrong integral");
			ClassicAssert.IsFalse (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest4 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(Int), b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			ClassicAssert.IsFalse (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsTrue (edecl.IsIntegral, "wrong integral");
			ClassicAssert.IsFalse (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsFalse (edecl.HasRawType, "wrong has raw type");
		}




		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest5 (ReflectorMode mode)
		{
			string code = "public enum foo:Int { case a=1, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			ClassicAssert.IsFalse (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsTrue (edecl.IsIntegral, "wrong integral");
			ClassicAssert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsTrue (edecl.HasRawType, "wrong has raw type");
			ClassicAssert.AreEqual ("Swift.Int", edecl.RawTypeName, "wrong raw type name");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest6 (ReflectorMode mode)
		{
			string code = "public enum foo:Int { case a, b, c, d }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			ClassicAssert.IsFalse (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsTrue (edecl.IsIntegral, "wrong integral");
			ClassicAssert.IsTrue (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsTrue (edecl.HasRawType, "wrong has raw type");
			ClassicAssert.AreEqual ("Swift.Int", edecl.RawTypeName, "wrong raw type name");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumSmokeTest7 (ReflectorMode mode)
		{
			string code = "public enum foo { case a(UInt), b(Int), c(Bool), d(Float) }";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.AllEnums.Count, "wrong enums count");
			EnumDeclaration edecl = module.AllEnums.First ();
			ClassicAssert.AreEqual (edecl.Name, "foo", "wrong name");
			ClassicAssert.AreEqual (4, edecl.Elements.Count, "wrong element count");
			ClassicAssert.IsFalse (edecl.IsTrivial, "wrong triviality");
			ClassicAssert.IsFalse (edecl.IsIntegral, "wrong integral");
			ClassicAssert.IsFalse (edecl.IsHomogenous, "wrong homogeneity");
			ClassicAssert.IsFalse (edecl.HasRawType, "wrong has raw type");

			ClassicAssert.AreEqual ("Swift.UInt", edecl ["a"].TypeName, "wrong raw type name uint");
			ClassicAssert.AreEqual ("Swift.Int", edecl ["b"].TypeName, "wrong raw type name int");
			ClassicAssert.AreEqual ("Swift.Bool", edecl ["c"].TypeName, "wrong raw type name bool");
			ClassicAssert.AreEqual ("Swift.Float", edecl ["d"].TypeName, "wrong raw type name float");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void OptionalSmokeTest1 (ReflectorMode mode)
		{
			string code = "public func optInt(x:Int) -> Int? { if (x >= 0) { return x; }\nreturn nil; }\n";
			ModuleDeclaration module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TypeAliasTest (ReflectorMode mode)
		{
			string code = "public typealias Foo = OpaquePointer\n" +
				"public typealias Bar = Foo\n" +
				"public func aliased(a: Bar, b: (Bar)->()) {\n}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "aliased");
			ClassicAssert.IsNotNull (func, "no func");
			var named = func.ParameterLists [0] [0].TypeSpec as NamedTypeSpec;
			ClassicAssert.IsNotNull (named, "not a named type spec");
			ClassicAssert.AreEqual ("Swift.OpaquePointer", named.Name, "wrong name");
			var closType = func.ParameterLists [0] [1].TypeSpec as ClosureTypeSpec;
			ClassicAssert.IsNotNull (closType, "not a closure");
			named = closType.Arguments as NamedTypeSpec;
			ClassicAssert.IsNotNull (named, "not a named type spec 2");
			ClassicAssert.AreEqual ("Swift.OpaquePointer", named.Name, "wrong name 2");

		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot identify deprecations https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void DeprecatedFunction (ReflectorMode mode)
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.IsDeprecated, "deprecated");
			ClassicAssert.IsFalse (func.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot identify deprecations https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void DeprecatedClass (ReflectorMode mode)
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");
			ClassicAssert.IsTrue (cl.IsDeprecated, "not deprecated");
			ClassicAssert.IsFalse (cl.IsUnavailable, "available");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle Deprecated and Unavailable https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObsoletedFunction (ReflectorMode mode)
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsFalse (func.IsDeprecated, "deprecated");
			ClassicAssert.IsTrue (func.IsUnavailable, "unavilable");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle obsoleted or unavailable https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObsoletedClass (ReflectorMode mode)
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");
			ClassicAssert.IsFalse (cl.IsDeprecated, "deprecated");
			ClassicAssert.IsTrue (cl.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle unavailable https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void UnavailableFunction (ReflectorMode mode)
		{
			string code =
				"@available(*, unavailable)" +
				"public func foo() { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "no func");
			ClassicAssert.IsFalse (func.IsDeprecated, "deprecated");
			ClassicAssert.IsTrue (func.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle unavailable https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void UnavailableClass (ReflectorMode mode)
		{
			string code =
				"@available(*, unavailable)" +
				"public class Foo { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "class");
			ClassicAssert.IsFalse (cl.IsDeprecated, "deprecated");
			ClassicAssert.IsTrue (cl.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle deprecation https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "not a module");
			var st = module.Structs.FirstOrDefault (f => f.Name == "CommandEvaluation");
			ClassicAssert.IsNotNull (st, "no struct");
			ClassicAssert.IsFalse (st.IsDeprecated, "deprecated");
			ClassicAssert.IsFalse (st.IsUnavailable, "unavailable");
			var func = st.AllMethodsNoCDTor ().Where (fn => fn.Name == "retrieveParameter").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "no func");
			ClassicAssert.IsTrue (func.IsDeprecated, "deprecated");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle unavailable https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void UnavailableProperty (ReflectorMode mode)
		{
			string code =
				"public struct JSON {\n" +
				"    @available(*, unavailable, renamed:\"null\")\n" +
    				"    public static var nullJSON: Int { return 3 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var st = module.Structs.FirstOrDefault (f => f.Name == "JSON");
			var prop = st.AllProperties ().Where (fn => fn.Name == "nullJSON").FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no prop");
			ClassicAssert.IsTrue (prop.IsUnavailable, "unavailable");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle extensions https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ExtensionProperty (ReflectorMode mode)
		{
			string code =
				"public extension Double {\n" +
				"    public var millisecond: Double  { return self / 1000 }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			ClassicAssert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			ClassicAssert.AreEqual (2, ext.Members.Count, $"Expected 2 members but got {ext.Members.Count}");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle extensions https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ExtensionFunc (ReflectorMode mode)
		{
			string code =
				"public extension Double {\n" +
				"    public func DoubleIt() -> Double  { return self * 2; }\n" +
				"}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			ClassicAssert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			ClassicAssert.AreEqual (1, ext.Members.Count, $"Expected 1 member but got {ext.Members.Count}");
			var func = ext.Members [0] as FunctionDeclaration;
			ClassicAssert.IsNotNull (func, $"Expected a FunctionDeclaration but got {ext.Members [0].GetType ().Name}");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle extensions https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "not a module");
			ClassicAssert.AreEqual (1, module.Extensions.Count, "Expected an extension");
			var ext = module.Extensions [0];
			ClassicAssert.AreEqual ("Swift.Double", ext.ExtensionOnTypeName, $"Incorrect type name {ext.ExtensionOnTypeName}");
			ClassicAssert.AreEqual (1, ext.Members.Count, $"Expected 1 member but got {ext.Members.Count}");
			var func = ext.Members [0] as FunctionDeclaration;
			ClassicAssert.IsNotNull (func, $"Expected a FunctionDeclaration but got {ext.Members [0].GetType ().Name}");
			ClassicAssert.AreEqual (1, ext.Inheritance.Count, $"Expected 1 inheritance but had {ext.Inheritance.Count}");
			var inh = ext.Inheritance [0];
			ClassicAssert.AreEqual ("SomeModule.Printer", inh.InheritedTypeName, $"Incorrect type name {inh.InheritedTypeName}");
			ClassicAssert.AreEqual (InheritanceKind.Protocol, inh.InheritanceKind, $"Should always be protocol inheritance");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var fooFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (fooFunc, "No func named foo");
			ClassicAssert.IsTrue (fooFunc.IsOptional, "should be optional");
			var barFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "bar").FirstOrDefault ();
			ClassicAssert.IsNotNull (barFunc, "No func named bar");
			ClassicAssert.IsFalse (barFunc.IsOptional, "should not be optional");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObjCOptionalProp (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc optional var X:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto1", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (3, proto.Members.Count (), "incorrect number of members");
			var xProp = proto.Members.OfType<PropertyDeclaration> ().Where (f => f.Name == "X").FirstOrDefault ();
			ClassicAssert.IsNotNull (xProp, "No prop named X");
			ClassicAssert.IsTrue (xProp.IsOptional, "prop is not optional");
			var getter = xProp.GetGetter ();
			ClassicAssert.IsNotNull (getter, "Null getter");
			ClassicAssert.IsTrue (getter.IsOptional, "getter is not optional");
			var setter = xProp.GetSetter ();
			ClassicAssert.IsNotNull (setter, "Null setter");
			ClassicAssert.IsTrue (setter.IsOptional, "setter is not optional");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObjCOptionalSubsript (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc optional subscript (index:Int) ->Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto1", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var func0 = proto.Members [0] as FunctionDeclaration;
			ClassicAssert.IsNotNull (func0, "expected a function declaration at index 0");
			ClassicAssert.IsTrue (func0.IsOptional, "func 0 should be optional");

			var func1 = proto.Members [1] as FunctionDeclaration;
			ClassicAssert.IsNotNull (func1, "expected a function declaration at index 1");
			ClassicAssert.IsTrue (func1.IsOptional, "func 1 should be optional");
		}

		[TestCase ("open", Accessibility.Open, ReflectorMode.Parser)]
		[TestCase ("public", Accessibility.Public, ReflectorMode.Parser, Ignore = "Bug in swift compiler (maybe) see https://bugs.swift.org/browse/SR-14304")]
		[TestCase ("internal", Accessibility.Internal, ReflectorMode.Parser, Ignore = "Bug in swift compiler (maybe) see https://bugs.swift.org/browse/SR-14304")]
		[TestCase ("private", Accessibility.Private, ReflectorMode.Parser, Ignore = "This is not a public interface, parser never sees it")]
		[TestCase ("open", Accessibility.Open, ReflectorMode.DylibBinder, Ignore = "Cannot handle accessibilities other than Public https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		[TestCase ("public", Accessibility.Public, ReflectorMode.DylibBinder)]
		[TestCase ("internal", Accessibility.Internal, ReflectorMode.DylibBinder, Ignore = "Cannot handle accessibilities other than Public https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		[TestCase ("private", Accessibility.Private, ReflectorMode.DylibBinder, Ignore = "Cannot handle accessibilities other than Public https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void PropertyVisibilityCore (string swiftVisibility, Accessibility accessibility, ReflectorMode mode)
		{
			string code = $@"open class Foo {{
			open {swiftVisibility} (set) weak var parent: Foo?
}}";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.AllClasses.Count (), "Expected a class.");
			var fooClass = module.AllClasses.First ();
			ClassicAssert.AreEqual (1, fooClass.AllProperties ().Count (), "Expected one property.");
			ClassicAssert.AreEqual (accessibility, fooClass.AllProperties () [0].GetSetter ().Access, "Unexpected Visibility.");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var fooFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (fooFunc, "No func named foo");
			ClassicAssert.AreEqual ("foo", fooFunc.ObjCSelector, $"Incorrect foo selector name {fooFunc.ObjCSelector}");
			var barFunc = proto.Members.OfType<FunctionDeclaration> ().Where (f => f.Name == "bar").FirstOrDefault ();
			ClassicAssert.IsNotNull (barFunc, "No func named bar");
			ClassicAssert.AreEqual ("barWithA:", barFunc.ObjCSelector, $"Incorrect bar selector name {barFunc.ObjCSelector}");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObjCPropSelector (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc var X:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (3, proto.Members.Count (), "incorrect number of members");
			var xProp = proto.Members.OfType<PropertyDeclaration> ().Where (f => f.Name == "X").FirstOrDefault ();
			ClassicAssert.IsNotNull (xProp, "No prop named X");
			var getter = xProp.GetGetter ();
			ClassicAssert.IsNotNull (getter, "Null getter");
			ClassicAssert.AreEqual ("X", getter.ObjCSelector, $"incorrect get X selector name {getter.ObjCSelector}");
			var setter = xProp.GetSetter ();
			ClassicAssert.IsNotNull (setter, "Null setter");
			ClassicAssert.AreEqual ("setX:", setter.ObjCSelector, $"incorrect set X selector name {setter.ObjCSelector}");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObjCPropSelectorLower (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto {\n" +
				"    @objc var x:Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (3, proto.Members.Count (), "incorrect number of members");
			var xProp = proto.Members.OfType<PropertyDeclaration> ().Where (f => f.Name == "x").FirstOrDefault ();
			ClassicAssert.IsNotNull (xProp, "No prop named x");
			var getter = xProp.GetGetter ();
			ClassicAssert.IsNotNull (getter, "Null getter");
			ClassicAssert.AreEqual ("x", getter.ObjCSelector, $"incorrect get X selector name {getter.ObjCSelector}");
			var setter = xProp.GetSetter ();
			ClassicAssert.IsNotNull (setter, "Null setter");
			ClassicAssert.AreEqual ("setX:", setter.ObjCSelector, $"incorrect set X selector name {setter.ObjCSelector}");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet handle objective-c selectors https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void ObjCSubsriptSelector (ReflectorMode mode)
		{
			string code =
				"import Foundation\n" +
				"@objc\n" +
				"public protocol Proto1 {\n" +
				"    @objc subscript (index:Int) ->Int { get set }\n" +
				"}\n";

			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.Protocols.Count (), "Expected a protocol.");
			var proto = module.Protocols.First ();
			ClassicAssert.IsTrue (proto.IsObjC, "not objc protocol");
			ClassicAssert.AreEqual ("SomeModule.Proto1", proto.ToFullyQualifiedName (true), "Misnamed protocol");
			ClassicAssert.AreEqual (2, proto.Members.Count (), "incorrect number of members");
			var func0 = proto.Members [0] as FunctionDeclaration;
			ClassicAssert.IsNotNull (func0, "expected a function declaration at index 0");
			ClassicAssert.AreEqual ("objectAtIndexedSubscript:", func0.ObjCSelector, $"Incorrect selector for getter {func0.ObjCSelector}");

			var func1 = proto.Members [1] as FunctionDeclaration;
			ClassicAssert.IsNotNull (func1, "expected a function declaration at index 1");
			ClassicAssert.AreEqual ("setObject:atIndexedSubscript:", func1.ObjCSelector, $"Incorrect selector for setter {func1.ObjCSelector}");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle required https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (2, module.Classes.Count (), "Expected 2 classes");
			var baseClass = module.Classes.FirstOrDefault (cl => cl.Name == "BaseWithReq");
			ClassicAssert.IsNotNull (baseClass, "didn't find base class");
			var subClass = module.Classes.FirstOrDefault (cl => cl.Name == "SubOfBase");
			ClassicAssert.IsNotNull (subClass, "didn't find sub class");

			var baseInit = baseClass.AllConstructors ().FirstOrDefault ();
			ClassicAssert.IsNotNull (baseInit, "no constructors in base class");
			ClassicAssert.IsTrue (baseInit.IsRequired, "incorrect IsRequired on base class");

			var subInit = subClass.AllConstructors ().FirstOrDefault ();
			ClassicAssert.IsNotNull (subInit, "no constructors in sub class");
			ClassicAssert.IsTrue (subInit.IsRequired, "incorrect IsRequired on sub class");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
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
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (2, module.Classes.Count (), "Expected 2 classes");
			var baseClass = module.Classes.FirstOrDefault (cl => cl.Name == "BaseWithoutReq");
			ClassicAssert.IsNotNull (baseClass, "didn't find base class");
			var subClass = module.Classes.FirstOrDefault (cl => cl.Name == "SubOfBase");
			ClassicAssert.IsNotNull (subClass, "didn't find sub class");

			var baseInit = baseClass.AllConstructors ().FirstOrDefault ();
			ClassicAssert.IsNotNull (baseInit, "no constructors in base class");
			ClassicAssert.IsFalse (baseInit.IsRequired, "incorrect IsRequired on base class");

			var subInit = subClass.AllConstructors ().FirstOrDefault ();
			ClassicAssert.IsNotNull (subInit, "no constructors in sub class");
			ClassicAssert.IsFalse (subInit.IsRequired, "incorrect IsRequired on sub class");

		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Close, but DylibBinder cannot get every private name https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void TestPublicPrivateParamNames (ReflectorMode mode)
		{
			string code = "public func foo(seen notseen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "no function");
			ClassicAssert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			ClassicAssert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			ClassicAssert.AreEqual ("seen", func.ParameterLists [0] [0].PublicName, "wrong public name");
			ClassicAssert.AreEqual ("notseen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			ClassicAssert.IsTrue (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestOnlyPublicParamNames (ReflectorMode mode)
		{
			string code = "public func foo(seen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "no function");
			ClassicAssert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			ClassicAssert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			ClassicAssert.AreEqual ("seen", func.ParameterLists [0] [0].PublicName, "wrong public name");
			ClassicAssert.AreEqual ("seen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			ClassicAssert.IsTrue (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot always get names correct: https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void TestNotRequiredParamName (ReflectorMode mode)
		{
			string code = "public func foo(_ seen:Int) { }\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "no function");
			ClassicAssert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			ClassicAssert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			ClassicAssert.AreEqual ("", func.ParameterLists [0] [0].PublicName, "wrong public name");
			ClassicAssert.AreEqual ("seen", func.ParameterLists [0] [0].PrivateName, "wrong private name");
			ClassicAssert.IsFalse (func.ParameterLists [0] [0].NameIsRequired, "Wrong name requirement");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestSimpleVariadicFunc (ReflectorMode mode)
		{
			string code = "public func itemsAsArray (a:Int ...) -> [Int] {\n return a\n}\n";
			var module = ReflectToModules (code, "SomeModule").Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itemsAsArray");
			ClassicAssert.IsNotNull (func, "no function");
			ClassicAssert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			ClassicAssert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			ClassicAssert.IsTrue (func.ParameterLists [0] [0].IsVariadic, "Parameter item is not marked variadic");
			ClassicAssert.IsTrue (func.IsVariadic, "Func is not mared variadic");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestSimpleNotVariadicFunc (ReflectorMode mode)
		{
			string code = "public func itemsAsArray (a:Int) -> [Int] {\n return [a]\n}\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itemsAsArray");
			ClassicAssert.IsNotNull (func, "no function");
			ClassicAssert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			ClassicAssert.AreEqual (1, func.ParameterLists [0].Count, "wrong number of parameters");
			ClassicAssert.IsFalse (func.ParameterLists [0] [0].IsVariadic, "Parameter item is not marked variadic");
			ClassicAssert.IsFalse (func.IsVariadic, "Func is not mared variadic");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "itMightBeAProtocol");
			ClassicAssert.IsNotNull (func, "no function");
			var returnType = func.ReturnTypeSpec as NamedTypeSpec;
			ClassicAssert.IsNotNull (returnType, "not a named type spec");
			ClassicAssert.IsTrue (returnType.GenericParameters.Count == 1, "Expected 1 generic parameter");
			var boundType = returnType.GenericParameters [0] as NamedTypeSpec;
			ClassicAssert.IsNotNull (boundType, "wrong kind of bound type");
			ClassicAssert.AreEqual ("SomeModule.Foo", boundType.Name, "Wrong bound type name");

		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.FirstOrDefault (c => c.Name == "Container");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "itMightBeAProtocol");
			ClassicAssert.IsNotNull (prop, "no prop");
			var returnType = prop.TypeSpec as NamedTypeSpec;
			ClassicAssert.IsNotNull (returnType, "not a named type spec");
			ClassicAssert.IsTrue (returnType.GenericParameters.Count == 1, "Expected 1 generic parameter");
			var boundType = returnType.GenericParameters [0] as NamedTypeSpec;
			ClassicAssert.IsNotNull (boundType, "wrong kind of bound type");
			ClassicAssert.AreEqual ("SomeModule.Foo", boundType.Name, "Wrong bound type name");

		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Convenience ctor marked designated https://github.com/xamarin/binding-tools-for-swift/issues/701")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			foreach (var ctor in cl.AllConstructors ()) {
				var item = ctor.ParameterLists.Last ().Last ();
				if (item.PublicName == "val")
					ClassicAssert.IsFalse (ctor.IsConvenienceInit, "designated ctor marked convenience");
				else
					ClassicAssert.IsTrue (ctor.IsConvenienceInit, "convenience ctor marked designated");
			}
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Protocols not fully there yet https://github.com/xamarin/binding-tools-for-swift/issues/698")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (fn => fn.Name == "joe");
			ClassicAssert.IsNotNull (func, "no func");

			var parm = func.ParameterLists [0] [0];
			var protoList = parm.TypeSpec as ProtocolListTypeSpec;
			ClassicAssert.IsNotNull (protoList, "not a proto list");
			ClassicAssert.AreEqual (2, protoList.Protocols.Count, "wrong protocol list count");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestFuncReturningAny (ReflectorMode mode)
		{
			var code = @"
public func returnsAny() -> Any {
    return 7
}";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.FirstOrDefault (fn => fn.Name == "returnsAny");
			ClassicAssert.IsNotNull (func, "no func");

			var returnType = func.ReturnTypeSpec as NamedTypeSpec;
			ClassicAssert.IsNotNull (returnType, "no return type");
			ClassicAssert.AreEqual ("Swift.Any", returnType.Name, $"Wrong type: {returnType.Name}");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void AssocTypeSmoke (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing
	func getThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			ClassicAssert.IsNotNull (protocol, "no protocol");
			ClassicAssert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			ClassicAssert.AreEqual ("Thing", assoc.Name, "wrong name");
			ClassicAssert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			ClassicAssert.IsNull (assoc.SuperClass, "non-null superclass");
			ClassicAssert.IsNull (assoc.DefaultType, "non-null default type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Order of associated types is off https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			ClassicAssert.IsNotNull (protocol, "no protocol");
			ClassicAssert.AreEqual (2, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			ClassicAssert.AreEqual ("ThingOne", assoc.Name, "wrong name");
			assoc = protocol.AssociatedTypes [1];
			ClassicAssert.AreEqual ("ThingTwo", assoc.Name, "wrong name");
			ClassicAssert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			ClassicAssert.IsNull (assoc.SuperClass, "non-null superclass");
			ClassicAssert.IsNull (assoc.DefaultType, "non-null default type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet get types for associated types https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void AssocTypeDefaultType (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing = Int
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			ClassicAssert.IsNotNull (protocol, "no protocol");
			ClassicAssert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			ClassicAssert.AreEqual ("Thing", assoc.Name, "wrong name");
			ClassicAssert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			ClassicAssert.IsNull (assoc.SuperClass, "non-null superclass");
			ClassicAssert.IsNotNull (assoc.DefaultType, "null default type");
			ClassicAssert.AreEqual ("Swift.Int", assoc.DefaultType.ToString (), "wrong type");
		}

		[TestCase (ReflectorMode.Parser, Ignore = "Don't have IteratorProtocol in type database")]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle inheritance for associatedtype https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void AssocTypeConformance (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing : IteratorProtocol
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			ClassicAssert.IsNotNull (protocol, "no protocol");
			ClassicAssert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			ClassicAssert.AreEqual ("Thing", assoc.Name, "wrong name");
			ClassicAssert.AreEqual (1, assoc.ConformingProtocols.Count, "wrong number of conf");
			ClassicAssert.AreEqual ("Swift.IteratorProtocol", assoc.ConformingProtocols [0].ToString (), "wrong protocol name");
			ClassicAssert.IsNull (assoc.SuperClass, "non-null superclass");
			ClassicAssert.IsNull (assoc.DefaultType, "non-null default type");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot yet get inheritance in associated types https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			ClassicAssert.IsNotNull (protocol, "no protocol");
			ClassicAssert.AreEqual (1, protocol.AssociatedTypes.Count, "no associated types");
			var assoc = protocol.AssociatedTypes [0];
			ClassicAssert.AreEqual ("Thing", assoc.Name, "wrong name");
			ClassicAssert.AreEqual (0, assoc.ConformingProtocols.Count, "wrong number of conf");
			ClassicAssert.IsNotNull (assoc.SuperClass, "null superclass");
			ClassicAssert.AreEqual ("SomeModule.Foo", assoc.SuperClass.ToString (), "wrong superclass name");
			ClassicAssert.IsNull (assoc.DefaultType, "non-null default type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void FindsAssocTypeByName (ReflectorMode mode)
		{
			var code = @"
public protocol HoldsThing {
	associatedtype Thing
	func doThing() -> Thing
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var protocol = module.Protocols.Where (p => p.Name == "HoldsThing").FirstOrDefault ();
			ClassicAssert.IsNotNull (protocol, "no protocol");
			ClassicAssert.IsTrue (protocol.HasAssociatedTypes, "no associated types?!");
			var assoc = protocol.AssociatedTypeNamed ("Thing");
			ClassicAssert.IsNotNull (assoc, "couldn't find associated type");
			assoc = protocol.AssociatedTypeNamed ("ThroatwarblerMangrove");
			ClassicAssert.IsNull (assoc, "Found a non-existent associated type");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestTLFuncNoArgsNoReturnOutput (ReflectorMode mode)
		{
			var code = @"public func SomeFunc () { }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc () -> ()", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestTLFuncNoArgsReturnsIntOutput (ReflectorMode mode)
		{
			var code = @"public func SomeFunc () -> Int { return 3; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc () -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestTLFuncNoArgsReturnsIntThrowsOutput (ReflectorMode mode)
		{
			var code = @"public func SomeFunc () throws -> Int { return 3; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc () throws -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestTLFuncOneArgSamePubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc (a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Public and private names are not always accurate. If they do not differ, they will be written as just one name. https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void TestTLFuncOneArgDiffPubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (b a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc (b a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Public and private names are not always accurate https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void TestTLFuncOneArgNoPubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (_ a: Int) -> Int { return a; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc (_ a: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestTLFuncTwoArgSamePubPrivReturnsInt (ReflectorMode mode)
		{
			var code = @"public func SomeFunc (a: Int, b: Int) -> Int { return a + b; }";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.TopLevelFunctions.Where (f => f.Name == "SomeFunc").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.SomeFunc (a: Swift.Int, b: Swift.Int) -> Swift.Int", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestPropGetFunc (ReflectorMode mode)
		{
			var code = @"
public class Foo {
	public init () { }
	public var prop: Int = 0
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "get_prop").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public var SomeModule.Foo.prop: Swift.Int { get }", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void TestPropGetSet (ReflectorMode mode)
		{
			var code = @"
public class Foo {
	public init () { }
	public var prop: Int = 0
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			var func = cl.AllProperties ().Where (p => p.Name == "prop").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public var SomeModule.Foo.prop: Swift.Int { get set }", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "SomeModule.Foo.Type is 'inout SomeModule.Foo.Type' instead of 'SomeModule.Foo.Type' https://github.com/xamarin/binding-tools-for-swift/issues/699")]
		public void TestCtorType (ReflectorMode mode)
		{
			var code = @"
public class Foo {
	public init () { }
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			var ctor = cl.AllConstructors ().FirstOrDefault ();
			ClassicAssert.IsNotNull (ctor, "no constructor");
			var pl = ctor.ParameterLists [0];
			ClassicAssert.AreEqual (1, pl.Count, "wrong number of parameters");
			var uncurriedType = pl [0].TypeName;
			ClassicAssert.AreEqual ("SomeModule.Foo.Type", uncurriedType, "wrong type");
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle subscripts https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "get_subscript").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "null func");
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.Foo.subscript [_ Index: Swift.Int] -> Swift.String { get }", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Issue with the generic parameters https://github.com/xamarin/binding-tools-for-swift/issues/702")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var cl = module.Classes.Where (c => c.Name == "Foo").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			var func = cl.AllMethodsNoCDTor ().Where (p => p.Name == "printIt").FirstOrDefault ();
			var output = func.ToString ();
			ClassicAssert.AreEqual ("Public SomeModule.Foo.printIt<T, U> (a: U) -> ()", output, "wrong signature");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Self not yet supported in protocols https://github.com/xamarin/binding-tools-for-swift/issues/698")]
		public void DetectsSelfEasy (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> Self
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			ClassicAssert.IsNotNull (proto, "no protocol");
			ClassicAssert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Self not yet supported in protocols https://github.com/xamarin/binding-tools-for-swift/issues/698")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			ClassicAssert.IsNotNull (proto, "no protocol");
			ClassicAssert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Self not yet supported in protocols https://github.com/xamarin/binding-tools-for-swift/issues/698")]
		public void DetectsSelfInTuple (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> (Int, Bool, Self)
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			ClassicAssert.IsNotNull (proto, "no protocol");
			ClassicAssert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Self not yet supported in protocols https://github.com/xamarin/binding-tools-for-swift/issues/698")]
		public void DetectsSelfInOptional (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> Self?
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			ClassicAssert.IsNotNull (proto, "no protocol");
			ClassicAssert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Self not yet supported in protocols https://github.com/xamarin/binding-tools-for-swift/issues/698")]
		public void DetectsSelfInBoundGeneric (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami() -> UnsafeMutablePointer<Self>
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			ClassicAssert.IsNotNull (proto, "no protocol");
			ClassicAssert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Self not yet supported in protocols https://github.com/xamarin/binding-tools-for-swift/issues/698")]
		public void DetectsSelfInClosure (ReflectorMode mode)
		{
			var code = @"
public protocol Simple {
	func whoami(a: (Self)->()) -> ()
}
";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var proto = module.Protocols.Where (p => p.Name == "Simple").FirstOrDefault ();
			ClassicAssert.IsNotNull (proto, "no protocol");
			ClassicAssert.IsTrue (proto.HasDynamicSelf, "no dynamic self");
		}

		[TestCase (ReflectorMode.Parser, Ignore = "not coming through as a let - apple's bug, not mine: https://bugs.swift.org/browse/SR-13790")]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "cannot handle lets https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void TopLevelLet (ReflectorMode mode)
		{
			var code = "public let myVar:Int = 42";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var prop = module.Properties.Where (p => p.Name == "myVar").FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no prop");
			ClassicAssert.IsTrue (prop.IsLet, "not a let");
			ClassicAssert.IsNull (prop.GetSetter (), "why is there a setter");
		}


		[TestCase (ReflectorMode.Parser, Ignore = "not coming through as a let")]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "cannot handle lets https://github.com/xamarin/binding-tools-for-swift/issues/697")]
		public void TheEpsilonIssue (ReflectorMode mode)
		{
			string code = "public let 𝑒 = 2.718\n";
			var module = ReflectToModules (code, "SomeModule", mode).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var prop = module.Properties.Where (p => p.Name == "𝑒").FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no prop");
			ClassicAssert.IsTrue (prop.IsLet, "not a let");
			ClassicAssert.IsNull (prop.GetSetter (), "why is there a setter");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle operators https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var opDecl = module.Operators.Where (op => op.Name == "*^*").FirstOrDefault ();
			ClassicAssert.AreEqual ("*^*", opDecl.Name, "name mismatch");
			ClassicAssert.AreEqual ("AdditionPrecedence", opDecl.PrecedenceGroup, "predence group mismatch");
			ClassicAssert.AreEqual (OperatorType.Infix, opDecl.OperatorType, "operator type mismatch");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle operators https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var opDecl = module.Operators.Where (op => op.Name == "*^^*").FirstOrDefault ();
			ClassicAssert.AreEqual ("*^^*", opDecl.Name, "name mismatch");
			ClassicAssert.IsNull (opDecl.PrecedenceGroup, "predence group mismatch");
			ClassicAssert.AreEqual (OperatorType.Prefix, opDecl.OperatorType, "operator type mismatch");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Cannot handle operators https://github.com/xamarin/binding-tools-for-swift/issues/697")]
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
			ClassicAssert.IsNotNull (module, "module is null");
			var opDecl = module.Operators.Where (op => op.Name == "*^&^*").FirstOrDefault ();
			ClassicAssert.AreEqual ("*^&^*", opDecl.Name, "name mismatch");
			ClassicAssert.IsNull (opDecl.PrecedenceGroup, "predence group mismatch");
			ClassicAssert.AreEqual (OperatorType.Postfix, opDecl.OperatorType, "operator type mismatch");
		}

		[Test]
		public void TypeAliasSmokeTest ()
		{
			var code = @"
public typealias Foo = Int
public func sum (a: Foo, b: Foo) -> Foo {
    return a + b
}
";
			var module = ReflectToModules (code, "SomeModule", ReflectorMode.Parser).FirstOrDefault (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			ClassicAssert.AreEqual (1, module.TypeAliases.Count, "wrong number of typealiases");
			var alias = module.TypeAliases [0];
			ClassicAssert.AreEqual (Accessibility.Public, alias.Access, "wrong access");
			ClassicAssert.AreEqual ("SomeModule.Foo", alias.TypeName, "wrong typealias name");
			ClassicAssert.AreEqual ("Swift.Int", alias.TargetTypeName, "wrong typealias target");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void UnwrappedOptionalTest (ReflectorMode mode)
		{
			var code = @"
public func sum (a: Int!, b: Int!) -> Int! {
	return a + b
}

";
			var module = ReflectToModules (code, "SomeModule", mode).FirstOrDefault (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var func = module.Functions.Where (fn => fn.Name == "sum").FirstOrDefault ();
			ClassicAssert.IsNotNull (func, "no func");
			ClassicAssert.AreEqual ("Swift.Optional<Swift.Int>", func.ReturnTypeName);
		}


		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder, Ignore = "Enum elements are not yet able to be found, https://github.com/xamarin/binding-tools-for-swift/issues/625")]
		public void EnumProtocolConformance (ReflectorMode mode)
		{
			var code = @"
public enum E : Error {
case a, b
}
";
			var module = ReflectToModules (code, "SomeModule", mode).FirstOrDefault (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");
			var en = module.Enums.FirstOrDefault (e => e.Name == "E");
			ClassicAssert.IsNotNull (en, "no enum");
			ClassicAssert.AreEqual (1, en.Inheritance.Count, "wrong inheritance count");
			ClassicAssert.AreEqual ("Swift.Error", en.Inheritance [0].InheritedTypeName, "wrong inherited name");
		}

		[TestCase (ReflectorMode.Parser)]
		[TestCase (ReflectorMode.DylibBinder)]
		public void InlineFunction (ReflectorMode mode)
		{
			var code = @"
@inlinable
@inline (__always)
public func sum (a:Int, b:Int) -> Int {
return a + b
}
";
			var module = ReflectToModules (code, "SomeModule", mode).FirstOrDefault (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module is null");

			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "sum");
			ClassicAssert.IsNotNull (fn, "no function");
		}

		[TestCase (ReflectorMode.Parser)]
		public void InitializedProperty (ReflectorMode mode)
		{
			var code = @"
@frozen
public struct Foo {
    public var X:Int = 17
}
";
			var module = ReflectToModules (code, "SomeModule", mode).FirstOrDefault (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "no module");
			var cl = module.Structs.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "X");
			ClassicAssert.IsNotNull (prop, "no prop");
		}
	}
}
