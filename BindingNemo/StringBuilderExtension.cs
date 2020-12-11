using System;
using System.Text;
using System.Text.RegularExpressions;
using Dynamo.SwiftLang;

namespace BindingNemo {
	public static class StringBuilderExtension {
		public static void RemoveDuplicateConsecutiveWords (this StringBuilder sb)
		{
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
			sb.AddModuleNameIfNotPresent ("Int");
			sb.AddModuleNameIfNotPresent ("UInt");
			sb.AddModuleNameIfNotPresent ("Bool");
			sb.AddModuleNameIfNotPresent ("Float");
			sb.AddModuleNameIfNotPresent ("Double");
		}

		static void AddModuleNameIfNotPresent (this StringBuilder sb, string type)
		{
			Match match = Regex.Match (sb.ToString (), @$"(^|\s|\(|\>){type}");
			while (match.Success) {
				sb.Replace (type, $"Swift.{type}", match.Index, match.Length);
				match = Regex.Match (sb.ToString (), @$"(^|\s\|\(|\>){type}");
			}
		}

		public static void EscapeCharactersName (this StringBuilder sb)
		{
			if (sb == null)
				return;

			sb.Replace ("&", "&amp;", 0, sb.ToString().Length);
			sb.Replace ("<", "&lt;", 0, sb.ToString ().Length);
			sb.Replace (">", "&gt;", 0, sb.ToString ().Length);
			sb.Replace ("\"", "&quot;", 0, sb.ToString ().Length);
			sb.Replace ("\'", "&apos;", 0, sb.ToString ().Length);
		}

		public static void TransformGenerics (this StringBuilder sb)
		{
			sb.Replace ("(0,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (0, 0));
			sb.Replace ("(0,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (0, 1));
			sb.Replace ("(1,0)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (1, 0));
			sb.Replace ("(1,1)", Dynamo.SwiftLang.SLGenericReferenceType.DefaultNamer (1, 1));
		}
	}
}
