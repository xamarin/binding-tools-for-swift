using System;
using System.Text;
using System.Text.RegularExpressions;
using Dynamo.SwiftLang;

namespace DylibBinder {
	public static class StringBuilderExtension {
		public static void RemoveDuplicateConsecutiveWords (this StringBuilder sb)
		{
			if (sb == null)
				return;

			// space out the words at the beginning of the parameters
			sb.Replace ("(", "( ");
			string pattern = @"\w*:";
			bool isFinished = false;
			while (!isFinished) {
				MatchCollection matches = Regex.Matches (sb.ToString (), pattern);
				if (matches.Count == 0) {
					isFinished = true;
					break;
				}
				for (var i = 0; i < matches.Count; i++) {
					if (i < matches.Count - 1) {
						if (matches [i].Value == matches [i + 1].Value) {
							sb.Remove (matches [i + 1].Index, matches [i + 1].Length + 1);
							break;
						}
					} else {
						isFinished = true;
						break;
					}
				}
			}
			// return that added space
			sb.Replace ("( ", "(");
		}

		public static void CorrectOptionals (this StringBuilder sb)
		{
			if (sb == null)
				return;

			while (sb.ToString ().Contains ("Optional")) {
				var match = Regex.Match (sb.ToString (), @"Swift\.Optional<");
				// look for the respective closing bracket
				var openBracketCount = 0;
				var closingBracketIndex = 0;
				for (int i = match.Index + match.Length; i < sb.ToString ().Length; i++) {
					if (sb.ToString ()[i] == '<') {
						openBracketCount++;
						continue;
					}
					else if (sb.ToString ()[i] == '>') {
						if (openBracketCount == 0) {
							closingBracketIndex = i;
							break;
						}
						openBracketCount--;
					}
				}
				sb.Replace ('>', '?', closingBracketIndex, 1);
				sb.Replace ("Swift.Optional<", "", match.Index, match.Length);
			}
		}

		public static void FixBrackets (this StringBuilder sb)
		{
			if (sb == null)
				return;
			if (sb.ToString ().Contains ("<")) {
				var firstOpenParenthesis = Regex.Match (sb.ToString (), @"\(");
				var matchesLessThan = Regex.Matches (sb.ToString (), "<");
				
				for (int i = 0; i < matchesLessThan.Count; i++) {
					if (matchesLessThan[i].Index < firstOpenParenthesis.Index) {
						continue;
					}
					var nextGreaterThan = Regex.Match (sb.ToString ().Substring(matchesLessThan [i].Index), ">");
					sb.Replace ('<', '(', matchesLessThan [i].Index, 1);
					sb.Replace ('>', ')', matchesLessThan [i].Index + nextGreaterThan.Index, 1);
				}
			}
		}

		public static void AddModule (this StringBuilder sb)
		{
			if (sb == null)
				return;

			sb.AddModuleNameIfNotPresent ("Int");
			sb.AddModuleNameIfNotPresent ("UInt");
			sb.AddModuleNameIfNotPresent ("Bool");
			sb.AddModuleNameIfNotPresent ("Float");
			sb.AddModuleNameIfNotPresent ("Double");
		}

		public static void RemoveAssociatedTypeRemnants (this StringBuilder sb)
		{
			if (sb == null)
				return;
			sb.Replace ("A0", "");
		}

		static void AddModuleNameIfNotPresent (this StringBuilder sb, string type)
		{
			if (sb == null)
				return;

			Match match = Regex.Match (sb.ToString (), @$"(^|\s|\(|\>){type}");
			while (match.Success) {
				sb.Replace (type, $"Swift.{type}", match.Index, match.Length);
				match = Regex.Match (sb.ToString (), @$"(^|\s\|\(|\>){type}");
			}
		}

		public static void TransformEscapeCharacters (this StringBuilder sb)
		{
			if (sb == null)
				return;

			sb.Replace ("&", "&amp;", 0, sb.ToString().Length);
			sb.Replace ("<", "&lt;", 0, sb.ToString ().Length);
			sb.Replace (">", "&gt;", 0, sb.ToString ().Length);
			sb.Replace ("\"", "&quot;", 0, sb.ToString ().Length);
			sb.Replace ("\'", "&apos;", 0, sb.ToString ().Length);
		}

		public static void AddParenthesisToClosure (this StringBuilder sb)
		{
			if (sb == null)
				return;

			MatchCollection arrowMatches = Regex.Matches (sb.ToString (), "->");
			MatchCollection colonMatches = Regex.Matches (sb.ToString (), ": ");

			foreach (Match arrow in arrowMatches) {
				int closestColonPosition = 0;
				foreach (Match colon in colonMatches) {
					if (colon.Index < arrow.Index)
						// adding 2 since the colon and space are 2 characters
						closestColonPosition = colon.Index + 2;
					else
						break;
				}
				sb.Insert (closestColonPosition, "(");
				// minus one because we added a character in the line above
				sb.Insert (arrow.Index + 1, ")");
			}
		}

		public static void TransformGenerics (this StringBuilder sb)
		{
			if (sb == null)
				return;

			sb.Replace ("(0,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (0, 0));
			sb.Replace ("(0,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (0, 1));
			sb.Replace ("(1,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (1, 0));
			sb.Replace ("(1,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (1, 1));
			sb.Replace ("(2,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (2, 0));
			sb.Replace ("(2,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (2, 1));
			sb.Replace ("(3,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (3, 0));
			sb.Replace ("(3,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (3, 1));
			sb.Replace ("(4,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (4, 0));
			sb.Replace ("(4,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (4, 1));
		}

		public static void TransformGenericsToThisLevel (this StringBuilder sb, string parameterString, string returnString, int depth, int genericArguments = 0, int genericParameterCount = 0)
		{
			if (sb == null)
				return;

			if ((parameterString.Contains ("0,") || returnString.Contains ("0,")) && (parameterString.Contains ("1,") || returnString.Contains ("1,"))) {
				sb.Replace ("1,0", $"{depth + 1},0");
				sb.Replace ("1,1", $"{depth + 1},1");
			}

			else if (genericParameterCount > 0 || genericArguments > 0) {
				sb.Replace ("0,0", $"{depth + 1},0");
				sb.Replace ("0,1", $"{depth + 1},1");
			}

		}
	}
}
