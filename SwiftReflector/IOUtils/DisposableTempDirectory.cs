using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace SwiftReflector.IOUtils {
	public class DisposableTempDirectory : IDisposable {
		// If directories are deleted at process startup (for previous
		// processes), or when the DisposableTempDirectory instance is
		// disposed/collected. Deleting at process startup is useful when
		// debugging tests: you can examine temporary files after the test has
		// completed.
		static readonly bool delete_on_launch;
		// If paths are deterministic (the nth request always gets the same
		// path). Deterministic paths are useful when debugging tests:
		// temporary files end up in the same location every time you run a
		// test from the IDE.
		static readonly bool deterministic;
		static int counter;
		static readonly string root;

		static DisposableTempDirectory ()
		{
			root = Path.Combine (Path.GetTempPath (), "binding-tools-for-swift");
#if DEBUG
			// Default to the helpful values when running unit tests in the IDE.
			// This is only something we want possible in DEBUG mode
			if (Assembly.GetEntryAssembly () == null) {
				delete_on_launch = true;
				deterministic = true;
				root = Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "binding-tools-for-swift-tests");
			}
			if (delete_on_launch && Directory.Exists (root))
				Directory.Delete (root, true);
#endif
			Directory.CreateDirectory (root);
		}

		public DisposableTempDirectory (string directoryName = null, bool prependGuid = true)
		{
			string unique = deterministic ? Interlocked.Increment (ref counter).ToString () : Guid.NewGuid ().ToString ();
			if (directoryName != null)
				directoryName = unique + directoryName;

			directoryName = directoryName ?? unique;

			this.DirectoryPath = Path.Combine (root, directoryName);

			Directory.CreateDirectory (this.DirectoryPath);
		}

		#region IDisposable implementation

		~DisposableTempDirectory ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!delete_on_launch)
				RemoveTempDirectoryAndContents ();
		}

		#endregion

		void RemoveTempDirectoryAndContents ()
		{
			Directory.Delete (DirectoryPath, true);
		}

		public string DirectoryPath { get; private set; }

		public string UniqueName (string prefix, string suffix, string extension)
		{
			return String.Format ("{0}{1}{2}{3}{4}",
				(prefix ?? ""),
				(deterministic ? Interlocked.Increment (ref counter).ToString () : Guid.NewGuid ().ToString ()),
				(suffix ?? ""),
				(extension != null && !extension.StartsWith (".", StringComparison.Ordinal) ? "." : ""),
				(extension ?? "")
			);
		}

		public string UniquePath (string prefix, string suffix, string extension)
		{
			return Path.Combine (DirectoryPath, UniqueName (prefix, suffix, extension));
		}
	}
}

