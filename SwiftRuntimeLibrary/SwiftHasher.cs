using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.Hasher")]
	[SwiftStruct(SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.Hasher_NominalTypeDescriptor, SwiftCoreConstants.Hasher_Metadata, "")]
	public class SwiftHasher : ISwiftStruct {
		public unsafe SwiftHasher ()
		{
			fixed (byte * thisDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
				var thisPtr = new IntPtr (thisDataPtr);
				NativeMethodsForSwiftHasher.PI_hasherNew (thisPtr);
			}
		}

		internal SwiftHasher (SwiftNominalCtorArgument unused)
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForSwiftHasher.PIMetadataAccessor_Hasher (SwiftMetadataRequest.Complete);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		unsafe void Dispose (bool disposing)
		{
			if (SwiftData != null) {
				fixed (byte *p = SwiftData) {
					StructMarshal.Marshaler.ReleaseNominalData (typeof (SwiftHasher), p);
				}
				SwiftData = null;
			}
		}

		~SwiftHasher ()
		{
			Dispose (false);
		}

		public unsafe void Combine<T>(T thing) where T: ISwiftHashable
		{
			fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
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
						var thingExistentialContainer = SwiftObjectRegistry.Registry.ExistentialContainerForProtocol (thing, typeof (ISwiftHashable));
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
			fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
				fixed (byte* bytesSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (bytes)) {
					NativeMethodsForSwiftHasher.PI_hasherCombine ((IntPtr)thisSwiftDataPtr, (IntPtr)bytesSwiftDataPtr);
				}
			}
		}

		public unsafe void Combine (byte[] bytes)
		{
			fixed (byte *bytesPtr = bytes) {
				using (UnsafeRawBufferPointer rawBytes = new UnsafeRawBufferPointer ((IntPtr)bytesPtr, bytes.Length)) {
					Combine (rawBytes);
				}
			}
		}

		public nint FinalizeHasher ()
		{
			unsafe {
				fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (this)) {
					return NativeMethodsForSwiftHasher.PI_hasherFinalize ((IntPtr)thisSwiftDataPtr);
				}
			}
		}

		public byte [] SwiftData {
			get;
			set;
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
