// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using SwiftRuntimeLibrary;

namespace SwiftReflector.IOUtils {
	public class ReliablePath {
		public static string GetParentDirectory (string path)
		{
			Exceptions.ThrowOnNull (path, nameof (path));
			if (path.EndsWith ("/")) {
				path = path.Substring (0, path.Length - 1);
			}
			return Path.GetDirectoryName (path);
		}
	}
}
