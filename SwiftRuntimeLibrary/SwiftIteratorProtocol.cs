// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;
#if !TOM_SWIFTY
using Foundation;
using ObjCRuntime;
#endif

namespace SwiftRuntimeLibrary {
	// this code is preliminary and is likely to change
	// do not depend on this

	[SwiftProtocolType (typeof (SwiftIteratorProtocolProxy<>), SwiftCoreConstants.LibSwiftCore, "$sStMp", true)]
	public interface IIteratorProtocol<T> {
		SwiftOptional<T> Next ();
	}

#if __IOS__
	[MonoNativeFunctionWrapper]
#endif
	delegate void NextFunc (IntPtr returnVal, IntPtr self);

	struct SwiftIteratorProtocolVtable {
		[MarshalAs (UnmanagedType.FunctionPtr)]
		public NextFunc func0;
	}


	public class SwiftIteratorProtocolProxy<T> : ISwiftObject, IIteratorProtocol<T> {
		static SwiftIteratorProtocolProxy ()
		{
			SetVTable ();
		}

		static void SetVTable ()
		{
			var vt = new SwiftIteratorProtocolVtable ();
			vt.func0 = NextFuncReceiver;
			IteratorProtocolPinvokes.SetVtable (ref vt, StructMarshal.Marshaler.Metatypeof (typeof (T)));
		}

		static void NextFuncReceiver (IntPtr returnVal, IntPtr self)
		{
			var instance = SwiftObjectRegistry.Registry.CSObjectForSwiftObject<SwiftIteratorProtocolProxy<T>> (self);
			var result = instance.Next ();
			StructMarshal.Marshaler.ToSwift (result, returnVal);
		}

		IIteratorProtocol<T> proxiedType;

		public SwiftIteratorProtocolProxy (IIteratorProtocol<T> proxiedType)
		{
			this.proxiedType = proxiedType;
			SwiftObject = IteratorProtocolPinvokes.NewIteratorProtocol (StructMarshal.Marshaler.Metatypeof (typeof (T)));
			SwiftCore.Retain (SwiftObject);
			SwiftObjectRegistry.Registry.Add (this);
		}

		SwiftIteratorProtocolProxy (IntPtr ptr)
			: this (ptr, SwiftObjectRegistry.Registry)
		{
		}

		SwiftIteratorProtocolProxy (IntPtr ptr, SwiftObjectRegistry registry)
		{
			SwiftObject = ptr;
			SwiftCore.Retain (ptr);
			registry.Add (this);
		}

		#region IDisposable implementation

		bool disposed = false;
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		~SwiftIteratorProtocolProxy ()
		{
			Dispose (false);
		}

		void Dispose (bool disposing)
		{
			if (!disposed) {
				if (disposing) {
					DisposeManagedResources ();
				}
				DisposeUnmanagedResources ();
				disposed = true;
			}
		}

		void DisposeManagedResources ()
		{
		}

		void DisposeUnmanagedResources ()
		{
			SwiftCore.Release (SwiftObject);
		}
		#endregion

		#region ISwiftObject implementation

		public IntPtr SwiftObject { get; set; }

		public static SwiftIteratorProtocolProxy<T> XamarinFactory (IntPtr p)
		{
			return new SwiftIteratorProtocolProxy<T> (p, SwiftObjectRegistry.Registry);
		}

		#endregion

		public static SwiftMetatype GetSwiftMetatype ()
		{
			return IteratorProtocolPinvokes.IteratorProtocolMetadataAccessor (SwiftMetadataRequest.Complete, StructMarshal.Marshaler.Metatypeof (typeof (T)));
		}

		#region IIteratorProtocol
		public SwiftOptional<T> Next ()
		{
			if (proxiedType == null)
				throw new NotSupportedException ("Non proxied types not supported yet");
			return proxiedType.Next ();
		}
		#endregion
	}

	internal static class IteratorProtocolPinvokes {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue19newIteratorProtocolAA27iteratorprotocol_xam_helperCyxGylF")]
		public static extern IntPtr NewIteratorProtocol (SwiftMetatype metadata);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue20IteratorProtocolNext6retval4thisySpyxSgG_AA27iteratorprotocol_xam_helperCyxGtlF")]
		public static extern void IteratorProtocolNext (IntPtr retval, IntPtr self, SwiftMetatype metadata);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue27iteratorprotocol_xam_helperCMa")]
		public static extern SwiftMetatype IteratorProtocolMetadataAccessor (SwiftMetadataRequest request, SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue31iteratorprotocol_set_xam_vtableyySV_ypXptF")]
		public static extern void SetVtable (ref SwiftIteratorProtocolVtable vtable, SwiftMetatype mt);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = "$s7XamGlue13iterateThings3ret4thisySpySSG_AA27iteratorprotocol_xam_helperCySiGtF")]
		public static extern void IterateThings (IntPtr ret, IntPtr self);
	}

	public class EnumerableIterator<T> : IIteratorProtocol<T> {
		IEnumerator<T> enumerator;
		public EnumerableIterator (IEnumerable<T> enumerable)
		{
			this.enumerator = enumerable.GetEnumerator ();
		}

		public SwiftOptional<T> Next ()
		{
			if (enumerator.MoveNext ()) {
				return SwiftOptional<T>.Some (enumerator.Current);
			} else {
				return SwiftOptional<T>.None ();
			}
		}
	}
}
