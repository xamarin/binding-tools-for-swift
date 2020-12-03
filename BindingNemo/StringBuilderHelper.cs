using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BindingNemo {
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

		public static List<string> ParseParameters (string signature)
		{
			if (!signature.Contains ("(") || !signature.Contains (")")) {
				return null;
			}

			// look for the closing parenthesis for the first opening parenthesis
			var matchOpenParenthesis = Regex.Matches (signature, @"\(");
			var matchCloseParenthesis = Regex.Matches (signature, @"\)");
			if (matchCloseParenthesis.Count == 0)
				return null;

			var selectedClose = 0;
			if (matchOpenParenthesis.Count > 1) {
				for (int i = 1; i < matchOpenParenthesis.Count; i++) {
					if (matchOpenParenthesis [i].Index < matchCloseParenthesis [selectedClose].Index) {
						selectedClose++;
					}
				}
			}
			string parametersString = signature.Substring((matchOpenParenthesis [0].Index + 1),matchCloseParenthesis [selectedClose].Index - (matchOpenParenthesis [0].Index + 1));
			if (parametersString == "") {
				return null;
			}

			List<string> parameters = new List<string> ();
			StringBuilder parameter = new StringBuilder ();
			int openedCount = 0;
			for (int i = 0; i < parametersString.Length; i++) {
				switch (parametersString [i]) {
				case '(':
					parameter.Append (parametersString [i]);
					openedCount++;
					break;
				case ')':
					parameter.Append (parametersString [i]);
					openedCount--;
					break;
				case ',':
					if (openedCount == 0) {
						parameters.Add (parameter.ToString ());
						parameter.Clear ();
						i++;
					}
					break;
				default:
					parameter.Append (parametersString [i]);
					break;
				}
			}
			parameters.Add (parameter.ToString ());

			return parameters;
		}
	}
}
