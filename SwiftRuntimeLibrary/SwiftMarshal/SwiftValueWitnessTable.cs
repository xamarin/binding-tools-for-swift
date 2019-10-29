// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal class SwiftValueWitnessTable {
		// as of swift 5, there are the operations on value types, in order
		//
		// initializeBufferWithCopyOfBuffer - T *(*initializeBufferWithCopyOfBuffer)(B *dest, B *src, M *self)
		// destroy - void (*destroy)(T *object, witness_t *self)
		// initializeWithCopy - T *(*initializeWithCopy)(T *dest, T *src, M *self)
		// assignWithCopy - T *(*assignWithCopy)(T *dest, T *src, M *self)
		// initializeWithTake - T *(*initializeWithTake)(T *dest, T *src, M *self)
		// assignWithTake - T *(*assignWithTake)(T *dest, T *src, M *self)
		// getEnumTagSinglePayload - unsigned (*getEnumTagSinglePayload)(const T* enum, UINT_TYPE emptyCases)
		// storeEnumTagSinglePayload - void (*storeEnumTagSinglePayload)(T* enum, UINT_TYPE whichCase, UINT_TYPE emptyCases)

		// Offsets 64 bit / 32 bit
		// Offset 00 00
		public IntPtr InitializeBufferWithCopyOfBufferOffset { get; private set; }
		// Offset 08 04
		public IntPtr DestroyOffset { get; private set; }
		// Offset 10 08
		public IntPtr InitializeWithCopyOffset { get; private set; }
		// Offset 18 0c
		public IntPtr AssignWithCopyOffset { get; private set; }
		// Offset 20 10
		public IntPtr InitializeWithTakeOffset { get; private set; }
		// Offset 28 14
		public IntPtr AssignWithTakeOffset { get; private set; }
		// Offset 30 18
		public IntPtr GetEnumTagSignPayloadOffset { get; private set; }
		// Offset 38 1c
		public IntPtr StoreEnumTageSignlePayloadOffset { get; private set; }

		// Offset 40 20
		public long Size { get; private set; }
		// Offset 48 24
		public long Stride { get; private set; }
		// Offset 50 28
		public uint Flags { get; private set; }
		// Offset 54 2c
		public uint ExtraInhabitantCount { get; private set; }

		public int AlignMask {
			get {
				return (int)(Flags & 0x00ff);
			}
		}
		public int Alignment { get { return AlignMask + 1; } }

		SwiftValueWitnessTable ()
		{
		}

		public static SwiftValueWitnessTable FromType (Type t)
		{
			if (t == null)
				throw new ArgumentNullException (nameof (t));
			// if the struct is generic, we get this dynamically
			// One pointer value behind the metatype is a pointer to the value witness table.
			// This was derived by writing the following swift code:
			// public struct Foo<T> {
			//    private var x = 5
			//	  private var y: T
			//    public init(a:T) { y = a }
			// }
			// public func getSize<T>(a:Foo<T>) -> Int {
			//	  return sizeof(Foo<T>)
			// }
			//
			// which produces the following code:
			// call       __TMaV5None13Foo       ; gets the Metadata
			// mov        rsi, qword [rax-8]     ; get the pointer 1 maching pointer behind the metadata ptr
			// mov        rsi, qword [rsi+0x88]  ; get the 17th pointer into the value witness table which is the size field
			//
			var meta = StructMarshal.Marshaler.Metatypeof (t);
			var backPtr = meta.handle - IntPtr.Size;
			var witPtr = Marshal.ReadIntPtr (backPtr);
#if DEBUG
			//Console.WriteLine ("Value witness table for " + t.Name);
			//Memory.Dump (witPtr, 20 * 8);
#endif
			return FromMemory (witPtr);
		}

		public static SwiftValueWitnessTable FromMemory (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentNullException (nameof (p), "Unable to read SwiftValueWitnessTable");
			int sizeofMachinePointer = IntPtr.Size;
			if (sizeofMachinePointer != 4 && sizeofMachinePointer != 8) {
				throw new SwiftRuntimeException ("Expected a maching pointer size of either 4 or 8, but got " + sizeofMachinePointer);
			}
			var table = new SwiftValueWitnessTable ();
			if (sizeofMachinePointer == 4)
				table.Read32 (p);
			else
				table.Read64 (p);
			return table;
		}

		internal static SwiftValueWitnessTable FromDylib (DynamicLib dylib, string witnessSymbolName)
		{
			var wit = dylib.FindSymbolAddress (witnessSymbolName);
			if (wit == IntPtr.Zero)
				return null;
			return FromMemory (wit);
		}

		internal static SwiftValueWitnessTable FromDylibFile (string path, DLOpenMode mode, string witnessSymbolName)
		{
			using (DynamicLib lib = new DynamicLib (path, mode)) {
				return FromDylib (lib, witnessSymbolName);
			}
		}

		internal static IntPtr ProtocolWitnessTableFromDylib (DynamicLib dylib, string witnessSymbolName)
		{
			var wit = dylib.FindSymbolAddress (witnessSymbolName);
			return wit;
		}

		internal static IntPtr ProtocolWitnessTableFromDylibFile (string path, DLOpenMode mode, string witnessSymbolName)
		{
			using (DynamicLib lib = new DynamicLib (path, mode)) {
				return ProtocolWitnessTableFromDylib (lib, witnessSymbolName);
			}
		}

		void Read32 (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentNullException (nameof (p), "Unable to read SwiftValueWintessTable elements");
			InitializeBufferWithCopyOfBufferOffset = ReadIntPtr (ref p);
			DestroyOffset = ReadIntPtr (ref p);
			InitializeWithCopyOffset = ReadIntPtr (ref p);
			AssignWithCopyOffset = ReadIntPtr (ref p);
			InitializeWithTakeOffset = ReadIntPtr (ref p);
			AssignWithTakeOffset = ReadIntPtr (ref p);
			GetEnumTagSignPayloadOffset = ReadIntPtr (ref p);
			StoreEnumTageSignlePayloadOffset = ReadIntPtr (ref p);

			Size = ReadInt32 (ref p);
			Stride = ReadInt32 (ref p);
			Flags = ReadUInt32 (ref p);
			ExtraInhabitantCount = ReadUInt32 (ref p);
		}

		void Read64 (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentNullException (nameof (p), "Unable to read SwiftValueWintessTable elements");
			InitializeBufferWithCopyOfBufferOffset = ReadIntPtr (ref p);
			DestroyOffset = ReadIntPtr (ref p);
			InitializeWithCopyOffset = ReadIntPtr (ref p);
			AssignWithCopyOffset = ReadIntPtr (ref p);
			InitializeWithTakeOffset = ReadIntPtr (ref p);
			AssignWithTakeOffset = ReadIntPtr (ref p);
			GetEnumTagSignPayloadOffset = ReadIntPtr (ref p);
			StoreEnumTageSignlePayloadOffset = ReadIntPtr (ref p);

			Size = ReadInt64 (ref p);
			Stride = ReadInt64 (ref p);
			Flags = ReadUInt32 (ref p);
			ExtraInhabitantCount = ReadUInt32 (ref p);

#if DEBUG
			//Console.WriteLine ("Read value witness table");
			//Console.WriteLine (InitializeBufferWithCopyOfBufferOffset.ToString ("X8"));
			//Console.WriteLine (DestroyBufferOffset.ToString ("X8"));
			//Console.WriteLine (InitializeWithCopyOffset.ToString ("X8"));
			//Console.WriteLine (AssignWithCopyOffset.ToString ("X8"));
			//Console.WriteLine (InitializeWithTakeOffset.ToString ("X8"));
			//Console.WriteLine (AssignWithTakeOffset.ToString ("X8"));
			//Console.WriteLine (GetEnumTagSignPayloadOffset.ToString ("X8"));
			//Console.WriteLine (StoreEnumTageSignlePayloadOffset.ToString ("X8"));
			//Console.WriteLine ("Size: " + Size);
			//Console.WriteLine ("Flags: " + Flags.ToString ("X8"));
			//Console.WriteLine ("Stride: " + Stride);
#endif
		}

		static IntPtr ReadIntPtr (ref IntPtr memory)
		{
			unsafe {
				void** ptr = (void**)memory;
				void* val = *ptr++;
				memory = (IntPtr)ptr;
				return (IntPtr)val;
			}
		}

		static short ReadInt16 (ref IntPtr memory)
		{
			unsafe {
				short* ptr = (short*)memory;
				short val = *ptr++;
				memory = (IntPtr)ptr;
				return val;
			}
		}
		static ushort ReadUInt16 (ref IntPtr memory)
		{
			unsafe {
				ushort* ptr = (ushort*)memory;
				ushort val = *ptr++;
				memory = (IntPtr)ptr;
				return val;
			}
		}
		static int ReadInt32 (ref IntPtr memory)
		{
			unsafe {
				int* ptr = (int*)memory;
				int val = *ptr++;
				memory = (IntPtr)ptr;
				return val;
			}
		}
		static uint ReadUInt32 (ref IntPtr memory)
		{
			unsafe {
				uint* ptr = (uint*)memory;
				uint val = *ptr++;
				memory = (IntPtr)ptr;
				return val;
			}
		}
		static long ReadInt64 (ref IntPtr memory)
		{
			unsafe {
				long* ptr = (long*)memory;
				long val = *ptr++;
				memory = (IntPtr)ptr;
				return val;
			}
		}
		static ulong ReadUInt64 (ref IntPtr memory)
		{
			unsafe {
				ulong* ptr = (ulong*)memory;
				ulong val = *ptr++;
				memory = (IntPtr)ptr;
				return val;
			}
		}

	}
}

