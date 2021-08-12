// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using NUnit.Framework;
using SwiftReflector.SwiftXmlReflection;

namespace XmlReflectionTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class TypeSpecParserTests {
		[Test]
		public void TestSimpleType ()
		{
			NamedTypeSpec ns = TypeSpecParser.Parse ("Swift.Int") as NamedTypeSpec;
			Assert.IsNotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
		}

		[Test]
		public void TestEmptyTuple ()
		{
			TupleTypeSpec tuple = TypeSpecParser.Parse ("()") as TupleTypeSpec;
			Assert.IsNotNull (tuple);
			Assert.AreEqual (0, tuple.Elements.Count);
		}

		[Test]
		public void TestSingleTuple ()
		{
			// single tuples get folded into their type
			NamedTypeSpec ns = TypeSpecParser.Parse ("(Swift.Int)") as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
		}

		[Test]
		public void TestDoubleTuple ()
		{
			TupleTypeSpec tuple = TypeSpecParser.Parse ("(Swift.Int, Swift.Float)") as TupleTypeSpec;
			Assert.IsNotNull (tuple);
			Assert.AreEqual (2, tuple.Elements.Count);
			NamedTypeSpec ns = tuple.Elements [0] as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
			ns = tuple.Elements [1] as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Float", ns.Name);
		}

		[Test]
		public void TestNestedTuple ()
		{
			TupleTypeSpec tuple = TypeSpecParser.Parse ("(Swift.Int, (Swift.Int, Swift.Int))") as TupleTypeSpec;
			Assert.IsNotNull (tuple);
			Assert.AreEqual (2, tuple.Elements.Count);
			NamedTypeSpec ns = tuple.Elements [0] as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
			tuple = tuple.Elements [1] as TupleTypeSpec;
			Assert.NotNull (tuple);
			Assert.AreEqual (2, tuple.Elements.Count);
			ns = tuple.Elements [0] as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
			ns = tuple.Elements [1] as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
		}

		[Test]
		public void TestFuncIntInt ()
		{
			ClosureTypeSpec close = TypeSpecParser.Parse ("Swift.Int -> Swift.Int") as ClosureTypeSpec;
			Assert.NotNull (close);
			NamedTypeSpec ns = close.Arguments as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
			ns = close.ReturnType as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
		}

		[Test]
		public void TestFuncVoidVoid ()
		{
			ClosureTypeSpec close = TypeSpecParser.Parse ("() -> ()") as ClosureTypeSpec;
			Assert.NotNull (close);
			TupleTypeSpec ts = close.Arguments as TupleTypeSpec;
			Assert.NotNull (ts);
			Assert.AreEqual (0, ts.Elements.Count);
			ts = close.ReturnType as TupleTypeSpec;
			Assert.NotNull (ts);
			Assert.AreEqual (0, ts.Elements.Count);
		}

		[Test]
		public void TestArrayOfInt ()
		{
			NamedTypeSpec ns = TypeSpecParser.Parse ("Swift.Array<Swift.Int>") as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Array", ns.Name);
			Assert.IsTrue (ns.ContainsGenericParameters);
			Assert.AreEqual (1, ns.GenericParameters.Count);
			ns = ns.GenericParameters [0] as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Int", ns.Name);
		}

		[Test]
		public void TestDictionaryOfIntString ()
		{
			NamedTypeSpec ns = TypeSpecParser.Parse ("Swift.Dictionary<Swift.Int, Swift.String>") as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.Dictionary", ns.Name);
			Assert.IsTrue (ns.ContainsGenericParameters);
			Assert.AreEqual (2, ns.GenericParameters.Count);
			NamedTypeSpec ns1 = ns.GenericParameters [0] as NamedTypeSpec;
			Assert.NotNull (ns1);
			Assert.AreEqual ("Swift.Int", ns1.Name);
			ns1 = ns.GenericParameters [1] as NamedTypeSpec;
			Assert.NotNull (ns1);
			Assert.AreEqual ("Swift.String", ns1.Name);
		}

		[Test]
		public void TestWithAttributes ()
		{
			TupleTypeSpec tupled = TypeSpecParser.Parse ("(Builtin.RawPointer, (@convention[thin] (Builtin.RawPointer, inout Builtin.UnsafeValueBuffer, inout SomeModule.Foo, @thick SomeModule.Foo.Type) -> ())?)")
				as TupleTypeSpec;
			Assert.NotNull (tupled);
			var ns = tupled.Elements [1] as NamedTypeSpec;
			Assert.IsTrue (ns.ContainsGenericParameters);
			Assert.AreEqual ("Swift.Optional", ns.Name);
			var close = ns.GenericParameters[0] as ClosureTypeSpec;
			Assert.AreEqual (1, close.Attributes.Count);
		}


		[Test]
		public void TestGeneric ()
		{
			NamedTypeSpec ns = TypeSpecParser.Parse ("Swift.UnsafeMutablePointer<(Swift.Int, Error, Swift.Bool)>") as NamedTypeSpec;
			Assert.NotNull (ns);
			Assert.AreEqual ("Swift.UnsafeMutablePointer", ns.Name);
			Assert.IsTrue (ns.ContainsGenericParameters);
			Assert.AreEqual (1, ns.GenericParameters.Count);
			var ts = ns.GenericParameters [0] as TupleTypeSpec;
			Assert.NotNull (ts);
			Assert.AreEqual (3, ts.Elements.Count);
		}

