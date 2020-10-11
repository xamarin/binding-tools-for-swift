// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using SwiftReflector;
using SwiftReflector.IOUtils;
using Xamarin.Utils;
using System.Text.RegularExpressions;

[assembly: Parallelizable]

namespace tomwiftytest {

	static class TestRunningCodeGenerator 
	{
		static CSUsingPackages GetTestEntryPointUsings (string nameSpace, PlatformName platform)
		{
			var usings = new CSUsingPackages ()
				.And ("System")
				.And ("System.IO")
				.And (nameSpace)
				.And ("SwiftRuntimeLibrary")
				.And ("SwiftRuntimeLibrary.SwiftMarshal")
				.And ("System.Runtime.InteropServices");

			if (platform != PlatformName.None)
				usings = usings.And ("Foundation");

			return usings;
		}

		public static CSFile GenerateTestEntry (CodeElementCollection<ICodeElement> callingCode, string testName, string nameSpace, PlatformName platform, CSClass otherClass = null,
			bool enforceUTF8Encoding = false)
		{
			var use = GetTestEntryPointUsings (nameSpace, platform);

			var ns = new CSNamespace (nameSpace);
			if (otherClass != null)
				ns.Block.Add (otherClass);

			var mainBody = new CSCodeBlock (callingCode);
			if (enforceUTF8Encoding) {
				InsertUTF8Assertion (mainBody);
			}
			mainBody.Add (CaptureSwiftOutputPostlude (testName));
			var main = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Void,
						 (CSIdentifier)"Main", new CSParameterList (new CSParameter (CSSimpleType.CreateArray ("string"), "args")),
						 mainBody);
			var mainClass = new CSClass (CSVisibility.Public, "NameNotImportant", new CSMethod [] { main });
			AddSupportingCode (mainClass, platform);

			ns.Block.Add (mainClass);

			return CSFile.Create (use, ns);
		}

		static void AddSupportingCode (CSClass cl, PlatformName platform)
		{
			switch (platform) {
			case PlatformName.macOS:
				AddMacSpecificInitializationCode (cl);
				break;
			case PlatformName.iOS:
			case PlatformName.tvOS:
			case PlatformName.watchOS:
				AddXISpecificInitializationCode (cl);
				break;
			case PlatformName.None:
				break;
			default:
				throw new NotImplementedException (platform.ToString ());
			}
		}

		static void AddXISpecificInitializationCode (CSClass cl)
		{
			// I'm not sure anything needs to be done.
		}

		static void AddMacSpecificInitializationCode (CSClass cl)
		{
			var parms = new CSParameterList {
				new CSParameter (CSSimpleType.String, "path"),
				new CSParameter (CSSimpleType.Int, "mode")
			};

			var dlopen = CSMethod.PrivatePInvoke (CSSimpleType.IntPtr, "dlopen", "/usr/lib/libSystem.dylib", "dlopen", parms);
			cl.Methods.Add (dlopen);

			var appID = new CSIdentifier ("Application");
			cl.Fields.Add (CSFieldDeclaration.FieldLine (new CSSimpleType (typeof (Type)), appID, null, CSVisibility.None, true));

			// NSApplication.Init ();
			var body = CSCodeBlock.Create (CSFunctionCall.FunctionCallLine ("AppKit.NSApplication.Init", false));

			var staticCtor = new CSMethod (CSVisibility.None, CSMethodKind.Static, null, cl.Name, new CSParameterList (), body);
			cl.Methods.Add (staticCtor);

		}

		static CSFunctionCall GetTypeCall (string parm)
		{
			return new CSFunctionCall ("Type.GetType", false, CSConstant.Val (parm));
		}

		static CSUsingPackages GetTestClassUsings (string nameSpace)
		{
			return new CSUsingPackages ()
				.And ("System")
				.And ("System.IO")
				.And (nameSpace)
				.And ("SwiftRuntimeLibrary")
				.And ("SwiftRuntimeLibrary.SwiftMarshal")
				.And ("Foundation")
				.And ("TomTest");
		}

