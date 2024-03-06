// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.IO;
using System.Diagnostics;
using Dynamo;
using SwiftReflector.Demangling;
using SwiftReflector;
using SwiftReflector.IOUtils;
using System.Text;
using System.Collections.Specialized;
using System.Collections.Generic;
using Xamarin.Utils;
using NUnit.Framework.Legacy;
using System.Linq;
using System.Xml.Linq;

namespace tomwiftytest {

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
	public sealed class RunWithLeaksAttribute : Attribute { }

	public enum XCodeCompiler {
		C = 0, Cpp, ObjectiveC, Swiftc, CSharp, CSharpExe
	}

	[TestFixture ()]
	[Parallelizable (ParallelScope.All)]
	public class Compiler {
		// Enviroment var that can be used to test binding-tools-for-swift from a package
		static string SOM_PATH = Environment.GetEnvironmentVariable ("SOM_PATH");
		public static string kframeWorkVersion = "7.0";
		public static string kswiftVersion = "5.5";
#if DEBUG
		public const string kSwiftRuntimeGlueDirectoryRel = "../../../../../swiftglue/bin/Debug/mac/FinalProduct/XamGlue.framework";
		public const string kSwiftRuntimeSourceDirectoryRel = "../../../../../swiftglue/";
#endif
		public static string kSwiftDeviceTestRoot = PosixHelpers.RealPath (Path.Combine (GetTestDirectory (), "../../devicetests"));
		public static string kLeakCheckBinary = PosixHelpers.RealPath (Path.Combine (GetTestDirectory (), "..", "..", "..", "..", "leaktest", "bin", "Debug", "leaktest"));
		public static string kSwiftRuntimeGlueDirectory = PosixHelpers.RealPath (SOM_PATH is null ? Path.Combine (GetTestDirectory (), kSwiftRuntimeGlueDirectoryRel) : FindPathFromEnvVariable ("lib/SwiftInterop/mac/XamGlue.framework"));
		public static string kXamGlueSourceDirectory = PosixHelpers.RealPath (SOM_PATH ?? Path.Combine (GetTestDirectory (), kSwiftRuntimeSourceDirectoryRel));

		static string kSystemBin = "/usr/bin/";
		public const string kSystemLib = "/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0/macosx";
		public static string kSwiftRuntimeOutputDirectory = Path.Combine (TestContext.CurrentContext.TestDirectory, $"../../../../../SwiftRuntimeLibrary/bin/Debug/net{Compiler.kframeWorkVersion}");
		public const string kSwiftRuntimeLibrary = "SwiftRuntimeLibrary";

		[ThreadStatic]
		static SwiftCompilerLocation systemCompilerLocation;
		public static SwiftCompilerLocation SystemCompilerLocation {
			get {
				if (systemCompilerLocation == null)
					systemCompilerLocation = new SwiftCompilerLocation (kSystemBin, kSystemLib);
				return systemCompilerLocation;
			}
		}


		public static List<string> kTypeDatabases = new List<string> { PosixHelpers.RealPath (SOM_PATH is null ? Path.Combine (GetTestDirectory (), "../../../../../bindings") : FindPathFromEnvVariable ("bindings")) };
		public const string kMono64Path = "/Library/Frameworks/Mono.framework/Versions/Current/bin/mono";
		public static string kTestRoot = PosixHelpers.RealPath (GetTestDirectory ());
#if DEBUG
		public static string kTomSwiftyPath = PosixHelpers.RealPath (SOM_PATH is null ? Path.Combine (GetTestDirectory (), "../../../../../tom-swifty/bin/Debug/tom-swifty.exe") : FindPathFromEnvVariable ("lib/binding-tools-for-swift/tom-swifty.exe"));
#else
		public static string kTomSwiftyPath = PosixHelpers.RealPath (SOM_PATH is null ? Path.Combine (GetTestDirectory (), "../../../../tom-swifty/bin/Release/tom-swifty.exe") : FindPathFromEnvVariable ("lib/binding-tools-for-swift/tom-swifty.exe"));
#endif
		public static string kMacProjPath = PosixHelpers.RealPath (Path.Combine (GetTestDirectory (), "../../../../../SwiftRuntimeLibrary.Mac/SwiftRuntimeLibrary.Mac.csproj"));
		public static string kiOSProjPath = PosixHelpers.RealPath (Path.Combine (GetTestDirectory (), "../../../../../SwiftRuntimeLibrary.iOS/SwiftRuntimeLibrary.iOS.csproj"));
		static string [] compilers = new string [] {
			"clang -x c", "clang -x c++", "clang -x objective-c", "swiftc", "mcs", "mcs", "swiftc", "swift"
		};

