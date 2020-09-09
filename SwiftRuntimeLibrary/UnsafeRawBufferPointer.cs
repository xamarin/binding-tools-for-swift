// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.UnsafeRawBufferPointer")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.UnsafeRawBufferPointer_NominalTypeDescriptor, SwiftCoreConstants.UnsafeRawBufferPointer_Metadata, "")]
	public class UnsafeRawBufferPointer : SwiftNativeValueType, ISwiftStruct, IEnumerable<byte> {

		public unsafe UnsafeRawBufferPointer (IntPtr start, nint count)
			: base ()
		{
			if (start == IntPtr.Zero)
				throw new ArgumentNullException (nameof (start));
			fixed (byte *thisDataPtr = SwiftData) {
				IntPtr thisPtr = new IntPtr (thisDataPtr);
				NativeMethodsForUnsafeRawBufferPointer.PI_UnsafeRawBufferPointer (thisPtr, start, count);
			}
		}

		internal UnsafeRawBufferPointer (SwiftValueTypeCtorArgument unused)
			: base ()
		{
		}

		public unsafe SwiftString DebugDescription {
			get {
				var retval = StructMarshal.DefaultValueType<SwiftString> ();
				fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (retval)) {
					fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
						NativeMethodsForUnsafeRawBufferPointer.PImethod_getDebugDescription ((IntPtr)retvalSwiftDataPtr,
						    (IntPtr)thisSwiftDataPtr);
						return retval;
					}
				}
			}
		}

		public unsafe nint Count {
			get {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					return NativeMethodsForUnsafeRawBufferPointer.PImethod_getCount ((IntPtr)thisSwiftDataPtr);
				}
			}
		}

		public unsafe byte this [int index] {
			get {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					return NativeMethodsForUnsafeRawBufferPointer.PImethod_getAt ((IntPtr)thisSwiftDataPtr, index);
				}
			}
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForUnsafeRawBufferPointer.PIMetadataAccessor_UnsafeRawBufferPointer (SwiftMetadataRequest.Complete);
		}

		public IEnumerator<byte> GetEnumerator ()
		{
			var count = Count;
			for (var i = 0; i < count; i++)
				yield return this [i];
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		~UnsafeRawBufferPointer ()
		{
			Dispose (false);
		}
	}

	internal class NativeMethodsForUnsafeRawBufferPointer {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeRawBufferPointerNew)]
		internal static extern void PI_UnsafeRawBufferPointer (IntPtr retval, IntPtr start, nint count);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint= SwiftCoreConstants.UnsafeRawBufferPointer_MetadataAccessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_UnsafeRawBufferPointer (SwiftMetadataRequest request);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeRawBufferPointerGetDescription)]
		internal static extern void PImethod_getDebugDescription (IntPtr retval, IntPtr this0);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeRawBufferPointerGetCount)]
		internal static extern nint PImethod_getCount (IntPtr this0);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeRawBufferPointerGetAt)]
		internal static extern byte PImethod_getAt (IntPtr this0, nint index);
	}
}
