using System;
using System.Runtime.InteropServices;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;
using tomwiftytest;

namespace SwiftReflector
{
	// These are experimental tests to look at the possibility of
	// using opaque structs to hold the payload for value types that
	// are passed by reference.
	// At present the .NET runtime only supports passing structs by
	// value that are 2 machine words or 1 machine word, but when this
	// is supported, we can support calling functions/methods that take or
	// return value types without writing wrappers.
	//
	// What we had been doing was doing a stack alloc of an array (cheap)
	// then calling the swift value witness table entry that copies (with retains
	// if needed) the value type from one place to another (not cheap) and upon return
	// calls the value witness table destroy function (not cheap)
	//
	// What we can do instead is use no wrapper function and pinvoke directly (cheaper)
	// and pass by value by blitting the payload array (very cheap).
	// We can get away with not worrying about retain release because swift
	// got rid of the retain-before-call and release-on-return approach as it's
	// redundant.

	[TestFixture]
	public class ExpermimentalTests
	{
		[Test]
		public void SingleIntStruct ()
		{
			var swiftCode = @"
@frozen
public struct OneInt {
	public var X:Int
	public init () { X = 17 }
}
";

			// C# aux code:
			// public struct XXOpaque1 {
			// nint x0
			// public static unsafe nint Getter (OneInt self)
			// {
			//    fixed (byte* thisSwiftDataPtr = SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType (self)) {
			//        var p = (XXOpaque1 *)thisSwiftDataPtr;
			//        return XGetter (*p);
			// }
			// [DllImport ("libExpermimentalTests.dylib", EntryPoint = "_$s18ExpermimentalTests6OneIntV1XSivg")]
			// public static extern nint XGetter (XXOpaque1 r);
			//

			var xxopName = new CSIdentifier ("XXOpaque1");
			var selfID = (CSIdentifier)"self";
			var thisSwiftDataPtrID = (CSIdentifier)"thisSwiftDataPtr";
			var pID = (CSIdentifier)"p";
			var getterID = (CSIdentifier)"Getter";
			var xgetterID = (CSIdentifier)"XGetter";

			var st = new CSStruct (CSVisibility.Public, xxopName);
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x0", value: null, CSVisibility.None));
			var body = new CSCodeBlock ();
			var fixedBlock = new CSFixedCodeBlock (CSSimpleType.ByteStar, thisSwiftDataPtrID,
				new CSFunctionCall ("SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType", false, selfID), null);

			fixedBlock.Add (CSVariableDeclaration.VarLine (pID, new CSCastExpression (new CSSimpleType ($"{xxopName.Name}*"), thisSwiftDataPtrID)));
			fixedBlock.Add (CSReturn.ReturnLine (new CSFunctionCall (xgetterID.Name, false, new CSUnaryExpression (CSUnaryOperator.Indirection, pID))));
			body.Add (fixedBlock);

			var getter = new CSMethod (CSVisibility.Public, CSMethodKind.StaticUnsafe, CSSimpleType.NInt, getterID,
				new CSParameterList (new CSParameter (new CSSimpleType ("OneInt"), selfID)), body);
			st.Methods.Add (getter);


			var pinvoke = CSMethod.PInvoke (CSVisibility.Public, CSSimpleType.NInt, xgetterID.Name,
				CSConstant.Val ("libExpermimentalTests.dylib"), "$s18ExpermimentalTests6OneIntV1XSivg", new CSParameterList (new CSParameter (new CSSimpleType (xxopName.Name), (CSIdentifier)"r")));
			st.Methods.Add (pinvoke);

			var decl = CSVariableDeclaration.VarLine ("oneInt", CSFunctionCall.Ctor ("OneInt"));
			var printerGenerated = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"oneInt.X");
			var printerDerived = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{xxopName.Name}.{getterID.Name}", false, (CSIdentifier)"oneInt"));
			var callingCode = CSCodeBlock.Create (decl, printerGenerated, printerDerived);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n17\n", otherClass: st);
		}

