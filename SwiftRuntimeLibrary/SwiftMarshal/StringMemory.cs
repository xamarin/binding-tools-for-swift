// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
#if DEBUG_SPEW
	public class StringMemory {
		private enum Discriminator {
			ImmortalSmall = 0x0e,
	    		ImmortalSmallASCII = 0x0a,
	    		ImmortalLarge = 0x08,
			Native = 0x00,
	    		SharedBridged = 0x04,
			ForeignBridged = 0x05,
		}
		private struct StringRep {
			public ulong FlagsAndCount;
			public ulong Storage;

			public unsafe StringRep (SwiftString str)
			{
				// in Swift 5, a string in 64 bits is two machine words.
				// the first is a set of flags and a count
				// the second is a pointer, the high nibble of which is a discriminator that
				// determines the storage which may or may not point to the actual string data.
				// In some cases, it is a pointer to a bridge object. In others is a pointer that
				// if offset by a "bias" which is either 32 bytes on a 64 bit sysem or 20 on a 32 bit system.
				// In addition, there is no pointer to string data and instead the raw data is packing into the
				// pointer and count instead.
				// For 32 bit systems, the layout is...weird and I haven't fully unpacked it yet.
				if (IntPtr.Size == 4)
					throw new NotImplementedException ("32 bit swift string support needs to get hashed out.");
				fixed (byte *swiftData = str.SwiftData) {
					ulong* flagsPtr = (ulong*)swiftData;
					FlagsAndCount = *flagsPtr++;
					Storage = *flagsPtr;
				}
			}

			public Discriminator Discriminator {
				get {
					return (Discriminator) (Storage >> 60);
				}
			}

			public IntPtr BridgeObject {
				get {
					switch (Discriminator) {
					case Discriminator.SharedBridged:
					case Discriminator.ImmortalLarge:
						return new IntPtr ((long)Storage & 0xFFF_FFFF_FFFF_FFF);
					default:
						return IntPtr.Zero;
					}
				}
			}

			public bool IsEmpty {
				get {
					return Discriminator == Discriminator.ImmortalSmall &&
						(Storage & 0x0FFF_FFFF_FFFF_FFFF) == 0 && FlagsAndCount == 0;
				}
			}

			public bool IsSmall {
				get {
					return Discriminator == Discriminator.ImmortalSmall || Discriminator == Discriminator.ImmortalSmallASCII;
				}
			}

			public ulong Count {
				get {
					return IsSmall ? (ulong)(FlagsAndCount >> 56) & 0x0F : FlagsAndCount & 0x0000_FFFF_FFFF_FFFF;
				}
			}
		}

		public static void PrintStringInfo (SwiftString str)
		{
			var stringRep = new StringRep (str);
			var nsstringPtr = stringRep.BridgeObject;
			Console.WriteLine ("Swift string: " + str.ToString ());
			Console.Write ($"Type: {stringRep.Discriminator} Ptr: {nsstringPtr.ToString ($"X{IntPtr.Size * 2}")} ");
			if (nsstringPtr != IntPtr.Zero) {
				// Here be dragons. I didn't see a good API to get the reference count for this object.
				// I intuited it by observing bytes in memory change on x64. This is clearly fragile.
				var refPtr = nsstringPtr + (IntPtr.Size + sizeof (int));
				var refCount = Marshal.ReadInt32 (refPtr);
				Console.Write ($"RefCount: {refCount}");
			}
			Console.WriteLine ();
		}
	}
#endif // DEBUG
}
