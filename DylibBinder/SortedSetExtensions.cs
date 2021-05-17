using System;
using System.Collections.Generic;
using SwiftReflector.Demangling;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal static class SortedSetExtensions {
		public static void AddRange<T> (this SortedSet<T> t, params IEnumerable<T> [] entriesArray)
		{
			foreach (var entries in entriesArray) {
				foreach (var entry in entries) {
					t.Add (entry);
				}
			}
		}

		public static SortedSet<T> Create<T> ()
		{
			switch (typeof (T)) {
			case Type t when t == typeof (OverloadInventory):
				return new SortedSet<T> (Comparer<T>.Create ((x, y) => {
					var type1 = x as OverloadInventory;
					var type2 = y as OverloadInventory;
					return string.Compare (type1?.Name.Name, type2?.Name.Name);
				}));
			case Type t when t == typeof (TLFunction):
				return new SortedSet<T> (Comparer<T>.Create ((x, y) => {
					var type1 = x as TLFunction;
					var type2 = y as TLFunction;
					return string.Compare (type1?.Module.Name, type2?.Module.Name);
				}));
			case Type t when t == typeof (PropertyContents):
				return new SortedSet<T> (Comparer<T>.Create ((x, y) => {
					var type1 = x as PropertyContents;
					var type2 = y as PropertyContents;
					return string.Compare (type1?.Name.Name, type2?.Name.Name);
				}));
			case Type t when t == typeof (ProtocolContents):
				return new SortedSet<T> (Comparer<T>.Create ((x, y) => {
					var type1 = x as ProtocolContents;
					var type2 = y as ProtocolContents;
					return string.Compare (type1?.Name.ToFullyQualifiedName (), type2?.Name.ToFullyQualifiedName ());
				}));
			case Type t when t == typeof (ClassContents):
				return new SortedSet<T> (Comparer<T>.Create ((x, y) => {
					var type1 = x as ClassContents;
					var type2 = y as ClassContents;
					return string.Compare (type1?.Name.ToFullyQualifiedName (), type2?.Name.ToFullyQualifiedName ());
				}));
			case Type t when t == typeof (string):
				return new SortedSet<T> (Comparer<T>.Create ((x, y) => {
					var type1 = x as string;
					var type2 = y as string;
					return string.Compare (type1, type2);
				}));
			default:
				throw new ArgumentException ($"Type: {typeof (T)} is not supported");
			}
		}
	}
}
