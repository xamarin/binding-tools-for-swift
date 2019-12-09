// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	public struct SwiftProtocolWitnessTable {

		IntPtr handle;
		public SwiftProtocolWitnessTable (IntPtr handle)
		{
			this.handle = handle;
		}

		public IntPtr Handle => handle;

		public SwiftProtocolConformanceDescriptor Conformance {
			get {
				if (handle == IntPtr.Zero)
					throw new InvalidOperationException ();
				return new SwiftProtocolConformanceDescriptor (Marshal.ReadIntPtr (handle));
			}
		}
	}
}
