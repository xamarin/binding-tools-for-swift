// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using tomswifty;
using tomwiftytest;
using Dynamo;
using Dynamo.CSLang;
using SwiftReflector;

namespace CommandLineTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class OutputTests {
		[Test]
		[Ignore ("We handle method descriptors now.")]
		public void TestWarningOutput ()
		{
			// This swift code produces a warning when run through binding-tools-for-swift.
			// If we ever fix binding-tools-for-swift to not show a warning for this particular code,
			// it needs to change to something else (that makes binding-tools-for-swift produce a warning).
			var swiftCode = "public class Foo {\npublic var x:Int = 3\n }";
			var output = RunBindingToolsForSwift (swiftCode, "TestWarningOutput");
			var lines = output.Split ('\n');
			CollectionAssert.Contains (lines, "warning SM4018: entry _$s11OutputTests3FooC1xSivMTq uses an unsupported swift feature, skipping.");
		}

		string RunBindingToolsForSwift (string swiftCode, string testName = null)
		{
			var nameSpace = "OutputTests";
			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				var compiler = Utils.CompileSwift (swiftCode, provider, nameSpace);

				var libName = $"lib{nameSpace}.dylib";

				using (DisposableTempDirectory temp = new DisposableTempDirectory ()) {
					File.Copy (Path.Combine (compiler.DirectoryPath, libName), Path.Combine (temp.DirectoryPath, libName));

					return Utils.CompileToCSharp (provider, temp.DirectoryPath, nameSpace, separateProcess: true);
				}
			}
		}
	}
}
