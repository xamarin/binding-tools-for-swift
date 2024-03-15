using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Dynamo.CSLang;
using SwiftReflector.TypeMapping;

#nullable enable

namespace SwiftReflector.Naming
{
	public class CSSafeNaming {

		static UnicodeCategory [] validStarts = {
			UnicodeCategory.UppercaseLetter,
			UnicodeCategory.LowercaseLetter,
			UnicodeCategory.TitlecaseLetter,
			UnicodeCategory.ModifierLetter,
			UnicodeCategory.OtherLetter,
			UnicodeCategory.LetterNumber
		};

		static bool ValidIdentifierStart (UnicodeCategory cat)
		{
			return validStarts.Contains (cat);
		}

		static UnicodeCategory [] validContent = {
			UnicodeCategory.DecimalDigitNumber,
			UnicodeCategory.ConnectorPunctuation,
			UnicodeCategory.Format
		};

		static bool ValidIdentifierContent (UnicodeCategory cat)
		{
			return ValidIdentifierStart (cat) || validContent.Contains (cat);
		}

		static bool IsValidIdentifier (int position, UnicodeCategory cat)
		{
			if (position == 0)
				return ValidIdentifierStart (cat);
			else
				return ValidIdentifierContent (cat);
		}

		static bool IsHighUnicode (string s)
		{
			// Steve says: this is arbitrary, but it solves an issue
			// with mcs and csc not liking certain Ll and Lu class
			// unicode characters (for now).
			// Open issue: https://github.com/dotnet/roslyn/issues/27986
			var encoding = Encoding.UTF32;
			var bytes = encoding.GetBytes (s);
			var utf32Value = BitConverter.ToUInt32 (bytes, 0);
			return utf32Value > 0xffff;
		}

		public static string SafeIdentifier (string name, ScopedNaming? naming = null)
		{
			var sb = new StringBuilder ();

			var characterEnum = StringInfo.GetTextElementEnumerator (name);
			while (characterEnum.MoveNext ()) {
				string c = characterEnum.GetTextElement ();
				int i = characterEnum.ElementIndex;

				var cat = CharUnicodeInfo.GetUnicodeCategory (name, i);

				if (IsValidIdentifier (i, cat) && !IsHighUnicode (c))
					sb.Append (i == 0 && cat == UnicodeCategory.LowercaseLetter ? c.ToUpper () : c);
				else
					sb.Append (UnicodeMapper.MapToUnicodeName (c));
			}

			if (CSKeywords.IsKeyword (sb.ToString ()))
				sb.Append ('_');
			return naming is not null ? naming.GenSym (sb.ToString ()) : sb.ToString ();
		}

		public static CSIdentifier SafeCSIdentifier (string name, ScopedNaming? naming)
		{
			return new CSIdentifier (SafeIdentifier (name, naming));
		}

		static Dictionary<char, string> operatorMap = new Dictionary<char, string> {
			{ '/', "Slash" },
			{ '=', "Equals" },
			{ '+', "Plus" },
			{ '-', "Minus" },
			{ '!', "Bang" },
			{ '*', "Star" },
			{ '%', "Percent" },
			{ '<', "LessThan" },
			{ '>', "GreaterThan" },
			{ '&', "Ampersand" },
			{ '|', "Pipe" },
			{ '^', "Hat" },
			{ '~', "Tilde" },
			{ '?', "QuestionMark"},
			{ '.', "Dot" },
		};

		static string OperatorCharToSafeString (char c)
		{
			return operatorMap.TryGetValue (c, out var result) ? result : c.ToString ();
		}

		public static string SafeOperatorName (string s)
		{
			var sb = new StringBuilder ();
			foreach (var c in s) {
				sb.Append (OperatorCharToSafeString (c));
			}
			return SafeIdentifier (sb.ToString ());
		}

		public static UnicodeMapper UnicodeMapper = UnicodeMapper.Default;
	}
}

