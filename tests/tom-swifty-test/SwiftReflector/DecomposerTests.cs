// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using tomwiftytest;
using SwiftReflector;
using SwiftReflector.Demangling;
using Xamarin;
using Dynamo.CSLang;
using Dynamo;
using SwiftReflector.TypeMapping;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class DecomposerTests {
		[Test]
		public void DecomposeSmokeTest ()
		{
			var func = Decomposer.Decompose ("_$s3foo6AClassC3barSiycyF", true) as TLFunction;
			var uncurriedFunc = func.Signature as SwiftUncurriedFunctionType;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "modulename");
			Assert.AreEqual (1, func.Class.ClassName.Nesting.Count, "nesting count");
			Assert.AreEqual ("AClass", func.Class.ClassName.NestingNames [0].Name, "AClass");
			Assert.AreEqual ("bar", func.Name.Name, "bar");
			Assert.IsNotNull (uncurriedFunc, "uncurriedFunc");
		}

		[Test]
		public void HasNLEntries ()
		{
			var stm = MachOTests.HelloSwiftAsLibrary (null);

			var entries = SymbolVisitor.Entries (stm).ToList ();
			Assert.AreEqual (1, entries.Count (), "1 entry");
			var isSwift3Str = entries [0].Entry.str == "__TF6noname4mainFT_SS";
			var isSwift4Str = entries [0].Entry.str == "__T06noname4mainSSyF";
			var isSwift5Str = entries [0].Entry.str == "_$s6noname4mainSSyF";
			Assert.IsTrue (isSwift3Str || isSwift4Str || isSwift5Str, "matches a platform");
		}

		[Test]
		public void PunyCode ()
		{
			Assert.AreEqual (Char.ConvertFromUtf32 (0x1F49B), "GrIh".DePunyCode ());
		}

		[Test]
		public void TestFuncVoidReturningVoid ()
		{
			var func = Decomposer.Decompose ("_$s3foo6lonelyyyF", true) as TLFunction;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "module");
			Assert.IsNotNull (func.Signature, "signature");
			Assert.IsTrue (func.Signature.IsVoid, "IsVoid");
			Assert.AreEqual (CoreCompoundType.Tuple, func.Signature.ReturnType.Type, "Is tuple");
			Assert.IsTrue (((SwiftTupleType)func.Signature.ReturnType).IsEmpty, "Is empty tuple");
		}

		[Test]
		public void TestEmptyConstructor ()
		{
			var func = Decomposer.Decompose ("_$s3foo5JuliaCACycfC"/*"__TFC3foo5JuliaCfMS0_FT_S0_"*/, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "module");
			Assert.IsNotNull (func.Signature, "signature");
			Assert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var cons = func.Signature as SwiftConstructorType;
			Assert.AreEqual (func.Class, cons.ReturnType, "is a class");
			Assert.AreEqual (CoreCompoundType.Tuple, func.Signature.Parameters.Type, "is a tuple");
			Assert.IsTrue (((SwiftTupleType)func.Signature.Parameters).IsEmpty, "is empty tuple");
		}

		[Test]
		public void TestIntConstructor ()
		{
			var func = Decomposer.Decompose ("_$s3foo5JuliaC5stuffACSi_tcfc", true) as TLFunction;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "module");
			Assert.IsNotNull (func.Signature, "signature");
			Assert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var cons = func.Signature as SwiftConstructorType;
			Assert.AreEqual (func.Class, cons.ReturnType, "is a class");
			var bit = func.Signature.GetParameter (0) as SwiftBuiltInType;
			Assert.NotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType);
		}

		[Test]
		public void TestClassConstructor ()
		{
			var func = Decomposer.Decompose ("_$s3foo5JuliaCMa", true) as TLFunction;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "module");
			Assert.IsNotNull (func.Signature, "signature");
			Assert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var cctor = func.Signature as SwiftClassConstructorType;
			Assert.IsNotNull (cctor, "cctor");
			var mct = cctor.ReturnType as SwiftMetaClassType;
			Assert.AreEqual (func.Class, mct.Class, "class type");
			Assert.AreEqual (CoreCompoundType.Tuple, func.Signature.Parameters.Type, "is a tuple");
			Assert.IsTrue (((SwiftTupleType)func.Signature.Parameters).IsEmpty, "is empty");
		}

		void TestFunc3XXXReturningVoid (string funcmangle, CoreBuiltInType csv)
		{
			var func = Decomposer.Decompose (funcmangle, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "module");
			Assert.IsNotNull (func.Signature, "signature");
			Assert.IsTrue (func.Signature.IsVoid, "IsVoid");
			var parms = func.Signature.Parameters;
			Assert.IsNotNull (parms, "parms");
			var tt = parms as SwiftTupleType;
			Assert.IsNotNull (tt, "tt");
			Assert.AreEqual (3, tt.Contents.Count, "tuple size");
			foreach (SwiftType st in tt.Contents) {
				SwiftBuiltInType scalar = st as SwiftBuiltInType;
				Assert.IsNotNull (scalar, "scalar");
				Assert.AreEqual (csv, scalar.BuiltInType, "scalar type");
			}
		}


		[Test]
		public void TestFunc3IntsReturningVoid ()
		{
			TestFunc3XXXReturningVoid ("_$s3foo6lonely1i1j1kySi_S2itF", CoreBuiltInType.Int);
		}

		[Test]
		public void TestFunc3BoolsReturningVoid ()
		{
			TestFunc3XXXReturningVoid ("_$s3foo6lonely1i1j1kySb_S2btF", CoreBuiltInType.Bool);
		}

		[Test]
		public void TestFunc3FloatsReturningVoid ()
		{
			TestFunc3XXXReturningVoid ("_$s3foo6lonely1i1j1kySf_S2ftF", CoreBuiltInType.Float);
		}


		[Test]
		public void TestFunc3DoublesReturningVoid ()
		{
			TestFunc3XXXReturningVoid ("_$s3foo6lonely1i1j1kySd_S2dtF", CoreBuiltInType.Double);
		}


		[Test]
		public void TestFunc3UIntsReturningVoid ()
		{
			TestFunc3XXXReturningVoid ("_$s3foo6lonely1i1j1kySu_S2utF", CoreBuiltInType.UInt);
		}


		void TestFuncReturningFoo (string funcMangle, CoreBuiltInType cbt)
		{
			var func = Decomposer.Decompose (funcMangle, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			Assert.AreEqual ("foo", func.Module.Name, "module");
			Assert.IsNotNull (func.Signature, "signature");
			Assert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var ret = func.Signature.ReturnType;

			var st = ret as SwiftBuiltInType;
			Assert.IsNotNull (st, "st");
			Assert.AreEqual (cbt, st.BuiltInType, "matches type");
		}


		[Test]
		public void TestFuncReturningInt ()
		{
			TestFuncReturningFoo ("_$s3foo6nonameSiyF", CoreBuiltInType.Int);
		}


		[Test]
		public void TestFuncReturningBool ()
		{
			TestFuncReturningFoo ("_$s3foo6nonameSbyF", CoreBuiltInType.Bool);
		}

		[Test]
		public void TestFuncReturningUInt ()
		{
			TestFuncReturningFoo ("_$s3foo6nonameSuyF", CoreBuiltInType.UInt);
		}

		[Test]
		public void TestFuncReturningFloat ()
		{
			TestFuncReturningFoo ("_$s3foo6nonameSfyF", CoreBuiltInType.Float);
		}

		[Test]
		public void TestFuncReturningDouble ()
		{
			TestFuncReturningFoo ("_$s3foo6nonameSdyF", CoreBuiltInType.Double);
		}

		static void BuiltInTypeIsA (SwiftType t, CoreBuiltInType ct)
		{
			SwiftBuiltInType bit = t as SwiftBuiltInType;
			if (bit == null)
				Assert.Fail ("Not a SwiftBuiltInType: " + t.GetType ().Name);
			Assert.AreEqual (ct, bit.BuiltInType, "same built in type");
		}

		[Test]
		public void TestSimpleArrayOfInt ()
		{
			var func = Decomposer.Decompose ("_$s3foo6nonameyySaySiGF", true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var gentype = func.Signature.Parameters as SwiftBoundGenericType;
			Assert.IsNotNull (gentype, "gentype");
			Assert.IsTrue (gentype.BaseType is SwiftClassType, "is a class type");
			var sct = gentype.BaseType as SwiftClassType;
			Assert.AreEqual ("Swift.Array", sct.ClassName.ToFullyQualifiedName (true), "is Swift.Array");
			Assert.AreEqual (1, gentype.BoundTypes.Count, "1 bound type");
			BuiltInTypeIsA (gentype.BoundTypes [0], CoreBuiltInType.Int);
		}

		[Test]
		public void TestSimpleArrayOfArrayOfInt ()
		{
			var func = Decomposer.Decompose ("_$s3foo6nonameyySaySaySiGGF", true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var gentype = func.Signature.Parameters as SwiftBoundGenericType;
			Assert.IsNotNull (gentype, "gentype");
			Assert.IsTrue (gentype.BaseType is SwiftClassType, "is class");
			var sct = gentype.BaseType as SwiftClassType;
			Assert.AreEqual ("Swift.Array", sct.ClassName.ToFullyQualifiedName (true), "is array");

			Assert.AreEqual (1, gentype.BoundTypes.Count);
			Assert.IsTrue (gentype.BoundTypes [0] is SwiftBoundGenericType, "is generic");
			gentype = gentype.BoundTypes [0] as SwiftBoundGenericType;
			Assert.IsTrue (gentype.BaseType is SwiftClassType, "is class");
			sct = gentype.BaseType as SwiftClassType;
			Assert.AreEqual ("Swift.Array", sct.ClassName.ToFullyQualifiedName (true), "is array");

			Assert.AreEqual (1, gentype.BoundTypes.Count);
			BuiltInTypeIsA (gentype.BoundTypes [0], CoreBuiltInType.Int);
		}

		[Test]
		public void TestSimpleDictIntOnBool ()
		{
			var func = Decomposer.Decompose ("_$s3foo6nonameyySDySiSbGF", true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var gentype = func.Signature.Parameters as SwiftBoundGenericType;
			Assert.IsNotNull (gentype, "gentype");
			Assert.IsTrue (gentype.BaseType is SwiftClassType, "is class");
			Assert.AreEqual (2, gentype.BoundTypes.Count, "2 bound types");
			BuiltInTypeIsA (gentype.BoundTypes [0], CoreBuiltInType.Int);
			BuiltInTypeIsA (gentype.BoundTypes [1], CoreBuiltInType.Bool);
		}


		[Test]
		public void TestTLFunctionUsingClass ()
		{
			var funcName = "_$s17unitHelpFrawework13xamarin_MontyC3valyyAA0E0CF";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
		}


		[Test]
		public void TestTLFunctionPublicGetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC8somePropSivg";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPublic, "IsPublic");
			Assert.AreEqual (PropertyType.Getter, prop.PropertyType, "PropertyType");
			Assert.IsNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPublicSetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC8somePropSivs";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPublic, "IsPublic");
			Assert.AreEqual (PropertyType.Setter, prop.PropertyType, "PropertyType");
			Assert.IsNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		[Ignore("haven't been able to generate a materializer in swift 5 yet.")]
		public void TestTLFunctionPublicMaterializer ()
		{
			var funcName = "";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPublic, "IsPublic");
			Assert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
			Assert.IsNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPublicModifyAccessor ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC8somePropSivM";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPublic, "public");
			Assert.AreEqual (PropertyType.ModifyAccessor, prop.PropertyType, "is modify accessor");
			Assert.IsNull (prop.PrivateName, "no private name");
		}

		[Test]
		[Ignore ("haven't been able to generate a materializer in swift 5 yet.")]
		public void TestTLFunctionPublicMaterializer1 ()
		{
			var funcName = "";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
		}


		[Test]
		public void TestTLFunctionPrivateGetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC2_x33_3D85A716E8AC30D62D97E78DB643A23DLLSivg";//"__TFC5None14NonegP33_8C43D7A2FD5ECCB447AC5E0DDCF4B73C10someBackerSi\n";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPrivate, "IsPrivate");
			Assert.AreEqual (PropertyType.Getter, prop.PropertyType, "PropertyType");
			Assert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPrivateSetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC2_x33_3D85A716E8AC30D62D97E78DB643A23DLLSivs";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPrivate, "IsPrivate");
			Assert.AreEqual (PropertyType.Setter, prop.PropertyType, "PropertyType");
			Assert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		[Ignore ("haven't been able to generate a materializer in swift 5 yet.")]
		public void TestTLFunctionPrivateMaterializer ()
		{
			var funcName = "__TFC5None14NonemP33_8C43D7A2FD5ECCB447AC5E0DDCF4B73C10someBackerSi";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPrivate, "IsPrivate");
			Assert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
			Assert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPrivateModifyAccessor ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC2_x33_3D85A716E8AC30D62D97E78DB643A23DLLSivM";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			Assert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsPrivate);
			Assert.AreEqual (PropertyType.ModifyAccessor, prop.PropertyType, "PropertyType");
			Assert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void HasMono64 ()
		{
			if (!File.Exists (Compiler.kMono64Path))
				Assert.Fail ("unable to find mono64 at location " + Compiler.kMono64Path);
		}


		[Test]
		public void TestFuncOfClassReturningClassInSameModule ()
		{
			var func = "_$s17unitHelpFrawework13xamarin_MontyC4doItyAA6GarbleCAA0E0CF";
			var tlf = Decomposer.Decompose (func, true) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var bft = tlf.Signature as SwiftBaseFunctionType;
			Assert.IsNotNull (bft, "bft");
			var sct = bft.ReturnType as SwiftClassType;
			Assert.IsNotNull (sct, "sct");
			Assert.AreEqual ("unitHelpFrawework.Garble", sct.ClassName.ToFullyQualifiedName ());
		}


		[Test]
		public void TestFuncWithInOutArgument ()
		{
			var func = "_$s17unitHelpFrawework9OneStructV9mutateVaryySizF";
			var tlf = Decomposer.Decompose (func, true) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var bft = tlf.Signature as SwiftBaseFunctionType;
			Assert.IsNotNull (bft, "bft");
			var argType = bft.Parameters as SwiftBuiltInType;
			Assert.IsNotNull (argType, "argType");
			Assert.IsTrue (argType.IsReference, "IsReference");
		}

		[Test]
		public void TestStructMeta ()
		{
			var func = "_$s17unitHelpFrawework7AStructVN";
			var def = Decomposer.Decompose (func, true) as TLDefinition;
			Assert.IsNotNull (def, "def");
		}


		[Test]
		public void DecomposeStructConstructor ()
		{
			var func = "_$s17unitHelpFrawework7AStructVACycfC";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature is SwiftConstructorType, "is constructor");
		}

		[Test]
		public void DecomposeGlobalAddressor ()
		{
			var func = "_$s17unitHelpFrawework7aGlobalSbvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsNotNull (tlf.OfType, "OfType");
		}

		[Test]
		public void DecomposeGlobalVariable ()
		{
			var func = "_$s17unitHelpFrawework7aGlobalSbvp";
			var vari = Decomposer.Decompose (func, false) as TLVariable;
			Assert.IsNotNull (vari, "vari");
		}



		[Test]
		public void DecomposeFunctionOfEnum ()
		{
			var func = "_$s17unitHelpFrawework10printFirstyyAA0E0OF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.ReturnType.IsEmptyTuple, "is empty tuple");
			var ct = tlf.Signature.Parameters as SwiftClassType;
			Assert.IsNotNull (ct, "ct");
			Assert.IsTrue (ct.EntityKind == MemberNesting.Enum, "is enum");
			Assert.AreEqual ("First", ct.ClassName.Terminus.Name, "is First");
		}

		[Test]
		public void DemcomposeCConventionCall ()
		{
			var func = "_$s17unitHelpFrawework12callSomeFuncyyyyXCF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.Parameters is SwiftCFunctionPointerType, "not a c function pointer type");
		}

		[Test]
		public void DecomposeUnsafePointer ()
		{
			// NB: this function used UnsafePointer<()> which is deprecated.
			var func = "_$s17unitHelpFrawework19setMonty_xam_vtable_3uvtyAA0f5_sub_E0V_SPyytGtF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			SwiftType bft = tlf.Signature.GetParameter (1);
			Assert.IsNotNull (bft, "bft");
			Assert.IsTrue (bft is SwiftBoundGenericType, "is SwiftBoundGeneric");
			var gen = bft as SwiftBoundGenericType;
			SwiftClassType baseType = gen.BaseType as SwiftClassType;
			Assert.NotNull (baseType, "baseType");
			Assert.AreEqual ("UnsafePointer", baseType.ClassName.Terminus.Name, "UnsafePointer");
		}


		[Test]
		public void DemcomposeReturnsInt64 ()
		{
			var func = "_$s17unitHelpFrawework5MontyC3vals5Int64VyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.ReturnType is SwiftClassType, "is swift class type");
		}

		[Test]
		public void DecomposeProtocol ()
		{
			var func = "_$s17unitHelpFrawework11ThisIsaFunc_1xs5Int64VAA7MyProto_p_AEtF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.ParameterCount == 2, "parameter count");
			var prot = tlf.Signature.GetParameter (0) as SwiftClassType;
			Assert.IsNotNull (prot, "prot");
			Assert.AreEqual (MemberNesting.Protocol, prot.EntityKind, "is protocol");
		}


		[Test]
		public void DecomposeSimpleGeneric ()
		{
			var func = "_$s17unitHelpFrawework3foo_1b1c1dyx_S3itlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (4, tlf.Signature.ParameterCount, "parameter count");
			Assert.IsTrue (tlf.Signature.ContainsGenericParameters, "contains generic parameters");
			Assert.AreEqual (1, tlf.Signature.GenericArguments.Count (), "generic arguments count");
		}

		[Test]
		public void DecomposeMultiGeneric ()
		{
			var func = "_$s17unitHelpFrawework3foo1a1b1c1dyx_q_q0_q1_tr2_lF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (4, tlf.Signature.ParameterCount, "parameter count");
			Assert.IsTrue (tlf.Signature.ContainsGenericParameters, "contains generic parameters");
			Assert.AreEqual (4, tlf.Signature.GenericArguments.Count (), "generic argument count");
		}

		[Test]
		public void DecomposeMetadataPattern ()
		{
			var func = "_$s17unitHelpFrawework3FooCMP";
			var gmp = Decomposer.Decompose (func, false) as TLGenericMetadataPattern;
			Assert.IsNotNull (gmp, "gmp");
			Assert.AreEqual ("unitHelpFrawework.Foo", gmp.Class.ClassName.ToFullyQualifiedName (true), "classname");
		}


		[Test]
		public void DecomposeStaticProp ()
		{
			var func = "_$s17unitHelpFrawework11aFinalClassC11aStaticPropSbvpZ";

			var tlv = Decomposer.Decompose (func, false) as TLVariable;
			Assert.IsNotNull (tlv, "tlv");
			Assert.IsTrue (tlv.IsStatic, "IsStatic");
			Assert.IsNotNull (tlv.Class, "Class");
		}

		[Test]
		public void DecomposeMultiProtoConstraints ()
		{
			var func = "_$s17unitHelpFrawework16xamarin_FooDuppy3foo1ayAA0E0CyxG_xtAA6DownerRzAA5UpperRzlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (1, tlf.Signature.GenericArguments.Count, "genric argument count");
			Assert.AreEqual (2, tlf.Signature.GenericArguments [0].Constraints.Count, "constraints count");
		}

		[Test]
		public void DecomposeVariableInitializer ()
		{
			var func = "_$s17unitHelpFrawework5MontyC3valSbvpfi";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			SwiftInitializerType sit = tlf.Signature as SwiftInitializerType;
			Assert.IsNotNull (sit, "sit");
			Assert.AreEqual ("val", sit.Name.Name, "name equal");
			SwiftBuiltInType bit = sit.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (bit, "bit");
			Assert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void DecomposeUnsafeRawPointer ()
		{
			var func = "_$s17unitHelpFrawework3FooSVyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeThrowingFunction ()
		{
			var func = "_$s17unitHelpFrawework7throwIt7doThrowSiSb_tKF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.CanThrow, "CanThrow");
		}

		[Test]
		public void DecomposeVariadicParameter()
		{
			var func = "_$s17unitHelpFrawework5AKLog8fullname4file4line6othersySS_SSSiypdtF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (4, tlf.Signature.ParameterCount, "ParameterCount");
			var arrtype = tlf.Signature.GetParameter (3) as SwiftBoundGenericType;
			Assert.IsNotNull (arrtype, "arrtype");

		}


		[Test]
		public void DecomposerDidSet()
		{
			var func = "_$s17unitHelpFrawework20AKPinkNoiseAudioUnitC9amplitudeSivW";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual (PropertyType.DidSet, prop.PropertyType, "PropertyType");
		}

		[Test]
		[Ignore("Can't get swift 5 generate a meterializer yet.")]
		public void DecomposerMaterializer ()
		{
			var func = "";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
		}

		[Test]
		[Ignore ("Can't get swift 5 generate a meterializer yet.")]
		public void DecomposerMaterializerWithPrivateName ()
		{
			var func = "";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
		}
	
		[Test]
		public void DecomposeWillSet()
		{
			var func = "_$s17unitHelpFrawework20AKPinkNoiseAudioUnitC9amplitudeSivw";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.AreEqual (PropertyType.WillSet, prop.PropertyType, "PropertyType");
		}

		[Test]
		public void DecomposeFieldOffset ()
		{
			var func = "_$s17unitHelpFrawework24AKVariableDelayAudioUnitC12rampDurationSdvpWvd";
			var tlf = Decomposer.Decompose (func, false) as TLFieldOffset;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("rampDuration", tlf.Identifier.Name, "name");
		}

		[Test]
		public void DecomposeConstructorArgInitializer()
		{
			var func = "_$s17unitHelpFrawework10AKMIDINodeC10enableMIDI4port4nameys6UInt32V_SStFfA0_";
			var init = Decomposer.Decompose (func, false) as TLDefaultArgumentInitializer;
			Assert.IsNotNull (init, "init");
			Assert.AreEqual (1, init.ArgumentIndex, "argument index");
			Assert.IsTrue (init.Signature is SwiftUncurriedFunctionType, "uncurried function");
		}
	
		[Test]
		public void DecomposeFuncArgInitializer ()
		{
			var func = "_$s17unitHelpFrawework10AKMIDINodeC10enableMIDI4port4nameys6UInt32V_SStFfA_";
			var init = Decomposer.Decompose (func, false) as TLDefaultArgumentInitializer;
			Assert.IsNotNull (init, "init");
			Assert.AreEqual (0, init.ArgumentIndex, "argument index");
			Assert.IsTrue (init.Signature is SwiftUncurriedFunctionType, "uncurried function");
		}

		[Test]
		public void DecomposeFuncArgInitializer1 ()
		{
			var func = "_$s17unitHelpFrawework10AKMIDINodeC11createError33_3D85A716E8AC30D62D97E78DB643A23DLL7message4codeSo7NSErrorCSS_SitFfA0_";
			var init = Decomposer.Decompose (func, false) as TLDefaultArgumentInitializer;
			Assert.IsNotNull (init, "init");
			Assert.AreEqual (1, init.ArgumentIndex, "argument index");
			Assert.IsTrue (init.Signature is SwiftUncurriedFunctionType, "uncurried function");
		}

		[Test]
		public void DecomposeMetaclass ()
		{
			var func = "_$s8AudioKit11AKOperationCMm";
			var mc = Decomposer.Decompose (func, false) as TLMetaclass;
			Assert.IsNotNull (mc, "mc");
			Assert.AreEqual ("AudioKit.AKOperation", mc.Class.ClassName.ToFullyQualifiedName(true));
		}


		[Test]
		public void DecomposePrivateNameProp ()
		{
			var func = "_$s17unitHelpFrawework11AKPinkNoiseC10internalAU33_3D85A716E8AC30D62D97E78DB643A23DLLSivg";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop.PrivateName, "PrivateName");
		}
	
		[Test]
		public void DecomposeUnsafeMutableAddressor()
		{
			var func = "_$s17unitHelpFrawework10AKBalancerC20ComponentDescriptionSSvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeStaticVariable ()
		{
			var func = "_$s8AudioKit10AKBalancerC20ComponentDescriptionSSvpZ";
			var tlf = Decomposer.Decompose (func, false) as TLVariable;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("AudioKit.AKBalancer", tlf.Class.ClassName.ToFullyQualifiedName (true));
			Assert.AreEqual ("ComponentDescription", tlf.Name.Name);
			Assert.IsTrue (tlf.IsStatic);
		}

		[Test]
		public void DecomposeVariable1 ()
		{
			var func = "_$s17unitHelpFrawework12callbackUgenSo8NSObjectCvp";
			var tlf = Decomposer.Decompose (func, false) as TLVariable;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("callbackUgen", tlf.Name.Name, "name match");
		}

		[Test]
		public void DecomposeUnsafeMutableAddressor1 ()
		{
			var func = "_$s17unitHelpFrawework10AKDurationV16secondsPerMinuteSivau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeUnsafeMutableAddressor2 ()
		{
			var func = "_$s17unitHelpFrawework10AKMetalBarV14scanSpeedRangeSNySdGvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeUnsafeMutableAddressor3 ()
		{
			var func = "_$s17unitHelpFrawework10AKMetalBarV12pregainRangeSNySdGvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeCurryThunk ()
		{
			var func = "_$s17unitHelpFrawework13AKAudioPlayerV25internalCompletionHandler33_3D85A716E8AC30D62D97E78DB643A23DLLyyFTc";
			var tlf = Decomposer.Decompose (func, false) as TLThunk;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (ThunkType.Curry, tlf.Thunk, "curry thunk");
		}


		[Test]
		public void DecomposeVarArgs()
		{
			var func = "_$s17unitHelpFrawework4JSONC17dictionaryLiteralACSS_yptd_tcfC";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (1, tlf.Signature.ParameterCount, "parameter count");
			Assert.IsNotNull (tlf.Signature.Parameters, "parameters");
			var bgt = tlf.Signature.GetParameter (0) as SwiftBoundGenericType;
			Assert.IsNotNull(bgt, "as bound generic");
			Assert.IsTrue (bgt.IsVariadic, "variadic");
		}


		[Test]
		public void DecomposeNestedGenerics()
		{
			var func = "_$s17unitHelpFrawework3FooC3BarC4doIt1a1b1cyx_qd__qd0__tlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual (3, tlf.Signature.ParameterCount, "parameter count");
			for (int i = 0; i < tlf.Signature.ParameterCount; i++) {
				var genRef = tlf.Signature.GetParameter (i) as SwiftGenericArgReferenceType;
				Assert.IsNotNull (genRef, "genRef");
				Assert.AreEqual (i, genRef.Depth, "depth");
				Assert.AreEqual (0, genRef.Index, "index");
			}			
		}

		[Test]
		public void DecomposeExtensionPropGetter ()
		{
			var func = "_$sSd17unitHelpFraweworkE11millisecondSdvg";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "Expected property");
			var extensionOn = prop.ExtensionOn as SwiftBuiltInType;
			Assert.IsNotNull (extensionOn, "Expected a swift built-in type for the extension on");
			Assert.AreEqual (CoreCompoundType.Scalar, extensionOn.Type, "Expected a scalar");
			Assert.AreEqual (CoreBuiltInType.Double, extensionOn.BuiltInType, "Expected a double");
		}


		[Test]
		public void DecomposeExtensionFunc ()
		{
			var func = "_$sSd4NoneE7printeryyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var fn = tlf.Signature as SwiftFunctionType;
			Assert.IsNotNull (fn, "Expected function");
			var extensionOn = fn.ExtensionOn as SwiftBuiltInType;
			Assert.IsNotNull (extensionOn, "Expected a swift built-in type for the extension on");
			Assert.AreEqual (CoreCompoundType.Scalar, extensionOn.Type, "Expected a scalar");
			Assert.AreEqual (CoreBuiltInType.Double, extensionOn.BuiltInType, "Expected a double");
		}
		
		[Test]
		public void DecomposeGenericWithConstraints ()
		{
			var func = "_$s17unitHelpFrawework03ChaD0V6reseed4withyx_tSTRzs6UInt32V7ElementRtzlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "Failed to decompose function");
			Assert.AreEqual (1, tlf.Signature.GenericArguments.Count, "Expected 1 generic argument");
			Assert.AreEqual (2, tlf.Signature.GenericArguments [0].Constraints.Count, "Expected 2 generic constraints");
		}


		[Test]
		public void DecomposeUsafeMutableAddressor1 ()
		{
			var func = "_$s17unitHelpFrawework20EasingFunctionLineary12CoreGraphics7CGFloatVAEcvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			Assert.IsNotNull (tlf, "Failed to decompose function");
			Assert.IsNotNull (tlf.OfType, "Expected non-null 'ofType'");
			Assert.AreEqual ("EasingFunctionLinear", tlf.Name.Name, $"Incorrect name {tlf.Name.Name}");
			var funcType = tlf.OfType as SwiftFunctionType;
			Assert.IsNotNull (funcType, "null function type");
		}

		[Test]
		public void DecomposeSubscriptGetter ()
		{
			var func = "_$s17unitHelpFrawework3FooCyS2icig";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsSubscript, "is subscript");
			Assert.AreEqual (PropertyType.Getter, prop.PropertyType, "getter");
		}

		[Test]
		public void DecomposeSubscriptSetter ()
		{
			var func = "_$s17unitHelpFrawework3FooCyS2icis";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsSubscript, "is subscript");
			Assert.AreEqual (PropertyType.Setter, prop.PropertyType, "setter");
		}

		[Test]
		public void DecomposeSubscriptModifier ()
		{
			var func = "_$s17unitHelpFrawework3FooCyS2iciM";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			Assert.IsNotNull (prop, "prop");
			Assert.IsTrue (prop.IsSubscript, "is subscript");
			Assert.AreEqual (PropertyType.ModifyAccessor, prop.PropertyType, "setter");
		}

		[Test]
		public void DecomposeNominalTypeDescriptorClass ()
		{
			var func = "_$s17unitHelpFrawework3FooCMn";
			var tlf = Decomposer.Decompose (func, false) as TLNominalTypeDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			Assert.AreEqual ("unitHelpFrawework.Foo", className, "className");
			Assert.IsTrue (tlf.Class.IsClass, "IsClass");
		}

		[Test]
		public void DecomposeNominalTypeDescriptorStruct ()
		{
			var func = "_$s17unitHelpFrawework3BarVMn";
			var tlf = Decomposer.Decompose (func, false) as TLNominalTypeDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			Assert.AreEqual ("unitHelpFrawework.Bar", className, "className");
			Assert.IsTrue (tlf.Class.IsStruct, "IsStruct");
		}

		[Test]
		public void DecomposeNominalTypeDescriptorEnum ()
		{
			var func = "_$s17unitHelpFrawework3BazOMn";
			var tlf = Decomposer.Decompose (func, false) as TLNominalTypeDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			Assert.AreEqual ("unitHelpFrawework.Baz", className, "className");
			Assert.IsTrue (tlf.Class.IsEnum, "IsEnum");
		}

		[Test]
		public void DecomposeProtocolTypeDescriptor ()
		{
			var func = "_$s17unitHelpFrawework4UppyMp";
			var tlf = Decomposer.Decompose (func, false) as TLProtocolTypeDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			Assert.AreEqual ("unitHelpFrawework.Uppy", className, "className");
			Assert.IsTrue (tlf.Class.IsProtocol, "IsProtocol");
		}

		[Test]
		public void DecomposeProtocolWitnessTable ()
		{
			var func = "_$s17unitHelpFrawework3FooCAA4UppyAAWP";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var sig = tlf.Signature as SwiftWitnessTableType;
			Assert.IsNotNull (sig, "sig");
			Assert.AreEqual (WitnessType.Protocol, sig.WitnessType, "is protocol");
			var className = sig.ProtocolType.ClassName.ToFullyQualifiedName (true);
			Assert.AreEqual ("unitHelpFrawework.Uppy", className, "protocol name");
		}

		[Test]
		public void DecomposeValueWitnessTable ()
		{
			var func = "_$s17unitHelpFrawework7AStructVWV";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "tlf");
			var sig = tlf.Signature as SwiftWitnessTableType;
			Assert.IsNotNull (sig, "sig");
			Assert.AreEqual (WitnessType.Value, sig.WitnessType, "is value type");
			var classType = sig.UncurriedParameter as SwiftClassType;
			Assert.IsNotNull (classType, "classType");
			var className = classType.ClassName.ToFullyQualifiedName (true);
			Assert.AreEqual ("unitHelpFrawework.AStruct", className);
		}

		[Test]
		public void DecomposeMethodDescriptor ()
		{
			var func = "_$s8itsAFive3BarC3foo1aS2i_tFTq";
			var tlf = Decomposer.Decompose (func, false) as TLMethodDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("foo", tlf.Signature.Name.Name, "name mismatch");
			var builtInType = tlf.Signature.ReturnType as SwiftBuiltInType;
			Assert.IsNotNull (builtInType, "return builtInType");
			Assert.AreEqual (CoreBuiltInType.Int, builtInType.BuiltInType, "return type mismatch");
			builtInType = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			Assert.IsNotNull (builtInType, "parameter builtInType");
			Assert.AreEqual (CoreBuiltInType.Int, builtInType.BuiltInType, "parameter type mismatch");
			Assert.AreEqual ("a", builtInType.Name.Name, "parameter name mismatch");
		}

		[Test]
		public void DecomposeModuleDescriptor ()
		{
			var func = "_$s8itsAFiveMXM";
			var tlf = Decomposer.Decompose (func, false) as TLModuleDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("itsAFive", tlf.Module.Name);
		}


		[Test]
		public void DecomposePropertyDescriptor ()
		{
			var func = "_$s8itsAFive3FooC1xSivpMV";
			var tlf = Decomposer.Decompose (func, false) as TLPropertyDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			Assert.AreEqual ("x", tlf.Name.Name);
			var ofType = tlf.OfType as SwiftBuiltInType;
			Assert.IsNotNull (ofType, "null ofType");
			Assert.AreEqual (CoreBuiltInType.Int, ofType.BuiltInType);
		}


		[Test]
		public void DecomposeReflectionMetadataDescriptor ()
		{
			var func = "_$s8itsAFive2E2OMF";
			var tlf = Decomposer.Decompose (func, false) as TLMetadataDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsFalse (tlf.IsBuiltIn, "IsBuiltIn");
			var ct = tlf.OfType as SwiftClassType;
			Assert.IsNotNull (ct, "not a class");
			Assert.AreEqual ("itsAFive.E2", ct.ClassName.ToFullyQualifiedName ());
		}


		[Test]
		public void DecomposeReflectionBuiltInMetadataDescriptor ()
		{
			var func = "_$s8itsAFive2E2OMB";
			var tlf = Decomposer.Decompose (func, false) as TLMetadataDescriptor;
			Assert.IsNotNull (tlf, "tlf");
			Assert.IsTrue (tlf.IsBuiltIn, "IsBuiltIn");
			var ct = tlf.OfType as SwiftClassType;
			Assert.IsNotNull (ct, "not a class");
			Assert.AreEqual ("itsAFive.E2", ct.ClassName.ToFullyQualifiedName ());
		}

		[Test]
		public void DecomposeProtocolConformanceDescriptor ()
		{
			var func = "_$sSayxG5Macaw12InterpolableABMc";
			var tlf = Decomposer.Decompose (func, false) as TLProtocolConformanceDescriptor;
			Assert.IsNotNull (tlf);

		}

		[Test]
		public void DecomposeExistentialMetatype ()
		{
			var func = "_$s24ProtocolConformanceTests14blindAssocFuncypXpyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			Assert.IsNotNull (tlf, "not a function");
			var returnType = tlf.Signature.ReturnType as SwiftExistentialMetaType;
			Assert.IsNotNull (returnType, "not an existential metatype");
			var protoList = returnType.Protocol;
			Assert.IsNotNull (protoList, "no protocol list");
			var proto = protoList.Protocols [0];
			Assert.AreEqual ("Swift.Any", proto.ClassName.ToFullyQualifiedName (), "class name mismatch");
		}
	}
}

