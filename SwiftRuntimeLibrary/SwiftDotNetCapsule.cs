using System;
using System.Runtime.InteropServices;
using Xamarin.iOS;

namespace SwiftRuntimeLibrary {
	public sealed class SwiftDotNetCapsule : ISwiftObject {
		static SwiftDotNetCapsule ()
		{
			SetCapsuleOnDeinit (OnDeInit);
		}

		internal class CapsuleTrackArgs : EventArgs {
			public CapsuleTrackArgs (SwiftDotNetCapsule capsule)
			{
				Capsule = capsule;
			}
			public SwiftDotNetCapsule Capsule { get; private set; }
		}

		internal static event EventHandler<EventArgs> DeInitCalled;
		internal static event EventHandler<EventArgs> AllocCalled;

		[MonoPInvokeCallback (typeof (Action<IntPtr>))]
		static void OnDeInit (IntPtr p)
		{
			if (SwiftObjectRegistry.Registry.Contains (p)) {
				var capsule = SwiftObjectRegistry.Registry.CSObjectForSwiftObject<SwiftDotNetCapsule> (p);
				DeInitCalled?.Invoke (null, new CapsuleTrackArgs (capsule));
				SwiftObjectRegistry.Registry.RemoveCapsule (capsule);
				// Can't call Dispose at this point.
				// It would be bad.
				// (specifically, when we get called here, it means that the object is in the process of being de-initialized
				// which means that the retain/release count is inaccessible. Since Dispose() will call ReleaseSwiftObject(),
				// this will crash.
				capsule.disposed = true;
			}
		}

		public SwiftDotNetCapsule (IntPtr p)
		{
			SwiftObject = AllocCapsule (p);
#if DEBUG
			//Console.WriteLine ("Constructing SwiftDotNetCapsule.");
			//Console.WriteLine ("Passed " + p.ToString ("X8"));
			//if (p != IntPtr.Zero) {
			//	SwiftMarshal.Memory.Dump (p, 128);
			//	Console.WriteLine ($"Retain count {SwiftCore.RetainCount (p)}, Weak retain count {SwiftCore.WeakRetainCount (p)}");
			//}
			//Console.WriteLine ("Capsule swift object: " + SwiftObject.ToString ("X8"));
			//Console.WriteLine ($"Retain count {SwiftCore.RetainCount (SwiftObject)}, Weak retain count {SwiftCore.WeakRetainCount (SwiftObject)}");
			//SwiftMarshal.Memory.Dump (SwiftObject, 128);
#endif
			SwiftObjectRegistry.Registry.Add (this);
			AllocCalled?.Invoke (null, new CapsuleTrackArgs (this));
		}

		SwiftDotNetCapsule (IntPtr p, SwiftObjectRegistry registry)
		{
			SwiftObject = p;
#if DEBUG
			//Console.WriteLine ("Constructing SwiftDotNetCapsule.");
			//Console.WriteLine ("Passed " + p.ToString("X8"));
			//SwiftMarshal.Memory.Dump (p, 128);
			//Console.WriteLine ($"Retain count {SwiftCore.RetainCount (p)}, Weak retain count {SwiftCore.WeakRetainCount (p)}");
#endif
			registry.Add (this);
			AllocCalled?.Invoke (null, new CapsuleTrackArgs (this));
		}

		public static SwiftDotNetCapsule XamarinFactory (IntPtr p)
		{
			return new SwiftDotNetCapsule (p, SwiftObjectRegistry.Registry);
		}

		public IntPtr SwiftObject { get; set; }

		bool disposed = false;
		~SwiftDotNetCapsule ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		void Dispose (bool disposing)
		{
			if (!disposed) {
				disposed = true;
				if (disposing) {
					DisposeManagedResources ();
				}
				DisposeUnmanagedResources ();
			}
		}

		void DisposeManagedResources ()
		{
			//            SwiftObjectRegistry.Registry.Remove(this);
		}

		void DisposeUnmanagedResources ()
		{
			SwiftMarshal.StructMarshal.ReleaseSwiftObject (this);
		}

		public IntPtr Data {
			get {
				return GetData (SwiftMarshal.StructMarshal.RetainSwiftObject (this));
			}
			set {
				SetData (SwiftMarshal.StructMarshal.RetainSwiftObject (this), value);
			}
		}

		public bool IsEscaping { get; set; }
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDotNetCapsule_AllocCapsule)]
		static extern IntPtr AllocCapsule (IntPtr p);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDotNetCapsule_GetData)]
		static extern IntPtr GetData (IntPtr inst);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDotNetCapsule_SetData)]
		static extern void SetData (IntPtr inst, IntPtr p);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftDotNetCapsule_SetCapsuleOnDeinit)]
		static extern void SetCapsuleOnDeinit ([MarshalAs (UnmanagedType.FunctionPtr)] Action<IntPtr> callBack);
	}
}
