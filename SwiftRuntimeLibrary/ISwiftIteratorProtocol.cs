// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;
#if __IOS__ || __MACOS__ || __TVOS__ || __WATCHOS__
using ObjCRuntime;
#endif
using System.Reflection;

namespace SwiftRuntimeLibrary {
	[SwiftProtocolType (typeof (SwiftIteratorProtocolProtocol<>), "libswiftCore.dylib", "$sStMp", true)]
	[SwiftTypeName ("Swift.IteratorProtocol")]
	public interface ISwiftIteratorProtocol<ATElement> {
		SwiftOptional<ATElement> Next ();
	}
}
