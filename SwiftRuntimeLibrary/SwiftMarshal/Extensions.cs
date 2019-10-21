using System;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	internal static class Extensions {
		public static bool IsTuple (this Type type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof(type));

			if (type == typeof (Tuple))
				return true;

			if (type != null) {
				if (type.IsGenericType) {
					var genType = type.GetGenericTypeDefinition ();
					if (genType == typeof (Tuple<>)
						|| genType == typeof (Tuple<,>)
						|| genType == typeof (Tuple<,,>)
						|| genType == typeof (Tuple<,,,>)
						|| genType == typeof (Tuple<,,,,>)
						|| genType == typeof (Tuple<,,,,,>)
						|| genType == typeof (Tuple<,,,,,,>)
						|| genType == typeof (Tuple<,,,,,,,>)
						|| genType == typeof (Tuple<,,,,,,,>))
						return true;
				}

				type = type.BaseType;
			}
			return false;
		}

		public static bool IsDelegate (this Type t)
		{
			return typeof (Delegate).IsAssignableFrom (t);
		}

		public static T [] Slice<T> (this T [] data, int index, int length, bool withSinglePadAtEnd = false)
		{
			//			#if DEBUG
			//			Console.WriteLine("Slice: array of length {0}, index {1}, take {2}.", data.Length, index, length);
			//			#endif
			if (index < 0 || index >= data.Length)
				throw new ArgumentOutOfRangeException (nameof(length));
			if (length < 0 || index + length > data.Length)
				throw new ArgumentOutOfRangeException (nameof (length));
			T [] result = new T [length + (withSinglePadAtEnd ? 1 : 0)];
			Array.Copy (data, index, result, 0, length);
			return result;
		}

		internal static void WriteIntPtr (this byte [] data, IntPtr value, int offset = 0)
		{
			if (data == null)
				throw new ArgumentNullException (nameof (data));
			if (offset < 0)
				throw new ArgumentOutOfRangeException (nameof (offset), "'offset' can't be negative.");
			if (data.Length < IntPtr.Size * (offset + 1))
				throw new ArgumentOutOfRangeException (nameof (data), "'data' isn't big enough to write the value at the specified offset.");
			unsafe {
				fixed (byte* ptr = data)
					((IntPtr *) ptr) [offset] = value;
			}
		}
	}
}

