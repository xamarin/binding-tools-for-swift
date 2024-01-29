// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using tomwiftytest;
using SwiftReflector.Demangling;
using Xamarin;
using Dynamo.CSLang;
using Dynamo;
using SwiftReflector.TypeMapping;
using NUnit.Framework.Legacy;

namespace SwiftReflector.Demangling {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class Swift4DemanglerTests {
		[Test]
		public void TestFuncReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsIntSiyF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var sbt = tlf.Signature.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (sbt);
			ClassicAssert.AreEqual (CoreBuiltInType.Int, sbt.BuiltInType);

		}

		[Test]
		public void TestFuncWithIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bS2i_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}

		[Test]
		public void TestFuncWithUIntIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bS2u_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.UInt, bit.BuiltInType);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithTupleOfUIntIntIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt3arg1cS2u1a_Si1bt_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("arg", argTuple.Contents [0].Name.ToString ());
			var tuple = argTuple.Contents [0] as SwiftTupleType;
			ClassicAssert.IsNotNull (tuple);
			ClassicAssert.AreEqual (2, tuple.Contents.Count);

			var bit = tuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual ("a", bit.Name.Name);
			ClassicAssert.AreEqual (CoreBuiltInType.UInt, bit.BuiltInType);

			bit = tuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual ("b", bit.Name.Name);
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");

			ClassicAssert.AreEqual ("c", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit2");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}




		[Test]
		public void TestFuncWithBoolIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiSb_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}

		[Test]
		public void TestFuncWithFloatIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiSf_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}

		[Test]
		public void TestFuncWithDoubleIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiSd_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Double, bit.BuiltInType);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithInOutIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bS2iz_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.IsTrue (argTuple.Contents [0].IsReference);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}


		[Test]
		public void TestFuncWithClassIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA7MyClassC_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var ct = argTuple.Contents [0] as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "ct");
			ClassicAssert.IsTrue (ct.IsClass);
			ClassicAssert.AreEqual ("unitHelpFrawework.MyClass", ct.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.IsFalse (ct.IsReference);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithInnerStructIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA7MyClassC8InnerFooV_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var ct = argTuple.Contents [0] as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "ct");
			ClassicAssert.IsTrue (ct.IsStruct);
			ClassicAssert.AreEqual ("unitHelpFrawework.MyClass.InnerFoo", ct.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.IsFalse (ct.IsReference);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithEnumIntArgsReturnInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA7FooEnumO_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var ct = argTuple.Contents [0] as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "ct");
			ClassicAssert.IsTrue (ct.IsEnum);
			ClassicAssert.AreEqual ("unitHelpFrawework.FooEnum", ct.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.IsFalse (ct.IsReference);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}

		[Test]
		public void TestFuncWithProtocolIntArgsReturnInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA11BarProtocol_p_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count, "tuple count");
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString (), "arg 1 name");
			var ct = argTuple.Contents [0] as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "ct");
			ClassicAssert.IsTrue (ct.IsProtocol, "isProtocol");
			ClassicAssert.AreEqual ("unitHelpFrawework.BarProtocol", ct.ClassName.ToFullyQualifiedName (true), "name match");
			ClassicAssert.IsFalse (ct.IsReference, "isReference");
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString (), "arg 2 name");
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int");
		}

		[Test]
		public void TestFuncWithBoolIntReturnTupleOfDoubleInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework12ReturnsTuple1a1bSb_SitSb_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			ClassicAssert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			ClassicAssert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");

			var retTuple = tlf.Signature.ReturnType as SwiftTupleType;
			ClassicAssert.IsNotNull (retTuple);
			ClassicAssert.AreEqual (2, retTuple.Contents.Count);

			bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit2");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);

			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit3");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}


		[Test]
		public void TestFuncWithOptionalIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1aS2iSg_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var bgt = tlf.Signature.GetParameter (0) as SwiftBoundGenericType;
			ClassicAssert.AreEqual ("a", bgt.Name.ToString (), "name matches");
			ClassicAssert.IsNotNull (bgt, "bgt");
			var baseType = bgt.BaseType as SwiftClassType;
			ClassicAssert.IsNotNull (baseType, "baseType");
			ClassicAssert.IsTrue (baseType.IsEnum, "isEnum");
			ClassicAssert.AreEqual ("Swift.Optional", baseType.ClassName.ToFullyQualifiedName (true), "is optional");
			ClassicAssert.AreEqual (1, bgt.BoundTypes.Count, "is 1 bound type");
			var bit = bgt.BoundTypes [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "is built-in type");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int");
		}


		[Test]
		public void TestClassMethodBoolReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC8TestFunc1aSiSb_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("TestFunc", tlf.Name.Name);
			var ucf = tlf.Signature as SwiftUncurriedFunctionType;
			ClassicAssert.IsNotNull (ucf, "ucf");
			ClassicAssert.IsTrue (ucf.UncurriedParameter.IsClass);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.IsFalse (tlf.Signature is SwiftStaticFunctionType);
		}

		[Test]
		public void TestMethodBoolThrows ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC11WillItThrow1aySb_tKF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.CanThrow);
		}

		[Test]
		public void TestFuncBoolThrows ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework16MaybeItWillThrow1aySb_tKF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.CanThrow);
		}

		[Test]
		public void TestStructMethodBoolReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooV8TestFunc1aSiSb_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("TestFunc", tlf.Name.Name);
			var ucf = tlf.Signature as SwiftUncurriedFunctionType;
			ClassicAssert.IsNotNull (ucf, "ucf");
			ClassicAssert.IsTrue (ucf.UncurriedParameter.IsStruct);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.IsFalse (tlf.Signature is SwiftStaticFunctionType);
		}

		[Test]
		public void TestEnumMethodBoolReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooO8TestFunc1aSiSb_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("TestFunc", tlf.Name.Name);
			var ucf = tlf.Signature as SwiftUncurriedFunctionType;
			ClassicAssert.IsNotNull (ucf, "ucf");
			ClassicAssert.IsTrue (ucf.UncurriedParameter.IsEnum);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.IsFalse (tlf.Signature is SwiftStaticFunctionType);
		}


		[Test]
		public void TestStaticClassMethodBoolReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC8TestFunc1aSiSb_tFZ", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("TestFunc", tlf.Name.Name);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.IsTrue (tlf.Signature is SwiftStaticFunctionType);
		}

		[Test]
		public void TestClassNonAllocatingCtorIntBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1a1bACSi_Sbtcfc", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (".nctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestClassCtorIntBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1a1bACSi_SbtcfC", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (".ctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestStructCtorIntBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework8MyStructV1a1bACSi_SbtcfC", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (".ctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (2, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestEnumCtorIntBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework6MyEnumO1a1b1cACSb_SiSftcfC", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (".ctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			ClassicAssert.AreEqual (3, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [2] as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit2");
			ClassicAssert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
		}



		[Test]
		public void TestClassDtor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCfd", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var dtor = tlf.Signature as SwiftDestructorType;
			ClassicAssert.IsNotNull (dtor, "dtor");
			ClassicAssert.AreEqual (Decomposer.kSwiftNonDeallocatingDestructorName.Name, dtor.Name.Name);
		}


		[Test]
		public void TestClassDeallocatingDtor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCfD", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var dtor = tlf.Signature as SwiftDestructorType;
			ClassicAssert.IsNotNull (dtor, "dtor");
			ClassicAssert.AreEqual (Decomposer.kSwiftDeallocatingDestructorName.Name, dtor.Name.Name);
		}

		[Test]
		public void TestClassGetterInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1xSivg", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual ("x", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.AreEqual (PropertyType.Getter, prop.PropertyType);
		}

		[Test]
		public void TestClassSetterInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1xSivs", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsFalse (prop.IsStatic);
			ClassicAssert.AreEqual ("x", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.AreEqual (PropertyType.Setter, prop.PropertyType);
		}

		[Test]
		public void TestGetterSubscriptIntBoolOntoFloat ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCySfSi_Sbtcig", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual ("subscript", prop.Name.Name);
			var sft = prop.OfType as SwiftFunctionType;
			ClassicAssert.IsNotNull (sft, "sft");
			ClassicAssert.AreEqual (2, sft.ParameterCount);
			var bit = sft.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = sft.GetParameter (1) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = sft.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bi2");
			ClassicAssert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
		}

		[Test]
		public void TestSetterSubscriptIntBoolOntoFloat ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCySfSi_Sbtcis", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual ("subscript", prop.Name.Name);
			var sft = prop.OfType as SwiftFunctionType;
			ClassicAssert.IsNotNull (sft, "sft");
			ClassicAssert.AreEqual (3, sft.ParameterCount);
			var bit = sft.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
			bit = sft.GetParameter (1) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit1");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = sft.GetParameter (2) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit2");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestClassStaticGetterInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC6FoobleSivgZ", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsStatic);
			ClassicAssert.AreEqual ("Fooble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.AreEqual (PropertyType.Getter, prop.PropertyType);
		}


		[Test]
		public void TestClassStaticSetterInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC6FoobleSivsZ", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsStatic);
			ClassicAssert.AreEqual ("Fooble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			ClassicAssert.AreEqual (PropertyType.Setter, prop.PropertyType);
		}



		[Test]
		public void TestMethodTakingFunc ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC7callFoo1ayyyXE_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftUncurriedFunctionType;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual (1, func.ParameterCount);
			var funcArg = func.GetParameter (0) as SwiftFunctionType;
			ClassicAssert.IsNotNull (funcArg);
			ClassicAssert.AreEqual (0, funcArg.ParameterCount);
			ClassicAssert.AreEqual ("a", funcArg.Name.Name);
		}


		[Test]
		public void TestGlobalGetterBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7TroubleSbvg", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsFalse (prop.IsStatic);
			ClassicAssert.IsTrue (prop.IsGlobal);
			ClassicAssert.AreEqual ("Trouble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			ClassicAssert.AreEqual (PropertyType.Getter, prop.PropertyType);
		}

		[Test]
		public void TestGlobalSetterBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7TroubleSbvs", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsFalse (prop.IsStatic);
			ClassicAssert.IsTrue (prop.IsGlobal);
			ClassicAssert.AreEqual ("Trouble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			ClassicAssert.AreEqual (PropertyType.Setter, prop.PropertyType);
		}


		[Test]
		public void TestGlobalVariableBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7TroubleSbvp", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlv = tld as TLVariable;
			ClassicAssert.IsNotNull (tlv, "tlv");
			ClassicAssert.AreEqual ("Trouble", tlv.Name.Name, "var name");
			var bit = tlv.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType, "is int");
		}


		[Test]
		public void TestClassMetadataAccessor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCMa", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var cctor = tlf.Signature as SwiftClassConstructorType;
			ClassicAssert.AreEqual (Decomposer.kSwiftClassConstructorName.Name, tlf.Name.Name);
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", tlf.Class.ClassName.ToFullyQualifiedName (true));
		}


		[Test]
		public void TestClassMetadata ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCN", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlm = tld as TLDirectMetadata;
			ClassicAssert.IsNotNull (tlm, "tlm");
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", tlm.Class.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.IsTrue (tlm.Class.IsClass);
		}

		[Test]
		public void TestNominalTypeDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooVMn", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tln = tld as TLNominalTypeDescriptor;
			ClassicAssert.IsNotNull (tln, "tln");
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", tln.Class.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.IsTrue (tln.Class.IsStruct);
		}

		[Test]
		public void TestProtocolDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework6SummerMp", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var ptd = tld as TLProtocolTypeDescriptor;
			ClassicAssert.IsNotNull (ptd, "ptd");
			ClassicAssert.AreEqual ("unitHelpFrawework.Summer", ptd.Class.ClassName.ToFullyQualifiedName (true));
		}

		[Test]
		public void TestVarInitializer ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC5waterSivpfi", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var initializer = tlf.Signature as SwiftInitializerType;
			ClassicAssert.IsNotNull (initializer, "initializer");
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", initializer.Owner.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.AreEqual ("water", initializer.Name.Name);
			ClassicAssert.AreEqual (InitializerType.Variable, initializer.InitializerType);
		}

		[Test]
		public void TestLazyCacheVariable ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCML", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tllcv = tld as TLLazyCacheVariable;
			ClassicAssert.IsNotNull (tllcv, "tllcv");
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", tllcv.Class.ClassName.ToFullyQualifiedName (true));
		}

		[Test]
		public void TestProtocolWitnessTable ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCAA6SummerAAWP", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var witness = tlf.Signature as SwiftWitnessTableType;
			ClassicAssert.IsNotNull (witness, "witness");
			ClassicAssert.AreEqual (WitnessType.Protocol, witness.WitnessType);
		}

		[Test]
		[Ignore ("wasn't able to generate this")]
		public void TestProtocolWitnessAccessor ()
		{
			var tld = Decomposer.Decompose ("__T05None13FooCAA6SummerAAWa", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var witness = tlf.Signature as SwiftWitnessTableType;
			ClassicAssert.IsNotNull (witness, "witness");
			ClassicAssert.AreEqual (WitnessType.ProtocolAccessor, witness.WitnessType);
		}


		[Test]
		public void TestValueWitnessTable ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework8TheThingVWV", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var witness = tlf.Signature as SwiftWitnessTableType;
			ClassicAssert.IsNotNull (witness, "witness");
			ClassicAssert.AreEqual (WitnessType.Value, witness.WitnessType);
		}

		[Test]
		public void TestGenericFuncOfT ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1ayx_tlF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.ContainsGenericParameters);
			ClassicAssert.AreEqual (1, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (genericParam, "genericParam");
			ClassicAssert.AreEqual (0, genericParam.Depth, "0 depth");
			ClassicAssert.AreEqual (0, genericParam.Index, "0 index");
			ClassicAssert.AreEqual (0, func.GenericArguments [0].Constraints.Count, "0 constraints at index 0");
		}

		[Test]
		public void TestGenericFuncOfTUOneProtoConstraint ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1a1byx_q_tAA5AdderR_r0_lF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.ContainsGenericParameters);
			ClassicAssert.AreEqual (2, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (genericParam, "genericParam");
			ClassicAssert.AreEqual (0, genericParam.Depth, "0 depth");
			ClassicAssert.AreEqual (0, genericParam.Index, "0 index");
			ClassicAssert.AreEqual (0, func.GenericArguments [0].Constraints.Count, "0 constraints at index 0");
			ClassicAssert.AreEqual (1, func.GenericArguments [1].Constraints.Count, "1 constraint at index 1");
			var constraint = func.GenericArguments [1].Constraints [0] as SwiftClassType;
			ClassicAssert.IsNotNull (constraint, "constraint");
			ClassicAssert.IsTrue (constraint.IsProtocol);
		}

		[Test]
		public void TestGenericFuncOfTUOneClassConstraint ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1a1byx_q_tAA3FooCRbzr0_lF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.ContainsGenericParameters);
			ClassicAssert.AreEqual (2, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (genericParam, "genericParam");
			ClassicAssert.AreEqual (0, genericParam.Depth, "0 depth");
			ClassicAssert.AreEqual (0, genericParam.Index, "0 index");
			ClassicAssert.AreEqual (1, func.GenericArguments [0].Constraints.Count, "1 constraint at index 0");
			ClassicAssert.AreEqual (0, func.GenericArguments [1].Constraints.Count, "0 constraint at index 0");
			var constraint = func.GenericArguments [0].Constraints [0] as SwiftClassType;
			ClassicAssert.IsNotNull (constraint, "constraint");
			ClassicAssert.IsTrue (constraint.IsClass);
		}

		[Test]
		public void TestGenericFuncOfTUTwoOneProtocolConstraint ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1a1byx_q_tAA5AdderR_AA6SubberR_r0_lF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.ContainsGenericParameters);
			ClassicAssert.AreEqual (2, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (genericParam, "genericParam");
			ClassicAssert.AreEqual (0, genericParam.Depth, "0 depth");
			ClassicAssert.AreEqual (0, genericParam.Index, "0 index");
			ClassicAssert.AreEqual (2, func.GenericArguments [1].Constraints.Count, "2 constraints at index 1");
			ClassicAssert.AreEqual (0, func.GenericArguments [0].Constraints.Count, "0 constraints at index 0");
			var constraint = func.GenericArguments [1].Constraints [0] as SwiftClassType;
			ClassicAssert.IsNotNull (constraint, "constraint");
			ClassicAssert.IsTrue (constraint.IsProtocol);
			constraint = func.GenericArguments [1].Constraints [1] as SwiftClassType;
			ClassicAssert.IsNotNull (constraint, "constraint");
			ClassicAssert.IsTrue (constraint.IsProtocol);
		}

		[Test]
		public void TestGenericFuncOfGenericClass ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1ayAA3FooCyxG_tlF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.IsTrue (func.ContainsGenericParameters);
			ClassicAssert.AreEqual (1, func.GenericArguments.Count, "1 gen arg");
			var genericParam = func.GetParameter (0) as SwiftBoundGenericType;
			ClassicAssert.AreEqual (1, genericParam.BoundTypes.Count, "1 bound type");
			var genericParamType = genericParam.BoundTypes [0] as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (genericParamType, "genericParamType");
			ClassicAssert.AreEqual (0, genericParamType.Depth, "0 depth");
			ClassicAssert.AreEqual (0, genericParamType.Index, "0 index");
		}

		[Test]
		public void TestOperatorEqEq ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC2eeoiySbAC_ACtFZ", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (OperatorType.Infix, tlf.Operator, "operator");
			ClassicAssert.AreEqual ("==", tlf.Name.Name);
		}


		[Test]
		public void TestOperatorMinusPlusMinus ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3spsoiyS2i_SitF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (OperatorType.Infix, tlf.Operator, "operator");
			ClassicAssert.AreEqual ("-+-", tlf.Name.Name);
		}

		[Test]
		public void TestUnicodeOperator ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework008deiFBEEeopyS2bF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ('\u2757', tlf.Name.Name [0]);
		}

		[Test]
		public void TestAnyObject ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3boo1byyXl_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (1, tlf.Signature.ParameterCount);
			var cl = tlf.Signature.GetParameter (0) as SwiftClassType;
			ClassicAssert.IsNotNull (cl, "cl");
			ClassicAssert.AreEqual ("Swift.AnyObject", cl.ClassName.ToFullyQualifiedName ());
			ClassicAssert.IsTrue (cl.IsClass);
		}


		[Test]
		public void TestAny ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3foo1byyp_tF", false);
			ClassicAssert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (1, tlf.Signature.ParameterCount, "1 parameter");
			var cl = tlf.Signature.GetParameter (0) as SwiftClassType;
			ClassicAssert.IsNotNull (cl, "cl");
			ClassicAssert.AreEqual ("Swift.Any", cl.ClassName.ToFullyQualifiedName ());
			ClassicAssert.IsTrue (cl.IsProtocol);
		}

		[Test]
		public void TestOptionalCtor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC4failACSgSb_tcfc", false);
			ClassicAssert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Expected function");
			ClassicAssert.IsTrue (tlf.Signature.IsConstructor, "Expected constructor");
			var ctorReturn = tlf.Signature.ReturnType as SwiftBoundGenericType;
			ClassicAssert.IsNotNull (ctorReturn, "Expected bound generic return");
			var payload = ctorReturn.BoundTypes [0] as SwiftClassType;
			ClassicAssert.IsNotNull (ctorReturn, "Expected class");
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", payload.ClassName.ToFullyQualifiedName (true), "Expected None.Foo");
			ClassicAssert.IsTrue (tlf.Signature.IsOptionalConstructor, "Not an optional ctor");
		}

		[Test]
		public void TestStaticExtensionFunc ()
		{
			var tld = Decomposer.Decompose ("_$sSi17unitHelpFraweworkE3fooSiyFZ", false);
			ClassicAssert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Expected function");
			ClassicAssert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			ClassicAssert.IsTrue (tlf.Signature is SwiftStaticFunctionType, "Expected static function");
			var scalar = tlf.Signature.ExtensionOn as SwiftBuiltInType;
			ClassicAssert.IsNotNull (scalar, "Expected swift built in type");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, scalar.BuiltInType, "Expected an Int");
		}


		[Test]
		public void TestStaticExtensionProp ()
		{
			var tld = Decomposer.Decompose ("_$sSi17unitHelpFraweworkE3fooSivgZ", false);
			ClassicAssert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Expected function");
			ClassicAssert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			ClassicAssert.IsTrue (tlf.Signature is SwiftPropertyType, "Expected property");
			var scalar = tlf.Signature.ExtensionOn as SwiftBuiltInType;
			ClassicAssert.IsNotNull (scalar, "Expected swift built in type");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, scalar.BuiltInType, "Expected an Int");
		}


		[Test]
		public void TestGenericExtensionOnFunc ()
		{
			var tld = Decomposer.Decompose ("_$sSb17unitHelpFraweworkE5truth1aSSx_tAA6TruthyRzlF", false);
			ClassicAssert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Expected function");
			ClassicAssert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var scalar = tlf.Signature.ExtensionOn as SwiftBuiltInType;
			ClassicAssert.IsNotNull (scalar, "Expected swift built int type");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, scalar.BuiltInType, "Expected a bool");
		}


		[Test]
		public void TestExtensionSubscript ()
		{
			var tld = Decomposer.Decompose ("_$sSa17unitHelpFraweworkAA6TruthyRzlEyxSgSScig", false);
			ClassicAssert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Expected function");
			ClassicAssert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "Expected property");
			ClassicAssert.IsTrue (prop.IsSubscript, "Expected subscript");
		}

		[Test]
		public void TestEulerOperatorDemangle ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework003BehopyySbyXKF", false);
			ClassicAssert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Expected function");
		}


		[Test]
		public void TestObjCTLFunction ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework21PathPositionAnimationC014createKeyframeF033_3D85A716E8AC30D62D97E78DB643A23DLLyyF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "expected function");
			ClassicAssert.AreEqual (tlf.Name.Name, "_3D85A716E8AC30D62D97E78DB643A23D", "name mistmatch.");

		}

		[Test]
		public void TestMaterializerExtension1 ()
		{
			var tld = Decomposer.Decompose ("__T0So6UIViewC12RazzleDazzleE14scaleTransformSC08CGAffineE0VSgfm", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "expected function");
			ClassicAssert.AreEqual (tlf.Name.Name, "scaleTransform", "name mismatch");
		}

		[Test]
		[Ignore ("AFAIK, Swifft 5 doesn't generate materializers")]
		public void TestMaterializerExtension2 ()
		{
			var tld = Decomposer.Decompose ("", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "expected function");
			ClassicAssert.AreEqual (tlf.Name.Name, "rotationTransform", "name mismatch");
		}

		[Test]
		[Ignore ("AFAIK, Swifft 5 doesn't generate materializers")]
		public void TestMaterializerExtension3 ()
		{
			var tld = Decomposer.Decompose ("", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "expected function");
			ClassicAssert.AreEqual (tlf.Name.Name, "translationTransform", "name mismatch");
		}

		[Test]
		public void TestProtocolConformanceDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s7XamGlue18xam_proxy_HashableCSQAAMc", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlprot = tld as TLProtocolConformanceDescriptor;
			ClassicAssert.IsNotNull (tlprot, "not a protocol conformance descriptor");
			ClassicAssert.IsNotNull (tlprot.ImplementingType, "no class");
			var cl = tlprot.ImplementingType as SwiftClassType;
			ClassicAssert.IsNotNull (cl);
			ClassicAssert.AreEqual ("XamGlue.xam_proxy_Hashable", cl.ClassName.ToFullyQualifiedName (), "wrong implementor");
			ClassicAssert.AreEqual ("Swift.Equatable", tlprot.Protocol.ClassName.ToFullyQualifiedName (), "wrong protocol");
		}

		[Test]
		public void TestTypeAlias ()
		{
			var tld = Decomposer.Decompose ("_$s13NSObjectTests21SomeVirtualClassmacOSC13StringVersion1vSSSo017NSOperatingSystemH0a_tF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var onlyArg = tlf.Signature.GetParameter (0) as SwiftClassType;
			ClassicAssert.IsNotNull (onlyArg, "not a class type arg");
			ClassicAssert.AreEqual ("Foundation.OperatingSystemVersion", onlyArg.ClassName.ToFullyQualifiedName (), "name mistmatch");
		}


		[Test]
		public void TestPropertyExtension ()
		{
			var tld = Decomposer.Decompose ("_$sSf10CircleMenuE7degreesSfvpMV", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a property descriptor");
			ClassicAssert.IsNotNull (tlf.ExtensionOn, "no extension");
			ClassicAssert.IsNull (tlf.Class, "has a class?!");
		}

		[Test]
		public void TestFieldOffsetExtension ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw16SWXMLHashOptionsC8encodingSS10FoundationE8EncodingVvpWvd", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFieldOffset;
			ClassicAssert.IsNotNull (tlf, "wrong type");
		}

		[Test]
		public void TestUIColorExtension0 ()
		{
			var tld = Decomposer.Decompose ("_$sSo7UIColorC3HueE3hexABSS_tcfC", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.IsTrue (tlf.Signature.IsConstructor, "not a constructor");
			ClassicAssert.IsNotNull (tlf.Signature.ExtensionOn, "no extension");
		}


		[Test]
		public void TestUIColorExtension1 ()
		{
			var tld = Decomposer.Decompose ("_$sSo7UIColorC8MaterialE4argbABs6UInt32V_tcfC", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.IsTrue (tlf.Signature.IsConstructor, "not a constructor");
			ClassicAssert.IsNotNull (tlf.Signature.ExtensionOn, "no extension");
		}

		[Test]
		public void TestUIColorExtension2 ()
		{
			var tld = Decomposer.Decompose ("_$sSo7UIColorC12DynamicColorE3hue10saturation9lightness5alphaAB12CoreGraphics7CGFloatV_A3JtcfC", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.IsTrue (tlf.Signature.IsConstructor, "not a constructor");
			ClassicAssert.IsNotNull (tlf.Signature.ExtensionOn, "no extension");
		}

		[Test]
		public void TestProtocolRequirementsDescriptor0 ()
		{
			var tld = Decomposer.Decompose ("_$s14DateTimePicker0abC8DelegateTL", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLProtocolRequirementsBaseDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a protocol requirements base descriptor");
			ClassicAssert.AreEqual ("DateTimePicker.DateTimePickerDelegate", tlf.Class.ClassName.ToFullyQualifiedName (true), "wrong name");
		}

		[Test]
		public void TestProtocolRequirementsDescriptor1 ()
		{
			var tld = Decomposer.Decompose ("_$s4Neon9FrameableTL", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLProtocolRequirementsBaseDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a protocol requirements base descriptor");
			ClassicAssert.AreEqual ("Neon.Frameable", tlf.Class.ClassName.ToFullyQualifiedName (true), "wrong name");
		}

		[Test]
		public void TestNREInMacaw ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw5ShapeC11interpolate_8progressACXDAC_SdtF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.AreEqual ("interpolate", tlf.Signature.Name.Name, "wrong name");
		}

		public void BaseReqTest (string mangle, string protoName, string reqName)
		{
			var tld = Decomposer.Decompose (mangle, false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLBaseConformanceDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a base conformance descriptor");
			ClassicAssert.AreEqual (protoName, tlf.Class.ClassName.ToFullyQualifiedName (true), "wrong name");
			ClassicAssert.AreEqual (reqName, tlf.ProtocolRequirement.ClassName.ToFullyQualifiedName (true), "wrong requirement");
		}

		[Test]
		public void TestProtocolRequirementDescriptor2 ()
		{
			BaseReqTest ("_$s4Neon10AnchorablePAA9FrameableTb", "Neon.Anchorable", "Neon.Frameable");
		}

		[Test]
		public void TestProtocolRequirementDescriptor3 ()
		{
			BaseReqTest ("_$s4Neon9GroupablePAA9FrameableTb", "Neon.Groupable", "Neon.Frameable");
		}

		[Test]
		public void TestSubscriptDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw10XMLIndexerOyACSicipMV", false);
			ClassicAssert.IsNotNull (tld, "failed descriptor");
		}

		[Test]
		public void TestFieldOffsetContainsClass ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw13ChangeHandlerC6handleyyxcvpWvd", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var fieldOffset = tld as TLFieldOffset;
			ClassicAssert.IsNotNull (fieldOffset, "field offset");
			var cl = fieldOffset.Class;
			ClassicAssert.IsNotNull (cl, "null class");
		}

		[Test]
		public void TestPrivateInitializer0 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka9_AlertRowC0029presentationModestorage_rAFJh33_D25096F98D3944FE1BCE11D750532E6DLLAA16PresentationModeOyAA08SelectorB10ControllerCyACyxGGGSgSgvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestPrivateInitializer1 ()
		{
			var tld = Decomposer.Decompose ("_$s15JTAppleCalendar0aB4ViewC0020theDatastorage_mdAJd33_70DF286E0E62C56975265F8CF5A8FF56LLAA0B4DataVSgvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestPrivateInitializer2 ()
		{
			var tld = Decomposer.Decompose ("_$s8Presentr0A10ControllerC0030shouldSwipeBottomstorage_cDAEi33_9D6ACB2CCC4A4980BDBB65F0F301220BLLSbSgvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestExtensionInit ()
		{
			var tld = Decomposer.Decompose ("_$sSd6EurekaE6stringSdSgSS_tcfC", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.IsNotNull (tlf.Signature.ExtensionOn, "no extension?");
			var swiftClassType = tlf.Signature.ExtensionOn as SwiftClassType;
			ClassicAssert.IsNotNull (swiftClassType, "extension is not a class?");
			ClassicAssert.AreEqual ("Swift.Double", swiftClassType.ClassName.ToFullyQualifiedName (), "bad name");
		}

		[Test]
		public void TestVarInInitializer0 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka22_TriplePickerInlineRowC13secondOptionsySayq_Gxcvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer1 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka22_TriplePickerInlineRowC12thirdOptionsySayq0_Gx_q_tcvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer2 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka22_DoublePickerInlineRowC13secondOptionsySayq_Gxcvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer3 ()
		{
			var tld = Decomposer.Decompose ("_$s11PaperSwitch08RAMPaperB0C24animationDidStartClosureyySbcvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer4 ()
		{
			var tld = Decomposer.Decompose ("_$s11PaperSwitch08RAMPaperB0C23animationDidStopClosureyySb_Sbtcvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestClosureDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s9FaceAware14ClosureWrapperC7closureyyxcvsTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestClosurePropertyDesc ()
		{
			var tld = Decomposer.Decompose ("_$s18XLActionController8CellSpecO6heighty12CoreGraphics7CGFloatVq_cvg", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestStaticVariable ()
		{
			var tld = Decomposer.Decompose ("_$s8Material5ThemeV4fontAA8FontType_pXpvMZ", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.AreEqual ("font", tlf.Name.Name);
		}

		[Test]
		public void TestStaticAddressorVariable ()
		{
			var tld = Decomposer.Decompose ("_$s8Material5ThemeV4fontAA8FontType_pXpvau", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlu = tld as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlu, "not an addressor");
			ClassicAssert.AreEqual ("font", tlu.Name.Name);
		}

		[Test]
		public void TestStaticVariable1 ()
		{
			var tld = Decomposer.Decompose ("_$s8Material5ThemeV4fontAA8FontType_pXpvpZ", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLVariable;
			ClassicAssert.IsNotNull (tlf, "not a function");
			ClassicAssert.AreEqual ("font", tlf.Name.Name);
		}

		[Test]
		public void TestPATReference ()
		{
			var tld = Decomposer.Decompose ("_$s24ProtocolConformanceTests9doSetProp1a1byxz_4ItemQztAA9Simplest3RzlF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var arg2 = tlf.Signature.GetParameter (1) as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (arg2, "Not an SLGenericReference");
			ClassicAssert.IsTrue (arg2.HasAssociatedTypePath, "No associated type path");
			ClassicAssert.AreEqual (1, arg2.AssociatedTypePath.Count, "wrong number of assoc type path elements");
			ClassicAssert.AreEqual ("Item", arg2.AssociatedTypePath [0], "Mismatch in assoc type name");
		}

		[Test]
		public void TestPATPathReference ()
		{
			var tld = Decomposer.Decompose ("_$s15BadAssociations7doPrint1a1byx_5Thing_4NameQZtAA13PrintableItemRzlF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var arg2 = tlf.Signature.GetParameter (1) as SwiftGenericArgReferenceType;
			ClassicAssert.IsNotNull (arg2, "Not an SLGenericReference");
			ClassicAssert.IsTrue (arg2.HasAssociatedTypePath, "No associated type path");
			ClassicAssert.AreEqual (2, arg2.AssociatedTypePath.Count, "wrong number of assoc type path elements");
			ClassicAssert.AreEqual ("Thing", arg2.AssociatedTypePath [0], "Mismatch in assoc type name 0");
			ClassicAssert.AreEqual ("Name", arg2.AssociatedTypePath [1], "Mismatch in assoc type name 1");
		}

		[Test]
		public void TestGenericMetatype ()
		{
			var tld = Decomposer.Decompose ("_$sSD14ExtensionTestsE5value6forKey6ofTypeqd__SgSS_qd__mtlF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var arg0 = tlf.Signature.GetParameter (0) as SwiftClassType;
			ClassicAssert.IsNotNull (arg0, "not a swift class type at arg0");
			var arg1 = tlf.Signature.GetParameter (1) as SwiftMetaClassType;
			ClassicAssert.IsNotNull (arg1, "not a metaclass type");
			ClassicAssert.IsNotNull (arg1.ClassGenericReference, "not a generic reference metatype");
		}

		[Test]
		public void TestProtocolList0 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie10StrokeNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvg", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList1 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie10StrokeNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvgTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList2 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie10StrokeNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvpMV", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a prop descriptor");
			var proptype = tlf.OfType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (proptype);
			ClassicAssert.AreEqual (2, proptype.Protocols.Count);
		}

		[Test]
		public void TestProtocolList3 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie10StrokeNodeC8rendererAA0C6Output_AA10Renderablepvg", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList4 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie10StrokeNodeC8rendererAA0C6Output_AA10RenderablepvgTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList5 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie10StrokeNodeC8rendererAA0C6Output_AA10RenderablepvpMV", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a prop descriptor");
			var proptype = tlf.OfType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (proptype);
			ClassicAssert.AreEqual (2, proptype.Protocols.Count);
		}

		[Test]
		public void TestProtocolList6 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie11EllipseNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvg", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList7 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie11EllipseNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvgTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList8 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie11EllipseNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvpMV", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (tlf, "not a prop descriptor");
			var proptype = tlf.OfType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (proptype);
			ClassicAssert.AreEqual (2, proptype.Protocols.Count);
		}

		[Test]
		public void TestProtocolList9 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie11PolygonNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvg", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void TestProtocolList10 ()
		{
			var tld = Decomposer.Decompose ("_$s6Lottie11PolygonNodeC11propertyMapAA17KeypathSearchable_AA0c8PropertyE0pvgTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var ret = tlf.Signature.ReturnType as SwiftProtocolListType;
			ClassicAssert.IsNotNull (ret, "not a protocol list");
			ClassicAssert.AreEqual (2, ret.Protocols.Count);
		}

		[Test]
		public void VariableInitializationExpression0 ()
		{
			var tld = Decomposer.Decompose ("_$s8Presentr0A10ControllerC0027shouldSwipeTopstorage_mvFAh33_9D6ACB2CCC4A4980BDBB65F0F301220BLLSbSgvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void VariableInitializationExpression1 ()
		{
			var tld = Decomposer.Decompose ("_$s8Presentr0A10ControllerC0030shouldSwipeBottomstorage_cDAEi33_9D6ACB2CCC4A4980BDBB65F0F301220BLLSbSgvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void VariableInitializationExpression2 ()
		{
			var tld = Decomposer.Decompose ("_$s8Presentr0A10ControllerC0030shouldSwipeBottomstorage_cDAEi33_9D6ACB2CCC4A4980BDBB65F0F301220BLLSbSgvpfi", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void MethodDescriptor0 ()
		{
			var tld = Decomposer.Decompose ("_$s8Presentr0A10ControllerC013presentedViewB0010presentingdB016presentationType12roundCorners12cornerRadius10dropShadow13backgroundTap14dismissOnSwipe0pqR9Direction0N5Color0N7Opacity14blurBackground0V5Style06customwD0019keyboardTranslationG00P8Animated27contextFrameForPresentation014outsideContextO0ACSo06UIViewB0C_AWSgAA012PresentationG0OSbSg12CoreGraphics7CGFloatVAA0aM0VSgAA0wO6ActionOSbAA07DismissrS0OSo7UIColorCSfSbSo012UIBlurEffectX0VSo6UIViewCSgAA019KeyboardTranslationG0OSbSo6CGRectVSgA7_tcfCTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var methodDesc = tld as TLMethodDescriptor;
			ClassicAssert.IsNotNull (methodDesc, "not a method descriptor");
		}

		[Test]
		public void TestExtensionDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$sSi16MySwiftFrameworkE8timesTwoSivpMV", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var pd = tld as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (pd, "not a property descriptor");
			ClassicAssert.IsNotNull (pd.ExtensionOn, "no extension");
			var onType = pd.ExtensionOn as SwiftBuiltInType;
			ClassicAssert.IsNotNull (onType, "not a built in type");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, onType.BuiltInType, "not an int");
		}

		[Test]
		public void TestReflectionMetadataField ()
		{
			var tld = Decomposer.Decompose ("_$s8itsAFive2E2OMF", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var mdd = tld as TLMetadataDescriptor;
			ClassicAssert.IsNotNull (mdd, "not a metadata descriptor");
			var cl = mdd.OfType as SwiftClassType;
			ClassicAssert.IsNotNull (cl, "not a class");
			ClassicAssert.AreEqual ("itsAFive.E2", cl.ClassName.ToFullyQualifiedName (), "wrong name");
		}

		[Test]
		public void TestReflectionMetadataBuiltin ()
		{
			var tld = Decomposer.Decompose ("_$s8itsAFive2E2OMB", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var mdd = tld as TLMetadataDescriptor;
			ClassicAssert.IsNotNull (mdd, "not a metadata descriptor");
			ClassicAssert.IsTrue (mdd.IsBuiltIn, "not built in");
			var cl = mdd.OfType as SwiftClassType;
			ClassicAssert.IsNotNull (cl, "not a class");
			ClassicAssert.AreEqual ("itsAFive.E2", cl.ClassName.ToFullyQualifiedName (), "wrong name");
		}

		[Test]
		public void TestBaseConformanceDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$sSHSQTb", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var bcd = tld as TLBaseConformanceDescriptor;
			ClassicAssert.IsNotNull (bcd, "not a conformance descriptor");
			var cl = bcd.ProtocolRequirement as SwiftClassType;
			ClassicAssert.IsNotNull (cl, "not a class");
			ClassicAssert.AreEqual ("Swift.Equatable", cl.ClassName.ToFullyQualifiedName (), "wrong name");
		}

		[Test]
		public void TestAssociatedTypeDescriptor0 ()
		{
			var tld = Decomposer.Decompose ("_$s12RowValueType6Eureka04RuleC0PTl", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var atdesc = tld as TLAssociatedTypeDescriptor;
			ClassicAssert.IsNotNull (atdesc, "not an associated type desc");
			ClassicAssert.AreEqual ("RowValueType", atdesc.AssociatedTypeName.Name, "wrong associated type name");
			ClassicAssert.AreEqual ("Eureka.RuleType", atdesc.Class.ClassName.ToFullyQualifiedName (), "protocol name mismatch");
		}

		[Test]
		public void TestAssociatedTypeDescriptor1 ()
		{
			var tld = Decomposer.Decompose ("_$s23PresentedControllerType6Eureka012PresenterRowC0PTl", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var atdesc = tld as TLAssociatedTypeDescriptor;
			ClassicAssert.IsNotNull (atdesc, "not an associated type desc");
			ClassicAssert.AreEqual ("PresentedControllerType", atdesc.AssociatedTypeName.Name, "wrong associated type name");
			ClassicAssert.AreEqual ("Eureka.PresenterRowType", atdesc.Class.ClassName.ToFullyQualifiedName (), "protocol name mismatch");
		}

		[Test]
		public void TestAssociatedTypeDescriptor2 ()
		{
			var tld = Decomposer.Decompose ("_$s9InlineRow6Eureka0aB4TypePTl", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var atdesc = tld as TLAssociatedTypeDescriptor;
			ClassicAssert.IsNotNull (atdesc, "not an associated type desc");
			ClassicAssert.AreEqual ("InlineRow", atdesc.AssociatedTypeName.Name, "wrong associated type name");
			ClassicAssert.AreEqual ("Eureka.InlineRowType", atdesc.Class.ClassName.ToFullyQualifiedName (), "protocol name mismatch");
		}

		[Test]
		public void TestMoreMethodDescriptor0 ()
		{
			var tld = Decomposer.Decompose ("_$s14ClassWrapTests10GarbleWCRCCACycfCTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var md = tld as TLMethodDescriptor;
			ClassicAssert.IsNotNull (md, "not a method descriptor");
			ClassicAssert.AreEqual ("GarbleWCRC", md.Name.Name, "name mismatch");
		}

		[Test]
		public void TestMoreMethodDescriptor1 ()
		{
			var tld = Decomposer.Decompose ("_$s14ClassWrapTests11MontyWMMIntCACycfCTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var md = tld as TLMethodDescriptor;
			ClassicAssert.IsNotNull (md, "not a method descriptor");
			ClassicAssert.AreEqual ("MontyWMMInt", md.Name.Name, "name mismatch");
		}

		[Test]
		public void TestMoreMethodDescriptor2 ()
		{
			var tld = Decomposer.Decompose ("_$s14ClassWrapTests11MontyWSMIntCACycfCTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var md = tld as TLMethodDescriptor;
			ClassicAssert.IsNotNull (md, "not a method descriptor");
			ClassicAssert.AreEqual ("MontyWSMInt", md.Name.Name, "name mismatch");
		}

		[Test]
		public void TestMoreMethodDescriptor3 ()
		{
			var tld = Decomposer.Decompose ("_$s14ClassWrapTests11MontyWSPIntCACycfCTq", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var md = tld as TLMethodDescriptor;
			ClassicAssert.IsNotNull (md, "not a method descriptor");
			ClassicAssert.AreEqual ("MontyWSPInt", md.Name.Name, "name mismatch");
		}

		[Test]
		public void TestMorePropertyDescriptor0 ()
		{
			var tld = Decomposer.Decompose ("_$s14ClassWrapTests11MontyWSPIntC3valSivpMV", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var pd = tld as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (pd, "not a property descriptor");
			ClassicAssert.AreEqual ("val", pd.Name.Name, "name mismatch");
		}

		[Test]
		public void TestPropertyThunk ()
		{
			var tld = Decomposer.Decompose ("_$s7CanFind3BarC1xSbvgTj", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "null function");
			var getter = tlf.Signature as SwiftPropertyThunkType;
			ClassicAssert.IsNotNull (getter, "not a property");
			ClassicAssert.AreEqual (PropertyType.Getter, getter.PropertyType, "not a getter");
			ClassicAssert.AreEqual ("x", getter.Name.Name, "wrong name");
		}

		[Test]
		public void TestStaticFuncThunk ()
		{
			var tld = Decomposer.Decompose ("_$s21NewClassCompilerTests06Publicb4OpenB15MethodBoolFalseC5thingSbyFZTj", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "null function");
			var func = tlf.Signature as SwiftStaticFunctionThunkType;
			ClassicAssert.IsNotNull (func, "not a static thunk func");
			ClassicAssert.AreEqual ("thing", func.Name.Name, "wrong name");
			ClassicAssert.AreEqual (0, func.ParameterCount, "wrong parameter count");
			var ret = func.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (ret, "wrong return type");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, ret.BuiltInType, "not a bool");
		}

		[Test]
		public void TestAllocatorMethodDispatchThunk ()
		{
			var tld = Decomposer.Decompose ("_$s8HelloMod0A0CACycfCTj", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			ClassicAssert.IsNotNull (tlf, "null function");
			var func = tlf.Signature as SwiftConstructorThunkType;
			ClassicAssert.IsNotNull (func, "not a thunk type");
			var ret = func.ReturnType as SwiftClassType;
			ClassicAssert.IsNotNull (ret, "not a class type");
			ClassicAssert.AreEqual ("HelloMod.Hello", ret.ClassName.ToFullyQualifiedName (), "wrong class name");
			ClassicAssert.AreEqual (0, func.ParameterCount, "parameters?");
		}

		[Test]
		public void TestClassMetadataOffset ()
		{
			var tld = Decomposer.Decompose ("_$s8HelloMod0A0CMo", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLMetadataBaseOffset;
			ClassicAssert.IsNotNull (tlf, "not a metadata offset");
			ClassicAssert.AreEqual ("HelloMod.Hello", tlf.Class.ClassName.ToFullyQualifiedName (), "wrong class name");
		}

		[Test]
		public void TestMethodLookupFunction ()
		{
			var tld = Decomposer.Decompose ("_$s8HelloMod0A0CMu", false);
			ClassicAssert.NotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestUnusualEnumCase0 ()
		{
			var tld = Decomposer.Decompose ("_$s7Sampler6NumberO4RealyACSdcACmFWC", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLEnumCase;
			ClassicAssert.IsNotNull (tlf, "not an enum case");
			var func = tlf.Signature as SwiftUncurriedFunctionType;
			ClassicAssert.IsNotNull (func);
			var instanceType = func.UncurriedParameter as SwiftClassType;
			ClassicAssert.IsNotNull (instanceType, "not a class in uncurried parameter");
			ClassicAssert.AreEqual ("Sampler.Number", instanceType.ClassName.ToFullyQualifiedName (), "wrong class name");
		}

		[Test]
		public void TestUnusualEnumCase1 ()
		{
			var tld = Decomposer.Decompose ("_$s7Sampler6NumberO7IntegeryACSicACmFWC", false);
			ClassicAssert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLEnumCase;
			ClassicAssert.IsNotNull (tlf, "not an enum case");
			var func = tlf.Signature as SwiftUncurriedFunctionType;
			ClassicAssert.IsNotNull (func);
			var instanceType = func.UncurriedParameter as SwiftClassType;
			ClassicAssert.IsNotNull (instanceType, "not a class in uncurried parameter");
			ClassicAssert.AreEqual ("Sampler.Number", instanceType.ClassName.ToFullyQualifiedName (), "wrong class name");
		}
	}
}
