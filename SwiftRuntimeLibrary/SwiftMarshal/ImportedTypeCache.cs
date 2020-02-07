// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal static class ImportedTypeCache {
		static Dictionary<Type, SwiftMetatype?> cache = new Dictionary<Type, SwiftMetatype?> ();
		static object lockObject = new object ();

		public static SwiftMetatype? SwiftMetatypeForType (Type t)
		{
			SwiftMetatype? result = null;

			lock (lockObject) {
				if (cache.TryGetValue (t, out result))
					return result;
#if !TOM_SWIFTY
				SwiftMetatype metatype;
				if (XamGlueMetadata.GetSwiftType (t, out metatype)) {
					result = metatype;
					cache.Add (t, result);
				}
#endif
			}
			return result;
		}

	}
}
