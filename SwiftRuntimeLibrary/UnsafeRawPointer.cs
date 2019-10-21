// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary {
	public struct UnsafeRawPointer {
		public UnsafeRawPointer (IntPtr p)
		{
			Pointer = p;
		}
		public UnsafeRawPointer (UnsafeRawPointer p)
			: this (p.Pointer)
		{
		}
		public UnsafeRawPointer (UnsafeMutableRawPointer p)
			: this (p.Pointer)
		{
		}
		public IntPtr Pointer;

		public static explicit operator IntPtr (UnsafeRawPointer ptr)
		{
			return ptr.Pointer;
		}

		public static explicit operator UnsafeRawPointer(IntPtr ptr)
		{
			return new UnsafeRawPointer (ptr);
		}
	}

	public struct UnsafeMutableRawPointer {
		public UnsafeMutableRawPointer (IntPtr p)
		{
			Pointer = p;
		}
		public UnsafeMutableRawPointer (UnsafeMutableRawPointer p)
			: this (p.Pointer)
		{
		}
		public UnsafeMutableRawPointer (UnsafeRawPointer p)
			: this (p.Pointer)
		{
		}
		public IntPtr Pointer;

		public static explicit operator IntPtr (UnsafeMutableRawPointer ptr)
		{
			return ptr.Pointer;
		}

		public static explicit operator UnsafeMutableRawPointer (IntPtr ptr)
		{
			return new UnsafeMutableRawPointer (ptr);
		}
	}

//	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.OpaquePointer_NominalTypeDescriptor, SwiftCoreConstants.OpaquePointer_Metadata, "")]
	public struct OpaquePointer {
		public OpaquePointer (IntPtr p)
		{
			Pointer = p;
		}
		public OpaquePointer (UnsafeMutableRawPointer p)
			: this (p.Pointer)
		{
		}
		public OpaquePointer (UnsafeRawPointer p)
			: this (p.Pointer)
		{
		}
		public IntPtr Pointer;

		public static OpaquePointer FromUnsafeMutablePointer<T> (UnsafeMutablePointer<T> ptr)
		{
			return new OpaquePointer (ptr.ToIntPtr ());
		}

		public static OpaquePointer FromUnsafePointer<T> (UnsafePointer<T> ptr)
		{
			return new OpaquePointer (ptr.ToIntPtr ());
		}
		public static explicit operator IntPtr (OpaquePointer ptr)
		{
			return ptr.Pointer;
		}

		public static explicit operator OpaquePointer (IntPtr ptr)
		{
			return new OpaquePointer (ptr);
		}
	}
}


