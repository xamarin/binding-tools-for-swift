// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {

	public enum SwiftOptionalCases {
		None,
		Some,
	}

	[SwiftTypeName ("Swift.Optional")]
	[SwiftEnumType (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.SwiftOptional_NominalTypeDescriptor, "", "")]
	public class SwiftOptional<T> : SwiftNativeValueType, ISwiftEnum {
		internal SwiftOptional (SwiftValueTypeCtorArgument unused)
			: base ()
		{
		}
		public SwiftOptional ()
			: base ()
		{
			unsafe {
				fixed (byte* p = SwiftData) {
					NativeMethodsForSwiftOptional.NewNone (new IntPtr (p),
									       StructMarshal.Marshaler.Metatypeof (typeof (T)));
				}
			}
		}

		public SwiftOptional (T value)
		{
			var valueType = typeof (T);
			if (valueType.IsClass && EqualityComparer<T>.Default.Equals (value, default (T)))
				throw new ArgumentNullException (nameof (value), $"SwiftOptional<{typeof (T).Name}> constructor requires a non-null object instance.");
			unsafe {
				fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
					var valPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
					var valIntPtr = new IntPtr (valPtr);
					valIntPtr = StructMarshal.Marshaler.ToSwift (value, valIntPtr);
					NativeMethodsForSwiftOptional.NewSome (new IntPtr (retvalSwiftDataPtr),
									      valIntPtr,
									      StructMarshal.Marshaler.Metatypeof (typeof (T)));
					StructMarshal.Marshaler.ReleaseSwiftPointer (typeof (T), valIntPtr);
				}
			}
		}

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return NativeMethodsForSwiftOptional.PIMetadataAccessor_SwiftOptional (SwiftMetadataRequest.Complete,
				 StructMarshal.Marshaler.Metatypeof (typeof (T)));
		}

		~SwiftOptional ()
		{
			Dispose (false);
		}

		public static SwiftOptional<T> None ()
		{
			return new SwiftOptional<T> ();
		}

		public static SwiftOptional<T> Some (T val)
		{
			return new SwiftOptional<T> (val);
		}

		public SwiftOptionalCases Case {
			get {
				unsafe {
					fixed (byte* thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
						return (SwiftOptionalCases)(int)NativeMethodsForSwiftOptional.Case (new IntPtr (thisSwiftDataPtr),
														    StructMarshal.Marshaler.Metatypeof (typeof (T)));
					}
				}
			}
		}

		public bool HasValue {
			get {
				return Case == SwiftOptionalCases.Some;
			}
		}

		public T Value {
			get {
				if (!HasValue)
					throw new InvalidOperationException ($"SwiftOptional {(typeof (T)).Name} has no value.");
				unsafe {
					byte* valPtr = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (T))];
					var valIntPtr = new IntPtr (valPtr);
					fixed (byte* optPtr = StructMarshal.Marshaler.PrepareValueType (this)) {
						NativeMethodsForSwiftOptional.Value (new IntPtr (optPtr),
										     valIntPtr,
										     StructMarshal.Marshaler.Metatypeof (typeof (T)));
					}
					return StructMarshal.Marshaler.ToNet<T> (valIntPtr);
				}
			}
		}

		public override string ToString ()
		{
			return HasValue ? Value.ToString () : "";
		}

	}


	internal class NativeMethodsForSwiftOptional {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForSwiftOptional_NewNone)]
		internal static extern void NewNone (IntPtr p, SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForSwiftOptional_NewSome)]
		internal static extern void NewSome (IntPtr retval, IntPtr valPtr, SwiftMetatype mt);

		[DllImport (SwiftCoreConstants.LibSwiftCore, EntryPoint = SwiftCoreConstants.SwiftOptional_PIMetadataAccessor)]
		internal static extern SwiftMetatype PIMetadataAccessor_SwiftOptional (SwiftMetadataRequest request, SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForSwiftOptional_HasValue)]
		internal static extern bool HasValue (IntPtr val, SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForSwiftOptional_Case)]
		internal static extern nint Case (IntPtr val, SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.NativeMethodsForSwiftOptional_Value)]
		internal static extern void Value (IntPtr optval, IntPtr val, SwiftMetatype mt);
	}
}

