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
			if (sb.ToString ().Contains ("Optional")) {
				MatchCollection matches = Regex.Matches (sb.ToString (), "Swift.Optional<Swift.*>");
				for (int i = 0; i < matches.Count; i++) {
					var optionalSize = matches [i].Length;
					// remove the last '>'
					sb.Remove (matches [i].Index + matches [i].Length - 1, 1);
					// insert '?' at the end
					sb.Insert (matches [i].Index + matches [i].Length - 1, "?");
					// delete the 'Swift.Optional<Swift."
					sb.Remove (matches [i].Index, 21);
				}

				MatchCollection matches2 = Regex.Matches (sb.ToString (), "Swift.Optional<.*>");
				for (int i = 0; i < matches2.Count; i++) {
					var optionalSize = matches2 [i].Length;
					// remove the last '>'
					sb.Remove (matches2 [i].Index + matches2 [i].Length - 1, 1);
					// insert '?' at the end
					sb.Insert (matches2 [i].Index + matches2 [i].Length - 1, "?");
					// delete the 'Swift.Optional<"
					sb.Remove (matches2 [i].Index, 15);
				}
			}
		}


		// Instead of doing this, remove all "Swift."
		// and look for every '<' and find next '>' and replace with '(' and ')'

		//public static void CorrectUnsafeMutablePointer (this StringBuilder sb)
		//{
		//	if (sb == null)
		//		return;
		//	if (sb.ToString ().Contains ("UnsafeMutablePointer")) {
		//		MatchCollection matches = Regex.Matches (sb.ToString (), "Swift.UnsafeMutablePointer<.*>");
		//		for (int i = 0; i < matches.Count; i++) {
		//			var optionalSize = matches [i].Length;
		//			// remove the last '>'
		//			sb.Remove (matches [i].Index + matches [i].Length - 1, 1);
		//			// insert ')' at the end
		//			sb.Insert (matches [i].Index + matches [i].Length - 1, ')');

		//			sb.Remove (matches [i].Index + 27, 1);
		//			sb.Insert (matches [i].Index + 27, '(');

		//			// delete the "Swift."
		//			sb.Remove (matches [i].Index, 6);
		//		}
		//	}
		//}

		public static void FixBrackets (this StringBuilder sb)
		{
			if (sb == null)
				return;
			if (sb.ToString ().Contains ('<')) {
				var matchesLessThan = Regex.Matches (sb.ToString (), "<");
				for (int i = 0; i < matchesLessThan.Count; i++) {

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
