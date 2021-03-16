using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class InnerX {
		// This class will be responsible for creating a dictionary
		// telling us if a TypeDeclaration is on the top level and if not,
		// which TypeDeclaration it is an innerType for

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
			if (nestingNames.Count == 0)
				return;

			var nestingNameString = ConvertNestingNamesToString (nestingNames);

			if (nestingNameString == null)
				return;

			if (nestingNames.Count == 1) {
				AddKeyIfNotPresent (nestingNameString);
				return;
			}

			nestingNames.RemoveAt (nestingNames.Count - 1);
			var parentNestingNameString = ConvertNestingNamesToString (nestingNames);
			AddKeyIfNotPresent (parentNestingNameString);

			var value = InnerXDict [parentNestingNameString];
			value.Add (c);
			InnerXDict [parentNestingNameString] = value;
		}

		void AddKeyIfNotPresent (string key)
		{
			if (InnerXDict.ContainsKey (key))
				return;
			InnerXDict.Add (key, new List<ClassContents> ());
		}

		string ConvertNestingNamesToString (List<SwiftReflector.SwiftName> nestingNames)
		{
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
