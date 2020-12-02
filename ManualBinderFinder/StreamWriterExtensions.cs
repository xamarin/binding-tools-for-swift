﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ManualBinderFinder.Models;
using SwiftReflector.Inventory;

namespace ManualBinderFinder {
	public static class StreamWriterExtensions {

		public static int IndentLevel { get; set; }

		//static void WriteXmlFile (string moduleName, List<ClassContents> classesList, List<ClassContents> enumsList, List<ClassContents> structsList, List<ProtocolContents> protocolsList)
		public static void WriteXmlFile (this StreamWriter sw, string moduleName, ModuleInventory mi)
		{
			List<ClassContents> classesList = CheckInventory.GetClasses (mi);
			List<ClassContents> enumsList = CheckInventory.GetEnums (mi);
			List<ClassContents> structsList = CheckInventory.GetStructs (mi);
			List<ProtocolContents> protocolsList = CheckInventory.GetProtocols (mi);

			Console.WriteLine ($"Extracting \"{moduleName}\"");

			IndentLevel = 1;

			sw.WriteModuleIntro (moduleName);
			sw.WriteClasses (moduleName, classesList);
			sw.WriteStructs (moduleName, structsList);
			sw.WriteEnums (moduleName, enumsList);
			sw.WriteProtocols (moduleName, protocolsList);
			sw.WriteModuleOutro ();

		}

		public static void WriteXmlIntro (this StreamWriter sw, string version)
		{
			sw.WriteLineWithIndent ($"<ManualBinderFinder version=\"{version}\" encoding=\"UTF-8\">");
			sw.WriteLineWithIndent ($"<xamreflect version=\"{version}\">");
			Indent ();
			sw.WriteLineWithIndent ($"<modulelist>");
			
		}

		public static void WriteModuleIntro (this StreamWriter sw, string moduleName)
		{
			// not sure how to find swift version
			Indent ();
			sw.WriteLineWithIndent ($"<Module name=\"{moduleName}\" swiftVersion=\"??\">");
		}

		public static void WriteClasses (this StreamWriter sw, string moduleName, List<ClassContents> classesList)
		{
			if (classesList.Count == 0) {
				return;
			}
			Indent ();
			sw.WriteTypeDeclarationOpener ("class", enums.Accessibility.Public, false, false, false, false);

			Indent ();
			sw.WriteLevelThreeOpener ("innerclasses");
			Indent ();
			foreach (var c in classesList) {
				sw.WriteLevelFourOpener ("Class", c.Name.ToString ());
				Indent ();
				sw.WriteClassBasedProperties (c);
				sw.WriteClassBasedMethods (c);
				Exdent ();
				sw.WriteLevelFourCloser ("Class");
			}
			Exdent ();
			sw.WriteLevelThreeCloser ("innerclasses");
			Exdent ();
			sw.WriteTypeDeclarationCloser ();
			Exdent ();
		}

		public static void WriteStructs (this StreamWriter sw, string moduleName, List<ClassContents> structsList)
		{
			if (structsList.Count == 0) {
				return;
			}
			Indent ();
			sw.WriteTypeDeclarationOpener ("struct", enums.Accessibility.Public, false, false, false, false);
			Indent ();
			sw.WriteLineWithIndent ($"<innerstructs>");
			Indent ();
			foreach (var s in structsList) {
				sw.WriteLineWithIndent ($"<Struct name=\"{s.Name.ToString ()}\">");
				Indent ();
				sw.WriteClassBasedProperties (s);
				sw.WriteClassBasedMethods (s);
				Exdent ();
				sw.WriteLineWithIndent ($"</Struct>");
			}
			Exdent ();
			sw.WriteLineWithIndent ($"</innerstructs>");
			Exdent ();
			sw.WriteTypeDeclarationCloser ();
			Exdent ();
		}

		public static void WriteEnums (this StreamWriter sw, string moduleName, List<ClassContents> enumsList)
		{
			if (enumsList.Count == 0) {
				return;
			}
			Indent ();
			sw.WriteTypeDeclarationOpener ("enum", enums.Accessibility.Public, false, false, false, false);
			Indent ();
			sw.WriteLevelThreeOpener ("innerenums");
			Indent ();
			foreach (var e in enumsList) {
				sw.WriteLevelFourOpener ("Enum", e.Name.ToString ());
				Indent ();
				sw.WriteClassBasedProperties (e);
				sw.WriteClassBasedMethods (e);
				Exdent ();
				sw.WriteLevelFourCloser ("Enum");
			}
			Exdent ();
			sw.WriteLevelThreeCloser ("innerenums");
			Exdent ();
			sw.WriteTypeDeclarationCloser ();
			Exdent ();
		}

