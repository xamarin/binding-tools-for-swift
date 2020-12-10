using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BindingNemo {
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
			if (sb.ToString ().Contains ("Swift.Optional<(Self.Element")) {
				Console.Write ("");
			}

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
					//var nextGreaterThan = Regex.Match (sb.ToString ()., ">");
					sb.Replace ('<', '(', matchesLessThan [i].Index, 1);
					sb.Replace ('>', ')', matchesLessThan [i].Index + nextGreaterThan.Index, 1);
				}
			}
		}

		public static void CorrectSelf (this StringBuilder sb)
		{
			sb.Replace ("(0,0)A0", "Swift.Self");
			sb.Replace ("(0,0)", "Swift.Self");

			// For now, replace "(0,1)", "(1,0)", and "(1,1)"
			//sb.Replace ("(1,0)", "???");
			//sb.Replace ("(1,1)", "???");
			//sb.Replace ("(0,1)", "???");
			sb.Replace ("(1,0)", "Swift.OneZero");
			sb.Replace ("(1,1)", "Swift.OneOne");
			sb.Replace ("(0,1)", "Swift.ZeroOne");
		}

		public static void AddModule (this StringBuilder sb)
		{
			// Just realized that 'Int' 'Uint' 'Bool' are acceptable
			//// replace "Int" with Swift.Int without replacing "UInt" or "Swift.Int"
			//Match IntMatch = Regex.Match (sb.ToString (), @"(^|\s|\()Int");
			//while (IntMatch.Success) {
			//	sb.Replace ("Int", "Swift.Int", IntMatch.Index, IntMatch.Length);
			//	IntMatch = Regex.Match (sb.ToString (), @"(^|\s\|\()Int");
			//}

			//// replace "UInt" with Swift.UInt without replacing "Swift.UInt"
			//Match UIntMatch = Regex.Match (sb.ToString (), @"(^|\s|\()UInt");
			//while (UIntMatch.Success) {
			//	sb.Replace ("UInt", "Swift.UInt", UIntMatch.Index, UIntMatch.Length);
			//	UIntMatch = Regex.Match (sb.ToString (), @"(^|\s\|\()UInt");
			//}
			//sb.Replace (" Bool", " Swift.Bool");
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
	}
}
