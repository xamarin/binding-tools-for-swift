using System;
using System.Collections.Generic;
using System.IO;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace BindingNemo {
	public class WriteXml {
		//public static void CreateXmlFile (string moduleName, ModuleInventory mi)
		public static void CreateXmlFile (Dictionary <string, string> libraries)
		{
			var version = "1.0";

			using (StreamWriter sw = new StreamWriter ($"../../Modules/modules.xml")) {
			//using (StreamWriter sw = new StreamWriter ($"./Modules/modules.xml")) {
				sw.WriteXmlIntro (version);

				foreach (var lib in libraries) {
					if (string.IsNullOrEmpty (lib.Value)) {
						Console.WriteLine ($"Skipping \"{lib.Key}\" since it was null or empty");
						continue;
					}
					try {
						var errors = new ErrorHandling ();
						var mi = ModuleInventory.FromFile (lib.Value, errors);
						//WriteXml.WriteXmlFile (lib.Key, mi);
						sw.WriteXmlFile (lib.Key, mi);
					} catch (Exception e) {
						Console.WriteLine ($"Could not create xml for {lib.Key} - {lib.Value}. {e.Message}");
					}
				}

				sw.WriteXmlOutro ();
			}
		}

		////public static void CreateXmlFile (string moduleName, ModuleInventory mi)
		//public static void CreateXmlFile (Dictionary<string, string> libraries)
		//{
		//	using (StreamWriter sw = new StreamWriter ($"../../../Modules/modules.xml")) {
		//		//using (StreamWriter sw = new StreamWriter ($"./Modules/{moduleName}.xml")) {
		//		foreach (var lib in libraries) {
		//			if (string.IsNullOrEmpty (lib.Value)) {
		//				Console.WriteLine ($"Skipping {lib.Key} since it was null or empty");
		//				continue;
		//			}
		//			try {
		//				var errors = new ErrorHandling ();
		//				var mi = ModuleInventory.FromFile (lib.Value, errors);
		//				//WriteXml.WriteXmlFile (lib.Key, mi);
		//				WriteXml.WriteXmlFile (lib.Key, mi);
		//			} catch (Exception e) {
		//				Console.WriteLine ($"Could not create xml for {lib.Key} - {lib.Value}. {e.Message}");
		//			}
		//		}
		//	}
		//}

		////static void WriteXmlFile (string moduleName, List<ClassContents> classesList, List<ClassContents> enumsList, List<ClassContents> structsList, List<ProtocolContents> protocolsList)
		//static void WriteXmlFile (string moduleName, ModuleInventory mi)
		//{
		//	List<ClassContents> classesList = CheckInventory.GetClasses (mi);
		//	List<ClassContents> enumsList = CheckInventory.GetEnums (mi);
		//	List<ClassContents> structsList = CheckInventory.GetStructs (mi);
		//	List<ProtocolContents> protocolsList = CheckInventory.GetProtocols (mi);

		//	var version = "1.011";


		//	using (StreamWriter sw = new StreamWriter ($"../../../Modules/modules.xml")) {
		//		//using (StreamWriter sw = new StreamWriter ($"./Modules/{moduleName}.xml")) {
		//		Console.WriteLine ($"Creating xml output for {moduleName} at ./Modules/modules.xml");
		//		sw.WriteXmlIntro (version);
		//		sw.WriteModuleIntro (moduleName);
		//		sw.WriteClasses (moduleName, classesList);
		//		sw.WriteStructs (moduleName, structsList);
		//		sw.WriteEnums (moduleName, enumsList);
		//		sw.WriteProtocols (moduleName, protocolsList);
		//		sw.WriteXmlOutro ();
		//		sw.WriteModuleOutro ();
		//	}
		//}
	}
}
