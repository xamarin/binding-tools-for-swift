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
using NUnit.Framework.Legacy;

namespace XmlReflectionTests {
	public class TypeAliasTests {

		static ModuleDeclaration module;
		static BaseDeclaration throwAway;

		static BaseDeclaration ThrowAwayContext ()
		{
			if (throwAway != null)
				return throwAway;
			if (module != null)
				module = new ModuleDeclaration ("NoName");

			throwAway = new ClassDeclaration () {
				Name = "AClass",
				Access = Accessibility.Public,
				Module = module,
				Kind = TypeKind.Class
			};
			return throwAway;
		}

		void FoldTest (string testName, BaseDeclaration context, string source, string expected, params TypeAliasDeclaration[] decls)
		{
			context = context ?? ThrowAwayContext ();
			var aliases = new List<TypeAliasDeclaration> ();
			aliases.AddRange (decls);

			var sourceTypeSpec = TypeSpecParser.Parse (source);

			var folder = new TypeAliasFolder (aliases);
			var result = folder.FoldAlias (context, sourceTypeSpec);
			ClassicAssert.IsNotNull (result, $"Test {testName} result was null");
			ClassicAssert.AreEqual (expected, result.ToString (), $"Test {testName} type spec mismatch");
		}

		[Test]
		public void SimpleFold ()
		{
			FoldTest ("SimpleFold", null, "Foo", "Swift.Int",
				new TypeAliasDeclaration () { TypeName = "Foo", TargetTypeName = "Swift.Int" });
		}

		[Test]
		public void DoubleFold ()
		{
			FoldTest ("DoubleFold", null, "Foo", "Swift.Int",
				new TypeAliasDeclaration () { TypeName = "Bar", TargetTypeName = "Swift.Int" },
				new TypeAliasDeclaration () { TypeName = "Foo", TargetTypeName = "Bar" });
		}

		[Test]
		public void CompleteGenericFold ()
		{
			FoldTest ("CompleteGenericFold", null, "Foo", "Array<Swift.Int>",
				new TypeAliasDeclaration () { TypeName = "Foo", TargetTypeName = "Array<Swift.Int>"}
				);
		}


		[Test]
		public void ParameterizedGenericFold ()
		{
			FoldTest ("ParameterizedGenericFold", null, "Foo<Swift.Int>", "Array<Swift.Int>",
				new TypeAliasDeclaration () { TypeName = "Foo<T>", TargetTypeName = "Array<T>" }
				);
		}


		[Test]
		public void PartialParameterizedGenericFold ()
		{
			FoldTest ("PartialParameterizedGenericFold", null, "Foo<Swift.Int>", "Dictionary<Swift.String, Swift.Int>",
				new TypeAliasDeclaration () { TypeName = "Foo<T>", TargetTypeName = "Dictionary<Swift.String, T>" }
				);
		}

		[Test]
		public void AssociatedTypeFold ()
		{
			FoldTest ("AssociatedTypeFold", null, "Foo<Proto>", "Dictionary<Proto.Key, Proto.Value>",
				new TypeAliasDeclaration () { TypeName = "Foo<T>", TargetTypeName = "Dictionary<T.Key, T.Value>" });
		}

		[Test]
		public void TupleTest ()
		{
			FoldTest ("TupleTest", null, "Foo<Swift.Int, Swift.Bool>", "(Swift.Int, Swift.Bool)",
				new TypeAliasDeclaration () { TypeName = "Foo<T, U>", TargetTypeName = "(T, U)" });
		}

		[Test]
		public void ClosureTest ()
		{
			FoldTest ("FoldTest", null, "Foo<(Swift.Int, Swift.Int), Swift.Bool>", "(Swift.Int, Swift.Int) -> Swift.Bool",
				new TypeAliasDeclaration () { TypeName = "Foo<T, U>", TargetTypeName = "T->U"});
		}

		[Test]
		public void ProtoListTest ()
		{
			FoldTest ("ClosureTest", null, "Foo", "Codable & Decodable",
				new TypeAliasDeclaration () { TypeName = "Foo", TargetTypeName = "Codable & Decodable" });
		}
	}
}
