using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftStruct(SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.UnsafeRawBufferPointer_NominalTypeDescriptor, SwiftCoreConstants.UnsafeRawBufferPointer_Metadata, "")]
	public class UnsafeRawBufferPointer : ISwiftStruct, IEnumerable<byte> {

		public unsafe UnsafeRawBufferPointer (IntPtr start, nint count)
		{
			if (start == IntPtr.Zero)
				throw new ArgumentNullException (nameof (start));
			fixed (byte *thisDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
				IntPtr thisPtr = new IntPtr (thisDataPtr);
				NativeMethodsForUnsafeRawBufferPointer.PI_UnsafeRawBufferPointer (thisPtr, start, count);
			}
		}

		internal UnsafeRawBufferPointer (SwiftNominalCtorArgument unused)
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}

		public unsafe SwiftString DebugDescription {
			get {
				var retval = StructMarshal.DefaultNominal<SwiftString> ();
				fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (retval)) {
					fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
						NativeMethodsForUnsafeRawBufferPointer.PImethod_getDebugDescription ((IntPtr)retvalSwiftDataPtr,
						    (IntPtr)thisSwiftDataPtr);
						return retval;
					}
				}
			}
		}

		public unsafe nint Count {
			get {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
					return NativeMethodsForUnsafeRawBufferPointer.PImethod_getCount ((IntPtr)thisSwiftDataPtr);
				}
			}
		}

		public unsafe byte this [int index] {
			get {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
					return NativeMethodsForUnsafeRawBufferPointer.PImethod_getAt ((IntPtr)thisSwiftDataPtr, index);
				}
			}
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForUnsafeRawBufferPointer.PIMetadataAccessor_UnsafeRawBufferPointer (SwiftMetadataRequest.Complete);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		void Dispose (bool disposing)
		{
			if (SwiftData != null) {
				unsafe {
					fixed (byte* p = SwiftData) {
						StructMarshal.Marshaler.ReleaseNominalData (typeof (UnsafeRawBufferPointer), p);
					}
					SwiftData = null;
				}
			}
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

		public byte [] SwiftData { get; set; }
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
