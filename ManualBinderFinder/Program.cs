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

			if (string.IsNullOrEmpty (options.dylibLibrary)){
				Console.WriteLine ("library is empty. Try --library=<dylib name>");
				return;
			}

			if (!options.dylibLibrary.Contains (".dylib")) {
				options.dylibLibrary += ".dylib";
			}

			var shellOutput = $"find ../SwiftToolchain*/ -iname {options.dylibLibrary}".Bash ();
			//var shellOutput = $"find ../../../../SwiftToolchain*/ -iname {options.dylibLibrary}".Bash ();
			if (!shellOutput.Contains (".dylib")) {
				Console.WriteLine ("dylib was not valid");
				return;
			}
			string dylibPath = shellOutput.Substring (0, shellOutput.IndexOf (Environment.NewLine));
			string dylibName = dylibPath.Split ('/').Last ().Split ('.').First ();
			var errors = new ErrorHandling ();

			var mi = ModuleInventory.FromFile (dylibPath, errors);

			CreateXmlFile (dylibName, mi);
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
				//Console.WriteLine ($"moduleClass has {moduleClass.Count()} elements");
				foreach (var c in moduleClass) {
					classesList.Add (c.Name.ToString ());
				}
			}
			//Console.WriteLine ($"classesList has {classesList.Count} elements");
			classesList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
			//Console.WriteLine ($"classesList STILL has {classesList.Count} elements");

			foreach (var m in miModules) {
				var moduleProto = mi.ProtocolsForName (m);
				//Console.WriteLine ($"moduleProto has {moduleProto.Count ()} elements");
				foreach (var p in moduleProto) {
					protocolList.Add (p.Name.ToString ());
				}
			}
			//Console.WriteLine ($"protocolList has {protocolList.Count} elements");
			protocolList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
			//Console.WriteLine ($"protocolList Still has {protocolList.Count} elements");

			foreach (var m in miModules) {
				var moduleEnum = mi.GetEnumsFromName (m);
				//Console.WriteLine ($"moduleEnum has {moduleEnum.Count ()} elements");
				foreach (var e in moduleEnum) {
					enumsList.Add (e.Name.ToString ());
				}
			}
			//Console.WriteLine ($"enumsList has {enumsList.Count} elements");
			enumsList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
			//Console.WriteLine ($"enumsList STILL has {enumsList.Count} elements");

			foreach (var m in miModules) {
				var moduleStruct = mi.GetStructsFromName (m);
				//Console.WriteLine ($"moduleStruct has {moduleStruct.Count ()} elements");
				foreach (var s in moduleStruct) {
					structsList.Add (s.Name.ToString ());
				}
			}
			//Console.WriteLine ($"structsList has {structsList.Count} elements");
			structsList.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
			//Console.WriteLine ($"structsList STILL has {structsList.Count} elements");

			//using (StreamWriter sw = new StreamWriter ($"../../../Modules/{moduleName}.xml")) {
			using (StreamWriter sw = new StreamWriter ($"./Modules/{moduleName}.xml")) {
				Console.WriteLine ($"Creating xml output for {moduleName} at ./Modules/{moduleName}.xml");
				sw.WriteLine ("<manualbinderfinder version=\"1.0\" encoding=\"UTF - 8\">");
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
//Console.WriteLine ($"{c.Name}: {c.Constructors} {modc.ClassConstructor} {modc.Destructors} {modc.Properties} {modc.StaticProperties} {modc.PrivateProperties} {modc.StaticPrivateProperties} {modc.Subscripts} {modc.PrivateSubscripts} {modc.Methods} {modc.StaticFunctions} {modc.WitnessTable} {modc.LazyCacheVariable} {modc.DirectMetadata} {modc.Metaclass} {modc.TypeDescriptor} {modc.FunctionsOfUnknownDestination} {modc.DefinitionsOfUnknownDestination} {modc.Variables} {modc.PropertyDescriptors} {modc.Initializers} {modc.ProtocolConformanceDescriptors} {modc.MethodDescriptors}");
