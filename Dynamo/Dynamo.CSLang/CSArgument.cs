// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Dynamo.CSLang {
	public class CSArgument : DelegatedSimpleElement {
		public CSArgument (ICSExpression expr)
		{
			Value = Exceptions.ThrowOnNull (expr, nameof(expr));
		}

		public ICSExpression Value { get; private set; }

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			Value.WriteAll (writer);
		}
	}

	public class CSArgumentList : CommaListElementCollection<CSArgument> {

		public void Add (ICSExpression expr)
		{
			Add (new CSArgument (expr));
		}
	}
}

