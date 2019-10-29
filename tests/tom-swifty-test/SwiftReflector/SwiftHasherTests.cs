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
	public class SwiftHasherTests {
		[Test]
		public void HashSimpleTest()
		{
			var swiftCode = @"public func hashNoOp () { }";

			var hashId = new CSIdentifier ("hash");
			var bytesId = new CSIdentifier ("bytes");
			var hashInit = new CSFunctionCall ("SwiftHasher", true);
			var hashDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, hashId, hashInit);
			var bytesDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, bytesId, new CSIdentifier ("new byte[] { 0, 1, 2, 3, 4 }"));
			var hashCall = CSFunctionCall.FunctionCallLine ($"{hashId.Name}.Combine", false, bytesId);
			var finalizeCall = new CSFunctionCall ($"{hashId}.FinalizeHasher", false);

			var hashValue0Id = new CSIdentifier ("hashValue0");
			var hashValue1Id = new CSIdentifier ("hashValue1");
			var hashValue0Decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, hashValue0Id, finalizeCall);
			var reInitLine = CSAssignment.Assign (hashId, hashInit);
			var hashValue1Decl = CSVariableDeclaration.VarLine (CSSimpleType.Var, hashValue1Id, finalizeCall);

			var printer = CSFunctionCall.ConsoleWriteLine (hashValue0Id == hashValue1Id);

			var callingCode = CSCodeBlock.Create (hashDecl, bytesDecl, hashCall, hashValue0Decl, reInitLine, hashCall, hashValue1Decl, printer);

			// this mess should generate:
	    		// var hash = new SwiftHasher ();
			// var bytes = new byte [] { 0, 1, 2, 3, 4 };
	    		// hash.Combine (bytes);
			// var hashValue0 = hash.Finalize0 ();
	    		// hash = new SwiftHasher ();
			// hash.Combine (bytes);
	    		// var hashValue1 = hash.Finalize0 ();
			// Console.WriteLine (hashValue0 == hashValue1);

			// this is essentially asserting that the hashvalue of the same byte array will be the same
	    		// when rehashed. This condition is invariant within process invocations in swift although
			// it may be different across different process invocations (in other words, Hasher seeds
	    		// using some process-specific value that may change run to run).


			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n");
		}

	}
}
