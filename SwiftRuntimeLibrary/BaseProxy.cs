// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary {
	public class BaseProxy {
		public BaseProxy (Type interfaceType, EveryProtocol everyProtocol)
		{
			InterfaceType = interfaceType;
			EveryProtocol = everyProtocol;
		}
		// be aware that EveryProtocol can be null in the case of the protocol coming from Swift
		public EveryProtocol EveryProtocol { get; private set; }
		public Type InterfaceType { get; private set; }

		public virtual ISwiftExistentialContainer ProxyExistentialContainer => null;
	}
}