		static string FindPathFromEnvVariable (string pathSuffix)
		{
			if (SOM_PATH != null) {
				var fullPath = Path.Combine (SOM_PATH.Replace ("\n", ""), pathSuffix);
				if (Directory.Exists (fullPath) || File.Exists (fullPath))
					return fullPath;
			}
			return null;
		}

		static string GetCompilerName (XCodeCompiler compiler)
		{
			int icomp = (int)compiler;
			if (icomp < 0 || icomp >= compilers.Length) throw new ArgumentOutOfRangeException ("compiler");
			return compilers [icomp];
		}

		static string GetTestDirectory ()
		{
			return TestContext.CurrentContext.TestDirectory;
		}

		static string sdkPath;
		static string GetSDKPath ()
		{
			if (sdkPath == null)
				sdkPath = ExecAndCollect.Run ("/usr/bin/xcrun", "--show-sdk-path --sdk macosx");
			return sdkPath.Trim ();
		}

		static string BuildArgs (XCodeCompiler compiler, string compilerName, string outfile, string pathToCode, string extraOptions)
		{
			if (extraOptions == null)
				throw new ArgumentNullException ("extraOptions");

			if (compiler == XCodeCompiler.Swiftc) {
				// check to see if the user wants to assert a particular module name.
				// this is clearly not a 100% solution since if some other option has to
				// contain the string '-module-name', then this is a false positive.
				string moduleOption = extraOptions.Contains ("-module-name") ? "" : "-module-name noname";
				return String.Format ("{0} -emit-library  -sdk {5} {4} {1} -o {2} {3}", compilerName, extraOptions, outfile,
					pathToCode, moduleOption, GetSDKPath ());
			} else if (compiler == XCodeCompiler.CSharp) {
				return String.Format ("{0} -target:library -out:{1} {2}", extraOptions, outfile, pathToCode);
			} else if (compiler == XCodeCompiler.CSharpExe) {
				return String.Format ("{0} -out:{1} {2}", extraOptions, outfile, pathToCode);
			} else {
				return String.Format ("{0} {1} -c -o {2} {3}", compilerName, extraOptions, outfile, pathToCode);
			}
		}

