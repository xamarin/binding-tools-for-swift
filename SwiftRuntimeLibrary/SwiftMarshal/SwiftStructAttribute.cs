// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public sealed class SwiftStructAttribute : SwiftValueTypeAttribute {

		public SwiftStructAttribute (string libraryName, string nominalTypeDescriptor, string metadata, string witnessTable)
			: base (libraryName, nominalTypeDescriptor, metadata, witnessTable)
		{
		}
	}




}

