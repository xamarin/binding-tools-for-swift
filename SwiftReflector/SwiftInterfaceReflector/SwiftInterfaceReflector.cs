// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using static SwiftInterfaceParser;
using System.Text;
using Dynamo;
using SwiftReflector.TypeMapping;
using SwiftReflector.SwiftXmlReflection;

namespace SwiftReflector.SwiftInterfaceReflector {
	public class SwiftInterfaceReflector : SwiftInterfaceBaseListener {
		// swift-interface-format-version: 1.0
		const string kSwiftInterfaceFormatVersion = "// swift-interface-format-version:";
		// swift-compiler-version: Apple Swift version 5.3 (swiftlang-1200.0.29.2 clang-1200.0.30.1)
		const string kSwiftCompilerVersion = "// swift-compiler-version: ";
		// swift-module-flags: -target x86_64-apple-macosx10.9 -enable-objc-interop -ena
		const string kSwiftModuleFlags = "// swift-module-flags:";

		const string kModuleName = "module-name";
		const string kTarget = "target";

		const string kIgnore = "IGNORE";

		Stack<XElement> currentElement = new Stack<XElement> ();
		Version interfaceVersion;
		Version compilerVersion;

		List<string> importModules = new List<string> ();
		List<XElement> operators = new List<XElement> ();
		List<Tuple<Function_declarationContext, XElement>> functions = new List<Tuple<Function_declarationContext, XElement>> ();
		List<XElement> extensions = new List<XElement> ();
		Dictionary<string, string> moduleFlags = new Dictionary<string, string> ();
		List<string> nominalTypes = new List<string> ();
		string moduleName;
		TypeDatabase typeDatabase;

		public SwiftInterfaceReflector (TypeDatabase typeDatabase)
		{
			this.typeDatabase = Exceptions.ThrowOnNull (typeDatabase, nameof (typeDatabase));
		}

		public void Reflect (string inFile, Stream outStm)
		{
			Exceptions.ThrowOnNull (inFile, nameof (inFile));
			Exceptions.ThrowOnNull (outStm, nameof (outStm));

			var xDocument = Reflect (inFile);

			xDocument.Save (outStm);
			currentElement.Clear ();
		}

		public XDocument Reflect (string inFile)
		{
			try {
				Exceptions.ThrowOnNull (inFile, nameof (inFile));

				if (!File.Exists (inFile))
					throw new ParseException ($"Input file {inFile} not found");


				var fileName = Path.GetFileName (inFile);
				moduleName = fileName.Split ('.') [0];

				var module = new XElement ("module");
				currentElement.Push (module);

				var charStream = CharStreams.fromPath (inFile);
				var lexer = new SwiftInterfaceLexer (charStream);
				var tokenStream = new CommonTokenStream (lexer);
				var parser = new SwiftInterfaceParser (tokenStream);
				var walker = new ParseTreeWalker ();
				walker.Walk (this, parser.swiftinterface ());

				if (currentElement.Count != 1)
					throw new ParseException ("At end of parse, stack should contain precisely one element");

				if (module != currentElement.Peek ())
					throw new ParseException ("Expected the final element to be the initial module");

				PatchPossibleOperators ();
				PatchExtensionShortNames ();

				module.Add (new XAttribute ("name", moduleName));
				SetLanguageVersion (module);

				var tlElement = new XElement ("xamreflect", new XAttribute ("version", "1.0"),
					new XElement ("modulelist", module));
				var xDocument = new XDocument (new XDeclaration ("1.0", "utf-8", "yes"), tlElement);
				return xDocument;
			} catch (ParseException) {
				throw;
			} catch (Exception e) {
				throw new ParseException ($"Unknown error parsing {inFile}: {e.Message}", e.InnerException);
			}
		}

		public override void EnterComment ([NotNull] CommentContext context)
		{
			var commentText = context.GetText ();
			InterpretCommentText (commentText);
		}

		public override void EnterClass_declaration ([NotNull] Class_declarationContext context)
		{
			var inheritance = GatherInheritance (context.type_inheritance_clause (), forceProtocolInheritance: false);
			var isDeprecated = false;
			var isUnavailable = false;
			var isFinal = context.final_clause () != null;
			var isObjC = AttributesContains (context.attributes (), "objc");
			var accessibility = ToAccess (context.access_level_modifier ());
			var typeDecl = ToTypeDeclaration ("class", context.class_name ().GetText (),
				accessibility, isObjC, isFinal, isDeprecated, isUnavailable, inheritance, generics: null);
			var generics = HandleGenerics (context.generic_parameter_clause (), context.generic_where_clause ());
			if (generics != null)
				typeDecl.Add (generics);
			currentElement.Push (typeDecl);
		}

		public override void ExitClass_declaration ([NotNull] Class_declarationContext context)
		{
			var classElem = currentElement.Pop ();
			var givenClassName = classElem.Attribute ("name").Value;
			var actualClassName = context.class_name ().GetText ();
			if (givenClassName != actualClassName)
				throw new ParseException ($"class name mismatch on exit declaration: expected {actualClassName} but got {givenClassName}");
			AddClassToCurrentElement (classElem);
		}

		public override void EnterStruct_declaration ([NotNull] Struct_declarationContext context)
		{
			var isDeprecated = false;
			var isUnavailable = false;
			var isFinal = true; // structs are always final
			var isObjC = AttributesContains (context.attributes (), "objc");
			var accessibility = ToAccess (context.access_level_modifier ());
			var typeDecl = ToTypeDeclaration ("struct", context.struct_name ().GetText (),
				accessibility, isObjC, isFinal, isDeprecated, isUnavailable, inherits: null, generics: null);
			var generics = HandleGenerics (context.generic_parameter_clause (), context.generic_where_clause ());
			if (generics != null)
				typeDecl.Add (generics);
			currentElement.Push (typeDecl);
		}

		public override void ExitStruct_declaration ([NotNull] Struct_declarationContext context)
		{
			var structElem = currentElement.Pop ();
			var givenStructName = structElem.Attribute ("name").Value;
			var actualStructName = context.struct_name ().GetText ();
			if (givenStructName != actualStructName)
				throw new ParseException ($"struct name mismatch on exit declaration: expected {actualStructName} but got {givenStructName}");
			AddStructToCurrentElement (structElem);
		}

