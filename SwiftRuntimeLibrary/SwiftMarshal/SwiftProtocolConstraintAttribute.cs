// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SwiftProtocolConstraintAttribute : Attribute {
		public SwiftProtocolConstraintAttribute (Type equivalentInterface,
						       string libraryName,
						       string protocolWitnessName)
		{
			EquivalentInterface = equivalentInterface;
			LibraryName = libraryName;
			ProtocolWitnessName = protocolWitnessName;
		}

		public Type EquivalentInterface { get; private set; }
		public string LibraryName { get; private set; }
		public string ProtocolWitnessName { get; set; }
	}
}
