// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary {
	public class XamTrivialSwiftObject : SwiftNativeObject {
		public XamTrivialSwiftObject ()
			: base (NativeMethodsForXamTrivialSwiftObject.PIctor (NativeMethodsForXamTrivialSwiftObject.PImeta ()),
				NativeMethodsForXamTrivialSwiftObject.PImeta (), SwiftObjectRegistry.Registry)
		{
		}

		XamTrivialSwiftObject (IntPtr p, SwiftObjectRegistry registry)
			: base (p, NativeMethodsForXamTrivialSwiftObject.PImeta (), registry)
		{
		}

		public static object XamarinFactory (IntPtr p)
		{
			return new XamTrivialSwiftObject (p, SwiftObjectRegistry.Registry);
		}
	}

	internal class NativeMethodsForXamTrivialSwiftObject {

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForXamTrivialSwiftObject_PIdtor)]
		internal static extern void PIdtor (IntPtr p);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForXamTrivialSwiftObject_PIctor)]
		internal static extern IntPtr PIctor (SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForXamTrivialSwiftObject_PImeta)]
		internal static extern SwiftMetatype PImeta ();
	}
}
