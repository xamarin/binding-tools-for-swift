// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin;
using System.Text;
using Dynamo;
using System.Collections;

namespace SwiftReflector {
	public static class Extensions {
		public static IEnumerable<T> Yield<T> (this T elem)
		{
			yield return elem;
		}

		public static bool IsSwiftEntryPoint (this NListEntry entry)
		{
			return !String.IsNullOrEmpty (entry.str) && (entry.str.StartsWith ("__T", StringComparison.Ordinal) ||
				entry.str.StartsWith ("_$s", StringComparison.Ordinal) || entry.str.StartsWith ("_$S", StringComparison.Ordinal));
		}

		public static IEnumerable<NListEntry> SwiftEntryPoints (this IEnumerable<NListEntry> entries)
		{
			return entries.Where (IsSwiftEntryPoint);
		}

		public static IEnumerable<string> SwiftEntryPointNames (this IEnumerable<NListEntry> entries)
		{
			return entries.SwiftEntryPoints ().Select (nle => nle.str);
		}

		public static string DePunyCode (this string s)
		{
			return PunyCode.PunySingleton.Decode (s);
		}

		public static Tuple<string, string> SplitModuleFromName (this string s)
		{
			int dotIndex = s.IndexOf ('.');
			if (dotIndex < 0)
				return new Tuple<string, string> (null, s);
			if (dotIndex == 0)
				return new Tuple<string, string> (null, s.Substring (1));
			return new Tuple<string, string> (s.Substring (0, dotIndex), s.Substring (dotIndex + 1));
		}

		public static string ModuleFromName (this string s)
		{
			return s.SplitModuleFromName ().Item1;
		}

		public static string NameWithoutModule (this string s)
		{
			return s.SplitModuleFromName ().Item2;
		}

		public static string [] DecomposeClangTarget (this string s)
		{
			if (String.IsNullOrEmpty (s))
				throw new ArgumentNullException (nameof (s));
			string [] parts = s.Split ('-');
			if (parts.Length != 3)
				throw new ArgumentOutOfRangeException (nameof (s), s, "target should be in the form cpu-platform-os");
			int shortest = parts [0].Length < Math.Min (parts [1].Length, parts [2].Length) ? 0
			                        : (parts [1].Length < Math.Min (parts [0].Length, parts [1].Length) ? 1 : 2);
			if (parts [shortest].Length == 0)
				throw new ArgumentOutOfRangeException (nameof (s), s, String.Format ("target (cpu-platform-os) has an empty {0} component.",
																				  new string [] { "cpu", "platform", "os" } [shortest]));
			return parts;
		}

		public static string ClangTargetCpu (this string s)
		{
			return s.DecomposeClangTarget () [0];
		}

		public static string ClangTargetPlatform (this string s)
		{
			return s.DecomposeClangTarget () [1];
		}

		public static string ClangTargetOS (this string s)
		{
			return s.DecomposeClangTarget () [2];
		}

		public static void Merge<T> (this HashSet<T> to, IEnumerable<T> from)
		{
			Exceptions.ThrowOnNull (from, nameof (from));
			foreach (T val in from)
				to.Add (val);
		}

		public static void DisposeAll<T> (this IEnumerable<T> coll) where T : IDisposable
		{
			Exceptions.ThrowOnNull (coll, nameof (coll));
			foreach (T obj in coll) {
				if ((IDisposable)obj != null)
					obj.Dispose ();
			}
		}

		public static string InterleaveStrings (this IEnumerable<string> elements, string separator, bool includeSepFirst = false)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (string s in elements.Interleave (separator, includeSepFirst))
				sb.Append (s);
			return sb.ToString ();
		}

		public static string InterleaveCommas (this IEnumerable<string> elements)
		{
			return elements.InterleaveStrings (", ");
		}

		public static bool IsSwift3(this Version vers)
		{
			return vers.Major == 3;
		}

		public static bool IsSwift4 (this Version vers)
		{
			return vers.Major == 4;
		}

		public static int ErrorCount(this List<ReflectorError> errors)
		{
			var count = 0;
			foreach (var error in errors) {
				if (!error.IsWarning)
					++count;
			}
			return count;
		}

		public static int WarningCount(this List<ReflectorError> errors)
		{
			return errors.Count - errors.ErrorCount ();
		}

		public static List<T> CloneAndPrepend<T>(this List<T> source, T item)
		{
			var result = new List<T> (source.Count + 1);
			result.Add (item);
			result.AddRange (source);
			return result;
		}

		public static T[] And<T> (this T[] first, T[] second)
		{
			Exceptions.ThrowOnNull (first, nameof (first));
			Exceptions.ThrowOnNull (second, nameof (second));
			var result = new T [first.Length + second.Length];
			Array.Copy (first, result, first.Length);
			Array.Copy (second, 0, result, first.Length, second.Length);
			return result;
		}
	}
}

