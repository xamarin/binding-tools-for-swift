using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary {
	public class XamTrivialSwiftObject : ISwiftObject {
		bool disposed;

		public XamTrivialSwiftObject ()
		{
			SwiftObject = NativeMethodsForXamTrivialSwiftObject.PIctor (NativeMethodsForXamTrivialSwiftObject.PImeta ());
			SwiftCore.Retain (SwiftObject);
			SwiftObjectRegistry.Registry.Add (this);
		}
		XamTrivialSwiftObject (IntPtr p, SwiftObjectRegistry registry)
		{
			SwiftObject = p;
			SwiftCore.Retain (p);
			registry.Add (this);
		}

		public static object XamarinFactory (IntPtr p)
		{
			return new XamTrivialSwiftObject (p, SwiftObjectRegistry.Registry);
		}

		public IntPtr SwiftObject { get; set; }

		~XamTrivialSwiftObject ()
		{
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposed) {
				if (disposing) {
					DisposeManagedResources ();
				}
				DisposeUnmanagedResources ();
				disposed = true;
			}
		}

		protected virtual void DisposeManagedResources ()
		{
		}

		protected virtual void DisposeUnmanagedResources ()
		{
			SwiftCore.Release (SwiftObject);
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
