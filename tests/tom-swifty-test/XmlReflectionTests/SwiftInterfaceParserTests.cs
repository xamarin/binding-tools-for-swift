// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;
using System.Linq;
using SwiftReflector;
using SwiftReflector.SwiftInterfaceReflector;
using SwiftReflector.TypeMapping;
using tomwiftytest;

namespace XmlReflectionTests {
	[TestFixture]
	public class SwiftInterfaceParserTests {

		static TypeDatabase typeDatabase;

		static SwiftInterfaceParserTests ()
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

		XDocument ReflectToXDocument (string code, string moduleName, out SwiftInterfaceReflector reflector)
		{
			var compiler = Utils.CompileSwift (code, moduleName: moduleName);
			var files = Directory.GetFiles (compiler.DirectoryPath);
			var file = files.FirstOrDefault (name => name.EndsWith (".swiftinterface", StringComparison.Ordinal));
			if (file == null)
				Assert.Fail ("no swiftinterface file");
			reflector = new SwiftInterfaceReflector (typeDatabase, new NoLoadLoader ());
			return reflector.Reflect (file);
		}

		List<ModuleDeclaration> ReflectToModules (string code, string moduleName, out SwiftInterfaceReflector reflector)
		{
			return Reflector.FromXml (ReflectToXDocument (code, moduleName, out reflector));
		}

		[Test]
		public void SimplestImportTest ()
		{
			var swiftCode = @"
import Swift
public func hello ()
{
    print (""hello"")
}
";
			SwiftInterfaceReflector reflector;
			var modules = ReflectToXDocument (swiftCode, "SomethingSomething", out reflector);

			var importModules = reflector.ImportModules;
			Assert.AreEqual (1, importModules.Count, "not 1 import module");
			Assert.AreEqual ("Swift", importModules [0], "not swift import module");
		}

		[Test]
		public void SimpleImportTest ()
		{
			var swiftCode = @"
import Swift
import Foundation

public func hello ()
{
    print (""hello"")
}
";
			SwiftInterfaceReflector reflector;
			var modules = ReflectToXDocument (swiftCode, "SomethingSomething", out reflector);

			var importModules = reflector.ImportModules;
			Assert.AreEqual (2, importModules.Count, "not 2 import modules");
			Assert.IsNotNull (importModules.FirstOrDefault (s => s == "Swift"), "no Swift import module");
			Assert.IsNotNull (importModules.FirstOrDefault (s => s == "Foundation"), "no Foundation import module");
		}

		[Test]
		public void TypeDatabaseHasOperators ()
		{
			var operators = typeDatabase.OperatorsForModule ("Swift");
			Assert.Less (0, operators.Count (), "no operators");
		}

		[Test]
		public void HasGlobalOperator ()
		{
			var swiftCode = @"
import Swift
public class Imag {
	public var Real:Float = 0, Imaginary: Float = 0

	public static func == (lhs: Imag, rhs: Imag) -> Bool {
		return lhs.Real == rhs.Real && lhs.Imaginary == rhs.Imaginary
	}
}
";

			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Imag");
			Assert.IsNotNull (cl, "no class");

			var fn = cl.Members.FirstOrDefault (m => m.Name == "==") as FunctionDeclaration;
			Assert.IsNotNull (fn, "no function");

			Assert.IsTrue (fn.IsOperator, "not an operator");
			Assert.AreEqual (OperatorType.Infix, fn.OperatorType, "wrong operator type");
		}

		[Test]
		public void HasLocalOperator ()
		{
			var swiftCode = @"
postfix operator *^*

public class Imag {
    public var Real:Float = 0, Imaginary: Float = 0
    
    public static postfix func *^* (lhs: Imag) -> Float {
        return 2 * lhs.Real
    }
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Imag");
			Assert.IsNotNull (cl, "no class");

			var fn = cl.Members.FirstOrDefault (m => m.Name == "*^*") as FunctionDeclaration;
			Assert.IsNotNull (fn, "no function");

			Assert.IsTrue (fn.IsOperator, "not an operator");
			Assert.AreEqual (OperatorType.Postfix, fn.OperatorType, "wrong operator type");
		}

		[Test]
		public void HasExtensionPostfixOperator ()
		{
			var swiftCode = @"
postfix operator *^*
public extension Int {
	static postfix func *^* (a: Int) -> Int {
		return a * 2
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var ext = module.Extensions.FirstOrDefault ();
			Assert.IsNotNull (ext, "no extensions");
			var extType = ext.ExtensionOnTypeName;
			Assert.AreEqual ("Swift.Int", extType, "wrong type");
			var extFunc = ext.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (extFunc, "no func");
			Assert.AreEqual ("*^*", extFunc.Name, "wrong func name");
			Assert.IsTrue (extFunc.IsOperator, "not an operator");
			Assert.AreEqual (OperatorType.Postfix, extFunc.OperatorType, "wrong operator type");
		}


		[Test]
		public void HasExtensionInfixOperator ()
		{
			var swiftCode = @"
infix operator *^*
public extension Int {
	static func *^* (lhs: Int, rhs: Int) -> Int {
		return lhs * 2 + rhs * 2
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var ext = module.Extensions.FirstOrDefault ();
			Assert.IsNotNull (ext, "no extensions");
			var extType = ext.ExtensionOnTypeName;
			Assert.AreEqual ("Swift.Int", extType, "wrong type");
			var extFunc = ext.Members [0] as FunctionDeclaration;
			Assert.IsNotNull (extFunc, "no func");
			Assert.AreEqual ("*^*", extFunc.Name, "wrong func name");
			Assert.IsTrue (extFunc.IsOperator, "not an operator");
			Assert.AreEqual (OperatorType.Infix, extFunc.OperatorType, "wrong operator type");
		}

		[Test]
		public void InheritanceKindIsClass ()
		{
			var swiftCode = @"
public class Foo {
	public init () { }
}

public class Bar : Foo {
	public override init () { }
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.Where (c => c.Name == "Bar").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			Assert.AreEqual (1, cl.Inheritance.Count, "wrong amount of inheritance");
			var inh = cl.Inheritance [0];
			Assert.AreEqual (InheritanceKind.Class, inh.InheritanceKind, "wrong inheritance kind");
		}

		[Test]
		public void CompoundInheritanceKindIsClass ()
		{
			var swiftCode = @"
public protocol Nifty { }

public class Foo {
	public init () { }
}

public class Bar : Foo, Nifty {
	public override init () { }
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.Where (c => c.Name == "Bar").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			Assert.AreEqual (2, cl.Inheritance.Count, "wrong amount of inheritance");
			var inh = cl.Inheritance [0];
			Assert.AreEqual (InheritanceKind.Class, inh.InheritanceKind, "wrong inheritance kind from class");
			inh = cl.Inheritance [1];
			Assert.AreEqual (InheritanceKind.Protocol, inh.InheritanceKind, "wrong inheritance kind from protocol");
		}

		[Test]
		public void WontLoadThisModuleHere ()
		{
			var swiftCode = @"
import AVKit
public class Bar {
	public init () { }
}
";
			SwiftInterfaceReflector reflector;
			Assert.Throws<ParseException> (() => ReflectToModules (swiftCode, "SomeModule", out reflector));
		}
	}
}