		public static Stream CompileUsing (string xcrun, XCodeCompiler compiler, string pathToCode, string pathToOutput, string extraOptions, string workingDirectory = null)
		{
			if (pathToCode == null)
				throw new ArgumentNullException ("pathToCode");
			//			if (!File.Exists (pathToCode))
			//				throw new FileNotFoundException ("Unable to locate source file.", pathToCode);
			if (pathToOutput == null)
				throw new ArgumentNullException ("pathToOutput");

			// sanitize inputs
			xcrun = xcrun ?? "/usr/bin/xcrun";
			if (compiler == XCodeCompiler.CSharp || compiler == XCodeCompiler.CSharpExe)
				xcrun = "/Library/Frameworks/Mono.framework/Versions/Current/bin/mcs";
			extraOptions = extraOptions ?? "";

			// get the xcrun subcommand
			string compilerName = GetCompilerName (compiler);

			string args = BuildArgs (compiler, compilerName, pathToOutput, pathToCode, extraOptions);

			ExecAndCollect.Run (xcrun, args);

			MemoryStream ms = new MemoryStream ();

			try {

				using (FileStream outstm = new FileStream (pathToOutput, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					outstm.CopyTo (ms);
				}
			} catch (Exception e) {
				Console.Write (e.Message);
			}
			ms.Seek ();
			return ms;
		}

		public static string CSCompile (string workingDirectory, string [] sourceFiles, string outputFile, string extraOptions = "", PlatformName platform = PlatformName.None)
		{
			if (platform != PlatformName.None) {
				return PlatformBuild (workingDirectory, outputFile, platform);
			} else {
				CreateRuntimeConfigJson (workingDirectory, outputFile);
				var compilerArgs = BuildCSCompileArgs (sourceFiles, outputFile, extraOptions, platform);
				// ShowAllFiles (sourceFiles);
				return ExecAndCollect.Run ("/usr/local/share/dotnet/dotnet", $"exec {GetDotnetCSCompiler ()} {compilerArgs}", workingDirectory: workingDirectory ?? "");
			}
		}

		static string PlatformBuild (string workingDirectory, string outputfile, PlatformName platform)
		{
			// what happens here:
			// make a new dotnet project for the specified platform (dotnet new ...)
			// add a reference to the platform csproj to get runtime
			// build the resulting project

			var sb = new StringBuilder ();
			var name = Path.GetFileNameWithoutExtension (outputfile);
			var projectDirectory = Path.Combine (workingDirectory, name);
			var dotnetPlatform = PlatformNameToDotnetPlatform (platform);
			var result = ExecAndCollect.Run ("/usr/local/share/dotnet/dotnet", $"new {dotnetPlatform} --name {name}", workingDirectory);
			sb.Append (result);
			RemoveExistingCSFiles (projectDirectory);
			CopySourceCSFiles (workingDirectory, projectDirectory);
			var projectReference = PlatformNameToPlatformProject (platform);
			if (!File.Exists (projectReference)) {
				throw new Exception ("Unable to find project: " + projectReference);
			}

			// copy xamglue, project libraries and swiftlibraries to somewhere useful
			// Maybe MonoBundle or make a Frameworks directory as well.

			result = ExecAndCollect.Run ("/usr/local/share/dotnet/dotnet", $"add reference {projectReference}", projectDirectory);
			sb.Append (result);
			AddXamGlueReference (projectDirectory, name + ".csproj", platform);
			result = ExecAndCollect.Run ("/usr/local/share/dotnet/dotnet", "build --property:LinkMode=SdkOnly --property:AllowUnsafeBlocks=true", projectDirectory);
			sb.Append (result);
			result = sb.ToString ();

			Console.WriteLine ("build output");
			Console.WriteLine (result);

			return result;
		}

		static void AddXamGlueReference (string projectDirectory, string csprojName, PlatformName platform)
		{
			var xamglueFrameworkPath = GetXamGluePath (platform);
			if (String.IsNullOrEmpty (xamglueFrameworkPath))
				return;
			if (!Directory.Exists (xamglueFrameworkPath))
				throw new Exception ($"Trying to add reference to xamglue but failed to find directory {xamglueFrameworkPath}");
			var proj = Path.Combine (projectDirectory, csprojName);
			var xdoc = XDocument.Load (proj);
			var project = xdoc.Element ("Project");
			if (project is null)
				throw new Exception ($"Unable to find Project node in {proj}");
			project.Add (
				new XElement ("ItemGroup",
					new XElement ("NativeReference",
						new XAttribute ("Include", xamglueFrameworkPath),
						new XAttribute ("Kind", "Framework"))));
			xdoc.Save (proj);
		}

		static string GetXamGluePath (PlatformName platform)
		{
			var platformDirectoryPart = "";
			var framework = "XamGlue.xcframework";
			switch (platform) {
			case PlatformName.macOS:
				return "";
			case PlatformName.iOS:
				platformDirectoryPart = "iphone";
				break;
			case PlatformName.tvOS:
				platformDirectoryPart = "appletv";
				break;
			case PlatformName.watchOS:
				platformDirectoryPart = "watch";
				break;
			default:
				throw new NotImplementedException ($"unknown platform getting xamglue path: {platform}");
			}

			var dir = Path.Combine (kXamGlueSourceDirectory, "bin", "Debug", platformDirectoryPart, "FinalProduct", framework);
			return dir;
		}

		static void RemoveExistingCSFiles (string directory)
		{
			var files = Directory.GetFiles (directory, "*.cs");
			foreach (var file in files) {
				File.Delete (file);
			}
		}

		static void CopySourceCSFiles (string source, string dest)
		{
			var files = Directory.GetFiles (source, "*.cs");
			foreach (var file in files) {
				File.Copy (file, Path.Combine (dest, Path.GetFileName (file)));
			}
		}

		static string PlatformNameToPlatformProject (PlatformName platform)
		{
			switch (platform)
			{
			case PlatformName.macOS:
				return kMacProjPath;
			case PlatformName.iOS:
				return kiOSProjPath;
			default:
				throw new ArgumentOutOfRangeException (nameof (platform), $"unknown platform {platform}");
			}
		}

		static string PlatformNameToDotnetPlatform (PlatformName platform)
		{
			switch (platform) {
			case PlatformName.macOS:
				return "macos";
			case PlatformName.iOS:
				return "ios";
			case PlatformName.tvOS:
				return "tvos";
			case PlatformName.watchOS:
				return "watchos";
			default:
				throw new ArgumentOutOfRangeException (nameof (platform), $"unknown platform {platform}");
			}
		}

		static void CreateRuntimeConfigJson (string workingDirectory, string outputFile)
		{
			var fileName = Path.ChangeExtension (outputFile, ".runtimeconfig.json");
			var fullPath = Path.Combine (workingDirectory ?? "", fileName);
			using var file = new StreamWriter (fullPath, false);
			file.WriteLine ($"{{\n  \"runtimeOptions\": {{\n    \"tfm\": \"net{kframeWorkVersion}\",\n    \"framework\": {{\n      \"name\": \"Microsoft.NETCore.App\",\n      \"version\": \"{kframeWorkVersion}.0\"\n    }}\n  }}\n}}");
		}

		static void ShowAllFiles (string [] sourceFiles)
		{
			// useful for debugging
			foreach (var file in sourceFiles)
				ShowFile (file);
		}

		static void ShowFile (string file)
		{
			Console.WriteLine ($"-------- {file} --------");
			using var reader = new StreamReader (file);
			var text = reader.ReadToEnd ();
			Console.WriteLine (text);
		}

		static string BuildCSCompileArgs (string [] sourceFiles, string outputFile, string extraOptions, PlatformName platform = PlatformName.None)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("-unsafe ");
			sb.Append ($"-define:_MAC_TS_TEST_ ");
			sb.Append ("-nowarn:CS0169 ");
			sb.Append ("-out:").Append (outputFile).Append (' ').Append (extraOptions);
			foreach (string s in sourceFiles) {
				sb.Append (' ').Append (s);
			}
			sb.Append (' ');
			switch (platform) {
			case PlatformName.None:
				var referenceAssemblyDir = GetReferenceAssemblyLocation ();
				sb.Append ($"-lib:{kSwiftRuntimeOutputDirectory} ");
				sb.Append ($"-r:{kSwiftRuntimeLibrary}.dll ");
				sb.Append ($"-r:{Path.Combine (referenceAssemblyDir, "System.dll")} ");
				sb.Append ($"-r:{Path.Combine (referenceAssemblyDir, "System.Console.dll")} ");
				sb.Append ($"-r:{Path.Combine (referenceAssemblyDir, "System.Runtime.dll")} ");
				sb.Append ($"-r:{Path.Combine (referenceAssemblyDir, "System.Runtime.InteropServices.dll")} ");
				sb.Append ($"-r:{Path.Combine (referenceAssemblyDir, "mscorlib.dll")} ");
				sb.Append ($"-r:{Path.Combine (referenceAssemblyDir, "System.Text.Encoding.Extensions.dll")} ");
				break;
			case PlatformName.macOS:
			case PlatformName.iOS:
				throw new NotImplementedException ($"This code path is obsolete for {platform} - how did you get here?");
			default:
				throw new NotImplementedException (platform.ToString ());
			}
			return sb.ToString ();
		}

