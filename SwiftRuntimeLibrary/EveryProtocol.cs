using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftNativeObject]
	public class EveryProtocol : ISwiftObject {
		protected IntPtr handle;

		protected SwiftMetatype class_handle;

		protected SwiftObjectFlags object_flags = SwiftObjectFlags.IsSwift;

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForEveryProtocol.PIMetadataAccessor_EveryProtocol (SwiftMetadataRequest.Complete);
		}

		static IntPtr _XamEveryProtocolCtorImpl ()
		{
			IntPtr retvalIntPtr;
			retvalIntPtr = NativeMethodsForEveryProtocol.PI_EveryProtocol (EveryProtocol.GetSwiftMetatype ());
			return retvalIntPtr;

		}

		public EveryProtocol ()
			: this (_XamEveryProtocolCtorImpl (), GetSwiftMetatype (), SwiftObjectRegistry.Registry)
		{
		}

		protected EveryProtocol (IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
		{
			if (SwiftNativeObjectAttribute.IsSwiftNativeObject (this)) {
				object_flags |= SwiftObjectFlags.IsDirectBinding;
			}
			class_handle = classHandle;
			SwiftObject = handle;
			registry.Add (this);
		}

		EveryProtocol (IntPtr handle, SwiftObjectRegistry registry) : this (handle, GetSwiftMetatype (), registry)
		{
		}

		public static EveryProtocol XamarinFactory (IntPtr p)
		{
			return new EveryProtocol (p, SwiftObjectRegistry.Registry);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if ((object_flags & SwiftObjectFlags.Disposed) !=
			    SwiftObjectFlags.Disposed) {
				if (disposing) {
					DisposeManagedResources ();
				}
				SwiftObjectRegistry.Registry.RemoveAndWeakRelease (this);
				DisposeUnmanagedResources ();
				object_flags |= SwiftObjectFlags.Disposed;
			}
		}

		protected virtual void DisposeManagedResources ()
		{
		}

		protected virtual void DisposeUnmanagedResources ()
		{
			SwiftCore.Release (SwiftObject);
		}

		~EveryProtocol ()
		{
			Dispose (false);
		}

		public IntPtr SwiftObject {
			get {
				return handle;
			}
			private set {
				handle = value;
			}
		}
	}

	internal class NativeMethodsForEveryProtocol {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.EveryProtocol_MetadataAccessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_EveryProtocol (SwiftMetadataRequest request);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.EveryProtocolNew)]
		internal static extern IntPtr PI_EveryProtocol (SwiftMetatype metaClass);
	}
}