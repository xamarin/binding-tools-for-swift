using System.IO;

namespace SwiftReflector.IOUtils {
	public class ReliablePath {
		public static string GetParentDirectory (string path)
		{
			Ex.ThrowOnNull (path, nameof (path));
			if (path.EndsWith ("/")) {
				path = path.Substring (0, path.Length - 1);
			}
			return Path.GetDirectoryName (path);
		}
	}
}
