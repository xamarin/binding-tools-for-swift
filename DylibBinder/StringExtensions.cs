using System;
using System.Text;
using System.Text.RegularExpressions;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal static class StringExtensions {
		static readonly string [] BuiltInTypes = { "Bool", "Double", "Float", "UInt", "Int" };

		internal static bool IsPublic (this string s)
		{
			// Need to find a better way to filter public elements. Issue https://github.com/xamarin/binding-tools-for-swift/issues/704
			Exceptions.ThrowOnNull (s, nameof (s));
			return !s.Contains ("_");
		}

		internal static string PrependModule (this string s, string moduleName)
		{
			Exceptions.ThrowOnNull (s, nameof (s));
			Exceptions.ThrowOnNull (moduleName, nameof (moduleName));
			return s.Insert (0, $"{moduleName}.");
		}

		internal static string AppendModuleToBit (this string s)
		{
			var sb = new StringBuilder (Exceptions.ThrowOnNull (s, nameof (s)));
			foreach (var builtInType in BuiltInTypes) {
				FindAndAppendModule (sb, builtInType);
			}
			return sb.ToString ();
		}

		static void FindAndAppendModule (StringBuilder sb, string builtInType)
		{
			Exceptions.ThrowOnNull (sb, nameof (sb));
			Exceptions.ThrowOnNull (builtInType, nameof (builtInType));
			var offset = 0;
			var sizeOfSwiftInsert = 6;
			MatchCollection matches = Regex.Matches (sb.ToString (), builtInType);
			foreach (Match match in matches) {
				if (builtInType == "Int" && IsIntMatchUInt (sb, match, offset))
					continue;

				if (!ContainsSwift (sb, match, sizeOfSwiftInsert)) {
					sb.Insert (match.Index + offset, "Swift.");
					offset += sizeOfSwiftInsert;
				}
			}
		}

		static bool IsIntMatchUInt (StringBuilder sb, Match match, int offset)
		{
			Exceptions.ThrowOnNull (sb, nameof (sb));
			Exceptions.ThrowOnNull (match, nameof (match));
			if (match.Index > 0) {
				if (sb[match.Index - 1 + offset] == 'U') {
					return true;
				}
				return false;
			}
			return false;
		}

		static bool ContainsSwift (StringBuilder sb, Match match, int sizeOfSwiftInsert)
		{
			Exceptions.ThrowOnNull (sb, nameof (sb));
			Exceptions.ThrowOnNull (match, nameof (match));
			if (match.Index > sizeOfSwiftInsert) {
				if (sb.ToString (match.Index - sizeOfSwiftInsert, sizeOfSwiftInsert) != "Swift.") {
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
