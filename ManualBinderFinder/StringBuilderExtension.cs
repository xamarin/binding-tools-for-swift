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

		public static void CorrectSelf (this StringBuilder sb)
		{
			sb.Replace ("(0,0)A0", "Self");
			sb.Replace ("(0,0)", "Self");
		}
	}
}
