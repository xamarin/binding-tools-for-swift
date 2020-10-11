// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary {
	public interface ISwiftValueType : IDisposable {
		byte [] SwiftData { get; set; }
	}

	public interface ISwiftEnum : ISwiftValueType {
	}

	public interface ISwiftStruct : ISwiftValueType {
	}

	public enum SwiftValueTypeCtorArgument {
		None = 0
	}
}
