// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Dynamo.SwiftLang {
	public class SLLine : DelegatedSimpleElement, ISLStatement {
		public SLLine (ISLExpr contents)
		{
			Contents = Exceptions.ThrowOnNull (contents, nameof(contents));
			if (!(contents is ISLLineable))
				throw new ArgumentException ("contents must be ILineable", nameof (contents));
		}

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			writer.BeginNewLine (true);
			Contents.WriteAll (writer);
			writer.Write (';', false);
			writer.EndLine ();
		}

		public ISLExpr Contents { get; private set; }
	}
}

