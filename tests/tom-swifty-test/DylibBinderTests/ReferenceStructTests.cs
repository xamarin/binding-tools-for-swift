// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

namespace tomwiftytest.DylibBinderTests {

	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]

	public class ReferenceStructTests {

		[Test]
		public void TestReferenceStructCreated ()
		{
			var swiftCode = @"
public struct Refer<T> {
    var contents: T
    public var refCount:Int = 0
    public init (a: T) {
        contents = a
        refCount = refCount + 1
    }
    public mutating func remove(at: Int) -> T {
       refCount = refCount - at
       return contents
    }
}";

			// write the dynamo code to instantiate a Refer<bool>
			CSLine referDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"Refer", false,
											new CSSimpleType ("bool")),
								   "referStruct", new CSFunctionCall ($"Refer<bool>", true,
											   new CSFunctionCall ("SwiftValueTypeCtorArgument", true)));

			// call obj.remove(1)
			CSFunctionCall removeDecl = CSFunctionCall.Function ("referStruct.Remove", (CSIdentifier)"1");
			CSLine callRemoveDecl = CSFunctionCall.ConsoleWriteLine (removeDecl);

			// print obj.RefCount
			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"referStruct.RefCount");

			// assert that it prints 0
			CSCodeBlock callingCode = CSCodeBlock.Create (referDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "0\n", testName: $"ReferenceStruct<bool>");
		}

		[Test]
		[Ignore ("TJ - Need to find out why this struct has a false value")]
		public void ConstructStruct ()
		{
			var swiftCode = @"
public struct Refer<T> {
    public var contents: T
    public init (a: T) {
        contents = a
    }
}";

			// write the dynamo code to instantiate a Refer<bool>
			CSLine referDecl = CSVariableDeclaration.VarLine (new CSSimpleType ($"Refer", false,
											new CSSimpleType ("bool")),
								   "referStruct", new CSFunctionCall ($"Refer<bool>", true,
											   new CSFunctionCall ("SwiftValueTypeCtorArgument", true)));

			// print obj.RefCount
			CSLine printer = CSFunctionCall.ConsoleWriteLine ((CSIdentifier)"referStruct.SwiftData");
			// 0

			// assert that it prints 0
			CSCodeBlock callingCode = CSCodeBlock.Create (referDecl, printer);
			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", testName: $"InitStruct<bool>");
		}
	}
}
