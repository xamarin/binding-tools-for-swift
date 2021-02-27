using System;
using System.Collections.Generic;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class CheckInventory {

		public static (List<ClassContents>, List<ClassContents>, List<ClassContents>) GetClassesStructsEnums (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> classes = new List<ClassContents> ();
			List<ClassContents> structs = new List<ClassContents> ();
			List<ClassContents> enums = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleClass = mi.ClassesForName (m);
				foreach (var elem in moduleClass) {
					if (elem.Name.ToString ().Contains ("_"))
						continue;
					if (elem.Name.IsClass)
						classes.Add (elem);
					else if (elem.Name.IsStruct)
						structs.Add (elem);
					else if (elem.Name.IsEnum)
						enums.Add (elem);
				}
			}
			classes.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			structs.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			enums.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return (classes, structs, enums);
		}

		public static List<ProtocolContents> GetProtocols (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ProtocolContents> protocols = new List<ProtocolContents> ();

			foreach (var m in modules) {
				var moduleProtocol = mi.ProtocolsForName (m);
				foreach (var p in moduleProtocol) {
					if (!p.Name.ToString ().Contains ("_"))
						protocols.Add (p);
				}
			}
			protocols.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return protocols;
		}
	}
}
