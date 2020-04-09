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
using SwiftReflector.Demangling;
using SwiftReflector.Inventory;
using SwiftReflector.TypeMapping;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class XmlToTLFMappingTests {

		void CanFindThing (string code, Func<FunctionDeclaration, bool> funcFinder,
			Func<TLFunction, bool> tlVerifier)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: "CanFind");

			var errors = new ErrorHandling ();
			ModuleInventory mi = ModuleInventory.FromFile (Path.Combine (compiler.DirectoryPath, "libCanFind.dylib"), errors);
			ModuleDeclaration mod = compiler.ReflectToModules (new string [] { compiler.DirectoryPath },
				new string [] { compiler.DirectoryPath }, null, "CanFind") [0];

			FunctionDeclaration funcDecl = mod.Functions.FirstOrDefault (funcFinder);
			Assert.IsNotNull (funcDecl, "no function found");

			// note: if you get an NRE from here then you are testing an argument that includes
			// an associated type path from a generic. Don't do that. You need much more infrastructure to
			// do that than you really want here.
			TLFunction func = XmlToTLFunctionMapper.ToTLFunction (funcDecl, mi, null);
			Assert.IsNotNull (func, $"failed to find TLFunction for {funcDecl.Name}");
			Assert.IsTrue (tlVerifier (func), "verifier failed");
		}

		[Test]
		public void CanFindVoidVoid ()
		{
			string code = "public func foo() { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}


		[Test]
		public void CanFindIntVoid ()
		{
			string code = "public func foo(x:Int) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}


		[Test]
		public void CanFindUIntVoid ()
		{
			string code = "public func foo(x:UInt) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindBoolVoid ()
		{
			string code = "public func foo(x:Bool) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindFloatVoid ()
		{
			string code = "public func foo(x:Float) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindDoubleVoid ()
		{
			string code = "public func foo(x:Double) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}


		[Test]
		public void CanFindStringVoid ()
		{
			string code = "public func foo(x:String) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntIntVoid ()
		{
			string code = "public func foo(x:Int, y:Int) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntUIntVoid ()
		{
			string code = "public func foo(x:Int, y:UInt) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntBoolVoid ()
		{
			string code = "public func foo(x:Int, y:Bool) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntFloatVoid ()
		{
			string code = "public func foo(x:Int, y:Float) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntDoubleVoid ()
		{
			string code = "public func foo(x:Int, y:Double) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntStringVoid ()
		{
			string code = "public func foo(x:Int, y:String) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindClassVoid ()
		{
			string code = "public class Bar { }\npublic func foo(x:Bar) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindClasClassVoid ()
		{
			string code = "public class Bar { }\npublic func foo(x:Bar, y:Bar) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}
		[Test]
		public void CanFindClasClassVoid1 ()
		{
			string code = "public class Bar { }\npublic class Baz { }\npublic func foo(x:Bar, y:Baz) { }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindIntVoidOverload ()
		{
			string code = "public func foo() { }\npublic func foo(x:Int) { }";
			CanFindThing (code, f => f.Name == "foo" && f.ParameterLists [0].Count == 1, f => f.Name.Name == "foo" && SingleTypeFromMaybeTuple (f.Signature.Parameters) is SwiftBuiltInType);
		}

		[Test]
		public void CanFindIntVoidOverload1 ()
		{
			string code = "public func foo(x:Float) { }\npublic func foo(x:Int) { }";
			CanFindThing (code, f => f.Name == "foo" && f.ParameterLists [0].Count == 1 &&
				f.ParameterLists [0] [0].TypeName != "Swift.Float", f =>
				  f.Name.Name == "foo" && SingleTypeFromMaybeTuple (f.Signature.Parameters) is SwiftBuiltInType &&
				  ((SwiftBuiltInType)SingleTypeFromMaybeTuple (f.Signature.Parameters)).BuiltInType == CoreBuiltInType.Int);
		}

		[Test]
		public void CanFindUIntVoidOverload1 ()
		{
			string code = "public func foo(x:Float) { }\npublic func foo(x:UInt) { }";
			CanFindThing (code, f => f.Name == "foo" && f.ParameterLists [0].Count == 1 &&
				f.ParameterLists [0] [0].TypeName != "Swift.Float", f =>
				  f.Name.Name == "foo" && SingleTypeFromMaybeTuple (f.Signature.Parameters) is SwiftBuiltInType &&
				  ((SwiftBuiltInType)SingleTypeFromMaybeTuple (f.Signature.Parameters)).BuiltInType == CoreBuiltInType.UInt);
		}

		public SwiftType SingleTypeFromMaybeTuple (SwiftType t)
		{
			SwiftTupleType tuple = t as SwiftTupleType;
			if (tuple != null) {
				Assert.AreEqual (1, tuple.Contents.Count, $"Item expected to be a single tuple element but as {tuple.Contents.Count}.");
				return tuple.Contents [0];
			} else {
				return t;
			}
		}

		[Test]
		public void CanFindBoolVoidOverload1 ()
		{
			string code = "public func foo(x:Float) { }\npublic func foo(x:Bool) { }";
			CanFindThing (code, f => f.Name == "foo" && f.ParameterLists [0].Count == 1 &&
				f.ParameterLists [0] [0].TypeName != "Swift.Float", f =>
					f.Name.Name == "foo" && SingleTypeFromMaybeTuple (f.Signature.Parameters) is SwiftBuiltInType &&
				  ((SwiftBuiltInType)SingleTypeFromMaybeTuple (f.Signature.Parameters)).BuiltInType == CoreBuiltInType.Bool);
		}

		[Test]
		public void CanFindDoubleVoidOverload1 ()
		{
			string code = "public func foo(x:Float) { }\npublic func foo(x:Double) { }";
			CanFindThing (code, f => f.Name == "foo" && f.ParameterLists [0].Count == 1 &&
				f.ParameterLists [0] [0].TypeName != "Swift.Float", f =>
				  f.Name.Name == "foo" && SingleTypeFromMaybeTuple (f.Signature.Parameters) is SwiftBuiltInType &&
					((SwiftBuiltInType)SingleTypeFromMaybeTuple (f.Signature.Parameters)).BuiltInType == CoreBuiltInType.Double);
		}


		[Test]
		public void CanFindReturnsInt ()
		{
			string code = "public func foo() -> Int { return 0; }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}


		[Test]
		public void CanFindReturnsUInt ()
		{
			string code = "public func foo() -> UInt { return 0; }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindReturnsBool ()
		{
			string code = "public func foo() -> Bool { return false; }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindReturnsFloat ()
		{
			string code = "public func foo() -> Float { return 0; }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindReturnsDouble ()
		{
			string code = "public func foo() -> Double { return 0; }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindReturnsClass ()
		{
			string code = "public class Bar { }\npublic func foo() -> Bar { return Bar(); }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindClassReturnsClass ()
		{
			string code = "public class Bar { }\npublic func foo(x:Bar) -> Bar { return x; }";
			CanFindThing (code, f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		void CanFindThing (string code, Func<ClassDeclaration, bool> classFinder,
			Func<FunctionDeclaration, bool> funcFinder,
			Func<TLFunction, bool> tlVerifier)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: "CanFind");

			var errors = new ErrorHandling ();
			ModuleInventory mi = ModuleInventory.FromFile (Path.Combine (compiler.DirectoryPath, "libCanFind.dylib"), errors);
			ModuleDeclaration mod = compiler.ReflectToModules (new string [] { compiler.DirectoryPath },
				new string [] { compiler.DirectoryPath }, null, "CanFind") [0];

			ClassDeclaration classDecl = mod.AllClasses.FirstOrDefault (classFinder);
			Assert.IsNotNull (classDecl, "nominal type not found");

			FunctionDeclaration funcDecl = classDecl.AllMethodsNoCDTor ().FirstOrDefault (funcFinder);
			Assert.IsNotNull (funcDecl, "func decl not found");

			// see the note in the implementation of CanFindThing above
			TLFunction func = XmlToTLFunctionMapper.ToTLFunction (funcDecl, mi, null);
			Assert.IsNotNull (func, "TLFunction not found");
			Assert.IsTrue (tlVerifier (func), "verifier failed");
		}

		[Test]
		public void CanFindMethodVoidVoid ()
		{
			string code = "public class Bar {\n public func foo() { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}


		[Test]
		public void CanFindMethodIntVoid ()
		{
			string code = "public class Bar {\n public func foo(x:Int) { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodUIntVoid ()
		{
			string code = "public class Bar {\n public func foo(x:UInt) { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodBoolVoid ()
		{
			string code = "public class Bar {\n public func foo(x:Bool) { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodFloatVoid ()
		{
			string code = "public class Bar {\n public func foo(x:Float) { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodDoubleVoid ()
		{
			string code = "public class Bar {\n public func foo(x:Double) { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringVoid ()
		{
			string code = "public class Bar {\n public func foo(x:String) { }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringInt ()
		{
			string code = "public class Bar {\n public func foo(x:String) -> Int { return 0; }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringUInt ()
		{
			string code = "public class Bar {\n public func foo(x:String) -> UInt { return 0; }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringBool ()
		{
			string code = "public class Bar {\n public func foo(x:String) -> Bool { return false; }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringFloat ()
		{
			string code = "public class Bar {\n public func foo(x:String) -> Int { return 0; }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringDouble ()
		{
			string code = "public class Bar {\n public func foo(x:String) -> Int { return 0; }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}

		[Test]
		public void CanFindMethodStringString ()
		{
			string code = "public class Bar {\n public func foo(x:String) -> String { return \"\"; }\n}";

			CanFindThing (code, f => f.Name == "Bar", f => f.Name == "foo", f => f.Name.Name == "foo");
		}


		[Test]
		public void FindsPropertyGetterAndSetterFuncs ()
		{
			string code = "public class Bar { public var x:Int = 0; }";

			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: "CanFind");

			ModuleDeclaration mod = compiler.ReflectToModules (new string [] { compiler.DirectoryPath },
			new string [] { compiler.DirectoryPath }, null, "CanFind") [0];

			ClassDeclaration classDecl = mod.AllClasses.FirstOrDefault (cl => cl.Name == "Bar");
			Assert.IsNotNull (classDecl);

			PropertyDeclaration propDecl = classDecl.Members.OfType<PropertyDeclaration> ().FirstOrDefault (p => p.Name == "x");
			Assert.IsNotNull (propDecl);

			FunctionDeclaration getter = propDecl.GetGetter ();
			Assert.IsNotNull (getter);

			FunctionDeclaration setter = propDecl.GetSetter ();
			Assert.IsNotNull (setter);
		}


		void FindsProperty (string code, Func<ClassDeclaration, bool> classFinder,
			Func<PropertyDeclaration, bool> propFinder)
		{
			CustomSwiftCompiler compiler = Utils.CompileSwift (code, moduleName: "CanFind");

			var errors = new ErrorHandling ();
			ModuleInventory mi = ModuleInventory.FromFile (Path.Combine (compiler.DirectoryPath, "libCanFind.dylib"), errors);
			ModuleDeclaration mod = compiler.ReflectToModules (new string [] { compiler.DirectoryPath },
				new string [] { compiler.DirectoryPath }, null, "CanFind") [0];

			ClassDeclaration classDecl = mod.AllClasses.FirstOrDefault (classFinder);
			Assert.IsNotNull (classDecl, "null class");

			PropertyDeclaration propDecl = classDecl.Members.OfType<PropertyDeclaration> ().FirstOrDefault (propFinder);
			Assert.IsNotNull (propDecl, "null property");

			FunctionDeclaration getter = propDecl.GetGetter ();
			Assert.IsNotNull (getter, "null getter");

			FunctionDeclaration setter = propDecl.GetSetter ();
			Assert.IsNotNull (setter, "null setter");

			TLFunction tlgetter = XmlToTLFunctionMapper.ToTLFunction (getter, mi, null);
			Assert.IsNotNull (tlgetter, "null tlgetter");

			TLFunction tlsetter = XmlToTLFunctionMapper.ToTLFunction (setter, mi, null);
			Assert.IsNotNull (tlsetter, "null tlsetter");
		}

		[Test]
		public void CanFindIntProperty ()
		{
			string code = "public class Bar { public var x:Int = 0; }";
			FindsProperty (code, cl => cl.Name == "Bar", p => p.Name == "x");
		}

		[Test]
		public void CanFindUIntProperty ()
		{
			string code = "public class Bar { public var x:UInt = 0; }";
			FindsProperty (code, cl => cl.Name == "Bar", p => p.Name == "x");
		}

		[Test]
		public void CanFindBoolProperty ()
		{
			string code = "public class Bar { public var x:Bool = false; }";
			FindsProperty (code, cl => cl.Name == "Bar", p => p.Name == "x");
		}

		[Test]
		public void CanFindFloatProperty ()
		{
			string code = "public class Bar { public var x:Float = 0; }";
			FindsProperty (code, cl => cl.Name == "Bar", p => p.Name == "x");
		}

		[Test]
		public void CanFindDoubleProperty ()
		{
			string code = "public class Bar { public var x:Double = 0; }";
			FindsProperty (code, cl => cl.Name == "Bar", p => p.Name == "x");
		}

		[Test]
		public void CanFindStringProperty ()
		{
			string code = "public class Bar { public var x:String = \"\"; }";
			FindsProperty (code, cl => cl.Name == "Bar", p => p.Name == "x");
		}


		[Test]
		public void CanFindVariadic ()
		{
			string code = "public func itemsAsArray (a:Int ...) -> [Int] {\n return a\n}\n";
			CanFindThing (code, f => f.Name == "itemsAsArray", f => 
			              f.Signature.ParameterCount == 1 && f.Signature.GetParameter (0).IsVariadic);
		}

	}
}
