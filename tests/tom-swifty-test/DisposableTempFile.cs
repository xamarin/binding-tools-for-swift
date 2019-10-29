// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace tomwiftytest {
	public class DisposableTempFile : IDisposable {
		public DisposableTempFile (string prefix, string suffix, string extension, bool makeStream)
		{
			// creates a filename in the form
			// [prefix]GUID[suffix][.extension]
			string filename = String.Format ("{0}{1}{2}{3}{4}",
				(prefix ?? ""),
				Guid.NewGuid ().ToString (),
				(suffix ?? ""),
				extension != null ? "." : "", extension ?? "");
			Filename = Path.Combine (Path.GetTempPath (), filename);
			if (makeStream)
				this.Stream = new FileStream (Filename, FileMode.Create);
		}

		public DisposableTempFile (string filename, bool makeStream)
		{
			Filename = Path.Combine (Path.GetTempPath (), filename);
			if (makeStream)
				this.Stream = new FileStream (Filename, FileMode.Create);
		}

		public Stream Stream { get; private set; }

		public string Filename { get; private set; }

		public void RemoveTempFile ()
		{
			lock (this) {
				if (File.Exists (Filename)) {
					if (this.Stream != null)
						this.Stream.Dispose ();
					File.Delete (Filename);
				}
			}
		}

		~DisposableTempFile ()
		{
			Dispose (false);
		}
		#region IDisposable implementation

		bool disposed;

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposed) {
				if (disposing)
					RemoveTempFile ();
				disposed = true;
			}
		}

		#endregion
	}
}

