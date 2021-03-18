using System;
using System.Collections.Generic;

namespace DylibBinder {
	// TODO Add AssociatedTypes
	internal class DBAssociatedType {
		public DBAssociatedType (string name)
		{
			Name = name;
		}

		public string Name { get; }
	}

	internal class DBAssociatedTypes {
		public DBAssociatedTypes ()
		{
		}

		public List<DBAssociatedType> AssociatedTypes { get; } = new List<DBAssociatedType> ();
	}
}
