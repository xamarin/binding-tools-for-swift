﻿using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DylibBinder {
	internal static class StringExtensions {
		internal static bool IsPublic (this string s)
		{
			if (s.Contains ("_"))
				return false;
			return true;
		}

		internal static string IsNull (this string s)
		{
			if (s == null)
				return "True";
			return "False";
		}

		internal static string PrependModule (this string s, string moduleName)
		{
			return s.Insert (0, $"{moduleName}.");
		}

		internal static string AppendModuleToBit (this string s)
		{
			var sb = new StringBuilder (s);
			var builtInTypes = new string [] { "Bool", "Double", "Float", "UInt", "Int" };
			foreach (var builtInType in builtInTypes) {
				FindAndAppendModule (sb, builtInType);
			}
			return sb.ToString ();
		}

		static void FindAndAppendModule (StringBuilder sb, string builtInType)
		{
			var offset = 0;
			var sizeOfSwiftInsert = 6;
			MatchCollection matches = Regex.Matches (sb.ToString (), builtInType);
			foreach (Match match in matches) {
				// TODO issue whith sb = "high: Swift.Int, low: Swift.USwift.Int"

				if (builtInType == "Int" && IsIntMatchUInt (sb, match, offset)) {
					continue;
				}

				if (!IsSwiftAlreadyPresent (sb, match, sizeOfSwiftInsert)) {
					sb.Insert (match.Index + offset, "Swift.");
					offset += sizeOfSwiftInsert;
				}
			}
		}

		static bool IsSwiftAlreadyPresent (StringBuilder sb, Match match, int sizeOfSwiftInsert)
		{
			if (match.Index > sizeOfSwiftInsert) {
				var substring = sb.ToString ().Substring (match.Index - sizeOfSwiftInsert, sizeOfSwiftInsert);
				if (sb.ToString ().Substring (match.Index - sizeOfSwiftInsert, sizeOfSwiftInsert) != "Swift.") {
					return false;
				}
				return true;
			}
			return false;
		}

		static bool IsIntMatchUInt (StringBuilder sb, Match match, int offset)
		{
			if (match.Index > 0) {
				if (sb.ToString ()[match.Index-1 + offset] == 'U') {
					return true;
				}
				return false;
			}
			return false;
		}
	}
}
