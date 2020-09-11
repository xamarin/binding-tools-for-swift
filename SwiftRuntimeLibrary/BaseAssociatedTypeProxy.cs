// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftNativeObjectTag ()]
	public class BaseAssociatedTypeProxy : SwiftNativeObject {
		protected BaseAssociatedTypeProxy (IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
			: base (handle, classHandle, registry)
		{
			if (IsCSObjectProxy)
				registry.Add (this);
		}

		protected BaseAssociatedTypeProxy (byte [] swiftTypeData, SwiftMetatype mt)
			: base (IntPtr.Zero, mt, SwiftObjectRegistry.Registry)
		{
			if (swiftTypeData == null)
				throw new ArgumentNullException (nameof (swiftTypeData));
			var length = swiftTypeData.Length;
			StructMarshal.Marshaler.NominalInitializeWithCopy (mt, swiftTypeData);
			SwiftData = new byte [length];
			Array.Copy (swiftTypeData, SwiftData, length);
			ProxiedMetatype = mt;
		}

		protected override void DisposeUnmanagedResources ()
		{

			if (IsCSObjectProxy) {
				base.DisposeUnmanagedResources ();
			} else {
				StructMarshal.Marshaler.NominalDestroy (ProxiedMetatype, SwiftData);
			}
		}

		~BaseAssociatedTypeProxy ()
		{
			Dispose (false);
		}

		protected bool IsCSObjectProxy => SwiftObject != IntPtr.Zero;

		protected byte [] SwiftData { get; set; }

		protected SwiftMetatype ProxiedMetatype { get; set; }
	}
}
