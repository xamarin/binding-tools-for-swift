// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftReflector {
	public static class Ex {
		public static T ThrowOnNull<T> (T o, string name, string message = null) where T : class
		{
			if (o == null)
				throw new ArgumentNullException (name ?? "<no name supplied>",
					message ?? "");
			return o;
		}
	}
}

