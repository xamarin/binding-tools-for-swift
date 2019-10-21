// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace SwiftReflector.SwiftXmlReflection {
	public class ProtocolDeclaration : ClassDeclaration {
		public ProtocolDeclaration ()
		{
			Kind = TypeKind.Protocol;
		}

		protected override TypeDeclaration UnrootedFactory ()
		{
			return new ProtocolDeclaration ();
		}
	}
}

