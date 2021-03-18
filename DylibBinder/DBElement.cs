using System;
using System.Collections.Generic;

namespace DylibBinder {
	// TODO Add Elements to enums
	internal class DBElement {
		public DBElement (string name, string intValue)
		{
			Name = name;
			IntValue = intValue;
		}

		public string Name { get; }
		public string IntValue { get; }
	}

	internal class DBElements {
		public DBElements ()
		{
		}

		public List<DBElement> Elements { get; } = new List<DBElement> ();
	}
}
