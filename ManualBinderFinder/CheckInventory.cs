using System;
using System.Collections.Generic;
using SwiftReflector.Inventory;

namespace ManualBinderFinder {
	public class CheckInventory {
		public static List<ClassContents> GetClasses (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> classes = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleClass = mi.GetClassesFromName (m);
				foreach (var c in moduleClass) {
					if (!c.Name.ToString ().Contains ("_"))
						classes.Add (c);
				}
			}
			classes.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return classes;
		}

		public static List<ClassContents> GetEnums (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> enums = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleEnum = mi.GetEnumsFromName (m);
				foreach (var e in moduleEnum) {
					if (!e.Name.ToString ().Contains ("_"))
						enums.Add (e);
				}
			}
			enums.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return enums;
		}

		public static List<ClassContents> GetStructs (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> structs = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleStruct = mi.GetStructsFromName (m);
				foreach (var s in moduleStruct) {
					if (!s.Name.ToString ().Contains ("_"))
						structs.Add (s);
				}
			}
			structs.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return structs;
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
