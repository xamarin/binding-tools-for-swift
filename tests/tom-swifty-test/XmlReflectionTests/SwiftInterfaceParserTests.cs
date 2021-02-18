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


		[Test]
		public void HasObjCAttribute ()
		{
			var swiftCode = @"
import Foundation
@objc
public class Foo : NSObject {
	public override init () { }
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToXDocument (swiftCode, "SomeModule", out reflector);

			var cl = module.Descendants ("typedeclaration").FirstOrDefault ();
			Assert.IsNotNull (cl, "no class");
			var clonlyattrs = cl.Element ("attributes");
			Assert.IsNotNull (clonlyattrs, "no attributes on class");
			var attribute = clonlyattrs.Descendants ("attribute")
				.Where (el => el.Attribute ("name").Value == "objc").FirstOrDefault ();
			Assert.IsNotNull (attribute, "no objc attribute");
			var initializer = cl.Descendants ("func")
				.Where (el => el.Attribute ("name").Value == ".ctor").FirstOrDefault ();
			Assert.IsNotNull (initializer, "no initializer");
			attribute = initializer.Descendants ("attribute")
				.Where (el => el.Attribute ("name").Value == "objc").FirstOrDefault ();
			Assert.IsNotNull (attribute, "no function attribute");
		}

		[Test]
		public void HasAttributeDeclarations ()
		{
			var swiftCode = @"
import Foundation
@objc
public class Foo : NSObject {
	public override init () { }
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			Assert.AreEqual (2, cl.Attributes.Count, "wrong number of attributes");

			var attr = cl.Attributes.FirstOrDefault (at => at.Name == "objc");
			Assert.IsNotNull (attr, "no objc attribute");
		}


		[Test]
		public void HasAttributeObjCSelectorParameter ()
		{
			var swiftCode = @"
import Foundation
@objc
public class Foo : NSObject {
	public override init () { }
	@objc(narwhal)
	public func DoSomething () -> Int {
		return 1
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			Assert.IsNotNull (method, "no method");

			Assert.AreEqual (1, method.Attributes.Count, "wrong number of attributes");

			var attr = method.Attributes.FirstOrDefault (at => at.Name == "objc");
			Assert.IsNotNull (attr, "no objc attribute");
			var attrParam = attr.Parameters [0] as AttributeParameterLabel;
			Assert.IsNotNull (attrParam, "not a label");
			Assert.AreEqual (attrParam.Label, "narwhal", "wrong label");
		}

		[Test]
		public void HasAvailableAttributeAll ()
		{
			var swiftCode = @"
import Foundation


public class Foo { 
    public init () { }
    @available (*, unavailable)
    public func DoSomething () -> Int {
        return 1
    }
} 
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			Assert.IsNotNull (method, "no method");

			Assert.AreEqual (1, method.Attributes.Count, "wrong number of attributes");
			var attr = method.Attributes [0];
			Assert.AreEqual (attr.Name, "available");
			Assert.AreEqual (3, attr.Parameters.Count, "wrong number of parameters");
			var label = attr.Parameters [0] as AttributeParameterLabel;
			Assert.IsNotNull (label, "not a label at 0");
			Assert.AreEqual ("*", label.Label, "not a star");
			label = attr.Parameters [1] as AttributeParameterLabel;
			Assert.IsNotNull (label, "not a label at 1");
			Assert.AreEqual (",", label.Label, "not a comma");
			label = attr.Parameters [2] as AttributeParameterLabel;
			Assert.IsNotNull (label, "not a label at 2");
			Assert.AreEqual ("unavailable", label.Label, "not unavailable");
		}

		[Test]
		public void CorrectObjCSupplied ()
		{
			var swiftCode = @"
import Foundation

@objc
public class Foo : NSObject { 
    override public init () { }
    @objc(narwhal)
    public func DoSomething () -> Int {
        return 1
    }
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			Assert.IsNotNull (method, "no method");

			Assert.AreEqual ("narwhal", method.ObjCSelector, "wrong selector");
		}

		[Test]
		public void CorrectObjCNotSupplied ()
		{
			var swiftCode = @"
import Foundation

@objc
public class Foo : NSObject { 
    override public init () { }
    @objc
    public func DoSomething () -> Int {
        return 1
    }
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			Assert.IsNotNull (method, "no method");

			Assert.AreEqual ("DoSomething", method.ObjCSelector, "wrong selector");
		}


		[Test]
		public void CorrectObjCDeInit ()
		{
			var swiftCode = @"
import Foundation

@objc
public class Foo : NSObject { 
    override public init () { }
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == ".dtor");
			Assert.IsNotNull (method, "no method");

			Assert.AreEqual ("dealloc", method.ObjCSelector, "wrong selector");
		}

		[Test]
		public void CorrectObjCMemberSelector ()
		{
			string swiftCode = @"
import Foundation

@objc
public class Foo : NSObject {
	override public init () { }
	public func bar(a: Int) { }
	public func foo () { }
	public func set(at: Int) { }
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "bar");
			Assert.IsNotNull (method, "no method bar");

			Assert.AreEqual ("barWithA:", method.ObjCSelector, "wrong bar selector");

			method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "foo");
			Assert.IsNotNull (method, "no method foo");

			Assert.AreEqual ("foo", method.ObjCSelector, "wrong foo selector");

			method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "set");
			Assert.IsNotNull (method, "no method set");

			Assert.AreEqual ("setAt:", method.ObjCSelector, "wrong set selector");

		}

		[Test]
		public void CorrectSubsriptSelector ()
		{
			// @objc subscript (index:Int) ->Int { get set }
			string swiftCode = @"
import Foundation

@objc
public class Foo : NSObject {
	override public init () { }
	public subscript (index:Int) ->Int {
		get {
			return 3;
		}
		set {
		}
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (mod => mod.Name == "SomeModule");
			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			Assert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "get_subscript");
			Assert.IsNotNull (method, "no method subscript getter");

			Assert.AreEqual ("objectAtIndexedSubscript:", method.ObjCSelector, "wrong subscript getter selector");

			method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "set_subscript");
			Assert.IsNotNull (method, "no method subscript setter");

			Assert.AreEqual ("setObject:atIndexedSubscript:", method.ObjCSelector, "wrong subscript setter selector");

		}


		[Test]
		public void DeprecatedFunction ()
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public func foo() { }";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.IsDeprecated, "deprecated");
			Assert.IsFalse (func.IsUnavailable, "unavailable");
		}

		[Test]
		public void ObsoletedFunction ()
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public func foo() { }";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (func, "func");
			Assert.IsFalse (func.IsDeprecated, "deprecated");
			Assert.IsTrue (func.IsUnavailable, "unavilable");
		}


		[Test]
		public void DeprecatedClass ()
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public class Foo { }";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			Assert.IsNotNull (cl, "no class");
			Assert.IsTrue (cl.IsDeprecated, "not deprecated");
			Assert.IsFalse (cl.IsUnavailable, "available");
		}

		[Test]
		public void OptionalType ()
		{
			string code = @"
public func foo (a: Bool) -> Int? {
    return a ? 42 : nil
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (fn, "no function");
			var retType = fn.ReturnTypeName;
			Assert.AreEqual ("Swift.Optional<Swift.Int>", retType, "wrong return");
		}

		[Test]
		public void DictionaryType ()
		{
			string code = @"
public func foo () -> [Int:Int] {
    return Dictionary<Int, Int>()
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (fn, "no function");
			var retType = fn.ReturnTypeName;
			Assert.AreEqual ("Swift.Dictionary<Swift.Int,Swift.Int>", retType, "wrong return");
		}

		[Test]
		public void ArrayType ()
		{
			string code = @"
public func foo () -> [Int] {
    return Array<Int>()
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			Assert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			Assert.IsNotNull (fn, "no function");
			var retType = fn.ReturnTypeName;
			Assert.AreEqual ("Swift.Array<Swift.Int>", retType, "wrong return");
		}
	}
}