		// compile to a temporary file.
		public static Stream CompileUsing (string xcrun, XCodeCompiler compiler, string pathToCode, string extraOptions)
		{
			using (DisposableTempFile outputFile = new DisposableTempFile (null, null, "out", false)) {
				return CompileUsing (xcrun, compiler, pathToCode, outputFile.Filename, extraOptions);
			}
		}

		public static Stream CompileUsing (string xcrun, XCodeCompiler compiler, string pathToCode, string extraOptions, bool useSourceAsWorkingDirectory)
		{
			string workingDirectory = useSourceAsWorkingDirectory ?
				Path.GetDirectoryName (pathToCode) : null;
			using (DisposableTempFile outputFile = new DisposableTempFile (null, null, "out", false)) {
				return CompileUsing (xcrun, compiler, pathToCode, outputFile.Filename, extraOptions, workingDirectory);
			}
		}


		// compile an inline code string
		public static Stream CompileStringUsing (string xcrun, XCodeCompiler compiler, string code, string extraOptions)
		{
			using (DisposableTempFile tf = new DisposableTempFile (null, null, compiler == XCodeCompiler.Swiftc ? "swift" : null, false)) {
				File.WriteAllText (tf.Filename, code);
				return CompileUsing (xcrun, compiler, tf.Filename, extraOptions);
			}
		}

