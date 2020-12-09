using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;

namespace BindingNemo {
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
			// sw.WriteLineWithIndent ($"<BindingNemo version=\"{version}\" encoding=\"UTF-8\">");
			// Indent ();
			sw.WriteLineWithIndent ($"<?xml version=\"{version}\" encoding=\"utf-8\"?>");
			sw.WriteLineWithIndent ($"<xamreflect version=\"{version}\">");
			Indent ();
			sw.WriteLineWithIndent ($"<modulelist>");
			
		}

		public static void WriteModuleIntro (this StreamWriter sw, string moduleName)
		{
			// not sure how to find swift version
			Indent ();
			sw.WriteLineWithIndent ($"<!-- swiftVersion not yet found -->");
			sw.WriteLineWithIndent ($"<Module name=\"{moduleName}\" swiftVersion=\"??\">");
		}

		public static void WriteClasses (this StreamWriter sw, string moduleName, List<ClassContents> classesList)
		{
			if (classesList.Count == 0) {
				return;
			}
			
			//Indent ();
			//sw.WriteLineWithIndent ("<innerclasses>");
			Indent ();
			foreach (var c in classesList) {
				sw.WriteTypeDeclarationOpener ("class", c.Name.ToString (), enums.Accessibility.Public, false, false, false, false);

				//sw.WriteTypeOpener ("Class", c.Name.ToString ());
				if (IsValidClassBasedMembers (c)) {
					Indent ();
					sw.WriteBasicOpener ("members");
					Indent ();
					sw.WriteClassBasedProperties (c);
					sw.WriteClassBasedMethods (c);
					//Exdent ();
					//sw.WriteBasicCloser ("Class");
					Exdent ();
					sw.WriteBasicCloser ("members");
					Exdent ();
				}
				sw.WriteTypeDeclarationCloser ();
			}
			//Exdent ();
			//sw.WriteBasicCloser ("innerclasses");
			
			
			Exdent ();
		}

		public static void WriteStructs (this StreamWriter sw, string moduleName, List<ClassContents> structsList)
		{
			if (structsList.Count == 0) {
				return;
			}
			Indent ();
			
			//sw.WriteLineWithIndent ($"<innerstructs>");
			//Indent ();
			foreach (var s in structsList) {
				sw.WriteTypeDeclarationOpener ("struct", s.Name.ToString (), enums.Accessibility.Public, false, false, false, false);
				if (IsValidClassBasedMembers (s)) {
					Indent ();
					sw.WriteBasicOpener ("members");
					Indent ();
					//sw.WriteTypeOpener ("Struct", s.Name.ToString ());
					//Indent ();
					sw.WriteClassBasedProperties (s);
					sw.WriteClassBasedMethods (s);
					//Exdent ();
					//sw.WriteLineWithIndent ($"</Struct>");
					Exdent ();
					sw.WriteBasicCloser ("members");
					Exdent ();
				}
				sw.WriteTypeDeclarationCloser ();
			}
			//Exdent ();
			//sw.WriteLineWithIndent ($"</innerstructs>");
			
			Exdent ();
		}

		public static void WriteEnums (this StreamWriter sw, string moduleName, List<ClassContents> enumsList)
		{
			if (enumsList.Count == 0) {
				return;
			}
			Indent ();
			
			//sw.WriteLineWithIndent ("<innerenums>");
			//Indent ();
			foreach (var e in enumsList) {
				sw.WriteTypeDeclarationOpener ("enum", e.Name.ToString (), enums.Accessibility.Public, false, false, false, false);
				if (IsValidClassBasedMembers (e)) {
					Indent ();
					sw.WriteBasicOpener ("members");
					Indent ();
					//sw.WriteTypeOpener ("Enum", e.Name.ToString ());
					//Indent ();
					sw.WriteClassBasedProperties (e);
					sw.WriteClassBasedMethods (e);
					//Exdent ();
					//sw.WriteBasicCloser ("Enum");
					Exdent ();
					sw.WriteBasicCloser ("members");
					Exdent ();
				}
				sw.WriteTypeDeclarationCloser ();
			}
			//Exdent ();
			//sw.WriteBasicCloser ("innerenums");
			
			Exdent ();
		}

		public static void WriteProtocols (this StreamWriter sw, string moduleName, List<ProtocolContents> protocolsList)
		{
			if (protocolsList.Count == 0) {
				return;
			}
			
			//Indent ();
			//sw.WriteLineWithIndent ("<innerprotocols>");
			Indent ();
			foreach (var p in protocolsList) {
				sw.WriteTypeDeclarationOpener ("protocol", p.Name.ToString (), enums.Accessibility.Public, false, false, false, false);
				if (IsValidProtocolBasedMembers (p)) {
					Indent ();
					sw.WriteBasicOpener ("members");
					Indent ();

					//sw.WriteTypeOpener ("Protocol", p.Name.ToString ());
					//Indent ();
					sw.WriteProtocolBasedMethods (p);
					//Exdent ();
					//sw.WriteBasicCloser ("Protocol");
					Exdent ();
					sw.WriteBasicCloser ("members");
					Exdent ();
				}
				sw.WriteTypeDeclarationCloser ();
			}
			//Exdent ();
			//sw.WriteBasicCloser ("innerprotocols");
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
			// Exdent ();
			// sw.WriteLineWithIndent ("</BindingNemo>");
		}

		public static void WriteClassBasedProperties (this StreamWriter sw, ClassContents c)
		{
			var propertiesList = new List<PropertyContents> ();
			propertiesList.AddRange (c.Properties.Values.ToList ());
			propertiesList.AddRange (c.StaticProperties.Values.ToList ());
			propertiesList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));

			foreach (var property in propertiesList) {
				var getter = property.Getter;
				var sig = StringBuilderHelper.EnhancePropertySignature (getter.ToString (), false);
				if (sig != null) {
					sw.WriteWithIndent ($"<property");
					var nameSB = new StringBuilder (property.Name.ToString ());
					nameSB.EscapeCharactersName ();
					sw.WriteTypeValue ("name", nameSB.ToString ());
					sw.WriteTypeValue ("isPossiblyIncomplete", "False");					
					//sw.WriteLineWithIndent ($"signature=\"{sig}\"");
					sw.WriteTypeValue ("isStatic", getter.IsStatic.ToString ());
					

					// can check if property is public or private but do not see Internal or Open options
					var isPublic = getter.IsPublic ? true : false;
					sw.WriteTypeValue ("accessibility", isPublic.ToString ());

					//sw.WriteLineWithIndent ($"<!-- property elements not yet found -->");

					sw.WriteTypeValue ("isDeprecated", "False");
					sw.WriteTypeValue ("isUnavailable", "False");
					sw.WriteTypeValue ("isOptional", "False");
					sw.WriteTypeValue ("type", "Named");
					sw.WriteLine (" storage=\"Addressed\"/>");
					
					//sw.WriteLineWithIndent ($"</property>");
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
				var enhancedSignature = StringBuilderHelper.EnhanceMethodSignature (signature.ToString (), isStatic);
				if (enhancedSignature != null) {

					if (signature.ToString () != lastWrittenClassSignature) {
						sw.WriteWithIndent ($"<func");
						Indent ();
						var nameSB = new StringBuilder (functions.Name.ToString ());
						nameSB.EscapeCharactersName ();
						sw.WriteTypeValue ("name", nameSB.ToString ());
						sw.WriteTypeValue ("isPossiblyIncomplete", "False");
						sw.WriteTypeValue ("hasThrows", signature.CanThrow.ToString ());
						sw.WriteTypeValue ("operatorKind", functions.Functions[0].Operator.ToString ());
						//sw.WriteLineWithIndent ($"signature=\"{enhancedSignature}\"");
						sw.WriteTypeValue ("isStatic", isStatic.ToString ());

						if (signature.ReturnType != null) {
							var enhancedReturn = StringBuilderHelper.EnhanceReturn (signature.ReturnType.ToString ());
							if (enhancedReturn != null)
								sw.WriteTypeValue ("returnType", enhancedReturn);
						}

						//sw.WriteLineWithIndent ($"<!-- class func elements not yet found -->");
						sw.WriteTypeValue ("accessibility", "Public");
						sw.WriteTypeValue ("isProperty", "False");
						sw.WriteTypeValue ("isFinal", "False");
						sw.WriteTypeValue ("isDeprecated", "False");
						sw.WriteTypeValue ("isUnavailable", "False");
						sw.WriteTypeValue ("isOptional", "False");
						// there is an IsOptionalConstructor in the signature?
						sw.WriteTypeValue ("isRequired", "False");
						sw.WriteTypeValue ("isConvenienceInit", "False");

						//sw.WriteLineWithIndent ($"<!-- class func elements still working on -->");
						sw.WriteLine (" objcSelector = \"\">");
						//objcSelector - a string representing the ObjC selector for the function

						var parameters = StringBuilderHelper.ParseParameters (enhancedSignature);
						if (parameters != null) {
							sw.WriteLineWithIndent ($"<parameterlists>");
							Indent ();
							sw.WriteLineWithIndent ($"<parameterlist index=\"0\">");
							Indent ();
							foreach (var parameter in parameters) {
								//sw.WriteLineWithIndent ($"<!-- parameter type & private name are not found -->");
								sw.WriteLineWithIndent ($"<parameter publicName=\"{parameter.Item1}\" privateName=\"{parameter.Item1}\" type=\"{TypeSpecParser.Parse (parameter.Item2)}\" isVariadic=\"{signature.IsVariadic.ToString ()}\"/>");
							}
							Exdent ();
							sw.WriteLineWithIndent ($"</parameterlist>");
							Exdent ();
							sw.WriteLineWithIndent ($"</parameterlists>");
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
				var enhancedSignature = StringBuilderHelper.EnhanceMethodSignature (protocol.Signature.ToString (), false);
				if (enhancedSignature != null && enhancedSignature != lastWrittenProtocolSignature) {
					sw.WriteWithIndent ($"<func");
					Indent ();
					var nameSB = new StringBuilder (protocol.Signature.Name.ToString ());
					nameSB.EscapeCharactersName ();
					sw.WriteTypeValue ("name", nameSB.ToString ());
					sw.WriteTypeValue ("isPossiblyIncomplete", "True");
					sw.WriteTypeValue ("operatorKind", protocol.Operator.ToString ());
					//sw.WriteLineWithIndent ($"signature=\"{enhancedSignature}\"");

					sw.WriteTypeValue ("isStatic", CheckStaticProtocolMethod (protocol).ToString ());

					if (protocol.Signature.ReturnType != null) {
						var enhancedReturn = StringBuilderHelper.EnhanceReturn (protocol.Signature.ReturnType.ToString ());
						if (enhancedReturn != null)
							sw.WriteTypeValue ("returnType", enhancedReturn);
					}
					sw.WriteTypeValue ("hasThrows", protocol.Signature.CanThrow.ToString ());

					//sw.WriteLineWithIndent ($"<!-- protocol func elements not yet found -->");
					sw.WriteTypeValue ("accessibility", "Public");
					sw.WriteTypeValue ("isProperty", "False");
					sw.WriteTypeValue ("isFinal", "False");
					sw.WriteTypeValue ("isDeprecated", "False");
					sw.WriteTypeValue ("isUnavailable", "False");
					sw.WriteTypeValue ("isOptional", "False");
						// there is an IsOptionalConstructor in the signature?
					sw.WriteTypeValue ("isRequired", "False");
					sw.WriteTypeValue ("isConvenienceInit", "False");

					//sw.WriteLineWithIndent ($"<!-- protocol func elements still working on -->");
					sw.WriteLine (" objcSelector = \"\">");
					//objcSelector - a string representing the ObjC selector for the function

					var parameters = StringBuilderHelper.ParseParameters (enhancedSignature);
					if (parameters != null) {
						sw.WriteLineWithIndent ($"<parameterlists>");
						Indent ();
						sw.WriteLineWithIndent ($"<parameterlist index=\"0\">");
						Indent ();
						foreach (var parameter in parameters) {
							//sw.WriteLineWithIndent ($"<!-- parameter type & private name are not found -->");
							sw.WriteLineWithIndent ($"<parameter publicName=\"{parameter.Item1}\" privateName=\"{parameter.Item1}\" type=\"{TypeSpecParser.Parse (parameter.Item2)}\" isVariadic=\"{protocol.Signature.IsVariadic.ToString ()}\"/>");
						}
						Exdent ();
						sw.WriteLineWithIndent ($"</parameterlist>");
						Exdent ();
						sw.WriteLineWithIndent ($"</parameterlists>");
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
					// .IsStatic does not exist in some types including SwiftUncurriedFunctionThunkType
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

		public static void WriteTypeDeclarationOpener (this StreamWriter sw, string kind, string name, enums.Accessibility accessibility, bool isObjC, bool isFinal, bool isDeprecated, bool isUnavailable, string module = "")
		{
			sw.WriteWithIndent ($"<typedeclaration kind=\"{kind}\"");
			sw.Write ($" name=\"{name}\"");
			if (module != "")
				sw.Write ($" module=\"{module}\"");
			sw.Write ($" accessibility=\"{accessibility.ToString ()}\"");
			sw.Write ($" isObjC=\"{isObjC.ToString ()}\"");
			sw.Write ($" isFinal=\"{isFinal.ToString ()}\"");
			sw.Write ($" isDeprecated=\"{isDeprecated.ToString ()}\"");
			sw.WriteLine ($" isUnavailable=\"{isUnavailable.ToString ()}\">");
			// sw.WriteLine ($" included=\"null\">");
		}

		public static void WriteTypeDeclarationCloser (this StreamWriter sw)
		{
			sw.WriteLineWithIndent ($"</typedeclaration>");
		}

		public static void WriteBasicOpener (this StreamWriter sw, string type)
		{
			sw.WriteLineWithIndent ($"<{type}>");
		}

		public static void WriteBasicCloser (this StreamWriter sw, string type)
		{
			sw.WriteLineWithIndent ($"</{type}>");
		}

		public static void WriteTypeOpener (this StreamWriter sw, string type, string name)
		{
			sw.WriteLineWithIndent ($"<{type} name=\"{name}\">");
		}

		public static void WriteTypeValue (this StreamWriter sw, string type, string value)
		{
			sw.Write ($" {type}=\"{value}\"");
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

		public static void WriteElement (this StreamWriter sw, string id, string value)
		{
			sw.WriteLine ($"{WriteIndents ()}<{id}>{value}</{id}>");
		}

		public static void WriteWithIndent (this StreamWriter sw, string content)
		{
			sw.Write ($"{WriteIndents ()}{content}");
		}

		static bool IsValidClassBasedMembers (ClassContents contents)
		{
			foreach (var prop in contents.Properties.Names) {
				if (!prop.Name.Contains ('_'))
					return true;
			}
			foreach (var staticProp in contents.StaticProperties.Names) {
				if (!staticProp.Name.Contains ('_'))
					return true;
			}
			foreach (var method in contents.Methods.Names) {
				if (!method.Name.Contains ('_'))
					return true;
			}
			foreach (var staticMethod in contents.StaticFunctions.Names) {
				if (!staticMethod.Name.Contains ('_'))
					return true;
			}
			return false;
		}
		static bool IsValidProtocolBasedMembers (ProtocolContents contents)
		{
			foreach (var prop in contents.FunctionsOfUnknownDestination.ToList ()) {
				if (!prop.Name.ToString ().Contains ('_'))
					return true;
			}
			return false;
		}
	}
}
