// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace SwiftReflector {
	public class ModuleMapper {
		Dictionary<SwiftName, SwiftName> map = new Dictionary<SwiftName, SwiftName> ();

		public ModuleMapper ()
		{
		}

		public bool IsMapped (SwiftName sn)
		{
			return map.ContainsKey (Ex.ThrowOnNull (sn, nameof (sn)));
		}

		public SwiftName Map (SwiftName sn)
		{
			SwiftName mapped = Ex.ThrowOnNull (sn, nameof(sn));
			if (map.TryGetValue (sn, out mapped))
				return mapped;
			return sn;
		}

		public void AddMapping (SwiftName key, SwiftName value)
		{
			map [Ex.ThrowOnNull (key, nameof(key))] = Ex.ThrowOnNull (value, nameof(value));
		}
	}
}