		public static DisposableTempFile CompileToFileUsing (string xcrun, XCodeCompiler compiler, string pathToCode, string extraOptions, DisposableTempFile outputFile = null)
		{
			outputFile = outputFile ?? new DisposableTempFile (null, null, "out", false);
			CompileUsing (xcrun, compiler, pathToCode, outputFile.Filename, extraOptions).Dispose (); // memory stream - make it go away
			return outputFile;
		}


		public static DisposableTempFile CompileStringToFileUsing (string xcrun, XCodeCompiler compiler, string code, string extraOptions, DisposableTempFile outputFile = null)
		{
			using (DisposableTempFile tf = new DisposableTempFile (null, null, compiler == XCodeCompiler.Swiftc ? "swift" : null, false)) {
				File.WriteAllText (tf.Filename, code);
				return CompileToFileUsing (xcrun, compiler, tf.Filename, extraOptions, outputFile);
			}
		}

		//		public static DisposableTempFile CompileStringToFileUsing(string xcrun, XCodeCompiler compiler, string code,
		//			string extraOptions, DisposableTempFile outputFile)
		//		{
		//			using (DisposableTempFile tf = new DisposableTempFile (null, null, compiler == XCodeCompiler.Swift ? "swift" : null, false)) {
		//				StreamWriter writer = new StreamWriter (tf.Filename);
		//				writer.Write (code);
		//				writer.Dispose ();
		//				return CompileToFileUsing (xcrun, compiler, tf.Filename, extraOptions, outputFile);
		//			}
		//		}


		public static string kHelloC = "#include <stdio.h>\nint main(int argc, char **argv){printf(\"hello, world\\n\"); return 0;}";
		public static string kHelloCpp = "#include <iostream>\nint main() { std::cout << \"hello, world\\n\"; return 0; }";
		public static string kHelloObjC = "#import <Foundation/Foundation.h>\nint main(int argc, const char * argv[]) { NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];NSLog (@\"Hello, World!\");[pool drain];return 0;}";
		public static string kHelloSwift = "public func main() -> String { return \"hello, world\"; }";
		public static string kHelloCSharp = "using System;\npublic class Hello { public static void Main(string[] args) { Console.WriteLine(\"hello, world\\n\"); } }";


		[Test]
		public void HelloWorldC ()
		{
			using (Stream ostm = CompileStringUsing (null, XCodeCompiler.C,
						    kHelloC,
						    null)) {
				ClassicAssert.IsNotNull (ostm);
				ClassicAssert.IsTrue (ostm.Length > 0);
			}
		}

		[Test]
		public void CSyntaxError ()
		{
			ClassicAssert.Throws<Exception> (() => {
				using (Stream ostm = CompileStringUsing (null, XCodeCompiler.C,
				    "thisisnotc",
				    null)) {
					ClassicAssert.IsNotNull (ostm);
					ClassicAssert.IsTrue (ostm.Length > 0);
				}
			});
		}

		[Test]
		public void HelloWorldCpp ()
		{
			using (Stream ostm = CompileStringUsing (null, XCodeCompiler.Cpp,
				kHelloCpp,
				null)) {
				ClassicAssert.IsNotNull (ostm);
				ClassicAssert.IsTrue (ostm.Length > 0);
			}
		}


