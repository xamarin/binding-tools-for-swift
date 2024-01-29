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
using NUnit.Framework.Legacy;

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
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "modulename");
			ClassicAssert.AreEqual (1, func.Class.ClassName.Nesting.Count, "nesting count");
			ClassicAssert.AreEqual ("AClass", func.Class.ClassName.NestingNames [0].Name, "AClass");
			ClassicAssert.AreEqual ("bar", func.Name.Name, "bar");
			ClassicAssert.IsNotNull (uncurriedFunc, "uncurriedFunc");
		}

		[Test]
		public void HasNLEntries ()
		{
			var stm = MachOTests.HelloSwiftAsLibrary (null);

			var entries = SymbolVisitor.Entries (stm).ToList ();
			ClassicAssert.AreEqual (1, entries.Count (), "1 entry");
			var isSwift3Str = entries [0].Entry.str == "__TF6noname4mainFT_SS";
			var isSwift4Str = entries [0].Entry.str == "__T06noname4mainSSyF";
			var isSwift5Str = entries [0].Entry.str == "_$s6noname4mainSSyF";
			ClassicAssert.IsTrue (isSwift3Str || isSwift4Str || isSwift5Str, "matches a platform");
		}

		[Test]
		public void PunyCode ()
		{
			ClassicAssert.AreEqual (Char.ConvertFromUtf32 (0x1F49B), "GrIh".DePunyCode ());
		}

		[Test]
		public void TestFuncVoidReturningVoid ()
		{
			var func = Decomposer.Decompose ("_$s3foo6lonelyyyF", true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "module");
			ClassicAssert.IsNotNull (func.Signature, "signature");
			ClassicAssert.IsTrue (func.Signature.IsVoid, "IsVoid");
			ClassicAssert.AreEqual (CoreCompoundType.Tuple, func.Signature.ReturnType.Type, "Is tuple");
			ClassicAssert.IsTrue (((SwiftTupleType)func.Signature.ReturnType).IsEmpty, "Is empty tuple");
		}

		[Test]
		public void TestEmptyConstructor ()
		{
			var func = Decomposer.Decompose ("_$s3foo5JuliaCACycfC"/*"__TFC3foo5JuliaCfMS0_FT_S0_"*/, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "module");
			ClassicAssert.IsNotNull (func.Signature, "signature");
			ClassicAssert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var cons = func.Signature as SwiftConstructorType;
			ClassicAssert.AreEqual (func.Class, cons.ReturnType, "is a class");
			ClassicAssert.AreEqual (CoreCompoundType.Tuple, func.Signature.Parameters.Type, "is a tuple");
			ClassicAssert.IsTrue (((SwiftTupleType)func.Signature.Parameters).IsEmpty, "is empty tuple");
		}

		[Test]
		public void TestIntConstructor ()
		{
			var func = Decomposer.Decompose ("_$s3foo5JuliaC5stuffACSi_tcfc", true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "module");
			ClassicAssert.IsNotNull (func.Signature, "signature");
			ClassicAssert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var cons = func.Signature as SwiftConstructorType;
			ClassicAssert.AreEqual (func.Class, cons.ReturnType, "is a class");
			var bit = func.Signature.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.NotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, bit.BuiltInType);
		}

		[Test]
		public void TestClassConstructor ()
		{
			var func = Decomposer.Decompose ("_$s3foo5JuliaCMa", true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "module");
			ClassicAssert.IsNotNull (func.Signature, "signature");
			ClassicAssert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var cctor = func.Signature as SwiftClassConstructorType;
			ClassicAssert.IsNotNull (cctor, "cctor");
			var mct = cctor.ReturnType as SwiftMetaClassType;
			ClassicAssert.AreEqual (func.Class, mct.Class, "class type");
			ClassicAssert.AreEqual (CoreCompoundType.Tuple, func.Signature.Parameters.Type, "is a tuple");
			ClassicAssert.IsTrue (((SwiftTupleType)func.Signature.Parameters).IsEmpty, "is empty");
		}

		void TestFunc3XXXReturningVoid (string funcmangle, CoreBuiltInType csv)
		{
			var func = Decomposer.Decompose (funcmangle, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "module");
			ClassicAssert.IsNotNull (func.Signature, "signature");
			ClassicAssert.IsTrue (func.Signature.IsVoid, "IsVoid");
			var parms = func.Signature.Parameters;
			ClassicAssert.IsNotNull (parms, "parms");
			var tt = parms as SwiftTupleType;
			ClassicAssert.IsNotNull (tt, "tt");
			ClassicAssert.AreEqual (3, tt.Contents.Count, "tuple size");
			foreach (SwiftType st in tt.Contents) {
				SwiftBuiltInType scalar = st as SwiftBuiltInType;
				ClassicAssert.IsNotNull (scalar, "scalar");
				ClassicAssert.AreEqual (csv, scalar.BuiltInType, "scalar type");
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
			ClassicAssert.IsNotNull (func, "func");
			ClassicAssert.AreEqual ("foo", func.Module.Name, "module");
			ClassicAssert.IsNotNull (func.Signature, "signature");
			ClassicAssert.IsFalse (func.Signature.IsVoid, "IsVoid");
			var ret = func.Signature.ReturnType;

			var st = ret as SwiftBuiltInType;
			ClassicAssert.IsNotNull (st, "st");
			ClassicAssert.AreEqual (cbt, st.BuiltInType, "matches type");
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
				ClassicAssert.Fail ("Not a SwiftBuiltInType: " + t.GetType ().Name);
			ClassicAssert.AreEqual (ct, bit.BuiltInType, "same built in type");
		}

		[Test]
		public void TestSimpleArrayOfInt ()
		{
			var func = Decomposer.Decompose ("_$s3foo6nonameyySaySiGF", true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var gentype = func.Signature.Parameters as SwiftBoundGenericType;
			ClassicAssert.IsNotNull (gentype, "gentype");
			ClassicAssert.IsTrue (gentype.BaseType is SwiftClassType, "is a class type");
			var sct = gentype.BaseType as SwiftClassType;
			ClassicAssert.AreEqual ("Swift.Array", sct.ClassName.ToFullyQualifiedName (true), "is Swift.Array");
			ClassicAssert.AreEqual (1, gentype.BoundTypes.Count, "1 bound type");
			BuiltInTypeIsA (gentype.BoundTypes [0], CoreBuiltInType.Int);
		}

		[Test]
		public void TestSimpleArrayOfArrayOfInt ()
		{
			var func = Decomposer.Decompose ("_$s3foo6nonameyySaySaySiGGF", true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var gentype = func.Signature.Parameters as SwiftBoundGenericType;
			ClassicAssert.IsNotNull (gentype, "gentype");
			ClassicAssert.IsTrue (gentype.BaseType is SwiftClassType, "is class");
			var sct = gentype.BaseType as SwiftClassType;
			ClassicAssert.AreEqual ("Swift.Array", sct.ClassName.ToFullyQualifiedName (true), "is array");

			ClassicAssert.AreEqual (1, gentype.BoundTypes.Count);
			ClassicAssert.IsTrue (gentype.BoundTypes [0] is SwiftBoundGenericType, "is generic");
			gentype = gentype.BoundTypes [0] as SwiftBoundGenericType;
			ClassicAssert.IsTrue (gentype.BaseType is SwiftClassType, "is class");
			sct = gentype.BaseType as SwiftClassType;
			ClassicAssert.AreEqual ("Swift.Array", sct.ClassName.ToFullyQualifiedName (true), "is array");

			ClassicAssert.AreEqual (1, gentype.BoundTypes.Count);
			BuiltInTypeIsA (gentype.BoundTypes [0], CoreBuiltInType.Int);
		}

		[Test]
		public void TestSimpleDictIntOnBool ()
		{
			var func = Decomposer.Decompose ("_$s3foo6nonameyySDySiSbGF", true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var gentype = func.Signature.Parameters as SwiftBoundGenericType;
			ClassicAssert.IsNotNull (gentype, "gentype");
			ClassicAssert.IsTrue (gentype.BaseType is SwiftClassType, "is class");
			ClassicAssert.AreEqual (2, gentype.BoundTypes.Count, "2 bound types");
			BuiltInTypeIsA (gentype.BoundTypes [0], CoreBuiltInType.Int);
			BuiltInTypeIsA (gentype.BoundTypes [1], CoreBuiltInType.Bool);
		}


		[Test]
		public void TestTLFunctionUsingClass ()
		{
			var funcName = "_$s17unitHelpFrawework13xamarin_MontyC3valyyAA0E0CF";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
		}


		[Test]
		public void TestTLFunctionPublicGetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC8somePropSivg";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPublic, "IsPublic");
			ClassicAssert.AreEqual (PropertyType.Getter, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPublicSetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC8somePropSivs";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPublic, "IsPublic");
			ClassicAssert.AreEqual (PropertyType.Setter, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		[Ignore("haven't been able to generate a materializer in swift 5 yet.")]
		public void TestTLFunctionPublicMaterializer ()
		{
			var funcName = "";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPublic, "IsPublic");
			ClassicAssert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPublicModifyAccessor ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC8somePropSivM";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPublic, "public");
			ClassicAssert.AreEqual (PropertyType.ModifyAccessor, prop.PropertyType, "is modify accessor");
			ClassicAssert.IsNull (prop.PrivateName, "no private name");
		}

		[Test]
		[Ignore ("haven't been able to generate a materializer in swift 5 yet.")]
		public void TestTLFunctionPublicMaterializer1 ()
		{
			var funcName = "";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
		}


		[Test]
		public void TestTLFunctionPrivateGetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC2_x33_3D85A716E8AC30D62D97E78DB643A23DLLSivg";//"__TFC5None14NonegP33_8C43D7A2FD5ECCB447AC5E0DDCF4B73C10someBackerSi\n";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPrivate, "IsPrivate");
			ClassicAssert.AreEqual (PropertyType.Getter, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPrivateSetter ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC2_x33_3D85A716E8AC30D62D97E78DB643A23DLLSivs";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPrivate, "IsPrivate");
			ClassicAssert.AreEqual (PropertyType.Setter, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		[Ignore ("haven't been able to generate a materializer in swift 5 yet.")]
		public void TestTLFunctionPrivateMaterializer ()
		{
			var funcName = "__TFC5None14NonemP33_8C43D7A2FD5ECCB447AC5E0DDCF4B73C10someBackerSi";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPrivate, "IsPrivate");
			ClassicAssert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void TestTLFunctionPrivateModifyAccessor ()
		{
			var funcName = "_$s17unitHelpFrawework4NoneC2_x33_3D85A716E8AC30D62D97E78DB643A23DLLSivM";
			var func = Decomposer.Decompose (funcName, true) as TLFunction;
			ClassicAssert.IsNotNull (func, "func");
			var prop = func.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsPrivate);
			ClassicAssert.AreEqual (PropertyType.ModifyAccessor, prop.PropertyType, "PropertyType");
			ClassicAssert.IsNotNull (prop.PrivateName, "PrivateName");
		}

		[Test]
		public void HasMono64 ()
		{
			if (!File.Exists (Compiler.kMono64Path))
				ClassicAssert.Fail ("unable to find mono64 at location " + Compiler.kMono64Path);
		}


		[Test]
		public void TestFuncOfClassReturningClassInSameModule ()
		{
			var func = "_$s17unitHelpFrawework13xamarin_MontyC4doItyAA6GarbleCAA0E0CF";
			var tlf = Decomposer.Decompose (func, true) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var bft = tlf.Signature as SwiftBaseFunctionType;
			ClassicAssert.IsNotNull (bft, "bft");
			var sct = bft.ReturnType as SwiftClassType;
			ClassicAssert.IsNotNull (sct, "sct");
			ClassicAssert.AreEqual ("unitHelpFrawework.Garble", sct.ClassName.ToFullyQualifiedName ());
		}


		[Test]
		public void TestFuncWithInOutArgument ()
		{
			var func = "_$s17unitHelpFrawework9OneStructV9mutateVaryySizF";
			var tlf = Decomposer.Decompose (func, true) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var bft = tlf.Signature as SwiftBaseFunctionType;
			ClassicAssert.IsNotNull (bft, "bft");
			var argType = bft.Parameters as SwiftBuiltInType;
			ClassicAssert.IsNotNull (argType, "argType");
			ClassicAssert.IsTrue (argType.IsReference, "IsReference");
		}

		[Test]
		public void TestStructMeta ()
		{
			var func = "_$s17unitHelpFrawework7AStructVN";
			var def = Decomposer.Decompose (func, true) as TLDefinition;
			ClassicAssert.IsNotNull (def, "def");
		}


		[Test]
		public void DecomposeStructConstructor ()
		{
			var func = "_$s17unitHelpFrawework7AStructVACycfC";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature is SwiftConstructorType, "is constructor");
		}

		[Test]
		public void DecomposeGlobalAddressor ()
		{
			var func = "_$s17unitHelpFrawework7aGlobalSbvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsNotNull (tlf.OfType, "OfType");
		}

		[Test]
		public void DecomposeGlobalVariable ()
		{
			var func = "_$s17unitHelpFrawework7aGlobalSbvp";
			var vari = Decomposer.Decompose (func, false) as TLVariable;
			ClassicAssert.IsNotNull (vari, "vari");
		}



		[Test]
		public void DecomposeFunctionOfEnum ()
		{
			var func = "_$s17unitHelpFrawework10printFirstyyAA0E0OF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.ReturnType.IsEmptyTuple, "is empty tuple");
			var ct = tlf.Signature.Parameters as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "ct");
			ClassicAssert.IsTrue (ct.EntityKind == MemberNesting.Enum, "is enum");
			ClassicAssert.AreEqual ("First", ct.ClassName.Terminus.Name, "is First");
		}

		[Test]
		public void DemcomposeCConventionCall ()
		{
			var func = "_$s17unitHelpFrawework12callSomeFuncyyyyXCF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.Parameters is SwiftCFunctionPointerType, "not a c function pointer type");
		}

		[Test]
		public void DecomposeUnsafePointer ()
		{
			// NB: this function used UnsafePointer<()> which is deprecated.
			var func = "_$s17unitHelpFrawework19setMonty_xam_vtable_3uvtyAA0f5_sub_E0V_SPyytGtF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			SwiftType bft = tlf.Signature.GetParameter (1);
			ClassicAssert.IsNotNull (bft, "bft");
			ClassicAssert.IsTrue (bft is SwiftBoundGenericType, "is SwiftBoundGeneric");
			var gen = bft as SwiftBoundGenericType;
			SwiftClassType baseType = gen.BaseType as SwiftClassType;
			ClassicAssert.NotNull (baseType, "baseType");
			ClassicAssert.AreEqual ("UnsafePointer", baseType.ClassName.Terminus.Name, "UnsafePointer");
		}


		[Test]
		public void DemcomposeReturnsInt64 ()
		{
			var func = "_$s17unitHelpFrawework5MontyC3vals5Int64VyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.ReturnType is SwiftClassType, "is swift class type");
		}

		[Test]
		public void DecomposeProtocol ()
		{
			var func = "_$s17unitHelpFrawework11ThisIsaFunc_1xs5Int64VAA7MyProto_p_AEtF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.ParameterCount == 2, "parameter count");
			var prot = tlf.Signature.GetParameter (0) as SwiftClassType;
			ClassicAssert.IsNotNull (prot, "prot");
			ClassicAssert.AreEqual (MemberNesting.Protocol, prot.EntityKind, "is protocol");
		}


		[Test]
		public void DecomposeSimpleGeneric ()
		{
			var func = "_$s17unitHelpFrawework3foo_1b1c1dyx_S3itlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (4, tlf.Signature.ParameterCount, "parameter count");
			ClassicAssert.IsTrue (tlf.Signature.ContainsGenericParameters, "contains generic parameters");
			ClassicAssert.AreEqual (1, tlf.Signature.GenericArguments.Count (), "generic arguments count");
		}

		[Test]
		public void DecomposeMultiGeneric ()
		{
			var func = "_$s17unitHelpFrawework3foo1a1b1c1dyx_q_q0_q1_tr2_lF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (4, tlf.Signature.ParameterCount, "parameter count");
			ClassicAssert.IsTrue (tlf.Signature.ContainsGenericParameters, "contains generic parameters");
			ClassicAssert.AreEqual (4, tlf.Signature.GenericArguments.Count (), "generic argument count");
		}

		[Test]
		public void DecomposeMetadataPattern ()
		{
			var func = "_$s17unitHelpFrawework3FooCMP";
			var gmp = Decomposer.Decompose (func, false) as TLGenericMetadataPattern;
			ClassicAssert.IsNotNull (gmp, "gmp");
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", gmp.Class.ClassName.ToFullyQualifiedName (true), "classname");
		}


		[Test]
		public void DecomposeStaticProp ()
		{
			var func = "_$s17unitHelpFrawework11aFinalClassC11aStaticPropSbvpZ";

			var tlv = Decomposer.Decompose (func, false) as TLVariable;
			ClassicAssert.IsNotNull (tlv, "tlv");
			ClassicAssert.IsTrue (tlv.IsStatic, "IsStatic");
			ClassicAssert.IsNotNull (tlv.Class, "Class");
		}

		[Test]
		public void DecomposeMultiProtoConstraints ()
		{
			var func = "_$s17unitHelpFrawework16xamarin_FooDuppy3foo1ayAA0E0CyxG_xtAA6DownerRzAA5UpperRzlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (1, tlf.Signature.GenericArguments.Count, "genric argument count");
			ClassicAssert.AreEqual (2, tlf.Signature.GenericArguments [0].Constraints.Count, "constraints count");
		}

		[Test]
		public void DecomposeVariableInitializer ()
		{
			var func = "_$s17unitHelpFrawework5MontyC3valSbvpfi";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			SwiftInitializerType sit = tlf.Signature as SwiftInitializerType;
			ClassicAssert.IsNotNull (sit, "sit");
			ClassicAssert.AreEqual ("val", sit.Name.Name, "name equal");
			SwiftBuiltInType bit = sit.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (bit, "bit");
			ClassicAssert.AreEqual (CoreBuiltInType.Bool, bit.BuiltInType);
		}

		[Test]
		public void DecomposeUnsafeRawPointer ()
		{
			var func = "_$s17unitHelpFrawework3FooSVyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeThrowingFunction ()
		{
			var func = "_$s17unitHelpFrawework7throwIt7doThrowSiSb_tKF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.CanThrow, "CanThrow");
		}

		[Test]
		public void DecomposeVariadicParameter()
		{
			var func = "_$s17unitHelpFrawework5AKLog8fullname4file4line6othersySS_SSSiypdtF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (4, tlf.Signature.ParameterCount, "ParameterCount");
			var arrtype = tlf.Signature.GetParameter (3) as SwiftBoundGenericType;
			ClassicAssert.IsNotNull (arrtype, "arrtype");

		}


		[Test]
		public void DecomposerDidSet()
		{
			var func = "_$s17unitHelpFrawework20AKPinkNoiseAudioUnitC9amplitudeSivW";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual (PropertyType.DidSet, prop.PropertyType, "PropertyType");
		}

		[Test]
		[Ignore("Can't get swift 5 generate a meterializer yet.")]
		public void DecomposerMaterializer ()
		{
			var func = "";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
		}

		[Test]
		[Ignore ("Can't get swift 5 generate a meterializer yet.")]
		public void DecomposerMaterializerWithPrivateName ()
		{
			var func = "";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual (PropertyType.Materializer, prop.PropertyType, "PropertyType");
		}
	
		[Test]
		public void DecomposeWillSet()
		{
			var func = "_$s17unitHelpFrawework20AKPinkNoiseAudioUnitC9amplitudeSivw";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.AreEqual (PropertyType.WillSet, prop.PropertyType, "PropertyType");
		}

		[Test]
		public void DecomposeFieldOffset ()
		{
			var func = "_$s17unitHelpFrawework24AKVariableDelayAudioUnitC12rampDurationSdvpWvd";
			var tlf = Decomposer.Decompose (func, false) as TLFieldOffset;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("rampDuration", tlf.Identifier.Name, "name");
		}

		[Test]
		public void DecomposeConstructorArgInitializer()
		{
			var func = "_$s17unitHelpFrawework10AKMIDINodeC10enableMIDI4port4nameys6UInt32V_SStFfA0_";
			var init = Decomposer.Decompose (func, false) as TLDefaultArgumentInitializer;
			ClassicAssert.IsNotNull (init, "init");
			ClassicAssert.AreEqual (1, init.ArgumentIndex, "argument index");
			ClassicAssert.IsTrue (init.Signature is SwiftUncurriedFunctionType, "uncurried function");
		}
	
		[Test]
		public void DecomposeFuncArgInitializer ()
		{
			var func = "_$s17unitHelpFrawework10AKMIDINodeC10enableMIDI4port4nameys6UInt32V_SStFfA_";
			var init = Decomposer.Decompose (func, false) as TLDefaultArgumentInitializer;
			ClassicAssert.IsNotNull (init, "init");
			ClassicAssert.AreEqual (0, init.ArgumentIndex, "argument index");
			ClassicAssert.IsTrue (init.Signature is SwiftUncurriedFunctionType, "uncurried function");
		}

		[Test]
		public void DecomposeFuncArgInitializer1 ()
		{
			var func = "_$s17unitHelpFrawework10AKMIDINodeC11createError33_3D85A716E8AC30D62D97E78DB643A23DLL7message4codeSo7NSErrorCSS_SitFfA0_";
			var init = Decomposer.Decompose (func, false) as TLDefaultArgumentInitializer;
			ClassicAssert.IsNotNull (init, "init");
			ClassicAssert.AreEqual (1, init.ArgumentIndex, "argument index");
			ClassicAssert.IsTrue (init.Signature is SwiftUncurriedFunctionType, "uncurried function");
		}

		[Test]
		public void DecomposeMetaclass ()
		{
			var func = "_$s8AudioKit11AKOperationCMm";
			var mc = Decomposer.Decompose (func, false) as TLMetaclass;
			ClassicAssert.IsNotNull (mc, "mc");
			ClassicAssert.AreEqual ("AudioKit.AKOperation", mc.Class.ClassName.ToFullyQualifiedName(true));
		}


		[Test]
		public void DecomposePrivateNameProp ()
		{
			var func = "_$s17unitHelpFrawework11AKPinkNoiseC10internalAU33_3D85A716E8AC30D62D97E78DB643A23DLLSivg";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop.PrivateName, "PrivateName");
		}
	
		[Test]
		public void DecomposeUnsafeMutableAddressor()
		{
			var func = "_$s17unitHelpFrawework10AKBalancerC20ComponentDescriptionSSvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeStaticVariable ()
		{
			var func = "_$s8AudioKit10AKBalancerC20ComponentDescriptionSSvpZ";
			var tlf = Decomposer.Decompose (func, false) as TLVariable;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("AudioKit.AKBalancer", tlf.Class.ClassName.ToFullyQualifiedName (true));
			ClassicAssert.AreEqual ("ComponentDescription", tlf.Name.Name);
			ClassicAssert.IsTrue (tlf.IsStatic);
		}

		[Test]
		public void DecomposeVariable1 ()
		{
			var func = "_$s17unitHelpFrawework12callbackUgenSo8NSObjectCvp";
			var tlf = Decomposer.Decompose (func, false) as TLVariable;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("callbackUgen", tlf.Name.Name, "name match");
		}

		[Test]
		public void DecomposeUnsafeMutableAddressor1 ()
		{
			var func = "_$s17unitHelpFrawework10AKDurationV16secondsPerMinuteSivau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeUnsafeMutableAddressor2 ()
		{
			var func = "_$s17unitHelpFrawework10AKMetalBarV14scanSpeedRangeSNySdGvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeUnsafeMutableAddressor3 ()
		{
			var func = "_$s17unitHelpFrawework10AKMetalBarV12pregainRangeSNySdGvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlf, "tlf");
		}

		[Test]
		public void DecomposeCurryThunk ()
		{
			var func = "_$s17unitHelpFrawework13AKAudioPlayerV25internalCompletionHandler33_3D85A716E8AC30D62D97E78DB643A23DLLyyFTc";
			var tlf = Decomposer.Decompose (func, false) as TLThunk;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (ThunkType.Curry, tlf.Thunk, "curry thunk");
		}


		[Test]
		public void DecomposeVarArgs()
		{
			var func = "_$s17unitHelpFrawework4JSONC17dictionaryLiteralACSS_yptd_tcfC";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (1, tlf.Signature.ParameterCount, "parameter count");
			ClassicAssert.IsNotNull (tlf.Signature.Parameters, "parameters");
			var bgt = tlf.Signature.GetParameter (0) as SwiftBoundGenericType;
			ClassicAssert.IsNotNull(bgt, "as bound generic");
			ClassicAssert.IsTrue (bgt.IsVariadic, "variadic");
		}


		[Test]
		public void DecomposeNestedGenerics()
		{
			var func = "_$s17unitHelpFrawework3FooC3BarC4doIt1a1b1cyx_qd__qd0__tlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual (3, tlf.Signature.ParameterCount, "parameter count");
			for (int i = 0; i < tlf.Signature.ParameterCount; i++) {
				var genRef = tlf.Signature.GetParameter (i) as SwiftGenericArgReferenceType;
				ClassicAssert.IsNotNull (genRef, "genRef");
				ClassicAssert.AreEqual (i, genRef.Depth, "depth");
				ClassicAssert.AreEqual (0, genRef.Index, "index");
			}			
		}

		[Test]
		public void DecomposeExtensionPropGetter ()
		{
			var func = "_$sSd17unitHelpFraweworkE11millisecondSdvg";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "Expected property");
			var extensionOn = prop.ExtensionOn as SwiftBuiltInType;
			ClassicAssert.IsNotNull (extensionOn, "Expected a swift built-in type for the extension on");
			ClassicAssert.AreEqual (CoreCompoundType.Scalar, extensionOn.Type, "Expected a scalar");
			ClassicAssert.AreEqual (CoreBuiltInType.Double, extensionOn.BuiltInType, "Expected a double");
		}


		[Test]
		public void DecomposeExtensionFunc ()
		{
			var func = "_$sSd4NoneE7printeryyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.Signature.IsExtension, "Expected extension");
			var fn = tlf.Signature as SwiftFunctionType;
			ClassicAssert.IsNotNull (fn, "Expected function");
			var extensionOn = fn.ExtensionOn as SwiftBuiltInType;
			ClassicAssert.IsNotNull (extensionOn, "Expected a swift built-in type for the extension on");
			ClassicAssert.AreEqual (CoreCompoundType.Scalar, extensionOn.Type, "Expected a scalar");
			ClassicAssert.AreEqual (CoreBuiltInType.Double, extensionOn.BuiltInType, "Expected a double");
		}
		
		[Test]
		public void DecomposeGenericWithConstraints ()
		{
			var func = "_$s17unitHelpFrawework03ChaD0V6reseed4withyx_tSTRzs6UInt32V7ElementRtzlF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "Failed to decompose function");
			ClassicAssert.AreEqual (1, tlf.Signature.GenericArguments.Count, "Expected 1 generic argument");
			ClassicAssert.AreEqual (2, tlf.Signature.GenericArguments [0].Constraints.Count, "Expected 2 generic constraints");
		}


		[Test]
		public void DecomposeUsafeMutableAddressor1 ()
		{
			var func = "_$s17unitHelpFrawework20EasingFunctionLineary12CoreGraphics7CGFloatVAEcvau";
			var tlf = Decomposer.Decompose (func, false) as TLUnsafeMutableAddressor;
			ClassicAssert.IsNotNull (tlf, "Failed to decompose function");
			ClassicAssert.IsNotNull (tlf.OfType, "Expected non-null 'ofType'");
			ClassicAssert.AreEqual ("EasingFunctionLinear", tlf.Name.Name, $"Incorrect name {tlf.Name.Name}");
			var funcType = tlf.OfType as SwiftFunctionType;
			ClassicAssert.IsNotNull (funcType, "null function type");
		}

		[Test]
		public void DecomposeSubscriptGetter ()
		{
			var func = "_$s17unitHelpFrawework3FooCyS2icig";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsSubscript, "is subscript");
			ClassicAssert.AreEqual (PropertyType.Getter, prop.PropertyType, "getter");
		}

		[Test]
		public void DecomposeSubscriptSetter ()
		{
			var func = "_$s17unitHelpFrawework3FooCyS2icis";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsSubscript, "is subscript");
			ClassicAssert.AreEqual (PropertyType.Setter, prop.PropertyType, "setter");
		}

		[Test]
		public void DecomposeSubscriptModifier ()
		{
			var func = "_$s17unitHelpFrawework3FooCyS2iciM";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var prop = tlf.Signature as SwiftPropertyType;
			ClassicAssert.IsNotNull (prop, "prop");
			ClassicAssert.IsTrue (prop.IsSubscript, "is subscript");
			ClassicAssert.AreEqual (PropertyType.ModifyAccessor, prop.PropertyType, "setter");
		}

		[Test]
		public void DecomposeNominalTypeDescriptorClass ()
		{
			var func = "_$s17unitHelpFrawework3FooCMn";
			var tlf = Decomposer.Decompose (func, false) as TLNominalTypeDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			ClassicAssert.AreEqual ("unitHelpFrawework.Foo", className, "className");
			ClassicAssert.IsTrue (tlf.Class.IsClass, "IsClass");
		}

		[Test]
		public void DecomposeNominalTypeDescriptorStruct ()
		{
			var func = "_$s17unitHelpFrawework3BarVMn";
			var tlf = Decomposer.Decompose (func, false) as TLNominalTypeDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			ClassicAssert.AreEqual ("unitHelpFrawework.Bar", className, "className");
			ClassicAssert.IsTrue (tlf.Class.IsStruct, "IsStruct");
		}

		[Test]
		public void DecomposeNominalTypeDescriptorEnum ()
		{
			var func = "_$s17unitHelpFrawework3BazOMn";
			var tlf = Decomposer.Decompose (func, false) as TLNominalTypeDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			ClassicAssert.AreEqual ("unitHelpFrawework.Baz", className, "className");
			ClassicAssert.IsTrue (tlf.Class.IsEnum, "IsEnum");
		}

		[Test]
		public void DecomposeProtocolTypeDescriptor ()
		{
			var func = "_$s17unitHelpFrawework4UppyMp";
			var tlf = Decomposer.Decompose (func, false) as TLProtocolTypeDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var className = tlf.Class.ClassName.ToFullyQualifiedName (true);
			ClassicAssert.AreEqual ("unitHelpFrawework.Uppy", className, "className");
			ClassicAssert.IsTrue (tlf.Class.IsProtocol, "IsProtocol");
		}

		[Test]
		public void DecomposeProtocolWitnessTable ()
		{
			var func = "_$s17unitHelpFrawework3FooCAA4UppyAAWP";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var sig = tlf.Signature as SwiftWitnessTableType;
			ClassicAssert.IsNotNull (sig, "sig");
			ClassicAssert.AreEqual (WitnessType.Protocol, sig.WitnessType, "is protocol");
			var className = sig.ProtocolType.ClassName.ToFullyQualifiedName (true);
			ClassicAssert.AreEqual ("unitHelpFrawework.Uppy", className, "protocol name");
		}

		[Test]
		public void DecomposeValueWitnessTable ()
		{
			var func = "_$s17unitHelpFrawework7AStructVWV";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "tlf");
			var sig = tlf.Signature as SwiftWitnessTableType;
			ClassicAssert.IsNotNull (sig, "sig");
			ClassicAssert.AreEqual (WitnessType.Value, sig.WitnessType, "is value type");
			var classType = sig.UncurriedParameter as SwiftClassType;
			ClassicAssert.IsNotNull (classType, "classType");
			var className = classType.ClassName.ToFullyQualifiedName (true);
			ClassicAssert.AreEqual ("unitHelpFrawework.AStruct", className);
		}

		[Test]
		public void DecomposeMethodDescriptor ()
		{
			var func = "_$s8itsAFive3BarC3foo1aS2i_tFTq";
			var tlf = Decomposer.Decompose (func, false) as TLMethodDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("foo", tlf.Signature.Name.Name, "name mismatch");
			var builtInType = tlf.Signature.ReturnType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (builtInType, "return builtInType");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, builtInType.BuiltInType, "return type mismatch");
			builtInType = tlf.Signature.GetParameter (0) as SwiftBuiltInType;
			ClassicAssert.IsNotNull (builtInType, "parameter builtInType");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, builtInType.BuiltInType, "parameter type mismatch");
			ClassicAssert.AreEqual ("a", builtInType.Name.Name, "parameter name mismatch");
		}

		[Test]
		public void DecomposeModuleDescriptor ()
		{
			var func = "_$s8itsAFiveMXM";
			var tlf = Decomposer.Decompose (func, false) as TLModuleDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("itsAFive", tlf.Module.Name);
		}


		[Test]
		public void DecomposePropertyDescriptor ()
		{
			var func = "_$s8itsAFive3FooC1xSivpMV";
			var tlf = Decomposer.Decompose (func, false) as TLPropertyDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.AreEqual ("x", tlf.Name.Name);
			var ofType = tlf.OfType as SwiftBuiltInType;
			ClassicAssert.IsNotNull (ofType, "null ofType");
			ClassicAssert.AreEqual (CoreBuiltInType.Int, ofType.BuiltInType);
		}


		[Test]
		public void DecomposeReflectionMetadataDescriptor ()
		{
			var func = "_$s8itsAFive2E2OMF";
			var tlf = Decomposer.Decompose (func, false) as TLMetadataDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsFalse (tlf.IsBuiltIn, "IsBuiltIn");
			var ct = tlf.OfType as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "not a class");
			ClassicAssert.AreEqual ("itsAFive.E2", ct.ClassName.ToFullyQualifiedName ());
		}


		[Test]
		public void DecomposeReflectionBuiltInMetadataDescriptor ()
		{
			var func = "_$s8itsAFive2E2OMB";
			var tlf = Decomposer.Decompose (func, false) as TLMetadataDescriptor;
			ClassicAssert.IsNotNull (tlf, "tlf");
			ClassicAssert.IsTrue (tlf.IsBuiltIn, "IsBuiltIn");
			var ct = tlf.OfType as SwiftClassType;
			ClassicAssert.IsNotNull (ct, "not a class");
			ClassicAssert.AreEqual ("itsAFive.E2", ct.ClassName.ToFullyQualifiedName ());
		}

		[Test]
		public void DecomposeProtocolConformanceDescriptor ()
		{
			var func = "_$sSayxG5Macaw12InterpolableABMc";
			var tlf = Decomposer.Decompose (func, false) as TLProtocolConformanceDescriptor;
			ClassicAssert.IsNotNull (tlf);

		}

		[Test]
		public void DecomposeExistentialMetatype ()
		{
			var func = "_$s24ProtocolConformanceTests14blindAssocFuncypXpyF";
			var tlf = Decomposer.Decompose (func, false) as TLFunction;
			ClassicAssert.IsNotNull (tlf, "not a function");
			var returnType = tlf.Signature.ReturnType as SwiftExistentialMetaType;
			ClassicAssert.IsNotNull (returnType, "not an existential metatype");
			var protoList = returnType.Protocol;
			ClassicAssert.IsNotNull (protoList, "no protocol list");
			var proto = protoList.Protocols [0];
			ClassicAssert.AreEqual ("Swift.Any", proto.ClassName.ToFullyQualifiedName (), "class name mismatch");
		}
	}
}

