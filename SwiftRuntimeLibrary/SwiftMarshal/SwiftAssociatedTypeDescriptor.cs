using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal enum ProtocolRequirementsKind {
		BaseProtocol,
		Method,
		Init,
		Getter,
		Setter,
		ReadCoroutine,
		ModifyCoroutine,
		AssociatedTypeAccessFunction,
		AssociatedConformanceAccessFunction,
	}

	internal struct SwiftAssociatedTypeDescriptor {
		IntPtr handle;
		public SwiftAssociatedTypeDescriptor (IntPtr handle)
		{
			this.handle = handle;
		}
		public IntPtr Handle => handle;

		public ProtocolRequirementsKind Kind {
			get {
				return (ProtocolRequirementsKind)(Marshal.ReadInt32 (handle) & 0xf);
			}
		}

		public bool IsValid {
			get {
				return handle != IntPtr.Zero;
			}
		}

		public bool IsInstance {
			get {
				return (Marshal.ReadInt32 (handle) & 0x10) != 0;
			}
		}

		public IntPtr DefaultImplementation {
			get {
				var relPtr = handle + sizeof (int);
				var ptrVal = Marshal.ReadInt32 (relPtr);
				if (ptrVal == 0)
					return IntPtr.Zero;
				return relPtr + ptrVal;
			}
		}
	}
}
