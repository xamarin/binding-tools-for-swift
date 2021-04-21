// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using tomwiftytest;
using Dynamo.CSLang;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ProtowitnessTest {

		[Test]
		[Ignore ("Not loading RegisterAccess - https://github.com/xamarin/binding-tools-for-swift/issues/656")]
		public void VerifyProtoAccess ()
		{
			var swiftCode = @"
import RegisterAccess

public protocol Ageist {
	func getAge () -> Int
}

public class AgeImp : Ageist {
	public init () { }
	public func getAge () -> Int {
		return 42
	}
}

@inline(never)
public func protoWitness<T: Ageist> (of: T) -> UnsafeRawPointer?
{
    return RegisterAccess.swiftAsmArg2()
}

public func myWitness<T: Ageist> (x: T) -> UnsafeRawPointer {
	return protoWitness(of: x)!
}
";
			// var proto = new AgeImp ();
			// var witnessPtr = StructMarshal.Marshaler.ProtocolWitnessof (typeof (IAgeist), typeof(AgeImp));
			// var gottenWitness = TopLevelEntities.ProtoWitness<AgeImp>(proto);
			// Console.Writeline(witnessPtr == gottenWitness.Pointer);

			var protoId = new CSIdentifier ("proto");
			var witnessPtrId = new CSIdentifier ("witnessPtr");
			var gottenWitnessId = new CSIdentifier ("gottenWitness");

			var protoDecl = CSVariableDeclaration.VarLine (protoId, new CSFunctionCall ("AgeImp", true));
			var witnessDecl = CSVariableDeclaration.VarLine (witnessPtrId, new CSFunctionCall ("StructMarshal.Marshaler.ProtocolWitnessof", false,
				new CSSimpleType ("IAgeist").Typeof (), new CSSimpleType ("AgeImp").Typeof ()));
			var gottenDecl = CSVariableDeclaration.VarLine (gottenWitnessId, new CSFunctionCall ("TopLevelEntities.MyWitness<AgeImp>", false, protoId));
			var printer = CSFunctionCall.ConsoleWriteLine (witnessPtrId == gottenWitnessId.Dot (new CSIdentifier ("Pointer")));

			var callingCode = CSCodeBlock.Create (protoDecl, witnessDecl, gottenDecl, printer);

			TestRunning.TestAndExecute (swiftCode, callingCode, "True\n", platform: PlatformName.macOS);

		}
	}
}