		[Test]
		public void TestEmbeddedClass()
		{
			NamedTypeSpec ns = TypeSpecParser.Parse ("Swift.Dictionary<Swift.String, T>.Index") as NamedTypeSpec;
			Assert.IsNotNull (ns);
			Assert.IsNotNull (ns.InnerType);
			Assert.AreEqual ("Index", ns.InnerType.Name);
			Assert.AreEqual ("Swift.Dictionary<Swift.String, T>.Index", ns.ToString ());
		}

		[Test]
		public void TestProtocolListAlphabetical ()
		{
			var specs = new NamedTypeSpec [] {
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Dfoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var protos = new ProtocolListTypeSpec (specs);
			Assert.AreEqual ("Afoo & Bfoo & Cfoo & Dfoo", protos.ToString (), "ToString mismatch");
		}

		[Test]
		public void TestProtocolListAlphabetical1 ()
		{
			var specs = new NamedTypeSpec [] {
				new NamedTypeSpec ("🤡Foo"),
				new NamedTypeSpec ("💩Foo"),
			};

			var protos = new ProtocolListTypeSpec (specs);
			Assert.AreEqual ("💩Foo & 🤡Foo", protos.ToString (), "ToString mismatch");
		}

		[Test]
		public void TestProtocolListMatch ()
		{
			var specs1 = new NamedTypeSpec [] {
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Dfoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var specs2 = new NamedTypeSpec [] {
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Dfoo"),
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var protos1 = new ProtocolListTypeSpec (specs1);
			var protos2 = new ProtocolListTypeSpec (specs2);

			Assert.IsTrue (protos1.Equals (protos2), "lists don't match");
		}

		[Test]
		public void TestProtocolListNotMatch ()
		{
			var specs1 = new NamedTypeSpec [] {
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Dfoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var specs2 = new NamedTypeSpec [] {
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Efoo"),
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var protos1 = new ProtocolListTypeSpec (specs1);
			var protos2 = new ProtocolListTypeSpec (specs2);

			Assert.IsFalse (protos1.Equals (protos2), "lists match?!");
		}

		[Test]
		public void TestProtocolListNotMatchLength ()
		{
			var specs1 = new NamedTypeSpec [] {
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Dfoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var specs2 = new NamedTypeSpec [] {
				new NamedTypeSpec ("Afoo"),
				new NamedTypeSpec ("Dfoo"),
				new NamedTypeSpec ("Cfoo"),
				new NamedTypeSpec ("Efoo"),
				new NamedTypeSpec ("Bfoo")
			};

			var protos1 = new ProtocolListTypeSpec (specs1);
			var protos2 = new ProtocolListTypeSpec (specs2);

			Assert.IsFalse (protos1.Equals (protos2), "lists match?!");
		}

		[Test]
		public void TestProtocolListParseSimple ()
		{
			var protocolListType = TypeSpecParser.Parse ("c & b & a") as ProtocolListTypeSpec;
			Assert.IsNotNull (protocolListType, "parse returned null");
			Assert.AreEqual (3, protocolListType.Protocols.Count, "wrong count");
			Assert.AreEqual ("a & b & c", protocolListType.ToString (), "mismatch roundtrip");
		}

		[Test]
		public void TestProtocolListParseNoSpacesBecauseWhyNot ()
		{
			var protocolListType = TypeSpecParser.Parse ("c&b&a") as ProtocolListTypeSpec;
			Assert.IsNotNull (protocolListType, "parse returned null");
			Assert.AreEqual (3, protocolListType.Protocols.Count, "wrong count");
			Assert.AreEqual ("a & b & c", protocolListType.ToString (), "mismatch roundtrip");
		}

		[Test]
		public void TestOptionalProtocolListType ()
		{
			var optionalList = TypeSpecParser.Parse ("d & f & e ?") as NamedTypeSpec;
			Assert.IsNotNull (optionalList, "no optional");
			Assert.AreEqual ("Swift.Optional", optionalList.Name);
			var proto = optionalList.GenericParameters [0] as ProtocolListTypeSpec;
			Assert.IsNotNull (proto, "not a protocol list");
			Assert.AreEqual (3, proto.Protocols.Count, "wrong count");
			Assert.AreEqual ("d & e & f", proto.ToString ());
		}

		[Test]
		public void TestReplaceInNameSuccess ()
		{
			var inType = TypeSpecParser.Parse ("Foo.Bar");
			var replaced = inType.ReplaceName ("Foo.Bar", "Slarty.Bartfast") as NamedTypeSpec;
			Assert.IsNotNull (replaced, "not a named spec");
			Assert.AreEqual ("Slarty.Bartfast", replaced.Name);
		}

		[Test]
		public void TestReplaceInNameFail ()
		{
			var inType = TypeSpecParser.Parse ("Foo.Bar");
			var same = inType.ReplaceName ("Blah", "Slarty.Bartfast") as NamedTypeSpec;
			Assert.AreEqual (same, inType, "changed?!");
		}

		[Test]
		public void TestReplaceInTupleSuccess ()
		{
			var inType = TypeSpecParser.Parse ("(Swift.Int, Foo.Bar, Foo.Bar)");
			var replaced = inType.ReplaceName ("Foo.Bar", "Slarty.Bartfast") as TupleTypeSpec;
			Assert.IsNotNull (replaced, "not a tuple spec");
			var name = replaced.Elements [1] as NamedTypeSpec;
			Assert.IsNotNull (name, "first elem isn't a named type spec");
			Assert.AreEqual ("Slarty.Bartfast", name.Name, "failed first");
			name = replaced.Elements [2] as NamedTypeSpec;
			Assert.IsNotNull (name, "second elem isn't a named type spec");
			Assert.AreEqual ("Slarty.Bartfast", name.Name, "failed second");
		}

		[Test]
		public void TestReplaceInTupleFail ()
		{
			var inType = TypeSpecParser.Parse ("(Swift.Int, Foo.Bar, Foo.Bar)");
			var same = inType.ReplaceName ("Blah", "Slarty.Bartfast") as TupleTypeSpec;
			Assert.AreEqual (same, inType, "changed?!");
		}

		[Test]
		public void TestReplaceInClosureSuccess ()
		{
			var inType = TypeSpecParser.Parse ("(Swift.Int, Foo.Bar) -> Foo.Bar");
			var replaced = inType.ReplaceName ("Foo.Bar", "Slarty.Bartfast") as ClosureTypeSpec;
			Assert.IsNotNull (replaced, "not a closure spec");
			var args = replaced.Arguments as TupleTypeSpec;
			Assert.IsNotNull (args, "first elem isn't a tuple spec");
			Assert.AreEqual (2, args.Elements.Count, "wrong arg count");
			var name = args.Elements [1] as NamedTypeSpec;
			Assert.AreEqual ("Slarty.Bartfast", name.Name, "first");
			name = replaced.ReturnType as NamedTypeSpec;
			Assert.AreEqual ("Slarty.Bartfast", name.Name, "return");
		}

		[Test]
		public void TestReplaceInClosureFail ()
		{
			var inType = TypeSpecParser.Parse ("(Swift.Int, Foo.Bar) -> Foo.Bar");
			var same = inType.ReplaceName ("Blah", "Slarty.Bartfast") as ClosureTypeSpec;
			Assert.IsNotNull (same, "not a closure spec");
			Assert.AreEqual (same, inType, "changed?!");
		}
		[Test]
		public void TestReplaceInProtoListSuccess ()
		{
			var inType = TypeSpecParser.Parse ("Swift.Equatable & Foo.Bar");
			var replaced = inType.ReplaceName ("Foo.Bar", "Slarty.Bartfast") as ProtocolListTypeSpec;
			Assert.IsNotNull (replaced, "not a protolist spec");
			var name = replaced.Protocols.Keys.FirstOrDefault (n => n.Name == "Slarty.Bartfast");
			Assert.IsNotNull (name, "not replaced");
		}

		[Test]
		public void TestReplaceInProtoListFail ()
		{
			var inType = TypeSpecParser.Parse ("Swift.Equatable & Foo.Bar");
			var same = inType.ReplaceName ("Blah", "Slarty.Bartfast") as ProtocolListTypeSpec;
			Assert.AreEqual (same, inType, "changed?!");
		}

		[Test]
		public void TestWeirdClosureIssue ()
		{
			var inType = TypeSpecParser.Parse ("@escaping[] (_onAnimation:Swift.Bool)->Swift.Void");
			Assert.IsTrue (inType is ClosureTypeSpec, "not closure");
			var closSpec = inType as ClosureTypeSpec;
			Assert.IsTrue (closSpec.IsEscaping, "not escaping");
			var textRep = closSpec.ToString ();
			var firstIndex = textRep.IndexOf ("_onAnimation");
			var lastIndex = textRep.LastIndexOf ("_onAnimation");
			Assert.IsTrue (firstIndex == lastIndex);
		}

		[Test]
		public void TestAttributeParsing ()
		{
			var inAttributeType = "Foo.Bar<Baz<Frobozz>>";
			var attribute = new AttributeDeclaration (inAttributeType);
			var attrType = attribute.AttributeType;
			Assert.AreEqual ("Foo.Bar", attrType.Name, "wrong type name");
			Assert.IsTrue (attrType.GenericParameters.Count == 1, "no first generic");
			var inner = attrType.GenericParameters [0] as NamedTypeSpec;
			Assert.IsNotNull (inner, "first is not a named type spec");
			Assert.AreEqual ("Baz<Frobozz>", inner.ToString (), "wrong first type spec");
			Assert.IsTrue (inner.GenericParameters.Count == 1, "no second generic");
			inner = inner.GenericParameters [0] as NamedTypeSpec;
			Assert.IsNotNull (inner, "second is not a named type spec");
			Assert.AreEqual ("Frobozz", inner.ToString (), "wrong second type spec");
		}
	}
}

