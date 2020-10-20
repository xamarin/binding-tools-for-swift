// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftNativeObjectTag]
	public class EveryProtocol : SwiftNativeObject {
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
			retvalIntPtr = NativeMethodsForEveryProtocol.PI_MakeEveryProtocol ();
			return retvalIntPtr;

		}

		public EveryProtocol ()
			: base (_XamEveryProtocolCtorImpl (), GetSwiftMetatype (), SwiftObjectRegistry.Registry)
		{
		}

		EveryProtocol (IntPtr handle, SwiftObjectRegistry registry) : base (handle, GetSwiftMetatype (), registry)
		{
		}

		public static EveryProtocol XamarinFactory (IntPtr p)
		{
			return new EveryProtocol (p, SwiftObjectRegistry.Registry);
		}

		~EveryProtocol ()
		{
			Dispose (false);
		}
	}

	internal class NativeMethodsForEveryProtocol {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.EveryProtocol_MetadataAccessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_EveryProtocol (SwiftMetadataRequest request);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.MakeEveryProtocol)]
		internal static extern IntPtr PI_MakeEveryProtocol ();
	}
}
