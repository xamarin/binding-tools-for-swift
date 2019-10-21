using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xamarin;

namespace SwiftReflector {
	public class FileEntryPair {
		public FileEntryPair (MachOFile file, NListEntry entry)
		{
			File = file;
			Entry = entry;
		}
		public MachOFile File { get; private set; }
		public NListEntry Entry { get; private set; }
	}

	public class FileSymbolPair {
		public FileSymbolPair (MachOFile file, string symbol)
		{
			File = file;
			Symbol = symbol;
		}

		public MachOFile File { get; private set; }
		public string Symbol { get; private set; }
	}

	// Visitor for every symbol table entry
	public class SymbolVisitor {
		public static bool AllFiles (MachOFile file) { return true; }
		public static bool AllPublic (NListEntry entry)
		{
			return entry.IsPublic && entry.EntryType == NListEntryType.InSection;
		}
		public static bool AllPublicSwift (NListEntry entry)
		{
			return entry.IsPublic && entry.EntryType == NListEntryType.InSection && entry.IsSwiftEntryPoint ();
		}

		public static Func<MachOFile, bool> OfArchitecture (MachO.Architectures arch)
		{
			return mf => mf.Architecture == arch;
		}

		public static IEnumerable<FileEntryPair> Entries (Stream stm, Func<NListEntry, bool> entryFilter = null,
			Func<MachOFile, bool> fileFilter = null)
		{
			fileFilter = fileFilter ?? AllFiles;
			entryFilter = entryFilter ?? AllPublic;
			return from file in MachO.Read (stm) where fileFilter (file)
			       from symTab in file.load_commands.OfType<SymTabLoadCommand> ()
			       from entry in symTab.nlist where entryFilter (entry)
			       select new FileEntryPair (file, entry);
		}

		public static IEnumerable<FileSymbolPair> Symbols (Stream stm, Func<NListEntry, bool> entryFilter = null,
			Func<MachOFile, bool> fileFilter = null)
		{
			fileFilter = fileFilter ?? AllFiles;
			entryFilter = entryFilter ?? AllPublic;
			return from file in MachO.Read (stm) where fileFilter (file)
			       from symTab in file.load_commands.OfType<SymTabLoadCommand> ()
			       from entry in symTab.nlist where entryFilter (entry)
			       select new FileSymbolPair (file, entry.str);
		}

		public static IEnumerable<FileSymbolPair> SwiftSymbols (Stream stm, Func<MachOFile, bool> fileFilter = null)
		{
			return Symbols (stm, AllPublicSwift, fileFilter);
		}
	}
}

