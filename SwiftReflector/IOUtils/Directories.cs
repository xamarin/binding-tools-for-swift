using System;
using System.IO;
namespace SwiftReflector.IOUtils {
	public static class Directories {
		// from https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
		public static void DeleteContentsAndSelf (string target_dir)
		{
			string [] files = Directory.GetFiles (target_dir);
			string [] dirs = Directory.GetDirectories (target_dir);

			foreach (string file in files) {
				File.SetAttributes (file, FileAttributes.Normal);
				File.Delete (file);
			}

			foreach (string dir in dirs) {
				DeleteContentsAndSelf (dir);
			}

			Directory.Delete (target_dir, false);
		}
	}
}
