// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Dynamo.CSLang {
	public class CSDelegateTypeDecl : DelegatedSimpleElement, ICSStatement {
		public CSDelegateTypeDecl (CSVisibility vis, CSType type, CSIdentifier name, CSParameterList parms)
		{
			Visibility = vis;
			Type = type != null ? type : CSSimpleType.Void;
			Name = Exceptions.ThrowOnNull (name, "name");
			Parameters = parms;
		}

		public CSVisibility Visibility { get; private set; }
		public CSType Type { get; private set; }
		public CSIdentifier Name { get; private set; }
		public CSParameterList Parameters { get; private set; }

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			writer.BeginNewLine (true);
			writer.Write (CSMethod.VisibilityToString (Visibility), false);
			writer.Write (" delegate ", true);
			Type.WriteAll (writer);
			writer.Write (' ', true);
			Name.WriteAll (writer);
			writer.Write ('(', true);
			Parameters.WriteAll (writer);
			writer.Write (')', true);
			writer.Write (';', false);
			writer.EndLine ();
		}
	}
}

