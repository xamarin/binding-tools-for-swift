using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public sealed class SwiftAnyObject : ISwiftObject {
		SwiftAnyObject(IntPtr ptr)
			: this (ptr, SwiftObjectRegistry.Registry)
		{
		}

		SwiftAnyObject (IntPtr ptr, SwiftObjectRegistry registry)
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

		~SwiftAnyObject ()
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

		public static SwiftAnyObject XamarinFactory (IntPtr p)
		{
			return new SwiftAnyObject (p, SwiftObjectRegistry.Registry);
		}

		#endregion

		public static SwiftAnyObject FromISwiftObject(ISwiftObject obj)
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
			using (var optional = SwiftOptional<T>.None()) {
				unsafe {
					fixed (byte* dataPtr = StructMarshal.Marshaler.PrepareNominal (optional)) {
						NativeMethodsForSwiftAnyObject.CastAs (new IntPtr (dataPtr), SwiftObject, metaType);
						return optional.HasValue ? optional.Value : default (T);
					}

				}
			}

		}
	}

	internal static class NativeMethodsForSwiftAnyObject {
		[DllImport(SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftAnyObject_CastAs)]
		public static extern void CastAs (IntPtr retval, IntPtr obj, SwiftMetatype meta);
	}
}

