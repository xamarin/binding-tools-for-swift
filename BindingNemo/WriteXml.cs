using System;
using System.Collections.Generic;
using System.IO;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace BindingNemo {
	public class WriteXml {
		public static void CreateXmlFile (Dictionary <string, string> libraries)
		{
			var version = "1.0";

			//using (StreamWriter sw = new StreamWriter ($"../../Modules/modules.xml")) {
			using (StreamWriter sw = new StreamWriter ($"Modules/modules.xml")) {
				sw.WriteXmlIntro (version);

				foreach (var lib in libraries) {
					if (string.IsNullOrEmpty (lib.Value)) {
						Console.WriteLine ($"Skipping \"{lib.Key}\" since it was null or empty");
						continue;
					}
					try {
						var errors = new ErrorHandling ();
						var mi = ModuleInventory.FromFile (lib.Value, errors);
						sw.WriteXmlFile (lib.Key, mi);
					} catch (Exception e) {
						Console.WriteLine ($"Could not create xml for {lib.Key} - {lib.Value}. {e.Message}");
					}
				}

				sw.WriteXmlOutro ();
			}
		}
	}
}
