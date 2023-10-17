// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using SwiftReflector.SwiftXmlReflection;
using System.Collections.Generic;
using SwiftReflector;
using tomwiftytest;
using System.Linq;
using SwiftReflector.TypeMapping;
using System.IO;
using System.Text;
using System.Xml.Linq;
using SwiftReflector.SwiftInterfaceReflector;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class GenerativeTests {
		static TypeDatabase typeDatabase;

		static GenerativeTests ()
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


		List<ModuleDeclaration> ReflectToModules (string code, string moduleName)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: moduleName);
			return ParserToModule (compiler.DirectoryPath, moduleName);
		}

		[Test]
		public void TestRootedAndUnrooted ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { } ", "SomeModule").Find (m => m.Name == "SomeModule");
			ClassDeclaration fooClass = module.AllClasses.Where (cl => cl.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (fooClass);
			Assert.IsFalse (fooClass.IsUnrooted);
			ClassDeclaration unrootedFoo = fooClass.MakeUnrooted () as ClassDeclaration;
			Assert.IsNotNull (unrootedFoo);
			Assert.IsTrue (unrootedFoo != fooClass);
			Assert.AreEqual (unrootedFoo.Name, fooClass.Name);
			Assert.AreEqual (unrootedFoo.Access, fooClass.Access);
			ClassDeclaration doubleUnrootedFoo = unrootedFoo.MakeUnrooted () as ClassDeclaration;
			Assert.IsNotNull (doubleUnrootedFoo);
			Assert.IsTrue (doubleUnrootedFoo == unrootedFoo);
		}

		[Test]
		public void RoundTripClass ()
		{
			ModuleDeclaration module = ReflectToModules ("public class Foo { } ",
								     "SomeModule").Find (m => m.Name == "SomeModule");
			ClassDeclaration fooClass = module.AllClasses.Where (cl => cl.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (fooClass);
			ClassDeclaration unrootedFoo = fooClass.MakeUnrooted () as ClassDeclaration;

			Entity entity = new Entity {
				SharpNamespace = "SomeModule",
				SharpTypeName = "Foo",
				Type = unrootedFoo
			};

			TypeDatabase db = new TypeDatabase ();
			db.Add (entity);

			MemoryStream ostm = new MemoryStream ();
			db.Write (ostm, "SomeModule");
			ostm.Seek (0, SeekOrigin.Begin);

			TypeDatabase dbread = new TypeDatabase ();
			var errors = dbread.Read (ostm);
			Utils.CheckErrors (errors);
			Entity entityRead = dbread.EntityForSwiftName ("SomeModule.Foo");
			Assert.IsNotNull (entityRead);
			Assert.AreEqual (entity.SharpNamespace, entityRead.SharpNamespace);
			Assert.AreEqual (entity.SharpTypeName, entityRead.SharpTypeName);
			Assert.IsTrue (entity.Type is ClassDeclaration);
		}

		[Test]
		public void RoundTripStruct ()
		{
			ModuleDeclaration module = ReflectToModules ("public struct Foo {\npublic var x:Int\n } ", "SomeModule").Find (m => m.Name == "SomeModule");
			StructDeclaration fooClass = module.AllStructs.Where (cl => cl.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (fooClass);
			StructDeclaration unrootedFoo = fooClass.MakeUnrooted () as StructDeclaration;

			Entity entity = new Entity {
				SharpNamespace = "SomeModule",
				SharpTypeName = "Foo",
				Type = unrootedFoo
			};

			TypeDatabase db = new TypeDatabase ();
			db.Add (entity);

			MemoryStream ostm = new MemoryStream ();
			db.Write (ostm, "SomeModule");
			ostm.Seek (0, SeekOrigin.Begin);

			TypeDatabase dbread = new TypeDatabase ();
			var errors = dbread.Read (ostm);
			Utils.CheckErrors (errors);
			Entity entityRead = dbread.EntityForSwiftName ("SomeModule.Foo");
			Assert.IsNotNull (entityRead);
			Assert.AreEqual (entity.SharpNamespace, entityRead.SharpNamespace);
			Assert.AreEqual (entity.SharpTypeName, entityRead.SharpTypeName);
			Assert.IsTrue (entity.Type is StructDeclaration);
		}

		[Test]
		public void RoundTripEnum ()
		{
			ModuleDeclaration module = ReflectToModules ("public enum Foo {\ncase a, b, c\n } ", "SomeModule").Find (m => m.Name == "SomeModule");
			EnumDeclaration fooClass = module.AllEnums.Where (cl => cl.Name == "Foo").FirstOrDefault ();
			Assert.IsNotNull (fooClass);
			EnumDeclaration unrootedFoo = fooClass.MakeUnrooted () as EnumDeclaration;

			Entity entity = new Entity {
				SharpNamespace = "SomeModule",
				SharpTypeName = "Foo",
				Type = unrootedFoo
			};

			TypeDatabase db = new TypeDatabase ();
			db.Add (entity);

			MemoryStream ostm = new MemoryStream ();
			db.Write (ostm, "SomeModule");
			ostm.Seek (0, SeekOrigin.Begin);

			TypeDatabase dbread = new TypeDatabase ();
			var errors = dbread.Read (ostm);
			Utils.CheckErrors (errors);
			Entity entityRead = dbread.EntityForSwiftName ("SomeModule.Foo");
			Assert.IsNotNull (entityRead);
			Assert.AreEqual (entity.SharpNamespace, entityRead.SharpNamespace);
			Assert.AreEqual (entity.SharpTypeName, entityRead.SharpTypeName);
			Assert.IsTrue (entity.Type is EnumDeclaration);
		}


		[Test]
		public void ContainsEscapingAttribute ()
		{
			var module = ReflectToModules (
				"public func SomeFunction(a: @escaping ()->()) {\n a() \n}\n", "SomeModule").Find (m => m.Name == "SomeModule");
			var func = module.AllTypesAndTopLevelDeclarations.OfType<FunctionDeclaration> ().FirstOrDefault ();
			Assert.IsNotNull (func);

			Assert.AreEqual (1, func.ParameterLists.Count);
			Assert.AreEqual (1, func.ParameterLists [0].Count);

			var paramItem = func.ParameterLists [0] [0];
			var spec = paramItem.TypeSpec;
			Assert.AreEqual (1, spec.Attributes.Count ());
			Assert.AreEqual ("escaping", spec.Attributes [0].Name);
		}


		[Test]
		public void NoEscapingAttribute ()
		{
			var module = ReflectToModules (
				"public func SomeFunction(a: ()->()) {\n a() \n}\n", "SomeModule").Find (m => m.Name == "SomeModule");
			var func = module.AllTypesAndTopLevelDeclarations.OfType<FunctionDeclaration> ().FirstOrDefault ();
			Assert.IsNotNull (func);

			Assert.AreEqual (1, func.ParameterLists.Count);
			Assert.AreEqual (1, func.ParameterLists [0].Count);

			var paramItem = func.ParameterLists [0] [0];
			var spec = paramItem.TypeSpec;
			Assert.AreEqual (0, spec.Attributes.Count ());
		}

		[Test]
		public void HasAnyAttribute ()
		{
			var module = ReflectToModules (
				"public func SomeFunction (a: any Equatable) { }\n", "SomeModule").FirstOrDefault ();
			Assert.IsNotNull (module, "no module");
			var func = module.AllTypesAndTopLevelDeclarations.OfType<FunctionDeclaration> ().FirstOrDefault ();
			Assert.IsNotNull (func, "no function");
			Assert.AreEqual (1, func.ParameterLists.Count, "wrong number of parameter lists");
			Assert.AreEqual (1, func.ParameterLists [0].Count, "wong number of parameters");

			var paramItem = func.ParameterLists [0] [0];
			var spec = paramItem.TypeSpec;
			Assert.IsTrue (spec.IsAny, "didn't find 'any'");
		}
	}
}

