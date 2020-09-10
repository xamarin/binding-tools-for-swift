// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.Hasher")]
	[SwiftStruct(SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.Hasher_NominalTypeDescriptor, SwiftCoreConstants.Hasher_Metadata, "")]
	public class SwiftHasher : SwiftNativeValueType, ISwiftStruct {
		public unsafe SwiftHasher ()
		{
			fixed (byte * thisDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
				var thisPtr = new IntPtr (thisDataPtr);
				NativeMethodsForSwiftHasher.PI_hasherNew (thisPtr);
			}
		}

		internal SwiftHasher (SwiftValueTypeCtorArgument unused)
			: base ()
		{
			StructMarshal.Marshaler.PrepareValueType (this);
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForSwiftHasher.PIMetadataAccessor_Hasher (SwiftMetadataRequest.Complete);
		}

		~SwiftHasher ()
		{
			Dispose (false);
		}

		public unsafe void Combine<T> (T thing) where T : ISwiftHashable
		{
			fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
				IntPtr thingIntPtr;
				ISwiftObject thingProxy = null;
				var thingIsSwiftable = StructMarshal.Marshaler.IsSwiftRepresentable (typeof (T));

				if (thingIsSwiftable) {
					byte* thingPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
					thingIntPtr = new IntPtr (thingPtr);
					StructMarshal.Marshaler.ToSwift (thing, thingIntPtr);
				} else {
					if (SwiftProtocolTypeAttribute.IsAssociatedTypeProxy (typeof (ISwiftHashable))) {
						byte* thingPtr0 = stackalloc byte [IntPtr.Size];
						thingIntPtr = new IntPtr (thingPtr0);
						Marshal.WriteIntPtr (thingIntPtr, thingProxy.SwiftObject);
					} else {
						var thingExistentialContainer = SwiftObjectRegistry.Registry.ExistentialContainerForProtocols (thing, typeof (ISwiftHashable));
						byte* thingProtoPtr = stackalloc byte [thingExistentialContainer.SizeOf];
						thingIntPtr = new IntPtr (thingProtoPtr);
						thingExistentialContainer.CopyTo (thingIntPtr);
					}
				}
				NativeMethodsForSwiftHasher.PI_hasherCombine ((IntPtr)thisSwiftDataPtr, thingIntPtr,
					StructMarshal.Marshaler.Metatypeof (typeof (T), new Type [] { typeof (ISwiftHashable) }),
		    			StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				if (thingIsSwiftable) {
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), thingIntPtr);
				} else {
					if (thingProxy != null) {
						StructMarshal.ReleaseSwiftObject (thingProxy);
					}
				}
			}
		}

		public unsafe void Combine (UnsafeRawBufferPointer bytes)
		{
			fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
				fixed (byte* bytesSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (bytes)) {
					NativeMethodsForSwiftHasher.PI_hasherCombine ((IntPtr)thisSwiftDataPtr, (IntPtr)bytesSwiftDataPtr);
				}
			}
		}

		public unsafe void Combine (byte [] bytes)
		{
			fixed (byte* bytesPtr = bytes) {
				using (UnsafeRawBufferPointer rawBytes = new UnsafeRawBufferPointer ((IntPtr)bytesPtr, bytes.Length)) {
					Combine (rawBytes);
				}
			}
		}

		public nint FinalizeHasher ()
		{
			unsafe {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					return NativeMethodsForSwiftHasher.PI_hasherFinalize ((IntPtr)thisSwiftDataPtr);
				}
			}
		}
	}

	internal class NativeMethodsForSwiftHasher {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.HasherNew)]
		internal static extern void PI_hasherNew (IntPtr retval);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.Hasher_MetadataAccessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_Hasher (SwiftMetadataRequest request);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.HasherCombine0)]
		internal static extern void PI_hasherCombine (IntPtr this0, IntPtr thing, SwiftMetatype mt, IntPtr ct);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.HasherCombine1)]
		internal static extern void PI_hasherCombine (IntPtr this0, IntPtr bytes);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.HasherFinalize)]
		internal static extern nint PI_hasherFinalize (IntPtr this0);
	}
}
