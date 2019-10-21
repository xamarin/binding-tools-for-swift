using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;
using Xamarin.iOS;

namespace SwiftRuntimeLibrary {
	public class SwiftHashableProxy : BaseProxy, ISwiftHashable {
		ISwiftHashable actualImpl;

		public SwiftHashableProxy (SwiftHashableProxy actualImplementation, EveryProtocol everyProtocol)
			: base (typeof (SwiftHashableProxy), everyProtocol)
		{
			actualImpl = actualImplementation;
		}

		public SwiftHashableProxy (ISwiftExistentialContainer container)
			: base (typeof (SwiftHashableProxy), null)
		{
			throw new NotImplementedException ("SwiftComparableProxy should never get constructed from an existential container.");
		}

		struct Hashable_xam_vtable {
			public delegate nint Delfunc0 (IntPtr self);
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public Delfunc0 func0;
		}

		static Hashable_xam_vtable vtableIHashable;
		static SwiftHashableProxy ()
		{
			XamSetVTable ();
		}

#if __IOS__
		[MonoPInvokeCallback(typeof(Hashable_xam_vtable.Delfunc0))]
#endif
		static nint HashFunc (IntPtr selfPtr)
		{
			var one = SwiftObjectRegistry.Registry.ProxyForEveryProtocolHandle<ISwiftHashable> (selfPtr);
			return one.HashValue;
		}

		static void XamSetVTable ()
		{
			vtableIHashable.func0 = HashFunc;
			PISetVtable (ref vtableIHashable);
		}

		public nint HashValue {
			get {
				
				var retval = PIHashvalue (StructMarshal.RetainSwiftObject (this.EveryProtocol));
				StructMarshal.ReleaseSwiftObject (this.EveryProtocol);
				return retval;
			}
		}

		public bool OpEquals (ISwiftEquatable other)
		{
			if (this == other)
				return true;
			var otherProxy = other as SwiftHashableProxy;
			if (otherProxy != null) {
				return actualImpl.OpEquals (otherProxy.actualImpl);
			}
			var otherEqProxy = other as SwiftEquatableProxy;
			if (otherEqProxy != null) {
				// why switch the order? Because otherEqProxy will go through its actualImpl.
				return otherEqProxy.OpEquals (actualImpl);
			}
			return actualImpl.OpEquals (other);
		}

		static IntPtr protocolWitnessTable;
		public static IntPtr ProtocolWitnessTable {
			get {
				if (protocolWitnessTable == IntPtr.Zero)
					protocolWitnessTable = SwiftCore.ProtocolWitnessTableFromFile (SwiftCore.kXamGlue, "$s7XamGlue13EveryProtocolCSHAAMc",
						EveryProtocol.GetSwiftMetatype());
				return protocolWitnessTable;
			}
		}

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftHashableProxy_PIHashvalue)]
		static extern nint PIHashvalue (IntPtr obj);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftHashableProxy_PISetVtable)]
		static extern void PISetVtable (ref Hashable_xam_vtable vt);
	}
}
