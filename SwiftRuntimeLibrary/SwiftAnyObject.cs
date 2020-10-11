// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public sealed class SwiftAnyObject : SwiftNativeObject {
		SwiftAnyObject (IntPtr ptr)
			: base (ptr, GetSwiftMetatype (), SwiftObjectRegistry.Registry)
		{
			SwiftCore.Retain (ptr);
		}

		~SwiftAnyObject ()
		{
			Dispose (false);
		}

		public static SwiftAnyObject XamarinFactory (IntPtr p)
		{
			return new SwiftAnyObject (p);
		}

		public static SwiftAnyObject FromISwiftObject (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			return new SwiftAnyObject (obj.SwiftObject);
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return SwiftCore.AnyObjectMetatype;
		}

		public T CastAs<T> () where T : class, ISwiftObject
		{
			var metaType = StructMarshal.Marshaler.Metatypeof (typeof (T));
			using (var optional = SwiftOptional<T>.None ()) {
				unsafe {
					fixed (byte* dataPtr = StructMarshal.Marshaler.PrepareValueType (optional)) {
						NativeMethodsForSwiftAnyObject.CastAs (new IntPtr (dataPtr), SwiftObject, metaType);
						return optional.HasValue ? optional.Value : default (T);
					}

				}
			}

		}
	}

	internal static class NativeMethodsForSwiftAnyObject {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftAnyObject_CastAs)]
		public static extern void CastAs (IntPtr retval, IntPtr obj, SwiftMetatype meta);
	}
}