		public override void EnterEnum_declaration ([NotNull] Enum_declarationContext context)
		{
			var isDeprecated = false;
			var isUnavailable = false;
			var isFinal = true; // enums are always final
			var isObjC = AttributesContains (context.attributes (), "objc");
			var accessibility = ToAccess (context.access_level_modifier ());

			var typeDecl = ToTypeDeclaration ("enum", EnumName (context),
				accessibility, isObjC, isFinal, isDeprecated, isUnavailable, inherits: null, generics: null);
			var generics = HandleGenerics (EnumGenericParameters (context), EnumGenericWhere (context));
			if (generics != null)
				typeDecl.Add (generics);
			currentElement.Push (typeDecl);
		}

		public override void ExitEnum_declaration ([NotNull] Enum_declarationContext context)
		{
			var enumElem = currentElement.Pop ();
			var givenEnumName = enumElem.Attribute ("name").Value;
			var actualEnumName = EnumName (context);
			if (givenEnumName != actualEnumName)
				throw new ParseException ($"enum name mismatch on exit declaration: expected {actualEnumName} but got {givenEnumName}");
			AddEnumToCurrentElement (enumElem);
		}

		static string EnumName (Enum_declarationContext context)
		{
			return context.union_style_enum () != null ?
				context.union_style_enum ().enum_name ().GetText () :
				context.raw_value_style_enum ().enum_name ().GetText ();
		}

		public override void EnterProtocol_declaration ([NotNull] Protocol_declarationContext context)
		{
			var inheritance = GatherInheritance (context.type_inheritance_clause (), forceProtocolInheritance: true);
			var isDeprecated = false;
			var isUnavailable = false;
			var isFinal = true; // protocols don't have final
			var isObjC = AttributesContains (context.attributes (), "objc");
			var accessibility = ToAccess (context.access_level_modifier ());
			var typeDecl = ToTypeDeclaration ("protocol", context.protocol_name ().GetText (),
				accessibility, isObjC, isFinal, isDeprecated, isUnavailable, inheritance, generics: null);
			currentElement.Push (typeDecl);
		}

		public override void ExitProtocol_declaration ([NotNull] Protocol_declarationContext context)
		{
			var protocolElem = currentElement.Pop ();
			var givenProtocolName = protocolElem.Attribute ("name").Value;
			var actualProtocolName = context.protocol_name ().GetText ();
			if (givenProtocolName != actualProtocolName)
				throw new ParseException ($"protocol name mismatch on exit declaration: expected {actualProtocolName} but got {givenProtocolName}");
			if (currentElement.Peek ().Name != "module")
				throw new ParseException ($"Expected a module on the element stack but found {currentElement.Peek ()}");
			currentElement.Peek ().Add (protocolElem);
		}

		public override void EnterProtocol_associated_type_declaration ([NotNull] Protocol_associated_type_declarationContext context)
		{
			var conformingProtocols = GatherConformingProtocols (context.type_inheritance_clause ());
			var defaultDefn = context.typealias_assignment ()?.type ().GetText ();
			var assocType = new XElement ("associatedtype",
				new XAttribute ("name", context.typealias_name ().GetText ()));
			if (defaultDefn != null)
				assocType.Add (new XAttribute ("defaulttype", defaultDefn));
			if (conformingProtocols != null && conformingProtocols.Count > 0) {
				var confomingElem = new XElement ("conformingprotocols", conformingProtocols.ToArray ());
				assocType.Add (confomingElem);
			}
			AddAssociatedTypeToCurrentElement (assocType);
		}

		List<XElement> GatherConformingProtocols (Type_inheritance_clauseContext context)
		{
			if (context == null)
				return null;
			var elems = new List<XElement> ();
			if (context.class_requirement () != null) {
				// not sure what to do here
				// this is just the keyword 'class'
			}
			var inheritance = context.type_inheritance_list ();
			while (inheritance != null) {
				var name = inheritance.type_identifier ()?.GetText ();
				if (name != null)
					elems.Add (new XElement ("conformingprotocol", new XAttribute ("name", name)));
				inheritance = context.type_inheritance_list ();
			}
			return elems;
		}

		static Generic_parameter_clauseContext EnumGenericParameters (Enum_declarationContext context)
		{
			return context.union_style_enum ()?.generic_parameter_clause () ??
				context.raw_value_style_enum ()?.generic_parameter_clause ();
		}

		static Generic_where_clauseContext EnumGenericWhere (Enum_declarationContext context)
		{
			return context.union_style_enum ()?.generic_where_clause () ??
				context.raw_value_style_enum ()?.generic_where_clause ();
		}

		public override void EnterFunction_declaration ([NotNull] Function_declarationContext context)
		{
			var head = context.function_head ();
			var signature = context.function_signature ();

			var name = context.function_name ().GetText ();
			var returnType = signature.function_result () != null ? signature.function_result ().type ().GetText () : "()";
			var accessibility = AccessibilityFromModifiers (head.declaration_modifiers ());
			var isStatic = IsStaticOrClass (head.declaration_modifiers ());
			var hasThrows = signature.throws_clause () != null || signature.rethrows_clause () != null;
			var isFinal = ModifiersContains (head.declaration_modifiers (), "final");
			var isOptional = ModifiersContains (head.declaration_modifiers (), "optional");
			var isConvenienceInit = false;
			var operatorKind = "None";
			var isDeprecated = false;
			var isUnavailable = false;
			var isMutating = ModifiersContains (head.declaration_modifiers (), "mutating");
			var isRequired = ModifiersContains (head.declaration_modifiers (), "required");
			var isProperty = false;
			var functionDecl = ToFunctionDeclaration (name, returnType, accessibility, isStatic, hasThrows,
				isFinal, isOptional, isConvenienceInit, objCSelector: null, operatorKind,
				isDeprecated, isUnavailable, isMutating, isRequired, isProperty);
			var generics = HandleGenerics (context.generic_parameter_clause (), context.generic_where_clause ());
			if (generics != null)
				functionDecl.Add (generics);

			currentElement.Push (functionDecl);

			if (isStatic || !IsInInstance ())
				functions.Add (new Tuple<Function_declarationContext, XElement> (context, functionDecl));
		}

		public override void ExitFunction_declaration ([NotNull] Function_declarationContext context)
		{
			ExitFunctionWithName (context.function_name ().GetText ());
		}

		void ExitFunctionWithName (string expectedName)
		{
			var functionDecl = currentElement.Pop ();
			if (functionDecl.Name != "func")
				throw new ParseException ($"Expected a func node but got a {functionDecl.Name}");
			var givenName = functionDecl.Attribute ("name");
			if (givenName == null)
				throw new ParseException ("func node doesn't have a name element");
			if (givenName.Value != expectedName)
				throw new ParseException ($"Expected a func node with name {expectedName} but got {givenName.Value}");

			AddElementToParentMembers (functionDecl);
		}

