// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace SwiftReflector.TypeMapping {
	public class NetParam {
		public NetParam (string name, NetTypeBundle bundle)
		{
			Name = Ex.ThrowOnNull (name, "name");
			Type = bundle;
		}
		public string Name { get; private set; }
		public NetTypeBundle Type { get; private set; }
	}

}