		public static Tuple<CSNamespace, CSUsingPackages> CreateTestClass (CodeElementCollection<ICodeElement> callingCode, string testName,
					  string expectedOutput, string nameSpace, string testClassName, CSClass otherClass, string skipReason, PlatformName platform,
					  bool enforceUTF8Encoding = false)
		{
			var use = GetTestClassUsings (nameSpace);
			
			// [TomSkip(skipReason)]
			// public class TomTesttestName : ITomTest
			// {
			//    public testClassName() { }
			//    public string TestName { get { return testName; } }
			//    public string ExpectedOutput { get { return expectedOuput; } }
			//    public void Run() {
			//       callingCode;
			//    }
			// }
			// otherClass

			CSNamespace ns = new CSNamespace (nameSpace);
			if (otherClass != null)
				ns.Block.Add (otherClass);

			CSCodeBlock body = new CSCodeBlock (callingCode);
			if (enforceUTF8Encoding) {
				InsertUTF8Assertion (body);
			}
			body.AddRange (callingCode);
			body.Add (CaptureSwiftOutputPostlude (testName));

			CSMethod run = new CSMethod (CSVisibility.Public, CSMethodKind.None, CSSimpleType.Void, new CSIdentifier ("Run"),
						new CSParameterList (), body);
			CSClass testClass = new CSClass (CSVisibility.Public, new CSIdentifier ($"TomTest{testName}"), new CSMethod [] { run });
			testClass.Inheritance.Add (new CSIdentifier ("ITomTest"));
			testClass.Properties.Add (MakeGetOnlyStringProp ("TestName", testName));
			testClass.Properties.Add (MakeGetOnlyStringProp ("ExpectedOutput", expectedOutput));
			ns.Block.Add (testClass);
			if (skipReason != null) {
				CSArgumentList al = new CSArgumentList ();
				al.Add (CSConstant.Val (skipReason));
				CSAttribute attr = new CSAttribute ("TomSkip", al);
				attr.AttachBefore (testClass);
			}
			return new Tuple<CSNamespace, CSUsingPackages> (ns, use);
		}

		static void InsertUTF8Assertion (CSCodeBlock body)
		{
			body.Insert (0, CSAssignment.Assign ("System.Console.OutputEncoding",
				new CSFunctionCall ("System.Text.UTF8Encoding", true, CSConstant.Val (false))));
		}

		static CSProperty MakeGetOnlyStringProp (string name, string val)
		{
			CSCodeBlock body = new CSCodeBlock ();
			body.Add (CSReturn.ReturnLine (CSConstant.Val (val)));

			return new CSProperty (CSSimpleType.String, CSMethodKind.None, new CSIdentifier (name), CSVisibility.Public,
					    body, CSVisibility.Public, null);
		}

