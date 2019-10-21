// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo;
using Dynamo.CSLang;
using NUnit.Framework;
using tomwiftytest;

namespace SwiftReflector
{
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class UnicodeMappingUnitTests
	{
		public const string Template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<unicodemapping version = ""1.0"">
	{0}
</unicodemapping>";

		public static string TemplateWith (string s) => string.Format (Template, s);

		[Test]
		public void EmptyMappingsAreIgnored ()
		{
			var mapper = new UnicodeMapper ();
			mapper.AddMappingsFromXML (TemplateWith (""));
			Assert.AreEqual ("U03A3", mapper.MapToUnicodeName ("Σ"));
		}

		[Test]
		public void MappingsWithMissingPartsAreIgnored ()
		{
			var mapper = new UnicodeMapper ();
			mapper.AddMappingsFromXML (TemplateWith ($@"<map from=""Σ""/>"));
			Assert.AreEqual ("U03A3", mapper.MapToUnicodeName ("Σ"));
		}

		[Test]
		public void BuiltinMapping ()
		{
			Assert.AreEqual ("Alpha", UnicodeMapper.Default.MapToUnicodeName ("α"));
		}

		[Test]
		public void WithNoMapping ()
		{
			Assert.AreEqual ("U03B6", UnicodeMapper.Default.MapToUnicodeName ("ζ"));
		}

		[Test]
		public void XMLMapping ()
		{
			var mapper = new UnicodeMapper ();
			mapper.AddMappingsFromXML (TemplateWith ($@"<map from=""Σ"" to=""Sigma""/>"));
			Assert.AreEqual ("Sigma", mapper.MapToUnicodeName ("Σ"));
		}

		[Test]
		public void XMLMultiPartsMapping ()
		{
			var mapper = new UnicodeMapper ();
			mapper.AddMappingsFromXML (TemplateWith ($@"<map from=""🍎"" to=""Apple""/>"));
			Assert.AreEqual ("Apple", mapper.MapToUnicodeName ("🍎"));
		}

		[Test]
		public void XMLRemapping ()
		{
			var mapper = new UnicodeMapper ();
			mapper.AddMappingsFromXML (TemplateWith ($@"<map from=""Σ"" to=""NotFinalMapping""/>"));
			mapper.AddMappingsFromXML (TemplateWith ($@"<map from=""Σ"" to=""Sigma""/>"));
			Assert.AreEqual ("Sigma", mapper.MapToUnicodeName ("Σ"));
		}
	}

	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class UnicodeMappingIntegrationTests
	{
		[Test]
		public void EndToEndUnicodeMapping ()
		{
			var mapper = new UnicodeMapper ();
			UnicodeTestCore ("U0001F34E", runOnDevice: true, unicodeMapper: mapper);
			mapper.AddMappingsFromXML (UnicodeMappingUnitTests.TemplateWith ($@"<map from=""🍎"" to=""Apple""/>"));
			UnicodeTestCore ("Apple", runOnDevice: false, unicodeMapper: mapper);
		}

		static void UnicodeTestCore (string expectedName, bool runOnDevice, UnicodeMapper unicodeMapper)
		{
			string SwiftCode = @"public final class Mapple {
public static func 🍎 () -> Int {
	return 42;
}
}";

			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();

			callingCode.Add (CSFunctionCall.ConsoleWriteLine (CSFunctionCall.Function ($"Mapple.{expectedName}")));

			if (runOnDevice)
				TestRunning.TestAndExecute (SwiftCode, callingCode, "42\n", testName : $"EndToEndUnicodeMapping{expectedName}", unicodeMapper: unicodeMapper);
			else
				TestRunning.TestAndExecuteNoDevice (SwiftCode, callingCode, "42\n", testName: $"EndToEndUnicodeMapping{expectedName}", unicodeMapper: unicodeMapper);
		}
	}
}

