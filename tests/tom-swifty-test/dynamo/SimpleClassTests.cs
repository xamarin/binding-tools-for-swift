using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit;
using NUnit.Framework;
using Dynamo.CSLang;
using System.Reflection;

namespace dynamotests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class SimpleClassTests {
		public SimpleClassTests ()
		{
		}

		[Test]
		public void EmptyClass ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, null)) {
				Utils.CompileAStream (stm);
			}
		}

		void DeclExposure (CSVisibility vis)
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.Byte, "b", null, CSVisibility.Public));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void ClassWithSingleDeclAllExposures ()
		{
			foreach (var vis in Enum.GetValues (typeof (CSVisibility))) {
				DeclExposure ((CSVisibility)vis);
			}
		}

		void DeclInitExposure (CSVisibility vis)
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.Byte, "b", CSConstant.Val ((byte)0), CSVisibility.Public));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void ClassWithSingleDeclInitAllExposures ()
		{
			foreach (var vis in Enum.GetValues (typeof (CSVisibility))) {
				DeclInitExposure ((CSVisibility)vis);
			}
		}

		void DeclType (CSType type)
		{
			if (type == CSSimpleType.Void)
				return;

			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Fields.Add (CSFieldDeclaration.FieldLine (type, "b", null, CSVisibility.Public));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void ClassWithSingleDeclAllTypes ()
		{
			foreach (MethodInfo mi in typeof (CSType).GetMethods ().Where (mii => mii.IsStatic && mii.IsPublic &&
				   mii.ReturnType == typeof (CSType))) {
				CSType cs = mi.Invoke (null, null) as CSType;
				if (cs != null)
					DeclType (cs);
			}
		}

		[Test]
		public void ConstructorTest ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Constructors.Add (CSMethod.PublicConstructor ("AClass", new CSParameterList (), new CSCodeBlock ()));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void ConstructorTestParam ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, new CSIdentifier ("x")));
				cl.Constructors.Add (CSMethod.PublicConstructor ("AClass", pl, new CSCodeBlock ()));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void ConstructorTestParamList ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"))
					.And (new CSParameter (CSSimpleType.Int, "y"));
				cl.Constructors.Add (CSMethod.PublicConstructor ("AClass", pl, new CSCodeBlock ()));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void MethodNoParams ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ();
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void MethodParam ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"));
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void MethodParamList ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"))
					.And (new CSParameter (CSSimpleType.Int, "y"));
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}


		[Test]
		public void VirtualMethodNoParams ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ();
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSMethodKind.Virtual, CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void VirtualMethodParam ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"));
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSMethodKind.Virtual, CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void VirtualMethodParamList ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"))
					.And (new CSParameter (CSSimpleType.Int, "y"));
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSMethodKind.Virtual, CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void StaticMethodNoParams ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ();
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSMethodKind.Virtual, CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void StaticMethodParam ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"));
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSMethodKind.Virtual, CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void StaticMethodParamList ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				CSParameterList pl = new CSParameterList ().And (new CSParameter (CSSimpleType.Int, "x"))
					.And (new CSParameter (CSSimpleType.Int, "y"));
				CSCodeBlock b = new CSCodeBlock ().And (CSReturn.ReturnLine (CSConstant.Val (0)));
				cl.Methods.Add (CSMethod.PublicMethod (CSMethodKind.Virtual, CSSimpleType.Int, "Foo", pl, b));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void PublicGetPrivateSetProp ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Properties.Add (CSProperty.PublicGetPrivateSet (CSSimpleType.Int, "Foo"));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void PublicGetSetProp ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Properties.Add (CSProperty.PublicGetSet (CSSimpleType.Int, "Foo"));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}

		[Test]
		public void PublicGetSetBacking ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Properties.Add (CSProperty.PublicGetSetBacking (CSSimpleType.Int, "Foo", true, "_bar"));
				return cl;
			})) {
				Utils.CompileAStream (stm);
			}
		}


		[Test]
		public void Pinvoke ()
		{
			using (Stream stm = Utils.BasicClass ("None", "AClass", null, cl => {
				cl.Methods.Add (CSMethod.PInvoke (CSVisibility.Public,
					CSSimpleType.IntPtr, "Walter", "__Internal", "_walter", new CSParameterList ()));
				return cl;
			}, use => {
				return use.And (new CSUsing ("System.Runtime.InteropServices"));
			})) {
				Utils.CompileAStream (stm);
			}
		}
	}
}

