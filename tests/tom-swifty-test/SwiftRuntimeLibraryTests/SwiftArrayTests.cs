// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SwiftRuntimeLibrary;

using NUnit.Framework;
using tomwiftytest;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibraryTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class SwiftArrayTests {
		public SwiftArrayTests ()
		{
			if (IntPtr.Size != 8)
				Assert.Ignore ("These tests are 64-bit only. Run from the command line instead if you're trying from the IDE."); // At least on macOS, because we don't build our swift libraries for 32-bit macOS.
		}

		[Test]
		public void DefaultConstructor ()
		{
			using (var arr = new SwiftArray<int> ()) {
				arr.Add (1);
				Assert.AreEqual (1, arr.Count, "Count");
			}
		}

		[Test]
		public void Constructor_Capacity ()
		{
			using (var arr = new SwiftArray<byte> ((nint) 20)) {
				Assert.AreEqual (0, arr.Count, "Count 1");
				Assert.GreaterOrEqual (arr.Capacity, 20, "Capacity 1");
				arr.Add (10);
				Assert.AreEqual (1, arr.Count, "Count 2");
				Assert.GreaterOrEqual (arr.Capacity, 20, "Capacity 2");
			}

			Assert.Throws<ArgumentOutOfRangeException> (() => new SwiftArray<int> (-1));
		}

		[Test]
		public void Constructor_Params ()
		{
			using (var arr = new SwiftArray<bool> (true, false, true)) {
				Assert.AreEqual (3, arr.Count, "Count 1");
				Assert.IsTrue (arr [0], "1");
				Assert.IsFalse (arr [1], "2");
				Assert.IsTrue (arr [2], "3");
			}

			Assert.Throws<ArgumentNullException> (() => new SwiftArray<sbyte> ((sbyte [])null), "Null");
			using (var arr = new SwiftArray<SwiftString> ((SwiftString) "Hello", (SwiftString) string.Empty)) {
				Assert.AreEqual (2, arr.Count, "Count 1");
				Assert.AreEqual ("Hello", arr [0].ToString (), "1");
				Assert.AreEqual (string.Empty, arr [1].ToString (), "2");
			}
		}

		[Test]
		public void Constructor_IList ()
		{
			var list = (IList<short>)new short [] { 1, 2, 3 };
			using (var arr = new SwiftArray<short> (list)) {
				Assert.AreEqual (3, arr.Count, "Count 1");
				Assert.AreEqual (1, arr [0], "1");
				Assert.AreEqual (2, arr [1], "2");
				Assert.AreEqual (3, arr [2], "3");
			}
			Assert.Throws<ArgumentNullException> (() => new SwiftArray<ushort> ((IList<ushort>)null), "ANE");
		}

		[Test]
		public void Constructor_IEnumerable ()
		{
			var enumerable = (IEnumerable<ushort>)new ushort [] { 1, 2, 3 };
			using (var arr = new SwiftArray<ushort> (enumerable)) {
				Assert.AreEqual (3, arr.Count, "Count 1");
				Assert.AreEqual (1, arr [0], "1");
				Assert.AreEqual (2, arr [1], "2");
				Assert.AreEqual (3, arr [2], "3");
			}
			Assert.Throws<ArgumentNullException> (() => new SwiftArray<ushort> ((IEnumerable<ushort>)null), "ANE");
		}

		[Test]
		public void Indexers ()
		{
			using (var arr = new SwiftArray<long> (1, 2, 3)) {
				Assert.AreEqual (3, arr.Count, "Count 1");
				Assert.AreEqual (1, arr [0], "1");
				Assert.AreEqual (2, arr [1], "2");
				Assert.AreEqual (3, arr [2], "3");
				Assert.Throws<IndexOutOfRangeException> (() => GC.KeepAlive (arr [3]), "IOORE 3");
				Assert.Throws<IndexOutOfRangeException> (() => GC.KeepAlive (arr [-1]), "IOORE -1");
				arr [0] = 10;
				arr [1] = 11;
				arr [2] = 12;
				Assert.Throws<IndexOutOfRangeException> (() => arr [3] = 13, "IOORE 13");
				Assert.Throws<IndexOutOfRangeException> (() => arr [-1] = 9, "IOORE 9");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => { arr [0] = 2; }, "setter ODE");
				Assert.Throws<ObjectDisposedException> (() => { var x = arr [1]; }, "getter ODE");
			}
		}

		[Test]
		public void Count ()
		{
			using (var arr = new SwiftArray<ulong> (1, 2, 3, 4, 5, 6, 7, 8, 9)) {
				Assert.AreEqual (9, arr.Count, "Count 1");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => { var x = arr.Count; }, "Count ODE");
			}
		}

		[Test]
		public void Capacity ()
		{
			using (var arr = new SwiftArray<byte> ((nint) 10)) {
				Assert.AreEqual (0, arr.Count, "Count 1");
				Assert.GreaterOrEqual (arr.Capacity, 10, "Capacity 1");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => { var x = arr.Capacity; }, "Capacity ODE");
			}
		}

		[Test]
		public void IEnumerable ()
		{
			using (var arr = new SwiftArray<sbyte> (1, 2, 3, 4, 5, 6, 7, 8, 9)) {
				var list = new List<sbyte> ();
				foreach (var item in arr)
					list.Add (item);
				CollectionAssert.AreEqual (new sbyte [] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list, "Enumerator");

				arr.Dispose ();
				var enumerator = arr.GetEnumerator (); // No exception
				Assert.Throws<ObjectDisposedException> (() => { foreach (var x in arr) { } }, "Enumerator ODE 3");
				Assert.Throws<ObjectDisposedException> (() => { enumerator.MoveNext (); }, "Enumerator ODE 2");
				Assert.DoesNotThrow (() => { var x = enumerator.Current; }, "Enumerator !ODE");
			}
		}

		[Test]
		public void Add ()
		{
			using (var arr = new SwiftArray<uint> ()) {
				Assert.AreEqual (0, arr.Count, "Count 1");
				arr.Add (20);
				Assert.GreaterOrEqual (arr.Capacity, 1, "Capacity 1");
				Assert.AreEqual (1, arr.Count, "Count 2");
				Assert.AreEqual (20, arr [0], "Item 1");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.Add (3), "Add ODE");
			}
		}

		[Test]
		public void AddRange_IList ()
		{
			var collection = (IList<int>)new int [] { 4, 5, 6 };
			using (var arr = new SwiftArray<int> ()) {
				arr.AddRange (collection);
				Assert.GreaterOrEqual (arr.Capacity, 3, "Capacity 1");
				Assert.AreEqual (3, arr.Count, "Count 2");
				Assert.AreEqual (4, arr [0], "Item 1");
				Assert.AreEqual (5, arr [1], "Item 2");
				Assert.AreEqual (6, arr [2], "Item 3");

				Assert.Throws<ArgumentNullException> (() => arr.AddRange ((IList<int>)null), "ANE");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.AddRange (collection), "AddRange ODE");
			}
		}

		[Test]
		public void AddRange_IEnumerable ()
		{
			var collection = (IEnumerable<int>)new int [] { 4, 5, 6 };
			using (var arr = new SwiftArray<int> ()) {
				arr.AddRange (collection);
				Assert.GreaterOrEqual (arr.Capacity, 3, "Capacity 1");
				Assert.AreEqual (3, arr.Count, "Count 2");
				Assert.AreEqual (4, arr [0], "Item 1");
				Assert.AreEqual (5, arr [1], "Item 2");
				Assert.AreEqual (6, arr [2], "Item 3");

				Assert.Throws<ArgumentNullException> (() => arr.AddRange ((IEnumerable<int>)null), "ANE");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.AddRange (collection), "AddRange ODE");
			}
		}

		[Test]
		public void Clear ()
		{
			using (var arr = new SwiftArray<float> (1, 2, 3, 4, 5, 6, 7, 8, 9)) {
				arr.Clear ();
				Assert.AreEqual (0, arr.Count, "Count 1");
				arr.Add (1);
				Assert.AreEqual (1, arr.Count, "Count 2");
				arr.Clear ();
				Assert.AreEqual (0, arr.Count, "Count 3");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.Clear (), "Clear ODE");
			}
		}

		[Test]
		public void Contains ()
		{
			using (var arr = new SwiftArray<ulong> (1, 2, 3, 4, 5, 6, 7, 8, 9)) {
				Assert.IsTrue (arr.Contains (8), "Contains 1");
				Assert.IsFalse (arr.Contains (10), "Contains 2");
				arr.Clear ();
				Assert.IsFalse (arr.Contains (8), "Contains 3");
				Assert.IsFalse (arr.Contains (10), "Contains 4");
				arr.Add (10);
				Assert.IsFalse (arr.Contains (8), "Contains 5");
				Assert.IsTrue (arr.Contains (10), "Contains 6");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.Contains (3), "Contains ODE");
			}
		}

		[Test]
		public void CopyTo ()
		{
			using (var arr = new SwiftArray<double> (1, 2, 3, 4, 5, 6, 7, 8, 9)) {
				var copy = new double [10];
				arr.CopyTo (copy, 1);
				CollectionAssert.AreEqual (new double [] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, copy, "CopyTo 1");

				copy = new double [9];
				Assert.Throws<ArgumentException> (() => arr.CopyTo (copy, 1), "CopyTo 2");
				CollectionAssert.AreEqual (new double [9], copy, "CopyTo 1");

				Assert.Throws<ArgumentOutOfRangeException> (() => arr.CopyTo (copy, -1), "CopyTo 3");
				Assert.Throws<ArgumentException> (() => arr.CopyTo (copy, int.MaxValue), "CopyTo 4");
				Assert.Throws<ArgumentNullException> (() => arr.CopyTo (null, 0), "CopyTo Null");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.CopyTo (copy, 0), "CopyTo ODE");
			}
		}

		[Test]
		public void Remove ()
		{
			using (var arr = new SwiftArray<ulong> (1, 2, 3, 4, 5, 6, 7, 8, 9)) {
				Assert.IsTrue (arr.Contains (8), "Contains 1");
				Assert.IsFalse (arr.Contains (10), "Contains 2");
				Assert.IsTrue (arr.Remove (8), "Remove 1");
				Assert.AreEqual (8, arr.Count, "Count 1");
				Assert.IsFalse (arr.Contains (8), "Contains 3");
				Assert.IsFalse (arr.Contains (10), "Contains 4");

				Assert.IsFalse (arr.Remove (8), "Remove 2");
				Assert.AreEqual (8, arr.Count, "Count 2");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.Remove (1), "Remove ODE");
			}
		}

		[Test]
		public void IndexOf ()
		{
			using (var arr = new SwiftArray<ulong> (9, 8, 7, 6, 5, 4, 3, 2, 1)) {
				Assert.AreEqual (2, arr.IndexOf (7), "IndexOf 1");
				Assert.AreEqual (-1, arr.IndexOf (10), "IndexOf 2");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.IndexOf (5), "IndexOf 4");
			}
		}

		[Test]
		public void Insert ()
		{
			using (var arr = new SwiftArray<ulong> (9, 8, 7, 6, 5, 4, 3, 2, 1)) {
				arr.Insert (4, 20);
				Assert.AreEqual (10, arr.Count, "Count 1");
				CollectionAssert.AreEqual (new ulong [] { 9, 8, 7, 6, 20, 5, 4, 3, 2, 1 }, arr, "Items 1");
				Assert.AreEqual (20, arr [4], "Item 4");

				Assert.Throws<ArgumentOutOfRangeException> (() => arr.Insert (-1, 100), "Insert Ex 1");
				Assert.Throws<ArgumentOutOfRangeException> (() => arr.Insert (11, 100), "Insert Ex 2");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.Insert (11, 100), "Insert ODE");
			}
		}

		[Test]
		public void RemoveAt ()
		{
			using (var arr = new SwiftArray<ulong> (9, 8, 7, 6, 5, 4, 3, 2, 1)) {
				Assert.IsTrue (arr.Contains (8), "Contains 1");
				Assert.IsFalse (arr.Contains (10), "Contains 2");
				arr.RemoveAt (1);
				Assert.AreEqual (8, arr.Count, "Count 1");
				Assert.IsFalse (arr.Contains (8), "Contains 3");
				Assert.IsFalse (arr.Contains (10), "Contains 4");

				Assert.IsFalse (arr.Remove (8), "Remove 2");
				Assert.AreEqual (8, arr.Count, "Count 2");

				Assert.Throws<ArgumentOutOfRangeException> (() => arr.RemoveAt (-1), "RemoveAt Ex 1");
				Assert.Throws<ArgumentOutOfRangeException> (() => arr.RemoveAt (20), "RemoveAt Ex 2");
				Assert.Throws<ArgumentOutOfRangeException> (() => arr.RemoveAt (9), "RemoveAt Ex 3");

				arr.Dispose ();
				Assert.Throws<ObjectDisposedException> (() => arr.RemoveAt (1), "RemoveAt ODE");
			}
		}

		[Test]
		public void ReadOnly ()
		{
			using (var arr = new SwiftArray<ulong> (9, 8, 7, 6, 5, 4, 3, 2, 1)) {
				Assert.IsFalse (arr.IsReadOnly, "IsReadOnly 1");

				arr.Dispose ();
				Assert.IsFalse (arr.IsReadOnly, "IsReadOnly 2"); // No ObjectDisposedException
			}
		}
	}
}