		XElement PeekAsFunction ()
		{
			var functionDecl = currentElement.Peek ();
			if (functionDecl.Name != "func")
				throw new ParseException ($"Expected a func node but got a {functionDecl.Name}");
			return functionDecl;
		}

		void AddElementToParentMembers (XElement elem)
		{
			var parent = currentElement.Peek ();
			var memberElem = GetOrCreate (parent, "members");
			memberElem.Add (elem);
		}

		bool IsInInstance ()
		{
			var parent = currentElement.Peek ();
			return parent.Name != "module";
		}

		public override void EnterInitializer_declaration ([NotNull] Initializer_declarationContext context)
		{
			var head = context.initializer_head ();

			var name = ".ctor";

			// may be optional, otherwise return type is the instance type
			var returnType = GetInstanceName () + (head.OpQuestion () != null ? "?" : "");
			var accessibility = AccessibilityFromModifiers (head.declaration_modifiers ());
			var isStatic = true;
			var hasThrows = context.throws_clause () != null || context.rethrows_clause () != null;
			var isFinal = ModifiersContains (head.declaration_modifiers (), "final");
			var isOptional = ModifiersContains (head.declaration_modifiers (), "optional");
			var isConvenienceInit = ModifiersContains (head.declaration_modifiers (), "convenience");
			var operatorKind = "None";
			var isDeprecated = false;
			var isUnavailable = false;
			var isMutating = ModifiersContains (head.declaration_modifiers (), "mutating");
			var isRequired = ModifiersContains (head.declaration_modifiers (), "required");
			var isProperty = false;
			var functionDecl = ToFunctionDeclaration (name, returnType, accessibility, isStatic, hasThrows,
				isFinal, isOptional, isConvenienceInit, objCSelector: null, operatorKind,
				isDeprecated, isUnavailable, isMutating, isRequired, isProperty);
			currentElement.Push (functionDecl);
		}

		public override void ExitInitializer_declaration ([NotNull] Initializer_declarationContext context)
		{
			ExitFunctionWithName (".ctor");
		}

		public override void EnterDeinitializer_declaration ([NotNull] Deinitializer_declarationContext context)
		{
			var name = ".dtor";
			var returnType = "()";
			// this might have to be forced to public, otherwise deinit is always internal, which it
			// decidedly is NOT.
			var accessibility = "Public";
			var isStatic = false;
			var hasThrows = false;
			var isFinal = ModifiersContains (context.declaration_modifiers (), "final");
			var isOptional = ModifiersContains (context.declaration_modifiers (), "optional");
			var isConvenienceInit = false;
			var operatorKind = "None";
			var isDeprecated = false;
			var isUnavailable = false;
			var isMutating = ModifiersContains (context.declaration_modifiers (), "mutating");
			var isRequired = ModifiersContains (context.declaration_modifiers (), "required");
			var isProperty = false;
			var functionDecl = ToFunctionDeclaration (name, returnType, accessibility, isStatic, hasThrows,
				isFinal, isOptional, isConvenienceInit, objCSelector: null, operatorKind,
				isDeprecated, isUnavailable, isMutating, isRequired, isProperty);

			// always has two parameter lists: (instance)()
			currentElement.Push (functionDecl);
			var parameterLists = new XElement ("parameterlists", MakeInstanceParameterList ());
			currentElement.Pop ();

			parameterLists.Add (new XElement ("parameterlist", new XAttribute ("index", "1")));
			functionDecl.Add (parameterLists);

			currentElement.Push (functionDecl);
		}

		public override void ExitDeinitializer_declaration ([NotNull] Deinitializer_declarationContext context)
		{
			ExitFunctionWithName (".dtor");
		}

		public override void EnterSubscript_declaration ([NotNull] Subscript_declarationContext context)
		{
			// subscripts are...funny.
			// They have one parameter list but expand out to two function declarations
			// To handle this, we process the parameter list here for the getter
			// If there's a setter, we make one of those too.
			// Then since we're effectively done, we push a special XElement on the stack
			// named IGNORE which will make the parameter list event handler exit.
			// On ExitSubscript_declaration, we remove the IGNORE tag

			var head = context.subscript_head ();
			var resultType = context.subscript_result ().GetText ();
			var accessibility = AccessibilityFromModifiers (head.declaration_modifiers ());
			var isDeprecated = false;
			var isUnavailable = false;
			var isStatic = false;
			var hasThrows = false;
			var isFinal = ModifiersContains (head.declaration_modifiers (), "final");
			var isOptional = ModifiersContains (head.declaration_modifiers (), "optional");
			var isMutating = ModifiersContains (head.declaration_modifiers (), "mutating");
			var isRequired = ModifiersContains (head.declaration_modifiers (), "required");
			var isProperty = true;

			var getParamList = MakeParamterList (head.parameter_clause ().parameter_list (), 1);
			var getFunc = ToFunctionDeclaration ("get_subscript", resultType, accessibility, isStatic, hasThrows,
				isFinal, isOptional, isConvenienceInit: false, objCSelector: null, "None",
				isDeprecated, isUnavailable, isMutating, isRequired, isProperty);

			currentElement.Push (getFunc);
			var getParamLists = new XElement ("parameterlists", MakeInstanceParameterList (), getParamList);
			currentElement.Pop ();

			getFunc.Add (getParamLists);

			AddElementToParentMembers (getFunc);

			var setParamList = context.getter_setter_keyword_block ()?.setter_keyword_clause () != null
				? MakeParamterList (head.parameter_clause ().parameter_list (), 1) : null;


			if (setParamList != null) {
				var index = setParamList.Elements ().Count ();
				var parmName = context.getter_setter_keyword_block ().setter_keyword_clause ().new_value_name ()?.GetText ()
					?? "newValue";
				var newValueParam = new XElement ("parameter", new XAttribute (nameof (index), index.ToString ()),
					new XAttribute ("type", resultType), new XAttribute ("publicName", parmName),
					new XAttribute ("privateName", parmName), new XAttribute ("isVariadic", false));
				setParamList.Add (newValueParam);

				var setFunc = ToFunctionDeclaration ("set_subscript", "()", accessibility, isStatic, hasThrows,
					isFinal, isOptional, isConvenienceInit: false, objCSelector: null, "None",
					isDeprecated, isUnavailable, isMutating, isRequired, isProperty);

				currentElement.Push (setFunc);
				var setParamLists = new XElement ("parameterlists", MakeInstanceParameterList (), setParamList);
				currentElement.Pop ();

				setFunc.Add (setParamLists);
				AddElementToParentMembers (setFunc);
			}

			// this makes the subscript parameter list get ignored because we already handled it.
			PushIgnore ();
		}

