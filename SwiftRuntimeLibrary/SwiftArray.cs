// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.Array")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.SwiftArray_NominalTypeDescriptor, "", "")]
	public sealed class SwiftArray<T> : SwiftNativeValueType, ISwiftStruct, IList<T> {

		byte [] CheckedSwiftData {
			get {
				if (SwiftData == null)
					throw new ObjectDisposedException (GetType ().ToString ());
				return SwiftData;
			}
		}

		public SwiftArray (nint capacity)
			: this (NativeMethodsForSwiftArray.NewArray (ValidateCapacity (capacity), ElementMetatype))
		{
		}

		static nint ValidateCapacity (nint capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException (nameof (capacity), "Capacity must be positive");
			return capacity;
		}

		internal SwiftArray (IntPtr p)
			: this (SwiftValueTypeCtorArgument.None)
		{
			CheckedSwiftData.WriteIntPtr (p, offset: 0);
		}

		public SwiftArray ()
			: this ((nint)0)
		{

		}

		public SwiftArray (IList<T> list)
			: this (Exceptions.ThrowOnNull (list, nameof (list)).Count)
		{
			AddRange (list);
		}

		public SwiftArray (IEnumerable<T> collection)
			: this ((nint)0)
		{
			AddRange (collection);
		}

		public SwiftArray (params T [] items)
			: this (Exceptions.ThrowOnNull (items, nameof (items)).Length)
		{
			AddRange (items);
		}

		static SwiftMetatype ElementMetatype {
			get {
				return StructMarshal.Marshaler.Metatypeof (typeof (T));
			}
		}

		static int ElementStride {
			get {
				return StructMarshal.Marshaler.Strideof (typeof (T));
			}
		}

		static int ElementSizeOf {
			get {
				return StructMarshal.Marshaler.Sizeof (typeof (T));
			}
		}

		~SwiftArray ()
		{
			Dispose (false);
		}

		internal SwiftArray (SwiftValueTypeCtorArgument unused)
			: base ()
		{
			
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForSwiftArray.PIMetadataAccessor_SwiftArray (SwiftMetadataRequest.Complete, ElementMetatype);
		}

		public T this [int index] {
			get {
				if (index < 0 || index >= Count)
					throw new IndexOutOfRangeException ();

				unsafe {
					byte* p = stackalloc byte [ElementStride];
					NativeMethodsForSwiftArray.ArrayGet (p, CheckedSwiftData, index, ElementMetatype);
					return StructMarshal.Marshaler.ToNet<T> (p, false);
				}
			}
			set {
				if (index < 0 || index >= Count)
					throw new IndexOutOfRangeException ();

				unsafe {
					byte* buff = stackalloc byte [ElementStride];
					StructMarshal.Marshaler.ToSwift (typeof (T), value, buff);
					NativeMethodsForSwiftArray.ArraySet (CheckedSwiftData, buff, index, ElementMetatype);
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), new IntPtr (buff));
				}
			}
		}

		public int Count {
			get {
				return (int) NativeMethodsForSwiftArray.Count (CheckedSwiftData, ElementMetatype);
			}
		}

		public int Capacity {
			get {
				return (int) NativeMethodsForSwiftArray.ArrayCapacity (CheckedSwiftData, ElementMetatype);
			}
		}

		public IEnumerator<T> GetEnumerator ()
		{
			int count = Count;
			for (int i = 0; i < count; i++) {
				yield return this [i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public void Add (T item)
		{
			unsafe {
				byte* buff = stackalloc byte [ElementStride];
				StructMarshal.Marshaler.ToSwift (typeof (T), item, buff);
				NativeMethodsForSwiftArray.ArrayAdd (CheckedSwiftData, buff, ElementMetatype);
				StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), new IntPtr (buff));
			}
		}

		public void AddRange (IList<T> list)
		{
			if (list == null)
				throw new ArgumentNullException (nameof (list));

			for (int i = 0; i < list.Count; i++)
				Add (list [i]);
		}

		public void AddRange (IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException (nameof (collection));

			foreach (T elem in collection) {
				Add (elem);
			}
		}


		public void Clear ()
		{
			NativeMethodsForSwiftArray.ArrayClear (CheckedSwiftData, ElementMetatype);
		}

		public bool Contains (T item)
		{
			foreach (T thing in this) {
				if (Equals (thing, item))
					return true;
			}
			return false;
		}

		public void CopyTo (T [] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException (nameof (array));

			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException (nameof (arrayIndex));

			if (Count > array.Length - arrayIndex)
				throw new ArgumentException ("Destination array was not long enough.");

			foreach (T thing in this) {
				array [arrayIndex++] = thing;
			}
		}

		public bool Remove (T item)
		{
			nint i = 0;
			foreach (T thing in this) {
				if (Equals (thing, item)) {
					NativeMethodsForSwiftArray.ArrayRemoveAt (CheckedSwiftData, i, ElementMetatype);
					return true;
				}
				i++;
			}
			return false;
		}

		public int IndexOf (T item)
		{
			int i = 0;
			foreach (T thing in this) {
				if (Equals (thing, item))
					return i;
				i++;
			}
			return -1;
		}

		public void Insert (int index, T item)
		{
			if (index < 0 || index >= Count)
				throw new ArgumentOutOfRangeException (nameof (index));

			unsafe {
				byte* buff = stackalloc byte [ElementStride];
				StructMarshal.Marshaler.ToSwift (typeof (T), item, buff);
				NativeMethodsForSwiftArray.ArrayInsert (CheckedSwiftData, buff, index, ElementMetatype);
				StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), new IntPtr (buff));
			}
		}


		public void RemoveAt (int index)
		{
			if (index < 0 || index >= Count)
				throw new ArgumentOutOfRangeException (nameof (index));

			NativeMethodsForSwiftArray.ArrayRemoveAt (CheckedSwiftData, index, ElementMetatype);
		}


		public bool IsReadOnly {
			get {
				return false;
			}
		}

	}

	internal class NativeMethodsForSwiftArray {
		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.SwiftArray_PIMetadataAccessor)]
		public static extern SwiftMetatype PIMetadataAccessor_SwiftArray (SwiftMetadataRequest request, SwiftMetatype m);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.SwiftArray_CTor)]
		public static extern IntPtr CTor (SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_Count)]
		public static extern nint Count (byte[] data, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_NewArray)]
		public static extern IntPtr NewArray (nint capacity, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArrayGet)]
		public unsafe static extern void ArrayGet (byte* data, byte[] arrPtr, nint index, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArraySet)]
		public unsafe static extern void ArraySet (byte[] data, byte* elem, nint index, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArrayClear)]
		public static extern void ArrayClear (byte[] data, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArrayRemoveAt)]
		public static extern void ArrayRemoveAt (byte[] data, nint index, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArrayAdd)]
		public unsafe static extern void ArrayAdd (byte[] data, byte* elem, SwiftMetatype m);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArrayInsert)]
		public unsafe static extern void ArrayInsert (byte[] data, byte* elem, nint index, SwiftMetatype m);
			
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftArray_ArrayCapacity)]
		public static extern nint ArrayCapacity (byte[] data, SwiftMetatype m);

	}
}
