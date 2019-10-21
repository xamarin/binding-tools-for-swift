using System;
namespace SwiftRuntimeLibrary {
	internal class SymbolicatorInfoAttribute : Attribute {
		public string DemangledString { get; set; }
		public bool Skip { get; set; } = false;

		public SymbolicatorInfoAttribute (string demangledString)
		{
			DemangledString = demangledString;
		}

		public SymbolicatorInfoAttribute () { }

#if SYMBOLICATOR
		public override string ToString ()
		{
			if (Skip)
			    return "\t\t[SymbolicatorInfo (Skip = true)]";
			return $"\t\t[SymbolicatorInfo (\"{DemangledString}\")]";
		}
#endif
	}
}
