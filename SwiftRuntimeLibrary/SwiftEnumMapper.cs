// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;

namespace SwiftRuntimeLibrary {
	internal static class SwiftEnumMapper {
		public static bool EnumHasRawValue (Type t)
		{
			if (!t.IsEnum)
				throw new ArgumentException (String.Format ("Type {0} is not an enum.", t.Name), "t");
			return t.GetCustomAttribute<SwiftEnumHasRawValueAttribute> () != null;
		}

		public static Type RawValueType (Type t)
		{
			if (!t.IsEnum)
				throw new ArgumentException (String.Format ("Type {0} is not an enum.", t.Name), "t");

			var attr = t.GetCustomAttribute<SwiftEnumHasRawValueAttribute> ();
			if (attr == null)
				throw new ArgumentException (String.Format ("Type {0} does not have the {1} attribute.",
					t.Name, typeof (SwiftEnumHasRawValueAttribute).Name));
			return attr.RawValueType;
		}

		public static T [] RawValuesOf<T> (Type t, Func<int, T> fetcher)
		{
			if (!EnumHasRawValue (t))
				throw new ArgumentException (String.Format ("Enum type {0} is not marked with {1}.",
					t.Name, typeof (SwiftEnumHasRawValueAttribute).Name));
			int nEnums = t.GetEnumNames ().Length;
			var values = new T [nEnums];
			// if we get here, we're looking at a regular int-based swift enum
			for (int i = 0; i < nEnums; i++) {
				values [i] = fetcher (i);
			}
			return values;
		}
	}
}

