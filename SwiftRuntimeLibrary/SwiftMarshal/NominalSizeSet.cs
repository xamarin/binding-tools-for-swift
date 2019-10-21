using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	class NominalSizeStride {
		NominalSizeStride () { }

		public int Size { get; private set; }
		public int Stride { get; private set; }
		public int Alignment { get; private set; }

		public static NominalSizeStride FromDylib (DynamicLib lib, string witnessTableName)
		{
			var wit = SwiftValueWitnessTable.FromDylib (lib, witnessTableName);
			if (wit == null) {
				throw new SwiftRuntimeException (String.Format ("Unable to find witness table entry {0} in library {1}.",
					witnessTableName, lib.FileName));
			}
			return new NominalSizeStride { Size = (int)wit.Size, Stride = (int)wit.Stride };
		}
		public static NominalSizeStride FromDylibFile (string file, DLOpenMode mode, string witnessTableName)
		{
			using (DynamicLib lib = new DynamicLib (file, mode)) {
				return FromDylib (lib, witnessTableName);
			}
		}
		public static NominalSizeStride FromType (Type t)
		{
			var wit = StructMarshal.ValueWitnessof (t);
			return new NominalSizeStride { Size = (int)wit.Size, Stride = (int)wit.Stride, Alignment = wit.Alignment };
		}
	}
}

