// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Enum, AllowMultiple = false)]
	public sealed class SwiftEnumBackingTypeAttribute : Attribute {
		public SwiftEnumBackingTypeAttribute (Type t)
		{
			if (t == null)
				throw new ArgumentNullException (nameof (t));
			BackingType = t;
		}
		public Type BackingType { get; private set; }
	}

}
