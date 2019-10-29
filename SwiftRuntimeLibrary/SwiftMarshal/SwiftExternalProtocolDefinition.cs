// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
	public class SwiftExternalProtocolDefinitionAttribute : Attribute {
		public SwiftExternalProtocolDefinitionAttribute (Type adoptingType, string libraryName, string protocolWitnessName)
		{
			AdoptingType = adoptingType;
			LibraryName = libraryName;
			ProtocolWitnessName = protocolWitnessName;
		}

		public Type AdoptingType { get; private set; }
		public string LibraryName { get; private set; }
		public string ProtocolWitnessName { get; set; }
	}
}