		public override void ExitSubscript_declaration ([NotNull] Subscript_declarationContext context)
		{
			PopIgnore ();
		}

		public override void EnterVariable_declaration ([NotNull] Variable_declarationContext context)
		{
			var head = context.variable_declaration_head ();
			var resultType = TrimColon (context.type_annotation ().GetText ());
			var accessibility = AccessibilityFromModifiers (head.declaration_modifiers ());
			var isDeprecated = false;
			var isUnavailable = false;
			var isStatic = ModifiersContains (head.declaration_modifiers (), "static");
			var hasThrows = false;
			var isFinal = ModifiersContains (head.declaration_modifiers (), "final");
			var isLet = head.let_clause () != null;
			var isOptional = ModifiersContains (head.declaration_modifiers (), "optional");
			var isMutating = ModifiersContains (head.declaration_modifiers (), "mutating");
			var isRequired = ModifiersContains (head.declaration_modifiers (), "required");
			var isProperty = true;

			var getParamList = new XElement ("parameterlist", new XAttribute ("index", "1"));
			var getFunc = ToFunctionDeclaration ("get_" + context.variable_name ().GetText (),
				resultType, accessibility, isStatic, hasThrows, isFinal, isOptional,
				isConvenienceInit: false, objCSelector: null, operatorKind: "None", isDeprecated,
				isUnavailable, isMutating, isRequired, isProperty);

			currentElement.Push (getFunc);
			var getParamLists = new XElement ("parameterlists", MakeInstanceParameterList (), getParamList);
			currentElement.Pop ();
			getFunc.Add (getParamLists);
			AddElementToParentMembers (getFunc);

			var setParamList = context.getter_setter_keyword_block ()?.setter_keyword_clause () != null ?
				new XElement ("parameterlist", new XAttribute ("index", "1")) : null;

			if (setParamList != null) {
				var parmName = context.getter_setter_keyword_block ().setter_keyword_clause ().new_value_name ()?.GetText ()
					?? "newValue";
				var newValueParam = new XElement ("parameter", new XAttribute ("index", "0"),
					new XAttribute ("type", resultType), new XAttribute ("publicName", parmName),
					new XAttribute ("privateName", parmName), new XAttribute ("isVariadic", false));
				setParamList.Add (newValueParam);
				var setFunc = ToFunctionDeclaration ("set_" + context.variable_name ().GetText (),
					"()", accessibility, isStatic, hasThrows, isFinal, isOptional,
					isConvenienceInit: false, objCSelector: null, operatorKind: "None", isDeprecated,
					isUnavailable, isMutating, isRequired, isProperty);

				currentElement.Push (setFunc);
				var setParamLists = new XElement ("parameterlists", MakeInstanceParameterList (), setParamList);
				currentElement.Pop ();

				setFunc.Add (setParamLists);
				AddElementToParentMembers (setFunc);
			}

			var prop = new XElement ("property", new XAttribute ("name", context.variable_name ().GetText ()),
				new XAttribute (nameof (accessibility), accessibility),
				new XAttribute ("type", resultType),
				new XAttribute ("storage", "Computed"),
				new XAttribute (nameof (isStatic), XmlBool (isStatic)),
				new XAttribute (nameof (isLet), XmlBool (isLet)),
				new XAttribute (nameof (isDeprecated), XmlBool (isDeprecated)),
				new XAttribute (nameof (isUnavailable), XmlBool (isUnavailable)),				
				new XAttribute (nameof (isOptional), XmlBool (isOptional)));
			AddElementToParentMembers (prop);

			PushIgnore ();
		}

		public override void EnterExtension_declaration ([NotNull] Extension_declarationContext context)
		{
			var accessibility = ToAccess (context.access_level_modifier ());
			var onType = context.type_identifier ().GetText ();
			var inherits = GatherInheritance (context.type_inheritance_clause (), forceProtocolInheritance: true);
			// why, you say, why put a "kind" tag into an extension?
			// The reason is simple: this is a hack. Most of the contents
			// of an extension are the same as a class and as a result we can
			// pretend that it's a class and everything will work to fill it out
			// using the class/struct/enum code for members.
			var extensionElem = new XElement ("extension", accessibility,
				new XAttribute (nameof (onType), onType),
				new XAttribute ("kind", "class"));
			if (inherits?.Count > 0)
				extensionElem.Add (new XElement (nameof (inherits), inherits.ToArray ()));
			currentElement.Push (extensionElem);
			extensions.Add (extensionElem);
		}

		public override void ExitExtension_declaration ([NotNull] Extension_declarationContext context)
		{
			var extensionElem = currentElement.Pop ();
			var onType = extensionElem.Attribute ("onType");
			var givenOnType = onType.Value;
			var actualOnType = context.type_identifier ().GetText ();
			if (givenOnType != actualOnType)
				throw new Exception ($"extension type mismatch on exit declaration: expected {actualOnType} but got {givenOnType}");
			// remove the "kind" attribute - you've done your job.
			extensionElem.Attribute ("kind")?.Remove ();

			currentElement.Peek ().Add (extensionElem);
		}

		public override void ExitImport_statement ([NotNull] Import_statementContext context)
		{
			// this is something like: import class Foo.Bar
			// and we're not handling that yet
			if (context.import_kind () != null)
				return;
			importModules.Add (context.import_path ().GetText ());
		}

		public override void EnterOperator_declaration ([NotNull] Operator_declarationContext context)
		{
			var operatorElement = InfixOperator (context.infix_operator_declaration ())
				?? PostfixOperator (context.postfix_operator_declaration ())
				?? PrefixOperator (context.prefix_operator_declaration ());
			operators.Add (operatorElement);

			currentElement.Peek ().Add (operatorElement);
		}

		XElement InfixOperator (Infix_operator_declarationContext context)
		{
			if (context == null)
				return null;
			return GeneralOperator ("Infix", context.@operator (), context.infix_operator_group ()?.GetText () ?? "");
		}

		XElement PostfixOperator (Postfix_operator_declarationContext context)
		{
			if (context == null)
				return null;
			return GeneralOperator ("Postfix", context.@operator (), "");
		}

