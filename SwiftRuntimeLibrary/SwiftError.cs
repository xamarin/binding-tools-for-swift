using System;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary {
	public class SwiftError {
		public SwiftError (IntPtr handle)
		{
			Handle = handle;
		}

		IntPtr ThrowIfInvalid ()
		{
			if (Handle == IntPtr.Zero)
				throw new SwiftRuntimeException ("SwiftError handle is invalid.");
			return Handle;
		}

		public IntPtr Handle { get; private set; }

		internal IntPtr Opaque1 {
			get {
				return Marshal.ReadIntPtr (SwiftMetatype.OffsetPtrByPtrSize (ThrowIfInvalid (), 0));
			}
		}

		internal IntPtr Opaque2 {
			get {
				return Marshal.ReadIntPtr (SwiftMetatype.OffsetPtrByPtrSize (ThrowIfInvalid (), 1));
			}
		}

		public SwiftMetatype Metatype {
			get {
				var p = Marshal.ReadIntPtr (SwiftMetatype.OffsetPtrByPtrSize (ThrowIfInvalid (), 5));
				return new SwiftMetatype (p);
			}
		}

		internal IntPtr ProtocolWitnessTable {
			get {
				var p = Marshal.ReadIntPtr (SwiftMetatype.OffsetPtrByPtrSize (ThrowIfInvalid (), 6));
				return p;
			}
		}

		internal SwiftMetatype HashableBaseType {
			get {
				var p = Marshal.ReadIntPtr (SwiftMetatype.OffsetPtrByPtrSize (ThrowIfInvalid (), 7));
				return new SwiftMetatype (p);
			}
		}

		internal IntPtr HashbleProtocolWitnessTable {
			get {
				var p = Marshal.ReadIntPtr (SwiftMetatype.OffsetPtrByPtrSize (ThrowIfInvalid (), 8));
				return p;
			}
		}

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftError_GetErrorDescription)]
		static extern void GetErrorDescription (IntPtr stringPtr, IntPtr handle);

		static string GetErrorDescription (IntPtr handle)
		{
			unsafe {
				using (var desc = new SwiftString (SwiftNominalCtorArgument.None)) {
					fixed (byte* p = desc.SwiftData) {
						GetErrorDescription (new IntPtr (p), handle);
					}
					return desc.ToString ();
				}
			}
		}

		public string Description {
			get {
				return GetErrorDescription (Handle);
			}
		}

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftError_DotNetErrorFactory)]
		static extern IntPtr DotNetErrorFactory (IntPtr message, IntPtr className);

		public static SwiftError FromException (Exception e)
		{
			if (e == null)
				throw new ArgumentNullException (nameof (e));
			if (e is SwiftException) {
				return ((SwiftException)e).Error;
			}
			using (SwiftString message = (SwiftString)e.Message, className = (SwiftString)e.GetType ().FullName) {
				unsafe {
					fixed (byte* mess = message.SwiftData, cl = className.SwiftData) {
						var handle = DotNetErrorFactory (new IntPtr (mess), new IntPtr (cl));
						return new SwiftError (handle);
					}
				}
			}
		}
	}
}
