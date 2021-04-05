using System;
using System.Collections.Generic;

namespace DylibBinder {
	internal static class HashSetExtensions {
		public static void AddRange<T> (this HashSet<T> t, params IEnumerable<T> [] entriesArray)
		{
			foreach (var entries in entriesArray) {
				foreach (var entry in entries) {
					t.Add (entry);
				}
			}
		}
	}
}
