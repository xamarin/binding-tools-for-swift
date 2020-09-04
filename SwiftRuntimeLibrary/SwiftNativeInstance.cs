// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftRuntimeLibrary {
	public abstract class SwiftNativeInstance : IDisposable {
		~SwiftNativeInstance ()
		{
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected abstract void Dispose (bool disposing);
	}
}
