// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftNativeObject ()]
	public class BaseAssociatedTypeProxy : ISwiftObject {
		IntPtr handle;
		SwiftMetatype class_handle;
		SwiftObjectFlags object_flags = SwiftObjectFlags.IsSwift;

		protected BaseAssociatedTypeProxy (IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
		{
			if (SwiftNativeObjectAttribute.IsSwiftNativeObject (this)) {
				object_flags |= SwiftObjectFlags.IsDirectBinding;
			}
			class_handle = classHandle;
			SwiftObject = handle;
			if (IsCSObjectProxy)
				registry.Add (this);
		}

		protected BaseAssociatedTypeProxy (byte [] swiftTypeData, SwiftMetatype mt)
		{
			if (swiftTypeData == null)
				throw new ArgumentNullException (nameof (swiftTypeData));
			var length = swiftTypeData.Length;
			StructMarshal.Marshaler.NominalInitializeWithCopy (mt, swiftTypeData);
			SwiftData = new byte [length];
			Array.Copy (swiftTypeData, SwiftData, length);
			ProxiedMetatype = mt;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if ((object_flags & SwiftObjectFlags.Disposed) != SwiftObjectFlags.Disposed) {
				if (disposing) {
					DisposeManagedResources ();
				}
				if (IsCSObjectProxy)
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
			if (IsCSObjectProxy) {
				SwiftCore.Release (SwiftObject);
			} else {
				StructMarshal.Marshaler.NominalDestroy (ProxiedMetatype, SwiftData);
			}
		}

		~BaseAssociatedTypeProxy ()
		{
			Dispose (false);
		}

		protected bool IsCSObjectProxy => handle != IntPtr.Zero;

		public IntPtr SwiftObject {
			get {
				return handle;
			}
			private set {
				handle = value;
			}
		}

		protected byte [] SwiftData { get; set; }

		protected SwiftMetatype ProxiedMetatype { get; set; }
	}
}
