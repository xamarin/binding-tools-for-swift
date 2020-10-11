// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.UnsafeMutableRawBufferPointer")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.UnsafeMutableRawBufferPointer_NominalTypeDescriptor, SwiftCoreConstants.UnsafeMutableRawBufferPointer_Metadata, "")]
	public class UnsafeMutableRawBufferPointer : SwiftNativeValueType, ISwiftStruct, IEnumerable<byte> {

		public unsafe UnsafeMutableRawBufferPointer (IntPtr start, nint count)
		{
			if (start == IntPtr.Zero)
				throw new ArgumentNullException (nameof (start));
			fixed (byte* thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
				IntPtr thisPtr = new IntPtr (thisDataPtr);
				NativeMethodsForUnsafeMutableRawBufferPointer.PI_UnsafeMutableRawBufferPointer (thisPtr, start, count);
			}
		}

		internal UnsafeMutableRawBufferPointer (SwiftValueTypeCtorArgument unused)
			: base ()
		{
		}

		public unsafe SwiftString DebugDescription {
			get {
				var retval = StructMarshal.DefaultValueType<SwiftString> ();
				fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (retval)) {
					fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
						NativeMethodsForUnsafeMutableRawBufferPointer.PImethod_getDebugDescription ((IntPtr)retvalSwiftDataPtr,
						    (IntPtr)thisSwiftDataPtr);
						return retval;
					}
				}
			}
		}

		public unsafe nint Count {
			get {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					return NativeMethodsForUnsafeMutableRawBufferPointer.PImethod_getCount ((IntPtr)thisSwiftDataPtr);
				}
			}
		}

		public unsafe byte this [int index] {
			get {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					return NativeMethodsForUnsafeMutableRawBufferPointer.PImethod_getAt ((IntPtr)thisSwiftDataPtr, index);
				}
			}
			set {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					NativeMethodsForUnsafeMutableRawBufferPointer.PImethod_setAt ((IntPtr)thisSwiftDataPtr, index, value);
				}
			}
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForUnsafeMutableRawBufferPointer.PIMetadataAccessor_UnsafeMutableRawBufferPointer (SwiftMetadataRequest.Complete);
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

		~UnsafeMutableRawBufferPointer ()
		{
			Dispose (false);
		}
	}

	internal class NativeMethodsForUnsafeMutableRawBufferPointer {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeMutableRawBufferPointerNew)]
		internal static extern void PI_UnsafeMutableRawBufferPointer (IntPtr retval, IntPtr start, nint count);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.UnsafeMutableRawBufferPointer_MetadataAccessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_UnsafeMutableRawBufferPointer (SwiftMetadataRequest request);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeMutableRawBufferPointerGetDescription)]
		internal static extern void PImethod_getDebugDescription (IntPtr retval, IntPtr this0);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeMutableRawBufferPointerGetCount)]
		internal static extern nint PImethod_getCount (IntPtr this0);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeMutableRawBufferPointerGetAt)]
		internal static extern byte PImethod_getAt (IntPtr this0, nint index);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.UnsafeMutableRawBufferPointerSetAt)]
		internal static extern byte PImethod_setAt (IntPtr this0, nint index, byte value);
	}
}
