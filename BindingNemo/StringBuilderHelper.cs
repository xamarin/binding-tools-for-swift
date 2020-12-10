using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BindingNemo {
	public class StringBuilderHelper {
		public static string EnhanceMethodSignature (string signature, bool isStatic)
		{
			if (string.IsNullOrEmpty (signature) || signature.Contains ("_"))
				return null;

			StringBuilder sb = new StringBuilder (signature);

			var matchesTest1 = Regex.Match (sb.ToString (), @": \(");
			if (!matchesTest1.Success) {

			}


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
			sb.AddModule ();
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
			sb.AddModule ();

			if (isStatic)
				sb.Insert (0, "static ");

			return sb.ToString ();
		}

		public static string EnhanceReturn (string returnSignature)
		{
			if (string.IsNullOrEmpty (returnSignature) || returnSignature.Contains ("_"))
				return null;
			else if (returnSignature == "()")
				return "";
			
			StringBuilder sb = new StringBuilder (returnSignature);
			sb.CorrectSelf ();
			
			sb.Replace ("(", "( ");
			sb.RemoveDuplicateConsecutiveWords ();
			// fix the spacing we added
			sb.Replace ("( ", "(");
			sb.CorrectOptionals ();
			sb.FixBrackets ();
			sb.AddModule ();

			return sb.ToString ();
		}

		public static string EnhancePropertyType (string type)
		{
			StringBuilder sb = new StringBuilder (type);
			sb.AddModule ();
			return sb.ToString ();
		}

		public static string ParsePropertyType (string signature)
		{
			StringBuilder sb = new StringBuilder (signature);
			sb.RemoveDuplicateConsecutiveWords ();
			if (!sb.ToString ().Contains (":")) {
				return null;
			}
			var colonMatch = Regex.Match (sb.ToString (), ": ");
			// we add 2 to the index due to the colon and the space after
			return sb.ToString ().Substring (colonMatch.Index + 2);
		}

		public static string ParseParametersFromSignature (string signature)
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

			return parametersString;
			
		}

		public static List<Tuple<string, string>> SeperateParameters (string parametersString) {
			if (parametersString == "()") {
				return null;
			}
			if (parametersString.Contains ("Meta")) {

			}
			var startingSB = new StringBuilder (parametersString);
			startingSB.RemoveDuplicateConsecutiveWords ();
			startingSB.CorrectSelf ();
			startingSB.FixBrackets ();
			startingSB.AddModule ();
			
			var correctedParameterString = startingSB.ToString ();

			List<string> parameters = new List<string> ();
			StringBuilder parameter = new StringBuilder ();
			int openedCount = 0;
			for (int i = 0; i < correctedParameterString.Length; i++) {
				if (i == 0 && correctedParameterString [i] == '(')
					continue;
				switch (correctedParameterString [i]) {
				case '(':
					parameter.Append (correctedParameterString [i]);
					openedCount++;
					break;
				case ')':
					parameter.Append (correctedParameterString [i]);
					openedCount--;
					break;
				case ',':
					if (openedCount == 0) {
						parameters.Add (parameter.ToString ());
						parameter.Clear ();
						i++;
					} else {
						parameter.Append (correctedParameterString [i]);
					}

					break;
				default:
					parameter.Append (correctedParameterString [i]);
					break;
				}
			}
			parameters.Add (parameter.ToString ());

			var nameTypeTupleList = new List<Tuple<string, string>> ();
			foreach (var p in parameters) {
				if (!p.Contains (":")) {
					nameTypeTupleList.Add (Tuple.Create ("_", p));
				} else {
					var splitP = p.Split (':');
					nameTypeTupleList.Add (Tuple.Create (splitP [0], splitP [1].Substring (1)));

				}
			}

			// check for 'Meta'. If the type is "Meta"+something else, change it to be just the something else
			//foreach (var type in nameTypeTupleList) {
			for (int i = 0; i < nameTypeTupleList.Count; i++) {
				if (nameTypeTupleList[i].Item2.Contains ("Meta")) {
					var typeSplit = nameTypeTupleList [i].Item2.Split (' ');
					if (typeSplit.Length > 2) {

					}
					if (typeSplit.Length > 1 && typeSplit[1] != "" && typeSplit[1] != ")") {
						var replacement = new Tuple <string, string> (nameTypeTupleList [i].Item1, typeSplit [1]);
						nameTypeTupleList.RemoveAt (i);
						nameTypeTupleList.Insert (i, replacement);
					}
				}
			}

			return nameTypeTupleList;
		}
		
	}
}