		[Test]
		public void ByteSingleIntStruct ()
		{
			var swiftCode = @"
@frozen
public struct OneInt {
	public var A:Int8
	public var X:Int
	public init () { A = 7; X = 17 }
}
";

			// C# aux code:
			// public struct XXOpaque2 {
			// nint x0
			// nint x1
			// public static unsafe nint Getter (OneInt self)
			// {
			//    fixed (byte* thisSwiftDataPtr = SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType (self)) {
			//        var p = (XXOpaque2 *)thisSwiftDataPtr;
			//        return XGetter (*p);
			// }
			// [DllImport ("libExpermimentalTests.dylib", EntryPoint = "_$s18ExpermimentalTests6OneIntV1XSivg")]
			// public static extern nint XGetter (XXOpaque2 r);
			//

			var xxopName = new CSIdentifier ("XXOpaque2");
			var selfID = (CSIdentifier)"self";
			var thisSwiftDataPtrID = (CSIdentifier)"thisSwiftDataPtr";
			var pID = (CSIdentifier)"p";
			var getterID = (CSIdentifier)"Getter";
			var xgetterID = (CSIdentifier)"XGetter";

			var st = new CSStruct (CSVisibility.Public, xxopName);
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x0", value: null, CSVisibility.None));
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x1", value: null, CSVisibility.None));
			var body = new CSCodeBlock ();
			var fixedBlock = new CSFixedCodeBlock (CSSimpleType.ByteStar, thisSwiftDataPtrID,
				new CSFunctionCall ("SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType", false, selfID), null);

			fixedBlock.Add (CSVariableDeclaration.VarLine (pID, new CSCastExpression (new CSSimpleType ($"{xxopName.Name}*"), thisSwiftDataPtrID)));
			fixedBlock.Add (CSReturn.ReturnLine (new CSFunctionCall (xgetterID.Name, false, new CSUnaryExpression (CSUnaryOperator.Indirection, pID))));
			body.Add (fixedBlock);

			var getter = new CSMethod (CSVisibility.Public, CSMethodKind.StaticUnsafe, CSSimpleType.NInt, getterID,
				new CSParameterList (new CSParameter (new CSSimpleType ("OneInt"), selfID)), body);
			st.Methods.Add (getter);


			var pinvoke = CSMethod.PInvoke (CSVisibility.Public, CSSimpleType.NInt, xgetterID.Name,
				CSConstant.Val ("libExpermimentalTests.dylib"), "$s18ExpermimentalTests6OneIntV1XSivg", new CSParameterList (new CSParameter (new CSSimpleType (xxopName.Name), (CSIdentifier)"r")));
			st.Methods.Add (pinvoke);

			var decl = CSVariableDeclaration.VarLine ("oneInt", CSFunctionCall.Ctor ("OneInt"));
			var printerGenerated = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"oneInt.X");
			var printerDerived = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{xxopName.Name}.{getterID.Name}", false, (CSIdentifier)"oneInt"));
			var callingCode = CSCodeBlock.Create (decl, printerGenerated, printerDerived);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n17\n", otherClass: st);
		}

		[Test]
		public void ByteByteSingleIntStruct ()
		{
			var swiftCode = @"
@frozen
public struct OneInt {
	public var A:Int8
	public var B:Int8
	public var X:Int
	public init () { A = 7; B = 13; X = 17 }
}
";

			// C# aux code:
			// public struct XXOpaque2 {
			// nint x0
			// nint x1
			// public static unsafe nint Getter (OneInt self)
			// {
			//    fixed (byte* thisSwiftDataPtr = SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType (self)) {
			//        var p = (XXOpaque2 *)thisSwiftDataPtr;
			//        return XGetter (*p);
			// }
			// [DllImport ("libExpermimentalTests.dylib", EntryPoint = "_$s18ExpermimentalTests6OneIntV1XSivg")]
			// public static extern nint XGetter (XXOpaque2 r);
			//

			var xxopName = new CSIdentifier ("XXOpaque2");
			var selfID = (CSIdentifier)"self";
			var thisSwiftDataPtrID = (CSIdentifier)"thisSwiftDataPtr";
			var pID = (CSIdentifier)"p";
			var getterID = (CSIdentifier)"Getter";
			var xgetterID = (CSIdentifier)"XGetter";

			var st = new CSStruct (CSVisibility.Public, xxopName);
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x0", value: null, CSVisibility.None));
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x1", value: null, CSVisibility.None));
			var body = new CSCodeBlock ();
			var fixedBlock = new CSFixedCodeBlock (CSSimpleType.ByteStar, thisSwiftDataPtrID,
				new CSFunctionCall ("SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType", false, selfID), null);

			fixedBlock.Add (CSVariableDeclaration.VarLine (pID, new CSCastExpression (new CSSimpleType ($"{xxopName.Name}*"), thisSwiftDataPtrID)));
			fixedBlock.Add (CSReturn.ReturnLine (new CSFunctionCall (xgetterID.Name, false, new CSUnaryExpression (CSUnaryOperator.Indirection, pID))));
			body.Add (fixedBlock);

			var getter = new CSMethod (CSVisibility.Public, CSMethodKind.StaticUnsafe, CSSimpleType.NInt, getterID,
				new CSParameterList (new CSParameter (new CSSimpleType ("OneInt"), selfID)), body);
			st.Methods.Add (getter);


			var pinvoke = CSMethod.PInvoke (CSVisibility.Public, CSSimpleType.NInt, xgetterID.Name,
				CSConstant.Val ("libExpermimentalTests.dylib"), "$s18ExpermimentalTests6OneIntV1XSivg", new CSParameterList (new CSParameter (new CSSimpleType (xxopName.Name), (CSIdentifier)"r")));
			st.Methods.Add (pinvoke);

			var decl = CSVariableDeclaration.VarLine ("oneInt", CSFunctionCall.Ctor ("OneInt"));
			var printerGenerated = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"oneInt.X");
			var printerDerived = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{xxopName.Name}.{getterID.Name}", false, (CSIdentifier)"oneInt"));
			var callingCode = CSCodeBlock.Create (decl, printerGenerated, printerDerived);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n17\n", otherClass: st);
		}

		[Test]
		public void ByteByteShortSingleIntStruct ()
		{
			var swiftCode = @"
@frozen
public struct OneInt {
	public var A:Int8
	public var B:Int8
	public var C:Int16
	public var X:Int
	public init () { A = 7; B = 13; C = 873; X = 17 }
}
";

			// C# aux code:
			// public struct XXOpaque2 {
			// nint x0
			// nint x1
			// public static unsafe nint Getter (OneInt self)
			// {
			//    fixed (byte* thisSwiftDataPtr = SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType (self)) {
			//        var p = (XXOpaque2 *)thisSwiftDataPtr;
			//        return XGetter (*p);
			// }
			// [DllImport ("libExpermimentalTests.dylib", EntryPoint = "_$s18ExpermimentalTests6OneIntV1XSivg")]
			// public static extern nint XGetter (XXOpaque2 r);
			//

			var xxopName = new CSIdentifier ("XXOpaque2");
			var selfID = (CSIdentifier)"self";
			var thisSwiftDataPtrID = (CSIdentifier)"thisSwiftDataPtr";
			var pID = (CSIdentifier)"p";
			var getterID = (CSIdentifier)"Getter";
			var xgetterID = (CSIdentifier)"XGetter";

			var st = new CSStruct (CSVisibility.Public, xxopName);
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x0", value: null, CSVisibility.None));
			st.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.NInt, "x1", value: null, CSVisibility.None));
			var body = new CSCodeBlock ();
			var fixedBlock = new CSFixedCodeBlock (CSSimpleType.ByteStar, thisSwiftDataPtrID,
				new CSFunctionCall ("SwiftRuntimeLibrary.SwiftMarshal.StructMarshal.Marshaler.PrepareValueType", false, selfID), null);

			fixedBlock.Add (CSVariableDeclaration.VarLine (pID, new CSCastExpression (new CSSimpleType ($"{xxopName.Name}*"), thisSwiftDataPtrID)));
			fixedBlock.Add (CSReturn.ReturnLine (new CSFunctionCall (xgetterID.Name, false, new CSUnaryExpression (CSUnaryOperator.Indirection, pID))));
			body.Add (fixedBlock);

			var getter = new CSMethod (CSVisibility.Public, CSMethodKind.StaticUnsafe, CSSimpleType.NInt, getterID,
				new CSParameterList (new CSParameter (new CSSimpleType ("OneInt"), selfID)), body);
			st.Methods.Add (getter);


			var pinvoke = CSMethod.PInvoke (CSVisibility.Public, CSSimpleType.NInt, xgetterID.Name,
				CSConstant.Val ("libExpermimentalTests.dylib"), "$s18ExpermimentalTests6OneIntV1XSivg", new CSParameterList (new CSParameter (new CSSimpleType (xxopName.Name), (CSIdentifier)"r")));
			st.Methods.Add (pinvoke);

			var decl = CSVariableDeclaration.VarLine ("oneInt", CSFunctionCall.Ctor ("OneInt"));
			var printerGenerated = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"oneInt.X");
			var printerDerived = CSFunctionCall.ConsoleWriteLine (new CSFunctionCall ($"{xxopName.Name}.{getterID.Name}", false, (CSIdentifier)"oneInt"));
			var callingCode = CSCodeBlock.Create (decl, printerGenerated, printerDerived);
			TestRunning.TestAndExecute (swiftCode, callingCode, "17\n17\n", otherClass: st);
		}
	}
}

