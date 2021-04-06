// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Xamarin;
using SwiftReflector.ExceptionTools;
using SwiftReflector.IOUtils;
using SwiftReflector.Demangling;
using ObjCRuntime;
using SwiftRuntimeLibrary;

namespace SwiftReflector.Inventory {
	public class ModuleInventory : Inventory<ModuleContents> {
		public MachO.Architectures Architecture { get; private set; }
		public int SizeofMachinePointer {
			get {
				return Architecture == MachO.Architectures.x86_64 ||
				Architecture == MachO.Architectures.ARM64 ? 8 : 4;
			}
		}

		public override void Add (TLDefinition tld, Stream srcStm)
		{
			ModuleContents module = null;
			if (!values.TryGetValue (tld.Module, out module)) {
				module = new ModuleContents (tld.Module, SizeofMachinePointer);
				//Console.WriteLine ($"Type {tld.Type} added to dictionary in Add ModuleInventory");
				values.Add (tld.Module, module);
			}
			module.Add (tld, srcStm);
		}

		public IEnumerable<SwiftName> ModuleNames {
			get { return values.Keys; }
		}

		public IEnumerable<ClassContents> Classes {
			get {
				return Values.SelectMany (modc => modc.Classes.Values);
			}
		}

		public IEnumerable<ClassContents> ClassesForName (SwiftName modName)
		{
			ModuleContents modcont = null;
			if (values.TryGetValue (modName, out modcont)) {
				return modcont.Classes.Values;
			} else {
				return new ClassContents [0];
			}
		}

		public IEnumerable<ProtocolContents> ProtocolsForName (SwiftName modName)
		{
			ModuleContents modcont = null;
			if (values.TryGetValue (modName, out modcont)) {
				return modcont.Protocols.Values;
			} else {
				return new ProtocolContents [0];
			}
		}

		// Change introduced by TJ Lambert
		// GetClassesFromName, GetEnumsFromName, and GetStructsFromName
		// is similar to ClassesForName () but it takes advantage
		// of the IsClass, IsEnum, and IsStruct bool set
		// inside ClassContent to filter classes, enums, and structs

		public IEnumerable<ClassContents> GetClassesFromName (SwiftName modName)
		{
			List<ClassContents> classes = new List<ClassContents> ();
			ModuleContents modcont = null;
			if (values.TryGetValue (modName, out modcont)) {
				foreach (var v in modcont.Classes.Values){
					if (v.Name.IsClass)
						classes.Add (v);
				}
			} 
			return classes;
		}

		public IEnumerable<ClassContents> GetEnumsFromName (SwiftName modName)
		{
			List<ClassContents> enums = new List<ClassContents> ();
			ModuleContents modcont = null;
			if (values.TryGetValue (modName, out modcont)) {
				foreach (var v in modcont.Classes.Values) {
					if (v.Name.IsEnum)
						enums.Add (v);
				}
			}
			return enums;
		}

		public IEnumerable<ClassContents> GetStructsFromName (SwiftName modName)
		{
			List<ClassContents> structs = new List<ClassContents> ();
			ModuleContents modcont = null;
			if (values.TryGetValue (modName, out modcont)) {
				foreach (var v in modcont.Classes.Values) {
					if (v.Name.IsStruct)
						structs.Add (v);
				}
			}
			return structs;
		}

		public ClassContents FindClass (string fullyQualifiedName)
		{
			string moduleName = fullyQualifiedName.Substring (0, fullyQualifiedName.IndexOf ('.'));
			ModuleContents modcont = null;
			if (values.TryGetValue (new SwiftName (moduleName, false), out modcont)) {
				return modcont.Classes.Values.Where (cc => cc.Name.ToFullyQualifiedName (true) == fullyQualifiedName).FirstOrDefault ();
			} else {
				return null;
			}
		}

		public ProtocolContents FindProtocol (string fullyQualifiedName)
		{
			string moduleName = fullyQualifiedName.ModuleFromName ();
			ModuleContents modcont = null;
			if (values.TryGetValue (new SwiftName (moduleName, false), out modcont)) {
				return modcont.Protocols.Values.Where (cc => cc.Name.ToFullyQualifiedName (true) == fullyQualifiedName).FirstOrDefault ();
			} else {
				return null;
			}

		}

