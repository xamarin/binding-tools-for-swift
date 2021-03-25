using System;
using System.Collections.Generic;
using SwiftReflector.Demangling;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal static class SortedSetExtensions {
		internal static void AddRange<T> (this SortedSet<T> t, params IEnumerable<T> [] entriesArray)
		{
			foreach (var entries in entriesArray) {
				foreach (var entry in entries) {
					t.Add (entry);
				}
			}
		}

		internal static SortedSet<OverloadInventory> CreateOverloadSortedSet ()
		{
			Comparison<OverloadInventory> comp = (x, y) => string.Compare (x.Name.Name, y.Name.Name);
			return new SortedSet<OverloadInventory> (Comparer<OverloadInventory>.Create (comp));
		}

		internal static SortedSet<TLFunction> CreateTLFunctionSortedSet ()
		{
			Comparison<TLFunction> comp = (x, y) => string.Compare (x.Module.Name, y.Module.Name);
			return new SortedSet<TLFunction> (Comparer<TLFunction>.Create (comp));
		}

		internal static SortedSet<PropertyContents> CreatePropertySortedSet ()
		{
			Comparison<PropertyContents> comp = (x, y) => string.Compare (x.Name.Name, y.Name.Name);
			return new SortedSet<PropertyContents> (Comparer<PropertyContents>.Create (comp));
		}

		internal static SortedSet<ProtocolContents> CreateProtocolSortedSet ()
		{
			Comparison<ProtocolContents> comp = (x, y) => string.Compare (x.Name.ToFullyQualifiedName (true), y.Name.ToFullyQualifiedName (true));
			return new SortedSet<ProtocolContents> (Comparer<ProtocolContents>.Create (comp));
		}

		internal static SortedSet<ClassContents> CreateClassSortedSet ()
		{
			Comparison<ClassContents> comp = (x, y) => string.Compare (x.Name.ToFullyQualifiedName (true), y.Name.ToFullyQualifiedName (true));
			return new SortedSet<ClassContents> (Comparer<ClassContents>.Create (comp));
		}
	}
}
