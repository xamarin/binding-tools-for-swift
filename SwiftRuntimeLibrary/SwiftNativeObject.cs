// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public abstract class SwiftNativeObject : SwiftNativeInstance, ISwiftObject {
		IntPtr handle;
		SwiftMetatype class_handle;
		SwiftObjectFlags object_flags = SwiftObjectFlags.IsSwift;


		// this is the one standard constructor for all objects
		// The classHandle is here so that the object matches the structure of the
		// ObjC counterpart.
		// "But why," you ask, "is the registry an argument since it is a singleton?"
		// "Because it's entirely possible to have a constructor in swift that will turn into
		// SomeObject(IntPtr someThing, SwiftMetatype classHandle)
		// but it should be impossible to have one with the signature below since the type
		// SwiftObjectRegistry doesn't exist in swift.
		protected SwiftNativeObject (IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
		{
			if (SwiftNativeObjectTagAttribute.IsSwiftNativeObject (this)) {
				object_flags |= SwiftObjectFlags.IsDirectBinding;
			}
			class_handle = classHandle;
			SwiftObject = handle;
			registry.Add (this);
			SwiftCore.Retain (SwiftObject);
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
