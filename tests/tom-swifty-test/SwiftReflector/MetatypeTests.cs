// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using tomwiftytest;
using SwiftReflector.TypeMapping;
using System.Collections.Generic;
using SwiftRuntimeLibrary.SwiftMarshal;
using SwiftReflector.Inventory;
using System.IO;
using System.Text;
using Xamarin;
using System.Linq;
using SwiftRuntimeLibrary;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.Reflection;
using Dynamo.CSLang;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class MetatypeTests {
		static string ToStringLiteral (string s)
		{
			using (var writer = new StringWriter ()) {
				using (var provider = CodeDomProvider.CreateProvider ("CSharp")) {
					provider.GenerateCodeFromExpression (new CodePrimitiveExpression (s), writer, null);
					return writer.ToString ();
				}
			}

		}


		[Test]
		public void SiTest ()
		{
			CheckType1 ("nint", "Struct\n");
			CheckName ("nint", "Swift.Int\n");
		}

		[Test]
		public void SuTest ()
		{
			CheckType1 ("nuint", "Struct\n");
			CheckName ("nuint", "Swift.UInt\n");
		}

		[Test]
		public void SSTest ()
		{
			CheckType1 ("SwiftString", "Struct\n");
			CheckName ("SwiftString", "Swift.String\n");
		}

		[Test]
		public void SbTest ()
		{
			CheckType1 ("bool", "Struct\n");
			CheckName ("bool", "Swift.Bool\n");
		}

		[Test]
		public void SfTest ()
		{
			CheckType1 ("float", "Struct\n");
			CheckName ("float", "Swift.Float\n");
		}

		[Test]
		public void SdTest ()
		{
			CheckType1 ("double", "Struct\n");
			CheckName ("double", "Swift.Double\n");
		}

		[Test]
		public void ByteTest ()
		{
			CheckType1 ("byte", "Struct\n");
			CheckName ("byte", "Swift.UInt8\n");
		}

		[Test]
		public void SByteTest ()
		{
			CheckType1 ("sbyte", "Struct\n");
			CheckName ("sbyte", "Swift.Int8\n");
		}

		[Test]
		public void ShortTest ()
		{
			CheckType1 ("short", "Struct\n");
			CheckName ("short", "Swift.Int16\n");
		}

		[Test]
		public void UShortTest ()
		{
			CheckType1 ("ushort", "Struct\n");
			CheckName ("ushort", "Swift.UInt16\n");
		}

		[Test]
		public void Int32Test ()
		{
			CheckType1 ("int", "Struct\n");
			CheckName ("int", "Swift.Int32\n");
		}

		[Test]
		public void UInt32Test ()
		{
			CheckType1 ("uint", "Struct\n");
			CheckName ("uint", "Swift.UInt32\n");
		}

		[Test]
		public void Int64Test ()
		{
			CheckType1 ("long", "Struct\n");
			CheckName ("long", "Swift.Int64\n");
		}

		[Test]
		public void UInt64Test ()
		{
			CheckType1 ("ulong", "Struct\n");
			CheckName ("ulong", "Swift.UInt64\n");
		}

		[TestCase ("MetalPerformanceShaders.MPSCnnBinaryConvolutionFlags", "MPSCNNBinaryConvolutionFlags")]
		[TestCase ("AudioToolbox.AudioFileSmpteTime", "AudioFile_SMPTE_Time")]
		[TestCase ("CloudKit.CKRecordZoneCapabilities", "Capabilities")]
		[TestCase ("MapKit.MKDistanceFormatterUnitStyle", "DistanceUnitStyle")]
		[TestCase ("StoreKit.SKProductDiscountPaymentMode", "PaymentMode")]
		public void EnumTest (string originalType, string expected)
		{
			CheckNameExecute (originalType, "__C." + expected + "\n"); // available in both iOS and Mac, otherwise not special
		}

		void CheckType1 (string typeName, string expected)
		{
			using (DisposableTempDirectory temp = new DisposableTempDirectory (null, true)) {

				string csFile = Path.Combine (temp.DirectoryPath, temp.UniqueName ("CS", "", "cs"));
				string source = $@"using System;
using SwiftRuntimeLibrary;

using SwiftRuntimeLibrary.SwiftMarshal;
namespace dlopentest
{{
	class MainClass
	{{
		public static void Main (string[] args)
		{{
			SwiftMetatype mt = StructMarshal.Marshaler.Metatypeof(typeof({typeName}));
			Console.WriteLine(mt.Kind);
		}}
	}}
}}";
				source += TestRunning.GetManagedConsoleRedirectCode ();
				File.WriteAllText (csFile, source);

				Compiler.CSCompile (temp.DirectoryPath, new string [] { csFile }, "TestIt.exe", $"-lib:{Compiler.CompilerLocation.SwiftCompilerLib}", PlatformName.macOS);
				TestRunning.CopyTestReferencesTo (temp.DirectoryPath);

				string output = Compiler.RunWithMono (Path.Combine (temp.DirectoryPath, "TestIt.exe"), temp.DirectoryPath, platform: PlatformName.macOS);
				Assert.AreEqual (expected, output);
				var typeBasedClassName = typeName.Replace('.', '_');

				string tsource = $@"using System;
using NewClassCompilerTests;
using SwiftRuntimeLibrary;
using TomTest;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace MetatypeTests
{{
	public class CheckTypeOne{typeBasedClassName} : ITomTest
	{{
	    public void Run()
		{{
			SwiftMetatype mt = StructMarshal.Marshaler.Metatypeof(typeof({typeName}));
			Console.WriteLine(mt.Kind);
		}}

		public string TestName {{ get {{ return ""CheckTypeOne{typeName}""; }} }}
		public string ExpectedOutput {{ get {{ return { ToStringLiteral (expected)}; }} }}
	}}
}}";
				tsource += TestRunning.GetManagedConsoleRedirectCode ();
				string thisTestPath = Path.Combine (Compiler.kSwiftDeviceTestRoot, "MetatypeTests");

				Directory.CreateDirectory (thisTestPath);
				string tpath = Path.Combine (thisTestPath, $"CheckTypeOne{typeName}.cs");
				File.WriteAllText (tpath, tsource);
			}
		}

		void CheckNameExecute (string typeName, string expected)
		{
			using (DisposableTempDirectory temp = new DisposableTempDirectory (null, true)) {

				string csFile = Path.Combine (temp.DirectoryPath, temp.UniqueName ("CS", "", "cs"));
				string source = $@"using System;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace dlopentest
{{
	class MainClass
	{{
		public static void Main (string[] args)
		{{
			SwiftNominalTypeDescriptor nt = StructMarshal.Marshaler.Metatypeof(typeof({typeName})).GetNominalTypeDescriptor();
			Console.WriteLine(nt.GetFullName());
		}}
	}}
}}";
				source += TestRunning.GetManagedConsoleRedirectCode ();
				File.WriteAllText (csFile, source);
				Compiler.CSCompile (temp.DirectoryPath, new string [] { csFile }, "TestIt.exe", $"-lib:{Compiler.CompilerLocation.SwiftCompilerLib}", PlatformName.macOS);
				TestRunning.CopyTestReferencesTo (temp.DirectoryPath);

				var output = TestRunning.Execute (temp.DirectoryPath, "TestIt.exe", PlatformName.macOS);
				Assert.AreEqual (expected, output);

				var tsource = $@"using System;
using NewClassCompilerTests;
using SwiftRuntimeLibrary;
using TomTest;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace MetatypeTests
{{
	public class CheckName{typeName} : ITomTest
	{{
		public void Run()
		{{
			SwiftNominalTypeDescriptor nt = StructMarshal.Marshaler.Metatypeof(typeof({typeName})).GetNominalTypeDescriptor();
			Console.WriteLine(nt.GetFullName());
		}}

		public string TestName {{ get {{ return ""CheckName{typeName}""; }} }}
		public string ExpectedOutput {{ get {{ return {ToStringLiteral (expected)}; }} }}
	}}
}}";

				var thisTestPath = Path.Combine (Compiler.kSwiftDeviceTestRoot, "MetatypeTests");

				Directory.CreateDirectory (thisTestPath);
				var tpath = Path.Combine (thisTestPath, $"CheckNameExecute{typeName}.cs");
				File.WriteAllText (tpath, tsource);
			}
		}

		void CheckName (string typeName, string expected)
		{
			using (DisposableTempDirectory temp = new DisposableTempDirectory (null, true)) {

				var csFile = Path.Combine (temp.DirectoryPath, temp.UniqueName ("CS", "", "cs"));
				var source = $@"using System;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace dlopentest
{{
	class MainClass
	{{
		public static void Main (string[] args)
		{{
			SwiftNominalTypeDescriptor nt = StructMarshal.Marshaler.Metatypeof(typeof({typeName})).GetNominalTypeDescriptor();
			Console.WriteLine(nt.GetFullName());
		}}
	}}
}}";
				source += TestRunning.GetManagedConsoleRedirectCode ();
				File.WriteAllText (csFile, source);
				Compiler.CSCompile (temp.DirectoryPath, new string [] { csFile }, "TestIt.exe", $"-lib:{Compiler.CompilerLocation.SwiftCompilerLib}", PlatformName.macOS);
				TestRunning.CopyTestReferencesTo (temp.DirectoryPath);

				var output = Compiler.RunWithMono (Path.Combine (temp.DirectoryPath, "TestIt.exe"), temp.DirectoryPath, platform: PlatformName.macOS);
				Assert.AreEqual (expected, output);

				var tsource = $@"using System;
using NewClassCompilerTests;
using SwiftRuntimeLibrary;
using TomTest;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace MetatypeTests
{{
	public class CheckName{typeName} : ITomTest
	{{
		public void Run()
		{{
			SwiftNominalTypeDescriptor nt = StructMarshal.Marshaler.Metatypeof(typeof({typeName})).GetNominalTypeDescriptor();
			Console.WriteLine(nt.GetFullName());
		}}

		public string TestName {{ get {{ return ""CheckName{typeName}""; }} }}
		public string ExpectedOutput {{ get {{ return {ToStringLiteral (expected)}; }} }}
	}}
}}";

				var thisTestPath = Path.Combine (Compiler.kSwiftDeviceTestRoot, "MetatypeTests");

				Directory.CreateDirectory (thisTestPath);
				var tpath = Path.Combine (thisTestPath, $"CheckName{typeName}.cs");
				File.WriteAllText (tpath, tsource);
			}
		}


		void TestObjectType (string type, string expectedOutput, PlatformName platform)
		{
			string source =
// no-op
$"public func GetType{type} () {{\n" +
"}\n";
			string testName = $"TestObjectType{type}";
			var mtID = new CSIdentifier ("mt");
			var mtDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, mtID, new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, new CSSimpleType (type).Typeof ()));
			var printer = CSFunctionCall.ConsoleWriteLine (mtID.Dot (new CSIdentifier ("Kind")));
			var callingCode = CSCodeBlock.Create (mtDecl, printer);
			TestRunning.TestAndExecute (source, callingCode, expectedOutput, testName:testName, platform: platform);
		}

		[TestCase (PlatformName.iOS)]
		public void NSCalculationErrorTest (PlatformName platform)
		{
			TestObjectType ("NSCalculationError", "Enum\n", platform);
		}

		[TestCase (PlatformName.iOS)]
		public void NSDecimalTest (PlatformName platform)
		{
			TestObjectType ("NSDecimal", "Struct\n", platform);
		}

		[TestCase("byte", "1\n")]
		[TestCase ("int", "4\n")]
		[TestCase ("double", "8\n")]
		public void TestSizeof(string typeName, string expected)
		{
			using (DisposableTempDirectory temp = new DisposableTempDirectory (null, true)) {

				string csFile = Path.Combine (temp.DirectoryPath, temp.UniqueName ("CS", "", "cs"));
				string source = $@"using System;
using SwiftRuntimeLibrary;

using SwiftRuntimeLibrary.SwiftMarshal;
namespace dlopentest
{{
	class MainClass
	{{
		public static void Main (string[] args)
		{{
			var size = StructMarshal.Marshaler.Sizeof(typeof({typeName}));
			Console.WriteLine(size);
		}}
	}}
}}";
				source += TestRunning.GetManagedConsoleRedirectCode ();
				File.WriteAllText (csFile, source);

				Compiler.CSCompile (temp.DirectoryPath, new string [] { csFile }, "TestIt.exe", $"-lib:{Compiler.CompilerLocation.SwiftCompilerLib}");
				TestRunning.CopyTestReferencesTo (temp.DirectoryPath);

				string output = Compiler.RunWithMono (Path.Combine (temp.DirectoryPath, "TestIt.exe"), temp.DirectoryPath);
				Assert.AreEqual (expected, output);
			}
		}


	}
}

