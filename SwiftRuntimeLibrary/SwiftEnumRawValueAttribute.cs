using System;

namespace SwiftRuntimeLibrary {
	[AttributeUsage (AttributeTargets.Enum, AllowMultiple = false)]
	public sealed class SwiftEnumHasRawValueAttribute : Attribute {
		public Type RawValueType { get; private set; }
		public SwiftEnumHasRawValueAttribute (Type t)
		{
			RawValueType = t;
		}
	}
}

