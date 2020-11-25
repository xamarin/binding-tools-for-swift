using System;
using System.Collections.Generic;
using System.IO;
using ManualBinderFinder.Models;
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
			sw.WriteLevelTwoOpener ("Classes");
			foreach (var c in classesList) {
				sw.WriteLevelThreeOpener ("Class", c.Name.ToString ());
				sw.WriteClassBasedClassConstructor (c);
				sw.WriteClassBasedConstructor (c);
				sw.WriteClassBasedDestructor (c);
				sw.WriteClassBasedProperties (c);
				sw.WriteClassBasedMethods (c);
				sw.WriteLevelThreeCloser ("Class");
			}
			sw.WriteLevelTwoCloser ("Classes");
		}

		public static void WriteStructs (this StreamWriter sw, string moduleName, List<ClassContents> structsList)
		{
			sw.WriteLevelTwoOpener ("Structs");
			foreach (var s in structsList) {
				sw.WriteLevelThreeOpener ("Struct", s.Name.ToString ());
				sw.WriteClassBasedClassConstructor (s);
				sw.WriteClassBasedConstructor (s);
				sw.WriteClassBasedDestructor (s);
				sw.WriteClassBasedProperties (s);
				sw.WriteClassBasedMethods (s);
				sw.WriteLevelThreeCloser ("Struct");
			}
			sw.WriteLevelTwoCloser ("Structs");
		}

		public static void WriteEnums (this StreamWriter sw, string moduleName, List<ClassContents> enumsList)
		{
			sw.WriteLevelTwoOpener ("Enums");
			foreach (var e in enumsList) {
				sw.WriteLevelThreeOpener ("Enum", e.Name.ToString ());
				sw.WriteClassBasedProperties (e);
				sw.WriteClassBasedMethods (e);
				sw.WriteLevelThreeCloser ("Enum");
			}
			sw.WriteLevelTwoCloser ("Enums");
		}

		public static void WriteProtocols (this StreamWriter sw, string moduleName, List<ProtocolContents> protocolsList)
		{
			sw.WriteLevelTwoOpener ("Protocols");
			foreach (var p in protocolsList) {
				sw.WriteLevelThreeOpener ("Protocol", p.Name.ToString ());
				sw.WriteProtocolBasedMethods (p);
				sw.WriteLevelThreeCloser ("Protocol");
			}
			sw.WriteLevelTwoCloser ("Protocols");
		}

		public static void WriteXmlOutro (this StreamWriter sw)
		{
			sw.WriteLine ($"</Module>");
		}

		public static void WriteClassBasedProperties (this StreamWriter sw, ClassContents c)
		{
			var propertiesList = new List<ClassInfo> ();

			foreach (var properties in c.Properties.Values) {
				var classInfo = new ClassInfo ();
				var getter = properties.Getter;
				var sig = StringBuiderHelper.EnhancePropertySignature (getter.ToString (), false);
				if (sig != null) {
					classInfo.Signature = sig;
					classInfo.Name = properties.Name.ToString ();
					classInfo.IsStatic = false;
					propertiesList.Add (classInfo);
				}
			}

			foreach (var properties in c.StaticProperties.Values) {
				var classInfo = new ClassInfo ();
				var getter = properties.Getter;
				var sig = StringBuiderHelper.EnhancePropertySignature (getter.ToString (), true);
				if (sig != null) {
					classInfo.Signature = sig;
					classInfo.Name = properties.Name.ToString ();
					classInfo.IsStatic = false;
					propertiesList.Add (classInfo);
				}
			}

			propertiesList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name, type2.Name));

			foreach (var property in propertiesList) {
				sw.WriteLine ($"			<Property>");
				sw.WriteLine ($"				<name=\"{property.Name}\">");
				sw.WriteLine ($"				<signature=\"{property.Signature}\">");
				sw.WriteLine ($"				<Static=\"{property.IsStatic.ToString ()}\">");
				sw.WriteLine ($"			</Property>");
			}
		}

		public static void WriteClassBasedMethods (this StreamWriter sw, ClassContents c)
		{
			var classFunctionSignatures = new List<ClassInfo> ();
			foreach (var functions in c.Methods.Values) {
				var classInfo = new ClassInfo ();
				var sig = StringBuiderHelper.EnhanceMethodSignature (functions.Functions [0].Signature.ToString (), false);
				if (sig != null) {
					classInfo.Signature = sig;
					classInfo.Name = functions.Name.ToString ();
					classInfo.IsStatic = false;
					classFunctionSignatures.Add (classInfo);
				}
			}
			foreach (var functions in c.StaticFunctions.Values) {
				var classInfo = new ClassInfo ();
				var sig = StringBuiderHelper.EnhanceMethodSignature (functions.Functions [0].Signature.ToString (), true);
				if (sig != null) {
					classInfo.Signature = sig;
					classInfo.Name = functions.Name.ToString ();
					classInfo.IsStatic = true;
					classFunctionSignatures.Add (classInfo);
				}
			}

			classFunctionSignatures.Sort ((type1, type2) => String.CompareOrdinal (type1.Name, type2.Name));
			var lastWrittenClassSignature = string.Empty;
			foreach (var classInfo in classFunctionSignatures) {
				if (classInfo.Signature != lastWrittenClassSignature) {
					sw.WriteLine ($"			<Method>");
					sw.WriteLine ($"				<name=\"{classInfo.Name}\">");
					sw.WriteLine ($"				<signature=\"{classInfo.Signature}\">");
					sw.WriteLine ($"				<isStatic=\"{classInfo.IsStatic.ToString ()}\">");
					sw.WriteLine ($"			</Method>");
				}
				lastWrittenClassSignature = classInfo.Signature;
			}
		}

		public static void WriteProtocolBasedMethods (this StreamWriter sw, ProtocolContents p)
		{
			var protocolFunctionSignatures = new List<ProtocolInfo> ();
			foreach (var f in p.FunctionsOfUnknownDestination) {
				var protocolInfo = new ProtocolInfo ();
				var sig = StringBuiderHelper.EnhanceMethodSignature (f.Signature.ToString (), false);
				if (sig != null) {
					protocolInfo.Signature = sig;
					protocolInfo.Name = f.Signature.Name.ToString ();
					// Need to check for static
					protocolInfo.IsStatic = false;
					protocolFunctionSignatures.Add (protocolInfo);
				}
			}

			protocolFunctionSignatures.Sort ((type1, type2) => String.CompareOrdinal (type1.Name, type2.Name));
			var lastWrittenProtocolSignature = string.Empty;
			foreach (var protocolInfo in protocolFunctionSignatures) {
				if (protocolInfo.Signature != lastWrittenProtocolSignature) {
					sw.WriteLine ($"			<Method>");
					sw.WriteLine ($"				<name=\"{protocolInfo.Name}\">");
					sw.WriteLine ($"				<signature=\"{protocolInfo.Signature}\">");
					sw.WriteLine ($"				<isStatic=\"{protocolInfo.IsStatic.ToString ()}\">");
					sw.WriteLine ($"			</Method>");
				}
				lastWrittenProtocolSignature = protocolInfo.Signature;
			}
		}

		public static void WriteClassBasedClassConstructor (this StreamWriter sw, ClassContents c)
		{
			foreach (var classConstructor in c.ClassConstructor.Names) {
				sw.WriteLine ($"			<ClassConstructor name=\"{classConstructor}\"/>");
			}
		}

		public static void WriteClassBasedConstructor (this StreamWriter sw, ClassContents c)
		{
			foreach (var constructor in c.Constructors.Names) {
				sw.WriteLine ($"			<Constructor name=\"{constructor}\"/>");
			}
		}

		public static void WriteClassBasedDestructor (this StreamWriter sw, ClassContents c)
		{
			foreach (var destructors in c.Destructors.Names) {
				sw.WriteLine ($"			<Destructor name=\"{destructors}\"/>");
			}
		}

		public static void WriteLevelTwoOpener (this StreamWriter sw, string type)
		{
			sw.WriteLine ($"	<{type}>");
		}

		public static void WriteLevelTwoCloser (this StreamWriter sw, string type)
		{
			sw.WriteLine ($"	</{type}>");
		}

		public static void WriteLevelThreeOpener (this StreamWriter sw, string type, string name)
		{
			sw.WriteLine ($"		<{type} name=\"{name}\">");
		}

		public static void WriteLevelThreeCloser (this StreamWriter sw, string type)
		{
			sw.WriteLine ($"		</{type}>");
		}
		
	}
}
