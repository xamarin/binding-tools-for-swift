using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ManualBinderFinder {
	public class StringBuiderHelper {
		public static string EnhanceMethodSignature (string signature, bool isStatic)
		{
			if (string.IsNullOrEmpty (signature) || signature.Contains ("_"))
				return null;

			StringBuilder sb = new StringBuilder (signature);
			// find the first ':' and delete it
			MatchCollection matches = Regex.Matches (sb.ToString (), ": ");
			sb.Remove (matches [0].Index, matches [0].Length);
			// remove "->()"
			sb.Replace ("->()", "");
			// space out the arguments from the parenthesis so we can find
			// duplicate consecutive words
			sb.Replace ("(", "( ");
			sb.RemoveDuplicateConsecutiveWords ();
			// fix the spacing we added
			sb.Replace ("( ", "(");
			sb.Replace ("->", " -> ");
			sb.CorrectSelf ();
			sb.Insert (0, "func ");
			sb.CorrectOptionals ();
			sb.FixBrackets ();
			//sb.Replace ("Swift.", "");
			if (isStatic)
				sb.Insert (0, "static ");
			return sb.ToString ();
		}

		public static string EnhancePropertySignature (string signature, bool isStatic)
		{
			if (string.IsNullOrEmpty (signature) || signature.Contains ("_"))
				return null;

			StringBuilder sb = new StringBuilder (signature);
			sb.Replace (": ()->", ": ");
			sb.Insert (0, "var ");

			sb.CorrectOptionals ();
			sb.CorrectSelf ();
			sb.FixBrackets ();
			//sb.Replace ("Swift.", "");

			if (isStatic)
				sb.Insert (0, "static ");

			return sb.ToString ();
		}

		public static string [] ParseParameters (string signature)
		{
			if (!signature.Contains("(") || !signature.Contains (")")) {
				return null;
			}
			// find the parameters between the signature's parenthesis
			var matchOpenParenthesis = Regex.Match (signature, @"\(");
			var matchCloseParenthesis = Regex.Match (signature, @"\)");
			var parametersString = signature[(matchOpenParenthesis.Index + 1)..matchCloseParenthesis.Index];

			// split the parameters by commas
			// this is splitting tuples as well
			// can I look for commas not inside brackets? <>
			var parameters = parametersString.Split (", ");
			return parameters;
		}
	}
}