		public static ModuleInventory FromFile (string pathToDynamicLibrary, ErrorHandling errors)
		{
			Exceptions.ThrowOnNull (pathToDynamicLibrary, nameof(pathToDynamicLibrary));
			FileStream stm = null;
			try {
				stm = new FileStream (pathToDynamicLibrary, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			} catch (Exception e) {
				errors.Add (ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 57, e, "unable to open file {0}: {1} \n{e.Message}", pathToDynamicLibrary, e.Message));
			}
			try {
				return FromStream (stm, errors, pathToDynamicLibrary);
			} finally {
				stm.Dispose ();
			}
		}

		public static ModuleInventory FromFiles (IEnumerable<string> pathsToLibraryFiles, ErrorHandling errors)
		{
			ModuleInventory inventory = null;
			foreach (string path in pathsToLibraryFiles) {
				if (inventory == null)
					inventory = new ModuleInventory ();
				using (FileStream stm = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					FromStreamInto (stm, inventory, errors, path);
				}
			}
			return inventory;
		}

		static ModuleInventory FromStreamInto (Stream stm, ModuleInventory inventory,
		                                       ErrorHandling errors, string fileName = null)
		{
			Exceptions.ThrowOnNull (errors, "errors");
			Exceptions.ThrowOnNull (stm, "stm");
			OffsetStream osstm = null;
			List<NListEntry> entries = null;
			try {
				List<MachOFile> macho = MachO.Read (stm, null).ToList ();
				MachOFile file = macho [0];

				List<SymTabLoadCommand> symbols = file.load_commands.OfType<SymTabLoadCommand> ().ToList ();
				NListEntryType nlet = symbols [0].nlist [0].EntryType;
				entries = symbols [0].nlist.
					Where ((nle, i) => nle.IsPublic && nle.EntryType == NListEntryType.InSection).ToList ();
				inventory.Architecture = file.Architecture;
				osstm = new OffsetStream (stm, file.StartOffset);
			} catch (Exception e) {
				errors.Add (ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 58, e, "Unable to retrieve functions from {0}: {1}",
					fileName ?? "stream", e.Message));
				return inventory;
			}

			bool isOldVersion = IsOldVersion (entries);

			foreach (var entry in entries) {
				if (!entry.IsSwiftEntryPoint ())
					continue;
				TLDefinition def = null;
				try {
					def = Decomposer.Decompose (entry.str, isOldVersion, Offset (entry));
				} catch (RuntimeException e) {
					var except = new RuntimeException (e.Code, false, e, $"error decomposing {entry.str}: {e.Message}, skipping");
					errors.Add (except);
				}
				catch (Exception e) {
					var except = new RuntimeException (ReflectorError.kDecomposeBase + 0, false, e, $"unexpected error handling {entry.str}: {e.Message}, skipping.");
					errors.Add (except);
				}
				if (def != null) {
					// this skips over privatized names
					var tlf = def as TLFunction;
					if (tlf != null && tlf.Name != null && tlf.Name.Name.Contains ("..."))
						continue;
					try {
						inventory.Add (def, osstm);
					} catch (RuntimeException e) {
						e = new RuntimeException (e.Code, e.Error, $"error dispensing top level definition of type {def.GetType ().Name} decomposed from {entry.str}: {e.Message}");
						errors.Add (e);
					}
				} else {
					var ex = ErrorHelper.CreateWarning (ReflectorError.kInventoryBase + 18, $"entry {entry.str} uses an unsupported swift feature, skipping.");
					errors.Add (ex);
				}
			}

			return inventory;
		}

		public static ModuleInventory FromStream (Stream stm, ErrorHandling errors, string fileName = null)
		{
			ModuleInventory inventory = new ModuleInventory ();
			return FromStreamInto (stm, inventory, errors, fileName);
		}

		static ulong Offset (NListEntry entry)
		{
			NListEntry32 nl32 = entry as NListEntry32;
			return nl32 != null ? nl32.n_value : ((NListEntry64)entry).n_value;
		}

		static bool IsOldVersion (List<NListEntry> entries)
		{
			foreach (NListEntry entry in entries) {
				if (entry.str.StartsWith ("__TMd", StringComparison.Ordinal))
					return true;
			}
			return false;
		}
	}
}

