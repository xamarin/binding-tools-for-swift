using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class InnerX {
		public InnerX ()
		{
		}

		public Dictionary<string, List<ClassContents>> InnerXDict { get; } = new Dictionary<string, List<ClassContents>> ();

		public Dictionary<string, List<ClassContents>> AddClassContentsList (params List<ClassContents>[] contents)
		{
			foreach (var classContentsList in contents) {
				foreach (var classContent in classContentsList) {
					AddItem (classContent);
				}
			}
			return InnerXDict;
		}

		void AddItem (ClassContents c)
		{
			var nestingNames = c.Name.NestingNames.ToList ();
			var nestingNameString = ConvertNestingNamesToString (nestingNames);

			if (nestingNameString == null)
				return;

			if (nestingNames.Count == 1) {
				AddKeyIfNotPresent (nestingNameString);
				return;
			}

			var parentNestingNames = nestingNames;
			parentNestingNames.RemoveAt (parentNestingNames.Count - 1);
			var ParentNestingNameString = ConvertNestingNamesToString (parentNestingNames);
			AddKeyIfNotPresent (ParentNestingNameString);

			var value = InnerXDict [ParentNestingNameString];
			value.Add (c);
			InnerXDict [ParentNestingNameString] = value;
		}

		void AddKeyIfNotPresent (string key)
		{
			if (InnerXDict.ContainsKey (key))
				return;
			InnerXDict.Add (key, new List<ClassContents> ());
		}

		string ConvertNestingNamesToString (List<SwiftReflector.SwiftName> nestingNames)
		{
			if (nestingNames == null || nestingNames.Count == 0)
				return null;

			var sb = new StringBuilder ();
			foreach (var name in nestingNames) {
				if (sb.Length == 0)
					sb.Append ($"Swift.{name.ToString ()}");
				else
					sb.Append ($".{name.ToString ()}");
			}
			return sb.ToString ();
		}
	}
}
