// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftReflector {
	public class SwiftTypeAttribute {
		public SwiftTypeAttribute (SwiftAttributeType attributeType)
		{
			AttributeType = AttributeType;
		}

		public string AttributeType { get; private set; }
	}
}
