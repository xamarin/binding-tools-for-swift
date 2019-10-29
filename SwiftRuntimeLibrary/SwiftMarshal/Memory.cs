// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal class Memory {
		public static unsafe void Copy (byte* src, byte* dest, int count)
		{
			while (count-- > 0) {
				*dest++ = *src++;
			}
		}
#if DEBUG
		public static void Dump (IntPtr ip, int count)
		{
			unsafe {
				if (ip == IntPtr.Zero)
					return;
				byte* p = (byte*)ip;
				for (int i = 0; i < count; i++) {
					if ((i % 8) != 0)
						Console.Write (" ");
					Console.Write ("{0}", (*p++).ToString ("X2"));
					if ((i % 8) == 7)
						Console.WriteLine ();
				}
				if (((count - 1) % 8 != 7))
					Console.WriteLine ();
			}
		}

		public static void DumpPtrs(IntPtr ip, int count)
		{
			for (int i = 0; i < count; i++) {
				var ptr = Marshal.ReadIntPtr (ip);
				Console.WriteLine (ptr.ToString ($"X{IntPtr.Size * 2}"));
				ip = ip + IntPtr.Size;
			}
		}

		[DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr object_getClassName (IntPtr obj);

		public static string ClassName (IntPtr obj)
		{
			if (obj == IntPtr.Zero)
				return "null handle";
			return Marshal.PtrToStringAuto (object_getClassName (obj));
		}

		public static string ClassName (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			return ClassName (obj.SwiftObject);
		}

		public static void DumpSwiftObject (ISwiftObject obj, bool terse)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			DumpSwiftObject (obj.SwiftObject, terse);
		}

		public static void DumpSwiftObject (IntPtr handle, bool terse)
		{
			if (handle == IntPtr.Zero) {
				Console.WriteLine ("null object");
				return;
			}
			Console.WriteLine ("Handle: " + handle.ToString ("X8"));
			if (!terse)
				DumpPtrs (handle, 2);
			Console.Write ($"Class Handle ({ClassName (handle)}): ");
			DumpPtrs (handle, 1);
			ulong bits = 0;
			var isImmortal = false;
			ulong unownedRefCount = 0;
			bool isDeiniting = false;
			ulong strongRefCount = 0;
			bool hasSideTable = false;
			if (IntPtr.Size == 8) {
				// 64 bit layout
				// |has side table:1|strong ref count:30|is deiniting: 1|unowned ref count:31|immortal:1|
				bits = (ulong)Marshal.ReadInt64 (handle + IntPtr.Size);
				isImmortal = (bits & 1) != 0;
				unownedRefCount = (bits >> 1) & 0x7ffffff;
				isDeiniting = ((bits >> 32) & 1) != 0;
				strongRefCount = ((bits >> 33) & 0x3fffffff);
				hasSideTable = (bits & 0x8000_0000_0000_0000) != 0;
			} else {
				// 32 bit layout
				// |has side table:1|strong ref count:22|is deiniting: 1|unowned: 7|immortal:1|
				bits = (uint)Marshal.ReadInt32 (handle + IntPtr.Size);
				isImmortal = (bits & 1) != 0;
				unownedRefCount = (bits >> 1) & 0x7f;
				isDeiniting = ((bits >> 8) & 1) != 0;
				strongRefCount = ((bits >> 9) & 0x3f_ffff);
				hasSideTable = (bits & 0x8000_0000) != 0;
			}
			if (!terse) {
				Console.WriteLine ("Immortal: " + isImmortal);
				Console.WriteLine ("Has side table: " + hasSideTable);
				if (!hasSideTable) {
					Console.WriteLine ("Is Deiniting: " + isDeiniting);
					Console.WriteLine ("Unowned reference count field: " + unownedRefCount);
					Console.WriteLine ("Strong reference count field: " + strongRefCount);
				}
				Console.WriteLine ("Reference counts from swift API:");
			}
			Console.WriteLine (MemoryStatus (handle));
		}

		public static string MemoryStatus (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			return MemoryStatus (obj.SwiftObject);
		}

		public static string MemoryStatus (IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				return "(zero handle)";
			return $"s: {SwiftCore.RetainCount (handle)} u: {SwiftCore.UnownedRetainCount (handle)} w: {SwiftCore.WeakRetainCount (handle)} IsDealloc: {SwiftCore.IsDeallocating (handle)}";
		}

		public static void PrintObjectInfo (ISwiftObject obj, string label, int stackFrameIndex)
		{
			var handle = obj == null ? IntPtr.Zero : obj.SwiftObject;
			var typeName = obj != null ? obj.GetType ().Name : "(null)";

			PrintObjectInfo (handle, label, typeName, stackFrameIndex > 0 ? stackFrameIndex + 1 : 0);
		}

		public static void PrintObjectInfo (IntPtr handle, string label, string typeName, int stackFrameIndex)
		{
			label = label ?? "";
			typeName = typeName ?? "";
			var frameInfo = "";
			if (stackFrameIndex > 0) {
				var frame = new StackTrace (true).GetFrame (stackFrameIndex);
				var method = frame.GetMethod ();
				frameInfo = $"{method.DeclaringType.Name}.{method} ";
			}
			if (handle == IntPtr.Zero) {
				Console.WriteLine ($"\x001b[36m{label} {frameInfo}- null object\x001b[0m");
				return;
			}
			var swiftClassName = ClassName (handle);
			var status = MemoryStatus (handle);
			Console.WriteLine ($"\x001b[36m{label} {frameInfo}{handle.ToString($"X{IntPtr.Size * 2}")} {typeName} {swiftClassName} {status}\x001b[0m");
		}

#endif
	}
}