		[Test]
		public void CppSyntaxError ()
		{
			ClassicAssert.Throws<Exception> (() => {
				using (Stream ostm = CompileStringUsing (null, XCodeCompiler.Cpp,
				    "thisisnotcpp",
				    null)) {
					ClassicAssert.IsNotNull (ostm);
					ClassicAssert.IsTrue (ostm.Length > 0);
				}
			});
		}


		[Test]
		public void HelloWorldObjC ()
		{
			using (Stream ostm = CompileStringUsing (null, XCodeCompiler.ObjectiveC,
				kHelloObjC,
				null)) {
				ClassicAssert.IsNotNull (ostm);
				ClassicAssert.IsTrue (ostm.Length > 0);
			}
		}

		[Test]
		public void ObjCSyntaxError ()
		{
			ClassicAssert.Throws<Exception> (() => {
				using (Stream ostm = CompileStringUsing (null, XCodeCompiler.ObjectiveC,
				    "thisisnotobjc",
				    null)) {
					ClassicAssert.IsNotNull (ostm);
					ClassicAssert.IsTrue (ostm.Length > 0);
				}
			});
		}


		[Test]
		public void SwiftSyntaxError ()
		{
			ClassicAssert.Throws<Exception> (() => {
				using (Stream ostm = CompileStringUsing (null, XCodeCompiler.Swiftc,
				    "thisisnotswift",
				    null)) {
					ClassicAssert.IsNotNull (ostm);
					ClassicAssert.IsTrue (ostm.Length > 0);
				}
			});
		}

		[Test]
		public void HelloWorldCSharp ()
		{
			using (Stream ostm = CompileStringUsing (null, XCodeCompiler.CSharp,
				kHelloCSharp,
				null)) {
				ClassicAssert.IsNotNull (ostm);
				ClassicAssert.IsTrue (ostm.Length > 0);
			}
		}

		[Test]
		public void CSharpSyntaxError ()
		{
			ClassicAssert.Throws<Exception> (() => {
				using (Stream ostm = CompileStringUsing (null, XCodeCompiler.CSharp,
				    "thisisnotcsharp",
				    null)) {
					ClassicAssert.IsNotNull (ostm);
					ClassicAssert.IsTrue (ostm.Length > 0);
				}
			});
		}

		[Test]
		public void HelloWorldSwiftcCustom ()
		{
			using (Stream ostm = CompileStringUsing (null, XCodeCompiler.Swiftc,
				kHelloSwift,
				null)) {
				ClassicAssert.IsNotNull (ostm);
				ClassicAssert.IsTrue (ostm.Length > 0);
			}
		}

		public static void FixXamGlueReferenceInDylibs (string workingDirectory)
		{
			foreach (var dylib in Directory.GetFiles (workingDirectory, "*.dylib"))
				FixXamGlueReferenceInDylibs (workingDirectory, dylib);
		}

		public static void FixXamGlueReferenceInDylibs (string workingDirectory, string library)
		{
			var dylib = Path.Combine (workingDirectory, library);
			if (!File.Exists (dylib))
				return;

			// Copy the XamGlue library if we don't already have it
			var thisLocation = Path.Combine (workingDirectory, "XamGlue.framework");
			var xamGlueLibrary = Path.Combine (thisLocation, "XamGlue");
			if (!File.Exists (xamGlueLibrary)) {
				if (!Directory.Exists (thisLocation))
					Directory.CreateDirectory (thisLocation);
				File.Copy (Path.Combine (Compiler.kSwiftRuntimeGlueDirectory, "XamGlue"), xamGlueLibrary, true);
			}
		}

		static bool RunCurrentTestWithLeaks {
			get {
				// Currently the leaks tool from Xcode 9.* requires sudo to work.
				// The leaks tool from Xcode 10+ works fine, but that's not the Xode version binding-tools-for-swift currently uses.
				// We run with sudo on the bots, so disable leaks tests elsewhere so that people don't have to run with sudo.
				if (string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("JENKINS_HOME")))
					return false;

				var trace = new StackTrace (2, false);
				for (int i = 0; i < trace.FrameCount; i++) {
					var frame = trace.GetFrame (i);
					var method = frame.GetMethod ();

					if (method.DeclaringType.Assembly != typeof (Compiler).Assembly)
						return false;

					if (method.IsDefined (typeof (RunWithLeaksAttribute), false))
						return true;

					if (method.IsDefined (typeof (TestAttribute), false) && method.DeclaringType.IsDefined (typeof (RunWithLeaksAttribute), false))
						return true;
				}
				return false;
			}
		}

