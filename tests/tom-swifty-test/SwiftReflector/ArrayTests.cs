using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using SwiftReflector.Inventory;
using tomwiftytest;
using SwiftReflector.IOUtils;
using Dynamo.CSLang;
using SwiftReflector.TypeMapping;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ArrayTests {


		void TLArraySimple (string swiftType, string csType, string output)
		{
			string swiftCode =
			    $"public func makeArrayTLAS{swiftType}()  -> [{swiftType}]\n {{\n return [{swiftType}]() \n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftArray", false, new CSSimpleType (csType)),
													new CSIdentifier ("arr"),
						    new CSFunctionCall ($"TopLevelEntities.MakeArrayTLAS{swiftType}", false));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("arr.Count"));
			callingCode.Add (decl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : $"MakeArrayTLAS{swiftType}");
		}

		[Test]
		public void SimpleArrInt ()
		{
			TLArraySimple ("Int", "nint", "0");
		}


		void TLArrayMethodSimple (string swiftType, string csType, string output)
		{
			string swiftCode =
			    $"public class FooTLAMS{swiftType} {{\n" +
			    $"public init() {{ }}\n" +
			    $"public func makeArray()  -> [{swiftType}]\n {{\n return [{swiftType}]() \n}}\n" +
			    $"}}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftArray", false, new CSSimpleType (csType)),
													new CSIdentifier ("arr"),
						    new CSFunctionCall ($"new FooTLAMS{swiftType}().MakeArray", false));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("arr.Count"));
			callingCode.Add (decl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLArrayMethodSimple{swiftType}");
		}

		[Test]
		public void SimpleArrMethodInt ()
		{
			TLArrayMethodSimple ("Int", "nint", "0");
		}


		void TLArrayMethodDups (string swiftType, string csType, string csValue, string output)
		{
			string swiftCode =
			    $"public class FooTLAMD{swiftType} {{\n" +
			    $"public init() {{ }}\n" +
			    $"public func makeArray(a:{swiftType})  -> [{swiftType}]\n {{\n return [{swiftType}](repeating:a, count:5) \n}}\n" +
			    $"}}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftArray", false, new CSSimpleType (csType)),
													new CSIdentifier ("arr"),
						    new CSFunctionCall ($"new FooTLAMD{swiftType}().MakeArray", false, new CSIdentifier (csValue)));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("arr.Count"));
			CSForEach feach = new CSForEach (new CSSimpleType (csType), "x", new CSIdentifier ("arr"), null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val (" {0}"), feach.Ident));

			callingCode.Add (decl);
			callingCode.Add (call);
			callingCode.Add (feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLArrayMethodDups{swiftType}");
		}

		[Test]
		public void TestArrayOfInts ()
		{
			TLArrayMethodDups ("Int", "nint", "3", "5 3 3 3 3 3");
		}

		[Test]
		public void TestArrayOfBools ()
		{
			TLArrayMethodDups ("Bool", "bool", "true", "5 True True True True True");
		}

		[Test]
		public void TestArrayOfFloat ()
		{
			TLArrayMethodDups ("Float", "float", "3.1f", "5 3.1 3.1 3.1 3.1 3.1");
		}

		[Test]
		public void TestArrayOfDouble ()
		{
			TLArrayMethodDups ("Double", "double", "3.1", "5 3.1 3.1 3.1 3.1 3.1");
		}

		[Test]
		public void TestArrayOfString ()
		{
			TLArrayMethodDups ("String", "SwiftString", "SwiftString.FromString(\"mom\")", "5 mom mom mom mom mom");
		}


		void TLArrayMethodAdd (string swiftType, string csType, string csValue, string csNewValue, string output)
		{
			string swiftCode =
			    $"public class FooTLAMA{swiftType} {{\n" +
			    $"public init() {{ }}\n" +
			    $"public func makeArray(a:{swiftType})  -> [{swiftType}]\n {{\n return [{swiftType}](repeating:a, count:2) \n}}\n" +
			    $"}}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftArray", false, new CSSimpleType (csType)),
													new CSIdentifier ("arr"),
						    new CSFunctionCall ($"new FooTLAMA{swiftType}().MakeArray", false, new CSIdentifier (csValue)));
			CSLine addLine = CSFunctionCall.FunctionCallLine ("arr.Add", false, new CSIdentifier (csNewValue));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("arr.Count"));
			CSForEach feach = new CSForEach (new CSSimpleType (csType), "x", new CSIdentifier ("arr"), null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val (" {0}"), feach.Ident));

			callingCode.Add (decl);
			callingCode.Add (addLine);
			callingCode.Add (call);
			callingCode.Add (feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLArrayMethodAdd{swiftType}");
		}


		[Test]
		public void TestArrayOfIntsAdd ()
		{
			TLArrayMethodAdd ("Int", "nint", "3", "-7", "3 3 3 -7");
		}


		[Test]
		public void TestArrayOfBoolAdd ()
		{
			TLArrayMethodAdd ("Bool", "bool", "true", "false", "3 True True False");
		}

		[Test]
		public void TestArrayOfFloatAdd ()
		{
			TLArrayMethodAdd ("Float", "float", "3.1f", "3.8f", "3 3.1 3.1 3.8");
		}

		[Test]
		public void TestArrayOfDoubleAdd ()
		{
			TLArrayMethodAdd ("Double", "double", "3.1", "3.8", "3 3.1 3.1 3.8");
		}

		[Test]
		public void TestArrayOfStringAdd ()
		{
			TLArrayMethodAdd ("String", "SwiftString", "SwiftString.FromString(\"mom\")",
							 "SwiftString.FromString(\"dad\")", "3 mom mom dad");
		}

		void TLArrayMethodInsert (string swiftType, string csType, string csValue, string csNewValue, string output)
		{
			string swiftCode =
			    $"public class FooTLAMI{swiftType} {{\n" +
			    $"public init() {{ }}\n" +
			    $"public func makeArray(a:{swiftType})  -> [{swiftType}]\n {{\n return [{swiftType}](repeating:a, count:2) \n}}\n" +
			    $"}}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftArray", false, new CSSimpleType (csType)),
													new CSIdentifier ("arr"),
						    new CSFunctionCall ($"new FooTLAMI{swiftType}().MakeArray", false, new CSIdentifier (csValue)));
			CSLine addLine = CSFunctionCall.FunctionCallLine ("arr.Insert", false, CSConstant.Val (1), new CSIdentifier (csNewValue));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("arr.Count"));
			CSForEach feach = new CSForEach (new CSSimpleType (csType), "x", new CSIdentifier ("arr"), null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val (" {0}"), feach.Ident));

			callingCode.Add (decl);
			callingCode.Add (addLine);
			callingCode.Add (call);
			callingCode.Add (feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLArrayMethodInsert{swiftType}");
		}

		[Test]
		public void TestArrayOfIntsInsert ()
		{
			TLArrayMethodInsert ("Int", "nint", "3", "-7", "3 3 -7 3");
		}


		[Test]
		public void TestArrayOfBoolInsert ()
		{
			TLArrayMethodInsert ("Bool", "bool", "true", "false", "3 True False True");
		}


		[Test]
		public void TestArrayOfFloatInsert ()
		{
			TLArrayMethodInsert ("Float", "float", "3.1f", "3.8f", "3 3.1 3.8 3.1");
		}

		[Test]
		public void TestArrayOfDoubleInsert ()
		{
			TLArrayMethodInsert ("Double", "double", "3.1", "3.8", "3 3.1 3.8 3.1");
		}


		[Test]
		public void TestArrayOfStringInsert ()
		{
			TLArrayMethodInsert ("String", "SwiftString", "SwiftString.FromString(\"mom\")",
								"SwiftString.FromString(\"dad\")", "3 mom dad mom");
		}


		void TLArrayMethodRemove (string swiftType, string csType, string csValue, string csNewValue, string output)
		{
			string swiftCode =
		$"public class FooTLAMR{swiftType} {{\n" +
				$"public init() {{ }}\n" +
		$"public func makeArray(a:{swiftType})  -> [{swiftType}]\n {{\n return [{swiftType}](repeating:a, count:2) \n}}\n" +
				$"}}";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftArray", false, new CSSimpleType (csType)),
													new CSIdentifier ("arr"),
						    new CSFunctionCall ($"new FooTLAMR{swiftType}().MakeArray", false, new CSIdentifier (csValue)));
			CSLine addLine = CSFunctionCall.FunctionCallLine ("arr.Insert", false, CSConstant.Val (1), new CSIdentifier (csNewValue));
			CSLine remLine = CSFunctionCall.FunctionCallLine ("arr.RemoveAt", false, CSConstant.Val (1));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("arr.Count"));
			CSForEach feach = new CSForEach (new CSSimpleType (csType), "x", new CSIdentifier ("arr"), null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val (" {0}"), feach.Ident));

			callingCode.Add (decl);
			callingCode.Add (addLine);
			callingCode.Add (remLine);
			callingCode.Add (call);
			callingCode.Add (feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLArrayMethodRemove{swiftType}");
		}

		[Test]
		public void TestArrayOfIntsRemove ()
		{
			TLArrayMethodRemove ("Int", "nint", "3", "-7", "2 3 3");
		}

		[Test]
		public void TestArrayOfBoolRemove ()
		{
			TLArrayMethodRemove ("Bool", "bool", "true", "false", "2 True True");
		}

		[Test]
		public void TestArrayOfFloatRemove ()
		{
			TLArrayMethodRemove ("Float", "float", "3.1f", "3.8f", "2 3.1 3.1");
		}

		[Test]
		public void TestArrayOfDoubleRemove ()
		{
			TLArrayMethodRemove ("Double", "double", "3.1", "3.8", "2 3.1 3.1");
		}

		[Test]
		public void TestArrayOfStringRemove ()
		{
			TLArrayMethodRemove ("String", "SwiftString", "SwiftString.FromString(\"mom\")",
					    "SwiftString.FromString(\"dad\")", "2 mom mom");
		}



		[Test]
		public void TestVariadicParameters ()
		{
			string swiftCode = @"public func intsAsArray (a:Int32 ... ) -> [Int32] {
return a
}";
			var arrId = new CSIdentifier ("arr");
			var otherarrId = new CSIdentifier ("otherarr");
			var arrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, arrId,
								     new CSFunctionCall ("SwiftArray<int>", true, CSConstant.Val (1), CSConstant.Val (2), CSConstant.Val (3)));
			var otherArrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, otherarrId, new CSFunctionCall ("TopLevelEntities.IntsAsArray", false, arrId));
			var feach = new CSForEach (CSSimpleType.Var, "x", otherarrId, null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, feach.Ident));

			var callingCode = CSCodeBlock.Create (arrDecl, otherArrDecl, feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, "123");
		}


		[Test]
		public void TestVariadiacInClass ()
		{
			string swiftCode = @"public class VarArr {
    public init () { }
    public func intsAsArray (a:Int32 ...) -> [Int32] {
        return a
    }
}
";
			var arrId = new CSIdentifier ("arr");
			var otherarrId = new CSIdentifier ("otherarr");
			var varArrId = new CSIdentifier ("vararr");
			var arrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, arrId,
								     new CSFunctionCall ("SwiftArray<int>", true, CSConstant.Val (1), CSConstant.Val (2), CSConstant.Val (3)));
			var varArrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, varArrId, new CSFunctionCall ("VarArr", true));
			var otherArrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, otherarrId, new CSFunctionCall ($"{varArrId.Name}.IntsAsArray", false, arrId));
			var feach = new CSForEach (CSSimpleType.Var, "x", otherarrId, null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, feach.Ident));

			var callingCode = CSCodeBlock.Create (arrDecl, varArrDecl, otherArrDecl, feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, "123");

		}

		[Test]
		public void TestVariadiacInOpenClass ()
		{
			string swiftCode = @"open class OpenVarArr {
    public init () { }
    open func intsAsArray (a:Int32 ...) -> [Int32] {
        return a
    }
}
";
			var arrId = new CSIdentifier ("arr");
			var otherarrId = new CSIdentifier ("otherarr");
			var varArrId = new CSIdentifier ("vararr");
			var arrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, arrId,
								     new CSFunctionCall ("SwiftArray<int>", true, CSConstant.Val (1), CSConstant.Val (2), CSConstant.Val (3)));
			var varArrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, varArrId, new CSFunctionCall ("OpenVarArr", true));
			var otherArrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, otherarrId, new CSFunctionCall ($"{varArrId.Name}.IntsAsArray", false, arrId));
			var feach = new CSForEach (CSSimpleType.Var, "x", otherarrId, null);
			feach.Body.Add (CSFunctionCall.FunctionCallLine ("Console.Write", false, feach.Ident));

			var callingCode = CSCodeBlock.Create (arrDecl, varArrDecl, otherArrDecl, feach);
			TestRunning.TestAndExecute (swiftCode, callingCode, "123", platform: PlatformName.macOS);

		}

	}
}
