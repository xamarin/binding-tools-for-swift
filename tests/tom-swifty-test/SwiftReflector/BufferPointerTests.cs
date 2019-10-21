// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using Dynamo.CSLang;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	public class BufferPointerTests {

		[TestCase ("UnsafeRawBufferPointer")]
		[TestCase ("UnsafeMutableRawBufferPointer")]
		public void TestAllocCount (string tag)
		{
			var swiftCode = $"public func neverUsed{tag}() {{\n}}\n";
			var ptrID = new CSIdentifier ("ptr");
			var ptrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ptrID,
				new CSFunctionCall ("System.Runtime.InteropServices.Marshal.AllocHGlobal", false, CSConstant.Val(128)));
			var clID = new CSIdentifier ("ubp");
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall (tag, true, ptrID, CSConstant.Val (128)));

			var printer = CSFunctionCall.ConsoleWriteLine (clID.Dot (new CSIdentifier ("Count")));
			var cleanUp = CSFunctionCall.FunctionCallLine ("System.Runtime.InteropServices.Marshal.FreeHGlobal", false, ptrID);
			var callingCode = CSCodeBlock.Create (ptrDecl, clDecl, printer, cleanUp);

			TestRunning.TestAndExecute (swiftCode, callingCode, "128\n", testName: "TestAllocCount" + tag);
		}


		[TestCase ("UnsafeRawBufferPointer")]
		[TestCase ("UnsafeMutableRawBufferPointer")]
		public void TestReadablilty (string tag)
		{
			var swiftCode = $"public func stillNeverUsed{tag}() {{\n}}\n";
			var ptrID = new CSIdentifier ("ptr");
			var ptrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ptrID,
				new CSFunctionCall ("System.Runtime.InteropServices.Marshal.AllocHGlobal", false, CSConstant.Val (128)));
			var writeCall = CSFunctionCall.FunctionCallLine ("System.Runtime.InteropServices.Marshal.WriteInt32", false, ptrID, CSConstant.Val (0x04040404));
			var clID = new CSIdentifier ("ubp");
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall (tag, true, ptrID, CSConstant.Val (128)));

			var printer = CSFunctionCall.ConsoleWriteLine (new CSIdentifier ($"{clID.Name}[0]"));
			var cleanUp = CSFunctionCall.FunctionCallLine ("System.Runtime.InteropServices.Marshal.FreeHGlobal", false, ptrID);
			var callingCode = CSCodeBlock.Create (ptrDecl, writeCall, clDecl, printer, cleanUp);

			TestRunning.TestAndExecute (swiftCode, callingCode, "4\n", testName: "TestAllocCount" + tag);
		}



		[TestCase ("UnsafeRawBufferPointer")]
		[TestCase ("UnsafeMutableRawBufferPointer")]
		public void TestDebugDescription (string tag)
		{
			var swiftCode = $"public func neverEverUsed{tag}() {{\n}}\n";
			var ptrID = new CSIdentifier ("ptr");
			var ptrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ptrID,
				new CSFunctionCall ("System.Runtime.InteropServices.Marshal.AllocHGlobal", false, CSConstant.Val (128)));
			var clID = new CSIdentifier ("ubp");
			var clDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, clID, new CSFunctionCall (tag, true, ptrID, CSConstant.Val (128)));

			var printer = CSFunctionCall.ConsoleWriteLine (clID.Dot (new CSIdentifier ("DebugDescription")));
			var cleanUp = CSFunctionCall.FunctionCallLine ("System.Runtime.InteropServices.Marshal.FreeHGlobal", false, ptrID);
			var callingCode = CSCodeBlock.Create (ptrDecl, clDecl, printer, cleanUp);

			TestRunning.TestAndExecute (swiftCode, callingCode, null, testName: "TestAllocCount" + tag,
				expectedOutputContains: new string [] { $"{tag}(start:", "count: 128)" });
		}


		[TestCase ("UnsafeRawPointer")]
		[TestCase ("UnsafeMutableRawPointer")]
		public void TestIdentityPointer (string tag)
		{
			var swiftCode = $"public func identityPointer(p: {tag}) -> {tag} {{\n return p\n }}";

			var ptrID = new CSIdentifier ("ptr");
			var ptrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, ptrID,
				new CSCastExpression (new CSSimpleType (tag), new CSFunctionCall ("IntPtr", true, CSConstant.Val (0x01020304))));
			var assign = CSAssignment.Assign (ptrID, new CSFunctionCall ("TopLevelEntities.IdentityPointer", false, ptrID));
			var printer = CSFunctionCall.ConsoleWriteLine (new CSCastExpression (CSSimpleType.IntPtr, ptrID));
			var callingCode = CSCodeBlock.Create (ptrDecl, assign, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "16909060\n", testName: "IdentityPointer" + tag);
		}

	}
}

