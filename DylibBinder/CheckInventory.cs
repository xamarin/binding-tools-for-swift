using System;
using System.Collections.Generic;
using SwiftReflector.Inventory;

namespace DylibBinder {
	internal class CheckInventory {
		public CheckInventory (ModuleInventory mi)
		{
			Classes = new List<ClassContents> ();
			Structs = new List<ClassContents> ();
			Enums = new List<ClassContents> ();
			Protocols = new List<ProtocolContents> ();

			GetClassesStructsEnums (mi);
			GetProtocols (mi);
		}

		public List<ClassContents> Classes { get; }
		public List<ClassContents> Structs { get; }
		public List<ClassContents> Enums { get; }
		public List<ProtocolContents> Protocols { get; }

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
			SortNominalTypeLists (Classes, Structs, Enums);
		}

		void GetProtocols (ModuleInventory mi)
		{
			foreach (var m in mi.ModuleNames) {
				foreach (var p in mi.ProtocolsForName (m)) {
					if (!p.Name.ToFullyQualifiedName (true).Contains ("_"))
						Protocols.Add (p);
				}
			}
			SortNominalTypeLists (Protocols);
		}

		void SortNominalTypeLists (List<ProtocolContents> protocolList)
		{
			if (protocolList != null)
				protocolList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToFullyQualifiedName (true), type2.Name.ToFullyQualifiedName (true)));
		}

		void SortNominalTypeLists (params List<ClassContents> [] nominalTypeLists)
		{
			foreach (var nominalTypeList in nominalTypeLists) {
				nominalTypeList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToFullyQualifiedName (true), type2.Name.ToFullyQualifiedName (true)));
			}
		}
	}
}
