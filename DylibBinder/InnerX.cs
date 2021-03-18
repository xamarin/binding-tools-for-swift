using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class InnerX {
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

			var nestingNameString = c.Name.ToFullyQualifiedName (true);

			if (nestingNameString == null)
				return;

			if (nestingNames.Count == 1) {
				AddKeyIfNotPresent (nestingNameString);
				return;
			}

			var parentNestingNameString = GetParentNameString (nestingNameString);
			if (parentNestingNameString == null)
				return;

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

		string GetParentNameString (string childName)
		{
			MatchCollection matches = Regex.Matches (childName, @"\.");
			if (matches.Count == 0)
				return null;

			return childName.Substring (0, matches [matches.Count - 1].Index);
		}
	}
}