		public static void WriteProtocols (this StreamWriter sw, string moduleName, List<ProtocolContents> protocolsList)
		{
			if (protocolsList.Count == 0) {
				return;
			}
			Indent ();
			sw.WriteTypeDeclarationOpener ("protocol", enums.Accessibility.Public, false, false, false, false);
			Indent ();
			sw.WriteLevelThreeOpener ("innerprotocols");
			Indent ();
			foreach (var p in protocolsList) {
				sw.WriteLevelFourOpener ("Protocol", p.Name.ToString ());
				Indent ();
				sw.WriteProtocolBasedMethods (p);
				Exdent ();
				sw.WriteLevelThreeCloser ("Protocol");
			}
			Exdent ();
			sw.WriteLevelThreeCloser ("innerprotocols");
			Exdent ();
			sw.WriteTypeDeclarationCloser ();
			Exdent ();
		}

		public static void WriteModuleOutro (this StreamWriter sw)
		{
			//Exdent ();
			sw.WriteLineWithIndent ("</Module>");
		}

		public static void WriteXmlOutro (this StreamWriter sw)
		{
			Exdent ();
			sw.WriteLineWithIndent ("</modulelist>");
			Exdent ();
			sw.WriteLineWithIndent ("</xamreflect>");
		}

		public static void WriteClassBasedProperties (this StreamWriter sw, ClassContents c)
		{
			var propertiesList = new List<PropertyContents> ();
			propertiesList.AddRange (c.Properties.Values.ToList ());
			propertiesList.AddRange (c.StaticProperties.Values.ToList ());
			propertiesList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));

