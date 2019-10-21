using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Dynamo.CSLang {
	public class CSInheritance : CommaListElementCollection<CSIdentifier> {
		public CSInheritance (IEnumerable<CSIdentifier> identifiers)
		{
			if (identifiers != null)
				AddRange (identifiers);
		}

		public CSInheritance (params string [] identifiers)
			: this (identifiers.Select (str => new CSIdentifier (str)))
		{
		}

		public void Add (Type t)
		{
			Exceptions.ThrowOnNull (t, "t");
			Add (new CSIdentifier (t.Name));
		}
	}
}

