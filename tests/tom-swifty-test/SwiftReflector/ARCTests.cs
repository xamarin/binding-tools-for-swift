// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using SwiftReflector.Inventory;
using SwiftReflector.IOUtils;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class ARCTests {
		[Test]
		public void ArcSingleClass ()
		{
			string swiftCode =
				"public final class Foo {\npublic var _nm:String\npublic init(name:String) {\n_nm = name }\n" +
		"deinit {\nprint(_nm)\n}\n}\n";
			swiftCode += TestRunning.CreateSwiftConsoleRedirect ();

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ()) {
				Utils.CompileSwift (swiftCode, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");
				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = CSFile.Create (use, ns);


				CSIdentifier inst = new CSIdentifier ("inst");
				CSLine newer = CSVariableDeclaration.VarLine ((CSSimpleType)"Foo", inst, CSFunctionCall.Ctor ("Foo", 
										CSFunctionCall.Function ("SwiftString.FromString", CSConstant.Val ("nothing"))));

				CSLine disposer = CSFunctionCall.FunctionCallLine (inst.Name + ".Dispose", false);

				CSCodeBlock mainBody = CSCodeBlock.Create (newer, disposer);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
				                              (CSIdentifier)"Main", new CSParameterList (new CSParameter (CSSimpleType.CreateArray ("string"), "args")), mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);

				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));
				var exeOutFilename = provider.UniquePath (null, "CSWrap", "exe");

				CodeWriter.WriteToFile (csOutFilename, csfile);

				Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), exeOutFilename);

				TestRunning.CopyTestReferencesTo (provider.DirectoryPath);
				string output = Compiler.RunWithDotnet (exeOutFilename, provider.DirectoryPath);
				ClassicAssert.AreEqual ("nothing\n", output);
			}
		}


		[Test]
		public void ArcClassStruct ()
		{
			string swiftCode =
				"public final class Foo {\npublic var _nm:String\npublic init(name:String) {\n_nm = name }\n" +
		"deinit {\nprint(_nm)\n}\n}\n" +
				"public struct Bar {\n public var a:Foo\n public init(f:Foo) {\n a = f\n}\n }\n"
				;
			swiftCode += TestRunning.CreateSwiftConsoleRedirect ();

			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {
				Utils.CompileSwift (swiftCode, provider);

				string libFileName = Path.Combine (provider.DirectoryPath, "libXython.dylib");
				var errors = new ErrorHandling ();

				ModuleInventory.FromFile (libFileName, errors);
				Utils.CheckErrors (errors);

				Utils.CompileToCSharp (provider);

				CSUsingPackages use = new CSUsingPackages ("System", "System.Runtime.InteropServices", "SwiftRuntimeLibrary");

				CSNamespace ns = new CSNamespace ("Xython");
				CSFile csfile = CSFile.Create (use, ns);

				CSIdentifier inst = new CSIdentifier ("inst");
				CSLine newer = CSVariableDeclaration.VarLine ((CSSimpleType)"Foo", inst, CSFunctionCall.Ctor ("Foo", 
                                                                             CSFunctionCall.Function ("SwiftString.FromString", CSConstant.Val ("nothing"))));

				CSIdentifier inst1 = new CSIdentifier ("bar");
				CSLine newer1 = CSVariableDeclaration.VarLine ((CSSimpleType)"Bar", inst1, CSFunctionCall.Ctor ("Bar", inst));

				CSLine disposer = CSFunctionCall.FunctionCallLine (inst.Name + ".Dispose", false);
				CSLine disposer1 = CSFunctionCall.FunctionCallLine (inst1.Name + ".Dispose", false);

				CSCodeBlock mainBody = CSCodeBlock.Create (newer, newer1, disposer, disposer1);

				CSMethod main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
					(CSIdentifier)"Main", new CSParameterList (new CSParameter (CSSimpleType.CreateArray ("string"), "args")),
					mainBody);
				CSClass mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
				ns.Block.Add (mainClass);

				string csOutFilename = provider.ProvideFileFor (provider.UniqueName (null, "CSWrap", "cs"));
				var exeOutFilename = provider.UniquePath (null, "CSWrap", "exe");

				CodeWriter.WriteToFile (csOutFilename, csfile);

				Compiler.CSCompile (provider.DirectoryPath, Directory.GetFiles (provider.DirectoryPath, "*.cs"), exeOutFilename);

				TestRunning.CopyTestReferencesTo (provider.DirectoryPath);

				string output = Compiler.RunWithDotnet (exeOutFilename, provider.DirectoryPath);
				ClassicAssert.AreEqual ("nothing\n", output);
			}
		}
	}
}
