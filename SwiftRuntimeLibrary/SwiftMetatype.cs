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

		public bool HasNominalDescriptor
		{
			get {
				return Kind == MetatypeKind.Class || Kind == MetatypeKind.Enum ||
					Kind == MetatypeKind.Struct;
			}
		}

		public SwiftNominalTypeDescriptor GetNominalTypeDescriptor ()
		{
			ThrowOnInvalid ();
			var kind = Kind;
			if (kind == MetatypeKind.Enum || kind == MetatypeKind.Struct)
				return NominalTypeDescriptorFromHandle (handle);
			if (kind == MetatypeKind.Class) {
				// see below:
				// The class header is 5 pointers followed by
				// 2 uint32s, 2 unit16s, 2 uint32s (= 6 ints)
				var nominalPtr = handle + (5 * IntPtr.Size + 6 * sizeof (int));
				nominalPtr = Marshal.ReadIntPtr (nominalPtr);
				if (nominalPtr == IntPtr.Zero)
					throw new NotSupportedException ("Class is an artificial class and has no type descriptor");
				return new SwiftNominalTypeDescriptor (nominalPtr);
			}
			throw new NotSupportedException ($"Can't get nominal type descriptor for {kind}");
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

		internal static int ClassSizeWithoutMembers {
			get {
				return 7 * IntPtr.Size + 6 * sizeof (int);
			}
		}

		internal static int ValueBaseSize {
			get {
				return 2 * IntPtr.Size;
			}
		}

		bool IsNominal {
			get {
				ThrowOnInvalid ();
				var kind = Kind;
				return kind == MetatypeKind.Enum || kind == MetatypeKind.Struct ||
					kind == MetatypeKind.Class;
			}
		}

		public bool IsGeneric {
			get {
				ThrowOnInvalid ();
				if (!IsNominal)
					return false;
				var typeDesc = GetNominalTypeDescriptor ();
				return typeDesc.IsGeneric ();
			}
		}

		public int GenericArgumentCount {
			get {
				ThrowOnInvalid ();
				if (!IsNominal)
					return 0;
				var typeDesc = GetNominalTypeDescriptor ();
				if (!typeDesc.IsGeneric ())
					return 0;

				return typeDesc.GetParameterCount ();
			}
		}

		public SwiftMetatype GetGenericMetatype (int index)
		{
			ThrowOnInvalid ();
			if (!IsNominal)
				throw new NotSupportedException ("Generics are only available for nominal types");
			var typeDesc = GetNominalTypeDescriptor ();
			if (index < 0 || index >= typeDesc.GetTotalGenericArgumentCount ())
				throw new ArgumentOutOfRangeException (nameof (index));
			var offsetToGenerics = typeDesc.GetGenericOffset ();
			var genericsPtr = handle + offsetToGenerics + index * IntPtr.Size;
			return new SwiftMetatype (Marshal.ReadIntPtr (genericsPtr));
		}

		#endregion

		#region Structs and Enums
		// Struct metadata is
		// Pointer kind
		// Pointer Description
		#endregion

		#region Optionals
		// optional metadata is
		// Pointer kind
		// Pointer Description
		// Pointer metadata of optionally bound type

		public SwiftMetatype GetOptionalBoundGeneric ()
		{
			ThrowOnInvalid ();
			if (Kind != MetatypeKind.Optional)
				throw new NotSupportedException ($"Type {Kind} is not an optional");
			var pointer = handle + 2 * IntPtr.Size;
			return new SwiftMetatype (Marshal.ReadIntPtr (pointer));
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

		void ThrowOnInvalidOrNotFunction ()
		{
			ThrowOnInvalid ();
			if (Kind != MetatypeKind.Function)
				throw new NotSupportedException ($"Operation not supported on type {Kind}");
		}

		public int GetFunctionParameterCount ()
		{
			ThrowOnInvalidOrNotFunction ();
			var paramCount = Marshal.ReadIntPtr (handle + IntPtr.Size).ToInt64 ();
			return (int)paramCount & 0xffff;
		}

		public SwiftMetatype GetFunctionParameter (int index)
		{
			var count = GetFunctionParameterCount ();
			if (index < 0 || index >= count)
				throw new ArgumentOutOfRangeException (nameof (index));
			var ptr = handle + (3 + index) * IntPtr.Size;
			return new SwiftMetatype (Marshal.ReadIntPtr (ptr));
		}

		public bool FunctionHasParameterFlags ()
		{
			ThrowOnInvalidOrNotFunction ();
			var flags = Marshal.ReadIntPtr (handle + IntPtr.Size).ToInt32 ();
			return (flags & 0x02000000) != 0;
		}

		public bool FunctionHasReturn ()
		{
			ThrowOnInvalidOrNotFunction ();
			var voidReturn = SwiftStandardMetatypes.Void;
			var returnType = Marshal.ReadIntPtr (handle + 2 * IntPtr.Size);
			return returnType != voidReturn.Handle;
		}

		public SwiftMetatype GetFunctionReturnType ()
		{
			ThrowOnInvalidOrNotFunction ();
			var returnType = Marshal.ReadIntPtr (handle + 2 * IntPtr.Size);
			if (returnType == IntPtr.Zero)
				throw new NotSupportedException ("Function has no return type");
			return new SwiftMetatype (returnType);
		}

		public SwiftCallingConvention GetFunctionCallingConvention ()
		{
			ThrowOnInvalidOrNotFunction ();
			var flags = Marshal.ReadIntPtr (handle + IntPtr.Size).ToInt32 ();
			return (SwiftCallingConvention)((flags >> 24) & 0xff);
		}
		#endregion

		#region ProtocolMetadata
		// Pointer kind
		// uint32 flags (high 8 bits are flags, low 24 bits is the number of witness tables)
		// uint32 number of protocol descriptors
		// [Pointer] super class constraint if and only if there is a superclass constraint
		// n Pointers - protocol descriptors for each protocol - low bits set if ObjC

		void ThrowOnInvalidOrNotProtocol ()
		{
			ThrowOnInvalid ();
			if (Kind != MetatypeKind.Protocol)
				throw new NotSupportedException ($"Operation not support on type {Kind}");
		}

		internal SwiftProtocolMetadataFlags GetProtocolMetadataFlags ()
		{
			ThrowOnInvalidOrNotProtocol ();
			var flags = Marshal.ReadInt32 (handle + IntPtr.Size);
			return (SwiftProtocolMetadataFlags)(flags >> 30);
		}

		internal SwiftSpecialProtocol GetProtocolSpecialProtocol ()
		{
			ThrowOnInvalidOrNotProtocol ();
			var flags = Marshal.ReadInt32 (handle + IntPtr.Size);
			return (SwiftSpecialProtocol)((flags >> 24) & 0x3f);
		}

		internal int GetProtocolWitnessTableCount ()
		{
			ThrowOnInvalidOrNotProtocol ();
			var flags = Marshal.ReadInt32 (handle + IntPtr.Size);
			return flags & 0x00ffffff;
		}

		internal int GetProtocolDescriptorCount ()
		{
			ThrowOnInvalidOrNotProtocol ();
			var count = Marshal.ReadInt32 (handle + IntPtr.Size + sizeof (int));
			return count;
		}

		internal bool HasExistentialSuperclassConstraint ()
		{
			return (GetProtocolMetadataFlags () & SwiftProtocolMetadataFlags.HasSuperClassConstraint) != 0;
		}

		internal int GetProtocolsSuperclassOffset ()
		{
			return IntPtr.Size + 2 * sizeof (int);
		}

		internal SwiftMetatype GetProtocolSuperclassConstraint ()
		{
			if (!HasExistentialSuperclassConstraint ())
				throw new NotSupportedException ("Existential metadata doesn't have a superclass constraint.");
			var ptr = Marshal.ReadIntPtr (handle + GetProtocolsSuperclassOffset ());
			return new SwiftMetatype (ptr);
		}

		internal int GetProtocolDescriptorOffset ()
		{
			return GetProtocolsSuperclassOffset () + (HasExistentialSuperclassConstraint () ? IntPtr.Size : 0);
		}

		internal SwiftNominalTypeDescriptor GetProtocolDescriptor (int index)
		{
			if (index < 0 || index >= GetProtocolDescriptorCount ())
				throw new ArgumentOutOfRangeException (nameof (index));
			var offset = GetProtocolDescriptorOffset () + (index * IntPtr.Size);
			// doc'n says low bit is set if it's ObjC
			var ptrVal = Marshal.ReadIntPtr (handle + offset).ToInt64 ();
			return new SwiftNominalTypeDescriptor (new IntPtr (ptrVal));
		}

		internal bool ProtocolDescriptorIsObjC (int index)
		{
			if (index < 0 || index >= GetProtocolDescriptorCount ())
				throw new ArgumentOutOfRangeException (nameof (index));
			var offset = GetProtocolDescriptorOffset () + (index * IntPtr.Size);
			// doc'n says low bit is set if it's ObjC
			var ptrVal = Marshal.ReadIntPtr (handle + offset).ToInt64 ();
			return (ptrVal & 1) != 0;
		}

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

