// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.IO;
using System.Diagnostics;
using Xamarin;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework.Legacy;

namespace tomwiftytest {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class MachOTests {
		public static Stream HelloSwiftAsLibrary (string extraOptions)
		{
			return Compiler.CompileStringUsing (null, XCodeCompiler.Swiftc, Compiler.kHelloSwift, extraOptions);
		}

		[Test]
		public void FromSwiftLibraryMacOS ()
		{
			Stream lib = HelloSwiftAsLibrary (null);
			List<MachOFile> macho = MachO.Read (lib, null).ToList ();
			ClassicAssert.IsNotNull (macho);
			ClassicAssert.AreEqual (1, macho.Count);
		}

		[Test]
		public void ContainsASymbolTable ()
		{
			Stream lib = HelloSwiftAsLibrary (null);
			List<MachOFile> macho = MachO.Read (lib, null).ToList ();
			ClassicAssert.IsNotNull (macho);
			ClassicAssert.AreEqual (1, macho.Count);
			MachOFile file = macho [0];
			bool hasSymbolTable = file.load_commands.Exists (lc => lc.cmd == (uint)MachO.LoadCommands.SymTab ||
				lc.cmd == (uint)MachO.LoadCommands.DySymTab);
			ClassicAssert.IsTrue (hasSymbolTable);
		}


		[Test]
		public void HasOneRealPublicSymbol ()
		{
			Stream lib = HelloSwiftAsLibrary (null);
			List<MachOFile> macho = MachO.Read (lib, null).ToList ();
			ClassicAssert.IsNotNull (macho);
			ClassicAssert.AreEqual (1, macho.Count);
			MachOFile file = macho [0];
			List<SymTabLoadCommand> symbols = file.load_commands.OfType<SymTabLoadCommand> ().ToList ();
			ClassicAssert.AreEqual (1, symbols.Count);
			NListEntryType nlet = symbols [0].nlist [0].EntryType;
			List<NListEntry> entries = symbols [0].nlist.
				Where ((nle, i) => nle.IsPublic && nle.EntryType == NListEntryType.InSection).ToList ();
			ClassicAssert.AreEqual (1, entries.Count);
		}

	}
}