		XElement PrefixOperator (Prefix_operator_declarationContext context)
		{
			if (context == null)
				return null;
			return GeneralOperator ("Prefix", context.@operator (), "");
		}

		XElement GeneralOperator (string operatorKind, OperatorContext context, string precedenceGroup)
		{
			return new XElement ("operator",
				new XAttribute ("name", context.Operator ().GetText ()),
				new XAttribute (nameof (operatorKind), operatorKind),
				new XAttribute (nameof (precedenceGroup), precedenceGroup));
		}

		XElement HandleGenerics (Generic_parameter_clauseContext genericContext, Generic_where_clauseContext whereContext)
		{
			if (genericContext == null)
				return null;
			var genericElem = new XElement ("genericparameters");
			foreach (var generic in genericContext.generic_parameter_list ().generic_parameter ()) {
				var name = generic.type_name ().GetText ();
				var genParam = new XElement ("param", new XAttribute ("name", name));
				genericElem.Add (genParam);
				var whereType = generic.type_identifier ()?.GetText () ??
					generic.protocol_composition_type ()?.GetText ();
				if (whereType != null) {
					genericElem.Add (MakeConformanceWhere (name, whereType));
				}
			}

			if (whereContext == null)
				return genericElem;

			foreach (var requirement in whereContext.requirement_list ().requirement ()) {
				if (requirement.conformance_requirement () != null) {
					var name = requirement.conformance_requirement ().type_identifier () [0].GetText ();

					// if there is no protocol composition type, then it's the second type identifier
					var from = requirement.conformance_requirement ().protocol_composition_type ()?.GetText ()
						?? requirement.conformance_requirement ().type_identifier () [1].GetText ();
					genericElem.Add (MakeConformanceWhere (name, from));
				} else {
					var name = requirement.same_type_requirement ().type_identifier ().GetText ();
					var type = requirement.same_type_requirement ().type ().GetText ();
					genericElem.Add (MakeEqualityWhere (name, type));
				}
			}

			return genericElem;
		}

		XElement MakeConformanceWhere (string name, string from)
		{
			return new XElement ("where", new XAttribute (nameof (name), name),
				new XAttribute ("relationship", "inherits"),
				new XAttribute (nameof (from), from));
		}

		XElement MakeEqualityWhere (string firsttype, string secondtype)
		{
			return new XElement ("where", new XAttribute (nameof (firsttype), firsttype),
				new XAttribute ("relationship", "equals"),
				new XAttribute (nameof (secondtype), secondtype));
		}

		public override void ExitVariable_declaration ([NotNull] Variable_declarationContext context)
		{
			PopIgnore ();
		}

		void PushIgnore ()
		{
			currentElement.Push (new XElement (kIgnore));
		}

		void PopIgnore ()
		{
			var elem = currentElement.Pop ();
			if (elem.Name != kIgnore)
				throw new ParseException ($"Expected an {kIgnore} element, but got {elem}");
		}

		bool ShouldIgnore ()
		{
			return currentElement.Peek ().Name == kIgnore;
		}

		public override void EnterParameter_clause ([NotNull] Parameter_clauseContext context)
		{
			if (ShouldIgnore ())
				return;

			var parameterLists = new XElement ("parameterlists");
			XElement instanceList = MakeInstanceParameterList ();
			var formalIndex = 0;
			if (instanceList != null) {
				parameterLists.Add (instanceList);
				formalIndex = 1;
			}

			var formalArguments = MakeParamterList (context.parameter_list (), formalIndex);

			parameterLists.Add (formalArguments);
			currentElement.Peek ().Add (parameterLists);
		}

		XElement MakeParamterList (Parameter_listContext parmList, int index)
		{
			var formalArguments = new XElement ("parameterlist", new XAttribute ("index", index.ToString ()));

			if (parmList != null) {
				var i = 0;
				foreach (var parameter in parmList.parameter ()) {
					var parameterElement = ToParameterElement (parameter, i);
					formalArguments.Add (parameterElement);
					i++;
				}
			}
			return formalArguments;
		}

		XElement MakeInstanceParameterList ()
		{
			var topElem = currentElement.Peek ();
			if (topElem.Name == "module")
				return null;
			if (topElem.Name != "func")
				throw new ParseException ($"Expecting a func node but got {topElem.Name}");
			if (NominalParentAfter (0) == null)
				return null;
			var funcName = topElem.Attribute ("name").Value;
			var isStatic = topElem.Attribute ("isStatic").Value == "true";
			var isCtorDtor = IsCtorDtor (funcName);
			var isClass = NominalParentAfter (0).Attribute ("kind").Value == "class";
			var instanceName = GetInstanceName ();
			var type = $"{(isClass ? "" : "inout ")}{instanceName}{(isCtorDtor ? ".Type" : "")}";
			var parameter = new XElement ("parameter", new XAttribute ("type", type),
				new XAttribute ("index", "0"), new XAttribute ("publicName", ""),
				new XAttribute ("privateName", "self"), new XAttribute ("isVariadic", "false"));
			return new XElement ("parameterlist", new XAttribute ("index", "0"), parameter);
		}

		XElement NominalParentAfter (int start)
		{
			for (var i = start + 1; i < currentElement.Count; i++) {
				var elem = currentElement.ElementAt (i);
				if (IsNominal (elem))
					return elem;
			}
			return null;
		}

		bool IsNominal (XElement elem)
		{
			var kind = elem.Attribute ("kind")?.Value;
			return kind != null && (kind == "class" || kind == "struct" || kind == "enum" || kind == "protocol");
		}

		string GetInstanceName ()
		{
			var nameBuffer = new StringBuilder ();
			for (int i = 0; i < currentElement.Count; i++) {
				var elem = currentElement.ElementAt (i);
				if (IsNominal (elem)) {
					if (elem.Name == "extension")
						return elem.Attribute ("onType").Value;
					if (nameBuffer.Length > 0)
						nameBuffer.Insert (0, '.');
					nameBuffer.Insert (0, elem.Attribute ("name").Value);
					var generics = elem.Element ("genericparameters");
					if (generics != null) {
						AddGenericsToName (nameBuffer, generics);
					}
				}
			}
			nameBuffer.Insert (0, '.');
			var module = currentElement.Last ();
			nameBuffer.Insert (0, moduleName);
			return nameBuffer.ToString ();
		}

		void AddGenericsToName (StringBuilder nameBuffer, XElement generics)
		{
			var isFirst = true;
			foreach (var name in GenericNames (generics)) {
				if (isFirst) {
					nameBuffer.Append ("<");
					isFirst = false;
				} else {
					nameBuffer.Append (", ");
				}
				nameBuffer.Append (name);
			}
			if (!isFirst)
				nameBuffer.Append (">");
		}

