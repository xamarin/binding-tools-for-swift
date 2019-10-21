// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
	public sealed class SwiftEnumTypeAttribute : SwiftNominalTypeAttribute {
		public SwiftEnumTypeAttribute (string libraryName, string nominalTypeDescriptor, string metadata, string witnessTable)
			: base (libraryName, nominalTypeDescriptor, metadata, witnessTable)
		{
		}
	}
}
