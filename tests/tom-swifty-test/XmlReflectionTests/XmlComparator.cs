using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlReflectionTests {
	public class XmlComparator {
		public XmlComparator ()
		{
		}

		public static List<string> Compare (XDocument first, XDocument second)
		{
			var diffs = new List<string> ();

			var firstElems = first.Elements ();
			var secondElems = second.Elements ();

			Compare (firstElems, secondElems, diffs);
			return diffs;
		}

		static void Compare (IEnumerable <XElement> first, IEnumerable<XElement> second, List<string> diffs)
		{
			var secondList = second.ToList ();

			foreach (var firstElem in first) {
				var secondElem = secondList.FirstOrDefault (el => el.Name == firstElem.Name);
				if (secondElem == null) {
					diffs.Add ($"Element {firstElem.Name} is missing from second");
				} else {
					secondList.Remove (secondElem);
					Compare (firstElem, secondElem, diffs);
				}
			}
		}

		static void Compare (XElement firstElem, XElement secondElem, List<string> diffs)
		{
			var firstAttributes = firstElem.Attributes ();
			var secondAttributes = secondElem.Attributes ();
			Compare (firstElem, firstAttributes, secondAttributes, diffs);
			if (firstElem.Value != secondElem.Value)
				diffs.Add ($"Contents of tag '{firstElem.Name}': \"{firstElem.Value}\" does not match \"{secondElem.Value}\" ");
			var firstElems = firstElem.Elements ();
			var secondElems = secondElem.Elements ();
			Compare (firstElems, secondElems, diffs);
		}

		static void Compare (XElement parentElem, IEnumerable <XAttribute> first, IEnumerable <XAttribute> second, List<string> diffs)
		{
			var secondList = second.ToList ();
			foreach (var firstAttr in first) {
				var secondAttr = secondList.FirstOrDefault (at => at.Name == firstAttr.Name);
				if (secondAttr == null) {
					diffs.Add ($"In elem {parentElem.Name}, attribute {firstAttr.Name} is missing");
				} else {
					secondList.Remove (secondAttr);
					if (firstAttr.Value != secondAttr.Value)
						diffs.Add ($"In Elem {parentElem.Name} attr {firstAttr.Name}, \"{firstAttr.Value}\" does not match \"{secondAttr.Value}\"");
				}
			}
		}
	}
}
