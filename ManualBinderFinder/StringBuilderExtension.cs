using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ManualBinderFinder {
	public static class StringBuilderExtension {
		public static void RemoveDuplicateConsecutiveWords (this StringBuilder sb)
		{
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
			if (sb.ToString ().Contains ('<')) {
				var firstOpenParenthesis = Regex.Match (sb.ToString (), @"\(");
				var matchesLessThan = Regex.Matches (sb.ToString (), "<");
				
				for (int i = 0; i < matchesLessThan.Count; i++) {
					if (matchesLessThan[i].Index < firstOpenParenthesis.Index) {
						continue;
					}
					var nextGreaterThan = Regex.Match (sb.ToString () [matchesLessThan [i].Index..], ">");
					sb.Replace ('<', '(', matchesLessThan [i].Index, 1);
					sb.Replace ('>', ')', matchesLessThan [i].Index + nextGreaterThan.Index, 1);
				}
			}
		}

		public static void CorrectSelf (this StringBuilder sb)
		{
			sb.Replace ("(0,0)A0", "Self");
			sb.Replace ("(0,0)", "Self");

			// For now, replace "(0,1)", "(1,0)", and "(1,1)"
			sb.Replace ("(1,0)", "???");
			sb.Replace ("(1,1)", "???");
			sb.Replace ("(0,1)", "???");
		}
	}
}
