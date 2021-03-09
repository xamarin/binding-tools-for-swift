using System;
using System.Collections.Generic;

namespace DylibBinder {
	// TODO Add AssociatedTypes
	public class DBAssociatedType {
		public DBAssociatedType (string name)
		{
			Name = name;
		}

		public string Name { get; }
	}

	public class DBAssociatedTypes {
		public DBAssociatedTypes ()
		{
		}

		public List<DBAssociatedType> AssociatedTypes { get; } = new List<DBAssociatedType> ();
	}
}
