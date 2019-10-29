// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {

	[SwiftTypeName ("Swift.UnsafeMutablePointer")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.UnsafeMutablePointer_NominalTypeDescriptor, "", "")]
	public class UnsafeMutablePointer<T> : ISwiftStruct {
		internal UnsafeMutablePointer (SwiftNominalCtorArgument unused)
		{
		}

		public UnsafeMutablePointer(UnsafePointer<T> ptr)
			: this(ptr.ToIntPtr())
		{			
		}

		public UnsafeMutablePointer(OpaquePointer ptr)
			: this ((IntPtr)ptr)
		{
		}

		UnsafeMutablePointer ()
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}

		UnsafeMutablePointer(IntPtr p)
			: this()
		{
			unsafe {
				fixed (byte* ptr = this.SwiftData) {
					Marshal.WriteIntPtr (new IntPtr (ptr), p);
				}
			}
		}

		public byte [] SwiftData { get; set; }

		~UnsafeMutablePointer ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		void Dispose (bool disposing)
		{
			StructMarshal.Marshaler.ReleaseNominalData (this);
		}

		public static UnsafeMutablePointer<T> Allocate(nint capacity)
		{
			unsafe {
				var self = new UnsafeMutablePointer<T> ();
				fixed (byte* ptr = self.SwiftData) {
					var actualPtr = new IntPtr (ptr);
					NativeMethodsForUnsafeMutablePointer.Allocate (actualPtr, capacity, StructMarshal.Marshaler.Metatypeof (typeof (T)));
				}
				return self;
			}
		}

		public void Initialize (T to)
		{
			Initialize (to, 1);
		}

		public void Initialize (T repeating, nint count)
		{
			unsafe {
				fixed (byte* ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					var valPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
					var valIntPtr = new IntPtr (valPtr);
					valIntPtr = StructMarshal.Marshaler.ToSwift (repeating, valIntPtr);
					NativeMethodsForUnsafeMutablePointer.Initialize (actualPtr, valIntPtr, count, StructMarshal.Marshaler.Metatypeof (typeof (T)));
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), valIntPtr);
				}
			}
		}

		public void Deinitialize (nint count)
		{
			unsafe {
				fixed (byte* ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					NativeMethodsForUnsafeMutablePointer.Deinitialize (actualPtr, count, StructMarshal.Marshaler.Metatypeof (typeof (T)));
				}
			}
		}

		public void Deallocate ()
		{
			unsafe {
				fixed (byte* ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					NativeMethodsForUnsafeMutablePointer.Deallocate (actualPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)));
				}
			}
		}

		public T Pointee {
			get {
				unsafe {
					fixed (byte* ptr = SwiftData) {
						var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
						var valPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
						var valIntPtr = new IntPtr (valPtr);
						NativeMethodsForUnsafeMutablePointer.Get(valIntPtr, actualPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)));
						return StructMarshal.Marshaler.ToNet<T> (valIntPtr);
					}
				}
			}
			set {
				unsafe {
					fixed (byte* ptr = SwiftData) {
						var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
						var valPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
						var valIntPtr = new IntPtr (valPtr);
						StructMarshal.Marshaler.ToSwift (value, valIntPtr);
						NativeMethodsForUnsafeMutablePointer.Set (actualPtr, valIntPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)));
						StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), valIntPtr);
					}
				}
			}
		}

		public IntPtr ToIntPtr()
		{
			unsafe {
				fixed (byte* ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					return actualPtr;
				}
			}
		}



		public UnsafeMutablePointer<T> Advance(nint by)
		{
			var retval = new UnsafeMutablePointer<T> ();
			unsafe {
				fixed (byte *ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					fixed (byte* retvalPtr = StructMarshal.Marshaler.PrepareNominal(retval)) {
						NativeMethodsForUnsafeMutablePointer.Advance(new IntPtr(retvalPtr), actualPtr, by, StructMarshal.Marshaler.Metatypeof (typeof (T)));
					}
				}
			}
			return retval;
		}

		internal UnsafeMutablePointer<T> AdvanceNative(nint by)
		{
			return new UnsafeMutablePointer<T> (IntPtr.Add(ToIntPtr (), (int)by * StructMarshal.Marshaler.Strideof (typeof (T))));
		}

		public UnsafeMutablePointer<T> Predecessor()
		{
			return Advance (-1);
		}

		public UnsafeMutablePointer<T> Successor()
		{
			return Advance (1);
		}
	}

	internal class NativeMethodsForUnsafeMutablePointer {

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.NativeMethodsForUnsafeMutablePointer_Metatype)]
		public static extern SwiftMetatype PIMetadataAccessor_UnsafeMutablePointer (SwiftMetatype T);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.NativeMethodsForUnsafeMutablePointer_Initialize)]
		public static extern void Initialize (IntPtr p, IntPtr to, nint count, SwiftMetatype t);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.NativeMethodsForUnsafeMutablePointer_Deinitialize)]
		public static extern void Deinitialize (IntPtr p, nint count, SwiftMetatype t);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.NativeMethodsForUnsafeMutablePointer_Deallocate)]
		public static extern void Deallocate (IntPtr p, SwiftMetatype t);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForUnsafeMutablePointer_Allocate)]
		public static extern void Allocate (IntPtr retval, nint capacity, SwiftMetatype t);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForUnsafeMutablePointer_Get)]
		public static extern void Get (IntPtr retval, IntPtr src, SwiftMetatype t);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForUnsafeMutablePointer_Set)]
		public static extern void Set (IntPtr ptr, IntPtr value, SwiftMetatype t);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForUnsafeMutablePointer_Advance)]
		public static extern void Advance (IntPtr retval, IntPtr p, nint by, SwiftMetatype t);
	}
}
