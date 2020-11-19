using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Options;
using System.Reflection;
using System.Diagnostics;

namespace ManualBinderFinder {
	class Program {
		static void Main (string [] args)
		{
			ManualBinderFinderOptions options = new ManualBinderFinderOptions ();
			var extra = options.optionsSet.Parse (args);
			//options.dylibLibrary = "libswiftCoreGraphics";

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return;
			}

			if (options.dylibLibraryList.Count == 0){
				Console.WriteLine ("library is empty. Try --library=<dylib name>");
				return;
			}

			foreach (string lib in options.dylibLibraryList) {
				string libName = lib;
				if (!libName.Contains (".dylib")) {
					libName += ".dylib";
				}
				var shellOutput = Shell.RunBash ($"find ../SwiftToolchain*/ -iname {libName}");

				//var shellOutput = $"find ../../../../SwiftToolchain*/ -iname {options.dylibLibrary}".Bash ();
				if (!shellOutput.Contains (".dylib")) {
					Console.WriteLine ($"{libName} was not valid");
					return;
				}
				string dylibPath = shellOutput.Substring (0, shellOutput.IndexOf (Environment.NewLine));
				string dylibName = dylibPath.Split ('/').Last ().Split ('.').First ();
				var errors = new ErrorHandling ();

				var mi = ModuleInventory.FromFile (dylibPath, errors);

				CreateXmlFile (dylibName, mi);
			}
			
		}

		static void CreateXmlFile (string moduleName, ModuleInventory mi)
		{
			var miClasses = mi.Classes;
			var miModules = mi.ModuleNames;
			List<string> classesList = new List<string> ();
			List<string> enumsList = new List<string> ();
			List<string> structsList = new List<string> ();
			List<string> protocolList = new List<string> ();

			foreach (var m in miModules) {
				var moduleClass = mi.GetClassesFromName (m);
				foreach (var c in moduleClass) {
					classesList.Add (c.Name.ToString ());
				}
			}
			classesList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));

			foreach (var m in miModules) {
				var moduleProto = mi.ProtocolsForName (m);
				foreach (var p in moduleProto) {
					protocolList.Add (p.Name.ToString ());
				}
			}
			protocolList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));

			foreach (var m in miModules) {
				var moduleEnum = mi.GetEnumsFromName (m);
				foreach (var e in moduleEnum) {
					enumsList.Add (e.Name.ToString ());
				}
			}
			enumsList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));

			foreach (var m in miModules) {
				var moduleStruct = mi.GetStructsFromName (m);
				foreach (var s in moduleStruct) {
					structsList.Add (s.Name.ToString ());
				}
			}
			structsList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));

			//using (StreamWriter sw = new StreamWriter ($"../../../Modules/{moduleName}.xml")) {
			using (StreamWriter sw = new StreamWriter ($"./Modules/{moduleName}.xml")) {
				Console.WriteLine ($"Creating xml output for {moduleName} at ./Modules/{moduleName}.xml");
				sw.WriteLine ("<manualbinderfinder version=\"1.01\" encoding=\"UTF - 8\">");
				sw.WriteLine ($"<Module name=\"{moduleName}\">");

				sw.WriteLine ($"    <Classes>");
				foreach (var c in classesList) {
					sw.WriteLine ($"        <Class name=\"{c}\"/>");
				}
				sw.WriteLine ($"    </Classes>");

				sw.WriteLine ($"    <Structs>");
				foreach (var s in structsList) {
					sw.WriteLine ($"        <Struct name=\"{s}\"/>");
				}
				sw.WriteLine ($"    </Structs>");

				sw.WriteLine ($"    <Enums>");
				foreach (var e in enumsList) {
					sw.WriteLine ($"        <Enum name=\"{e}\"/>");
				}
				sw.WriteLine ($"    </Enums>");

				sw.WriteLine ($"    <Protocols>");
				foreach (var p in protocolList) {
					sw.WriteLine ($"        <Protocol name=\"{p}\"/>");
				}
				sw.WriteLine ($"    </Protocols>");

				sw.WriteLine ($"</Module>");
			}
		}
	}

}
