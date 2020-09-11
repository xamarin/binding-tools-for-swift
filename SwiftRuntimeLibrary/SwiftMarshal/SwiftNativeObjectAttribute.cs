// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class SwiftNativeObjectTagAttribute : Attribute {
		public static bool IsSwiftNativeObject (object o)
		{
			if (o == null)
				throw new ArgumentNullException (nameof (o));
			return o.GetType ().GetCustomAttributes (typeof (SwiftNativeObjectTagAttribute), false).Length > 0;
		}
	}
}
