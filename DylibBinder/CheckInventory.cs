using System;
using System.Collections.Generic;
using SwiftReflector.Inventory;
using SwiftReflector;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class CheckInventoryValues {
		public SortedSet<ClassContents> Classes { get; } = SortedSetExtensions.Create<ClassContents> ();
		public SortedSet<ClassContents> Structs { get; } = SortedSetExtensions.Create<ClassContents> ();
		public SortedSet<ClassContents> Enums { get; } = SortedSetExtensions.Create<ClassContents> ();
		public SortedSet<ProtocolContents> Protocols { get; } = SortedSetExtensions.Create<ProtocolContents> ();
	}

	internal class CheckInventoryDictionary {
		public SortedDictionary<string, CheckInventoryValues> CheckInventoryDict { get; } = new SortedDictionary<string, CheckInventoryValues> ();

		public CheckInventoryDictionary (ModuleInventory mi)
		{
			GetValues (Exceptions.ThrowOnNull (mi, nameof (mi)));
		}

		void GetValues (ModuleInventory mi)
		{
			foreach (var module in mi.ModuleNames) {
				if (!CheckInventoryDict.ContainsKey (module.Name))
					CheckInventoryDict.Add (module.Name, new CheckInventoryValues ());

				GetClassesStructsEnums (mi, module);
				GetProtocols (mi, module);
			}
		}

		void GetClassesStructsEnums (ModuleInventory mi, SwiftName module)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			Exceptions.ThrowOnNull (module, nameof (module));
			foreach (var elem in mi.ClassesForName (module)) {
				if (!elem.Name.ToFullyQualifiedName ().IsPublic ())
					continue;
				if (elem.Name.IsClass)
					CheckInventoryDict [module.Name].Classes.Add (elem);
				else if (elem.Name.IsStruct)
					CheckInventoryDict [module.Name].Structs.Add (elem);
				else if (elem.Name.IsEnum)
					CheckInventoryDict [module.Name].Enums.Add (elem);
			}
		}

		void GetProtocols (ModuleInventory mi, SwiftName module)
		{
			Exceptions.ThrowOnNull (mi, nameof (mi));
			Exceptions.ThrowOnNull (module, nameof (module));
			foreach (var p in mi.ProtocolsForName (module)) {
				if (!p.Name.ToFullyQualifiedName ().Contains ("_"))
					CheckInventoryDict [module.Name].Protocols.Add (p);
			}
		}
	}
}
