using System;

namespace ManualBinderFinder {
	public class ClassInfo {
		public ClassInfo ()
		{
		}

		public string Name { get; set; }
		public string Signature { get; set; }
		public bool IsStatic { get; set; }
		public string ReturnType { get; set; }
		public string [] Parameters { get; set; }
	}
}
