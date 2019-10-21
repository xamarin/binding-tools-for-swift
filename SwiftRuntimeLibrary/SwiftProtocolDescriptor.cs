// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary {
	public struct SwiftProtocolDescriptor {
		internal IntPtr handle;
		public SwiftProtocolDescriptor (IntPtr handle)
		{
			this.handle = handle;
		}

		internal bool IsValid { get { return handle != IntPtr.Zero; } }

		void ThrowOnInvalid ()
		{
			if (!IsValid)
				throw new NotSupportedException ();
		}

		internal string GetMangledName ()
		{
			ThrowOnInvalid ();
			var p = SwiftMetatype.OffsetPtrByPtrSize (handle, 1);
			return Marshal.PtrToStringAnsi (p);
		}

		internal int GetInheritedProtocolCount ()
		{
			ThrowOnInvalid ();
			var p = SwiftMetatype.OffsetPtrByPtrSize (handle, 2);
			p = Marshal.ReadIntPtr (p);
			if (p == IntPtr.Zero)
				return 0;
			return (int)SwiftMetatype.ReadPointerSizedInt (p);
		}

		internal SwiftProtocolDescriptor [] GetInheritedProtocols ()
		{
			var p = SwiftMetatype.OffsetPtrByPtrSize (handle, 2);
			p = Marshal.ReadIntPtr (p);
			if (p == IntPtr.Zero)
				return new SwiftProtocolDescriptor [0];
			int count = (int)SwiftMetatype.ReadPointerSizedInt (p);
			return FromMemory (SwiftMetatype.OffsetPtrByPtrSize (p, 1), count);
		}

		static SwiftProtocolDescriptor [] FromMemory (IntPtr p, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException (nameof (count));
			var descs = new SwiftProtocolDescriptor [count];
			for (int i = 0; i < count; i++) {
				descs [i] = new SwiftProtocolDescriptor (Marshal.ReadIntPtr (p));
				p = SwiftMetatype.OffsetPtrByPtrSize (p, 1);
			}
			return descs;
		}

		int Flags ()
		{
			long l = SwiftMetatype.ReadPointerSizedInt (SwiftMetatype.OffsetPtrByPtrSize (handle, 8));
			return (int)(l >> 32);
		}

		internal bool GetIsSwiftProtocol ()
		{
			ThrowOnInvalid ();
			return (Flags () & (1 << 0)) != 0;
		}

		internal bool GetIsClassConstrained ()
		{
			ThrowOnInvalid ();
			return (Flags () & (1 << 1)) == 0;
		}

		internal bool GetIsWitnessTableDispatch ()
		{
			ThrowOnInvalid ();
			return (Flags () & (1 << 2)) != 0;
		}

	}
}
