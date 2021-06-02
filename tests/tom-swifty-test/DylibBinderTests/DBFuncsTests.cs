using System;
using System.Collections.Generic;
using System.IO;
using DylibBinder;
using NUnit.Framework;
using SwiftReflector;
using SwiftReflector.IOUtils;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace tomwiftytest.DylibBinderTests {

	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class DBFuncsTests {

		static string [] SwiftTypes { get; } = new string [] {
			"Int",
			"Float",
			"Double",
			"String",
			"Character",
			"Bool",
		};

		static (string, string) [] SwiftTypesWithValues { get; } = new (string, string) [] {
			("Int", "0"),
			("Float", "0"),
			("Double", "0"),
			("String", $"\"a\""),
			("Character", $"\"a\""),
			("Bool", "false"),
		};

		[TestCaseSource(nameof(SwiftTypes))]
		public void StaticTest (string type)
		{
			string swiftCode = $"public class TestClass {{ public static func staticFunc(test: {type}) {{}}}}";

			using (DisposableTempDirectory provider = new DisposableTempDirectory (null, false)) {

				Utils.CompileSwift (swiftCode, provider);
				var dylibPath = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var moduleDeclarations = DylibBinderUtils.DylibBinderToModule (dylibPath, "libXython.dylib");

				var decl = DylibBinderUtils.GetClassDeclaration (moduleDeclarations);
				var funcDecl = decl as FunctionDeclaration;
				Assert.IsTrue (funcDecl.IsStatic, "The method was not static");
			}
		}

		[TestCaseSource (nameof (SwiftTypesWithValues))]
		public void PropertyTest ((string type, string value) tuple)
		{
			string swiftCode = $"public class TestClass {{ public var propertyName: {tuple.type} = {tuple.value}}}";

			using (DisposableTempDirectory provider = new DisposableTempDirectory (null, false)) {

				Utils.CompileSwift (swiftCode, provider);
				var dylibPath = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var moduleDeclarations = DylibBinderUtils.DylibBinderToModule (dylibPath, "libXython.dylib");

				var decl = DylibBinderUtils.GetClassDeclaration (moduleDeclarations);
				var propDecl = decl as PropertyDeclaration;
				Assert.True (decl is PropertyDeclaration, "Did not come out as a PropertyDeclaration");
			}
		}

		[TestCaseSource (nameof (SwiftTypes))]
		public void ReturnTypeTest (string type)
		{
			string swiftCode = $"public class TestClass {{ public func testFunc(test: {type}) -> {type} {{return test}}}}";

			using (DisposableTempDirectory provider = new DisposableTempDirectory (null, false)) {

				Utils.CompileSwift (swiftCode, provider);
				var dylibPath = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var moduleDeclarations = DylibBinderUtils.DylibBinderToModule (dylibPath, "libXython.dylib");

				var decl = DylibBinderUtils.GetClassDeclaration (moduleDeclarations);
				var funcDecl = decl as FunctionDeclaration;
				Assert.AreEqual (funcDecl.ReturnTypeSpec, new NamedTypeSpec ($"Swift.{type}"), $"The return type \"{type}\" was not found");
			}
		}

		[TestCaseSource (nameof (SwiftTypes))]
		public void ThrowTest (string type)
		{
			string swiftCode = $"public class TestClass {{ public func testFunc (test: {type}) throws -> {type} {{return test}}}}";

			using (DisposableTempDirectory provider = new DisposableTempDirectory (null, false)) {

				Utils.CompileSwift (swiftCode, provider);
				var dylibPath = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var moduleDeclarations = DylibBinderUtils.DylibBinderToModule (dylibPath, "libXython.dylib");

				var decl = DylibBinderUtils.GetClassDeclaration (moduleDeclarations);
				var funcDecl = decl as FunctionDeclaration;
				Assert.IsTrue (funcDecl.HasThrows, $"Method did not throw");
			}
		}
	}
}
