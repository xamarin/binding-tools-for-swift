using System;
using System.Collections.Generic;
using System.IO;
using SwiftReflector.Inventory;

namespace ManualBinderFinder {
	public static class StreamWriterExtensions {
		public static void WriteXmlIntro (this StreamWriter sw, string moduleName, string version)
		{
			sw.WriteLine ($"<manualbinderfinder version=\"{version}\" encoding=\"UTF - 8\">");
			sw.WriteLine ($"<Module name=\"{moduleName}\">");
		}

		public static void WriteClasses (this StreamWriter sw, string moduleName, List<ClassContents> classesList)
		{
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
						var sig = StringBuiderHelper.EnhanceSignature (functions.Functions [0].Signature.ToString ());
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
		}

		public static void WriteStructs (this StreamWriter sw, string moduleName, List<ClassContents> structsList)
		{
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
						var sig = StringBuiderHelper.EnhanceSignature (functions.Functions [0].Signature.ToString ());
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
		}

		public static void WriteEnums (this StreamWriter sw, string moduleName, List<ClassContents> enumsList)
		{
			sw.WriteLine ($"    <Enums>");
			foreach (var e in enumsList) {
				sw.WriteLine ($"        <Enum name=\"{e.Name.ToString ()}\"/>");
			}
			sw.WriteLine ($"    </Enums>");
		}
		public static void WriteProtocols (this StreamWriter sw, string moduleName, List<ProtocolContents> protocolsList)
		{
			sw.WriteLine ($"    <Protocols>");
			foreach (var p in protocolsList) {
				sw.WriteLine ($"        <Protocol name=\"{p.Name.ToString ()}\">");

				var protocolFunctionSignatures = new List<string> ();
				foreach (var f in p.FunctionsOfUnknownDestination) {
					var sig = StringBuiderHelper.EnhanceSignature (f.Signature.ToString ());
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
		}

		public static void WriteXmlOutro (this StreamWriter sw)
		{
			sw.WriteLine ($"</Module>");
		}
	}
}
