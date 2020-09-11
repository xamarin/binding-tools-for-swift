// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public abstract class SwiftNativeObject : SwiftNativeInstance, ISwiftObject {
		IntPtr handle;
		SwiftMetatype class_handle;
		SwiftObjectFlags object_flags = SwiftObjectFlags.IsSwift;

		protected SwiftNativeObject (IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
		{
			if (SwiftNativeObjectTagAttribute.IsSwiftNativeObject (this)) {
				object_flags |= SwiftObjectFlags.IsDirectBinding;
			}
			class_handle = classHandle;
			SwiftObject = handle;
			registry.Add (this);
		}

		protected override void Dispose (bool disposing)
		{
			if ((object_flags & SwiftObjectFlags.Disposed) != SwiftObjectFlags.Disposed) {
				if (disposing) {
					DisposeManagedResources ();
				}
				DisposeUnmanagedResources ();
				SwiftObjectRegistry.Registry.RemoveAndWeakRelease (this);
				object_flags |= SwiftObjectFlags.Disposed;
			}
		}

		protected virtual void DisposeManagedResources ()
		{
		}

		protected virtual void DisposeUnmanagedResources ()
		{
			SwiftCore.Release (SwiftObject);
			SwiftObject = IntPtr.Zero;
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
}
