using System;
using System.Collections.Generic;
using SwiftReflector.Inventory;
using SwiftReflector;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class CheckInventory {
		public SortedSet<ClassContents> Classes { get; } = SortedSetExtensions.Create<ClassContents> ();
		public SortedSet<ClassContents> Structs { get; } = SortedSetExtensions.Create<ClassContents> ();
		public SortedSet<ClassContents> Enums { get; } = SortedSetExtensions.Create<ClassContents> ();
		public SortedSet<ProtocolContents> Protocols { get; } = SortedSetExtensions.Create<ProtocolContents> ();

		public CheckInventory (ModuleInventory mi, SwiftName module)
		{
			GetValues (Exceptions.ThrowOnNull (mi, nameof (mi)), Exceptions.ThrowOnNull (module, nameof (module)));
		}

		void GetValues (ModuleInventory mi, SwiftName module)
		{
			GetClassesStructsEnums (mi, module);
			GetProtocols (mi, module);
		}

		void GetClassesStructsEnums (ModuleInventory mi, SwiftName module)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			Exceptions.ThrowOnNull (module, nameof (module));
			foreach (var elem in mi.ClassesForName (module)) {
				if (!elem.Name.ToFullyQualifiedName ().IsPublic ())
					continue;
				if (elem.Name.IsClass)
					Classes.Add (elem);
				else if (elem.Name.IsStruct)
					Structs.Add (elem);
				else if (elem.Name.IsEnum)
					Enums.Add (elem);
			}
		}

		void GetProtocols (ModuleInventory mi, SwiftName module)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			Exceptions.ThrowOnNull (module, nameof (module));
			foreach (var p in mi.ProtocolsForName (module)) {
				if (p.Name.ToFullyQualifiedName ().IsPublic ())
					Protocols.Add (p);
			}
		}

		public static List<OverloadInventory> GetGlobalFunctions (ModuleInventory mi, SwiftName module)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			Exceptions.ThrowOnNull (module, nameof (module));
			var functions = new List<OverloadInventory> ();

			foreach (var f in mi.FunctionsForName (module)) {
				if (f.Name.Name.IsPublic ()) {
					functions.Add (f);
				}
			}
			return functions;
		}
	}
}
