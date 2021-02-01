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

namespace XmlReflectionTests {
	[TestFixture]
	public class SwiftInterfaceParserTests {
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
			reflector = new SwiftInterfaceReflector ();
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
	}
}
