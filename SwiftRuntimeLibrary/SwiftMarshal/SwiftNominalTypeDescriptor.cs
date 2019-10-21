// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	public struct SwiftNominalTypeDescriptor {
		IntPtr handle;
		public SwiftNominalTypeDescriptor (IntPtr handle)
		{
			this.handle = handle;
		}

		static bool KindIsValid (NominalTypeDescriptorKind kind)
		{
			switch (kind) {
			case NominalTypeDescriptorKind.Class:
			case NominalTypeDescriptorKind.Enum:
			case NominalTypeDescriptorKind.Struct:
				return true;
			default:
				return false;
			}
		}

		public bool IsValid {
			get {
				return handle != IntPtr.Zero;
			}
		}

		public NominalTypeDescriptorKind GetKind ()
		{
			// these are from MetadataValues.h in ContextDescriptorFlags
			ThrowOnInvalid ();
			switch (Marshal.ReadInt32 (handle) & 0x1f) {
			case 0:
				return NominalTypeDescriptorKind.Module;
			case 1:
				return NominalTypeDescriptorKind.Extension;
			case 2:
				return NominalTypeDescriptorKind.Anonymous;
			case 3:
				return NominalTypeDescriptorKind.Protocol;
			case 16:
				return NominalTypeDescriptorKind.Class;
			case 17:
				return NominalTypeDescriptorKind.Struct;
			case 18:
				return NominalTypeDescriptorKind.Enum;
			default:
				throw new NotSupportedException ();
			}
		}

		public bool IsGeneric ()
		{
			// this is from MetadataValues.h in ContextDescriptorFlags
			ThrowOnInvalid ();
			return (Marshal.ReadInt32 (handle) & 0x80) != 0;
		}

		public byte GetVersion ()
		{
			ThrowOnInvalid ();
			return (byte)(Marshal.ReadInt32 (handle) >> 8);
		}

		public bool IsUnique ()
		{
			// this is from MetadataValues.h in ContextDescriptorFlags
			ThrowOnInvalid ();
			return (Marshal.ReadInt32 (handle) & 0x40) != 0;
		}

		void ThrowOnInvalid ()
		{
			if (!IsValid)
				throw new InvalidOperationException ();
		}

		void ThrowOnInvalidOrNoFields ()
		{
			ThrowOnInvalid ();
			if (!(GetKind () == NominalTypeDescriptorKind.Class || GetKind () == NominalTypeDescriptorKind.Struct))
				throw new InvalidOperationException ();
		}

		void ThrowOnInvalidOrNotEnum ()
		{
			ThrowOnInvalid ();
			if (GetKind () != NominalTypeDescriptorKind.Enum)
				throw new InvalidOperationException ();
		}

		public string GetName ()
		{
			ThrowOnInvalid ();
			return ReadRelativeString (handle + 2 * sizeof (int));
		}

		public string GetFullName ()
		{
			ThrowOnInvalid ();
			var buffer = new StringBuilder (GetName ());
			var parent = handle;
			while (true) {
				parent = GetParent (parent);
				if (parent == IntPtr.Zero)
					break;
				buffer.Insert (0, '.');
				buffer.Insert (0, ReadRelativeString (parent + 2 * sizeof (int)));
			}
			return buffer.ToString ();
		}

		static IntPtr GetParent (IntPtr handle)
		{
			var parentPtr = handle + sizeof (int);
			var parentOffset = Marshal.ReadInt32 (parentPtr);
			if (parentOffset == 0)
				return IntPtr.Zero;
			parentPtr += parentOffset;
			return parentPtr;
		}

		static string ReadRelativeString (IntPtr memory)
		{
			var targetPosition = memory;
			int offset = Marshal.ReadInt32 (memory);
			targetPosition += offset;
			var len = 0;
			while (Marshal.ReadByte (targetPosition, len) != 0)
				++len;
			var buffer = new byte [len];
			Marshal.Copy (targetPosition, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString (buffer);
		}

		internal int GetFieldCount ()
		{
			ThrowOnInvalidOrNoFields ();
			var targetPosition = handle + sizeof (int);
			return Marshal.ReadInt32 (targetPosition);
		}

		internal static SwiftNominalTypeDescriptor? FromDylib (DynamicLib dylib, string nomDescSymbolName)
		{
			var nom = dylib.FindSymbolAddress (nomDescSymbolName);
			if (nom == IntPtr.Zero)
				return null;
			return new SwiftNominalTypeDescriptor (nom);
		}

		internal static SwiftNominalTypeDescriptor? FromDylibFile (string pathName, DLOpenMode openMode, string nomDescSymbolName)
		{
			using (DynamicLib dylib = new DynamicLib (pathName, openMode)) {
				return FromDylib (dylib, nomDescSymbolName);
			}
		}


	}

}

