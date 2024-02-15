// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public class SwiftComparableProxy : BaseProxy, ISwiftComparable {
		ISwiftComparable actualImpl;

		public SwiftComparableProxy (ISwiftComparable actualImplementation, EveryProtocol everyProtocol)
			: base (typeof (ISwiftComparable), everyProtocol)
		{
			actualImpl = actualImplementation;
		}

		public SwiftComparableProxy (ISwiftExistentialContainer container)
			: base (typeof (ISwiftComparable), null)
		{
			throw new NotImplementedException ("SwiftComparableProxy should never get constructed from an existential container.");
		}

		struct Comparable_xam_vtable {
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public unsafe delegate* unmanaged<IntPtr, IntPtr, int> opEqualFunc;
			[MarshalAs (UnmanagedType.FunctionPtr)]
			public unsafe delegate* unmanaged<IntPtr, IntPtr, int> opLessFunc;
		}

		static Comparable_xam_vtable vtableIComparable;
		static SwiftComparableProxy ()
		{
			XamSetVTable ();
		}

		[UnmanagedCallersOnly]
		static int EqFunc (IntPtr oneptr, IntPtr twoptr)
		{
			if (oneptr == twoptr)
				return 1;
			var one = SwiftObjectRegistry.Registry.ProxyForEveryProtocolHandle<ISwiftComparable> (oneptr);
			var two = SwiftObjectRegistry.Registry.ProxyForEveryProtocolHandle<ISwiftComparable> (twoptr);
			return one.OpEquals (two) ? 1 : 0;
		}

		[UnmanagedCallersOnly]
		static int LessFunc (IntPtr oneptr, IntPtr twoptr)
		{
			if (oneptr == twoptr)
				return 0;
			var one = SwiftObjectRegistry.Registry.ProxyForEveryProtocolHandle<ISwiftComparable> (oneptr);
			var two = SwiftObjectRegistry.Registry.ProxyForEveryProtocolHandle<ISwiftComparable> (twoptr);
			return one.OpLess (two) ? 1 : 0;
		}

		static void XamSetVTable ()
		{
			unsafe {
				vtableIComparable.opEqualFunc = &EqFunc;
				vtableIComparable.opLessFunc = &LessFunc;
				PISetVtable (ref vtableIComparable);
			}
		}

		public bool OpEquals (ISwiftEquatable other)
		{
			if (this == other)
				return true;
			var otherProxy = other as SwiftComparableProxy;
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

		public bool OpLess (ISwiftComparable other)
		{
			if (this == other)
				return false;
			var otherProxy = other as SwiftComparableProxy;
			if (otherProxy != null) {
				return actualImpl.OpLess (otherProxy.actualImpl);
			}
			return actualImpl.OpLess (other);
		}


		static IntPtr protocolWitnessTable;
		public static IntPtr ProtocolWitnessTable {
			get {
				if (protocolWitnessTable == IntPtr.Zero)
					protocolWitnessTable = SwiftCore.ProtocolWitnessTableFromFile (SwiftCore.kXamGlue, "$s7XamGlue13EveryProtocolCSLAAMc",
						EveryProtocol.GetSwiftMetatype ());
				return protocolWitnessTable;
			}
		}

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftComparableProxy_PISetVtable)]
		static extern void PISetVtable (ref Comparable_xam_vtable vt);
	}
}
