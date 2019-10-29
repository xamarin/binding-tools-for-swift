// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Dynamo.SwiftLang {
	public class SLSubscript : CodeElementCollection<ICodeElement> {
		// VIS subscript(parms) -> type {
		//    get {
		//    }
		//    set { // newValue:type is implicit
		//    }
		// }
		// get is mandatory, set is optional

		public SLSubscript (Visibility vis, FunctionKind funcKind, SLType type,
		                    SLParameterList parms, SLCodeBlock getter, SLCodeBlock setter)
		{
			Visibility = vis;
			Type = Exceptions.ThrowOnNull (type, nameof(type));
			Parameters = Exceptions.ThrowOnNull (parms, nameof(parms));
			GetterBody = Exceptions.ThrowOnNull (getter, nameof(getter));
			SetterBody = setter; // can be null

			List<string> elems = new List<string> ();
			if (vis != Visibility.None)
				elems.Add (SLFunc.ToVisibilityString (vis) + " ");
			if ((funcKind & FunctionKind.Final) != 0)
				elems.Add ("final ");
			if ((funcKind & FunctionKind.Override) != 0)
				elems.Add ("override ");
			if ((funcKind & FunctionKind.Required) != 0)
				elems.Add ("required ");
			if ((funcKind & FunctionKind.Static) != 0)
				elems.Add ("static ");
			if ((funcKind & FunctionKind.Class) != 0)
				elems.Add ("class ");
			elems.Add ("subscript ");

			AddRange (elems.Select ((el, i) => i == 0 ? (ICodeElement)new SimpleLineElement (el, false, true, true) : new SimpleElememt (el)));
			Add (Parameters);
			Add (SimpleElememt.Spacer);
			Add (new SimpleElememt ("->"));
			Add (SimpleElememt.Spacer);
			Add (Type);
			SLCodeBlock block = new SLCodeBlock (null);
			block.Add (new SimpleElememt ("get"));
			block.Add (SimpleElememt.Spacer);
			block.Add (GetterBody);
			if (SetterBody != null) {
				block.Add (new SimpleElememt ("set"));
				block.Add (SimpleElememt.Spacer);
				block.Add (SetterBody);
			}
			Add (block);
		}

		public Visibility Visibility { get; private set; }
		public SLType Type { get; private set; }
		public SLParameterList Parameters { get; private set; }
		public SLCodeBlock GetterBody { get; private set; }
		public SLCodeBlock SetterBody { get; private set; }
	}
}