		IEnumerable<string> GenericNames (XElement generics)
		{
			return generics.Elements ().Where (elem => elem.Name == "param").Select (elem => elem.Attribute ("name").Value);
		}

		XElement ToParameterElement (ParameterContext context, int index)
		{
			var typeAnnotation = context.type_annotation ();
			var isInOut = typeAnnotation.inout_clause () != null;
			var type = typeAnnotation.type ().GetText ();
			var publicName = context.external_parameter_name ()?.GetText () ?? "";
			var privateName = context.local_parameter_name ()?.GetText () ?? "";
			var isVariadic = context.range_operator () != null;
			var isEscaping = AttributesContains (typeAnnotation.attributes (), "escaping");
			var isAutoClosure = AttributesContains (typeAnnotation.attributes (), "autoclosure");
			var typeBuilder = new StringBuilder ();
			if (isEscaping)
				typeBuilder.Append ("@escaping[] ");
			if (isAutoClosure)
				typeBuilder.Append ("@autoclosure[] ");
			if (isInOut)
				typeBuilder.Append ("inout ");
			typeBuilder.Append (type);
			type = typeBuilder.ToString ();

			var paramElement = new XElement ("parameter", new XAttribute (nameof (index), index.ToString ()),
				new XAttribute (nameof (type), type), new XAttribute (nameof (publicName), publicName),
				new XAttribute (nameof (privateName), privateName), new XAttribute (nameof (isVariadic), XmlBool (isVariadic)));
			return paramElement;
		}

		List<XElement> GatherInheritance (Type_inheritance_clauseContext context, bool forceProtocolInheritance)
		{
			var inheritance = new List<XElement> ();
			if (context == null)
				return inheritance;
			var list = context.type_inheritance_list ();
			while (list != null) {
				var inheritanceKind = forceProtocolInheritance ? "protocol" :
					(inheritance.Count > 0 ? "protocol" : "unknown");
				var elem = new XElement ("inherit", new XAttribute ("type", list.type_identifier ().GetText ()),
					new XAttribute (nameof (inheritanceKind), inheritanceKind));
				inheritance.Add (elem);
				list = list.type_inheritance_list ();
			}

			return inheritance;
		}

		XElement ToTypeDeclaration (string kind, string name, string accessibility, bool isObjC,
			bool isFinal, bool isDeprecated, bool isUnavailable, List<XElement> inherits, XElement generics)
		{
			var xobjects = new List<XObject> ();
			if (generics != null)
				xobjects.Add (generics);
			xobjects.Add (new XAttribute (nameof (kind), kind));
			xobjects.Add (new XAttribute (nameof (name), name));
			xobjects.Add (new XAttribute (nameof (accessibility), accessibility));
			xobjects.Add (new XAttribute (nameof (isObjC), XmlBool (isObjC)));
			xobjects.Add (new XAttribute (nameof (isFinal), XmlBool (isFinal)));
			xobjects.Add (new XAttribute (nameof (isDeprecated), XmlBool (isDeprecated)));
			xobjects.Add (new XAttribute (nameof (isUnavailable), XmlBool (isUnavailable)));

			xobjects.Add (new XElement ("members"));
			if (inherits != null && inherits.Count > 0)
				xobjects.Add (new XElement (nameof (inherits), inherits.ToArray ()));
			return new XElement ("typedeclaration", xobjects.ToArray ());
		}


		XElement ToFunctionDeclaration (string name, string returnType, string accessibility,
			bool isStatic, bool hasThrows, bool isFinal, bool isOptional, bool isConvenienceInit,
			string objCSelector, string operatorKind, bool isDeprecated, bool isUnavailable,
			bool isMutating, bool isRequired, bool isProperty)
		{
			var decl = new XElement ("func", new XAttribute (nameof (name), name), new XAttribute (nameof (returnType), returnType),
				new XAttribute (nameof (accessibility), accessibility), new XAttribute (nameof (isStatic), XmlBool (isStatic)),
				new XAttribute (nameof (hasThrows), XmlBool (hasThrows)), new XAttribute (nameof (isFinal), XmlBool (isFinal)),
				new XAttribute (nameof (isOptional), XmlBool (isOptional)),
				new XAttribute (nameof (isConvenienceInit), XmlBool (isConvenienceInit)),
				new XAttribute (nameof (isDeprecated), XmlBool (isDeprecated)),
				new XAttribute (nameof (isUnavailable), XmlBool (isUnavailable)),
				new XAttribute (nameof (isRequired), XmlBool (isRequired)),
				new XAttribute (nameof (isProperty), XmlBool (isProperty)),
				new XAttribute (nameof (isMutating), XmlBool (isMutating)));

			if (operatorKind != null) {
				decl.Add (new XAttribute (nameof (operatorKind), operatorKind));
			}
			if (objCSelector != null) {
				decl.Add (new XAttribute (nameof (objCSelector), objCSelector));
			}
			return decl;
		}

		void PatchPossibleOperators ()
		{
			foreach (var func in functions) {
				var operatorKind = GetOperatorType (func.Item1);
				if (operatorKind != OperatorType.None) {
					func.Item2.Attribute (nameof (operatorKind))?.Remove ();
					func.Item2.SetAttributeValue (nameof (operatorKind), operatorKind.ToString ());
				}
			}
		}

		void PatchExtensionShortNames ()
		{
			foreach (var ext in extensions) {
				var onType = TypeSpecParser.Parse (ext.Attribute ("onType").Value);
				var replacementType = FullyQualify (onType);
				ext.Attribute ("onType").Value = replacementType.ToString ();
			}
		}

		TypeSpec FullyQualify (TypeSpec spec)
		{
			switch (spec.Kind) {
			case TypeSpecKind.Named:
				return FullyQualify (spec as NamedTypeSpec);
			case TypeSpecKind.Closure:
				return FullyQualify (spec as ClosureTypeSpec);
			case TypeSpecKind.ProtocolList:
				return FullyQualify (spec as ProtocolListTypeSpec);
			case TypeSpecKind.Tuple:
				return FullyQualify (spec as TupleTypeSpec);
			default:
				throw new NotImplementedException ($"unknown TypeSpec kind {spec.Kind}");
			}
		}

