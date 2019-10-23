// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class SwiftMethodWrapTests {

		public void WrapSingleMethod (string type, string returnVal)
		{
			string simpleClass = String.Format ("public final class Monty {{ public init() {{ }}\n public func val() -> {0} {{ return {1}; }} }}", type, returnVal);

			using (DisposableTempDirectory provider = new DisposableTempDirectory (null, false)) {
				Utils.CompileSwift (simpleClass, provider);
				Utils.CompileToCSharp (provider);
			}
		}

		[Test]
		public void WrapSingleMethodInt ()
		{
			WrapSingleMethod ("Int", "42");
		}

		[Test]
		public void WrapSingleMethodFloat ()
		{
			WrapSingleMethod ("Float", "42.0");
		}

		[Test]
		public void WrapSingleMethodDouble ()
		{
			WrapSingleMethod ("Double", "42.0");
		}

		[Test]
		public void WrapSingleMethodBool ()
		{
			WrapSingleMethod ("Bool", "true");
		}

		[Test]
		public void WrapSingleMethodUInt ()
		{
			WrapSingleMethod ("UInt", "42");
		}

		[Test]
		public void WrapSingleMethodString ()
		{
			WrapSingleMethod ("Monty", "self");
		}

		[Test]
		public void WrapSingleMethodClass ()
		{
			WrapSingleMethod ("String", "\"nothing\"");
		}
	}
}

