using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NUnit.Framework;
using Dynamo.SwiftLang;
using Dynamo;
using tomwiftytest;

namespace dynamotests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class SwiftLangTests {
		void FuncReturningFoo (SLType type, SLConstant val)
		{
			SLLine line = SLReturn.ReturnLine (val);
			SLCodeBlock block = new SLCodeBlock (new ICodeElement [] { line });
			SLFunc func = new SLFunc (Visibility.Public, type, new SLIdentifier ("simpleFunc"), null, block);

			string code = CodeWriter.WriteToString (func);
			Compiler.CompileStringUsing (null, XCodeCompiler.SwiftcCustom, code, null);
		}

		[Test]
		public void FuncReturningInt ()
		{
			FuncReturningFoo (SLSimpleType.Int, SLConstant.Val (4));
		}

		[Test]
		public void FuncReturningBool ()
		{
			FuncReturningFoo (SLSimpleType.Bool, SLConstant.Val (false));
		}

		[Test]
		public void FuncReturningFloat ()
		{
			FuncReturningFoo (SLSimpleType.Float, SLConstant.Val (4.0f));
		}

		[Test]
		public void FuncReturningDouble ()
		{
			FuncReturningFoo (SLSimpleType.Float, SLConstant.Val (4.0));
		}

		[Test]
		public void SimpleLet ()
		{
			SLLine line = SLDeclaration.LetLine ("foo", SLSimpleType.Int, SLConstant.Val (5), Visibility.Public);
			string code = CodeWriter.WriteToString (line);
			Compiler.CompileStringUsing (null, XCodeCompiler.SwiftcCustom, code, null);
		}



		void BindToValue (bool isLet, SLType type, ISLExpr value, Visibility vis)
		{
			SLLine line = isLet ? SLDeclaration.LetLine ("foo", type, value, vis)
				: SLDeclaration.VarLine ("foo", type, value, vis);
			string code = CodeWriter.WriteToString (line);
			Compiler.CompileStringUsing (null, XCodeCompiler.SwiftcCustom, code, null);
		}

		[Test]
		public void TestLetInt ()
		{
			BindToValue (true, SLSimpleType.Int, SLConstant.Val (42), Visibility.Public);
			BindToValue (false, SLSimpleType.Int, SLConstant.Val (42), Visibility.Public);
			BindToValue (true, SLSimpleType.Int, SLConstant.Val (42), Visibility.Private);
			BindToValue (false, SLSimpleType.Int, SLConstant.Val (42), Visibility.Private);
			BindToValue (true, SLSimpleType.Int, SLConstant.Val (42), Visibility.Internal);
			BindToValue (false, SLSimpleType.Int, SLConstant.Val (42), Visibility.Internal);
			BindToValue (true, SLSimpleType.Int, SLConstant.Val (42), Visibility.None);
			BindToValue (false, SLSimpleType.Int, SLConstant.Val (42), Visibility.None);
		}

		[Test]
		public void TestLetIntInfer ()
		{
			BindToValue (true, null, SLConstant.Val (42), Visibility.Public);
			BindToValue (false, null, SLConstant.Val (42), Visibility.Public);
		}

		[Test]
		public void TestLetFloat ()
		{
			BindToValue (true, SLSimpleType.Float, SLConstant.Val (42f), Visibility.Public);
			BindToValue (false, SLSimpleType.Float, SLConstant.Val (42f), Visibility.Public);
		}

		[Test]
		public void TestLetFloatInfer ()
		{
			BindToValue (true, null, SLConstant.Val (42f), Visibility.Public);
			BindToValue (false, null, SLConstant.Val (42f), Visibility.Public);
		}


	}
}

