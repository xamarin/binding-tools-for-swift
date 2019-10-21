using System;
namespace SwiftRuntimeLibrary {
	public class SwiftException : Exception {
		public SwiftException (string message, SwiftError error)
			: base (message)
		{
			Error = error;
		}

		public SwiftError Error { get; private set; }
	}
}
