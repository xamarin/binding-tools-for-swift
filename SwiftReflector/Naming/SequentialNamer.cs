using System;
using System.Threading;
using SwiftReflector.Demangling;

#nullable enable

namespace SwiftReflector.Naming {
	public class SequentialNamer {
		int current;
		string prefix;

		public SequentialNamer (string prefix, int start = 0)
		{
			this.prefix = prefix;
			current = start;
		}

		public string SafeName (string name)
		{
			return CSSafeNaming.SafeIdentifier ($"{prefix}{name}{Interlocked.Increment (ref current)}");
		}
	}

	public class PInvokeNamer : SequentialNamer {
		public PInvokeNamer (int start = 0) : base ("PI", start)
		{
		}

		public string SafeName (TLFunction tlf)
		{
			var module = tlf.Module.Name ?? "NoModule";
			var name = tlf.Name.Name ?? "Anonymous";
			return SafeName ($"_{module}_{name}");			
		}
	}
}

