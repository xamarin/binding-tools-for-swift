// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace SwiftReflector.IOUtils {
	public static class PosixHelpers {
		// This implementation of RealPath is here because we need specific behavior
		// that other implementations do not give or do not have the proper granularity
		// to allow us to wrap them.
		// First, we need RealPath to be able to work on paths that might not exist (yet).
		// Second, we need them to work on null.
		// In the Mono.Posix assembly, there is an implementation of RealPath which fails on
		// non-existent paths and on null.
		// The implementation of GetCanonicalPath is private, so I can't call it to handle
		// path elements that are . or ..
		public static string RealPath (string path)
		{
			// intentional: return null on null so that
			// var foo = ReadPath(somethingNull) works
			if (String.IsNullOrEmpty (path))
				return path;

			// A custom implementation for inexistent paths, since realpath doesn't support those
			if (!File.Exists (path) && !Directory.Exists (path)) {
				// Path.GetFullPath will resolve '.' and '..', and it works on non-existent paths.
				path = Path.GetFullPath (path);
				// A containing directory will exist, so run RealPath recursively on parent directories.
				return Path.Combine (RealPath (Path.GetDirectoryName (path)), Path.GetFileName (path));
			}

			// The path exists, so we can use realpath.
			// Mono.Unix.UnixPath.GetRealPath doesn't resolve symlinks unless it's the last component.
			// Mono.Unix.UnixPath.GetCompleteRealPath doesn't resolve relative paths in symlinks.
			// So P/Invoke the platforms's realpath method instead.
			return GetRealPath (path);
		}

		[DllImport ("/usr/lib/libSystem.dylib", SetLastError = true)]
		static extern string realpath (string path, IntPtr zero);

		internal static string GetRealPath (string path)
		{
			var rv = realpath (path, IntPtr.Zero);
			if (rv != null)
				return rv;

			var errno = Marshal.GetLastWin32Error ();
			ErrorHelper.Warning (ReflectorError.kCantHappenBase + 67, "Unable to canonicalize the path '{0}': {1} ({2}).", path, strerror (errno), errno);
			return path;
		}

		[DllImport ("/usr/lib/libSystem.dylib", SetLastError = true, EntryPoint = "strerror")]
		static extern IntPtr _strerror (int errno);

		internal static string strerror (int errno)
		{
			return Marshal.PtrToStringAuto (_strerror (errno));
		}

	}
}
