using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dynamo.SwiftLang;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace DylibBinder {
	public static class StreamWriterExtensions {

		static int IndentLevel { get; set; }
		static int privateIndex { get; set; } = 0;

		public static void WriteXmlFile (this StreamWriter sw, string moduleName, ModuleInventory mi)
		{
			(var classesList, var structsList, var enumsList) = CheckInventory.GetClassesStructsEnums (mi);
			var protocolsList = CheckInventory.GetProtocols (mi);

			Console.WriteLine ($"Extracting \"{moduleName}\"");

			var innerX = new InnerX ();
			var innerXDictionary = innerX.AddClassContentsList (classesList, enumsList, structsList);
			IndentLevel = 1;

			sw.WriteModuleIntro (moduleName);
			sw.WriteClassContentsList (classesList, "class", innerXDictionary, 0);
			sw.WriteClassContentsList (enumsList, "enum", innerXDictionary, 0);
			sw.WriteClassContentsList (structsList, "struct", innerXDictionary, 0);
			sw.WriteProtocols (moduleName, protocolsList);
			sw.WriteModuleOutro ();
		}

		public static void WriteXmlIntro (this StreamWriter sw, string version)
		{
			sw.WriteLineWithIndent ($"<?xml version=\"{version}\" encoding=\"utf-8\"?>");
			sw.WriteLineWithIndent ($"<xamreflect version=\"{version}\">");
			Indent ();
			sw.WriteLineWithIndent ($"<modulelist>");
		}

		public static void WriteModuleIntro (this StreamWriter sw, string moduleName)
		{
			Indent ();
			sw.WriteLineWithIndent ($"<module name=\"{moduleName}\" swiftVersion=\"5.0\">");
		}

		public static void WriteClassContentsList (this StreamWriter sw, List<ClassContents> classContentsList, string nominalType, Dictionary<string, List<ClassContents>> innerXDictionary, int depth)
		{
			if (classContentsList.Count == 0) {
				return;
			}

			Indent ();
			foreach (var c in classContentsList) {
				// we only want to write innerX inside innerX elements and not at the top depth
				if (depth == 0 && c.Name.NestingNames.Count > 1) {
					continue;
				}
				sw.WriteTypeDeclarationOpener (nominalType, c.Name.ToString (), enums.Accessibility.Public, false, false, false, false);

				if (ContainsValidClassBasedMembers (c)) {
					Indent ();
					sw.WriteBasicOpener ("members");
					Indent ();
					(var propertyGenericChecker, var propertyAssociatedChecker) = sw.WriteClassBasedProperties (c, depth);
					(var methodGenericChecker, var methodAssociatedChecker) = sw.WriteClassBasedMethods (c, depth);
					Exdent ();
					sw.WriteBasicCloser ("members");
					var childrenGenerics = sw.WriteInnerX (c, innerXDictionary, depth);
					sw.WritePropertyMethodGenerics (propertyGenericChecker, depth, methodGenericChecker, childrenGenerics);
					sw.WriteAssociateTypes (methodAssociatedChecker, propertyAssociatedChecker);

					Exdent ();
				}
				sw.WriteTypeDeclarationCloser ();
			}
			Exdent ();
		}

		static (List<string>, List<string>) WriteClassContents (this StreamWriter sw, ClassContents classContents, string nominalType, Dictionary<string, List<ClassContents>> innerXDictionary, int depth)
		{
			if (classContents == null)
				return (null, null);

			Indent ();

			var propertyGenericChecker = new List<string> ();
			var methodGenericChecker = new List<string> ();
			var propertyAssociatedChecker = new List<string> ();
			var methodAssociatedChecker = new List<string> ();

			sw.WriteTypeDeclarationOpener (nominalType, classContents.Name.ToString (), enums.Accessibility.Public, false, false, false, false);

			if (ContainsValidClassBasedMembers (classContents)) {
				Indent ();
				sw.WriteBasicOpener ("members");
				Indent ();
				(propertyGenericChecker, propertyAssociatedChecker) = sw.WriteClassBasedProperties (classContents, depth);
				(methodGenericChecker, methodAssociatedChecker) = sw.WriteClassBasedMethods (classContents, depth);
				Exdent ();
				sw.WriteBasicCloser ("members");
				var childrenGenerics = sw.WriteInnerX (classContents, innerXDictionary, depth);
				sw.WritePropertyMethodGenerics (propertyGenericChecker, depth, methodGenericChecker, childrenGenerics);
				sw.WriteAssociateTypes (methodAssociatedChecker, propertyAssociatedChecker);

				Exdent ();
			}
			sw.WriteTypeDeclarationCloser ();

			Exdent ();
			return (propertyGenericChecker, methodGenericChecker);
		}

		public static void WriteProtocols (this StreamWriter sw, string moduleName, List<ProtocolContents> protocolsList)
		{
			if (protocolsList.Count == 0)
				return;

			Indent ();
			foreach (var p in protocolsList) {
				sw.WriteTypeDeclarationOpener ("protocol", p.Name.ToString (), enums.Accessibility.Public, false, false, false, false);
				Indent ();
				sw.WriteBasicOpener ("members");
				Indent ();
				(var methodGenericChecker, var methodAssociatedChecker) = sw.WriteProtocolBasedMethods (p);
				Exdent ();
				sw.WriteBasicCloser ("members");
				sw.WritePropertyMethodGenerics (null, 0, methodGenericChecker);
				sw.WriteAssociateTypes (methodAssociatedChecker, null);
				Exdent ();
				sw.WriteTypeDeclarationCloser ();
			}
			Exdent ();
		}

		public static void WriteModuleOutro (this StreamWriter sw)
		{
			sw.WriteLineWithIndent ("</module>");
		}

		public static void WriteXmlOutro (this StreamWriter sw)
		{
			Exdent ();
			sw.WriteLineWithIndent ("</modulelist>");
			Exdent ();
			sw.WriteLineWithIndent ("</xamreflect>");
		}

		public static (List<string>, List<string>) WriteClassBasedProperties (this StreamWriter sw, ClassContents c, int depth)
		{
			var propertiesList = new List<PropertyContents> ();
			propertiesList.AddRange (c.Properties.Values.ToList ());
			propertiesList.AddRange (c.StaticProperties.Values.ToList ());
			propertiesList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			var propertyGenerics = new List<string> ();
			var typedeclarationAssociatedTypes = new List<string> ();

			foreach (var property in propertiesList) {
				var getter = property.Getter;
				var sig = StringBuilderHelper.EnhancePropertySignature (getter.ToString (), false);
				if (sig != null) {
					if (getter.IsPrivate || !getter.IsPublic) {
						continue;
					}
					sw.WriteWithIndent ($"<property");
					var name = StringBuilderHelper.EscapeCharacters (property.Name.ToString ());
					var isStatic = getter.IsStatic.ToString ();
					sw.WriteTypeValue ("name", name);
					sw.WriteTypeValue ("isPossiblyIncomplete", "False");
					sw.WriteTypeValue ("isStatic", isStatic);

					var isPublic = getter.IsPublic ? Accessibility.Public : Accessibility.Unknown;
					sw.WriteTypeValue ("accessibility", isPublic.ToString ());

					var propertyType = StringBuilderHelper.ParsePropertyType (sig);

					var parsed = TypeSpecParser.Parse (propertyType).ToString ();
					var filteredType = StringBuilderHelper.EscapeCharacters (parsed);

					sw.WriteTypeValue ("type", filteredType);

					CheckTypeDeclarationGenerics (null, null, filteredType, ref propertyGenerics, depth);

					//elements not present
					sw.WriteTypeValue ("isDeprecated", "False");
					sw.WriteTypeValue ("isUnavailable", "False");
					sw.WriteTypeValue ("isOptional", "False");
					sw.WriteTypeValue ("storage", "Addressed");
					if (getter.GenericArguments != null && getter.GenericArguments.Count != 0) {
						sw.WriteLine (">");
						sw.WriteInnerGenericParamters (null, getter, depth);
						sw.WriteLineWithIndent ("</property>");
					} else
						sw.WriteLine ("/>");
					var parameterAssociateChecker = CheckParameterAssociateTypes (getter.ReturnType, getter, depth);
					CheckTypeDeclarationAssociatedTypes (parameterAssociateChecker, null, ref typedeclarationAssociatedTypes);

					if (property.Getter != null)
						sw.WriteGetter (name, isStatic, filteredType, getter, depth);
					if (property.Setter != null)
						sw.WriteSetter (name, isStatic, filteredType, getter, depth);
				}
			}
			return (propertyGenerics, typedeclarationAssociatedTypes);
		}

		public static (List<string>, List<string>) WriteClassBasedMethods (this StreamWriter sw, ClassContents c, int depth)
		{
			var methodList = new List<OverloadInventory> ();
			methodList.AddRange (c.Methods.Values.ToList ());
			methodList.AddRange (c.StaticFunctions.Values.ToList ());
			methodList.Sort ((type1, type2) => String.CompareOrdinal (type1.Name.ToString (), type2.Name.ToString ()));
			var typedeclarationGenerics = new List<string> ();
			var typedeclarationAssociatedTypes = new List<string> ();

			var lastWrittenClassSignature = string.Empty;
			foreach (var functions in methodList) {
				foreach (var f in functions.Functions) {
					var signature = f.Signature;

					// check if the function rethrows
					MatchCollection arrowMatch = Regex.Matches (signature.ToString (), "->");
					if (arrowMatch.Count > 1)
						continue;

					if (StringBuilderHelper.CheckForPrivateSignature (signature.ToString ()) ||
						signature.ToString () == lastWrittenClassSignature)
						continue;

					var isStatic = signature.GetType () == typeof (SwiftReflector.SwiftStaticFunctionType) ? true : false;
					sw.WriteWithIndent ($"<func");
					Indent ();
					sw.WriteTypeValue ("name", StringBuilderHelper.EscapeCharacters (functions.Name.ToString ()));
					sw.WriteTypeValue ("hasThrows", signature.CanThrow.ToString ());
					sw.WriteTypeValue ("operatorKind", functions.Functions [0].Operator.ToString ());
					sw.WriteTypeValue ("isStatic", isStatic.ToString ());
					sw.WriteTypeValue ("isPossiblyIncomplete", "False");

					var hasInstace = !signature.IsConstructor && !isStatic;

					var returnGenericChecker = sw.WriteReturnType (signature, depth, signature.GenericArguments.Count, signature.GenericParameterCount);
					var returnAssociateChecker = CheckParameterAssociateTypes (signature.ReturnType, signature, depth);
					sw.WriteConstantAttributes ();
					sw.WriteInnerGenericParamters (signature, null, depth);
					var parameterGenericChecker = sw.WriteParameters (signature, depth, hasInstace);
					var parameterAssociateChecker = CheckParameterAssociateTypes (signature.Parameters, signature, depth);
					CheckTypeDeclarationGenerics (returnGenericChecker, parameterGenericChecker, null, ref typedeclarationGenerics, depth);
					CheckTypeDeclarationAssociatedTypes (returnAssociateChecker, parameterAssociateChecker, ref typedeclarationAssociatedTypes);
					Exdent ();
					sw.WriteLineWithIndent ($"</func>");

					lastWrittenClassSignature = signature.ToString ();
				}

			}
			return (typedeclarationGenerics, typedeclarationAssociatedTypes);
		}

		public static (List<string>, List<string>) WriteProtocolBasedMethods (this StreamWriter sw, ProtocolContents p)
		{
			if (!IsValidProtocolBasedMembers (p))
				return (null, null);

			var protocols = new List<SwiftReflector.Demangling.TLFunction> ();
			protocols.AddRange (p.FunctionsOfUnknownDestination.ToList ());
			protocols.Sort ((type1, type2) => String.CompareOrdinal (type1.Signature.Name.ToString (), type2.Signature.Name.ToString ()));
			var typedeclarationGenerics = new List<string> ();
			var typedeclarationAssociatedTypes = new List<string> ();

			var lastWrittenProtocolSignature = string.Empty;
			foreach (var protocol in protocols) {
				if (protocol.Signature.ToString () == lastWrittenProtocolSignature)
					continue;
				sw.WriteWithIndent ($"<func");
				Indent ();
				sw.WriteTypeValue ("name", StringBuilderHelper.EscapeCharacters (protocol.Signature.Name.ToString ()));
				sw.WriteTypeValue ("operatorKind", protocol.Operator.ToString ());
				sw.WriteTypeValue ("isStatic", CheckStaticProtocolMethod (protocol).ToString ());

				var returnGenericChecker = sw.WriteReturnType (protocol.Signature, 0, protocol.Signature.GenericArguments.Count, protocol.Signature.GenericParameterCount);
				var returnAssociateChecker = CheckParameterAssociateTypes (protocol.Signature.ReturnType, protocol.Signature, 0);
				sw.WriteTypeValue ("hasThrows", protocol.Signature.CanThrow.ToString ());
				sw.WriteTypeValue ("isPossiblyIncomplete", "True");

				sw.WriteConstantAttributes ();
				var parameterGenericChecker = sw.WriteParameters (protocol.Signature, 0);
				var parameterAssociateChecker = CheckParameterAssociateTypes (protocol.Signature.Parameters, protocol.Signature, 0);

				CheckTypeDeclarationGenerics (returnGenericChecker, parameterGenericChecker, null, ref typedeclarationGenerics, 0);
				CheckTypeDeclarationAssociatedTypes (returnAssociateChecker, parameterAssociateChecker, ref typedeclarationAssociatedTypes);
				Exdent ();
				sw.WriteLineWithIndent ($"</func>");

				lastWrittenProtocolSignature = protocol.Signature.ToString ();
			}
			return (typedeclarationGenerics, typedeclarationAssociatedTypes);
		}

		static List<string> CheckReturnAssociateTypes (SwiftReflector.SwiftType returnType)
		{
			if (returnType == null)
				return null;

			var retType = returnType as SwiftReflector.SwiftGenericArgReferenceType;
			if (retType == null)
				return null;

			var associatedTypeList = new List<string> ();
			foreach (var associatedType in retType.AssociatedTypePath) {
				if (!associatedTypeList.Contains (associatedType)) {
					associatedTypeList.Add (associatedType);
				}
			}
			return associatedTypeList;
		}

		static List<string> CheckParameterAssociateTypes (object parameters, SwiftReflector.SwiftBaseFunctionType signature, int depth)
		{
			if (parameters == null)
				return null;

			if (depth > 0 && ((signature.GenericParameterCount == 0 && signature.GenericArguments.Count == 0) && !(signature.ToString ().Contains ("0,") && signature.ToString ().Contains ("1,")))) {
				return null;
			}

			var associatedTypeList = new List<string> ();

			switch (parameters.GetType ().Name) {
			case "SwiftGenericArgReferenceType":
				HandleAssociateGenericArgType (parameters as SwiftReflector.SwiftGenericArgReferenceType, ref associatedTypeList);
				break;
			case "SwiftTupleType":
				HandleAssociateTupleType (parameters as SwiftReflector.SwiftTupleType, ref associatedTypeList);
				break;
			case "SwiftFunctionType":
				HandleAssociateFunctionType (parameters as SwiftReflector.SwiftFunctionType, ref associatedTypeList);
				break;
			case "SwiftBoundGenericType":
				HandleAssociateBoundType (parameters as SwiftReflector.SwiftBoundGenericType, ref associatedTypeList);
				break;
			default:
				break;
			}

			return associatedTypeList;
		}

		static void HandleAssociateTupleType (SwiftReflector.SwiftTupleType parameters, ref List<string> associatedTypeList)
		{
			foreach (var contents in parameters.Contents) {
				if (contents is SwiftReflector.SwiftGenericArgReferenceType)
					HandleAssociateGenericArgType (contents as SwiftReflector.SwiftGenericArgReferenceType, ref associatedTypeList);
				else if (contents is SwiftReflector.SwiftBoundGenericType)
					HandleAssociateBoundType (contents as SwiftReflector.SwiftBoundGenericType, ref associatedTypeList);
				else if (contents is SwiftReflector.SwiftFunctionType)
					HandleAssociateFunctionType (contents as SwiftReflector.SwiftFunctionType, ref associatedTypeList);
			}
		}

		static void HandleAssociateBoundType (SwiftReflector.SwiftBoundGenericType contents, ref List<string> associatedTypeList)
		{
			foreach (var boundTypes in contents.BoundTypes) {
				var associatedGenericArgTypes = boundTypes as SwiftReflector.SwiftGenericArgReferenceType;
				if (associatedGenericArgTypes == null)
					continue;
				HandleAssociateGenericArgType (associatedGenericArgTypes, ref associatedTypeList);
			}
		}

		static void HandleAssociateFunctionType (SwiftReflector.SwiftFunctionType parameter, ref List<string> associatedTypeList)
		{
			if (parameter.Parameters is SwiftReflector.SwiftGenericArgReferenceType) {
				HandleAssociateGenericArgType (parameter.Parameters as SwiftReflector.SwiftGenericArgReferenceType, ref associatedTypeList);
			} else if (parameter.Parameters is SwiftReflector.SwiftBoundGenericType) {
				HandleAssociateBoundType (parameter.Parameters as SwiftReflector.SwiftBoundGenericType, ref associatedTypeList);
			}
		}

		static void HandleAssociateGenericArgType (SwiftReflector.SwiftGenericArgReferenceType parameter, ref List<string> associatedTypeList)
		{
			foreach (var associatedType in parameter.AssociatedTypePath) {
				if (!associatedTypeList.Contains (associatedType)) {
					associatedTypeList.Add (associatedType);
				}
			}
		}

		static List<string> WriteReturnType (this StreamWriter sw, SwiftReflector.SwiftBaseFunctionType signature, int depth, int genericArgumentsCount = 0, int genericParameterCount = 0)
		{
			if (signature == null)
				return null;

			var returnType = signature.ReturnType;
			if (returnType != null && !String.IsNullOrEmpty (returnType.ToString ()) && returnType.ToString () != "()") {
				var enhancedReturn = StringBuilderHelper.EnhanceReturn (signature, depth, genericArgumentsCount, genericParameterCount);
				var escapedReturn = StringBuilderHelper.EscapeCharacters (enhancedReturn);
				if (escapedReturn != null) {
					sw.WriteTypeValue ("returnType", escapedReturn);
					var retList = new List<string> ();
					if (escapedReturn.Contains ("T0"))
						retList.Add ("T0");
					if (escapedReturn.Contains ("T1"))
						retList.Add ("T1");
					return retList;
				}
			} else {
				sw.WriteTypeValue ("returnType", "()");
			}
			return null;
		}

		static void WriteConstantAttributes (this StreamWriter sw)
		{
			//elements not present
			sw.WriteTypeValue ("accessibility", "Public");
			sw.WriteTypeValue ("isProperty", "False");
			sw.WriteTypeValue ("isFinal", "False");
			sw.WriteTypeValue ("isDeprecated", "False");
			sw.WriteTypeValue ("isUnavailable", "False");
			sw.WriteTypeValue ("isOptional", "False");
			sw.WriteTypeValue ("isRequired", "False");
			sw.WriteTypeValue ("isConvenienceInit", "False");
			sw.WriteLine (" objcSelector=\"\">");
		}

		static void WritePropertyMethodGenerics (this StreamWriter sw, List<string> propertyGenericChecker, int depth, List<string> methodGenericChecker, List<string> childrenGenerics = null)
		{
			var genericCounter = 0;

			for (int i = 0; i < 6; i++) {
				if (propertyGenericChecker != null) {
					if (propertyGenericChecker.Contains ($"T{i}")) {
						genericCounter++;
						continue;
					}
				}

				if (methodGenericChecker != null) {
					if (methodGenericChecker.Contains ($"T{i}")) {
						genericCounter++;
						continue;
					}
				}

				if (childrenGenerics != null) {
					if (childrenGenerics.Contains ($"T{i}")) {
						genericCounter++;
						continue;
					}
				}
			}

			sw.WriteTypeDeclarationGenericParameters (genericCounter, depth);
		}

		static void WriteTypeDeclarationGenericParameters (this StreamWriter sw, int genericsCount, int depth)
		{
			if (genericsCount == 0 || depth > 0) {
				return;
			}
			sw.WriteLineWithIndent ("<genericparameters>");
			Indent ();
			for (int i = 0; i < genericsCount; i++) {
				sw.WriteLineWithIndent ($"<param name=\"T{i}\"/>");
			}
			Exdent ();
			sw.WriteLineWithIndent ("</genericparameters>");
		}

		static void WriteInnerGenericParamters (this StreamWriter sw, SwiftReflector.SwiftBaseFunctionType signature, SwiftReflector.SwiftPropertyType property, int depth)
		{
			var genparams = 0;

			if (signature != null) {
				// this signature contains generic parameters at the typedeclaration and function level
				if (signature.ToString ().Contains ("0,") && signature.ToString ().Contains ("1,")) {
					if (signature.ToString ().Contains ("1,0"))
						genparams += 1;
					if (signature.ToString ().Contains ("1,1"))
						genparams += 1;
				}

				// this signature contains generic parameters at the function level only and uses the (0,X)
				else if ((signature.GenericArguments.Count != 0 || signature.GenericParameterCount != 0) && signature.ToString ().Contains ("0,")) {
					if (signature.ToString ().Contains ("0,0"))
						genparams += 1;
					if (signature.ToString ().Contains ("0,1"))
						genparams += 1;
				}

				// this signature contains generic parameters at the function level only and uses the (1,X)
				else if ((signature.GenericArguments.Count != 0 || signature.GenericParameterCount != 0) && signature.ToString ().Contains ("1,")) {
					if (signature.ToString ().Contains ("1,0"))
						genparams += 1;
					if (signature.ToString ().Contains ("1,1"))
						genparams += 1;
				}
			}

			if (genparams == 0)
				return;

			var kNames = "TUVWABCDEFGHIJKLMN";
			var depthPrefix = kNames [depth + 1];

			sw.WriteLineWithIndent ("<genericparameters>");
			Indent ();
			for (int i = 0; i < genparams; i++) {
				sw.WriteLineWithIndent ($"<param name=\"{depthPrefix}{i}\"/>");
			}
			Exdent ();
			sw.WriteLineWithIndent ("</genericparameters>");
		}

		static void WriteAssociateTypes (this StreamWriter sw, List<string> methodAssociatedChecker, List<string> propertyAssociatedChecker)
		{
			var associateTypes = new List<string> ();
			if (methodAssociatedChecker != null) {
				foreach (var associateType in methodAssociatedChecker) {
					if (!associateTypes.Contains (associateType))
						associateTypes.Add (associateType);
				}
			}

			if (propertyAssociatedChecker != null) {
				foreach (var associateType in propertyAssociatedChecker) {
					if (!associateTypes.Contains (associateType))
						associateTypes.Add (associateType);
				}
			}

			if (associateTypes.Count == 0)
				return;

			sw.WriteLineWithIndent ("<associatedtypes>");
			Indent ();
			for (int i = 0; i < associateTypes.Count; i++) {
				sw.WriteLineWithIndent ($"<associatedtype name=\"{associateTypes [i]}\"/>");
			}
			Exdent ();
			sw.WriteLineWithIndent ("</associatedtypes>");

		}

		static void CheckTypeDeclarationGenerics (List<string> fromReturn, List<string> fromParameters, string propertyType, ref List<string> typedeclarationGenerics, int depth)
		{
			if (fromReturn != null && fromReturn.Contains ("T0") && !typedeclarationGenerics.Contains ("T0"))
				typedeclarationGenerics.Add ("T0");
			if (fromReturn != null && fromReturn.Contains ("T1") && !typedeclarationGenerics.Contains ("T1"))
				typedeclarationGenerics.Add ("T1");

			if (fromParameters != null && fromParameters.Contains ("T0") && !typedeclarationGenerics.Contains ("T0"))
				typedeclarationGenerics.Add ("T0");
			if (fromParameters != null && fromParameters.Contains ("T1") && !typedeclarationGenerics.Contains ("T1"))
				typedeclarationGenerics.Add ("T1");

			if (propertyType != null && propertyType.Contains ("T0") && !typedeclarationGenerics.Contains ("T0"))
				typedeclarationGenerics.Add ("T0");
			if (propertyType != null && propertyType.Contains ("T1") && !typedeclarationGenerics.Contains ("T1"))
				typedeclarationGenerics.Add ("T1");
		}

		static void CheckTypeDeclarationAssociatedTypes (List<string> fromReturn, List<string> fromParameters, ref List<string> typedeclarationAssociatedTypes)
		{
			if (fromReturn != null) {
				foreach (var associatedType in fromReturn) {
					if (!typedeclarationAssociatedTypes.Contains (associatedType))
						typedeclarationAssociatedTypes.Add (associatedType);
				}
			}
			if (fromParameters != null) {
				foreach (var associatedType in fromParameters) {
					if (!typedeclarationAssociatedTypes.Contains (associatedType))
						typedeclarationAssociatedTypes.Add (associatedType);
				}
			}
		}

		// TODO add the hasInstance functionality, also for constructors
		// I will add this in when porting this use XElements as this will be much easier
		// for now, I will add fix to xml cleaner
		static List<string> WriteParameters (this StreamWriter sw, SwiftReflector.SwiftBaseFunctionType signature, int depth, bool hasInstance = false)
		{
			var parameterString = signature.Parameters.ToString ();
			var isVariadic = signature.IsVariadic;

			sw.WriteLineWithIndent ($"<parameterlists>");
			Indent ();

			var parameterListIndex = 0;

			sw.WriteLineWithIndent ($"<parameterlist index=\"{parameterListIndex}\">");
			Indent ();

			// if the function has an Instance or is a constructor, we will need to add additional
			// parameter list containing 'self'
			if (hasInstance || signature.IsConstructor) {

			}



			List<Tuple<string, string>> parameters = new List<Tuple<string, string>> ();

			var genericCheckerList = new List<string> ();

			if (parameterString != null)
				parameters = StringBuilderHelper.SeperateParameters (signature, depth, signature.GenericArguments.Count, signature.GenericParameterCount);

			if (parameterString != null && parameters != null) {
				for (var i = 0; i < parameters.Count; i++) {
					try {
						string privateName;
						if (parameters [i].Item1 == "_" || string.IsNullOrEmpty (parameters [i].Item1)) {
							privateName = $"private{privateIndex}";
							privateIndex++;
						} else
							privateName = parameters [i].Item1;

						string type = TypeSpecParser.Parse (parameters [i].Item2).ToString ();
						var handledClosureType = StringBuilderHelper.ReapplyClosureParenthesis (type);
						var escapedType = StringBuilderHelper.EscapeCharacters (handledClosureType);
						if (escapedType.Contains ("T0"))
							genericCheckerList.Add ("T0");
						if (escapedType.Contains ("T1"))
							genericCheckerList.Add ("T1");
						sw.WriteLineWithIndent ($"<parameter index=\"{i}\" publicName=\"{parameters [i].Item1}\" privateName=\"{privateName}\" type=\"{escapedType}\" isVariadic=\"{isVariadic}\"/>");
					} catch (Exception e) {
						Console.WriteLine ($"Problem Parsing the type: {parameters [i].Item2} with exception {e.Message}");
					}
				}
			}
			Exdent ();
			sw.WriteLineWithIndent ($"</parameterlist>");

			Exdent ();
			sw.WriteLineWithIndent ($"</parameterlists>");
			return genericCheckerList;
		}

		static void WriteFilteredParameters (this StreamWriter sw, string parameterType, string isVariadic, bool isGetter)
		{
			sw.WriteLineWithIndent ($"<parameterlists>");
			Indent ();
			sw.WriteLineWithIndent ($"<parameterlist index=\"0\">");
			Indent ();

			sw.WriteGetSetParameter (parameterType, isVariadic, isGetter);

			Exdent ();
			sw.WriteLineWithIndent ($"</parameterlist>");

			Exdent ();
			sw.WriteLineWithIndent ($"</parameterlists>");
		}

		static List<string> WriteInnerX (this StreamWriter sw, ClassContents c, Dictionary<string, List<ClassContents>> innerXDictionary, int depth)
		{
			var name = c.Name.ToString ();
			if (!innerXDictionary.ContainsKey (name))
				return null;

			var genericsAccumulator = new List<string> ();
			var innerXClassContents = innerXDictionary [name];
			foreach (var classContent in innerXClassContents) {
				var typeString = classContent.TypeDescriptor.Class.EntityKind.ToString ();

				if (typeString != "Class" && typeString != "Struct" && typeString != "Enum")
					return null;

				typeString = typeString.ToLower ();
				sw.WriteBasicOpener ($"inner{typeString}");

				switch (typeString) {
				case "class":
					var classGenericsLists = sw.WriteClassContents (classContent, "class", innerXDictionary, depth + 1);
					genericsAccumulator.AddRange (classGenericsLists.Item1);
					genericsAccumulator.AddRange (classGenericsLists.Item2);
					break;
				case "struct":
					var structGenericsLists = sw.WriteClassContents (classContent, "struct", innerXDictionary, depth + 1);
					genericsAccumulator.AddRange (structGenericsLists.Item1);
					genericsAccumulator.AddRange (structGenericsLists.Item2);
					break;
				case "enum":
					var enumGenericsLists = sw.WriteClassContents (classContent, "enum", innerXDictionary, depth + 1);
					genericsAccumulator.AddRange (enumGenericsLists.Item1);
					genericsAccumulator.AddRange (enumGenericsLists.Item2);
					break;

				}
				sw.WriteBasicCloser ($"inner{typeString}");
			}
			return genericsAccumulator;
		}

		static void WriteGetSetParameter (this StreamWriter sw, string parameterType, string isVariadic, bool isGetter)
		{
			if (isGetter)
				sw.WriteLineWithIndent ($"<parameter index=\"0\" publicName=\"_\" privateName=\"privateName\" type=\"{parameterType}\" isVariadic=\"{isVariadic}\"/>");
			else
				sw.WriteLineWithIndent ($"<parameter index=\"0\" publicName=\"newValue\" privateName=\"newValue\" type=\"{parameterType}\" isVariadic=\"{isVariadic}\"/>");
		}

		static void WriteGetter (this StreamWriter sw, string name, string isStatic, string returnType, SwiftReflector.SwiftPropertyType getter, int depth)
		{
			sw.WriteWithIndent ($"<func");
			Indent ();
			sw.WriteTypeValue ("name", $"get_{name}");
			sw.WriteTypeValue ("isStatic", isStatic);
			sw.WriteTypeValue ("isProperty", "True");

			sw.WriteTypeValue ("returnType", returnType);
			sw.WriteTypeValue ("isPossiblyIncomplete", "False");
			//elements not present
			sw.WriteTypeValue ("operatorKind", "None");
			sw.WriteTypeValue ("hasThrows", "False");
			sw.WriteTypeValue ("accessibility", "Public");
			sw.WriteTypeValue ("propertyType", "Getter");
			sw.WriteTypeValue ("isFinal", "False");
			sw.WriteTypeValue ("isDeprecated", "False");
			sw.WriteTypeValue ("isUnavailable", "False");
			sw.WriteTypeValue ("isOptional", "False");
			sw.WriteTypeValue ("isRequired", "False");
			sw.WriteTypeValue ("isConvenienceInit", "False");
			sw.WriteLine (" objcSelector=\"\">");
			sw.WriteInnerGenericParamters (null, getter, depth);
			sw.WriteFilteredParameters (returnType, "False", true);
			Exdent ();
			sw.WriteLineWithIndent ($"</func>");
		}

		static void WriteSetter (this StreamWriter sw, string name, string isStatic, string returnType, SwiftReflector.SwiftPropertyType getter, int depth)
		{
			sw.WriteWithIndent ($"<func");
			Indent ();
			sw.WriteTypeValue ("name", $"set_{name}");
			sw.WriteTypeValue ("isStatic", isStatic);
			sw.WriteTypeValue ("isProperty", "True");
			sw.WriteReturnType (getter, depth);
			sw.WriteTypeValue ("isPossiblyIncomplete", "False");
			sw.WriteTypeValue ("propertyType", "Setter");
			sw.WriteTypeValue ("operatorKind", "None");
			sw.WriteTypeValue ("hasThrows", "False");
			//elements not present
			sw.WriteTypeValue ("accessibility", "Public");
			sw.WriteTypeValue ("isFinal", "False");
			sw.WriteTypeValue ("isDeprecated", "False");
			sw.WriteTypeValue ("isUnavailable", "False");
			sw.WriteTypeValue ("isOptional", "False");
			sw.WriteTypeValue ("isRequired", "False");
			sw.WriteTypeValue ("isConvenienceInit", "False");
			sw.WriteLine (" objcSelector=\"\">");
			sw.WriteInnerGenericParamters (null, getter, depth);
			sw.WriteFilteredParameters (returnType, "False", false);
			Exdent ();
			sw.WriteLineWithIndent ($"</func>");
		}

		static bool CheckStaticProtocolMethod (SwiftReflector.Demangling.TLFunction protocol)
		{
			switch (protocol.Signature.GetType ().ToString ()) {
				case "SwiftReflector.SwiftStaticFunctionThunkType":
					return true;
				case "SwiftReflector.SwiftPropertyType":
					// .IsStatic does not exist in some types including SwiftUncurriedFunctionThunkType and SwiftUncurriedFunctionType
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

		static bool ContainsValidClassBasedMembers (ClassContents contents)
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
