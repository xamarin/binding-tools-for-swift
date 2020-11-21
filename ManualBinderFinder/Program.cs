using System;
using SwiftReflector.Inventory;
using SwiftReflector;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace ManualBinderFinder {
	class Program {
		static void Main (string [] args)
		{
			ManualBinderFinderOptions options = new ManualBinderFinderOptions ();
			var extra = options.optionsSet.Parse (args);
			//options.dylibLibraryList = new List<string> () { "libswiftCore" };

			if (options.PrintHelp) {
				options.PrintUsage (Console.Out);
				return;
			}

			if (options.dylibLibraryList.Count == 0){
				Console.WriteLine ("library is empty. Try --library=<dylib name>");
				return;
			}

			if (string.IsNullOrEmpty (options.platform)) {
				options.platform = "all";
			} else if (options.validPlatform.Contains (options.platform.ToLower ())) {
				Console.WriteLine ("Platform was not recognized");
				return;
			}

			if (string.IsNullOrEmpty (options.architecture)) {
				options.architecture = "all";
			} else if (options.validArchitecture.Contains (options.architecture.ToLower ())) {
				Console.WriteLine ("Architecture was not recognized");
				return;
			}

			Console.WriteLine ($"{options.platform}");
			Console.WriteLine ($"{options.architecture}");

			foreach (string lib in options.dylibLibraryList) {
				string libName = lib;
				if (!libName.Contains (".dylib")) {
					libName += ".dylib";
				}
				var shellOutput = Shell.RunBash ($"find ../SwiftToolchain*/ -iname {libName}");
				//var shellOutput = Shell.RunBash ($"find ../../../../SwiftToolchain*/ -iname {libName}");

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
			List<ClassContents> classesList = GetClasses (mi);
			List<ClassContents> enumsList = GetEnums (mi);
			List<ClassContents> structsList = GetStructs (mi);
			List<ProtocolContents> protocolList = GetProtocols (mi);

			WriteXmlFile (moduleName, classesList, enumsList, structsList, protocolList);
			
		}

		static List<ClassContents> GetClasses (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> classes = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleClass = mi.GetClassesFromName (m);
				foreach (var c in moduleClass) {
					classes.Add (c);
				}
			}
			classes.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return classes;
		}

		static List<ClassContents> GetEnums (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> enums = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleEnum = mi.GetClassesFromName (m);
				foreach (var e in moduleEnum) {
					enums.Add (e);
				}
			}
			enums.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return enums;
		}

		static List<ClassContents> GetStructs (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ClassContents> structs = new List<ClassContents> ();

			foreach (var m in modules) {
				var moduleStruct = mi.GetClassesFromName (m);
				foreach (var s in moduleStruct) {
					structs.Add (s);
				}
			}
			structs.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return structs;
		}

		static List<ProtocolContents> GetProtocols (ModuleInventory mi)
		{
			var modules = mi.ModuleNames;
			List<ProtocolContents> protocols = new List<ProtocolContents> ();

			foreach (var m in modules) {
				var moduleProtocol = mi.ProtocolsForName (m);
				foreach (var p in moduleProtocol) {
					protocols.Add (p);
				}
			}
			protocols.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			return protocols;
		}

		static void WriteXmlFile (string moduleName, List<ClassContents> classesList, List<ClassContents> enumsList, List<ClassContents> structsList, List<ProtocolContents> protocolList)
		{
			//using (StreamWriter sw = new StreamWriter ($"../../../Modules/{moduleName}.xml")) {
			using (StreamWriter sw = new StreamWriter ($"./Modules/{moduleName}.xml")) {
				Console.WriteLine ($"Creating xml output for {moduleName} at ./Modules/{moduleName}.xml");
				sw.WriteLine ("<manualbinderfinder version=\"1.011\" encoding=\"UTF - 8\">");
				sw.WriteLine ($"<Module name=\"{moduleName}\">");

				sw.WriteLine ($"    <Classes>");
				foreach (var c in classesList) {
					sw.WriteLine ($"        <Class name=\"{c.Name.ToString ()}\">");
					foreach (var classConstructor in c.ClassConstructor.Names) {
						sw.WriteLine ($"            <ClassConstructor name=\"{classConstructor}\"/>");
					}
					foreach (var constructor in c.Constructors.Names) {
						sw.WriteLine ($"            <Constructors name=\"{constructor}\"/>");
					}
					foreach (var destructors in c.Destructors.Names) {
						sw.WriteLine ($"            <Destructors name=\"{destructors}\"/>");
					}
					var classFunctionSignatures = new List<string> ();
					foreach (var functions in c.Methods.Values) {
						try {
							var sig = EnhanceSignature (functions.Functions [0].Signature.ToString ());
							classFunctionSignatures.Add (sig);
						} catch (Exception) {

						}
					}
					classFunctionSignatures.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
					var lastWrittenClassSignature = string.Empty;
					foreach (var sig in classFunctionSignatures) {
						if (sig != lastWrittenClassSignature) {
							sw.WriteLine ($"            <Functions signature=\"{sig}\"/>");
						}
						lastWrittenClassSignature = sig;

					}

					foreach (var properties in c.Properties.Names) {
						sw.WriteLine ($"            <Properties name=\"{properties}\"/>");
					}
					foreach (var variables in c.Variables.Names) {
						sw.WriteLine ($"            <Variables name=\"{variables}\"/>");
					}
					sw.WriteLine ($"        </Class>");
				}
				sw.WriteLine ($"    </Classes>");

				sw.WriteLine ($"    <Structs>");
				foreach (var s in structsList) {
					sw.WriteLine ($"        <Struct name=\"{s.Name.ToString ()}\">");
					foreach (var classConstructor in s.ClassConstructor.Names) {
						sw.WriteLine ($"            <ClassConstructor name=\"{classConstructor}\"/>");
					}
					foreach (var constructor in s.Constructors.Names) {
						sw.WriteLine ($"            <Constructors name=\"{constructor}\"/>");
					}
					foreach (var destructors in s.Destructors.Names) {
						sw.WriteLine ($"            <Destructors name=\"{destructors}\"/>");
					}
					//foreach (var functions in s.Methods.Values) {
					//	try {
					//		var sig = EnhanceSignature (functions.Functions[0].Signature.ToString ());
					//		sw.WriteLine ($"            <Functions signature=\"func {sig}\"/>");
					//	}
					//	catch (Exception) {
					//	}
					//}

					var structFunctionSignatures = new List<string> ();
					foreach (var functions in s.Methods.Values) {
						try {
							var sig = EnhanceSignature (functions.Functions [0].Signature.ToString ());
							structFunctionSignatures.Add (sig);
						} catch (Exception) {

						}
					}
					structFunctionSignatures.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
					var lastWrittenStructSignature = string.Empty;
					foreach (var sig in structFunctionSignatures) {
						if (sig != lastWrittenStructSignature) {
							sw.WriteLine ($"            <Functions signature=\"{sig}\"/>");
						}
						lastWrittenStructSignature = sig;

					}

					foreach (var properties in s.Properties.Names) {
						sw.WriteLine ($"            <Properties name=\"{properties}\"/>");
					}
					foreach (var variables in s.Variables.Names) {
						sw.WriteLine ($"            <Variables name=\"{variables}\"/>");
					}
					sw.WriteLine ($"        </Struct>");
				}
				sw.WriteLine ($"    </Structs>");

				sw.WriteLine ($"    <Enums>");
				foreach (var e in enumsList) {
					sw.WriteLine ($"        <Enum name=\"{e.Name.ToString ()}\"/>");
				}
				sw.WriteLine ($"    </Enums>");

				sw.WriteLine ($"    <Protocols>");
				foreach (var p in protocolList) {
					sw.WriteLine ($"        <Protocol name=\"{p.Name.ToString ()}\">");

					var protocolFunctionSignatures = new List<string> ();
					foreach (var f in p.FunctionsOfUnknownDestination) {
						var sig = EnhanceSignature (f.Signature.ToString ());
						protocolFunctionSignatures.Add (sig);
					}

					protocolFunctionSignatures.Sort ((type1, type2) => String.CompareOrdinal (type1, type2));
					var lastWrittenProtocolSignature = string.Empty;
					foreach (var sig in protocolFunctionSignatures) {
						if (sig != lastWrittenProtocolSignature) {
							sw.WriteLine ($"            <Functions signature=\"{sig}\"/>");
						}
						lastWrittenProtocolSignature = sig;
					}

					sw.WriteLine ($"        </Protocol>");
				}
				sw.WriteLine ($"    </Protocols>");

				sw.WriteLine ($"</Module>");
			}
		}

		static string EnhanceSignature (string signature)
		{
			if (string.IsNullOrEmpty (signature))
				return string.Empty;

			StringBuilder sb = new StringBuilder (signature);
			MatchCollection matches = Regex.Matches (sb.ToString (), ": ");
			sb.Remove (matches [0].Index, matches [0].Length);
			sb.Replace ("->()", "");
			sb.Replace ("(", "( ");
			StringBuilder sb2 = RemoveDuplicateConsecutiveWords (sb);
			sb2.Replace ("( ", "(");
			sb2.Replace ("->", " -> ");
			sb2.Replace ("(0,0)A0", "Self");
			sb2.Replace ("(0,0)", "Self");
			sb2.Insert (0, "func ");
			return sb2.ToString ();
		}

		static StringBuilder RemoveDuplicateConsecutiveWords (StringBuilder s)
		{
			StringBuilder sb = new StringBuilder (s.ToString ());
			string pattern = @"\w*:";
			bool isFinished = false;
			while (!isFinished) {
 				MatchCollection matches = Regex.Matches (sb.ToString (), pattern);
				if (matches.Count == 0) {
					isFinished = true;
					break;
				}
				for (var i = 0; i < matches.Count; i++) {
					if (i < matches.Count - 1) {
						if (matches [i].Value == matches [i + 1].Value) {
							sb.Remove (matches [i + 1].Index, matches [i + 1].Length + 1);
							break;
						}
					} else {
						isFinished = true;
						break;
					}
				}
			}
			return sb;
		}

	}

}
