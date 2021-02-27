using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DylibBinder {
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

		public static string EnhanceReturn (SwiftReflector.SwiftBaseFunctionType signature, int depth, int genericArguments = 0, int genericParameterCount = 0)
		{
			var returnSignature = signature.ReturnType.ToString ();
			if (string.IsNullOrEmpty (returnSignature) || returnSignature.Contains ("_"))
				return null;
			else if (returnSignature == "()")
				return "";
			
			StringBuilder sb = new StringBuilder (returnSignature);
			sb.TransformGenericsToThisLevel (signature.Parameters.ToString (), returnSignature, depth, genericArguments, genericParameterCount);
			sb.TransformGenerics ();
			sb.RemoveDuplicateConsecutiveWords ();
			sb.CorrectOptionals ();
			sb.AddModule ();
			sb.RemoveAssociatedTypeRemnants ();

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
			sb.RemoveAssociatedTypeRemnants ();
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

		public static List<Tuple<string, string>> SeperateParameters (SwiftReflector.SwiftBaseFunctionType signature, int depth, int genericArguments = 0, int genericParameterCount = 0) {
			var parametersString = signature.Parameters.ToString ();
			if (parametersString == "()") {
				return null;
			}

			var startingSB = new StringBuilder (parametersString);
			startingSB.RemoveDuplicateConsecutiveWords ();
			startingSB.TransformGenericsToThisLevel (parametersString, signature.ReturnType.ToString (), depth, genericArguments, genericParameterCount);
			startingSB.TransformGenerics ();
			startingSB.AddModule ();
			startingSB.RemoveAssociatedTypeRemnants ();
			var testBeforeClosure = startingSB.ToString ();
			startingSB.AddParenthesisToClosure ();

			List<string> parameters = BreakParameterStringToList (startingSB.ToString ());
			return CreateParameterTuple (parameters);
		}

		public static string ReapplyClosureParenthesis (string typeString)
		{
			var sb = new StringBuilder (typeString);
			sb.AddParenthesisToClosure ();
			return sb.ToString ();
		}

		static List<string> BreakParameterStringToList (string parameterString)
		{
			List<string> parameters = new List<string> ();
			StringBuilder parameter = new StringBuilder ();
			StringBuilder updatedParameterString = new StringBuilder (parameterString);
			int openedParenthesisCount = 0;
			int openedBracketCount = 0;

			// we want to remove the first and last parenthesis if they match eachother
			// and are therefore useless here
			if (parameterString[0] == '(' && parameterString[parameterString.Length - 1] == ')') {
				var openParensMatch = Regex.Matches (parameterString, @"\(");
				var closedParensMatch = Regex.Matches (parameterString, @"\)");
				if (openParensMatch.Count == closedParensMatch.Count) {
					updatedParameterString.Remove (updatedParameterString.ToString ().Length - 1, 1);
					updatedParameterString.Remove (0, 1);
				}
			}

			parameterString = updatedParameterString.ToString ();

			for (int i = 0; i < parameterString.Length; i++) {
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

			var openedParenthesisCount = 0;
			// split the parameters into their name and their type
			foreach (var p in parameters) {
				var done = false;
				if (!p.Contains (":")) {
					nameTypeTupleList.Add (Tuple.Create ("_", p));
				} else {
					for (var i = 0; i < p.Length; i++) {
						if (done)
							break;
						switch (p [i]) {
						case '(':
							openedParenthesisCount++;
							break;
						case ')':
							openedParenthesisCount--;
							break;
						case ':':
							if (openedParenthesisCount == 0) {
								if (i == 0)
									nameTypeTupleList.Add (Tuple.Create ("_", p.Substring (i + 2)));
								else
									nameTypeTupleList.Add (Tuple.Create (p.Substring (0, i), p.Substring (i+2)));
								done = true;
							}
							break;
						default:
							break;
						}
					}
					if (!done)
						nameTypeTupleList.Add (Tuple.Create ("_", p));
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

		public static string EscapeCharacters (string s)
		{
			var sb = new StringBuilder (s);
			sb.TransformEscapeCharacters ();
			return sb.ToString ();
		}
	}
}
