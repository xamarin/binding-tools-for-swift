using System;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal static class SwiftStandardMetatypes {
		static SwiftStandardMetatypes ()
		{
			using (DynamicLib lib = new DynamicLib (SwiftCoreConstants.LibSwiftCore, DLOpenMode.Now)) {
				Int = SwiftMetatype.FromDylib (lib, "$sSiN").Value;
				UInt = SwiftMetatype.FromDylib (lib, "$sSuN").Value;
				String = SwiftMetatype.FromDylib (lib, "$sSSN").Value;
				Bool = SwiftMetatype.FromDylib (lib, "$sSbN").Value;
				Float = SwiftMetatype.FromDylib (lib, "$sSfN").Value;
				Double = SwiftMetatype.FromDylib (lib, "$sSdN").Value;
				Int8 = SwiftMetatype.FromDylib (lib, "$ss4Int8VN").Value;
				UInt8 = SwiftMetatype.FromDylib (lib, "$ss5UInt8VN").Value;
				Int16 = SwiftMetatype.FromDylib (lib, "$ss5Int16VN").Value;
				UInt16 = SwiftMetatype.FromDylib (lib, "$ss6UInt16VN").Value;
				Int32 = SwiftMetatype.FromDylib (lib, "$ss5Int32VN").Value;
				UInt32 = SwiftMetatype.FromDylib (lib, "$ss6UInt32VN").Value;
				Int64 = SwiftMetatype.FromDylib (lib, "$ss5Int64VN").Value;
				UInt64 = SwiftMetatype.FromDylib (lib, "$ss6UInt64VN").Value;
				Void = SwiftMetatype.FromDylib (lib, "$sytN", IntPtr.Size).Value;
				UnsafeRawPointer = SwiftMetatype.FromDylib (lib, "$sSVN").Value;
				UnsafeMutableRawPointer = SwiftMetatype.FromDylib (lib, "$sSvN").Value;
				OpaquePointer = SwiftMetatype.FromDylib (lib, SwiftCoreConstants.OpaquePointer_Metadata).Value;
			}
			try { // don't require libswiftCoreGraphics.dylib
				using (DynamicLib lib = new DynamicLib ("libswiftCoreGraphics.dylib", DLOpenMode.Now)) {
					CGFloat = SwiftMetatype.FromDylib (lib, "$s12CoreGraphics7CGFloatVN").Value;
				}
			} catch (ArgumentException) { }
		}

		public static SwiftMetatype Int { get; private set; }
		public static SwiftMetatype UInt { get; private set; }
		public static SwiftMetatype String { get; private set; }
		public static SwiftMetatype Bool { get; private set; }
		public static SwiftMetatype UnicodeScalar { get; private set; }
		public static SwiftMetatype Float { get; private set; }
		public static SwiftMetatype Double { get; private set; }
		public static SwiftMetatype Int8 { get; private set; }
		public static SwiftMetatype UInt8 { get; private set; }
		public static SwiftMetatype Int16 { get; private set; }
		public static SwiftMetatype UInt16 { get; private set; }
		public static SwiftMetatype Int32 { get; private set; }
		public static SwiftMetatype UInt32 { get; private set; }
		public static SwiftMetatype Int64 { get; private set; }
		public static SwiftMetatype UInt64 { get; private set; }
		public static SwiftMetatype Void { get; private set; }
		public static SwiftMetatype CGFloat { get; private set; }
		public static SwiftMetatype UnsafeRawPointer { get; private set; }
		public static SwiftMetatype UnsafeMutableRawPointer { get; private set; }
		public static SwiftMetatype OpaquePointer { get; private set; }
	}
}

