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
using System.Text;
using NUnit.Framework.Legacy;

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
				ClassicAssert.Fail ("no swiftinterface file");
			reflector = new SwiftInterfaceReflector (typeDatabase, new NoLoadLoader ());
			return reflector.Reflect (file);
		}

		List<ModuleDeclaration> ReflectToModules (string code, string moduleName, out SwiftInterfaceReflector reflector)
		{
			return Reflector.FromXml (ReflectToXDocument (code, moduleName, out reflector), typeDatabase);
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
			if (importModules.Count == 3) {
				ClassicAssert.IsTrue (importModules.Contains ("_Concurrency"), "no _Concurrency import");
				ClassicAssert.IsTrue (importModules.Contains ("_StringProcessing"), "no _StringProcessing import");
			} else if (importModules.Count > 3) {
				ClassicAssert.Fail ($"Expected 3 swift modules, but got {importModules.Count}: {AllImportModules (importModules)}");
			}
			ClassicAssert.IsTrue (importModules.Contains ("Swift"), "no Swift import");
		}

		static string AllImportModules (List<string> importModules)
		{
			return String.Join (", ", importModules);
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
			if (importModules.Count == 4) {
				ClassicAssert.IsTrue (importModules.Contains ("_Concurrency"), "no _Concurrency import");
				ClassicAssert.IsTrue (importModules.Contains ("_StringProcessing"), "no _StringProcessing import");
			} else if (importModules.Count > 4) {
				ClassicAssert.Fail ($"Expected 3 swift modules, but got {importModules.Count}: {AllImportModules (importModules)}");
			}
			ClassicAssert.IsTrue (importModules.Contains ("Swift"), "no Swift import");
			ClassicAssert.IsTrue (importModules.Contains ("Foundation"), "no Foundation import");
		}

		[Test]
		public void TypeDatabaseHasOperators ()
		{
			var operators = typeDatabase.OperatorsForModule ("Swift");
			ClassicAssert.Less (0, operators.Count (), "no operators");
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

			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Imag");
			ClassicAssert.IsNotNull (cl, "no class");

			ClassicAssert.AreEqual (2, cl.AllProperties ().Count, "wrong property count");

			var fn = cl.Members.FirstOrDefault (m => m.Name == "==") as FunctionDeclaration;
			ClassicAssert.IsNotNull (fn, "no function");

			ClassicAssert.IsTrue (fn.IsOperator, "not an operator");
			ClassicAssert.AreEqual (OperatorType.Infix, fn.OperatorType, "wrong operator type");
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

			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Imag");
			ClassicAssert.IsNotNull (cl, "no class");

			ClassicAssert.AreEqual (2, cl.AllProperties ().Count, "wrong property count");

			var fn = cl.Members.FirstOrDefault (m => m.Name == "*^*") as FunctionDeclaration;
			ClassicAssert.IsNotNull (fn, "no function");

			ClassicAssert.IsTrue (fn.IsOperator, "not an operator");
			ClassicAssert.AreEqual (OperatorType.Postfix, fn.OperatorType, "wrong operator type");
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

			ClassicAssert.IsNotNull (module, "no module");

			var ext = module.Extensions.FirstOrDefault ();
			ClassicAssert.IsNotNull (ext, "no extensions");
			var extType = ext.ExtensionOnTypeName;
			ClassicAssert.AreEqual ("Swift.Int", extType, "wrong type");
			var extFunc = ext.Members [0] as FunctionDeclaration;
			ClassicAssert.IsNotNull (extFunc, "no func");
			ClassicAssert.AreEqual ("*^*", extFunc.Name, "wrong func name");
			ClassicAssert.IsTrue (extFunc.IsOperator, "not an operator");
			ClassicAssert.AreEqual (OperatorType.Postfix, extFunc.OperatorType, "wrong operator type");
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

			ClassicAssert.IsNotNull (module, "no module");

			var ext = module.Extensions.FirstOrDefault ();
			ClassicAssert.IsNotNull (ext, "no extensions");
			var extType = ext.ExtensionOnTypeName;
			ClassicAssert.AreEqual ("Swift.Int", extType, "wrong type");
			var extFunc = ext.Members [0] as FunctionDeclaration;
			ClassicAssert.IsNotNull (extFunc, "no func");
			ClassicAssert.AreEqual ("*^*", extFunc.Name, "wrong func name");
			ClassicAssert.IsTrue (extFunc.IsOperator, "not an operator");
			ClassicAssert.AreEqual (OperatorType.Infix, extFunc.OperatorType, "wrong operator type");
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

			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.Where (c => c.Name == "Bar").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			ClassicAssert.AreEqual (1, cl.Inheritance.Count, "wrong amount of inheritance");
			var inh = cl.Inheritance [0];
			ClassicAssert.AreEqual (InheritanceKind.Class, inh.InheritanceKind, "wrong inheritance kind");
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

			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.Where (c => c.Name == "Bar").FirstOrDefault ();
			ClassicAssert.IsNotNull (cl, "no class");
			ClassicAssert.AreEqual (2, cl.Inheritance.Count, "wrong amount of inheritance");
			var inh = cl.Inheritance [0];
			ClassicAssert.AreEqual (InheritanceKind.Class, inh.InheritanceKind, "wrong inheritance kind from class");
			inh = cl.Inheritance [1];
			ClassicAssert.AreEqual (InheritanceKind.Protocol, inh.InheritanceKind, "wrong inheritance kind from protocol");
		}

		[Test]
		[Ignore ("Throwing wrong exception")]
		public void WontLoadThisModuleHere ()
		{
			var swiftCode = @"
import AVKit
public class Bar {
	public init () { }
}
";
			SwiftInterfaceReflector reflector;
			ClassicAssert.Throws<ParseException> (() => ReflectToModules (swiftCode, "SomeModule", out reflector));
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
			ClassicAssert.IsNotNull (cl, "no class");
			var clonlyattrs = cl.Element ("attributes");
			ClassicAssert.IsNotNull (clonlyattrs, "no attributes on class");
			var attribute = clonlyattrs.Descendants ("attribute")
				.Where (el => el.Attribute ("name").Value == "objc").FirstOrDefault ();
			ClassicAssert.IsNotNull (attribute, "no objc attribute");
			var initializer = cl.Descendants ("func")
				.Where (el => el.Attribute ("name").Value == ".ctor").FirstOrDefault ();
			ClassicAssert.IsNotNull (initializer, "no initializer");
			attribute = initializer.Descendants ("attribute")
				.Where (el => el.Attribute ("name").Value == "objc").FirstOrDefault ();
			ClassicAssert.IsNotNull (attribute, "no function attribute");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			ClassicAssert.AreEqual (2, cl.Attributes.Count, "wrong number of attributes");

			var attr = cl.Attributes.FirstOrDefault (at => at.Name == "objc");
			ClassicAssert.IsNotNull (attr, "no objc attribute");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			ClassicAssert.IsNotNull (method, "no method");

			ClassicAssert.AreEqual (1, method.Attributes.Count, "wrong number of attributes");

			var attr = method.Attributes.FirstOrDefault (at => at.Name == "objc");
			ClassicAssert.IsNotNull (attr, "no objc attribute");
			var attrParam = attr.Parameters [0] as AttributeParameterLabel;
			ClassicAssert.IsNotNull (attrParam, "not a label");
			ClassicAssert.AreEqual (attrParam.Label, "narwhal", "wrong label");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			ClassicAssert.IsNotNull (method, "no method");

			ClassicAssert.AreEqual (1, method.Attributes.Count, "wrong number of attributes");
			var attr = method.Attributes [0];
			ClassicAssert.AreEqual (attr.Name, "available");
			ClassicAssert.AreEqual (3, attr.Parameters.Count, "wrong number of parameters");
			var label = attr.Parameters [0] as AttributeParameterLabel;
			ClassicAssert.IsNotNull (label, "not a label at 0");
			ClassicAssert.AreEqual ("*", label.Label, "not a star");
			label = attr.Parameters [1] as AttributeParameterLabel;
			ClassicAssert.IsNotNull (label, "not a label at 1");
			ClassicAssert.AreEqual (",", label.Label, "not a comma");
			label = attr.Parameters [2] as AttributeParameterLabel;
			ClassicAssert.IsNotNull (label, "not a label at 2");
			ClassicAssert.AreEqual ("unavailable", label.Label, "not unavailable");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			ClassicAssert.IsNotNull (method, "no method");

			ClassicAssert.AreEqual ("narwhal", method.ObjCSelector, "wrong selector");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "DoSomething");
			ClassicAssert.IsNotNull (method, "no method");

			ClassicAssert.AreEqual ("DoSomething", method.ObjCSelector, "wrong selector");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == ".dtor");
			ClassicAssert.IsNotNull (method, "no method");

			ClassicAssert.AreEqual ("dealloc", method.ObjCSelector, "wrong selector");
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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "bar");
			ClassicAssert.IsNotNull (method, "no method bar");

			ClassicAssert.AreEqual ("barWithA:", method.ObjCSelector, "wrong bar selector");

			method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "foo");
			ClassicAssert.IsNotNull (method, "no method foo");

			ClassicAssert.AreEqual ("foo", method.ObjCSelector, "wrong foo selector");

			method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "set");
			ClassicAssert.IsNotNull (method, "no method set");

			ClassicAssert.AreEqual ("setAt:", method.ObjCSelector, "wrong set selector");

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
			ClassicAssert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");

			var method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "get_subscript");
			ClassicAssert.IsNotNull (method, "no method subscript getter");

			ClassicAssert.AreEqual ("objectAtIndexedSubscript:", method.ObjCSelector, "wrong subscript getter selector");

			method = cl.Members.OfType<FunctionDeclaration> ().FirstOrDefault (fn => fn.Name == "set_subscript");
			ClassicAssert.IsNotNull (method, "no method subscript setter");

			ClassicAssert.AreEqual ("setObject:atIndexedSubscript:", method.ObjCSelector, "wrong subscript setter selector");

		}


		[Test]
		public void DeprecatedFunction ()
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public func foo() { }";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.IsDeprecated, "deprecated");
			ClassicAssert.IsFalse (func.IsUnavailable, "unavailable");
		}

		[Test]
		public void ObsoletedFunction ()
		{
			string code =
				"@available(swift, obsoleted:3.0, message: \"no reason\")" +
				"public func foo() { }";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "module");
			var func = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsFalse (func.IsDeprecated, "deprecated");
			ClassicAssert.IsTrue (func.IsUnavailable, "unavilable");
		}


		[Test]
		public void DeprecatedClass ()
		{
			string code =
				"@available(*, deprecated, message: \"no reason\")" +
				"public class Foo { }";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.Classes.FirstOrDefault (f => f.Name == "Foo");
			ClassicAssert.IsNotNull (cl, "no class");
			ClassicAssert.IsTrue (cl.IsDeprecated, "not deprecated");
			ClassicAssert.IsFalse (cl.IsUnavailable, "available");
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
			ClassicAssert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			var retType = fn.ReturnTypeName;
			ClassicAssert.AreEqual ("Swift.Optional<Swift.Int>", retType, "wrong return");
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
			ClassicAssert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			var retType = fn.ReturnTypeName;
			ClassicAssert.AreEqual ("Swift.Dictionary<Swift.Int,Swift.Int>", retType, "wrong return");
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
			ClassicAssert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			var retType = fn.ReturnTypeName;
			ClassicAssert.AreEqual ("Swift.Array<Swift.Int>", retType, "wrong return");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestAsyncBasic ()
		{
			var code = @"
public func foo () async -> Int {
	return 5
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
		}

		[Test]
		public void TestAsyncBasicNotPresent ()
		{
			var code = @"
public func foo () -> Int {
	return 5
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var fn = module.TopLevelFunctions.FirstOrDefault (f => f.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestAsyncMethod ()
		{
			var code = @"
public class bar {
	public init () { }
	public func foo () async -> Int {
		return 5
	}
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllClasses.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var fn = cl.AllMethodsNoCDTor ().FirstOrDefault (m => m.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
		}

		[Test]
		public void TestAsyncMethodNotPresent ()
		{
			var code = @"
public class bar {
	public init () { }
	public func foo () -> Int {
		return 5
	}
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllClasses.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var fn = cl.AllMethodsNoCDTor ().FirstOrDefault (m => m.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestAsyncProtocolMethod ()
		{
			var code = @"
public protocol bar {
	func foo () async -> Int
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllProtocols.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var fn = cl.AllMethodsNoCDTor ().FirstOrDefault (m => m.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
		}

		[Test]
		public void TestAsyncProtocolMethodNotPresent ()
		{
			var code = @"
public protocol bar {
	func foo () -> Int
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllProtocols.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var fn = cl.AllMethodsNoCDTor ().FirstOrDefault (m => m.Name == "foo");
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestAsyncPropertyPresent ()
		{
			var code = @"
public class bar {
	public init () { }
	var _x = 4
	public var x:Int {
		get async {
			return _x
		}
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllClasses.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "x");
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.GetGetter ();
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
			ClassicAssert.IsTrue (prop.IsAsync, "prop not async");
		}

		[Test]
		public void TestAsyncPropertyNotPresent ()
		{
			var code = @"
public class bar {
	public init () { }
	var _x = 4
	public var x:Int {
		get {
			return _x
		}
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllClasses.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "x");
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.GetGetter ();
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
			ClassicAssert.IsFalse (prop.IsAsync, "prop not async");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestAsyncSubscriptPresent ()
		{
			var code = @"
public class bar {
	public init () { }
	public subscript(a: Int) -> Int {
		get async {
			return a * 2;
		}
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllClasses.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllSubscripts ().FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.Getter;
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
			ClassicAssert.IsTrue (prop.IsAsync, "prop not async");
		}

		[Test]
		public void TestAsyncSubscriptNotPresent ()
		{
			var code = @"
public class bar {
	public init () { }
	public subscript(a: Int) -> Int {
		get {
			return a * 2;
		}
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllClasses.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllSubscripts ().FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.Getter;
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
			ClassicAssert.IsFalse (prop.IsAsync, "prop not async");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestProtocolPropertyAsyncPresent ()
		{
			var code = @"
public protocol bar {
	var x:Int {
		get async
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllProtocols.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "x");
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.GetGetter ();
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
			ClassicAssert.IsTrue (prop.IsAsync, "prop not async");
		}

		[Test]
		public void TestProtocolPropertyAsyncNotPresent ()
		{
			var code = @"
public protocol bar {
	var x:Int {
		get
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllProtocols.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllProperties ().FirstOrDefault (p => p.Name == "x");
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.GetGetter ();
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
			ClassicAssert.IsFalse (prop.IsAsync, "prop not async");
		}

		[Test]
		[Ignore ("not until we update to 5.5")]
		public void TestProtocolAsyncSubscriptPresent ()
		{
			var code = @"
public protocol bar {
	subscript(a: Int) -> Int {
		get async
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllProtocols.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllSubscripts ().FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.Getter;
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsTrue (fn.IsAsync, "not async");
			ClassicAssert.IsTrue (prop.IsAsync, "prop not async");
		}

		[Test]
		public void TestProtocolAsyncSubscriptNotPresent ()
		{
			var code = @"
public protocol bar {
	subscript(a: Int) -> Int {
		get
	}
}
";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (code, "SomeModule", out reflector).Find (m => m.Name == "SomeModule");
			ClassicAssert.IsNotNull (module, "not a module");
			var cl = module.AllProtocols.FirstOrDefault (c => c.Name == "bar");
			ClassicAssert.IsNotNull (cl, "no class");
			var prop = cl.AllSubscripts ().FirstOrDefault ();
			ClassicAssert.IsNotNull (prop, "no property");
			var fn = prop.Getter;
			ClassicAssert.IsNotNull (fn, "no function");
			ClassicAssert.IsFalse (fn.IsAsync, "not async");
			ClassicAssert.IsFalse (prop.IsAsync, "prop not async");
		}
	}
}