		public static int RunCommandWithLeaks (string executable, StringBuilder args, IDictionary<string, string> env = null, StringBuilder output = null, bool verbose = false, string workingDirectory = null)
		{
			string outputFile = null;
			try {
				if (RunCurrentTestWithLeaks) {
					// Run with the leaks executable (in fact our wrapper tool for the leaks tool).
					args.Insert (0, ' ');
					args.Insert (0, executable);
					executable = Compiler.kLeakCheckBinary;
					// The DYLD_variables don't reach the leaktest executable for some reason, so rename them, and then rename them back in the leaktest executable.
					foreach (var key in new List<string> (env.Keys)) {
						env.Add ("LEAKTEST_" + key, env [key]);
						env.Remove (key);
					}
					// Running with leaks causes additional output to stdout/stderr, so take steps to write the stdout from the test itself to a file to untangle it from leaks' output.
					outputFile = Path.GetTempFileName ();
					env.Add ("LEAKTEST_STDOUT_PATH", outputFile);
				} else {
					foreach (var key in new List<string> (env.Keys)) {
						if (key.StartsWith ("LEAK", StringComparison.Ordinal))
							env.Remove (key);
					}
				}

				var rv = ExecAndCollect.RunCommand (executable, args.ToString (), env, output, workingDirectory: workingDirectory ?? string.Empty);

				if (rv != 0) {
					var outputStr = output.ToString ();
					Console.WriteLine ($"Test failed to execute (exit code: {rv}):\n{outputStr}");
					throw new Exception ($"Test failed to execute (exit code: {rv}):\n{outputStr}");
				}

				if (!string.IsNullOrEmpty (outputFile)) {
					output.Clear ();
					output.Append (File.ReadAllText (outputFile));
				}

				return rv;
			} finally {
				if (!string.IsNullOrEmpty (outputFile))
					File.Delete (outputFile);
			}
		}

		public static string RunWithDotnet (string filename, string workingDirectory = null, PlatformName platform = PlatformName.None)
		{
			// XamGlue is really a framework (XamGlue.framework), and any libraries linked with XamGlue would have a reference to @rpath/XamGlue.framework/XamGlue.
			// When running the test executable with mono (as opposed to from an actual XM/XI .app), we have no way of specifying rpath (since the executable
			// is mono itself), so we copy the XamGlue library into the current directory, and fixup any dylibs with references to @rpath/XamGlue.framework/XamGlue
			// to point to the XamGlue library instead.
			// If the libraries were compiled properly (linked with the XamGlue library instead of framework), this wouldn't be necessary, but it's simpler to
			// fixup things here than fix everywhere else.
			var executablePath = workingDirectory;
			if (string.IsNullOrEmpty (executablePath)) {
				executablePath = Path.GetDirectoryName (filename);
				if (string.IsNullOrEmpty (executablePath))
					executablePath = Environment.CurrentDirectory;
			}
			FixXamGlueReferenceInDylibs (executablePath);

			var args = new StringBuilder ();
			args.Append ("exec ");
			args.Append (Exceptions.ThrowOnNull (filename, nameof (filename))).Append (' ');
			var executable = "/usr/local/share/dotnet/dotnet";
			var env = new Dictionary<string, string> ();

			// this will let you see why things might not link
			// or why libraries might not load (for instance if dependent libraries can't be found)
			//env.Add ("MONO_LOG_LEVEL", "debug");
			//env.Add ("MONO_LOG_MASK", "dll");
			// this will print out every library that was loaded
			//env.Add ("DYLD_PRINT_LIBRARIES") = "YES";
			env.Add ("DYLD_LIBRARY_PATH", AddOrAppendPathTo (Environment.GetEnvironmentVariables (), "DYLD_LIBRARY_PATH", $"/usr/lib/swift:{kSwiftRuntimeGlueDirectory}"));
			switch (platform) {
			case PlatformName.macOS:
				break;
			case PlatformName.None:
				env ["DYLD_LIBRARY_PATH"] = AddOrAppendPathTo (env, "DYLD_LIBRARY_PATH", ".");
				if (workingDirectory != null)
					env ["DYLD_LIBRARY_PATH"] = AddOrAppendPathTo (env, "DYLD_LIBRARY_PATH", workingDirectory);
				break;
			default:
				throw new NotImplementedException (platform.ToString ());
			}

			var sb = new StringBuilder ();
			// uncomment this to see the DYLD_LIBRARY_PATH in the output.
			// Do NOT leave this uncommented as it will fail nearly all the tests
			//			sb.AppendLine ("DYLD_LIBRARY_PATH: " + env ["DYLD_LIBRARY_PATH"]);

			var rv = RunCommandWithLeaks (executable, args, env, sb, workingDirectory: workingDirectory ?? string.Empty);

			if (rv != 0) {
				Console.WriteLine ($"Test failed to execute (exit code: {rv}):\n{sb}");
				throw new Exception ($"Test failed to execute (exit code: {rv}):\n{sb}");
			}

			return sb.ToString ();
		}

