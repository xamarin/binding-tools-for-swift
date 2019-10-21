using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {

	public class SwiftClassObject {
		[StructLayout (LayoutKind.Sequential, Pack = 2)]
		struct SwiftClassPriv {
			public IntPtr ClassClass;
			public IntPtr SuperClass;
			public IntPtr Cache1, Cache2;
			public IntPtr Data;
			public uint ClassFlags;
			public uint InstanceAddressPoint;
			public uint InstanceSize;
			public ushort InstanceAlignMask;
			public ushort Reserved;
			public uint ClassSize;
			public uint ClassAddressPoint;
			public IntPtr Description;
			public IntPtr IVarDestroyer;

			public bool IsSwiftTypeMetadata {
				get {
					return (Data.ToInt64 () & 1) == 1;
				}
			}
		}

		SwiftClassPriv classObj;
		SwiftClassObject classClass;
		SwiftClassObject superClass;
		IntPtr vtableStart = IntPtr.Zero;

		SwiftClassObject (SwiftClassPriv classPriv, IntPtr classStart)
		{
			classObj = classPriv;
			if (classStart != IntPtr.Zero) {
				vtableStart = classStart + Marshal.SizeOf (typeof (SwiftClassPriv));
			}
		}

		SwiftClassObject (IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ArgumentNullException (nameof (p));
			classObj = (SwiftClassPriv)Marshal.PtrToStructure (p, typeof (SwiftClassPriv));
			vtableStart = p + Marshal.SizeOf (typeof (SwiftClassPriv));
		}

		static SwiftClassPriv GetSwiftClassPriv (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			var classPtr = SwiftCore.GetClassPtr (obj);
			if (classPtr == IntPtr.Zero)
				throw new ArgumentOutOfRangeException (nameof (obj));
			return (SwiftClassPriv)Marshal.PtrToStructure (classPtr, typeof (SwiftClassPriv));
		}

		public static SwiftClassObject FromSwiftObject (ISwiftObject obj)
		{
			SwiftClassPriv classPriv = GetSwiftClassPriv (obj);
			if (!classPriv.IsSwiftTypeMetadata)
				throw new NotSupportedException ("class object is an Objective C class.");
			return new SwiftClassObject (classPriv, SwiftCore.GetClassPtr (obj));
		}

		public static bool IsSwiftClassObject (ISwiftObject obj)
		{
			return GetSwiftClassPriv (obj).IsSwiftTypeMetadata;
		}

		public SwiftClassObject ClassClass {
			get {
				if (classClass == null) {
					if (classObj.ClassClass != IntPtr.Zero)
						classClass = new SwiftClassObject (classObj.ClassClass);
				}
				return classClass;
			}
		}

		public SwiftClassObject SuperClass {
			get {
				if (superClass == null) {
					if (classObj.SuperClass != IntPtr.Zero)
						superClass = new SwiftClassObject (classObj.SuperClass);
				}
				return superClass;
			}
		}

		public bool IsSwift1Class { get { return (classObj.ClassFlags & 1) != 0; } }
		public bool UsesSwift1RefCounting { get { return (classObj.ClassFlags & 2) != 0; } }
		public uint InstanceAddressPoint { get { return classObj.InstanceAddressPoint; } }
		public uint InstanceSize { get { return classObj.InstanceSize; } }
		public ushort InstanceAlignMask { get { return (ushort)classObj.InstanceAlignMask; } }
		public uint ClassSize { get { return classObj.ClassSize; } }
		public bool IsNominalTypeDescriptorValid { get { return classObj.Description != IntPtr.Zero; } }
		public SwiftNominalTypeDescriptor NominalTypeDescriptor { get { return new SwiftNominalTypeDescriptor (classObj.Description); } }
		public IntPtr Vtable { get { return vtableStart; } }
		public int VtableSize {
			get {
				int classHeaderSize = Marshal.SizeOf (typeof (SwiftClassPriv));
				return ((int)ClassSize - classHeaderSize) / IntPtr.Size;
			}
		}
	}

}
