using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	[Flags]
	internal enum DLOpenMode {
		None = 0,
		Lazy = 1 << 0,
		Now = 1 << 1,
		Local = 1 << 2,
		Global = 1 << 3,
		NoLoad = 1 << 4,
		NoDelete = 1 << 7,
		First = 1 << 8
	}

	internal class DynamicLib : IDisposable {
		[DllImport ("libSystem.B.dylib", EntryPoint = "dlopen")]
		static extern IntPtr DLOpen ([MarshalAs (UnmanagedType.LPStr)]string file, DLOpenMode flags);

		[DllImport ("libSystem.B.dylib", EntryPoint = "dlerror")]
		static extern IntPtr _DLError ();

		[DllImport ("libSystem.B.dylib", EntryPoint = "dlclose")]
		static extern int DLClose (IntPtr handle);

		[DllImport ("libSystem.B.dylib", EntryPoint = "dlsym")]
		static extern IntPtr DLSym (IntPtr handle, [MarshalAs (UnmanagedType.LPStr)]string name);


		public static string DLError {
			get {
				IntPtr p = _DLError ();
				return Marshal.PtrToStringAnsi (p);
			}
		}


		IntPtr handle;

		public DynamicLib (string fileName, DLOpenMode flags)
		{
			if (fileName == null)
				throw new ArgumentNullException (nameof (fileName));
			if (flags == DLOpenMode.None)
				throw new ArgumentOutOfRangeException (nameof (flags));

			string selfPath = Path.GetDirectoryName (typeof (DynamicLib).Assembly.Location);
			string [] possibleFiles = new string [] {
				fileName,
				Path.Combine(selfPath, Path.Combine($"Frameworks/{fileName}.framework/{fileName}"))
			};

			var sb = new StringBuilder ();
			foreach (string file in possibleFiles) {
				FileName = file;
				handle = DLOpen (file, flags);
				if (handle == IntPtr.Zero) {
					sb.Append ('\n');
					sb.Append ($"+{file} {(File.Exists (file) ? "(exists)" : " (does not exist)")}");
					string err = DLError;
					if (err != null) {
						sb.Append (": ");
						sb.Append (err);
					}
				} else {
					break;
				}
			}
			if (handle == IntPtr.Zero) {
				string sbbuff = sb.ToString ();
				throw new ArgumentException ($"Unable to load library {fileName}:{sbbuff}", nameof (fileName));
			}

		}

		public string FileName { get; private set; }

		public IntPtr FindSymbolAddress (string name)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));
			var p = DLSym (handle, name);
			if (p == IntPtr.Zero)
				if (FileName != null)
					throw new ArgumentException ($"Unable to find symbol {name} from handle {handle} in {FileName}: {DLError}.", nameof (name));
				else
					throw new ArgumentException ($"Unable to find symbol {name} from handle {handle}: {DLError}.", nameof (name));
			return p;
		}


		~DynamicLib ()
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
			if (disposing) {
				DisposeManagedResources ();
			}
			DisposeUnmanagedResources ();
		}

		protected virtual void DisposeManagedResources () { }
		protected virtual void DisposeUnmanagedResources ()
		{
			if (handle != IntPtr.Zero) {
				DLClose (handle);
				handle = IntPtr.Zero;
			}
		}
	}
}

