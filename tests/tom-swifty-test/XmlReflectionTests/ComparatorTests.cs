using System.Text;
using System.Xml.Linq;
using NUnit;
using NUnit.Framework;

namespace XmlReflectionTests {
	[TestFixture]
	public class ComparatorTests {

		void Compare (string testName, int expectedDiffs, string firstXml, string secondXml)
		{
			var firstXDoc = XDocument.Parse (firstXml);
			var secondXDoc = XDocument.Parse (secondXml);

			var diffs = XmlComparator.Compare (firstXDoc, secondXDoc);

			var sb = new StringBuilder ();
			diffs.ForEach (s => sb.Append (s).Append ('\n'));

			Assert.AreEqual (expectedDiffs, diffs.Count, $"Mismatch from test {testName} diffs:\n{sb.ToString ()}");
		}

		[Test]
		public void TestEasyMatch ()
		{
			var first = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two three=""four""/>
  five
</one>
";
			Compare ("TestEasyMatch", 0, first, first);
		}

		[Test]
		public void TestMatchElemsDifferentOrder ()
		{
			var first = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two/>
  <three/>
</one>
";
			var second = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <three/>
  <two/>
</one>
";
			Compare ("TestMatchElemsDifferentOrder", 0, first, second);
		}

		[Test]
		public void TestMatchAttrsDifferentOrder ()
		{
			var first = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two three=""a"" four=""b""/>
</one>
";
			var second = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two four=""b"" three=""a"" />
</one>
";
			Compare ("TestMatchAttrsDifferentOrder", 0, first, second);
		}

		[Test]
		public void TestMissingAttrs ()
		{
			var first = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two three=""a"" four=""b""/>
</one>
";
			var second = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two three=""a"" />
</one>
";
			Compare ("TestMissingAttrs", 1, first, second);
		}

		[Test]
		public void TestMatchAttrsDifferentContents ()
		{
			var first = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two three=""a"" four=""b""/>
</one>
";
			var second = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two four=""b"" three=""c"" />
</one>
";
			Compare ("TestMatchAttrsDifferentContents", 1, first, second);
		}

		[Test]
		public void TestMissingElem ()
		{
			var first = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two/>
  <three/>
</one>
";
			var second = @"<?xml version=""1.0"" encoding=""UTF - 8""?>
<one>
  <two/>
</one>
";
			Compare ("TestMissingElem", 1, first, second);
		}
	}
}
