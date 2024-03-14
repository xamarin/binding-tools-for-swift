using System;
using System.Collections.Generic;
using Dynamo.CSLang;

#nullable enable

namespace SwiftReflector.Naming
{
	public class ScopedNaming
	{
		string autoSymPrefix;
		Stack<HashSet<string>> names = new Stack<HashSet<string>> ();

		public ScopedNaming (string? autoSymPrefix = null)
		{
			this.autoSymPrefix = string.IsNullOrEmpty (autoSymPrefix) ? "_symbol" : autoSymPrefix;
			names.Push (new HashSet<string> ());
		}

		public void EnterScope ()
		{
			names.Push (new HashSet<string> (names.Peek ()));
		}

		public void ExitScope ()
		{
			if (names.Count == 0)
				throw new Exception ("exited top level scope - that's bad");
		}

		HashSet<string> Top => names.Peek ();

		bool Contains (string symbol)
		{
			return Top.Contains (symbol);
		}

		public string GenSym (string prefix)
		{
			if (string.IsNullOrEmpty (prefix))
				throw new ArgumentException ("prefix must not be empty", nameof (prefix));
			lock (names) {
				if (Contains (prefix)) {
					var index = 1;
					while (true) {
						var candidate = $"{prefix}{index}";
						if (!Contains (candidate)) {
							prefix = candidate;
							break;
						}
						index++;
					}
				}
				Top.Add (prefix);
				return prefix;
			}
		}

		public CSIdentifier GenID (string prefix)
		{
			return new CSIdentifier (GenSym (prefix));
		}

		
		public string GenSym ()
		{
			return GenSym (autoSymPrefix);
		}
	}
}

