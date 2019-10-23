// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Dynamo.SwiftLang {
	public class SLLetMatch : SLBaseExpr, ISLLineable {
		public SLLetMatch (SLIdentifier name, ISLExpr expr)
		{
			Name = Exceptions.ThrowOnNull (name, nameof (name));
			Expr = expr;
		}

		public SLIdentifier Name { get; private set; }
		public ISLExpr Expr { get; private set; }

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			writer.Write ("let ", true);
			Name.WriteAll (writer);
			if (Expr != null) {
				SimpleElememt.Spacer.WriteAll (writer);
				Expr.WriteAll (writer);
			}
		}

		public static SLLetMatch LetAs (string name, SLType asType)
		{
			return new SLLetMatch (new SLIdentifier (name), asType != null ? new SLAsExpr (asType) : null);
		}
	}
}
