// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using SwiftRuntimeLibrary;

namespace SwiftReflector {
	public class ModuleMapper {
		Dictionary<SwiftName, SwiftName> map = new Dictionary<SwiftName, SwiftName> ();

		public ModuleMapper ()
		{
		}

		public bool IsMapped (SwiftName sn)
		{
			return map.ContainsKey (Exceptions.ThrowOnNull (sn, nameof (sn)));
		}

		public SwiftName Map (SwiftName sn)
		{
			SwiftName mapped = Exceptions.ThrowOnNull (sn, nameof(sn));
			if (map.TryGetValue (sn, out mapped))
				return mapped;
			return sn;
		}

		public void AddMapping (SwiftName key, SwiftName value)
		{
			map [Exceptions.ThrowOnNull (key, nameof(key))] = Exceptions.ThrowOnNull (value, nameof(value));
		}
	}
}