		public static string AddOrAppendPathTo (System.Collections.IDictionary sd, string key, string value)
		{
			if (sd.Contains (key)) {
				return String.Format ("{0}:{1}", sd [key], value);
			} else {
				return value;
			}
		}

		public static TempDirectoryFilenameProvider CompileSwiftCode (string pathToCompiler, string options, string sourceCode)
		{
			using (MemoryStream stream = new MemoryStream ()) {
				StreamWriter writer = new StreamWriter (stream);
				writer.Write (sourceCode);
				writer.Flush ();
				stream.Position = 0;
				return CompileSwiftCode (pathToCompiler, options, stream);
			}
		}


		public static TempDirectoryFilenameProvider CompileSwiftCode (string pathToCompiler, string options, Stream sourceCode)
		{
			TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider ("swiftCompile", true);
			try {
				string path = provider.ProvideFileFor (provider.UniqueName (null, null, "swift"));
				using (FileStream codeStm = new FileStream (path, FileMode.Create)) {
					sourceCode.CopyTo (codeStm);
				}

				string args = String.Format ("{0} {1}", options, path);
				ExecAndCollect.Run (pathToCompiler, args, workingDirectory: provider.DirectoryPath);
			} catch {
				if (provider != null) {
					provider.Dispose ();
				}
				throw;
			}
			return provider;
		}
		static string? csdirectoryCache = null;
		static string GetCSSdkLocation ()
		{
			if (csdirectoryCache is null) {
				var parentDirectory = "/usr/local/share/dotnet/sdk/";
				var searchPattern = $"{kframeWorkVersion}.*";
				csdirectoryCache = MaxDirectory (parentDirectory, searchPattern);
			}
			return csdirectoryCache;
		}

		static string GetDotnetCSCompiler ()
		{
			var path = Path.Combine (GetCSSdkLocation (), "Roslyn/bincore/csc.dll");
			if (!File.Exists (path))
				throw new Exception ($"Unable to find dotnet cs compiler - expecting it at {path}");
			return path;
		}

		static string? refdirectoryCache = null;
		static string GetReferenceAssemblyLocation ()
		{
			// pattern is {sdk}{max}/ref/net{sdk}
			if (refdirectoryCache is null) {
				var parentDirectory = "/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/";
				var searchPattern = $"{kframeWorkVersion}.*";
				refdirectoryCache = Path.Combine (MaxDirectory (parentDirectory, searchPattern), $"ref/net{kframeWorkVersion}");
				if (!Directory.Exists (refdirectoryCache))
					throw new Exception ($"Unable to find dotnet reference assmeblies. Looked in {refdirectoryCache}");
			}
			return refdirectoryCache;
		}

		static string MaxDirectory (string parentDirectory, string searchPattern)
		{
			var directories = Directory.GetDirectories (parentDirectory, searchPattern);
			if (directories.Length == 0) {
				throw new Exception ($"Unable to find directory using the pattern {parentDirectory}{searchPattern}");
			}
			Array.Sort (directories);
			return directories [directories.Length - 1];
		}
	}
}

