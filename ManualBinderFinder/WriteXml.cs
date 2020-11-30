using System;
using System.Collections.Generic;
using System.IO;
using SwiftReflector.Inventory;

namespace ManualBinderFinder {
	public class WriteXml {
		public static void CreateXmlFile (string moduleName, ModuleInventory mi)
		{
			List<ClassContents> classesList = CheckInventory.GetClasses (mi);
			List<ClassContents> enumsList = CheckInventory.GetEnums (mi);
			List<ClassContents> structsList = CheckInventory.GetStructs (mi);
			List<ProtocolContents> protocolList = CheckInventory.GetProtocols (mi);
			WriteXmlFile (moduleName, classesList, enumsList, structsList, protocolList);
		}

		static void WriteXmlFile (string moduleName, List<ClassContents> classesList, List<ClassContents> enumsList, List<ClassContents> structsList, List<ProtocolContents> protocolsList)
		{
			var version = "1.011";
			using (StreamWriter sw = new StreamWriter ($"../../../Modules/{moduleName}.xml")) {
			//using (StreamWriter sw = new StreamWriter ($"./Modules/{moduleName}.xml")) {
				Console.WriteLine ($"Creating xml output for {moduleName} at ./Modules/{moduleName}.xml");
				sw.WriteXmlIntro (moduleName, version);
				sw.WriteClasses (moduleName, classesList);
				sw.WriteStructs (moduleName, structsList);
				sw.WriteEnums (moduleName, enumsList);
				sw.WriteProtocols (moduleName, protocolsList);
				sw.WriteXmlOutro ();
			}
		}
	}
}
