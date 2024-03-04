// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Dynamo;
using Dynamo.CSLang;
using SwiftReflector.Inventory;
using NUnit.Framework;
using tomwiftytest;
using SwiftReflector.TypeMapping;
using NUnit.Framework.Legacy;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ConstructorTests {
		[Test]
		public void SimpleConstructor ()
		{
			string swiftcode = "public class None { public init() { } }";
			using (Stream stm = Compiler.CompileStringUsing (null, XCodeCompiler.Swiftc, swiftcode, "")) {
				var errors = new ErrorHandling ();
				ModuleInventory inventory = ModuleInventory.FromStream (stm, errors);
				Utils.CheckErrors (errors);
				ClassicAssert.AreEqual (1, inventory.Classes.Count ());
				ClassContents cl = inventory.Classes.First ();
				ClassicAssert.AreEqual ("noname.None", cl.Name.ToFullyQualifiedName ());
				ClassicAssert.AreEqual (2, cl.Constructors.Values.Count ());
			}
		}
	}
}

