// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector.ExceptionTools;
using ObjCRuntime;

namespace SwiftReflector.IOUtils {
	public abstract class TempDirectoryStreamProvider<T> : DisposableTempDirectory, IStreamProvider<T>, IFileProvider<T> {
		Dictionary<T, string> inProgress = new Dictionary<T, string> (), complete = new Dictionary<T, string> ();
		object lock_obj = new object ();

		// directoryName can be null
		public TempDirectoryStreamProvider (string directoryName, bool prependGuid)
			: base (directoryName, prependGuid)
		{
		}

		protected abstract string FromThing (T thing);

		#region IStreamProvider implementation
		public Stream ProvideStreamFor (T thing)
		{
			lock (lock_obj) {
				string name = FromThing (thing);
				string path = Path.Combine (DirectoryPath, name);

				if (inProgress.ContainsKey (thing) || complete.ContainsKey (thing))
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 5, $"Already providing stream for {thing}");
				Stream stm = new FileStream (path, FileMode.Create);
				inProgress.Add (thing, path);
				return stm;
			}
		}

		public bool IsEmpty {
			get { return complete.Count == 0 && inProgress.Count == 0; }
		}

		public IEnumerable<string> CompletedFileNames {
			get {
				return complete.Keys.Select (t => FromThing (t));
			}
		}

		public IEnumerable<string> InProgressFileNames {
			get {
				return inProgress.Keys.Select (t => FromThing (t));
			}
		}

		public void NotifyStreamDone (T thing, Stream stm)
		{
			lock (lock_obj) {
				string name = FromThing (thing);
				string path = Path.Combine (DirectoryPath, name);

				if (!inProgress.ContainsKey (thing))
					ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 6, $"Attempt to write unknown object associated with {name}");
				if (complete.ContainsKey (thing))
					ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 7, $"Attempt to write already completed object {name}");
				inProgress.Remove (thing);
				complete.Add (thing, path);
				stm.Close ();
			}
		}

		public void RemoveStream(T thing, Stream stm)
		{
			lock (lock_obj) {
				string name = FromThing (thing);
				string path = Path.Combine (DirectoryPath, name);
				inProgress.Remove (thing);
				stm.Close ();
				try {
					File.Delete (path);
				}
				catch (Exception err){
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 8, $"Failed attempt to remove file {path}: {err.Message}");
				}
			}
		}

		#endregion

		#region IFileProvider implementation

		public string ProvideFileFor (T thing)
		{
			lock (lock_obj) {
				string name = FromThing (thing);
				string path = Path.Combine (DirectoryPath, name);
				if (inProgress.ContainsKey (thing) || complete.ContainsKey (thing))
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 9, $"attempt to multiply write file associated with {name}");
				return path;
			}
		}

		public void NotifyFileDone (T thing, string stm)
		{
			lock (lock_obj) {
				string name = FromThing (thing);
				string path = Path.Combine (DirectoryPath, name);

				if (!inProgress.ContainsKey (thing))
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 10, $"attempt to write file for unknown object {name}");
				if (complete.ContainsKey (thing))
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 11, $"attempt to write file for already completed object {name}");
				inProgress.Remove (thing);
				complete.Add (thing, path);
			}
		}

		#endregion
	}
}

