using System;
namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class SwiftNativeObjectAttribute : Attribute {
		public static bool IsSwiftNativeObject (object o)
		{
			if (o == null)
				throw new ArgumentNullException (nameof (o));
			return o.GetType ().GetCustomAttributes (typeof (SwiftNativeObjectAttribute), false).Length > 0;
		}
	}
}
