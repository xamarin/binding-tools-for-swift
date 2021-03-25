using System;
using System.Collections.Generic;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class CheckInventory {
		public CheckInventory (ModuleInventory mi)
		{
			Classes = SortedSetExtensions.CreateClassSortedSet ();
			Structs = SortedSetExtensions.CreateClassSortedSet ();
			Enums = SortedSetExtensions.CreateClassSortedSet ();
			Protocols = SortedSetExtensions.CreateProtocolSortedSet ();

			GetClassesStructsEnums (mi);
			GetProtocols (mi);
		}

		public SortedSet<ClassContents> Classes { get; }
		public SortedSet<ClassContents> Structs { get; }
		public SortedSet<ClassContents> Enums { get; }
		public SortedSet<ProtocolContents> Protocols { get; }

		void GetClassesStructsEnums (ModuleInventory mi)
		{
			foreach (var m in mi.ModuleNames) {
				foreach (var elem in mi.ClassesForName (m)) {
					if (!elem.Name.ToFullyQualifiedName (true).IsPublic ())
						continue;
					if (elem.Name.IsClass)
						Classes.Add (elem);
					else if (elem.Name.IsStruct)
						Structs.Add (elem);
					else if (elem.Name.IsEnum)
						Enums.Add (elem);
				}
			}
		}

		void GetProtocols (ModuleInventory mi)
		{
			foreach (var m in mi.ModuleNames) {
				foreach (var p in mi.ProtocolsForName (m)) {
					if (!p.Name.ToFullyQualifiedName (true).Contains ("_"))
						Protocols.Add (p);
				}
			}
		}
	}
}
