// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SwiftRuntimeLibrary;

namespace SwiftReflector.Importing {
	public class PatternMatch {
		Regex regex;
		public PatternMatch (string pattern)
		{
			Pattern = Exceptions.ThrowOnNull (pattern, nameof (pattern));
			regex = new Regex (pattern);
		}
		public string Pattern { get; set; }

		public bool Matches (string target)
		{
			return regex.Match (target).Success;
		}
	}

	public class MatchCollection : List<PatternMatch>
	{
		public MatchCollection ()
			: base ()
		{
		}

		public MatchCollection (IEnumerable<PatternMatch> matches)
			: this()
		{
			AddRange (matches);
		}

		public bool Matches (string target)
		{
			return this.Any (pattern => pattern.Matches (target));
		}

		public bool Matches<T> (T thing, Func<T, string> selector)
		{
			return Matches (selector (thing));
		}

		public IEnumerable<T> Including<T> (IEnumerable <T> things, Func<T, string> selector)
		{
			return things.Where (thing => Matches (thing, selector));
		}

		public IEnumerable<T> Excluding<T> (IEnumerable<T> things, Func<T, string> selector)
		{
			return things.Where (thing => !Matches (thing, selector));
		}

		public static MatchCollection FromXml (string xmlFile, bool loadIncludes)
		{
			Exceptions.ThrowOnNull (xmlFile, nameof (xmlFile));

			var doc = XDocument.Load (xmlFile);
			var matches = from elem in doc.Element (loadIncludes ? "Includes" : "Excludes").Elements ("Pattern")
				      select new PatternMatch (elem.Value);
			return new MatchCollection (matches);
		}
	}
}
