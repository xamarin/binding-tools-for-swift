using System;
using NUnit.Framework;
using tomwiftytest;
using Dynamo;
using Dynamo.SwiftLang;
using SwiftReflector.Inventory;
using System.Linq;
using Dynamo.CSLang;
using SwiftRuntimeLibrary;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class LinkageTests {


		[Test]
		[TestCase (PlatformName.iOS)]
		[TestCase (PlatformName.macOS)]
		public void TestMissingNSObject(PlatformName platform)
		{
			string swiftCode =
				"import Foundation\n" +
				"public final class MissingNSObject {\n" +
				"   public init() { }\n" + 
				"   public func val() -> Int { return 5; }\n" +
				"   public func skip(a:NSObject) { }\n" +
				"}";


			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			callingCode.Add (CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Ctor ("MissingNSObject").Dot (CSFunctionCall.Function ("Val"))));
			TestRunning.TestAndExecute (swiftCode, callingCode, "5\n", platform: platform);			
		}
	}
}