		TypeSpec FullyQualify (NamedTypeSpec named)
		{
			var dirty = false;
			var newName = named.Name;

			if (!named.Name.Contains (".")) {
				newName = ReplaceName (named.Name);
				dirty = true;
			}

			var genParts = new TypeSpec [named.GenericParameters.Count];
			var index = 0;
			foreach (var gen in named.GenericParameters) {
				var newGen = FullyQualify (gen);
				genParts[index++] = newGen;
				if (newGen != gen)
					dirty = true;
			}

			if (dirty) {
				var newNamed = new NamedTypeSpec (newName, genParts);
				newNamed.Attributes.AddRange (named.Attributes);
				return newNamed;
			}

			return named;
		}

		TypeSpec FullyQualify (TupleTypeSpec tuple)
		{
			var dirty = false;
			var parts = new TypeSpec [tuple.Elements.Count];
			var index = 0;
			foreach (var spec in tuple.Elements) {
				var newSpec = FullyQualify (spec);
				if (newSpec != spec)
					dirty = true;
				parts [index++] = newSpec;
			}

			if (dirty) {
				var newTup = new TupleTypeSpec (parts);
				newTup.Attributes.AddRange (tuple.Attributes);
				return newTup;
			}

			return tuple;
		}

		TypeSpec FullyQualify (ProtocolListTypeSpec protolist)
		{
			var dirty = false;
			var parts = new List<NamedTypeSpec> ();
			foreach (var named in protolist.Protocols.Keys) {
				var newNamed = FullyQualify (named);
				parts.Add (newNamed as NamedTypeSpec);
				if (newNamed != named)
					dirty = true;
			}

			if (dirty) {
				var newProto = new ProtocolListTypeSpec (parts);
				newProto.Attributes.AddRange (protolist.Attributes);
				return newProto;
			}

			return protolist;
		}

		TypeSpec FullyQualify (ClosureTypeSpec clos)
		{
			var dirty = false;
			var args = FullyQualify (clos.Arguments);
			if (args != clos.Arguments)
				dirty = true;
			var returnType = FullyQualify (clos.ReturnType);
			if (returnType != clos.ReturnType)
				dirty = true;

			if (dirty) {
				var newClosure = new ClosureTypeSpec (args, returnType);
				newClosure.Attributes.AddRange (clos.Attributes);
				return newClosure;
			}

			return clos;
		}

		string ReplaceName (string nonQualified)
		{
			Exceptions.ThrowOnNull (nonQualified, nameof (nonQualified));

			var localName = ReplaceLocalName (nonQualified);
			if (localName != null)
				return localName;
			var globalName = ReplaceGlobalName (nonQualified);
			if (globalName == null)
				throw new ParseException ($"Unable to find fully qualified name for non qualified type {nonQualified}");
			return globalName;
		}

		string ReplaceLocalName (string nonQualified)
		{
			foreach (var candidate in nominalTypes) {
				var candidateWithoutModule = StripModule (candidate);
				if (nonQualified == candidateWithoutModule)
					return candidate;
			}
			return null;
		}

		string ReplaceGlobalName (string nonQualified)
		{
			foreach (var module in importModules) {
				var candidateName = $"{module}.{nonQualified}";
				var entity = typeDatabase.TryGetEntityForSwiftName (candidateName);
				if (entity != null)
					return candidateName;
			}
			return null;
		}

		string StripModule (string fullyQualifiedName)
		{
			if (fullyQualifiedName.StartsWith (moduleName, StringComparison.Ordinal))
				// don't forget the '.'
				return fullyQualifiedName.Substring (moduleName.Length + 1);
			return fullyQualifiedName; 
		}

		static bool AttributesContains (AttributesContext context, string key)
		{
			if (context == null)
				return false;
			foreach (var attr in context.attribute ()) {
				if (attr.attribute_name ().GetText () == key)
					return true;
			}
			return false;
		}

		static bool AttributesContainsAny (AttributesContext context, string [] keys)
		{
			foreach (var attr in context.attribute ()) {
				var attrName = attr.attribute_name ().GetText ();
				foreach (var key in keys) {
					if (key == attrName)
						return true;
				}
			}
			return false;
		}

		static Dictionary<string, string> accessMap = new Dictionary<string, string> () {
			{ "public", "Public" },
			{ "private", "Private" },
			{ "open", "Open" },
			{ "internal", "Internal" },
		};

		string AccessibilityFromModifiers (Declaration_modifiersContext context)
		{
			// If there is no context, we need to search for the appropriate context
			// Swift has a number of "interesting" rules for implicitly defined accessibility
			// If the parent element is a protocol, it's public
			// If the parent is public, internal, or open then it's open
			// If the parent is private or fileprivate, then it's private

			// Note that I don't make any distinction between private and fileprivate
			// From our point of view, they're the same: they're things that we don't
			// have access to and don't care about in writing a reflector of the public
			// API.
			if (context == null) {
				var parentElem = NominalParentAfter (-1);
				if (parentElem == null)
					return "Internal";
				if (parentElem.Attribute ("kind").Value == "protocol")
					return "Public";
				switch (parentElem.Attribute ("accessibility").Value) {
				case "public":
				case "internal":
				case "open":
					return "Internal";
				case "private":
				case "fileprivate":
					return "Private";
				}
			}
			foreach (var modifer in context.declaration_modifier ()) {
				string result;
				if (accessMap.TryGetValue (modifer.GetText (), out result))
					return result;
			}
			return "internal";
		}

		static bool ModifiersContains (Declaration_modifiersContext context, string match)
		{
			if (context == null)
				return false;
			foreach (var modifier in context.declaration_modifier ()) {
				var text = modifier.GetText ();
				if (text == match)
					return true;
			}
			return false;
		}

		static bool ModifiersContainsAny (Declaration_modifiersContext context, string [] matches)
		{
			if (context == null)
				return false;
			foreach (var modifier in context.declaration_modifier ()) {
				var text = modifier.GetText ();
				foreach (var match in matches)
					if (text == match)
						return true;
			}
			return false;
		}

		static bool IsStaticOrClass (Declaration_modifiersContext context)
		{
			return ModifiersContainsAny (context, new string [] { "static", "class" });
		}

		static bool IsFinal (Declaration_modifiersContext context)
		{
			return ModifiersContains (context, "final");
		}

		void AddStructToCurrentElement (XElement elem)
		{
			var parentElement = GetOrCreateParentElement ("innerstructs");
			parentElement.Add (elem);
			RegisterNominal (elem);
		}

		void AddEnumToCurrentElement (XElement elem)
		{
			var parentElement = GetOrCreateParentElement ("innerenums");
			parentElement.Add (elem);
			RegisterNominal (elem);
		}

