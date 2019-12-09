// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal struct ResilientWitnessEntry {
		public IntPtr ProtocolRequirement;
		public string WitnessRequirement;
		public static ResilientWitnessEntry FromMemory (IntPtr memory)
		{
			var protoPtrOffset = Marshal.ReadInt32 (memory);
			var isIndirect = (protoPtrOffset & 1) != 0;
			protoPtrOffset &= ~1;
			var protoRequirement = isIndirect ? Marshal.ReadIntPtr (memory + protoPtrOffset) : memory + protoPtrOffset;

			memory = memory + sizeof (int);
			var strPtrOffset = Marshal.ReadInt32 (memory) & ~1;
			var str = FromUTF8 (memory + strPtrOffset);

			return new ResilientWitnessEntry {
				ProtocolRequirement = protoRequirement,
				WitnessRequirement = str
			};
		}
		static string FromUTF8 (IntPtr ptr)
		{
			unsafe {
				var len = 0;
				var bp = (byte *)ptr.ToPointer ();
				while (bp [len] != 0) len++;
				var buffer = new byte [len];
				Marshal.Copy (ptr, buffer, 0, len);
				return Encoding.UTF8.GetString (buffer);
			}
		}
	}

	public struct SwiftProtocolConformanceDescriptor {
		const int kProtocolDescOffset = 0;
		const int kTypeDescOffset = 1;
		const int kWitnessOffset = 2;
		const int kFlagsOffset = 3;
		const int kTrailingItemsOffset = 4;

		readonly IntPtr handle;

		public SwiftProtocolConformanceDescriptor (IntPtr handle)
		{
			this.handle = handle;
		}

		public IntPtr Handle => handle;

		public bool IsValid {
			get {
				return handle != IntPtr.Zero;
			}
		}

		void ThrowOnInvalid ()
		{
			if (!IsValid)
				throw new InvalidOperationException ();
		}

		public SwiftNominalTypeDescriptor ProtocolDescriptor {
			get {
				ThrowOnInvalid ();
				return new SwiftNominalTypeDescriptor (ReadRelativeIndirectPointerOffsetBy (kProtocolDescOffset));
			}
		}

		// the kTypeDescOffset in this type is a union of 4 possible things:
		// a pointer Nominal Type Descriptor of the implementing type
		// an indirect pointer to a Nominal Type Descriptor of the implementing type
		// a pointer to an ObjC class name
		// a pointer to an ObjC class object
		// 

		public SwiftNominalTypeDescriptor ImplementingTypeDescriptor {
			get {
				var kind = MetadataKind;
				var ptr = ReadRelativePointerOffsetBy (kTypeDescOffset);
				if (kind == SwiftProtocolConformanceTypeDescriptorKind.DirectTypeDescriptor) {
					return new SwiftNominalTypeDescriptor (ptr);
				} else if (kind == SwiftProtocolConformanceTypeDescriptorKind.IndirectTypeDescriptor) {
					return new SwiftNominalTypeDescriptor (Marshal.ReadIntPtr (ptr));
				}
				throw new SwiftRuntimeException ($"Expected a protocol descriptor for a swift metadata kind, but was {MetadataKind}");
			}
		}

		IntPtr ObjCClassName {
			get {
				unsafe {
					return ReadRelativePointerOffsetBy (kTypeDescOffset);
				}
			}
		}

		public SwiftMetatype ObjCClass {
			get {
				var kind = MetadataKind;
				var ptr = ReadRelativePointerOffsetBy (kTypeDescOffset);
				if (kind == SwiftProtocolConformanceTypeDescriptorKind.IndirectObjCClass) {
					return new SwiftMetatype (ptr);
				} else if (kind == SwiftProtocolConformanceTypeDescriptorKind.DirectObjCClassName) {
					return new SwiftMetatype (SwiftCore.objc_lookUpClass (ObjCClassName));
				}
				throw new SwiftRuntimeException ($"Expected a protocol descriptor for an ObjC metadata kind, but was {MetadataKind}");
			}
		}

		internal IntPtr WitnessTable {
			get {
				return HandleOffsetBy (kWitnessOffset);
			}
		}

		IntPtr ReadRelativePointerOffsetBy (int offset)
		{
			var targetHandle = HandleOffsetBy (offset);
			var relPointer = Marshal.ReadInt32 (targetHandle);
			return targetHandle + relPointer;
		}

		IntPtr ReadRelativeIndirectPointerOffsetBy (int offset)
		{
			// offset is the number of int32 sized words offset from the handle
			// a relative indirect pointer is either:
			// a relative pointer to a pointer if the low bit is set
			// a relative pointer to an object if the low bit is clear

			var ptrAndFlag = PointerAndFlagOffsetBy (offset);

			return ptrAndFlag.Item2 ? Marshal.ReadIntPtr (ptrAndFlag.Item1) : ptrAndFlag.Item1;
		}

		IntPtr HandleOffsetBy (int offset)
		{
			return handle + (offset * sizeof (int));
		}

		Tuple<IntPtr, bool> PointerAndFlagOffsetBy (int offset)
		{
			var targetHandle = HandleOffsetBy (offset);
			var relPointer = Marshal.ReadInt32 (targetHandle);
			var lowBit = (relPointer & 1) != 0;
			targetHandle = targetHandle + (relPointer & ~1);
			return new Tuple<IntPtr, bool> (targetHandle, lowBit);
		}

		int Flags {
			get {
				return Marshal.ReadInt32 (handle + (kFlagsOffset * sizeof (int)));
			}
		}

		public SwiftProtocolConformanceTypeDescriptorKind MetadataKind {
			get {
				return (SwiftProtocolConformanceTypeDescriptorKind)((Flags >> 3) & 0x7);
			}
		}

		public bool IsRetroactive {
			get {
				return (Flags & (1 << 6)) != 0;
			}
		}

		bool IsSynthesizedNonUnique {
			get {
				return (Flags & (1 << 7)) != 0;
			}
		}

		int ConditionalRequirementCount {
			get {
				return (Flags >> 8) & 0xff;
			}
		}

		bool HasResilientWitnesses {
			get {
				return (Flags & (1 << 16)) != 0;
			}
		}

		bool HasGenericWitnessTable {
			get {
				return (Flags & (1 << 17)) != 0;
			}
		}

		public SwiftNominalTypeDescriptor ContextDescriptor {
			get {
				if (!IsRetroactive)
					throw new NotSupportedException ();
				return new SwiftNominalTypeDescriptor (ReadRelativePointerOffsetBy (kTrailingItemsOffset));
			}
		}

		int GenericRequirementsOffset {
			get {
				return kTrailingItemsOffset + (IsRetroactive ? 1 : 0);
			}
		}

		// note for future coders - a GenericRequirementDescriptor is this:
		// int32 flags
		// int32 relative direct pointer to mangled name of the type
		// union {
		//    relative pointer Type
		//    relative pointer protocol descriptor protocol
		//    relative indirectable pointer conformance
		//    int32 generic layout kind
		// }
		// size - 3 int32s
		const int kGenericRequirementSize = 3;

		int ResilientWitnessHeaderOffset {
			get {
				return GenericRequirementsOffset + kGenericRequirementSize * ConditionalRequirementCount;
			}
		}

		internal int ResilientWitnessCount {
			get {
				if (!HasResilientWitnesses)
					return 0;
				var ptr = HandleOffsetBy (ResilientWitnessHeaderOffset);
				return Marshal.ReadInt32 (ptr);
			}
		}


		internal int ResilientWitnessEntryOffset {
			get {
				return ResilientWitnessHeaderOffset + (HasResilientWitnesses ? 1 : 0);
			}
		}

		internal IntPtr ResilientWitnessPointer (int index)
		{
			return HandleOffsetBy (ResilientWitnessEntryOffset + (index * kResilientWitnessSize));
		}

		// note for future coders - a resilient witness table entry is:
		// int32 indirectable relative pointer to descriptor, low bit is indirectability
		//      descriptor may be an associated type descriptor or a method descriptor or ... ?
		// int32 direct relative pointer to mangled name of which, for some reason, has the low bit set.
		// Weirdness that I see:
		// in a static implementor of IteratorProtocol, this table is orderd as
		// associated type descriptor for Element
		// pointer to Si (Swift.Int) (low bit set)
		// method descriptor for next ()->A.Element?
		// pointer to protocol witness for next()->A.Element?
		//
		// The problem here is that ordering of this table is not yet apparent to me.
		// It could be associated types first then members
		// If it is that, what is the ordering of the associated types and what is the ordering of the members?
		// How do I know how many associated types there are?
		// size - 2 int32s

		const int kResilientWitnessSize = 2;

		internal ResilientWitnessEntry[] GetResilientWitnessEntries ()
		{
			var count = ResilientWitnessCount;
			var entries = new ResilientWitnessEntry [count];
			if (count > 0) {
				var entryPtr = HandleOffsetBy (ResilientWitnessEntryOffset);
				for (int i=0; i < count; i++) {
					entries [i] = ResilientWitnessEntry.FromMemory (entryPtr);
					entryPtr = entryPtr + (kResilientWitnessSize * sizeof (int));
				}
			}
			return entries;
		}

		int GenericWitnessOffset {
			get {
				return ResilientWitnessEntryOffset + (ResilientWitnessCount * kResilientWitnessSize);
			}
		}

		// note for future coders, a generic witness table contains the following:
		// int16 witness table size in words
		// int16 witness table private size in words
		// int32 relative direct pointer to instantiator
		// int32 relative direct pointer to private data
	}
}
