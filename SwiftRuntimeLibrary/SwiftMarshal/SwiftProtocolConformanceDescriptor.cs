// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
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

		public SwiftNominalTypeDescriptor ImplementingTypeDescriptor {
			get {
				return new SwiftNominalTypeDescriptor (HandleOffsetBy (kTypeDescOffset));
			}
		}

		internal IntPtr WitnessTable {
			get {
				return HandleOffsetBy (kWitnessOffset);
			}
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

		public bool IsSwift {
			get {
				return (Flags & (1 << 0)) != 0;
			}
		}

		public bool HasClassConstraint {
			get {
				return (Flags & (1 << 1)) != 0;
			}
		}

		public ProtocolDispatchStrategy DispatchStrategy {
			get {
				return (ProtocolDispatchStrategy)((Flags >> 2) & 0xf);
			}
		}

		public SwiftSpecialProtocol SpecialProtocol {
			get {
				return (SwiftSpecialProtocol)((Flags >> 6) & 0xf);
			}
		}

		public bool IsResilient {
			get {
				return ((Flags >> 10) & 1) != 0;
			}
		}
	}
}