		void AddClassToCurrentElement (XElement elem)
		{
			var parentElement = GetOrCreateParentElement ("innerclasses");
			parentElement.Add (elem);
			RegisterNominal (elem);
		}

		void RegisterNominal (XElement elem)
		{
			var builder = new StringBuilder ();
			while (elem != null) {
				if (builder.Length > 0)
					builder.Insert (0, '.');
				var namePart = elem.Attribute ("name")?.Value ?? moduleName;
				builder.Insert (0, namePart);
				elem = elem.Parent;
			}
			nominalTypes.Add (builder.ToString ());
		}

		void AddAssociatedTypeToCurrentElement (XElement elem)
		{
			var parentElement = GetOrCreateParentElement ("associatedtypes");
			parentElement.Add (elem);
		}

		XElement GetOrCreateParentElement (string parentContainerName)
		{
			var current = currentElement.Peek ();
			if (current.Name == "module") {
				return current;
			}
			var container = GetOrCreate (current, parentContainerName);
			return container;
		}

		OperatorType GetOperatorType (Function_declarationContext context)
		{
			var localOp = LocalOperatorType (context);
			return localOp == OperatorType.None ? GlobalOperatorType (context.function_name ().GetText ())
				: localOp;
		}

		OperatorType LocalOperatorType (Function_declarationContext context)
		{
			var head = context.function_head ();
			if (!ModifiersContains (head.declaration_modifiers (), "static"))
				return OperatorType.None;


			// if the function declaration contains prefix 
			if (ModifiersContains (head.declaration_modifiers (), "prefix")) {
				return OperatorType.Prefix;
			} else if (ModifiersContains (head.declaration_modifiers (), "postfix")) {
				return OperatorType.Postfix;
			}

			var opName = context.function_name ().GetText ();

			foreach (var op in operators) {
				var targetName = op.Attribute ("name").Value;
				var targetKind = op.Attribute ("operatorKind").Value;
				if (opName == targetName && targetKind == "Infix")
					return OperatorType.Infix;
			}
			return OperatorType.None;
		}

		OperatorType GlobalOperatorType (string name)
		{
			foreach (var op in typeDatabase.FindOperators (importModules)) {
				if (op.Name == name)
					return op.OperatorType;
			}
			return OperatorType.None;
		}

		void InterpretCommentText (string commentText)
		{
			if (commentText.StartsWith (kSwiftInterfaceFormatVersion)) {
				AssignSwiftInterfaceFormat (commentText.Substring (kSwiftInterfaceFormatVersion.Length));
			} else if (commentText.StartsWith (kSwiftCompilerVersion)) {
				AssignSwiftCompilerVersion (commentText.Substring (kSwiftCompilerVersion.Length));
			} else if (commentText.StartsWith (kSwiftModuleFlags)) {
				ExtractModuleFlags (commentText.Substring (kSwiftModuleFlags.Length));
				moduleFlags.TryGetValue (kModuleName, out moduleName);
			}
		}

		void AssignSwiftInterfaceFormat (string formatVersion)
		{
			// when we get here, we should see something like
			// [white-space]*VERSION[white-space]
			formatVersion = formatVersion.Trim ();
			if (!Version.TryParse (formatVersion, out interfaceVersion))
				throw new ArgumentOutOfRangeException (nameof (formatVersion), $"Expected a version string in the interface format but got {formatVersion}");
		}

		void AssignSwiftCompilerVersion (string compilerVersion)
		{
			// when we get here, we should see something like:
			// [white-space]*Apple? Swift version VERSION (swiftlang-VERSION clang-VERSION)
			var parts = compilerVersion.Trim ().Split (' ', '\t'); // don't know if tab is a thing
									       // expect in the array:
									       // 0: Apple
									       // 1: Swift
									       // 2: verion
									       // 3: VERSION

			var swiftIndex = Array.IndexOf (parts, "Swift");
			if (swiftIndex < 0)
				throw new ArgumentOutOfRangeException (nameof (compilerVersion), $"Expected 'Swift' in the version string, but got {compilerVersion}");
			if (parts [swiftIndex + 1] != "version")
				throw new ArgumentOutOfRangeException (nameof (compilerVersion), $"Expected a compiler version string but got {compilerVersion}");
			var version = parts [swiftIndex + 2];
			if (version.EndsWith ("-dev", StringComparison.Ordinal))
				version = version.Substring (0, version.Length - "-dev".Length);
			if (!Version.TryParse (version, out this.compilerVersion))
				throw new ArgumentOutOfRangeException (nameof (compilerVersion), $"Expected a compiler version number but got {compilerVersion}");
		}

		void ExtractModuleFlags (string commentText)
		{
			var args = commentText.Trim ().Split (' ', '\t');
			int index = 0;
			while (index < args.Length) {
				var arg = args [index++];
				if (arg [0] != '-')
					throw new ArgumentOutOfRangeException (nameof (CommentContext),
						$"Expected argument {index - 1} to start with a '-' but got {arg} (args: {commentText}");
				var key = arg.Substring (1);
				var val = "";
				if (index < args.Length && args [index] [0] != '-') {
					val = args [index++];
				}
				moduleFlags [key] = val;
			}
		}

		void SetLanguageVersion (XElement module)
		{
			if (compilerVersion != null) {
				module.Add (new XAttribute ("swiftVersion", compilerVersion.ToString ()));
			}
		}

		static string XmlBool (bool b)
		{
			return b ? "true" : "false";
		}

		static string ToAccess (Access_level_modifierContext access)
		{
			var accessstr = access != null ? access.GetText () : "internal";
			switch (accessstr) {
			case "public":
				return "Public";
			case "private":
				return "Private";
			case "open":
				return "Open";
			case "internal":
				return "Internal";
			default:
				return "Unknown";
			}
		}


		static XElement GetOrCreate (XElement elem, string key)
		{
			var members = elem.Element (key);
			if (members == null) {
				members = new XElement (key);
				elem.Add (members);
			}
			return members;
		}

		static string [] ctorDtorNames = new string [] {
			"init", "init?", "init!", "deinit"
		};

		static bool IsCtorDtor (string name)
		{
			return ctorDtorNames.Contains (name);
		}

		static string TrimColon (string input)
		{
			if (!input.Contains (":"))
				return input;
			input = input.Trim ();
			return input.StartsWith (":", StringComparison.Ordinal) ?
				input.Substring (1) : input;
		}


		public List<String> ImportModules { get { return importModules; } }
	}
}
