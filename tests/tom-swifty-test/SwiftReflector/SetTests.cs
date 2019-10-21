using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using tomwiftytest;
using SwiftReflector.Inventory;
using SwiftReflector.IOUtils;
using Dynamo.CSLang;


namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class SetTests {

		[Test]
		[TestCase ("Bool", "bool", "true")]
		[TestCase ("Int", "nint", "43")]
		[TestCase ("Float", "float", "42.1f")]
		[TestCase ("Double", "double", "42.1")]
		[TestCase ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")")]
		public void TestAddCount (string swiftType, string cstype, string val1)
		{
			var variant = swiftType;
			var swiftCode =
				$"public func makeSetTAC{variant}() -> Set<{swiftType}> {{\n" +
				"    return Set()\n" +
				"}\n";
			var callingCode = new CodeElementCollection<ICodeElement> ();

			var setID = new CSIdentifier ("theSet");
			var setDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftSet", false, new CSSimpleType (cstype)), setID,
								     new CSFunctionCall ($"TopLevelEntities.MakeSetTAC{variant}", false));
			var countLine = CSFunctionCall.ConsoleWriteLine (setID.Dot (new CSIdentifier ("Count")));
			var addLine = CSFunctionCall.FunctionCallLine ("theSet.Insert", false, new CSIdentifier (val1));

			callingCode.Add (setDecl);
			callingCode.Add (countLine);
			callingCode.Add (addLine);
			callingCode.Add (countLine);

			TestRunning.TestAndExecute (swiftCode, callingCode, "0\n1\n", testName : $"TestAddCount{variant}");
		}

		[Test]
		[TestCase ("Bool", "bool", "true")]
		[TestCase ("Int", "nint", "43")]
		[TestCase ("Float", "float", "42.1f")]
		[TestCase ("Double", "double", "42.1")]
		[TestCase ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")")]
		public void TestSetContains (string swiftType, string cstype, string val)
		{
			var variant = swiftType;
			var swiftCode =
				$"public func makeSetTC{variant}() -> Set<{swiftType}> {{\n" +
				"    return Set()\n" +
				"}\n";
			var callingCode = new CodeElementCollection<ICodeElement> ();

			var setID = new CSIdentifier ("theSet");
			var setDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftSet", false, new CSSimpleType (cstype)), setID,
								     new CSFunctionCall ($"TopLevelEntities.MakeSetTC{variant}", false));
			var containsLine = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("theSet.Contains", (CSIdentifier)val));

			var addLine = CSFunctionCall.FunctionCallLine ("theSet.Insert", false, new CSIdentifier (val));

			callingCode.Add (setDecl);
			callingCode.Add (containsLine);
			callingCode.Add (addLine);
			callingCode.Add (containsLine);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\nTrue\n", testName : $"TestSetContains{variant}");
		}

		[Test]
		[TestCase ("Bool", "bool", "true")]
		[TestCase ("Int", "nint", "43")]
		[TestCase ("Float", "float", "42.1f")]
		[TestCase ("Double", "double", "42.1")]
		[TestCase ("String", "SwiftString", "SwiftString.FromString(\"hi mom\")")]
		public void TestSetRemove (string swiftType, string cstype, string val)
		{
			var variant = swiftType;
			var swiftCode =
				$"public func makeSetTR{variant}() -> Set<{swiftType}> {{\n" +
				"    return Set()\n" +
				"}\n";


			var callingCode = new CodeElementCollection<ICodeElement> ();

			var setID = new CSIdentifier ("theSet");
			var setDecl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftSet", false, new CSSimpleType (cstype)), setID,
								     new CSFunctionCall ($"TopLevelEntities.MakeSetTR{variant}", false));
			var valID = new CSIdentifier ("theVal");
			var valDecl = CSVariableDeclaration.VarLine (new CSSimpleType (cstype), valID, new CSIdentifier (val));
			var containsLine = CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ("theSet.Contains", (CSIdentifier)val));

			var addLine = CSFunctionCall.FunctionCallLine ("theSet.Insert", false, valID);
			var removeLine = CSFunctionCall.FunctionCallLine ("theSet.Remove", false, valID);

			callingCode.Add (setDecl);
			callingCode.Add (valDecl);
			callingCode.Add (containsLine);
			callingCode.Add (addLine);
			callingCode.Add (containsLine);
			callingCode.Add (removeLine);
			callingCode.Add (containsLine);

			TestRunning.TestAndExecute (swiftCode, callingCode, "False\nTrue\nFalse\n", testName : $"TestSetRemove{variant}");
		}
	}
}
