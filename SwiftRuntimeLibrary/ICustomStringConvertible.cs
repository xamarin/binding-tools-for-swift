// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.CustomStringConvertible")]
	[SwiftProtocolType (typeof (CustomStringConvertibleXamProxy), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ICustomStringConvertible_ProtocolDescriptor)]
	public interface ICustomStringConvertible {
		SwiftString Description { get; }
	}

	public class CustomStringConvertibleXamProxy : BaseProxy, ICustomStringConvertible {
		ICustomStringConvertible actualImpl;
		SwiftExistentialContainer1 container;

		static CustomStringConvertible_xam_vtable xamVtableICustomStringConvertible;
		static CustomStringConvertibleXamProxy ()
		{
			XamSetVTable ();
		}

		public CustomStringConvertibleXamProxy (ICustomStringConvertible actualImplementation, EveryProtocol everyProtocol)
			: base (typeof (ICustomStringConvertible), everyProtocol)
		{
			actualImpl = actualImplementation;
			container = new SwiftExistentialContainer1 (everyProtocol, ProtocolWitnessTable);
		}

		public CustomStringConvertibleXamProxy (ISwiftExistentialContainer container)
			: base (typeof (ICustomStringConvertible), null)
		{
			this.container = new SwiftExistentialContainer1 (container);
		}

		public override ISwiftExistentialContainer ProxyExistentialContainer => container;

		[UnmanagedCallersOnly]
		static void xamVtable_recv_get_Description (IntPtr xam_retval, IntPtr self)
		{
			var container = new SwiftExistentialContainer1 (self);
			var proxy = SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<ICustomStringConvertible> (container);
			var retval = proxy.Description;
			StructMarshal.Marshaler.ToSwift (retval, xam_retval);
		}

		static void XamSetVTable ()
		{
			unsafe {
				xamVtableICustomStringConvertible.func0 = &xamVtable_recv_get_Description;
				byte* vtData = stackalloc byte [Marshal.SizeOf (xamVtableICustomStringConvertible)];
				Marshal.WriteIntPtr ((IntPtr)vtData, (IntPtr)xamVtableICustomStringConvertible.func0);
				NativeMethodsForICustomStringConvertible.SwiftXamSetVtable ((IntPtr)vtData);
			}
		}

		public SwiftString Description {
			get {
				if (actualImpl != null)
					return actualImpl.Description;
				unsafe {
					SwiftString retval = new SwiftString (SwiftValueTypeCtorArgument.None);
					fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType (retval)) {
						NativeMethodsForICustomStringConvertible.PIpropg_IConvertiblexamarin_NoneDConvertibleGdescription (new IntPtr (retvalSwiftDataPtr),
						    ref container);
						return retval;
					}
				}
			}
		}

		struct CustomStringConvertible_xam_vtable {
			public unsafe delegate *unmanaged<IntPtr, IntPtr, void> func0;
		}

		static IntPtr protocolWitnessTable;
		public static IntPtr ProtocolWitnessTable {
			get {
				if (protocolWitnessTable == IntPtr.Zero)
					protocolWitnessTable = SwiftCore.ProtocolWitnessTableFromFile (SwiftCore.kXamGlue, XamGlueConstants.ICustomStringConvertible_ConformanceIdentifier,
						EveryProtocol.GetSwiftMetatype());
				return protocolWitnessTable;
			}
		}
	}


	internal class NativeMethodsForICustomStringConvertible {
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.ICustomStringConvertible_NoneDConvertibleGdescription)]
		internal static extern void PIpropg_IConvertiblexamarin_NoneDConvertibleGdescription (IntPtr retval, ref SwiftExistentialContainer1 container);
		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.ICustomStringConvertible_SwiftXamSetVtable)]
		internal static extern void SwiftXamSetVtable (IntPtr vt);
	}
}
