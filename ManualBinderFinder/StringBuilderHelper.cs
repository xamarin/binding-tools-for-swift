using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ManualBinderFinder {
	public class StringBuiderHelper {
		public static string EnhanceSignature (string signature)
		{
			if (string.IsNullOrEmpty (signature))
				return string.Empty;

			StringBuilder sb = new StringBuilder (signature);
			MatchCollection matches = Regex.Matches (sb.ToString (), ": ");
			sb.Remove (matches [0].Index, matches [0].Length);
			sb.Replace ("->()", "");
			sb.Replace ("(", "( ");
			sb.RemoveDuplicateConsecutiveWords ();
			sb.Replace ("( ", "(");
			sb.Replace ("->", " -> ");
			sb.Replace ("(0,0)A0", "Self");
			sb.Replace ("(0,0)", "Self");
			sb.Insert (0, "func ");
			return sb.ToString ();
		}
	}
}