			foreach (var property in propertiesList) {
				var getter = property.Getter;
				var sig = StringBuiderHelper.EnhancePropertySignature (getter.ToString (), false);
				if (sig != null) {
					sw.WriteLineWithIndent ($"<property>");
					Indent ();
					sw.WriteLineWithIndent ($"<name=\"{property.Name.ToString ()}\">");
					//sw.WriteLineWithIndent ($"<accessibility=\"{property.}\">");
					//sw.WriteLineWithIndent ($"<signature=\"{sig}\">");
					sw.WriteLineWithIndent ($"<Static=\"{getter.IsStatic.ToString ()}\">");
					Exdent ();
					sw.WriteLineWithIndent ($"</property>");
				}
			}
		}

		public static void WriteClassBasedMethods (this StreamWriter sw, ClassContents c)
		{
			var methodList = new List<OverloadInventory> ();
			methodList.AddRange (c.Methods.Values.ToList ());
			methodList.AddRange (c.StaticFunctions.Values.ToList ());
			methodList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));

			var lastWrittenClassSignature = string.Empty;
			foreach (var functions in methodList) {
				var signature = functions.Functions [0].Signature;
				var isStatic = signature.GetType () == typeof(SwiftReflector.SwiftStaticFunctionType) ? true : false;
				var enhancedSignature = StringBuiderHelper.EnhanceMethodSignature (signature.ToString (), isStatic);
				if (enhancedSignature != null) {

					if (signature.ToString () != lastWrittenClassSignature) {
						sw.WriteLineWithIndent ($"<func>");
						Indent ();
						sw.WriteLineWithIndent ($"<name=\"{functions.Name.ToString ()}\">");
						sw.WriteLineWithIndent ($"<hasThrows=\"{signature.CanThrow.ToString ()}\">");
						sw.WriteLineWithIndent ($"<operatorKind=\"{functions.Functions[0].Operator.ToString ()}\">");
						sw.WriteLineWithIndent ($"<signature=\"{enhancedSignature}\">");
						sw.WriteLineWithIndent ($"<isStatic=\"{isStatic.ToString ()}\">");

						if (signature.ReturnType != null) {
							var returnSB = new StringBuilder (signature.ReturnType.ToString ());
							returnSB.CorrectSelf ();
							sw.WriteLineWithIndent ($"<returnType=\"{returnSB.ToString ()}\">");
						}

						
						try {
							var parameters = StringBuiderHelper.ParseParameters (enhancedSignature);
							if (parameters != null) {
								sw.WriteLineWithIndent ($"<parameterlist>");
								Indent ();
								foreach (var parameter in parameters) {
									sw.WriteLineWithIndent ($"<parameter publicName=\"{parameter}\">");
								}
								Exdent ();
								sw.WriteLineWithIndent ($"</parameterlist>");
							}
						}
						catch (Exception) {
							Console.WriteLine ("Exception");
						}

						
						Exdent ();
						sw.WriteLineWithIndent ($"</func>");
					}
					lastWrittenClassSignature = signature.ToString ();
				}
			}
		}

		public static void WriteProtocolBasedMethods (this StreamWriter sw, ProtocolContents p)
		{
			var protocols = new List<SwiftReflector.Demangling.TLFunction> ();
			protocols.AddRange (p.FunctionsOfUnknownDestination.ToList ());
			protocols.Sort ((type1, type2) => String.CompareOrdinal (type1.Signature.Name.ToString (), type2.Signature.Name.ToString ()));

			var lastWrittenProtocolSignature = string.Empty;
			foreach (var protocol in protocols) {
				var enhancedSignature = StringBuiderHelper.EnhanceMethodSignature (protocol.Signature.ToString (), false);
				if (enhancedSignature != null && enhancedSignature != lastWrittenProtocolSignature) {
					sw.WriteLineWithIndent ($"<func>");
					Indent ();
					sw.WriteLineWithIndent ($"<name=\"{protocol.Signature.Name.ToString ()}\">");
					sw.WriteLineWithIndent ($"<operatorKind=\"{protocol.Operator.ToString ()}\">");
					sw.WriteLineWithIndent ($"<signature=\"{enhancedSignature}\">");
					
					sw.WriteLineWithIndent ($"<isStatic=\"{CheckStaticProtocolMethod (protocol)}\">");
					try {
						var parameters = StringBuiderHelper.ParseParameters (enhancedSignature);
						if (parameters != null) {
							sw.WriteLineWithIndent ($"<parameterlist>");
							Indent ();
							foreach (var parameter in parameters) {
								sw.WriteLineWithIndent ($"<parameter publicName=\"{parameter}\">");
							}
							Exdent ();
							sw.WriteLineWithIndent ($"</parameterlist>");
						}
					}
					catch (Exception) {
						Console.WriteLine ("Exception");
					}
					
					Exdent ();
					sw.WriteLineWithIndent ($"</func>");
				}
				lastWrittenProtocolSignature = enhancedSignature;
			}
		}

		public static bool CheckStaticProtocolMethod (SwiftReflector.Demangling.TLFunction protocol)
		{
			switch (protocol.Signature.GetType ().ToString ()) {
				case "SwiftReflector.SwiftStaticFunctionThunkType":
					return true;
				case "SwiftReflector.SwiftPropertyType":
					// .IsStatic does not exist in SwiftUncurriedFunctionThunkType
					return ((SwiftReflector.SwiftPropertyType)protocol.Signature).IsStatic;
				case "SwiftReflector.SwiftPropertyThunkType":
					return ((SwiftReflector.SwiftPropertyThunkType)protocol.Signature).IsStatic;
				case "SwiftReflector.SwiftUncurriedFunctionThunkType":
					return false;
				case "SwiftReflector.SwiftUncurriedFunctionType":
					return false;
				default:
					return false;
			}
		}

		public static void WriteTypeDeclarationOpener (this StreamWriter sw, string kind, enums.Accessibility accessibility, bool isObjC, bool isFinal, bool isDeprecated, bool isUnavailable, string module = "")
		{
			sw.WriteWithIndent ($"<typedeclaration kind=\"{kind}\"");
			if (module != "")
				sw.Write ($" module=\"{module}\"");
			sw.Write ($" accessibility=\"{accessibility.ToString ()}\"");
			sw.Write ($" isObjC=\"{isObjC.ToString ()}\"");
			sw.Write ($" isFinal=\"{isFinal.ToString ()}\"");
			sw.Write ($" isDeprecated=\"{isDeprecated.ToString ()}\"");
			sw.WriteLine ($" isUnavailable=\"{isUnavailable.ToString ()}\">");
		}

		public static void WriteTypeDeclarationCloser (this StreamWriter sw)
		{
			sw.WriteLineWithIndent ($"</typedeclaration>");
		}

		public static void WriteLevelThreeOpener (this StreamWriter sw, string type)
		{
			sw.WriteLineWithIndent ($"<{type}>");
		}

		public static void WriteLevelThreeCloser (this StreamWriter sw, string type)
		{
			sw.WriteLineWithIndent ($"</{type}>");
		}

		public static void WriteLevelFourOpener (this StreamWriter sw, string type, string name)
		{
			sw.WriteLineWithIndent ($"<{type} name=\"{name}\">");
		}

		public static void WriteLevelFourCloser (this StreamWriter sw, string type)
		{
			sw.WriteLineWithIndent ($"</{type}>");
		}


		public static string WriteIndents ()
		{
			var indentsSB = new StringBuilder ();
			for (int i = 0; i < IndentLevel; i++) {
				//indentsSB.Append ("\t");
				indentsSB.Append ("   ");
			}
			return indentsSB.ToString ();
		}

		public static void Indent ()
		{
			IndentLevel++;
		}

		public static void Exdent ()
		{
			if (IndentLevel > 0)
				IndentLevel--;
		}

		public static void WriteLineWithIndent (this StreamWriter sw, string content)
		{
			sw.WriteLine ($"{WriteIndents ()}{content}");
		}

		public static void WriteWithIndent (this StreamWriter sw, string content)
		{
			sw.Write ($"{WriteIndents ()}{content}");
		}

	}
}
