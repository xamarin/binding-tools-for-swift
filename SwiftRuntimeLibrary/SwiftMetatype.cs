// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public struct SwiftMetatype {
		const long kMaxDiscriminator = 0x7ff; // Swift ABI doc'n
		internal IntPtr handle;
		public MetatypeKind Kind { get { return MetatypeKindFromHandle (handle); } }
		public bool IsValid { get { return handle != IntPtr.Zero; } }

		public SwiftMetatype (IntPtr handle)
		{
			this.handle = handle;
		}

		void ThrowOnInvalid ()
		{
			if (!IsValid)
				throw new NotSupportedException ();
		}

		public IntPtr Handle { get { return handle; }}

		internal static SwiftMetatype? FromDylib (DynamicLib dylib, string metaDescName)
		{
			return FromDylib (dylib, metaDescName, 0);
		}

		internal static SwiftMetatype? FromDylib (DynamicLib dylib, string metaDescName, int offset)
		{
			var meta = dylib.FindSymbolAddress (metaDescName);
			if (meta == IntPtr.Zero) {
				return null;
			}

			return new SwiftMetatype (meta + offset);
		}

		internal static SwiftMetatype? FromDylib (string pathName, DLOpenMode openMode, string metaDescName)
		{
			using (DynamicLib dylib = new DynamicLib (pathName, openMode)) {
				return FromDylib (dylib, metaDescName);
			}
		}

		public SwiftNominalTypeDescriptor GetNominalTypeDescriptor ()
		{
			ThrowOnInvalid ();
			var kind = Kind;
			if (kind == MetatypeKind.Enum || kind == MetatypeKind.Struct)
				return NominalTypeDescriptorFromHandle (handle);
			if (kind == MetatypeKind.Class && IsSwiftClassMetaType ()) {
				var nominalPtr = Marshal.ReadIntPtr (OffsetPtrByPtrSize (handle, IntPtr.Size == 4 ? 11 : 8));
				return NominalTypeDescriptorFromHandle (nominalPtr);
			}
			throw new NotSupportedException ();
		}

		#region Classes
		// Class metadata is TargetClassMetadata
		// A TargetClassMetadata is:
		//	TargetAnyClassMetadata
		//	ClassFlags flags
		//	uint32 InstanceAddressPoint
		//	uint32 InstanceSize
		//	uint16 InstanceAlignMask
		//	uint16 Reserved
		//	uint32 ClassSize
		//	uint32 ClassAddressPoint
		//	Pointer ClassDescriptor description
		//	Pointer IVarDestroyer
		//	Class Members
		// A TargetAnyClassMetadata is:
		//	TargetHeapMetadata
		//	Pointer TargetClassMetadata super
		//	Pointer [2] cache data
		//	StoredSize Data - if low bit is set, it’s a swift class
		// A ClassFlags is a uint32
		// ---------------------------
		// 1 pointer meta class
		// 1 pointer super class
		// 2 pointers cache data
		// 1 pointer data - if low bit is set, it’s a swift class
		// Only value if low bit of previous is set:
		// uint32 flags
		// uint32 InstanceAddressPoint
		// uint32 InstanceSize
		// uint16 InstanceAlignMask
		// uint16 Reserved
		// uint32 ClassSize
		// uint32 ClassAddressPoint
		// Pointer ClassDescriptor description
		// Pointer IVarDestroyer
		// Class Members

		void ThrowOnInvalidOrNotClassType ()
		{
			ThrowOnInvalid ();
			if (Kind != MetatypeKind.Class)
				throw new NotSupportedException ();
		}

		internal SwiftMetatype GetBaseMetatype ()
		{
			ThrowOnInvalidOrNotClassType ();
			return new SwiftMetatype (Marshal.ReadIntPtr (OffsetPtrByPtrSize (handle, 1)));
		}

		internal IntPtr GetIsaPointer ()
		{
			ThrowOnInvalidOrNotClassType ();
			return Marshal.ReadIntPtr (handle);
		}

		internal bool IsSwiftClassMetaType ()
		{
			ThrowOnInvalidOrNotClassType ();
			long rodataPtr = ReadPointerSizedInt (OffsetPtrByPtrSize (handle, 4));
			return (rodataPtr & 1) != 0;
		}

		#endregion

		#region Tuples

		// Derived from Metadata.h
		// Tuple Metadata is TargetTupleTypeMetadata
		// A TargetTupleTypeMetadata is:
		// 	TargetMetadata
		// 	StoredSize NumElements
		// 	TargetPointer Labels
		// 	Block of Element
		// A TargetMetadata is:
		// 	StoredPointer Kind
		// An Element is:
		//	TargetPointer Metadata
		//	StoredSize offset
		// -----------------------
		// 1 pointer - kind/flags
		// 1 machine word - size in elements
		// 1 pointer - labels
		// Data for element 0:
		// 1 pointer - metadata
		// 1 machine word - offset into tuple data for this element

		int GetTupleSize ()
		{
			return (int)ReadPointerSizedInt (OffsetPtrByPtrSize (handle, 1));
		}

		internal SwiftMetatype [] GetTupleMetatypes ()
		{
			if (Kind != MetatypeKind.Tuple)
				throw new NotSupportedException ();
			int size = GetTupleSize ();
			var metatypes = new SwiftMetatype [size];
			for (int i = 0; i < size; i++) {
				metatypes [i] = GetTupleMetatypePtr (i);
			}
			return metatypes;
		}

		internal int [] GetTupleElementOffsets ()
		{
			if (Kind != MetatypeKind.Tuple)
				throw new NotSupportedException ();
			int size = GetTupleSize ();
			var offsets = new int [size];
			for (int i=0; i < size; i++) {
				offsets [i] = GetTupleElementOffset (i);
			}
			return offsets;
		}

		SwiftMetatype GetTupleMetatypePtr (int i)
		{
			IntPtr ptrToPtr = OffsetPtrByPtrSize (handle, 3 + 2 * i);
			return new SwiftMetatype (Marshal.ReadIntPtr (ptrToPtr));
		}

		int GetTupleElementOffset (int i)
		{
			IntPtr ptrToInt = OffsetPtrByPtrSize (handle, 4 + 2 * i);
			return (int)ReadPointerSizedInt (ptrToInt);
		}

		#endregion

		#region ProtocolMetadata
		// for future use
		#endregion


		#region MetatypeMetadata
		// for future use
		#endregion


		#region FunctionMetadata
		// Derived from Metadata.h
		// Function Metadata is a TargetFunctionTypeMetadata
		// A TargetFunctionTypeMetadata is:
		// 	TargetMetadata
		// 	StoredSize Flags
		//	Return Metadata pointer
		// 	Parameters
		//	Parameter Flags
		// flags is a machine word. The bottom 16 bits is the number of parameters.
		// calling convention goes in the 3rd byte. It's one of:
		// 0x000000 - swift
		// 0x010000 - block
		// 0x020000 - thin
		// 0x030000 - c
		// If it throws bit 0x01000000 will be set.
		// If there are parameter flags, bit 0x02000000 will be set
		// If it is escaping, bit 0x04000000 will be set
		// Parameters is a contiguous block of pointers to metadata for each parameter
		// Parameter Flags, if present, is a continguous block of uint32 one for each parameter.
		// In parameter flags, the bottom 8 bits determines the ownership. See SwiftParameterOwnership (1 is inout).
		// The rest determine if the parameter is variadic SwiftParameterFlags.Variadic and/or
		// SwiftParameterFlags.AutoClosure.
		#endregion

		internal static MetatypeKind MetatypeKindFromHandle (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p));
			long val = ReadPointerSizedInt (p);
			if (val == 0)
				return MetatypeKind.None;
			if (val > kMaxDiscriminator)
				return MetatypeKind.Class;
			return (MetatypeKind)val;
		}

		internal static SwiftNominalTypeDescriptor NominalTypeDescriptorFromHandle (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p));
			var ptrToNom = Marshal.ReadIntPtr (OffsetPtrByPtrSize (p, 1));
			return new SwiftNominalTypeDescriptor (ptrToNom);
		}

		internal static long ReadPointerSizedInt (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (p));
			return Marshal.ReadIntPtr (p).ToInt64 ();
		}

		internal static IntPtr OffsetPtrByPtrSize (IntPtr p, int n)
		{
			return p + (n * IntPtr.Size);
		}
	}
}

