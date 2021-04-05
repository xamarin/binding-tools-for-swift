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

		public static SortedSet<OverloadInventory> CreateOverloadSortedSet ()
		{
			Comparison<OverloadInventory> comp = (x, y) => string.Compare (x.Name.Name, y.Name.Name);
			return new SortedSet<OverloadInventory> (Comparer<OverloadInventory>.Create (comp));
		}

		public static SortedSet<TLFunction> CreateTLFunctionSortedSet ()
		{
			Comparison<TLFunction> comp = (x, y) => string.Compare (x.Module.Name, y.Module.Name);
			return new SortedSet<TLFunction> (Comparer<TLFunction>.Create (comp));
		}

		public static SortedSet<PropertyContents> CreatePropertySortedSet ()
		{
			Comparison<PropertyContents> comp = (x, y) => string.Compare (x.Name.Name, y.Name.Name);
			return new SortedSet<PropertyContents> (Comparer<PropertyContents>.Create (comp));
		}

		public static SortedSet<ProtocolContents> CreateProtocolSortedSet ()
		{
			Comparison<ProtocolContents> comp = (x, y) => string.Compare (x.Name.ToFullyQualifiedName (), y.Name.ToFullyQualifiedName ());
			return new SortedSet<ProtocolContents> (Comparer<ProtocolContents>.Create (comp));
		}

		public static SortedSet<ClassContents> CreateClassSortedSet ()
		{
			Comparison<ClassContents> comp = (x, y) => string.Compare (x.Name.ToFullyQualifiedName (), y.Name.ToFullyQualifiedName ());
			return new SortedSet<ClassContents> (Comparer<ClassContents>.Create (comp));
		}

		public static SortedSet<string> CreateStringSortedSet ()
		{
			Comparison<string> comp = (x, y) => string.Compare (x, y);
			return new SortedSet<string> (Comparer<string>.Create (comp));
		}
	}
}
