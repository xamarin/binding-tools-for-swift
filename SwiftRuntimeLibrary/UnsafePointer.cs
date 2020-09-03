// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {

	[SwiftTypeName ("Swift.UnsafePointer")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.UnsafePointer_NominalTypeDescriptor, "", "")]
	public class UnsafePointer<T> : ISwiftStruct {
		internal UnsafePointer (SwiftNominalCtorArgument unused)
		{
		}

		UnsafePointer ()
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}

		UnsafePointer (IntPtr p)
			: this ()
		{
			unsafe {
				fixed (byte* ptr = this.SwiftData) {
					Marshal.WriteIntPtr (new IntPtr (ptr), p);
				}
			}
		}

		public UnsafePointer(UnsafeMutablePointer<T> ptr)
			: this(ptr.ToIntPtr())
		{
		}

		public UnsafePointer(UnsafePointer<T> ptr)
			: this(ptr.ToIntPtr())
		{
		}

		public UnsafePointer(OpaquePointer ptr)
			: this((IntPtr)ptr)
		{
		}

		public byte [] SwiftData { get; set; }

		~UnsafePointer ()
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
			StructMarshal.Marshaler.NominalDestroy (this);
		}


		public IntPtr ToIntPtr ()
		{
			unsafe {
				fixed (byte* ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					return actualPtr;
				}
			}
		}

		// "Why," you may ask, "is it that I'm using the same P/Invokes for UnsafePointer and UnsafeMutablePointer?"
		// Because it's just pointer arithmetic. The difference between them is that UnsafeMutablePointer has set methods.
		// The actual representation is identical: a pointer
		public UnsafePointer<T> Advance (nint by)
		{
			var retval = new UnsafePointer<T> ();
			unsafe {
				fixed (byte* ptr = SwiftData) {
					var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
					fixed (byte* retvalPtr = StructMarshal.Marshaler.PrepareNominal (retval)) {
						NativeMethodsForUnsafeMutablePointer.Advance (new IntPtr (retvalPtr), actualPtr, by, StructMarshal.Marshaler.Metatypeof (typeof (T)));
					}
				}
			}
			return retval;
		}

		internal UnsafePointer<T> AdvanceNative (nint by)
		{
			return new UnsafePointer<T> (IntPtr.Add (ToIntPtr (), (int)by * StructMarshal.Marshaler.Strideof (typeof (T))));
		}

		public UnsafePointer<T> Predecessor ()
		{
			return Advance (-1);
		}

		public UnsafePointer<T> Successor ()
		{
			return Advance (1);
		}

		public T Pointee {
			get {
				unsafe {
					fixed (byte* ptr = SwiftData) {
						var actualPtr = Marshal.ReadIntPtr (new IntPtr (ptr));
						var valPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
						var valIntPtr = new IntPtr (valPtr);
						NativeMethodsForUnsafeMutablePointer.Get (valIntPtr, actualPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)));
						return StructMarshal.Marshaler.ToNet<T> (valIntPtr);
					}
				}
			}
		}
	}
}
