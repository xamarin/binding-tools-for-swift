// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class SwiftThrowsAttribute : Attribute {
		public SwiftThrowsAttribute ()
		{
		}
	}
}
