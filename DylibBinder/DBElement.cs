using System;
using System.Collections.Generic;

namespace DylibBinder {
	public class DBElement {
		public DBElement (string name, string intValue)
		{
			Name = name;
			IntValue = intValue;
		}

		public string Name { get; }
		public string IntValue { get; }
	}

	public class DBElements {
		public DBElements ()
		{
		}

		public List<DBElement> Elements { get; } = new List<DBElement> ();
	}
}
