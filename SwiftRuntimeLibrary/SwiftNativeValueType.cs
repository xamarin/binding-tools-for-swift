// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public abstract class SwiftNativeValueType : SwiftNativeInstance, ISwiftNominalType {
		protected SwiftNativeValueType ()
		{
			StructMarshal.Marshaler.PrepareNominal (this);
		}

		bool disposed;
		protected override void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			StructMarshal.Marshaler.NominalDestroy (this);
			SwiftData = null;
		}

		public byte [] SwiftData { get; set; }
	}
}
