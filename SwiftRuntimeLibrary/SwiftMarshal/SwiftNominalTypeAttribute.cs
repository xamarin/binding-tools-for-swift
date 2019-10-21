// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Class)]
	public class SwiftNominalTypeAttribute : Attribute {
		public SwiftNominalTypeAttribute (string libraryName, string nominalTypeDescriptor, string metadata, string witnessTable)
		{
			if (libraryName == null)
				throw new ArgumentNullException (nameof (libraryName));
			if (nominalTypeDescriptor == null)
				throw new ArgumentNullException (nameof (nominalTypeDescriptor));
			if (metadata == null)
				throw new ArgumentNullException (nameof (metadata));
			if (witnessTable == null)
				throw new ArgumentException (nameof (witnessTable));
			LibraryName = libraryName;
			NominalTypeDescriptor = nominalTypeDescriptor;
			Metadata = metadata;
			WitnessTable = witnessTable;
		}
		// FYI - when I write out [V|O|C][module][class], I'm lying about this.
		// It's actually more complicated than this and it is the representation of a mangled swift class name
		// which is:
		// [TypeSpec]*[module][NameParts]
		// where TypeSpec is one of V, O, C for struct, enum class.
		// NameParts is 1 swift name for each TypeSpec
		// module is a swift name
		// and a swift name is a number prefixed string.
		// Now you know.

		public string LibraryName { get; private set; }
		// This is the mangled name of the nominal type descriptor table
		// usually in the form _TMn[V|O|C][module][class]
		public string NominalTypeDescriptor { get; private set; }
		// This is the mangled name of the direct metadata.
		// It used to be in the form _TMd[V|O|C][module][class], but is now in the form
		// _TM[V|O|C][module][class]
		public string Metadata { get; private set; }

		public string WitnessTable { get; private set; }
	}
}
