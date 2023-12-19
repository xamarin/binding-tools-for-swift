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
		public static string kSwiftRuntimeOutputDirectory = Path.Combine (TestContext.CurrentContext.TestDirectory, "../../../../SwiftRuntimeLibrary/bin/Debug");
		public const string kSwiftRuntimeLibrary = "SwiftRuntimeLibrary";


		public static string kSwiftRuntimeMacOutputDirectory = Path.Combine (TestContext.CurrentContext.TestDirectory, "../../../../SwiftRuntimeLibrary.Mac/bin/Debug");
		public static string kSwiftRuntimeiOSOutputDirectory = Path.Combine (TestContext.CurrentContext.TestDirectory, "../../../../SwiftRuntimeLibrary.iOS/bin/Debug");
		public const string kSwiftRuntimeLibraryMac = "SwiftRuntimeLibrary.Mac";
		public const string kSwiftRuntimeLibraryiOS = "SwiftRuntimeLibrary.iOS";

		public static string kXamarinMacDir = "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/Xamarin.Mac";
		public static string kXamariniOSDir = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS";

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


		[Test]
		public void SwiftRuntimeLibraryExists ()
		{
			ClassicAssert.IsTrue (Directory.Exists (kSwiftRuntimeOutputDirectory));
			ClassicAssert.IsTrue (File.Exists (Path.Combine (kSwiftRuntimeOutputDirectory, kSwiftRuntimeLibrary + ".dll")));
		}


		[Test]
		public void SwiftRuntimeLibraryMacExists ()
		{
			ClassicAssert.IsTrue (Directory.Exists (kSwiftRuntimeMacOutputDirectory));
			ClassicAssert.IsTrue (File.Exists (Path.Combine (kSwiftRuntimeMacOutputDirectory, kSwiftRuntimeLibraryMac + ".dll")));
		}


		string CompileWithInvokingCode (CSFile cs, CSLine invoker, string nameSpace)
		{
			CSNamespace ns = new CSNamespace (nameSpace);
			CSCodeBlock mainBody = CSCodeBlock.Create (invoker);
			CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
				new CSIdentifier ("Main"), new CSParameterList (new CSParameter (new CSSimpleType ("string", true), "args")),
				mainBody);
			CSClass cl = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
			ns.Block.Add (cl);

			cs.Namespaces.Add (ns);

			using (DisposableTempFile csOut = new DisposableTempFile (null, null, "cs", true)) {
				CodeWriter.WriteToFile (csOut.Filename, cs);

				using (Stream csExeStm = Compiler.CompileUsing (null, XCodeCompiler.CSharpExe, csOut.Filename,
				                                                $"-lib:{kSwiftRuntimeMacOutputDirectory} -r:{kSwiftRuntimeLibraryMac}")) {
					using (DisposableTempFile csExe = new DisposableTempFile (null, null, "exe", true)) {
						csExeStm.CopyTo (csExe.Stream);
						csExe.Stream.Close ();
						string output = Compiler.RunWithMono (csExe.Filename);
						return output;
					}
				}
			}
		}


	}
}

