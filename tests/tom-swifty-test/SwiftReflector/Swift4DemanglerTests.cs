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

namespace SwiftReflector.Demangling {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class Swift4DemanglerTests {
		[Test]
		public void TestFuncReturningInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsIntSiyF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var sbt = tlf.Signature.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (sbt);
			Assert.AreEqual (CoreBuiltInType.Int, sbt.BuiltInType);

		}

		[Test]
		public void TestFuncWithIntArgsReturningInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bS2i_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}

		[Test]
		public void TestFuncWithUIntIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bS2u_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.UInt, bit.BuiltInType);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithTupleOfUIntIntIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt3arg1cS2u1a_Si1bt_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("arg", argTuple.Contents [0].Name.ToString ());
			var tuple = argTuple.Contents [0] as SwiftTupleType;
			Assert.IsNotNull (tuple);
			Assert.AreEqual (2, tuple.Contents.Count);

			var bit = tuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual ("a", bit.Name.Name);
			Assert.AreEqual (CoreBuiltInType.UInt, bit.BuiltInType);

			bit = tuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual ("b", bit.Name.Name);
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");

			Assert.AreEqual ("c", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit2");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}




		[Test]
		public void TestFuncWithBoolIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiSb_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}

		[Test]
		public void TestFuncWithFloatIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiSf_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}

		[Test]
		public void TestFuncWithDoubleIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiSd_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Double, bit.BuiltInType);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithInOutIntArgsReturningInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bS2iz_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.IsTrue (argTuple.Contents [0].IsReference);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}


		[Test]
		public void TestFuncWithClassIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA7MyClassC_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var ct = argTuple.Contents [0] as SwiftClassType;
			Assert.IsNotNull (ct, "ct");
			Assert.IsTrue (ct.IsClass);
			Assert.AreEqual ("unitHelpFrawework.MyClass", ct.ClassName.ToFullyQualifiedName (true));
			Assert.IsFalse (ct.IsReference);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithInnerStructIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA7MyClassC8InnerFooV_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var ct = argTuple.Contents [0] as SwiftClassType;
			Assert.IsNotNull (ct, "ct");
			Assert.IsTrue (ct.IsStruct);
			Assert.AreEqual ("unitHelpFrawework.MyClass.InnerFoo", ct.ClassName.ToFullyQualifiedName (true));
			Assert.IsFalse (ct.IsReference);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}


		[Test]
		public void TestFuncWithEnumIntArgsReturnInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA7FooEnumO_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var ct = argTuple.Contents [0] as SwiftClassType;
			Assert.IsNotNull (ct, "ct");
			Assert.IsTrue (ct.IsEnum);
			Assert.AreEqual ("unitHelpFrawework.FooEnum", ct.ClassName.ToFullyQualifiedName (true));
			Assert.IsFalse (ct.IsReference);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
		}

		[Test]
		public void TestFuncWithProtocolIntArgsReturnInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1a1bSiAA11BarProtocol_p_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count, "tuple count");
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString (), "arg 1 name");
			var ct = argTuple.Contents [0] as SwiftClassType;
			Assert.IsNotNull (ct, "ct");
			Assert.IsTrue (ct.IsProtocol, "isProtocol");
			Assert.AreEqual ("unitHelpFrawework.BarProtocol", ct.ClassName.ToFullyQualifiedName (true), "name match");
			Assert.IsFalse (ct.IsReference, "isReference");
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString (), "arg 2 name");
			var bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int");
		}

		[Test]
		public void TestFuncWithBoolIntReturnTupleOfDoubleInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework12ReturnsTuple1a1bSb_SitSb_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			Assert.AreEqual ("a", argTuple.Contents [0].Name.ToString ());
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			Assert.AreEqual ("b", argTuple.Contents [1].Name.ToString ());
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");

			var retTuple = tlf.Signature.ReturnType as SwiftTupleType;
			Assert.IsNotNull (retTuple);
			Assert.AreEqual (2, retTuple.Contents.Count);

			bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit2");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);

			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit3");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type1");
		}

	
		[Test]
		public void TestFuncWithOptionalIntArgsReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework10ReturnsInt1aS2iSg_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var bgt = tlf.Signature.GetParameter (0) as SwiftBoundGenericType;
			Assert.AreEqual ("a", bgt.Name.ToString (), "name matches");
			Assert.IsNotNull (bgt, "bgt");
			var baseType = bgt.BaseType as SwiftClassType;
			Assert.IsNotNull (baseType, "baseType");
			Assert.IsTrue (baseType.IsEnum, "isEnum");
			Assert.AreEqual ("Swift.Optional", baseType.ClassName.ToFullyQualifiedName (true), "is optional");
			Assert.AreEqual (1, bgt.BoundTypes.Count, "is 1 bound type");
			var bit = bgt.BoundTypes [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "is built-in type");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int");
		}


		[Test]
		public void TestClassMethodBoolReturningInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC8TestFunc1aSiSb_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("TestFunc", tlf.Name.Name);
			var ucf = tlf.Signature as SwiftUncurriedFunctionType;
			Assert.IsNotNull (ucf, "ucf");
			Assert.IsTrue (ucf.UncurriedParameter.IsClass);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.IsFalse (tlf.Signature is SwiftStaticFunctionType);
		}

		[Test]
		public void TestMethodBoolThrows()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC11WillItThrow1aySb_tKF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.CanThrow);
		}

		[Test]
		public void TestFuncBoolThrows()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework16MaybeItWillThrow1aySb_tKF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.CanThrow);
		}

		[Test]
		public void TestStructMethodBoolReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooV8TestFunc1aSiSb_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("TestFunc", tlf.Name.Name);
			var ucf = tlf.Signature as SwiftUncurriedFunctionType;
			Assert.IsNotNull (ucf, "ucf");
			Assert.IsTrue (ucf.UncurriedParameter.IsStruct);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.IsFalse (tlf.Signature is SwiftStaticFunctionType);
		}

		[Test]
		public void TestEnumMethodBoolReturningInt ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooO8TestFunc1aSiSb_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("TestFunc", tlf.Name.Name);
			var ucf = tlf.Signature as SwiftUncurriedFunctionType;
			Assert.IsNotNull (ucf, "ucf");
			Assert.IsTrue (ucf.UncurriedParameter.IsEnum);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.IsFalse (tlf.Signature is SwiftStaticFunctionType);
		}


		[Test]
		public void TestStaticClassMethodBoolReturningInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC8TestFunc1aSiSb_tFZ", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("TestFunc", tlf.Name.Name);
			var bit = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = tlf.Signature.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.IsTrue (tlf.Signature is SwiftStaticFunctionType);
		}

		[Test]
		public void TestClassNonAllocatingCtorIntBool()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1a1bACSi_Sbtcfc", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (".nctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestClassCtorIntBool()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1a1bACSi_SbtcfC", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (".ctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestStructCtorIntBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework8MyStructV1a1bACSi_SbtcfC", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (".ctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (2, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestEnumCtorIntBool ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework6MyEnumO1a1b1cACSb_SiSftcfC", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (".ctor", tlf.Name.Name);
			var argTuple = tlf.Signature.Parameters as SwiftTupleType;
			Assert.AreEqual (3, argTuple.Contents.Count);
			var bit = argTuple.Contents [0] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = argTuple.Contents [1] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = argTuple.Contents [2] as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit2");
			Assert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
		}



		[Test]
		public void TestClassDtor()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCfd", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var dtor = tlf.Signature as SwiftDestructorType;
			Assert.IsNotNull (dtor, "dtor");
			Assert.AreEqual (Decomposer.kSwiftNonDeallocatingDestructorName.Name, dtor.Name.Name);
		}


		[Test]
		public void TestClassDeallocatingDtor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCfD", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var dtor = tlf.Signature as SwiftDestructorType;
			Assert.IsNotNull (dtor, "dtor");
			Assert.AreEqual (Decomposer.kSwiftDeallocatingDestructorName.Name, dtor.Name.Name);
		}

		[Test]
		public void TestClassGetterInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1xSivg", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual ("x", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.AreEqual (PropertyType.Getter, prop.PropertyType);
		}
	
		[Test]
		public void TestClassSetterInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC1xSivs", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsFalse (prop.IsStatic);
			Assert.AreEqual ("x", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.AreEqual (PropertyType.Setter, prop.PropertyType);
		}
	
		[Test]
		public void TestGetterSubscriptIntBoolOntoFloat()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCySfSi_Sbtcig", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual ("subscript", prop.Name.Name);
			var sft = prop.OfType as SwiftFunctionType;
			Assert.IsNotNull (sft, "sft");
			Assert.AreEqual (2, sft.ParameterCount);
			var bit = sft.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = sft.GetParameter (1) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			bit = sft.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bi2");
			Assert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
		}

		[Test]
		public void TestSetterSubscriptIntBoolOntoFloat ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassCySfSi_Sbtcis", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual ("subscript", prop.Name.Name);
			var sft = prop.OfType as SwiftFunctionType;
			Assert.IsNotNull (sft, "sft");
			Assert.AreEqual (3, sft.ParameterCount);
			var bit = sft.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Float, bit.BuiltInType);
			bit = sft.GetParameter (1) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit1");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			bit = sft.GetParameter (2) as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit2");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void TestClassStaticGetterInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC6FoobleSivgZ", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsStatic);
			Assert.AreEqual ("Fooble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.AreEqual (PropertyType.Getter, prop.PropertyType);
		}
	
	
		[Test]
		public void TestClassStaticSetterInt()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC6FoobleSivsZ", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsStatic);
			Assert.AreEqual ("Fooble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType, "is int type");
			Assert.AreEqual (PropertyType.Setter, prop.PropertyType);
		}
	
	

		[Test]
		public void TestMethodTakingFunc()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7MyClassC7callFoo1ayyyXE_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftUncurriedFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual (1, func.ParameterCount);
			var funcArg = func.GetParameter (0) as SwiftFunctionType;
			Assert.IsNotNull (funcArg);
			Assert.AreEqual (0, funcArg.ParameterCount);
			Assert.AreEqual ("a", funcArg.Name.Name);
		}
	

		[Test]
		public void TestGlobalGetterBool()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7TroubleSbvg", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsFalse (prop.IsStatic);
			Assert.IsTrue (prop.IsGlobal);
			Assert.AreEqual ("Trouble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			Assert.AreEqual (PropertyType.Getter, prop.PropertyType);
		}
	
		[Test]
		public void TestGlobalSetterBool()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7TroubleSbvs", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsFalse (prop.IsStatic);
			Assert.IsTrue (prop.IsGlobal);
			Assert.AreEqual ("Trouble", prop.Name.Name);
			var bit = prop.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
			Assert.AreEqual (PropertyType.Setter, prop.PropertyType);
		}
	

		[Test]
		public void TestGlobalVariableBool()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7TroubleSbvp", false);
			Assert.IsNotNull (tld, "tld");
			var tlv = tld as TLVariable;
			Assert.IsNotNull (tlv, "tlv");
			Assert.AreEqual ("Trouble", tlv.Name.Name, "var name");
			var bit = tlv.OfType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType, "is int");
		}


		[Test]
		public void TestClassMetadataAccessor()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCMa", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var cctor = tlf.Signature as SwiftClassConstructorType;
			Assert.AreEqual (Decomposer.kSwiftClassConstructorName.Name, tlf.Name.Name);
			Assert.AreEqual ("unitHelpFrawework.Foo", tlf.Class.ClassName.ToFullyQualifiedName (true));
		}


		[Test]
		public void TestClassMetadata()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCN", false);
			Assert.IsNotNull (tld, "tld");
			var tlm = tld as TLDirectMetadata;
			Assert.IsNotNull (tlm, "tlm");
			Assert.AreEqual ("unitHelpFrawework.Foo", tlm.Class.ClassName.ToFullyQualifiedName (true));
			Assert.IsTrue (tlm.Class.IsClass);
		}

		[Test]
		public void TestNominalTypeDescriptor()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooVMn", false);
			Assert.IsNotNull (tld, "tld");
			var tln = tld as TLNominalTypeDescriptor;
			Assert.IsNotNull (tln, "tln");
			Assert.AreEqual ("unitHelpFrawework.Foo", tln.Class.ClassName.ToFullyQualifiedName (true));
			Assert.IsTrue (tln.Class.IsStruct);
		}

		[Test]
		public void TestProtocolDescriptor()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework6SummerMp", false);
			Assert.IsNotNull (tld, "tld");
			var ptd = tld as TLProtocolTypeDescriptor;
			Assert.IsNotNull (ptd, "ptd");
			Assert.AreEqual ("unitHelpFrawework.Summer", ptd.Class.ClassName.ToFullyQualifiedName (true));
		}

		[Test]
		public void TestVarInitializer()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC5waterSivpfi", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var initializer = tlf.Signature as SwiftInitializerType;
			Assert.IsNotNull (initializer, "initializer");
			Assert.AreEqual ("unitHelpFrawework.Foo", initializer.Owner.ClassName.ToFullyQualifiedName (true));
			Assert.AreEqual ("water", initializer.Name.Name);
			Assert.AreEqual (InitializerType.Variable, initializer.InitializerType);
		}

		[Test]
		public void TestLazyCacheVariable()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCML", false);
			Assert.IsNotNull (tld, "tld");
			var tllcv = tld as TLLazyCacheVariable;
			Assert.IsNotNull (tllcv, "tllcv");
			Assert.AreEqual ("unitHelpFrawework.Foo", tllcv.Class.ClassName.ToFullyQualifiedName (true));
		}

		[Test]
		public void TestProtocolWitnessTable()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooCAA6SummerAAWP", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var witness = tlf.Signature as SwiftWitnessTableType;
			Assert.IsNotNull (witness, "witness");
			Assert.AreEqual (WitnessType.Protocol, witness.WitnessType);
		}

		[Test]
		[Ignore("wasn't able to generate this")]
		public void TestProtocolWitnessAccessor()
		{
			var tld = Decomposer.Decompose ("__T05None13FooCAA6SummerAAWa", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var witness = tlf.Signature as SwiftWitnessTableType;
			Assert.IsNotNull (witness, "witness");
			Assert.AreEqual (WitnessType.ProtocolAccessor, witness.WitnessType);
		}


		[Test]
		public void TestValueWitnessTable()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework8TheThingVWV", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var witness = tlf.Signature as SwiftWitnessTableType;
			Assert.IsNotNull (witness, "witness");
			Assert.AreEqual (WitnessType.Value, witness.WitnessType);
		}

		[Test]
		public void TestGenericFuncOfT()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1ayx_tlF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.ContainsGenericParameters);
			Assert.AreEqual (1, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			Assert.IsNotNull (genericParam, "genericParam");
			Assert.AreEqual (0, genericParam.Depth, "0 depth");
			Assert.AreEqual (0, genericParam.Index, "0 index");
			Assert.AreEqual (0, func.GenericArguments [0].Constraints.Count, "0 constraints at index 0");
		}

		[Test]
		public void TestGenericFuncOfTUOneProtoConstraint()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1a1byx_q_tAA5AdderR_r0_lF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.ContainsGenericParameters);
			Assert.AreEqual (2, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			Assert.IsNotNull (genericParam, "genericParam");
			Assert.AreEqual (0, genericParam.Depth, "0 depth");
			Assert.AreEqual (0, genericParam.Index, "0 index");
			Assert.AreEqual (0, func.GenericArguments [0].Constraints.Count, "0 constraints at index 0");
			Assert.AreEqual (1, func.GenericArguments [1].Constraints.Count, "1 constraint at index 1");
			var constraint = func.GenericArguments [1].Constraints [0] as SwiftClassType;
			Assert.IsNotNull (constraint, "constraint");
			Assert.IsTrue (constraint.IsProtocol);
		}

		[Test]
		public void TestGenericFuncOfTUOneClassConstraint ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1a1byx_q_tAA3FooCRbzr0_lF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.ContainsGenericParameters);
			Assert.AreEqual (2, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			Assert.IsNotNull (genericParam, "genericParam");
			Assert.AreEqual (0, genericParam.Depth, "0 depth");
			Assert.AreEqual (0, genericParam.Index, "0 index");
			Assert.AreEqual (1, func.GenericArguments [0].Constraints.Count, "1 constraint at index 0");
			Assert.AreEqual (0, func.GenericArguments [1].Constraints.Count, "0 constraint at index 0");
			var constraint = func.GenericArguments [0].Constraints [0] as SwiftClassType;
			Assert.IsNotNull (constraint, "constraint");
			Assert.IsTrue (constraint.IsClass);
		}
	
		[Test]
		public void TestGenericFuncOfTUTwoOneProtocolConstraint ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1a1byx_q_tAA5AdderR_AA6SubberR_r0_lF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.ContainsGenericParameters);
			Assert.AreEqual (2, func.GenericArguments.Count);
			var genericParam = func.GetParameter (0) as SwiftGenericArgReferenceType;
			Assert.IsNotNull (genericParam, "genericParam");
			Assert.AreEqual (0, genericParam.Depth, "0 depth");
			Assert.AreEqual (0, genericParam.Index, "0 index");
			Assert.AreEqual (2, func.GenericArguments [1].Constraints.Count, "2 constraints at index 1");
			Assert.AreEqual (0, func.GenericArguments [0].Constraints.Count, "0 constraints at index 0");
			var constraint = func.GenericArguments [1].Constraints [0] as SwiftClassType;
			Assert.IsNotNull (constraint, "constraint");
			Assert.IsTrue (constraint.IsProtocol);
			constraint = func.GenericArguments [1].Constraints [1] as SwiftClassType;
			Assert.IsNotNull (constraint, "constraint");
			Assert.IsTrue (constraint.IsProtocol);
		}

		[Test]
		public void TestGenericFuncOfGenericClass()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework7doPrint1ayAA3FooCyxG_tlF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var func = tlf.Signature as SwiftFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.IsTrue (func.ContainsGenericParameters);
			Assert.AreEqual (1, func.GenericArguments.Count, "1 gen arg");
			var genericParam = func.GetParameter (0) as SwiftBoundGenericType;
			Assert.AreEqual (1, genericParam.BoundTypes.Count, "1 bound type");
			var genericParamType = genericParam.BoundTypes [0] as SwiftGenericArgReferenceType;
			Assert.IsNotNull (genericParamType, "genericParamType");
			Assert.AreEqual (0, genericParamType.Depth, "0 depth");
			Assert.AreEqual (0, genericParamType.Index, "0 index");
		}

		[Test]
		public void TestOperatorEqEq()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC2eeoiySbAC_ACtFZ", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (OperatorType.Infix, tlf.Operator, "operator");
			Assert.AreEqual ("==", tlf.Name.Name);
		}


		[Test]
		public void TestOperatorMinusPlusMinus()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3spsoiyS2i_SitF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (OperatorType.Infix, tlf.Operator, "operator");
			Assert.AreEqual ("-+-", tlf.Name.Name);
		}

		[Test]
		public void TestUnicodeOperator()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework008deiFBEEeopyS2bF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ('\u2757', tlf.Name.Name[0]);
		}

		[Test]
		public void TestAnyObject()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3boo1byyXl_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (1, tlf.Signature.ParameterCount);
			var cl = tlf.Signature.GetParameter (0) as SwiftClassType;
			Assert.IsNotNull (cl, "cl");
			Assert.AreEqual ("Swift.AnyObject", cl.ClassName.ToFullyQualifiedName ());
			Assert.IsTrue (cl.IsClass);
		}
	

		[Test]
		public void TestAny()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3foo1byyp_tF", false);
			Assert.IsNotNull (tld, "tld");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (1, tlf.Signature.ParameterCount, "1 parameter");
			var cl = tlf.Signature.GetParameter (0) as SwiftClassType;
			Assert.IsNotNull (cl, "cl");
			Assert.AreEqual ("Swift.Any", cl.ClassName.ToFullyQualifiedName ());
			Assert.IsTrue (cl.IsProtocol);
		}

		[Test]
		public void TestOptionalCtor ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework3FooC4failACSgSb_tcfc", false);
			Assert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "Expected function");
			Assert.IsTrue (tlf.Signature.IsConstructor, "Expected constructor");
			var ctorReturn = tlf.Signature.ReturnType as SwiftBoundGenericType;
			Assert.IsNotNull (ctorReturn, "Expected bound generic return");
			var payload = ctorReturn.BoundTypes [0] as SwiftClassType;
			Assert.IsNotNull (ctorReturn, "Expected class");
			Assert.AreEqual ("unitHelpFrawework.Foo", payload.ClassName.ToFullyQualifiedName (true), "Expected None.Foo");
			Assert.IsTrue (tlf.Signature.IsOptionalConstructor, "Not an optional ctor");
		}

		[Test]
		public void TestStaticExtensionFunc ()
		{
			var tld = Decomposer.Decompose ("_$sSi17unitHelpFraweworkE3fooSiyFZ", false);
			Assert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "Expected function");
			Assert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			Assert.IsTrue (tlf.Signature is SwiftStaticFunctionType, "Expected static function");
			var scalar = tlf.Signature.ExtensionOn as SwiftBuiltInType;
			Assert.IsNotNull (scalar, "Expected swift built in type");
			Assert.AreEqual (CoreBuiltInType.Int, scalar.BuiltInType, "Expected an Int");
		}


		[Test]
		public void TestStaticExtensionProp ()
		{
			var tld = Decomposer.Decompose ("_$sSi17unitHelpFraweworkE3fooSivgZ", false);
			Assert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "Expected function");
			Assert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			Assert.IsTrue (tlf.Signature is SwiftPropertyType, "Expected property");
			var scalar = tlf.Signature.ExtensionOn as SwiftBuiltInType;
			Assert.IsNotNull (scalar, "Expected swift built in type");
			Assert.AreEqual (CoreBuiltInType.Int, scalar.BuiltInType, "Expected an Int");
		}


		[Test]
		public void TestGenericExtensionOnFunc ()
		{
			var tld = Decomposer.Decompose ("_$sSb17unitHelpFraweworkE5truth1aSSx_tAA6TruthyRzlF", false);
			Assert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "Expected function");
			Assert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var scalar = tlf.Signature.ExtensionOn as SwiftBuiltInType;
			Assert.IsNotNull (scalar, "Expected swift built int type");
			Assert.AreEqual (CoreBuiltInType.Bool, scalar.BuiltInType, "Expected a bool");
		}


		[Test]
		public void TestExtensionSubscript ()
		{
			var tld = Decomposer.Decompose ("_$sSa17unitHelpFraweworkAA6TruthyRzlEyxSgSScig", false);
			Assert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "Expected function");
			Assert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "Expected property");
			Assert.IsTrue (prop.IsSubscript, "Expected subscript");
		}

		[Test]
		public void TestEulerOperatorDemangle ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework003BehopyySbyXKF", false);
			Assert.IsNotNull (tld, "Failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "Expected function");
		}


		[Test]
		public void TestObjCTLFunction ()
		{
			var tld = Decomposer.Decompose ("_$s17unitHelpFrawework21PathPositionAnimationC014createKeyframeF033_3D85A716E8AC30D62D97E78DB643A23DLLyyF", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "expected function");
			Assert.AreEqual (tlf.Name.Name, "_3D85A716E8AC30D62D97E78DB643A23D", "name mistmatch.");

		}

		[Test]
		public void TestMaterializerExtension1 ()
		{
			var tld = Decomposer.Decompose ("__T0So6UIViewC12RazzleDazzleE14scaleTransformSC08CGAffineE0VSgfm", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "expected function");
			Assert.AreEqual (tlf.Name.Name, "scaleTransform", "name mismatch");
		}

		[Test]
		[Ignore ("AFAIK, Swifft 5 doesn't generate materializers")]
		public void TestMaterializerExtension2 ()
		{
			var tld = Decomposer.Decompose ("", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "expected function");
			Assert.AreEqual (tlf.Name.Name, "rotationTransform", "name mismatch");
		}

		[Test]
		[Ignore ("AFAIK, Swifft 5 doesn't generate materializers")]
		public void TestMaterializerExtension3 ()
		{
			var tld = Decomposer.Decompose ("", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "expected function");
			Assert.AreEqual (tlf.Name.Name, "translationTransform", "name mismatch");
		}

		[Test]
		public void TestProtocolConformanceDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s7XamGlue18xam_proxy_HashableCSQAAMc", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlprot = tld as TLProtocolConformanceDescriptor;
			Assert.IsNotNull (tlprot, "not a protocol conformance descriptor");
			Assert.IsNotNull (tlprot.ImplementingType, "no class");
			var cl = tlprot.ImplementingType as SwiftClassType;
			Assert.IsNotNull (cl);
			Assert.AreEqual ("XamGlue.xam_proxy_Hashable", cl.ClassName.ToFullyQualifiedName (), "wrong implementor");
			Assert.AreEqual ("Swift.Equatable", tlprot.Protocol.ClassName.ToFullyQualifiedName (), "wrong protocol");
		}

		[Test]
		public void TestTypeAlias ()
		{
			var tld = Decomposer.Decompose ("_$s13NSObjectTests21SomeVirtualClassmacOSC13StringVersion1vSSSo017NSOperatingSystemH0a_tF", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			var onlyArg = tlf.Signature.GetParameter (0) as SwiftClassType;
			Assert.IsNotNull (onlyArg, "not a class type arg");
			Assert.AreEqual ("Foundation.OperatingSystemVersion", onlyArg.ClassName.ToFullyQualifiedName (), "name mistmatch");
		}


		[Test]
		public void TestPropertyExtension ()
		{
			var tld = Decomposer.Decompose ("_$sSf10CircleMenuE7degreesSfvpMV", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLPropertyDescriptor;
			Assert.IsNotNull (tlf, "not a property descriptor");
			Assert.IsNotNull (tlf.ExtensionOn, "no extension");
			Assert.IsNull (tlf.Class, "has a class?!");
		}

		[Test]
		public void TestFieldOffsetExtension ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw16SWXMLHashOptionsC8encodingSS10FoundationE8EncodingVvpWvd", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFieldOffset;
			Assert.IsNotNull (tlf, "wrong type");
		}

		[Test]
		public void TestUIColorExtension0 ()
		{
			var tld = Decomposer.Decompose ("_$sSo7UIColorC3HueE3hexABSS_tcfC", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			Assert.IsTrue (tlf.Signature.IsConstructor, "not a constructor");
			Assert.IsNotNull (tlf.Signature.ExtensionOn, "no extension");
		}


		[Test]
		public void TestUIColorExtension1 ()
		{
			var tld = Decomposer.Decompose ("_$sSo7UIColorC8MaterialE4argbABs6UInt32V_tcfC", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			Assert.IsTrue (tlf.Signature.IsConstructor, "not a constructor");
			Assert.IsNotNull (tlf.Signature.ExtensionOn, "no extension");
		}

		[Test]
		public void TestUIColorExtension2 ()
		{
			var tld = Decomposer.Decompose ("_$sSo7UIColorC12DynamicColorE3hue10saturation9lightness5alphaAB12CoreGraphics7CGFloatV_A3JtcfC", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			Assert.IsTrue (tlf.Signature.IsConstructor, "not a constructor");
			Assert.IsNotNull (tlf.Signature.ExtensionOn, "no extension");
		}

		[Test]
		public void TestProtocolRequirementsDescriptor0 ()
		{
			var tld = Decomposer.Decompose ("_$s14DateTimePicker0abC8DelegateTL", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLProtocolRequirementsBaseDescriptor;
			Assert.IsNotNull (tlf, "not a protocol requirements base descriptor");
			Assert.AreEqual ("DateTimePicker.DateTimePickerDelegate", tlf.Class.ClassName.ToFullyQualifiedName (true), "wrong name");
		}

		[Test]
		public void TestProtocolRequirementsDescriptor1 ()
		{
			var tld = Decomposer.Decompose ("_$s4Neon9FrameableTL", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLProtocolRequirementsBaseDescriptor;
			Assert.IsNotNull (tlf, "not a protocol requirements base descriptor");
			Assert.AreEqual ("Neon.Frameable", tlf.Class.ClassName.ToFullyQualifiedName (true), "wrong name");
		}

		[Test]
		public void TestNREInMacaw ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw5ShapeC11interpolate_8progressACXDAC_SdtF", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			Assert.AreEqual ("interpolate", tlf.Signature.Name.Name, "wrong name");
		}

		public void BaseReqTest (string mangle, string protoName, string reqName)
		{
			var tld = Decomposer.Decompose (mangle, false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLBaseConformanceDescriptor;
			Assert.IsNotNull (tlf, "not a base conformance descriptor");
			Assert.AreEqual (protoName, tlf.Class.ClassName.ToFullyQualifiedName (true), "wrong name");
			Assert.AreEqual (reqName, tlf.ProtocolRequirement.ClassName.ToFullyQualifiedName (true), "wrong requirement");
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
			Assert.IsNotNull (tld, "failed descriptor");
		}

		[Test]
		public void TestFieldOffsetContainsClass ()
		{
			var tld = Decomposer.Decompose ("_$s5Macaw13ChangeHandlerC6handleyyxcvpWvd", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var fieldOffset = tld as TLFieldOffset;
			Assert.IsNotNull (fieldOffset, "field offset");
			var cl = fieldOffset.Class;
			Assert.IsNotNull (cl, "null class");
		}

		[Test]
		public void TestPrivateInitializer0 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka9_AlertRowC0029presentationModestorage_rAFJh33_D25096F98D3944FE1BCE11D750532E6DLLAA16PresentationModeOyAA08SelectorB10ControllerCyACyxGGGSgSgvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestPrivateInitializer1 ()
		{
			var tld = Decomposer.Decompose ("_$s15JTAppleCalendar0aB4ViewC0020theDatastorage_mdAJd33_70DF286E0E62C56975265F8CF5A8FF56LLAA0B4DataVSgvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestPrivateInitializer2 ()
		{
			var tld = Decomposer.Decompose ("_$s8Presentr0A10ControllerC0030shouldSwipeBottomstorage_cDAEi33_9D6ACB2CCC4A4980BDBB65F0F301220BLLSbSgvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
		}

		[Test]
		public void TestExtensionInit ()
		{
			var tld = Decomposer.Decompose ("_$sSd6EurekaE6stringSdSgSS_tcfC", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			Assert.IsNotNull (tlf.Signature.ExtensionOn, "no extension?");
			var swiftClassType = tlf.Signature.ExtensionOn as SwiftClassType;
			Assert.IsNotNull (swiftClassType, "extension is not a class?");
			Assert.AreEqual ("Swift.Double", swiftClassType.ClassName.ToFullyQualifiedName (), "bad name");
		}

		[Test]
		public void TestVarInInitializer0 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka22_TriplePickerInlineRowC13secondOptionsySayq_Gxcvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer1 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka22_TriplePickerInlineRowC12thirdOptionsySayq0_Gx_q_tcvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer2 ()
		{
			var tld = Decomposer.Decompose ("_$s6Eureka22_DoublePickerInlineRowC13secondOptionsySayq_Gxcvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer3 ()
		{
			var tld = Decomposer.Decompose ("_$s11PaperSwitch08RAMPaperB0C24animationDidStartClosureyySbcvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestVarInInitializer4 ()
		{
			var tld = Decomposer.Decompose ("_$s11PaperSwitch08RAMPaperB0C23animationDidStopClosureyySb_Sbtcvpfi", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestClosureDescriptor ()
		{
			var tld = Decomposer.Decompose ("_$s9FaceAware14ClosureWrapperC7closureyyxcvsTq", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestClosurePropertyDesc ()
		{
			var tld = Decomposer.Decompose ("_$s18XLActionController8CellSpecO6heighty12CoreGraphics7CGFloatVq_cvg", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
		}

		[Test]
		public void TestStaticVariable ()
		{
			var tld = Decomposer.Decompose ("_$s8Material5ThemeV4fontAA8FontType_pXpvMZ", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			Assert.AreEqual ("font", tlf.Name.Name);
		}

		[Test]
		public void TestStaticAddressorVariable ()
		{
			var tld = Decomposer.Decompose ("_$s8Material5ThemeV4fontAA8FontType_pXpvau", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlu = tld as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlu, "not an addressor");
			Assert.AreEqual ("font", tlu.Name.Name);
		}

		[Test]
		public void TestStaticVariable1 ()
		{
			var tld = Decomposer.Decompose ("_$s8Material5ThemeV4fontAA8FontType_pXpvpZ", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLVariable;
			Assert.IsNotNull (tlf, "not a function");
			Assert.AreEqual ("font", tlf.Name.Name);
		}

		[Test]
		public void TestPATReference ()
		{
			var tld = Decomposer.Decompose ("_$s24ProtocolConformanceTests9doSetProp1a1byxz_4ItemQztAA9Simplest3RzlF", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			var arg2 = tlf.Signature.GetParameter (1) as SwiftGenericArgReferenceType;
			Assert.IsNotNull (arg2, "Not an SLGenericReference");
			Assert.IsTrue (arg2.HasAssociatedTypePath, "No associated type path");
			Assert.AreEqual (1, arg2.AssociatedTypePath.Count, "wrong number of assoc type path elements");
			Assert.AreEqual ("Item", arg2.AssociatedTypePath [0], "Mismatch in assoc type name");
		}

		[Test]
		public void TestPATPathReference ()
		{
			var tld = Decomposer.Decompose ("_$s15BadAssociations7doPrint1a1byx_5Thing_4NameQZtAA13PrintableItemRzlF", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			var arg2 = tlf.Signature.GetParameter (1) as SwiftGenericArgReferenceType;
			Assert.IsNotNull (arg2, "Not an SLGenericReference");
			Assert.IsTrue (arg2.HasAssociatedTypePath, "No associated type path");
			Assert.AreEqual (2, arg2.AssociatedTypePath.Count, "wrong number of assoc type path elements");
			Assert.AreEqual ("Thing", arg2.AssociatedTypePath [0], "Mismatch in assoc type name 0");
			Assert.AreEqual ("Name", arg2.AssociatedTypePath [1], "Mismatch in assoc type name 1");
		}

		[Test]
		public void TestGenericMetatype ()
		{
			var tld = Decomposer.Decompose ("_$sSD14ExtensionTestsE5value6forKey6ofTypeqd__SgSS_qd__mtlF", false);
			Assert.IsNotNull (tld, "failed decomposition");
			var tlf = tld as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			var arg0 = tlf.Signature.GetParameter (0) as SwiftClassType;
			Assert.IsNotNull (arg0, "not a swift class type at arg0");
			var arg1 = tlf.Signature.GetParameter (1) as SwiftMetaClassType;
			Assert.IsNotNull (arg1, "not a metaclass type");
			Assert.IsNotNull (arg1.ClassGenericReference, "not a generic reference metatype");
		}
	}
}
