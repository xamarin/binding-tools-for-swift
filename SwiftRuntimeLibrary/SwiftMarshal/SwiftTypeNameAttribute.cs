using System;
using System.Reflection;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
	public class SwiftTypeNameAttribute : Attribute {
		public SwiftTypeNameAttribute (string swiftName)
		{
			SwiftName = Exceptions.ThrowOnNull (swiftName, nameof (swiftName));
		}

		public string SwiftName { get; private set; }

		public static bool TryGetSwiftName (Type t, out string swiftName)
		{
			Exceptions.ThrowOnNull (t, nameof (t));
			var attr = t.GetCustomAttribute<SwiftTypeNameAttribute> ();
			swiftName = attr != null ? attr.SwiftName : null;
			return swiftName != null;
		}
	}
}