		static CodeElementCollection<ICodeElement> CaptureSwiftOutputPostlude (string fileName)
		{


			//#if _MAC_TS_TEST_
			//            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
			//#else
			//NSUrl[] urls = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User);
			//string path = Path.Combine(urls[0].Path, fileName);
			//#endif

			//NSUrl[] urls = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User);
			//string path = Path.Combine(urls[0].Path, fileName);
			//if (File.Exists(path))
			//{
			//    Console.Write(File.ReadAllText(path));
			//}

			CSCodeBlock block = new CSCodeBlock ();
			CSIdentifier pathID = new CSIdentifier ("path");

			block.Add (new CSIdentifier ("\n#if _MAC_TS_TEST_\n"));
			block.Add (CSVariableDeclaration.VarLine (CSSimpleType.String, pathID,
							      new CSFunctionCall ("Path.Combine", false,
									       new CSInject ("Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)") +
									       CSConstant.Val ("/Documents/"),
									       CSConstant.Val (fileName))));
			block.Add (new CSIdentifier ("\n#else\n"));
			CSIdentifier urlID = new CSIdentifier ("urls");
			CSLine urlsLine = CSVariableDeclaration.VarLine (new CSSimpleType ("NSUrl", true), urlID,
								    new CSFunctionCall ("NSFileManager.DefaultManager.GetUrls", false,
										     new CSIdentifier ("NSSearchPathDirectory.DocumentDirectory"),
										     new CSIdentifier ("NSSearchPathDomain.User")));
			block.Add (urlsLine);


			CSLine pathLine = CSVariableDeclaration.VarLine (CSSimpleType.String, pathID,
								    new CSFunctionCall ("Path.Combine", false,
										     new CSArray1D (urlID.Name, CSConstant.Val (0)).Dot (new CSIdentifier ("Path")),
										     CSConstant.Val (fileName)));
			block.Add (pathLine);
			block.Add (new CSIdentifier ("\n#endif\n"));

			CSCodeBlock ifBlock = new CSCodeBlock ();
			CSLine writer = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSFunctionCall ("File.ReadAllText", false, pathID));
			ifBlock.Add (writer);
			ifBlock.Add (CSFunctionCall.FunctionCallLine ("File.Delete", false, pathID));
			CSIfElse iftest = new CSIfElse (new CSFunctionCall ("File.Exists", false, pathID), ifBlock);
			block.Add (iftest);
			return block;
		}


		public static string kSwiftFileWriter =
		    "import Foundation\n" +
		    "fileprivate func writeToFile(_ s: String, _ file: String) {\n" +
		    "    if let dir = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {\n" +
		    "        let fileURL = dir.appendingPathComponent(file)\n" +
		    "        try! s.write(to: fileURL, atomically: false, encoding: .utf8)\n" +
		    "}\n}\n";
	}

	public class TestRunning {

		static void SetInvokingTestNameIfUnset (ref string callingMethodName, out string callingMethodClass)
		{
			var stackTrace = new System.Diagnostics.StackTrace ();
			var callingMethod = stackTrace.GetFrame (2).GetMethod ();

			if (string.IsNullOrEmpty (callingMethodName)) {
				if (!callingMethod.CustomAttributes.Any (x => x.AttributeType.Name == "TestAttribute"))
					Assert.Fail ("TestRunning expect invocations without an explicit `testName` parameter to be invoked from the [Test] method directly. Consider passing an explicit `testName`.");

				callingMethodName = callingMethod.Name;
			}
			callingMethodClass = callingMethod.DeclaringType.Name;
		}


		public static void TestAndExecuteNoDevice (string swiftCode, CodeElementCollection<ICodeElement> callingCode,
		                                           string expectedOutput, string testName = null,
		                                           PlatformName platform = PlatformName.None,
							   UnicodeMapper unicodeMapper = null)
		{
			SetInvokingTestNameIfUnset (ref testName, out string nameSpace);

			using (var provider = new DisposableTempDirectory ()) {
				var compiler = Utils.SystemCompileSwift (swiftCode, provider, nameSpace);

				var libName = $"lib{nameSpace}.dylib";
				var tempDirectoryPath = Path.Combine (provider.DirectoryPath, "BuildDir");
				Directory.CreateDirectory (tempDirectoryPath);
				File.Copy (Path.Combine (compiler.DirectoryPath, libName), Path.Combine (tempDirectoryPath, libName));

				Utils.CompileToCSharp (provider, tempDirectoryPath, nameSpace, unicodeMapper: unicodeMapper);

				CSFile csFile = TestRunningCodeGenerator.GenerateTestEntry (callingCode, testName, nameSpace, platform);
				csFile.Namespaces.Add (CreateManagedConsoleRedirect ());
				CodeWriter.WriteToFile (Path.Combine (tempDirectoryPath, "NameNotImportant.cs"), csFile);

				var sourceFiles = Directory.GetFiles (tempDirectoryPath, "*.cs");
				Compiler.CSCompile (tempDirectoryPath, sourceFiles, "NameNotImportant.exe", platform: platform);

				CopyTestReferencesTo (tempDirectoryPath, platform);

				var output = Execute (tempDirectoryPath, "NameNotImportant.exe", platform);
				Assert.AreEqual (expectedOutput, output);
			}			
		}

		public static void TestAndExecute (string swiftCode, CodeElementCollection<ICodeElement> callingCode,
					    string expectedOutput,
					    string testName = null,
					    CSClass otherClass = null, string skipReason = null,
						 string iosExpectedOutput = null,
					    PlatformName platform = PlatformName.None,
					    UnicodeMapper unicodeMapper = null, int expectedErrorCount = 0,
					    Action<string> postCompileCheck = null,
					    string[] expectedOutputContains = null,
					    bool enforceUTF8Encoding = false)
		{
			SetInvokingTestNameIfUnset (ref testName, out string nameSpace);
			string testClassName = "TomTest" + testName;

			using (var provider = new DisposableTempDirectory ()) {
				var compiler = Utils.SystemCompileSwift (swiftCode, provider, nameSpace);

				var libName = $"lib{nameSpace}.dylib";
				var tempDirectoryPath = Path.Combine (provider.DirectoryPath, "BuildDir");
				Directory.CreateDirectory (tempDirectoryPath);
				File.Copy (Path.Combine (compiler.DirectoryPath, libName), Path.Combine (tempDirectoryPath, libName));

				Utils.CompileToCSharp (provider, tempDirectoryPath, nameSpace, unicodeMapper: unicodeMapper, expectedErrorCount: expectedErrorCount);
				if (postCompileCheck != null)
					postCompileCheck (tempDirectoryPath);

				Tuple<CSNamespace, CSUsingPackages> testClassParts = TestRunningCodeGenerator.CreateTestClass (callingCode, testName, iosExpectedOutput ?? expectedOutput, nameSpace,
										       testClassName, otherClass, skipReason, platform, enforceUTF8Encoding);

				var thisTestPath = Path.Combine (Compiler.kSwiftDeviceTestRoot, nameSpace);
				Directory.CreateDirectory (thisTestPath);

				var thisTestPathSwift = Path.Combine (thisTestPath, "swiftsrc");
				Directory.CreateDirectory (thisTestPathSwift);

				var swiftPrefix = string.Empty;
				var swiftSuffix = string.Empty;
				var csPrefix = string.Empty;
				var csSuffix = string.Empty;
				var nameSuffix = string.Empty;
				switch (platform) {
				case PlatformName.macOS:
					swiftPrefix = "#if os(OSX)\n";
					swiftSuffix = "#endif\n";
					csPrefix = "#if __MACOS__\n";
					csSuffix = "\n#endif\n";
					nameSuffix = "_macOS";
					break;
				case PlatformName.iOS:
					swiftPrefix = "#if os(iOS)\n";
					swiftSuffix = "#endif\n";
					csPrefix = "#if __IOS__\n";
					csSuffix = "\n#endif\n";
					nameSuffix = "_iOS";
					break;
				case PlatformName.tvOS:
					swiftPrefix = "#if os(tvOS)\n";
					swiftSuffix = "#endif\n";
					csPrefix = "#if __TVOS__\n";
					csSuffix = "\n#endif\n";
					nameSuffix = "_tvOS";
					break;
				case PlatformName.watchOS:
					swiftPrefix = "#if os(watchOS)\n";
					swiftSuffix = "#endif\n";
					csPrefix = "#if __WATCHOS__\n";
					csSuffix = "\n#endif\n";
					nameSuffix = "_watchOS";
					break;
				case PlatformName.None:
					break;
				default:
					throw new NotImplementedException (platform.ToString ());
				}

				File.WriteAllText (Path.Combine (thisTestPathSwift, $"{testClassName}{testName}{nameSuffix}.swift"), swiftPrefix + swiftCode + swiftSuffix);

				CSFile csTestFile = CSFile.Create (testClassParts.Item2, testClassParts.Item1);
				var csTestFilePath = Path.Combine (thisTestPath, $"{testClassName}{testName}{nameSuffix}.cs");
				// Write out the file without csPrefix/csSuffix
				CodeWriter.WriteToFile (csTestFilePath, csTestFile);
				if (!string.IsNullOrEmpty (csPrefix) || !string.IsNullOrEmpty (csSuffix)) {
					// Read the C# code, and prepend/append the csPrefix/csSuffix blobs, then save the modified contents again.
					File.WriteAllText (csTestFilePath, csPrefix + File.ReadAllText (csTestFilePath) + csSuffix);
				}

				var csFile = TestRunningCodeGenerator.GenerateTestEntry (callingCode, testName, nameSpace, platform, otherClass, enforceUTF8Encoding);
				csFile.Namespaces.Add (CreateManagedConsoleRedirect ());
				CodeWriter.WriteToFile (Path.Combine (tempDirectoryPath, "NameNotImportant.cs"), csFile);

				var sourceFiles = Directory.GetFiles (tempDirectoryPath, "*.cs");
				var objcRuntimePath = Path.Combine (tempDirectoryPath, "ObjCRuntime");
				if (Directory.Exists (objcRuntimePath))
					sourceFiles = sourceFiles.And (Directory.GetFiles (objcRuntimePath, "*.cs"));

				var compilerWarnings = Compiler.CSCompile (tempDirectoryPath, sourceFiles, "NameNotImportant.exe", platform: platform);

				if (compilerWarnings.Contains ("warning"))
					FailOnBadWarnings (compilerWarnings);

				CopyTestReferencesTo (tempDirectoryPath, platform);

				var output = Execute (tempDirectoryPath, "NameNotImportant.exe", platform);
				if (expectedOutput != null)
					Assert.AreEqual (expectedOutput, output);
				else {
					foreach (var s in expectedOutputContains) {
						Assert.IsTrue (output.Contains (s), $"Expected to find string {s} in {output}");
					}
				}
			}
		}

		public static string CreateSwiftConsoleRedirect ()
		{
			return @"
import Foundation
import Darwin

func print(_ value:String) {
	let line = value + ""\n""
	let path = ProcessInfo.processInfo.environment[""LEAKTEST_STDOUT_PATH""];
	if path == nil {
		fputs(line, stdout)
	} else {
	   let fileUpdater = FileHandle(forUpdatingAtPath: path!)!
	   fileUpdater.seekToEndOfFile()
	   fileUpdater.write(line.data(using: .utf8)!)
	   fileUpdater.closeFile()
	}
}
	";
		}

		// Get managed code that replaces System.Console, and instead writes
		// the output to LEAKTEST_STDOUT_PATH instead (if set, otherwise works
		// just like System.Console).
		public static string GetManagedConsoleRedirectCode ()
		{
			return @"
#if !DEVICETESTS
public static class Console {
	static string filename;
	static string Filename {
		get {
			if (filename == null)
				filename = Environment.GetEnvironmentVariable (""LEAKTEST_STDOUT_PATH"") ?? string.Empty;
			return filename;
		}
	}

	static void write (string value)
	{
		if (string.IsNullOrEmpty (Filename)) {
			global::System.Console.Write (value);
		} else {
			System.IO.File.AppendAllText (Filename, value);
		}
	}

	public static void Write (object value)
	{
		write (value?.ToString ());
	}
	public static void Write (string value, params object[] args)
	{
		write (value == null ? string.Empty : string.Format (value, args));
	}
	public static void WriteLine (object value)
	{
		write (value?.ToString () + Environment.NewLine);
	}
	public static void WriteLine (string value, params object[] args)
	{
		write ((value == null ? string.Empty : string.Format (value, args)) + Environment.NewLine);
	}
}
#endif
";
		}

		public static CSNamespace CreateManagedConsoleRedirect ()
		{
			// Same as GetManagedConsoleRedirectCode, just different format.
			var cs = new CSNamespace ();

			var console = new CSClass (CSVisibility.Public, "Console", isStatic: true);
			console.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.String, new CSIdentifier ("filename"), isStatic: true));
			console.Properties.Add (
				new CSProperty (
					CSSimpleType.String,
					CSMethodKind.Static,
					new CSIdentifier ("Filename"),
					CSVisibility.None,
					CSCodeBlock.Create (
						new CSIfElse (
							new CSBinaryExpression (
								CSBinaryOperator.Equal,
								new CSIdentifier ("filename"),
								new CSIdentifier ("null")
							),
							CSCodeBlock.Create (
								CSAssignment.Assign (
									new CSIdentifier ("filename"),
										new CSBinaryExpression (
											CSBinaryOperator.NullCoalesce,
											CSFunctionCall.Function (
												"Environment.GetEnvironmentVariable",
												CSConstant.Val ("LEAKTEST_STDOUT_PATH")
											),
											new CSIdentifier ("string.Empty")
										)
									)
								)
							),
						CSReturn.ReturnLine (new CSIdentifier ("filename"))
					),
					CSVisibility.None,
					null
				)
			);
			console.Methods.Add (
				new CSMethod (
					CSVisibility.Public,
					CSMethodKind.Static,
					CSSimpleType.Void,
					new CSIdentifier ("write"),
					new CSParameterList (
						new CSParameter (CSSimpleType.String, new CSIdentifier ("value"))
					),
					CSCodeBlock.Create (
						new CSIfElse (
							new CSInject ("string.IsNullOrEmpty (Filename)"),
							CSCodeBlock.Create (CSFunctionCall.FunctionCallLine ("global::System.Console.Write", new CSIdentifier ("value"))),
							CSCodeBlock.Create (CSFunctionCall.FunctionCallLine ("System.IO.File.AppendAllText", new CSIdentifier ("Filename"), new CSIdentifier ("value")))
						)
					)
				)
			);
			console.Methods.Add (
				new CSMethod (
					CSVisibility.Public,
					CSMethodKind.Static,
					CSSimpleType.Void,
					new CSIdentifier ("Write"),
					new CSParameterList (
						new CSParameter (CSSimpleType.Object, new CSIdentifier ("value"))
					),
					CSCodeBlock.Create (
						CSFunctionCall.FunctionCallLine (
							"write",
							false,
							new CSInject ("value?.ToString ()")
						)
					)
				)
			);
			console.Methods.Add (
				new CSMethod (
					CSVisibility.Public,
					CSMethodKind.Static,
					CSSimpleType.Void,
					new CSIdentifier ("Write"),
					new CSParameterList (
						new CSParameter (CSSimpleType.String, new CSIdentifier ("value")),
						new CSParameter (CSSimpleType.CreateArray ("object"), new CSIdentifier ("args"), CSParameterKind.Params)
					),
					CSCodeBlock.Create (
						CSFunctionCall.FunctionCallLine (
							"write",
							false,
							new CSInject ("value == null ? string.Empty : string.Format (value, args)")
						)
					)
				)
			);
			console.Methods.Add (
				new CSMethod (
					CSVisibility.Public,
					CSMethodKind.Static,
					CSSimpleType.Void,
					new CSIdentifier ("WriteLine"),
					new CSParameterList (
						new CSParameter (CSSimpleType.Object, new CSIdentifier ("value"))
					),
					CSCodeBlock.Create (
						CSFunctionCall.FunctionCallLine (
							"write",
							false,
							new CSInject ("value?.ToString () + Environment.NewLine")
						)
					)
				)
			);
			console.Methods.Add (
				new CSMethod (
					CSVisibility.Public,
					CSMethodKind.Static,
					CSSimpleType.Void,
					new CSIdentifier ("WriteLine"),
					new CSParameterList (
						new CSParameter (CSSimpleType.String, new CSIdentifier ("value")),
						new CSParameter (CSSimpleType.CreateArray ("object"), new CSIdentifier ("args"), CSParameterKind.Params)
					),
					CSCodeBlock.Create (
						CSFunctionCall.FunctionCallLine (
							"write",
							false,
							new CSInject ("(value == null ? string.Empty : string.Format (value, args)) + Environment.NewLine")
						)
					)
				)
			);
			cs.Block.Add (console);
			return cs;
		}

		public static string Execute (string workingDirectory, string executable, PlatformName platform)
		{
			switch (platform) {
			case PlatformName.macOS: {
					// for macOS we create an app bundle with mmp
					var name = Path.GetFileNameWithoutExtension (executable);
					var mmp = new StringBuilder ();

					mmp.AppendLine ($"/cache:{StringUtils.Quote (Path.Combine (workingDirectory, "mmp-cache"))}");
					mmp.AppendLine ($"/root-assembly:{StringUtils.Quote (Path.Combine (workingDirectory, executable))}");
					mmp.AppendLine ($"/sdkroot:/Applications/Xcode.app");
					mmp.AppendLine ($"/profile:Xamarin.Mac,v2.0,Profile=Mobile");
					mmp.AppendLine ($"/arch:x86_64");
					mmp.AppendLine ($"/assembly:{StringUtils.Quote (Path.Combine (ConstructorTests.kXamarinMacDir, "Xamarin.Mac.dll"))}");
					mmp.AppendLine ($"/output:{StringUtils.Quote (Path.Combine (workingDirectory, name))}");
					mmp.AppendLine ($"/assembly:{StringUtils.Quote (Path.Combine (ConstructorTests.kSwiftRuntimeMacOutputDirectory, ConstructorTests.kSwiftRuntimeLibraryMac))}.dll");
					mmp.AppendLine ($"/debug");
					mmp.AppendLine ($"/linksdkonly"); // FIXME: link all doesn't work for all tests, this needs looking into.
					mmp.AppendLine ($"/native-reference:{StringUtils.Quote (Compiler.kSwiftRuntimeGlueDirectory)}"); // link with XamGlue.framework
					foreach (var dylib in Directory.GetFiles (workingDirectory, "*.dylib")) // Link with any dylibs produced by the test 
						mmp.AppendLine ($"/native-reference:{StringUtils.Quote (dylib)}");
					mmp.AppendLine ($"/v /v /v /v");

					var output = new StringBuilder ();
					var responseFile = Path.Combine (workingDirectory, name + ".rsp");
					File.WriteAllText (responseFile, mmp.ToString ());
					var rv = ExecAndCollect.RunCommand ("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/bin/mmp", "--link_flags=-headerpad_max_install_names " + StringUtils.Quote ($"@{responseFile}"), output: output, verbose: true);
					if (rv != 0) {
						Console.WriteLine (output);
						throw new Exception ($"Failed to run mmp, exit code: {rv}");
					}

					// This should probably go into mmp/mtouch
					// run swift-stdlib-tool to get swift libraries into the app.
					var appPath = Path.Combine (workingDirectory, name, name + ".app");
					var appExecutable = Path.Combine (appPath, "Contents", "MacOS", name);
					var swift_stdlib_tool = new StringBuilder ();
					swift_stdlib_tool.Append ($"swift-stdlib-tool ");
					swift_stdlib_tool.Append ($"--copy ");
					swift_stdlib_tool.Append ($"--verbose ");
					swift_stdlib_tool.Append ($"--scan-executable {StringUtils.Quote (appExecutable)} ");
					swift_stdlib_tool.Append ($"--platform macosx ");
					swift_stdlib_tool.Append ($"--destination {StringUtils.Quote (Path.Combine (appPath, "Contents", "Frameworks"))} ");
					swift_stdlib_tool.Append ($"--strip-bitcode ");
					swift_stdlib_tool.Append ($"--scan-folder {StringUtils.Quote (Path.Combine (appPath, "Contents", "MonoBundle"))} ");
					swift_stdlib_tool.Append ($"--scan-folder {StringUtils.Quote (Path.Combine (appPath, "Contents", "Frameworks"))} ");
					swift_stdlib_tool.Append ($"--platform macosx ");
					swift_stdlib_tool.Append ($"--source-libraries {StringUtils.Quote (Compiler.CompilerLocation.SwiftCompilerLib)} ");
					output.Clear ();
					rv = ExecAndCollect.RunCommand ("xcrun", swift_stdlib_tool.ToString (), output: output, verbose: true);
					if (rv != 0) {
						Console.WriteLine (output);
						throw new Exception ($"Failed to run swift-stdlib-tool, exit code: {rv}\n{output}\n");
					}

					// This should probably go into mmp/mtouch
					// make sure the executable has the Frameworks and MonoBundle directories as rpaths.
					var install_name_tool = new StringBuilder ();
					install_name_tool.Append ($"install_name_tool ");
					install_name_tool.Append ($"-add_rpath @executable_path/../Frameworks ");
					install_name_tool.Append ($"-add_rpath @executable_path/../MonoBundle ");
					install_name_tool.Append ($"{StringUtils.Quote (Path.Combine (appPath, "Contents", "MacOS", name))} ");
					output.Clear ();
					rv = ExecAndCollect.RunCommand ("xcrun", install_name_tool.ToString (), output: output, verbose: true);
					if (rv != 0) {
						Console.WriteLine (output);
						throw new Exception ($"Failed to run install_name_tool, exit code: {rv}\n{output}\n");
					}

					var exec_output = new StringBuilder ();
					var exec_env = new Dictionary<string, string> ();
					//exec_env.Add ("MONO_LOG_LEVEL", "debug");
					//exec_env.Add ("MONO_LOG_MASK", "dll");
					var exec_rv = Compiler.RunCommandWithLeaks (appExecutable, new StringBuilder (), exec_env, exec_output);
					if (exec_rv != 0) {
						Console.WriteLine (exec_output);
						throw new Exception ($"Execution failed with exit code {exec_rv}\n{output}\n{exec_output}");
					}
					return exec_output.ToString ();
				}
			case PlatformName.iOS:
				Assert.Ignore ($"Execution does not apply during a test run for {platform}, tests will be executed as part of the device tests.");
				return string.Empty;
			case PlatformName.None: {
					return Compiler.RunWithMono (executable, workingDirectory, platform: platform);
				}
			default:
				throw new NotImplementedException (platform.ToString ());
			}
		}

		static string [] testMacRuntimeAssemblies = {
			Path.Combine (ConstructorTests.kXamarinMacDir, "Xamarin.Mac.dll"),
		};
		static string [] testiOSRuntimeAssemblies = {
			Path.Combine (ConstructorTests.kXamariniOSDir, "Xamarin.iOS.dll"),
		};

		public static void CopyTestReferencesTo (string targetDirectory, PlatformName platform = PlatformName.None)
		{
			IEnumerable<string> references = null;

			switch (platform) {
			case PlatformName.macOS:
				references = testMacRuntimeAssemblies;
				break;
			case PlatformName.iOS:
				references = testiOSRuntimeAssemblies;
				break;
			case PlatformName.None:
				return;
			default:
				throw new NotImplementedException (platform.ToString ());
			}

			foreach (var path in references) {
				if (!File.Exists (path))
					throw new ArgumentException ($"Unable to find required assembly {path}.");
				File.Copy (path, Path.Combine (targetDirectory, Path.GetFileName (path)));
			}
		}

		public static void FailOnBadWarnings (string warnings)
		{
			var regex = new Regex ("warning CS\\d\\d\\d\\d:");
			var matches = regex.Matches (warnings);
			if (matches.Count == 0)
				return;
			var capturedMatches = new List<string> ();
			for (int i = 0; i < matches.Count; i++) {
				capturedMatches.Add (matches [i].Value);
			}
			capturedMatches = capturedMatches.Distinct ().ToList ();
			// CS2019 is a warning about an unused local. We have many tests
			// that generate them. Removing them all is not a small task at this
			// point.
			capturedMatches.Remove ("warning CS0219:");
			if (capturedMatches.Count > 0)
				Assert.Fail ($"Unexpected C# compiler warning(s): {warnings}");
		}
	}
}
