// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftRuntimeLibrary {
	public class SwiftRuntimeException : Exception {
		public SwiftRuntimeException (string message)
			: base (message)
		{
		}
	}
}

