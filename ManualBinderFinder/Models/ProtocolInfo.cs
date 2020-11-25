using System;
namespace ManualBinderFinder.Models {
	public class ProtocolInfo {
		public ProtocolInfo ()
		{
		}

		public string Name { get; set; }
		public string Signature { get; set; }
		public bool IsStatic { get; set; }
	}
}
