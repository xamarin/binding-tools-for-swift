using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class InnerX {
		public InnerX ()
		{
			dict = new Dictionary<string, List<ClassContents>> ();
		}

		Dictionary<string, List<ClassContents>> dict { get; }

		public Dictionary<string, List<ClassContents>> AddClassContentsList (params List<ClassContents>[] contents)
		{
			foreach (var classContentsList in contents) {
				foreach (var classContent in classContentsList) {
					AddItem (classContent);
				}
			}
			return dict;
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

			var value = dict [ParentNestingNameString];
			value.Add (c);
			dict [ParentNestingNameString] = value;
		}

		void AddKeyIfNotPresent (string key)
		{
			if (dict.ContainsKey (key))
				return;
			dict.Add (key, new List<ClassContents> ());
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
