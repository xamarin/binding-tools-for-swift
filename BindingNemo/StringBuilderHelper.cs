using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BindingNemo {
	public class StringBuilderHelper {

		public static bool CheckForPrivateSignature (string signature)
		{
			if (string.IsNullOrEmpty (signature) || signature.Contains ("_"))
				return true;
			return false;
		}

		public static string EnhancePropertySignature (string signature, bool isStatic)
		{
			if (string.IsNullOrEmpty (signature) || signature.Contains ("_"))
				return null;

			StringBuilder sb = new StringBuilder (signature);
			sb.Replace (": ()->", ": ");
			sb.Insert (0, "var ");

			sb.CorrectOptionals ();
			sb.TransformGenerics ();
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
			sb.TransformGenerics ();
			sb.RemoveDuplicateConsecutiveWords ();
			sb.CorrectOptionals ();
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
			sb.AddModule ();
			if (!sb.ToString ().Contains (":")) {
				return null;
			}
			var colonMatch = Regex.Match (sb.ToString (), ": ");
			// we add 2 to the index due to the colon and the space after
			var type = sb.ToString ().Substring (colonMatch.Index + 2);
			return RemoveMetaProperty (type);
		}
		
		static string RemoveMetaProperty (string propertyType)
		{
			if (!propertyType.Contains ("Meta") && !propertyType.Contains ("Existential Metatype")) {
				return propertyType;
			}
			var typeSplit = propertyType.Split (' ');
			if (typeSplit.Length > 1 && typeSplit [1] != "" && typeSplit [1] != ")") {
				return typeSplit [typeSplit.Length - 1];
			}
			return propertyType;
		}

		public static List<Tuple<string, string>> SeperateParameters (string parametersString) {
			if (parametersString == "()") {
				return null;
			}

			var startingSB = new StringBuilder (parametersString);
			startingSB.RemoveDuplicateConsecutiveWords ();
			startingSB.TransformGenerics ();
			startingSB.AddModule ();

			List<string> parameters = BreakParameterStringToList (startingSB.ToString ());
			return CreateParameterTuple (parameters);
		}

		static List<string> BreakParameterStringToList (string parameterString)
		{
			List<string> parameters = new List<string> ();
			StringBuilder parameter = new StringBuilder ();
			int openedParenthesisCount = 0;
			int openedBracketCount = 0;
			for (int i = 0; i < parameterString.Length; i++) {
				if (i == 0 && parameterString [i] == '(')
					continue;
				switch (parameterString [i]) {
				case '(':
					parameter.Append (parameterString [i]);
					openedParenthesisCount++;
					break;
				case ')':
					parameter.Append (parameterString [i]);
					openedParenthesisCount--;
					break;
				case '<':
					parameter.Append (parameterString [i]);
					openedBracketCount++;
					break;
				case '>':
					// see if this is a part of '->'
					if (i > 0 && parameterString [i - 1] == '-') {
						parameter.Append (parameterString [i]);
					} else {
						parameter.Append (parameterString [i]);
						openedBracketCount--;
					}
					break;
				case ',':
					if (openedParenthesisCount == 0 && openedBracketCount == 0) {
						parameters.Add (parameter.ToString ());
						parameter.Clear ();
						i++;
					} else {
						parameter.Append (parameterString [i]);
					}
					break;
				default:
					parameter.Append (parameterString [i]);
					break;
				}
			}
			parameters.Add (parameter.ToString ());
			return parameters;
		}

		static List<Tuple<string, string>> CreateParameterTuple (List<string> parameters)
		{
			var nameTypeTupleList = new List<Tuple<string, string>> ();

			// split the parameters into their name and their type
			foreach (var p in parameters) {
				if (!p.Contains (":")) {
					nameTypeTupleList.Add (Tuple.Create ("_", p));
				} else {
					var splitP = p.Split (':');
					if (splitP [0] == "") {
						nameTypeTupleList.Add (Tuple.Create ("_", splitP [1].Substring (1)));
					} else {
						nameTypeTupleList.Add (Tuple.Create (splitP [0], splitP [1].Substring (1)));
					}
				}
			}

			// check for 'Meta'. If the type is "Meta"+something else, change it to be just the something else
			for (int i = 0; i < nameTypeTupleList.Count; i++) {
				if (nameTypeTupleList [i].Item2.Contains ("Meta")) {
					var typeSplit = nameTypeTupleList [i].Item2.Split (' ');
					if (typeSplit.Length > 1 && typeSplit [1] != "" && typeSplit [1] != ")") {
						var replacement = new Tuple<string, string> (nameTypeTupleList [i].Item1, typeSplit [1]);
						nameTypeTupleList.RemoveAt (i);
						nameTypeTupleList.Insert (i, replacement);
					}
				}
			}
			return nameTypeTupleList;
		}
	}
}
