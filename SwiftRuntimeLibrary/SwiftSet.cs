using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftStruct(SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.SwiftSet_NominalTypeDescriptor, "", "")]
	public class SwiftSet<T> : ISwiftStruct {

		public SwiftSet ()
			: this ((nint)0)
		{
		}

		public SwiftSet (nint capacity)
			: this (SwiftNominalCtorArgument.None)
		{
			unsafe {
				fixed (byte* retvalData = StructMarshal.Marshaler.PrepareNominal (this)) {
					SetPI.NewSet (new IntPtr (retvalData), capacity, StructMarshal.Marshaler.Metatypeof (typeof (T)),
							 StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				}
			}
		}

		internal SwiftSet (SwiftNominalCtorArgument unused)
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}


		public static SwiftMetatype GetSwiftMetatype ()
		{
			return SetPI.PIMetadataAccessor_SwiftSet (SwiftMetadataRequest.Complete, StructMarshal.Marshaler.Metatypeof (typeof (T)),
						       StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
		}

		~SwiftSet ()
		{
			Dispose (false);
		}

		public byte [] SwiftData { get; set; }

		bool disposed = false;
		public void Dispose ()
		{
			if (!disposed) {
				disposed = true;
				Dispose (true);
				GC.SuppressFinalize (this);
			}
		}

		void Dispose (bool disposing)
		{
			StructMarshal.Marshaler.ReleaseNominalData (this);
		}

		public unsafe nint Count {
			get {
				fixed (byte* thisPtr = SwiftData) {
					var thisIntPtr = new IntPtr (thisPtr);
					return SetPI.SetGetCount (thisIntPtr,
					                          StructMarshal.Marshaler.Metatypeof (typeof (T)),
								  StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				}
			}
		}

		public unsafe bool IsEmpty {
			get {
				fixed (byte* thisPtr = SwiftData) {
					var thisIntPtr = new IntPtr (thisPtr);
					return SetPI.SetIsEmpty (thisIntPtr,
					                         StructMarshal.Marshaler.Metatypeof (typeof (T)),
					                         StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				}
			}
		}

		public unsafe nint Capacity {
			get {
				fixed (byte* thisPtr = SwiftData) {
					var thisIntPtr = new IntPtr (thisPtr);
					return SetPI.SetGetCapacity (thisIntPtr,
								  StructMarshal.Marshaler.Metatypeof (typeof (T)),
								  StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				}
			}
		}

		public unsafe bool Contains (T key)
		{
			fixed (byte *thisPtr = SwiftData) {
				var thisIntPtr = new IntPtr (thisPtr);
				byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
				var keyBufferPtr = new IntPtr (keyBuffer);
				StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);
				var val = (nint) 0; 
				val = SetPI.SetContains (thisIntPtr, keyBufferPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)), StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);
				return (val & 1) != 0;
			}
		}

		public unsafe Tuple<bool, T> Insert (T key)
		{
			fixed (byte* thisPtr = SwiftData) {
				var thisIntPtr = new IntPtr (thisPtr);
				byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
				var keyBufferPtr = new IntPtr (keyBuffer);
				StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);
				byte* retBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (Tuple<bool, T>))];
				var retBufferPtr = new IntPtr (retBuffer);
				SetPI.SetInsert (retBufferPtr, thisIntPtr, keyBufferPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)),
				                 StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);
				return StructMarshal.Marshaler.ToNet<Tuple<bool, T>> (retBufferPtr);
			}
		}

		public unsafe SwiftOptional<T> Remove (T key)
		{
			fixed (byte* thisPtr = SwiftData) {
				var thisIntPtr = new IntPtr (thisPtr);
				byte* keyBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (T))];
				var keyBufferPtr = new IntPtr (keyBuffer);
				StructMarshal.Marshaler.ToSwift (typeof (T), key, keyBufferPtr);
				byte* retBuffer = stackalloc byte [StructMarshal.Marshaler.Sizeof (typeof (SwiftOptional<T>))];
				var retBufferPtr = new IntPtr (retBuffer);
				SetPI.SetRemove (retBufferPtr, thisIntPtr, keyBufferPtr, StructMarshal.Marshaler.Metatypeof (typeof (T)),
						 StructMarshal.Marshaler.ProtocolWitnessof (typeof (ISwiftHashable), typeof (T)));
				StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), keyBufferPtr);
				return StructMarshal.Marshaler.ToNet<SwiftOptional<T>> (retBufferPtr);
			}
		}
	}

	internal static class SetPI {
		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.SwiftSet_PIMetadataAccessor)]
		public static extern SwiftMetatype PIMetadataAccessor_SwiftSet (SwiftMetadataRequest request, SwiftMetatype t, IntPtr protocolWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_NewSet)]
		public static extern void NewSet (IntPtr retval, nint capacity, SwiftMetatype t, IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_SetGetCount)]
		public static extern nint SetGetCount (IntPtr self, SwiftMetatype t, IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_SetIsEmpty)]
		public static extern bool SetIsEmpty (IntPtr self, SwiftMetatype t, IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_SetGetCapacity)]
		public static extern nint SetGetCapacity (IntPtr self, SwiftMetatype t, IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_SetContains)]
		public static extern nint SetContains (IntPtr self, IntPtr keyPtr, SwiftMetatype keyType, IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_SetInsert)]
		public static extern void SetInsert (IntPtr retvalPtr, IntPtr self, IntPtr keyPtr, SwiftMetatype keyType, IntPtr protoWitness);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftSet_SetRemove)]
		public static extern void SetRemove (IntPtr retvalPtr, IntPtr self, IntPtr keyPtr, SwiftMetatype keyType, IntPtr protoWitness);
	}
}
