// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo;
using System.IO;
using Dynamo.SwiftLang;
using SwiftReflector.ExceptionTools;
using SwiftReflector.IOUtils;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector {

	public class MethodWrapping {
		static string kXamPrefix = "xamarin_";
		IStreamProvider<SwiftClassName> provider;
		Dictionary<string, Dictionary<string, List<string>>> modulesOntoClasses = new Dictionary<string, Dictionary<string, List<string>>> ();
		TypeMapper typeMapper;
		ModuleDeclaration wrappingModule;
		HashSet<string> uniqueModuleReferences = new HashSet<string> ();
		ErrorHandling errors;
		FunctionReferenceCodeMap referenceCodeMap = new FunctionReferenceCodeMap ();
		string substituteForSelf = "";

		public MethodWrapping (IStreamProvider<SwiftClassName> provider, TypeMapper typeMapper, string wrappingModuleName, ErrorHandling errors)
		{
			wrappingModule = new ModuleDeclaration (Exceptions.ThrowOnNull (wrappingModuleName, "wrappingModuleName"));
			this.provider = Exceptions.ThrowOnNull (provider, "provider");
			this.typeMapper = typeMapper;
			this.errors = errors;
		}

		public FunctionReferenceCodeMap FunctionReferenceCodeMap { get { return referenceCodeMap; } }

		void AddImportIfNotPresent (SLImportModules modules, string modname)
		{
			Exceptions.ThrowOnNull (modules, nameof (modules));
			Exceptions.ThrowOnNull (modname, nameof (modname));
			if (modname == wrappingModule.Name)
				return;
			modules.AddIfNotPresent (modname);
			uniqueModuleReferences.Add (modname);
		}

		void AddXamGlueImport (SLImportModules modules)
		{
			AddImportIfNotPresent (modules, "XamGlue");
		}

		public static string WrapperFuncName (string moduleName, string funcName)
		{
			return string.Format ("{0}{1}F{2}", kXamPrefix, moduleName, funcName);
		}

		public static string WrapperOperatorName (TypeMapper typeMapper, string moduleNameOrFullClassName, string operatorName, OperatorType type)
		{
			var operatorType = "";
			switch (type) {
			case OperatorType.Infix:
				operatorType = "OIn";
				break;
			case OperatorType.Postfix:
				operatorType = "OPo";
				break;
			case OperatorType.Prefix:
				operatorType = "OPr";
				break;
			case OperatorType.Unknown:
				operatorType = "OUk";
				break;
			default:
				// should never happen.
				throw new ArgumentOutOfRangeException (nameof (type));
			}
				
			operatorName = CleanseOperatorName (typeMapper, operatorName);
			var classPrefix = moduleNameOrFullClassName.Replace ('.', 'D');
			return $"{kXamPrefix}{classPrefix}{operatorType}{operatorName}";
		}

		public static string WrapperName (SwiftClassName name, string methodName, bool isExtension)
		{
			return WrapperName (name.ToFullyQualifiedName (false), methodName, isExtension);
		}

		public static string WrapperName (string fullyQualifiedClassName, string methodName, bool isExtension)
		{
			string classPrefix = fullyQualifiedClassName.Replace ('.', 'D');
			return $"{kXamPrefix}{classPrefix}{(isExtension ? 'E' : 'D')}{methodName}";
		}

		public static string WrapperCtorName (SwiftClassName name, bool isExtension)
		{
			string classPrefix = name.ToFullyQualifiedName (false).Replace ('.', 'D');
			return $"{kXamPrefix}{classPrefix}{(isExtension ? 'E' : 'D')}{name.Terminus}";
		}

		public static string WrapperName (SwiftClassName name, string methodName, PropertyType propType, bool isSubScript, bool isExtension)
		{
			return WrapperName (name.ToFullyQualifiedName (), methodName, propType, isSubScript, isExtension);
		}

		public static string EnumFactoryCaseWrapperName (SwiftClassName name, string caseName)
		{
			string classPrefix = name.ToFullyQualifiedName (false).Replace ('.', 'D');
			return string.Format ("{0}{1}f{2}", kXamPrefix, classPrefix, caseName);
		}

		public static string EnumFactoryCaseWrapperName (EnumDeclaration en, string caseName)
		{
			string classPrefix = en.ToFullyQualifiedName (false).Replace ('.', 'D');
			return string.Format ("{0}{1}f{2}", kXamPrefix, classPrefix, caseName);
		}

		public static string EnumPayloadWrapperName (EnumDeclaration en, string caseName)
		{
			string classPrefix = en.ToFullyQualifiedName (false).Replace ('.', 'D');
			return string.Format ("{0}{1}P{2}", kXamPrefix, classPrefix, caseName);
		}

		public static string EnumCaseFinderWrapperName (SwiftClassName name)
		{
			string classPrefix = name.ToFullyQualifiedName (false).Replace ('.', 'D');
			return string.Format ("{0}{1}ec", kXamPrefix, classPrefix);
		}

		public static string EnumCaseFinderWrapperName (EnumDeclaration en)
		{
			string classPrefix = en.ToFullyQualifiedName (false).Replace ('.', 'D');
			return string.Format ("{0}{1}ec", kXamPrefix, classPrefix);
		}

		public static string WrapperName (string fullyQualifiedClassName, string methodName, PropertyType propType, bool isSubScript, bool isExtension)
		{
			var lastIndex = fullyQualifiedClassName.LastIndexOf ('.');
			if (lastIndex >= 0) {
				fullyQualifiedClassName = fullyQualifiedClassName.Substring (0, lastIndex - 1) + (isExtension ? "E" : "D") + fullyQualifiedClassName.Substring (lastIndex + 1);
			}
			string classPrefix = fullyQualifiedClassName.Replace ('.', 'D');
			char propMarker = isSubScript ? 's' : 'p';
			switch (propType) {
			case PropertyType.Getter:
				propMarker = 'G';
				break;
			case PropertyType.Setter:
				propMarker = 'S';
				break;
			case PropertyType.Materializer:
				propMarker = 'M';
				break;
			default:
				throw ErrorHelper.CreateError (ReflectorError.kWrappingBase + 0, $"unknown property type {propType.ToString ()} wrapping function {methodName} in {fullyQualifiedClassName}");
			}
			return string.Format ("{0}{1}{2}{3}", kXamPrefix, classPrefix, propMarker, methodName);
		}

		public HashSet<string> WrapModule (ModuleDeclaration mod, ModuleInventory inventory)
		{
			foreach (ClassDeclaration cl in mod.AllClasses) {
				if (ShouldSkipDeprecated(cl, "Class"))
					continue;
				if (cl.Access.IsPrivateOrInternal ())
					continue;
				if (cl.IsFinal || cl.Access == Accessibility.Public) {
					WrapClass (cl, inventory);
				} else {
					WrapSubclassable (cl, inventory);
				}
			}

			foreach (StructDeclaration st in mod.AllStructs) {
				if (ShouldSkipDeprecated(st, "Struct")) {
					continue;
				}
				if (st.Access.IsPrivateOrInternal ())
					continue;
				WrapStruct (st, inventory);
			}

			foreach (EnumDeclaration en in mod.AllEnums) {
				if (ShouldSkipDeprecated(en, "Enum"))
					continue;
				if (en.Access.IsPrivateOrInternal ())
					continue;
				WrapEnum (en, inventory);
			}

			foreach (ProtocolDeclaration pr in mod.AllProtocols) {
				if (ShouldSkipDeprecated (pr, "Protocol") || pr.IsObjC)
					continue;
				if (pr.Access.IsPrivateOrInternal ())
					continue;
				WrapProtocol (pr, inventory);
			}

			for (int i = 0; i < mod.Extensions.Count; i++) {
				if (mod.Extensions [i].Members.All (x => x.Access.IsPrivateOrInternal ()))
					continue;
				WrapExtension (mod.Extensions [i], inventory, i);
			}

			WrapFunctions (mod);

			WrapProperties (mod, inventory);

			var retval = uniqueModuleReferences;
			uniqueModuleReferences = new HashSet<string> ();
			return retval;
		}


		bool ShouldSkipDeprecated(TypeDeclaration decl, string typeName)
		{
			if (decl.IsDeprecated || decl.IsUnavailable) {
				errors.SkippedTypes.Add (decl.ToFullyQualifiedName (true));
				var reason = decl.IsDeprecated ? "deprecated" : "obsolete or unavailable";
				errors.Add (ErrorHelper.CreateWarning (ReflectorError.kWrappingBase + 4, $"{typeName} {decl.ToFullyQualifiedName ()} is {reason}, skipping"));
			}
			return false;
		}

		void WrapProperties (ModuleDeclaration mod, ModuleInventory inventory)
		{
			SLFile slfile = null;
			try {
				foreach (PropertyDeclaration prop in mod.TopLevelProperties) {
					if (prop.IsDeprecated)
						continue;
					if (!(prop.Access == Accessibility.Public || prop.Access == Accessibility.Open))
						continue;
					if (slfile == null) {
						slfile = new SLFile (null);
					}
					try {
						WrapProperty (prop, inventory, slfile);
					}
					catch (Exception err) {
						errors.SkippedFunctions.Add (prop.ToFullyQualifiedName (true));
						errors.Add (err);
					}
				}
			} finally {
				WriteSLFile (slfile, mod, "Props");
			}
		}

		void WrapFunctions (ModuleDeclaration mod)
		{
			SLFile slfile = null;
			try {
				WrapFunctions (ref slfile, mod.TopLevelFunctions);
			} finally {
				WriteSLFile (slfile, mod, "Funcs");
			}
		}

		void WrapFunctions (ref SLFile slfile, IEnumerable<FunctionDeclaration> funcs)
		{
			foreach (var fn in funcs) {
				if (fn.IsProperty)
					continue;
				if (fn.IsDeprecated || fn.IsUnavailable) {
					errors.SkippedFunctions.Add (fn.ToFullyQualifiedName (true));
					var reason = fn.IsDeprecated ? "deprecated" : "obsolete or unavailable";
					errors.Add (ErrorHelper.CreateWarning (ReflectorError.kWrappingBase + 5, $"Top level function {fn.ToFullyQualifiedName ()} is {reason}, skipping"));
					continue;
				}
				if (BoundClosureError (fn, null, "wrapping top level functions")) {
					continue;
				}
				if (FuncNeedsWrapping (fn, typeMapper)) {
					// make the file in a lazy way
					if (slfile == null) {
						slfile = new SLFile (null);
					}
					try {
						WrapFunction (fn, slfile);
					} catch (Exception e) {
						errors.SkippedFunctions.Add (fn.ToFullyQualifiedName (true));
						errors.Add (e);
					}
				}
			}
		}


		void WriteSLFile (SLFile slfile, ModuleDeclaration mod, string optionalSuffix)
		{
			if (slfile != null && slfile.Functions.Count > 0) {
				var sn = new SwiftClassName (new SwiftName (mod.Name + (optionalSuffix ?? ""), false),
							     new List<MemberNesting> (), new List<SwiftName> ());
				var stm = provider.ProvideStreamFor (sn);
				var writer = new CodeWriter (stm);
				try {
					uniqueModuleReferences.Merge (slfile.Imports.Select (im => im.Module));

					slfile.WriteAll (writer);
					writer.TextWriter.Flush ();
				} finally {
					provider.NotifyStreamDone (sn, stm);
				}
			}
		}

		public bool TryGetClassesForModule (string module, out Dictionary<string, List<string>> classes)
		{
			return modulesOntoClasses.TryGetValue (module, out classes);
		}

		public void WrapExtension (ExtensionDeclaration ext, ModuleInventory moduleInventory, int index)
		{
			var extensionTypeName = ext.ExtensionOnType as NamedTypeSpec;
			if (extensionTypeName == null)
				throw ErrorHelper.CreateError (ReflectorError.kWrappingBase + 6, $"In wrapping extension, expected a nominal type but got {ext.ExtensionOnType.GetType ().Name}");
			var nameParts = extensionTypeName.NameWithoutModule.Split ('.').Select (str => new SwiftName (str, false)).ToList ();
			// normally we get a wrapping class named after the source class, but extensions are on a type not in the module
			// and there could be multiples of the extended type.
			// To avoid name clashes, we're naming this "OurModule.extended.nominal.name.parts.Extension{index}".
			// we lie and say that the member nesting is all classes. This is fine because this is not a formal
			// class name but is just for naming the file.
			nameParts.Add (new SwiftName ($"Extension{index}", false));
			var nesting = nameParts.Select (namepart => MemberNesting.Class).ToList ();
			var sn = new SwiftClassName (new SwiftName (ext.Module.Name, false), nesting, nameParts);

			var stm = provider.ProvideStreamFor (sn);
			CodeWriter writer = null;
			try {
				writer = new CodeWriter (stm);
				WrapExtension (ext, writer, moduleInventory);
				writer.TextWriter.Flush ();
				provider.NotifyStreamDone (sn, stm);
			} catch (Exception err) {
				HandleWrappingException (err, null, sn, stm, $"wrapping an extension on {ext.ExtensionOnTypeName}");
			}
		}


		public void WrapClass (ClassDeclaration cl, ModuleInventory modInventory)
		{
			var sn = XmlToTLFunctionMapper.ToSwiftClassName (cl);
			var stm = provider.ProvideStreamFor (sn);
			CodeWriter writer = null;
			try {
				writer = new CodeWriter (stm);
				WrapClass (cl, writer, modInventory, null, null);
				writer.TextWriter.Flush ();
				provider.NotifyStreamDone (sn, stm);
			} catch (Exception err) {
				HandleWrappingException (err, cl, sn, stm, $"wrapping a class {cl.ToFullyQualifiedName ()}");
			}
		}

		public void WrapProtocol (ProtocolDeclaration pr, ModuleInventory modInventory)
		{
			var sn = XmlToTLFunctionMapper.ToSwiftClassName (pr);
			var stm = provider.ProvideStreamFor (sn);
			CodeWriter writer = null;
			try {
				writer = new CodeWriter (stm);
				WrapProtocol (pr, writer, modInventory);
				writer.TextWriter.Flush ();
				provider.NotifyStreamDone (sn, stm);
			} catch (Exception err) {
				HandleWrappingException (err, pr, sn, stm, $"wrapping a protocol {pr.ToFullyQualifiedName ()}");
			}
		}

		public void WrapSubclassable (ClassDeclaration cl, ModuleInventory modInventory)
		{
			SwiftClassName sn = XmlToTLFunctionMapper.ToSwiftClassName (cl);
			Stream stm = provider.ProvideStreamFor (sn);
			CodeWriter writer = null;
			try {
				writer = new CodeWriter (stm);
				WrapSubclassable (cl, writer, modInventory);
				writer.TextWriter.Flush ();
				provider.NotifyStreamDone (sn, stm);
			} catch (Exception err) {
				HandleWrappingException (err, cl, sn, stm, $"wrapping an open class {cl.ToFullyQualifiedName ()}");
			}
		}

		public void WrapStruct (StructDeclaration st, ModuleInventory modInventory)
		{
			var sn = XmlToTLFunctionMapper.ToSwiftClassName (st);
			var stm = provider.ProvideStreamFor (sn);
			CodeWriter writer = null;
			try {
				writer = new CodeWriter (stm);
				WrapStruct (st, writer, modInventory, errors);
				writer.TextWriter.Flush ();
				provider.NotifyStreamDone (sn, stm);
			} catch (Exception err) {
				HandleWrappingException (err, st, sn, stm, $"wrapping a struct {st.ToFullyQualifiedName ()}");
			}
		}

		public void WrapEnum (EnumDeclaration en, ModuleInventory modInventory)
		{
			var sn = XmlToTLFunctionMapper.ToSwiftClassName (en);
			var stm = provider.ProvideStreamFor (sn);
			CodeWriter writer = null;
			try {
				writer = new CodeWriter (stm);
				WrapEnum (en, writer, modInventory);
				writer.TextWriter.Flush ();
				provider.NotifyStreamDone (sn, stm);
			} catch (Exception err) {
				HandleWrappingException (err, en, sn, stm, $"wrapping an enum {en.ToFullyQualifiedName ()}");
			}
		}

		void HandleWrappingException (Exception err, BaseDeclaration toSkip, SwiftClassName sn, Stream stm, string doing)
		{
			provider.RemoveStream (sn, stm);
			if (!(err is RuntimeException)) {
				err = ErrorHelper.CreateWarning (ReflectorError.kWrappingBase + 15, err, $"Error while {doing}, skipping.");
			}
			errors.Add (err);
			if (toSkip != null)
				errors.SkippedTypes.Add (toSkip.ToFullyQualifiedName ());
		}


		void WrapProperty (PropertyDeclaration decl, ModuleInventory modInventory, SLFile slfile)
		{
			var getter = decl.GetGetter ();
			if (getter == null) {
				getter = new FunctionDeclaration {
					Module = decl.Module,
					Name = "get_" + decl.Name,
					ReturnTypeName = decl.TypeName,
					IsProperty = true,
				};
				getter.ParameterLists.Add (new List<ParameterItem> ());
				getter.Generics.AddRange (decl.Generics);
			}


			var setter = decl.GetSetter ();
			// if it's not a let binding, then it's a setter and it's likely that the setter might mutate the struct
			// so we need to pass it by reference


			// conditions:
			// It's not a let and is not computed

			if (!decl.IsLet && (decl.Storage != StorageKind.Computed)) {
				setter = new FunctionDeclaration {
					Module = decl.Module,
					Name = "set_" + decl.Name,
					ReturnTypeName = "()",
					IsProperty = true,
				};
				setter.Generics.AddRange (decl.Generics);
				setter.ParameterLists.Add (new List<ParameterItem> ());
				var parameter = new ParameterItem {
					PrivateName = "newValue",
					PublicName = "newValue",
					TypeName = decl.TypeName
				};
				setter.ParameterLists [0].Add (parameter);
			}

			var getWrapperName = WrapperName (decl.Module.Name, decl.Name, PropertyType.Getter, false, decl.IsExtension);
			var getWrapper = MapTopLevelFuncToWrapperFunc (slfile.Imports, getter, getWrapperName);
			slfile.Functions.Add (getWrapper);
			if (setter != null) {
				var setWrapperName = WrapperName (decl.Module.Name, decl.Name, PropertyType.Setter, false, decl.IsExtension);
				var setWrapper = MapTopLevelFuncToWrapperFunc (slfile.Imports, setter, setWrapperName);
				slfile.Functions.Add (setWrapper);
			}
		}


		public static SwiftType GetPropertyType (PropertyDeclaration decl, ModuleInventory modInventory)
		{
			if (decl.Storage == StorageKind.Computed) {
				if (modInventory.TryGetValue (decl.Module.Name, out ModuleContents contents)) {
					if (contents.Functions.TryGetValue (decl.Name, out OverloadInventory getterOverload)) {
						foreach (var func in getterOverload.Functions) {
							if (func.Signature.ParameterCount == 0 && func.Signature.ReturnType != null && !func.Signature.ReturnType.IsEmptyTuple)
								return func.Signature.ReturnType;
						}
					}
				}
			} else {
				if (modInventory.TryGetValue (decl.Module.Name, out ModuleContents contents)) {
					if (contents.Variables.TryGetValue (decl.Name, out VariableContents variable)) {
						if (variable.Addressors.Count > 0) {
							SwiftAddressorType addressor = variable.Addressors [0].Signature as SwiftAddressorType;
							if (addressor != null) {
								return addressor.ReturnType;
							}
						} else {
							return variable.Variable.OfType;
						}
					}
				}
			}
			throw ErrorHelper.CreateError (ReflectorError.kWrappingBase + 2, $"unable to find the type ({decl.TypeName}) of property {decl.ToFullyQualifiedName ()}");
		}


		void WrapFunction (FunctionDeclaration fn, SLFile slfile)
		{
			AddImportIfNotPresent (slfile.Imports, fn.Module.Name);
			var func = MapTopLevelFuncToWrapperFunc (slfile.Imports, fn);
			if (func != null)
				slfile.Functions.Add (func);
		}

		public static bool FuncNeedsWrapping (FunctionDeclaration fn, ModuleInventory modInventory, TypeMapper typeMapper)
		{
			if (fn.Access == Accessibility.Open)
				return true;
			if (fn.HasThrows)
				return true;
			if (fn.IsOperator)
				return true;
			if (fn.IsExtension)
				return true;
			var tlf = XmlToTLFunctionMapper.ToTLFunction (fn, modInventory, typeMapper);
			if (tlf == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 1, $"Unable to find function for declaration of {fn.ToFullyQualifiedName ()}.");
			}
			if (tlf.Signature.CanThrow)
				return true;
			return (tlf.Signature.ReturnType != null && typeMapper.MustForcePassByReference (tlf.Signature.ReturnType)) ||
				tlf.Signature.EachParameter.Any (st => typeMapper.MustForcePassByReference (st));
		}

		public static bool FuncNeedsWrapping (FunctionDeclaration fn, TypeMapper typeMapper)
		{
			if (fn.Access == Accessibility.Open)
				return true;
			if (fn.HasThrows)
				return true;
			if (fn.IsOperator)
				return true;
			if (fn.IsExtension)
				return true;
			bool hasReturn = fn.ReturnTypeSpec != null && !fn.ReturnTypeSpec.IsEmptyTuple;
			bool returnNeedsWrapping = false;
			if (hasReturn) {
				returnNeedsWrapping = fn.IsTypeSpecGeneric (fn.ReturnTypeSpec) ||
							fn.ReturnTypeSpec is TupleTypeSpec ||
							fn.ReturnTypeSpec is ClosureTypeSpec ||
							fn.ReturnTypeSpec is ProtocolListTypeSpec ||
							typeMapper.MustForcePassByReference (fn, fn.ReturnTypeSpec);
			}
			return returnNeedsWrapping ||
				(!fn.IsConstructor && fn.ParameterLists.Count != 1) ||
				  fn.ParameterLists.Last ().Any (pi => fn.IsTypeSpecGeneric (pi) ||
								 pi.TypeSpec is TupleTypeSpec ||
				                                 pi.TypeSpec is ClosureTypeSpec ||
								 typeMapper.MustForcePassByReference (fn, pi.TypeSpec));
		}


		void AddFunctionToOverallList (BaseDeclaration decl, string functionName)
		{
			string moduleName = decl.Module.Name;
			string className = decl.ToFullyQualifiedName (false);
			Dictionary<string, List<string>> classes = null;
			if (!modulesOntoClasses.TryGetValue (moduleName, out classes)) {
				classes = new Dictionary<string, List<string>> ();
				modulesOntoClasses.Add (moduleName, classes);
			}

			List<string> functions = null;
			if (!classes.TryGetValue (className, out functions)) {
				functions = new List<string> ();
				classes.Add (className, functions);
			}
			if (!functions.Contains (functionName))
				functions.Add (functionName);
		}

		void WrapEnum (EnumDeclaration en, CodeWriter cw, ModuleInventory modInventory)
		{
			SLFile file = new SLFile (null);
			var modules = file.Imports;
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (en);

			var enumContents = modInventory.FindClass (en.ToFullyQualifiedName (true));
			if (enumContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 2, $"Unable to find class contents for enum {en.ToFullyQualifiedName (true)}.");

			var funcs = file.Functions;

			var publicConstructors = en.AllConstructors ().Where (fd => fd.Access == Accessibility.Public).ToList ();

			if (publicConstructors.Count > 0) {
				foreach (FunctionDeclaration funcDecl in publicConstructors) {
					if (ShouldSkipDeprecated (funcDecl, "Constructor"))
						continue;
					if (BoundClosureError (funcDecl, en, "wrapping a constructor in an enum"))
						continue;
					TLFunction ctorTlf = XmlToTLFunctionMapper.ToTLFunction (funcDecl, modInventory, typeMapper);
					if (ctorTlf == null)
						throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 3, $"Unable to find constructor for struct {funcDecl.ToFullyQualifiedName (true)}.");
					SLFunc func = null;
					try {
						func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
					} catch (Exception e) {
						SkipWithWarning (funcDecl, en, "wrapping a constructor in an enum", e);
						continue;
					}
					funcs.Add (func);
					AddFunctionToOverallList (en, func.Name.Name);
				}
			}

			if (!en.IsTrivial) {
				funcs.AddRange (en.Elements.Select (elem => MakeEnumFactory (enumContents, en, elem, modules)).Where (el => el != null));
				funcs.Add (MakeEnumCaseFinder (enumContents, en, modules));
				funcs.AddRange (en.Elements.Select (elem => MakeEnumPayload (enumContents, en, elem, modules)).Where (el => el != null));
			}

			foreach (FunctionDeclaration funcDecl in en.AllMethodsNoCDTor ().Where (fd => fd.Access == Accessibility.Public)) {
				if (funcDecl.IsProperty)
					continue;
				if (ShouldSkipDeprecated (funcDecl, "Method"))
					continue;
				if (BoundClosureError (funcDecl, en, "wrapping a method in an enum"))
					continue;
				SLFunc func = null;
				try {
					func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
				} catch (Exception e) {
					SkipWithWarning (funcDecl, en, "wrapping a method in an enum", e);
					continue;
				}
				funcs.Add (func);
				AddFunctionToOverallList (en, func.Name.Name);
			}


			foreach (PropertyDeclaration propDecl in en.Members.OfType<PropertyDeclaration> ()) {
				if (propDecl.IsDeprecated || propDecl.IsUnavailable || !propDecl.IsPublicOrOpen)
					continue;
				var getDecl = propDecl.GetGetter ();
				var setDecl = propDecl.GetSetter ();
				if (BoundClosureError (getDecl, propDecl, "wrapping a property getter in an enum"))
					continue;
				if (BoundClosureError (setDecl, propDecl, "wrapping a property setter in an enum"))
					continue;
				if (getDecl.Access != Accessibility.Public && (setDecl != null && setDecl.Access != Accessibility.Public))
					continue;

				if (getDecl != null) {
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, getDecl);
					} catch (Exception e) {
						SkipWithWarning (getDecl, en, "wrapping a property getter in an enum", e);
						continue;
					}

					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (propDecl, slfunc.Name.Name);
					}
				}

				if (setDecl != null) {
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, setDecl);
					} catch (Exception e) {
						SkipWithWarning (setDecl, en, "wrapping a property setter in an enum", e);
						continue;
					}

					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (propDecl, slfunc.Name.Name);
					}
				}
			}

			foreach (SubscriptDeclaration subDecl in en.AllSubscripts ()) {
				if (ShouldSkipDeprecated (subDecl.Getter, "Method"))
					continue;
				if (subDecl.Getter.Access == Accessibility.Public) {
					var func = XmlToTLFunctionMapper.ToTLFunction (subDecl.Getter, modInventory, typeMapper);
					if (func == null)
						throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 7, $"Unable to find function for struct subscript getter {subDecl.Getter.ToFullyQualifiedName ()}.");
				}

				if (subDecl.Setter != null && subDecl.Setter.Access == Accessibility.Public) {
					var func = XmlToTLFunctionMapper.ToTLFunction (subDecl.Setter, modInventory, typeMapper);
					if (func == null)
						throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 8, $"Unable to find function for struct subscript setter {subDecl.Setter.ToFullyQualifiedName ()}.");
				}
			}

			uniqueModuleReferences.Merge (file.Imports.Select (im => im.Module));


			file.WriteAll (cw);
		}

		SLFunc MakeEnumCaseFinder (ClassContents enumContents, EnumDeclaration en, SLImportModules modules)
		{
			// public func enumCaseFinder( val:UnsafePointer<Foo>) -> Int {
			// switch val.pointee {
			// case .a: return 0;
			// case .b: return 1;
			// ...
			// }
			// -- or --
			// public func enumCaseFinder(val:Foo) -> Int {
			// switch val {
			// case .a: return 0;
			// case .b: return 1;
			// ...
			// }
			var cases = new List<SLCase> ();
			for (int i = 0; i < en.Elements.Count; i++) {
				var elem = en.Elements [i];

				var theCase = new SLCase (new SLIdentifier ("." + elem.Name), new SLReturn (SLConstant.Val (i)));
				cases.Add (theCase);
			}


			var genericDeclaration = new List<SLGenericTypeDeclaration> ();
			var parentGenerics = en.Generics.Select (gd => {
				Tuple<int, int> depthIndex = en.GetGenericDepthAndIndex (gd.Name);
				SLGenericReferenceType grt = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);

				if (genericDeclaration.FirstOrDefault (ag => ag.Name.Name == grt.Name) == null) {
					SLGenericTypeDeclaration sldecl = new SLGenericTypeDeclaration (new SLIdentifier (grt.Name));
					sldecl.Constraints.AddRange (gd.Constraints.Select (bc => {
						InheritanceConstraint inh = bc as InheritanceConstraint;
						if (bc == null)
							throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 18, "Equality constraints not supported (yet)");
						return new SLGenericConstraint (true, grt, typeMapper.TypeSpecMapper.MapType (en, modules, inh.InheritsTypeSpec, false));
					}));
					genericDeclaration.Add (sldecl);
				}
				return grt;
			}).ToList ();


			var valIdent = new SLIdentifier ("val");
			var switchCase = new SLSwitch (valIdent.Dot (new SLIdentifier ("pointee")), cases);
			AddImportIfNotPresent (modules, en.Module.Name);
			SLType valType = new SLSimpleType (en.ToFullyQualifiedName (false));
			if (en.ContainsGenericParameters) {
				valType = new SLBoundGenericType (valType.ToString (), parentGenerics);
			}
			valType = new SLBoundGenericType ("UnsafePointer", valType);
			var body = new SLCodeBlock (null);
			body.Add (switchCase);

			var func = new SLFunc (Visibility.Public, SLSimpleType.Int, new SLIdentifier (EnumCaseFinderWrapperName (en)),
			                       new SLParameterList (new SLParameter (valIdent, valIdent, valType, SLParameterKind.None)),
					      body);
			func.GenericParams.AddRange (genericDeclaration);
			return func;
		}

		SLFunc MakeEnumPayload (ClassContents enumContents, EnumDeclaration en, EnumElement elem, SLImportModules modules)
		{
			// public func payloadCasea(f:UnsafeMutablePointer<Foo>) -> Bar {
			// return try! { (b:Foo) throws -> Bar in
			//     if case .a(let x) = b { return x }
			//     else { throw SwiftEnumError.undefined }
			//   } (f)
			// -- or -- 
			// public func payloadCasea(retval:UnsafeMutablePointer<Bar>, f:UnsafeMutablePointer<Foo>) {
			// retval = try! { (b:Foo) throws -> Bar in
			//     if case .a(let x) = b { return x }
			//     else { throw SwiftEnumError.undefined }
			//   } (f)
			AddXamGlueImport (modules);

			var genericDeclaration = new List<SLGenericTypeDeclaration> ();
			var parentGenerics = en.Generics.Select (gd => {
				var depthIndex = en.GetGenericDepthAndIndex (gd.Name);
				var grt = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);

				if (genericDeclaration.FirstOrDefault (ag => ag.Name.Name == grt.Name) == null) {
					var sldecl = new SLGenericTypeDeclaration (new SLIdentifier (grt.Name));
					sldecl.Constraints.AddRange (gd.Constraints.Select (bc => {
						var inh = bc as InheritanceConstraint;
						if (bc == null)
							throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 20, "Equality constraints not supported (yet)");
						return new SLGenericConstraint (true, grt, typeMapper.TypeSpecMapper.MapType (en, modules, inh.InheritsTypeSpec, false));
					}));
					genericDeclaration.Add (sldecl);
				}
				return grt;
			}).ToList ();

			var elemType = elem.TypeSpec;
			if (elemType == null)
				return null;
			var slElemType = typeMapper.TypeSpecMapper.MapType (en, modules, elemType, true);
			SLType slEnumType = new SLSimpleType (en.ToFullyQualifiedName (false));
			if (en.ContainsGenericParameters) {
				slEnumType = new SLBoundGenericType (
					slEnumType.ToString (), parentGenerics
					);
			}
			var paramType = new SLBoundGenericType ("UnsafePointer", slEnumType);


			var funcName = new SLIdentifier (EnumPayloadWrapperName (en, elem.Name));
			var retvalId = new SLIdentifier ("retval");
			var valId = new SLIdentifier ("val");
			var body = new SLCodeBlock (null);
			bool retvalPassByReference = en.IsTypeSpecGeneric (elemType) || typeMapper.MustForcePassByReference (en, elemType) || elemType is ProtocolListTypeSpec;




			SLFunc theFunc = null;
			if (retvalPassByReference) {
				var parms = new SLParameterList (
					new SLParameter (retvalId, new SLBoundGenericType ("UnsafeMutablePointer", slElemType)),
					new SLParameter (valId, paramType, SLParameterKind.None));
				theFunc = new SLFunc (Visibility.Public, null, funcName, parms, body);
			} else {
				var parms = new SLParameterList (new SLParameter (valId, paramType, SLParameterKind.None));
				theFunc = new SLFunc (Visibility.Public, slElemType, funcName, parms, body);
			}

			var closure = MakePayloadClosure (elem.Name, slEnumType, slElemType);
			var appl = new SLClosureCall (closure, null);
			appl.Parameters.Add (new SLArgument (valId, valId.Dot (new SLIdentifier ("pointee"))));
			var tryBang = new SLTry (appl, true);

			if (retvalPassByReference) {
				body.Add (SLFunctionCall.FunctionCallLine (new SLIdentifier ($"{retvalId.Name}.initialize"),
									new SLArgument (new SLIdentifier ("to"), tryBang, true)));
			} else {
				body.Add (SLReturn.ReturnLine (tryBang));
			}
			theFunc.GenericParams.AddRange (genericDeclaration);

			return theFunc;
		}

		SLClosure MakePayloadClosure (string elemName, SLType enumType, SLType elemType)
		{
			// { (val:enumType) throws -> elemType in
			//   if case .a(let x) = val { return x }
			//   else { throw SwiftEnumError.undefined }
			// }
			var body = new CodeElementCollection<ICodeElement> ();
			var valId = new SLIdentifier ("val");
			var closure = new SLClosure (elemType, new SLTupleType (new SLNameTypePair (valId, enumType)), body, true);

			var patXId = new SLIdentifier ("x");

			var ifclause = new SLCodeBlock (null).And (SLReturn.ReturnLine (patXId));
			var elseclause = new SLCodeBlock (null).And (new SLThrow (new SLIdentifier ("SwiftEnumError.undefined")));


			var ifcase = new SLIfElse (new SLIdentifier (String.Format (".{0}(let {1}) = {2}", elemName, patXId.Name, valId.Name)),
						  ifclause, elseclause, true);
			body.Add (ifcase);

			return closure;
		}

		SLFunc MakeEnumFactory (ClassContents enumContents, EnumDeclaration en, EnumElement elem, SLImportModules modules)
		{
			// If the type is too big to return:
			// public func factoryCasea(inout retval:Foo, val:Bar) {
			// 		retval = Foo.a(val)
			// }
			// -- or --
			// public func factoryCasea(inout retval:Foo) {
			//		retval = Foo.a
			// }
			// otherwise
			// public func factoryCasea(val:Bar) -> Foo {
			//    return Foo.a(b);
			// }
			// -- or --
			// public func factoryCasea() -> Foo {
			//   return Foo.a;
			//
			var elemType = elem.TypeSpec;
			SLType slElemType = elemType != null ? typeMapper.TypeSpecMapper.MapType (en, modules, elemType, true) : null;
			var genericDeclaration = new List<SLGenericTypeDeclaration> ();
			var parentGenerics = en.Generics.Select (gd => {
				var depthIndex = en.GetGenericDepthAndIndex (gd.Name);
				var grt = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);

				if (genericDeclaration.FirstOrDefault (ag => ag.Name.Name == grt.Name) == null) {
					var sldecl = new SLGenericTypeDeclaration (new SLIdentifier (grt.Name));
					sldecl.Constraints.AddRange (gd.Constraints.Select (bc => {
						var inh = bc as InheritanceConstraint;
						if (bc == null)
							throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 21, "Equality constraints not supported (yet)");
						return new SLGenericConstraint (true, grt, typeMapper.TypeSpecMapper.MapType (en, modules, inh.InheritsTypeSpec, false));
					}));
					genericDeclaration.Add (sldecl);
				}
				return grt;
			}).ToList ();



			SLType retValType = new SLSimpleType (en.ToFullyQualifiedName (false));
			var funcName = new SLIdentifier (EnumFactoryCaseWrapperName (en, elem.Name));
			var retvalId = new SLIdentifier ("retval");
			if (en.ContainsGenericParameters) {
				retValType = new SLBoundGenericType (retValType.ToString (), parentGenerics);
			}
			var valId = new SLIdentifier ("val");
			var body = new SLCodeBlock (null);
			SLParameter valuePart = null;
			// the default initial value is:
			// EnumType.elemName
			// If there is an initial value, it needs to be
			// EnumType.elemName(value)
			// If the value is passed by reference, it needs to be
			// EnumType.elemName(value.pointee)
			var initialValue = $"{en.ToFullyQualifiedName (false)}.{elem.Name}";
			if (elemType != null) {
				var valLabel = elemType.TypeLabel != null ? $"{elemType.TypeLabel}: " : "";
				if (en.IsTypeSpecGeneric (elemType) || typeMapper.MustForcePassByReference (en, elemType)) {
					valuePart = new SLParameter (valId, new SLBoundGenericType ("UnsafeMutablePointer", slElemType));
					initialValue = $"{en.ToFullyQualifiedName (false)}.{elem.Name}({valLabel}{valId.Name}.pointee)";
				} else {
					valuePart = new SLParameter (valId, slElemType);
					initialValue = $"{en.ToFullyQualifiedName (false)}.{elem.Name}({valLabel}{valId.Name})";
				}
			}

			var parms = new SLParameterList (new SLParameter (retvalId, new SLBoundGenericType ("UnsafeMutablePointer", retValType)));

			if (elemType != null)
				parms.Parameters.Add (valuePart);

			SLLine initcall = SLFunctionCall.FunctionCallLine (new SLIdentifier ($"{retvalId.Name}.initialize"),
			                                                   new SLArgument (new SLIdentifier ("to"), new SLIdentifier (initialValue), true));

			body.Add (initcall);
			var func = new SLFunc (Visibility.Public, null, funcName, parms, body);
			func.GenericParams.AddRange (genericDeclaration);
			return func;
		}

		void WrapStruct (StructDeclaration st, CodeWriter cw, ModuleInventory modInventory, ErrorHandling errors)
		{
			SLFile file = new SLFile (null);
			var modules = file.Imports;
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (st);

			var funcs = file.Functions;

			var publicConstructors = st.AllConstructors ().Where (fd => fd.Access == Accessibility.Public).ToList ();

			if (publicConstructors.Count > 0) {
				foreach (FunctionDeclaration funcDecl in publicConstructors) {
					if (ShouldSkipDeprecated (funcDecl, "Constructor"))
						continue;
					if (BoundClosureError (funcDecl, st, "wrapping a constructor in a struct"))
						continue;
					var ctorTlf = XmlToTLFunctionMapper.ToTLFunction (funcDecl, modInventory, typeMapper);
					if (ctorTlf == null) {
						var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerReferenceBase + 24, $"Unable to find constructor for struct {funcDecl.ToFullyQualifiedName ()}, skipping.");
						errors.Add (ex);
						continue;
					}
					SLFunc func = null;
					try {
						func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
					} catch (Exception e) {
						SkipWithWarning (funcDecl, st, "wrapping a constructor in a struct", e);
						continue;
					}
					funcs.Add (func);
					AddFunctionToOverallList (st, func.Name.Name);
				}
			}

			foreach (FunctionDeclaration funcDecl in st.AllMethodsNoCDTor ().Where (fd => fd.Access == Accessibility.Public)) {
				if (funcDecl.IsProperty)
					continue;
				if (ShouldSkipDeprecated (funcDecl, "Method"))
					continue;
				if (BoundClosureError (funcDecl, st, "wrapping a method in a struct"))
					continue;
				SLFunc func = null;
				try {
					func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
				} catch (Exception e) {
					SkipWithWarning (funcDecl, st, "wrapping a method in a struct", e);
					continue;
				}
				funcs.Add (func);
				AddFunctionToOverallList (st, func.Name.Name);
			}

			foreach (PropertyDeclaration propDecl in st.Members.OfType<PropertyDeclaration> ()) {
				if (propDecl.IsDeprecated || propDecl.IsUnavailable)
					continue;
				var getDecl = propDecl.GetGetter ();
				var setDecl = propDecl.GetSetter ();

				if (ShouldSkipDeprecated (getDecl, "Property"))
					continue;
				if (BoundClosureError (getDecl, st, "wrapping a getter in a struct"))
					continue;
				if (BoundClosureError (setDecl, st, "wrapping a setter in a struct"))
					continue;

				if (getDecl.Access != Accessibility.Public && (setDecl != null && setDecl.Access != Accessibility.Public))
					continue;

				if (getDecl.Access == Accessibility.Public) {
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, getDecl);
					} catch (Exception e) {
						SkipWithWarning (getDecl, st, "wrapping a getter in a struct", e);
					}
					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (propDecl, slfunc.Name.Name);
					}
				}

				if (setDecl != null && setDecl.Access == Accessibility.Public) {
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, setDecl);
					} catch (Exception e) {
						SkipWithWarning (setDecl, st, "wrapping a setter in a struct", e);
						continue;
					}
					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (propDecl, slfunc.Name.Name);
					}
				}
			}


			foreach (SubscriptDeclaration subDecl in st.AllSubscripts ()) {
				if (ShouldSkipDeprecated (subDecl.Getter, "subscript"))
					continue;
				if (subDecl.Getter.Access == Accessibility.Public) {
					if (!BoundClosureError (subDecl.Getter, st, "wrapping a struct subscript getter")) {
						SLFunc slfunc = null;
						try {
							slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, subDecl.Getter);
						} catch (Exception e) {
							SkipWithWarning (subDecl.Getter, st, "wrapping a struct subscript getter", e);
							continue;
						}
						if (slfunc != null) {
							funcs.Add (slfunc);
							AddFunctionToOverallList (subDecl.Getter, slfunc.Name.Name);
						}
					}
				}

				if (subDecl.Setter != null && subDecl.Setter.Access == Accessibility.Public) {
					if (!BoundClosureError (subDecl.Setter, st, "wrapping a struct subscript setter")) {
						SLFunc slfunc = null;
						try {
							slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, subDecl.Setter);
						} catch (Exception e) {
							SkipWithWarning (subDecl.Setter, st, "wrapping a struct subscript setter", e);
							continue;
						}
						if (slfunc != null) {
							funcs.Add (slfunc);
							AddFunctionToOverallList (subDecl.Setter, slfunc.Name.Name);
						}
					}
				}
			}

			uniqueModuleReferences.Merge (file.Imports.Select (im => im.Module));

			file.WriteAll (cw);
		}

		void WrapProtocol (ClassDeclaration cl, CodeWriter cw, ModuleInventory modInventory)
		{
			var protocol = cl as ProtocolDeclaration;
			var overrider = new OverrideBuilder (typeMapper, cl, null, wrappingModule);
			uniqueModuleReferences.Merge (overrider.ModuleReferences);
			var file = new SLFile (overrider.Imports);
			file.Classes.AddRange (overrider.ClassImplementations);
			file.Functions.AddRange (overrider.Functions);
			file.Declarations.AddRange (overrider.Declarations);
			try {
				if (protocol.HasAssociatedTypes || (protocol.HasDynamicSelf && !protocol.HasDynamicSelfInReturnOnly))
					EstablishSubtituteForSelf (OverrideBuilder.kAssocTypeGeneric);
				else
					EstablishSubtituteForSelf (overrider.SubstituteForSelf);
				WrapFunctions (ref file, overrider.FunctionsToWrap);

				EstablishSubtituteForSelf (overrider.SubstituteForSelf);

				WrapClass (protocol.HasAssociatedTypes || (protocol.HasDynamicSelf && !protocol.HasDynamicSelfInReturnOnly) ? overrider.OverriddenClass : overrider.OriginalClass,
					cw, modInventory, file, null, protocol);
			} finally {
				RelinquishSubstituteForSelf ();
			}
			var entity = typeMapper.GetEntityForSwiftClassName (cl.ToFullyQualifiedName (true));
			if (entity == null) {
				throw ErrorHelper.CreateError (ReflectorError.kWrappingBase + 3, $"Unable to locate entity for {cl.ToFullyQualifiedName ()} in type database.");
			}
			entity.ProtocolProxyModule = wrappingModule.Name;
		}

		void WrapSubclassable (ClassDeclaration cl, CodeWriter cw, ModuleInventory modInventory)
		{
			var overrider = new OverrideBuilder (typeMapper, cl, null, wrappingModule);
			typeMapper.RegisterClass (overrider.OverriddenClass);
			uniqueModuleReferences.Merge (overrider.ModuleReferences);
			var file = new SLFile (overrider.Imports);
			file.Classes.AddRange (overrider.ClassImplementations);
			file.Functions.AddRange (overrider.Functions);
			file.Declarations.AddRange (overrider.Declarations);
			WrapClass (overrider.OriginalClass, cw, modInventory, file, overrider.OverriddenClass);
		}


		void WrapExtension (ExtensionDeclaration extension, CodeWriter cw, ModuleInventory moduleInventory)
		{
			var entity = this.typeMapper.GetEntityForTypeSpec (extension.ExtensionOnType);
			if (entity == null)
				throw ErrorHelper.CreateError (ReflectorError.kWrappingBase + 9, $"Unable to find type declaration for type extension on {extension.ExtensionOnTypeName}");

			var extensionOnDecl = entity.Type as BaseDeclaration;
			if (extensionOnDecl == null)
				throw ErrorHelper.CreateError (ReflectorError.kWrappingBase + 10, $"Unable to build an extension on type {extension.ExtensionOnTypeName} - expected a BaseDeclaration but got a {entity.Type.GetType ().Name}");

			// Here's our neat trick -
			// We make a phony class based one the name, module, parentage, and signature of the extensionOn type.
			// Then we add all the members from the extension itself
			// We send that to be wrapped as if it were a class and off we go...
			var shamDeclaration = new ClassDeclaration ();
			shamDeclaration.Module = extensionOnDecl.Module;
			shamDeclaration.Name = extensionOnDecl.Name;
			shamDeclaration.Parent = extensionOnDecl.Parent;
			shamDeclaration.Generics.AddRange (extensionOnDecl.Generics);

			shamDeclaration.Members.AddRange (extension.Members);
			foreach (var member in shamDeclaration.Members) {
				member.Parent = shamDeclaration;
			}
			var file = new SLFile (null);
			file.Imports.AddIfNotPresent (extension.Module.Name);
			WrapClass (shamDeclaration, cw, moduleInventory, file, null);
		}

		void WrapClass (ClassDeclaration cl, CodeWriter cw, ModuleInventory modInventory,
			SLFile file, ClassDeclaration externalCl = null, ProtocolDeclaration originalProtocol = null)
		{
			file = file ?? new SLFile (null);

			var modules = file.Imports;
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (cl);
			var externalClassName = externalCl != null ? XmlToTLFunctionMapper.ToSwiftClassName (externalCl) : null;

			var funcs = file.Functions;

			foreach (FunctionDeclaration funcDecl in cl.AllMethodsNoCDTor ().Where (fd => fd.Access == Accessibility.Public || fd.IsVirtualClassMethod)) {
				if (ShouldSkipDeprecated(funcDecl, "Method"))
					continue;
				if (funcDecl.IsProperty)
					continue;
				if (BoundClosureError (funcDecl, cl, "wrapping a method in a class"))
					continue;
				SLFunc func = null;
				try {
					func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
				} catch (Exception e) {
					SkipWithWarning (funcDecl, cl, "wrapping a method in a class", e);
					continue;
				}
				funcs.Add (func);
				AddFunctionToOverallList (cl, func.Name.Name);
			}

			if (externalCl != null) {
				foreach (FunctionDeclaration funcDecl in externalCl.AllConstructors ().Where (fd => fd.Access == Accessibility.Public)) {
					if (ShouldSkipDeprecated(funcDecl, "Constructor"))
						continue;
					if (BoundClosureError (funcDecl, cl, "wrapping an overrider constructor in a class"))
						continue;
					if (FuncNeedsWrapping (funcDecl, typeMapper)) {
						SLFunc func = null;
						try {
							func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
						} catch (Exception e) {
							SkipWithWarning (funcDecl, cl, "wrapping an overrider constructor in a class", e);
							continue;
						}
						funcs.Add (func);
						AddFunctionToOverallList (externalCl, func.Name.Name);
					}
				}

				foreach (FunctionDeclaration funcDecl in externalCl.AllMethodsNoCDTor ().Where (fd => fd.Access == Accessibility.Internal
					  && fd.Name.StartsWith (OverrideBuilder.SuperPrefix))) {
					if (ShouldSkipDeprecated(funcDecl, "Method"))
						continue;
					if (funcDecl.IsProperty && !funcDecl.IsSubscript)
						continue;
					if (BoundClosureError (funcDecl, cl, "wrapping an overriding method in a class"))
						continue;
					SLFunc func = null;
					try {
						func = MapFuncDeclToWrapperFunc (externalClassName, modules, funcDecl);
					} catch (Exception e) {
						SkipWithWarning (funcDecl, cl, "wrapping an overriding methods in a class", e);
						continue;
					}
					funcs.Add (func);
					AddFunctionToOverallList (externalCl, func.Name.Name);
				}

				foreach (PropertyDeclaration propDecl in externalCl.Members.OfType<PropertyDeclaration> ().Where (fd => fd.Access == Accessibility.Internal)) {
					if (propDecl.IsDeprecated || propDecl.IsUnavailable)
						continue;
					var getDecl = propDecl.GetGetter ();
					if (getDecl == null)
						continue;
					var setDecl = propDecl.GetSetter ();

					if (ShouldSkipDeprecated(getDecl, "Property"))
						continue;

					if (BoundClosureError (getDecl, cl, "wrapping an overriding property getter in a class"))
						continue;
					if (setDecl != null && BoundClosureError (setDecl, cl, "wrapping an overriding property setter in a class"))
						continue;
					SLFunc getFunc = null;
					try {
						getFunc = MapFuncDeclToWrapperFunc (externalClassName, modules, getDecl);
					
					} catch (Exception e) {
						SkipWithWarning (getDecl, cl,  "wrapping an overriding property getter in a class", e);
						continue; // yes, I think it's best to skip the getter too at this point.
					}

					if (getFunc != null) {
						funcs.Add (getFunc);
						AddFunctionToOverallList (propDecl, getFunc.Name.Name);
					}

					if (setDecl != null) {
						SLFunc setFunc = null;
						try {
							setFunc = MapFuncDeclToWrapperFunc (externalClassName, modules, setDecl);
						} catch (Exception e) {
							SkipWithWarning (setDecl, cl, "wrapping an overriding property setter in a class", e);
							continue;
						}
						if (setFunc != null) {
							funcs.Add (setFunc);
							AddFunctionToOverallList (propDecl, setFunc.Name.Name);
						}
					}
				}
			} else {
				foreach (FunctionDeclaration funcDecl in cl.AllConstructors ().Where (fd => fd.Access == Accessibility.Public)) {
					if (ShouldSkipDeprecated (funcDecl, "Constructor"))
						continue;
					if (FuncNeedsWrapping (funcDecl, typeMapper)) {
						if (BoundClosureError (funcDecl, cl, "wrapping a constructor in a class"))
							continue;
						SLFunc func = null;
						try {
							func = MapFuncDeclToWrapperFunc (swiftClassName, modules, funcDecl);
						} catch (Exception e) {
							SkipWithWarning (funcDecl, cl, "wrapping a constructor in a class", e);
							continue;
						}
						funcs.Add (func);
						AddFunctionToOverallList (cl, func.Name.Name);
					}
				}
			}


			foreach (PropertyDeclaration propDecl in cl.Members.OfType<PropertyDeclaration> ()) {
				if (!propDecl.IsPublicOrOpen)
					continue;
				if (propDecl.IsDeprecated || propDecl.IsUnavailable)
					continue;
				var getDecl = propDecl.GetGetter ();
				var setDecl = propDecl.GetSetter ();

				if (ShouldSkipDeprecated (getDecl, "Property")) {
					continue;
				}

				if (BoundClosureError (getDecl, cl, "wrapping a property getter in a class"))
					continue;
				if (setDecl != null && BoundClosureError (setDecl, cl, "wrapping a property setter in a class"))
					continue;

				if (getDecl != null && getDecl.IsPublicOrOpen) {
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, getDecl);
					} catch (Exception e) {
						SkipWithWarning (getDecl, cl, "wrapping a property getter in a class", e);
						continue;
					}
					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (propDecl, slfunc.Name.Name);
					}
				}

				if (setDecl != null && setDecl.IsPublicOrOpen) {
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, setDecl);
					} catch (Exception e) {
						SkipWithWarning (setDecl, cl, "wrapping a property setter in a class", e);
						continue;
					}
					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (propDecl, slfunc.Name.Name);
					}
				}

			}

			foreach (SubscriptDeclaration subDecl in cl.AllSubscripts ()) {
				if (ShouldSkipDeprecated(subDecl.Getter, "Subscript"))
					continue;
				if (subDecl.Getter.IsPublicOrOpen) {
					if (BoundClosureError (subDecl.Getter, cl, "wrapping a subscript property getter in a class"))
						continue;
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, subDecl.Getter);
					} catch (Exception e) {
						SkipWithWarning (subDecl.Getter, cl, "wrapping a subscript property getter in a class", e);
						continue;
					}
					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (subDecl.Getter, slfunc.Name.Name);
					}
				}

				if (subDecl.Setter != null && subDecl.Setter.IsPublicOrOpen) {
					if (BoundClosureError (subDecl.Setter, cl, "wrapping a subscript property setter in a class"))
						continue;
					SLFunc slfunc = null;
					try {
						slfunc = MapFuncDeclToWrapperFunc (swiftClassName, modules, subDecl.Setter);
					} catch (Exception e) {
						SkipWithWarning (subDecl.Setter, cl, "wrapping a subscript property setter in a class", e);
						continue;
					}
					if (slfunc != null) {
						funcs.Add (slfunc);
						AddFunctionToOverallList (subDecl.Setter, slfunc.Name.Name);
					}
				}
			}

			uniqueModuleReferences.Merge (file.Imports.Select (im => im.Module));

			file.WriteAll (cw);

		}

		bool ShouldSkipDeprecated(FunctionDeclaration decl, string entity)
		{
			if (decl.IsDeprecated || decl.IsUnavailable) {
				var reason = decl.IsDeprecated ? "deprecated" : "unavailable or obsolete";
				var ex = ErrorHelper.CreateWarning (ReflectorError.kWrappingBase + 11, $"{decl.ToFullyQualifiedName ()} is {reason}, skipping.");
				errors.SkippedFunctions.Add (decl.ToFullyQualifiedName (true));
				errors.Add (ex);
				return true;
			}
			return false;
		}

		SwiftClassType GetSwiftClassTypeFromProperty (TLFunction func)
		{
			if (func == null)
				return null;
			return func.Class;
		}


		IEnumerable<SLParameter> FilterCallParams (BaseDeclaration declContext, List<SLParameter> callParms, List<ParameterItem> originalParms,
			SLImportModules modules)
		{
			return callParms.Select ((ntp, i) => {
				if (originalParms [i].TypeSpec is TupleTypeSpec) {
					string type = ntp.ParameterKind == SLParameterKind.InOut ? "UnsafeMutablePointer" : "UnsafePointer";
					return new SLParameter (ntp.PublicName, ntp.PrivateName, new SLBoundGenericType (type, ntp.TypeAnnotation));
				} else {
					return ntp;
				}
			});
		}


		SLFunc MapFuncDeclToWrapperFunc (SwiftClassName className, SLImportModules modules, FunctionDeclaration funcDecl)
		{
			var usedNames = new List<string> ();
			var callParms = new List<SLParameter> ();
			var parms = new List<SLParameter> ();
			var genericDeclaration = new SLGenericTypeDeclarationCollection ();
			var hasInstance = !funcDecl.IsStatic && !funcDecl.IsConstructor;

			if (funcDecl.ParameterLists.Count > 2) {
				throw new NotImplementedException ("support for functions with only 1 or 2 parameter lists.");
			}

			if (funcDecl.ContainsBoundGenericClosure ())
				throw new NotImplementedException ("can't handle closures types bound in generics");

			typeMapper.TypeSpecMapper.MapParams (typeMapper, funcDecl, modules, callParms, funcDecl.ParameterLists.Last (), false, genericDeclaration,
				!String.IsNullOrEmpty (substituteForSelf), substituteForSelf);
			parms.AddRange (FilterCallParams (funcDecl, callParms, funcDecl.ParameterLists.Last (), modules));

			usedNames.AddRange (parms.Select (p => p.PrivateName.Name));
			usedNames.Add (funcDecl.Name);
			usedNames.AddRange (className.NestingNames.Select (nm => nm.Name));

			var parentGenerics = funcDecl.Parent.Generics.Select (gd => {
				var depthIndex = funcDecl.GetGenericDepthAndIndex (gd.Name);
				var grt = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);

				if (genericDeclaration.FirstOrDefault (ag => ag.Name.Name == grt.Name) == null) {
					var sldecl = new SLGenericTypeDeclaration (new SLIdentifier (grt.Name));
#if SWIFT4
#else
					sldecl.Constraints.AddRange (gd.Constraints.Select (bc => {
						var inh = bc as InheritanceConstraint;
						if (bc == null)
							throw new CompilerException ("Equality constraints not supported (yet)");
						return new SLGenericConstraint (true, grt, typeMapper.TypeSpecMapper.MapType (funcDecl, modules, inh.InheritsTypeSpec));
					}));
#endif
					genericDeclaration.Add (sldecl);
				}
				return grt;
			}).ToList ();

			GatherFunctionDeclarationGenerics (funcDecl, genericDeclaration);

			var instanceName = hasInstance ? new SLIdentifier (GetUniqueNameForInstance (parms)) : null;
			bool instanceIsAPointer = false;
			if (hasInstance)
				usedNames.Add (instanceName.Name);

			if (hasInstance) {
				bool instanceIsStructOrEnum = IsStructOrEnum (typeMapper, funcDecl.ParameterLists [0] [0]);
				bool instanceIsProtocol = !instanceIsStructOrEnum && IsProtocol (typeMapper, funcDecl.ParameterLists [0] [0]);
				bool instanceIsValueType = instanceIsStructOrEnum || instanceIsProtocol;
				bool instanceIsExtensionSetter = (funcDecl.IsSetter || funcDecl.IsSubscriptSetter) && funcDecl.IsExtension;

				SLType instanceType = null;
				instanceType = typeMapper.SwiftTypeMapper.MapType (modules, className);
				if (funcDecl.Parent.ContainsGenericParameters) {
					instanceType = new SLBoundGenericType (
						instanceType.ToString (), parentGenerics
						);
				}
				instanceIsAPointer = instanceIsValueType || instanceIsExtensionSetter;
				if (instanceIsAPointer)
					instanceType = new SLBoundGenericType ("UnsafeMutablePointer", instanceType);
				var kind = SLParameterKind.None;// instanceIsValueType || instanceIsExtensionSetter ? SLParameterKind.InOut : SLParameterKind.None;
				parms.Insert (0, new SLParameter (instanceName, instanceType, kind));
			} else {
				AddImportIfNotPresent (modules, className.Module.Name);
				//				modules.AddIfNotPresent (className.Module.Name);
			}

			var retType = FigureReturnTypeSpecSelfSubstitution (funcDecl);

			SLIdentifier returnName = null;
			ICodeElement callLine = null;
			SLType mappedReturn = null;
			bool isGenericClassReturn = retType.ContainsGenericParameters && typeMapper.GetEntityTypeForTypeSpec (retType) == EntityType.Class;
			bool makeInOut = (!isGenericClassReturn && MustBeInOut (funcDecl, retType)) || funcDecl.HasThrows || retType is ProtocolListTypeSpec;

			// Oh hooray! Thanks Apple! The ABI for swift doesn't match the standard ABI for structs which
			// means that if we try to call it normally, we crash hard.
			// To mitigate this, all wrapper functions that return type struct need to have an extra
			// parameter which is a reference to a struct.
			if (makeInOut) {
				returnName = new SLIdentifier (GetUniqueNameForReturn (parms));

				if (funcDecl.HasThrows) {
					SLType slReturn = null;
					if (retType != null && !retType.IsEmptyTuple) {
						slReturn = typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true);
					} else {
						slReturn = SLSimpleType.Void;
					}
					SLType newReturnType = ReturnTypeToExceptionType (slReturn);
					parms.Insert (0, new SLParameter (returnName, newReturnType));
				} else {

					if (funcDecl.IsTypeSpecGeneric (retType)) {
						if (retType.ContainsGenericParameters) {
							SLType retSLType = typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true);
							parms.Insert (0, new SLParameter (returnName,
											   new SLBoundGenericType ("UnsafeMutablePointer", retSLType)));
						} else {
							Tuple<int, int> depthIndex = funcDecl.GetGenericDepthAndIndex (retType);
							parms.Insert (0, new SLParameter (returnName,
															   new SLBoundGenericType ("UnsafeMutablePointer",
																					  new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2))));
						}
					} else {
						parms.Insert (0, new SLParameter (returnName,
							new SLBoundGenericType ("UnsafeMutablePointer", typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true))));
					}
				}
			}

			string callsiteName;
			if (hasInstance) {
				callsiteName = instanceIsAPointer ? $"{instanceName.Name}.pointee" : instanceName.Name;
			} else {
				var callsiteType = typeMapper.TypeSpecMapper.MapType (funcDecl.Parent, modules, TypeSpecParser.Parse (funcDecl.Parent.ToFullyQualifiedNameWithGenerics ()), false);
				callsiteName = callsiteType.ToString ();
			}
			string instanceCallName = null;

			if (funcDecl.IsConstructor) {
				instanceCallName = funcDecl.Parent.ToFullyQualifiedName (false);
			} else if (funcDecl.IsSubscript) {
				if (funcDecl.IsVariadic) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 29, $"Unable to wrap variadic indexer in class {className.ToFullyQualifiedName (true)}");
				}
				instanceCallName = callsiteName;
			} else if (funcDecl.IsProperty) {
				instanceCallName = String.Format ("{0}.{1}", callsiteName, funcDecl.PropertyName);
			} else {
				instanceCallName = String.Format ("{0}.{1}", callsiteName, funcDecl.Name);
			}

			SLArgument [] args = null;
			SLBaseExpr newvalueExpr = null;
			var preMarshalCode = new List<ICodeElement> ();

			var wholeList = BuildArguments (funcDecl, callParms, funcDecl.ParameterLists.Last (), modules,
												preMarshalCode);
			if (funcDecl.IsSetter || funcDecl.IsSubscriptSetter) {
				for (int i = 0; i < wholeList.Count; i++) {
					if (wholeList [i].Identifier.Name == "newValue") {
						newvalueExpr = wholeList [i].Expr;
						wholeList.RemoveAt (i);
						break;
					}
				}
			}
			args = wholeList.ToArray ();

			SLBaseExpr callSite = null;

			if (funcDecl.IsSubscript) {
				var indexSite = new SLSubscriptExpr (new SLIdentifier (instanceCallName),
				                                     args.Select (slarg => slarg.Expr));
				if (funcDecl.IsGetter) {
					callSite = indexSite;
				} else {
					callSite = new SLBinding (indexSite, newvalueExpr);
				}
			} else if (funcDecl.IsProperty) {
				if (funcDecl.IsGetter) {
					callSite = new SLIdentifier (instanceCallName);
				} else {
					callSite = new SLBinding (new SLIdentifier (instanceCallName), newvalueExpr);
				}
			} else {
				if (funcDecl.IsVariadic) {
					var variadicAdapter = BuildVariadicAdapter (new SLIdentifier (instanceCallName), funcDecl, typeMapper, modules);
					var callID = new SLIdentifier (MarshalEngine.Uniqueify ("variadicAdapter", usedNames));
					usedNames.Add (callID.Name);
					var binding = SLDeclaration.LetLine (callID, null, variadicAdapter, Visibility.None);
					preMarshalCode.Add (binding);
					args = StripArgumentLabels (args);
					callSite = new SLFunctionCall (callID.Name, funcDecl.IsConstructor, args);
				} else {
					if (funcDecl.IsOperator) {
						switch (funcDecl.OperatorType) {
						case OperatorType.Infix:
							callSite = new SLLineableOperator (new SLBinaryExpr (funcDecl.Name, args [0].Expr, args [1].Expr));
							break;
						case OperatorType.Prefix:
							callSite = new SLLineableOperator (new SLUnaryExpr (funcDecl.Name, args [0].Expr, true));
							break;
						case OperatorType.Postfix:
							callSite = new SLLineableOperator (new SLUnaryExpr (funcDecl.Name, args [0].Expr, false));
							break;
						default:
							throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 64, $"Unknown static instance operator class for {funcDecl.ToFullyQualifiedName (true)} - neither infix, prefix, nor postfix");
						}
					} else {
						callSite = new SLFunctionCall (instanceCallName, funcDecl.IsConstructor, args);
					}
				}
			}


			// instead of returning, the call line is going to instead
			// be a binding to the inout parameter.
			if (makeInOut) {
				if (funcDecl.HasThrows) {
					// do {
					//     let temp = try callSite // or try callSite
					//     setExceptionNotThrown(value:temp, retval:returnName)
					// } catch let error {
					//     setExceptionThrown(err:error, retval:returnName)
					// }
					AddXamGlueImport (modules);
					//					modules.AddIfNotPresent("XamGlue");
					var doBlock = new SLCodeBlock (null);
					if (retType == null || retType.IsEmptyTuple) {
						doBlock.Add (new SLLine (new SLTry (callSite)));
						doBlock.Add (SLFunctionCall.FunctionCallLine ("setExceptionNotThrown",
											      new SLArgument (new SLIdentifier ("value"),
						                                                              new SLIdentifier ("()"), true),
																	new SLArgument (new SLIdentifier ("retval"), returnName, true)));
					} else {
						string exceptionCall = "setExceptionNotThrown";

						var temp = new SLIdentifier (GetUniqueNameForFoo ("temp", parms));
						var tempDecl = new SLDeclaration (true, new SLBinding (temp, new SLTry (callSite)), Visibility.None);
						doBlock.Add (new SLLine (tempDecl));
						doBlock.Add (SLFunctionCall.FunctionCallLine (exceptionCall,
						                                              new  SLArgument (new SLIdentifier ("value"), temp, true),
						                                              new SLArgument (new SLIdentifier ("retval"), returnName, true)));
					}
					var error = new SLIdentifier (GetUniqueNameForFoo ("error", parms));
					var catcher = new SLCatch (error.Name, null);
					catcher.Body.Add (SLFunctionCall.FunctionCallLine ("setExceptionThrown",
					                                                   new SLArgument (new SLIdentifier ("err"), error, true),
					                                                   new SLArgument (new SLIdentifier ("retval"), returnName, true)));
					var doCatch = new SLDo (doBlock, catcher);
					callLine = doCatch;
				} else {
					// retval.initialize(callSite(...))
					callLine = SLFunctionCall.FunctionCallLine (returnName.Name + ".initialize",
						new SLArgument (new SLIdentifier ("to"), callSite, true));
				}
			} else {
				// Class<T> as opposed to T
				if (retType != null && funcDecl.IsTypeSpecGeneric (retType) && retType.ContainsGenericParameters) {
					var pi = new ParameterItem ();
					pi.PublicName = pi.PrivateName = "notImportant";
					pi.TypeSpec = retType;
					var ntp = typeMapper.TypeSpecMapper.ToParameter (typeMapper, funcDecl, modules, pi,
					                                                 0, true, genericDeclaration, true, "");
					mappedReturn = ntp.TypeAnnotation;

				} else {
					mappedReturn = (retType == null || !retType.IsEmptyTuple) ? typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true) : null;
				}
				// closures are special
				if (funcDecl.ReturnTypeSpec is ClosureTypeSpec closureReturn) {
					if (!closureReturn.Arguments.IsEmptyTuple || !closureReturn.ReturnType.IsEmptyTuple) {
						modules.AddIfNotPresent ("XamGlue");
						var closureArgs = closureReturn.Arguments as TupleTypeSpec;
						var argCount = closureArgs == null ? 1 : closureArgs.Elements.Count;
						var wrapFuncName = closureReturn.ReturnType.IsEmptyTuple ? "swiftActionWrapper" : "swiftFuncWrapper";
						callSite = new SLFunctionCall (wrapFuncName, false, new SLArgument (new SLIdentifier ($"f{argCount}"), callSite, true));
					}
				}
				callLine = mappedReturn != null ?
					SLReturn.ReturnLine (callSite) : new SLLine (callSite);

			}


			string funcName = null;
			if (funcDecl.IsConstructor) {
				funcName = WrapperCtorName (className, funcDecl.IsExtension);
			} else if (funcDecl.IsSubscript) {
				funcName = WrapperName (className, funcDecl.PropertyName,
					(funcDecl.IsGetter ? PropertyType.Getter :
				         (funcDecl.IsSetter ? PropertyType.Setter : PropertyType.Materializer)), true, funcDecl.IsExtension);
			} else if (funcDecl.IsProperty) {
				funcName = WrapperName (className, funcDecl.PropertyName,
					(funcDecl.IsGetter ? PropertyType.Getter :
				         (funcDecl.IsSetter ? PropertyType.Setter : PropertyType.Materializer)), false, funcDecl.IsExtension);
			} else if (funcDecl.IsOperator) {
				funcName = WrapperOperatorName (typeMapper, funcDecl.Parent.ToFullyQualifiedName (true), funcDecl.Name, funcDecl.OperatorType);
			} else {
				funcName = WrapperName (className, funcDecl.Name, funcDecl.IsExtension);
			}

			var funcBody = new SLCodeBlock (preMarshalCode);
			funcBody.Add (callLine);

			var genericDeclarations = ReduceGenericDeclarations (genericDeclaration);
			if (genericDeclarations.Count > 0) {
				funcName = FuncNameWithReferenceCode (funcName, referenceCodeMap.GenerateReferenceCode (funcDecl));
			}

			var func = new SLFunc (Visibility.Public, mappedReturn, new SLIdentifier (funcName),
			                       new SLParameterList (parms), funcBody);


			func.GenericParams.AddRange (ReduceGenericDeclarations (genericDeclaration));
			return func;
		}

		TypeSpec FigureReturnTypeSpecSelfSubstitution (FunctionDeclaration funcDecl)
		{
			// the raison d'etre of this routine is to deal with the complexity of mapping return values of
			// type Self.
			// If a class has a method that returns Self, the resulting type is the type of the class.
			// If a protocol has no methods that take arguments of type Self, then Self is the type of the protocol.
			// If a protocol has methods that take arguments of type Self, then Self is the provided substitute.
			// In no other valid case can Self be a return type.
			if (String.IsNullOrEmpty (substituteForSelf) || !funcDecl.ReturnTypeSpec.HasDynamicSelf)
				return funcDecl.ReturnTypeSpec;

			var parent = funcDecl.Parent as ClassDeclaration;
			if (parent == null)
				throw new ArgumentOutOfRangeException (nameof (funcDecl), "parent should be a class or a protocol");

			// may be a protocol
			var parentProto = parent as ProtocolDeclaration;

			if (parentProto == null)
				return funcDecl.ReturnTypeSpec.ReplaceName ("Self", parent.ToFullyQualifiedNameWithGenerics ());

			return parentProto.HasDynamicSelfInReturnOnly ? funcDecl.ReturnTypeSpec.ReplaceName ("Self", parent.ToFullyQualifiedNameWithGenerics ())
				: funcDecl.ReturnTypeSpec.ReplaceName ("Self", substituteForSelf);
		}

		static SLType ReturnTypeToExceptionType (SLType retType)
		{
			SLType mainType = retType;
			var tup = new SLTupleType (
				new SLNameTypePair ("_", mainType),
				new SLNameTypePair ("_", new SLSimpleType ("Error")),
				new SLNameTypePair ("_", SLSimpleType.Bool));
			return new SLBoundGenericType ("UnsafeMutablePointer", tup);
		}

		public static TypeSpec ReturnTypeToExceptionType (TypeSpec retType)
		{
			var tup = new TupleTypeSpec ();
			tup.Elements.Add (retType);
			tup.Elements.Add (new NamedTypeSpec ("Swift.Error"));
			tup.Elements.Add (new NamedTypeSpec ("Swift.Bool"));
			var exceptionType = new NamedTypeSpec ("Swift.UnsafeMutablePointer");
			exceptionType.GenericParameters.Add (tup);
			return exceptionType;
		}

		static SLGenericTypeDeclarationCollection ReduceGenericDeclarations (SLGenericTypeDeclarationCollection genDecls)
		{
			var uniqDecls =
				genDecls.GroupBy (decl => decl.Name.Name).Select (group => group.First ()).OrderBy (decl => decl.Name.Name).ToList ();
			var newDecls = new SLGenericTypeDeclarationCollection ();
			newDecls.AddRange (uniqDecls);
			return newDecls;
		}



		SLFunc MapTopLevelFuncToWrapperFunc (SLImportModules modules, FunctionDeclaration funcDecl, string wrappingName = null)
		{
			var usedNames = new List<string> ();
			var callParms = new List<SLParameter> ();
			var parms = new List<SLParameter> ();

			if (funcDecl.ContainsBoundGenericClosure ())
				throw new NotImplementedException ("can't handle closures types bound in generics in top level functions");

			// set up the parameters for the declaration
			typeMapper.TypeSpecMapper.MapParams (typeMapper, funcDecl, modules, callParms, funcDecl.ParameterLists.Last (), false,
				remapSelf: !String.IsNullOrEmpty (substituteForSelf), selfReplacement: substituteForSelf);

			usedNames.Add (funcDecl.Name);

			for (int i = 0; i < callParms.Count; i++) {
				var candidateParm = callParms [i];
				var candidateName = (candidateParm.PublicNameIsOptional ? candidateParm.PrivateName : candidateParm.PublicName).Name;
				var finalName = MarshalEngine.Uniqueify (candidateName, usedNames);
				usedNames.Add (finalName);
				if (finalName != candidateName) {
					candidateParm = new SLParameter (candidateParm.PublicName, new SLIdentifier (finalName),
						candidateParm.TypeAnnotation, candidateParm.ParameterKind);
				}
				callParms [i] = candidateParm;
			}

			parms.AddRange (FilterCallParams (funcDecl, callParms, funcDecl.ParameterLists.Last (), modules));

			var retType = funcDecl.ReturnTypeSpec;

			SLIdentifier returnName = null;
			var preMarshalCode = new List<ICodeElement> ();
			ICodeElement callLine = null;
			SLType mappedReturn = null;

			bool makeInOut = MustBeInOut (funcDecl, retType) || funcDecl.HasThrows || retType is ProtocolListTypeSpec;

			// Oh hooray! Thanks Apple! The ABI for swift doesn't match the standard ABI for structs which
			// means that if we try to call it normally, we crash hard.
			// To mitigate this, all wrapper functions that return type struct need to have an extra
			// parameter which is a reference to a struct.
			if (makeInOut) {
				returnName = new SLIdentifier (GetUniqueNameForReturn (parms));
				usedNames.Add (returnName.Name);

				if (funcDecl.HasThrows) {
					SLType slReturn = null;
					if (retType != null && !retType.IsEmptyTuple) {
						slReturn = typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true);
					} else {
						slReturn = SLSimpleType.Void;
					}
					var newReturnType = ReturnTypeToExceptionType (slReturn);
					parms.Insert (0, new SLParameter (returnName, newReturnType));
				} else {
					if (funcDecl.IsTypeSpecGeneric (retType)) {
						if (retType.ContainsGenericParameters || retType is TupleTypeSpec) {
							SLType retSLType = typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true);
							parms.Insert (0, new SLParameter (returnName,
							                                  new SLBoundGenericType ("UnsafeMutablePointer", retSLType)));
						} else {
							Tuple<int, int> depthIndex = funcDecl.GetGenericDepthAndIndex (retType);
							parms.Insert (0, new SLParameter (returnName,
							                                  new SLBoundGenericType ("UnsafeMutablePointer",
														  new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2))));
						}
					} else {
						parms.Insert (0, new SLParameter (returnName,
						                                  new SLBoundGenericType ("UnsafeMutablePointer", typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true))));
					}
				}
			}

			SLBaseExpr callSite = null;
			DelegatedCommaListElemCollection<SLArgument> args = BuildArguments (funcDecl, callParms, funcDecl.ParameterLists [0], modules, preMarshalCode);

			if (funcDecl.IsOperator) {
				switch (funcDecl.OperatorType) {
				case OperatorType.Infix:
					callSite = new SLLineableOperator (new SLBinaryExpr (funcDecl.Name, args [0].Expr, args [1].Expr));
					break;
				case OperatorType.Prefix:
					callSite = new SLLineableOperator (new SLUnaryExpr (funcDecl.Name, args [0].Expr, true));
					break;
				case OperatorType.Postfix:
					callSite = new SLLineableOperator (new SLUnaryExpr (funcDecl.Name, args [0].Expr, false));
					break;
				default:
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 23, $"Unknown operator class for {funcDecl.ToFullyQualifiedName (true)} - neither infix, prefix, nor postfix");
				}
			} else {
				var funcCallName = funcDecl.Name;
				if (funcDecl.IsVariadic) {
					var variadicAdapter = BuildVariadicAdapter (new SLIdentifier (funcCallName), funcDecl, typeMapper, modules);
					var callID = new SLIdentifier (MarshalEngine.Uniqueify ("variadicAdapter", usedNames));
					usedNames.Add (callID.Name);
					var binding = SLDeclaration.LetLine (callID, null, variadicAdapter, Visibility.None);
					preMarshalCode.Add (binding);
					args = StripArgumentLabels (args);
					callSite = new SLFunctionCall (callID, args);
				} else if (funcDecl.IsProperty) {
					modules.AddIfNotPresent (funcDecl.Module.Name);
					if (funcDecl.IsGetter) {
						callSite = new SLIdentifier (funcDecl.PropertyName);
					} else {
						callSite = new SLBinding (new SLIdentifier (funcDecl.PropertyName), args.Last ().Expr);
					}
				} else {
					callSite = new SLFunctionCall (new SLIdentifier (funcCallName), args);
				}
			}
			var closureReturn = retType as ClosureTypeSpec;
			if (closureReturn != null) {
				// anything but () -> ()
				if (!closureReturn.Arguments.IsEmptyTuple || !closureReturn.ReturnType.IsEmptyTuple) {
					modules.AddIfNotPresent ("XamGlue");
					var closureArgs = closureReturn.Arguments as TupleTypeSpec;
					var argCount = closureArgs == null ? 1 : closureArgs.Elements.Count;
					var wrapFuncName = closureReturn.ReturnType.IsEmptyTuple ? "swiftActionWrapper" : "swiftFuncWrapper";
					callSite = new SLFunctionCall (wrapFuncName, false, new SLArgument (new SLIdentifier ($"f{argCount}"), callSite, true));
				}
			}


			// instead of returnning, the call line is going to instead
			// be a binding to the inout parameter.
			if (makeInOut) {
				if (funcDecl.HasThrows) {
					// do {
					//     let temp = try callSite // or try callSite
					//     setExceptionNotThrown(value:temp, retval:returnName)
					// } catch let error {
					//     setExceptionThrown(err:error, retval:returnName)
					// }
					AddXamGlueImport (modules);
					var doBlock = new SLCodeBlock (null);
					if (retType == null || retType.IsEmptyTuple) {
						doBlock.Add (new SLLine (new SLTry (callSite)));
						doBlock.Add (SLFunctionCall.FunctionCallLine ("setExceptionNotThrown",
						                                              new SLArgument (new SLIdentifier ("value"), new SLIdentifier ("()"), true),
						                                              new SLArgument (new SLIdentifier ("retval"), returnName, true)));
					} else {
						string exceptionCall = "setExceptionNotThrown";

						var temp = new SLIdentifier (GetUniqueNameForFoo ("temp", parms));
						var tempDecl = new SLDeclaration (true, new SLBinding (temp, new SLTry (callSite)), Visibility.None);
						doBlock.Add (new SLLine (tempDecl));
						doBlock.Add (SLFunctionCall.FunctionCallLine (exceptionCall,
						                                              new SLArgument (new SLIdentifier ("value"), temp, true),
						                                              new SLArgument (new SLIdentifier ("retval"), returnName, true)));
					}
					var error = new SLIdentifier (GetUniqueNameForFoo ("error", parms));
					var catcher = new SLCatch (error.Name, null);
					catcher.Body.Add (SLFunctionCall.FunctionCallLine ("setExceptionThrown",
					                                                   new SLArgument (new SLIdentifier ("err"), error, true),
					                                                   new SLArgument (new SLIdentifier ("retval"), returnName, true)));
					var doCatch = new SLDo (doBlock, catcher);
					callLine = doCatch;
				} else {
					// retval.initialize(callSite(...))
					callLine = SLFunctionCall.FunctionCallLine (returnName.Name + ".initialize",
						new SLArgument (new SLIdentifier ("to"), callSite, true));
				}
			} else {
				mappedReturn = !retType.IsEmptyTuple ? typeMapper.TypeSpecMapper.MapType (funcDecl, modules, retType, true) : null;
				callLine = mappedReturn != null ?
					SLReturn.ReturnLine (callSite) : new SLLine (callSite);
			}


			var funcBody = new SLCodeBlock (preMarshalCode);
			funcBody.Add (callLine);

			var funcName = funcDecl.IsOperator ? WrapperOperatorName (typeMapper, funcDecl.Module.Name, funcDecl.Name, funcDecl.OperatorType) : WrapperFuncName (funcDecl.Module.Name, funcDecl.Name);
			funcName = wrappingName ?? funcName;

			if (funcDecl.ContainsGenericParameters) {
				funcName = FuncNameWithReferenceCode (funcName, referenceCodeMap.GenerateReferenceCode (funcDecl));
			}

			var func = new SLFunc (Visibility.Public, mappedReturn,
			                       new SLIdentifier (funcName),
			                       new SLParameterList (parms), funcBody);

			if (funcDecl.ContainsGenericParameters) {
				func.GenericParams.AddRange (ToSLGeneric (funcDecl, typeMapper));
			}
			return func;
		}

		public static string FuncNameWithReferenceCode (string funcName, int referenceCode)
		{
			return $"{funcName}{referenceCode.ToString ("D8")}";
		}

		public static IEnumerable<SLGenericTypeDeclaration> ToSLGeneric (FunctionDeclaration context, TypeMapper typeMapper)
		{
			foreach (var generic in context.Generics) {
				yield return ToSLGeneric (context, generic, typeMapper);
			}
		}

		public static SLGenericTypeDeclaration ToSLGeneric (FunctionDeclaration context, GenericDeclaration generic, TypeMapper typeMapper)
		{
			var depthIndex = context.GetGenericDepthAndIndex (generic.Name);
			var gpName = SLGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2);
			var sldecl = new SLGenericTypeDeclaration (new SLIdentifier (gpName));

			foreach (var constraint in generic.Constraints) {
				if (!IsRedundantConstraint (generic, constraint, context, typeMapper)) {
					sldecl.Constraints.Add (ToSLGenericConstraint (context, constraint, gpName));
				}
			}
			return sldecl;
		}

		public static SLGenericConstraint ToSLGenericConstraint (FunctionDeclaration context, BaseConstraint baseConstr, string gpName)
		{
			var inh = baseConstr as InheritanceConstraint;
			if (inh != null) {

				return new SLGenericConstraint (true, new SLSimpleType (gpName), new SLSimpleType (inh.Inherits));
			} else {
				var eq = (EqualityConstraint)baseConstr;
				var type1 = SubstituteGenericName (eq.Type1.Split ('.'), gpName);

				var type2Parts = eq.Type2.Split ('.');
				var type2 = eq.Type2;
				if (context.IsTypeSpecGeneric (type2Parts [0])) {
					var depthIndex = context.GetGenericDepthAndIndex (type2Parts [0]);
					type2 = SubstituteGenericName (type2Parts, SLGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2));
				}

				return new SLGenericConstraint (false, new SLSimpleType (type1), new SLSimpleType (type2));
			}
		}

		static string SubstituteGenericName (string [] pieces, string subPart)
		{
			if (pieces.Length == 1 || pieces.Length == 2) {
				pieces [0] = subPart;
			}
			// Module.T.U
			else if (pieces.Length > 2) {
				pieces [1] = subPart;
			}
			return String.Join (".", pieces);
		}


		static bool IsRedundantConstraint (GenericDeclaration genericDeclaration, BaseConstraint constraint, FunctionDeclaration funcDecl, TypeMapper typeMapper)
		{
			var typeSpecs = funcDecl.ParameterLists.Last ().Select (param => param.TypeSpec).ToList ();
			if (!TypeSpec.IsNullOrEmptyTuple (funcDecl.ReturnTypeSpec))
				typeSpecs.Add (funcDecl.ReturnTypeSpec);
			return IsRedundantConstraint (genericDeclaration, constraint, funcDecl, typeSpecs, typeMapper);
		}

		static bool IsRedundantConstraint (GenericDeclaration genericDeclaration, BaseConstraint constraint, FunctionDeclaration funcDecl, List<TypeSpec> types, TypeMapper typeMapper)
		{
			foreach (var typeSpec in types) {
				if (IsRedundantConstraint (genericDeclaration, constraint, funcDecl, typeSpec, typeMapper))
					return true;
			}
			return false;
		}

		static bool IsRedundantConstraint (GenericDeclaration genericDeclaration, BaseConstraint constraint, FunctionDeclaration funcDecl, TypeSpec typeSpec, TypeMapper typeMapper)
		{
			var namedTypeSpec = typeSpec as NamedTypeSpec;
			if (namedTypeSpec != null) {
				if (!namedTypeSpec.ContainsGenericParameters)
					return false;
				var typeInfo = typeMapper.GetEntityForTypeSpec (namedTypeSpec);
				if (typeInfo != null && typeInfo.Type.ContainsGenericParameters) {
					var sourceDepthIndex = funcDecl.GetGenericDepthAndIndex (genericDeclaration.Name);
					for (int i = 0; i < Math.Min (namedTypeSpec.GenericParameters.Count, typeInfo.Type.Generics.Count()); i++) {
						var subName = namedTypeSpec.GenericParameters [i] as NamedTypeSpec;
						if (subName == null)
							continue;
						if (!subName.IsUnboundGeneric (funcDecl, typeMapper))
							continue;
						var destDepthIndex = funcDecl.GetGenericDepthAndIndex (subName);
						if (destDepthIndex.Item1 == sourceDepthIndex.Item1 && destDepthIndex.Item2 == sourceDepthIndex.Item2 && ConstraintMatchesOnType (constraint, typeInfo, i))
							return true;
					}
				}
				foreach (var specialization in namedTypeSpec.GenericParameters) {
					if (specialization.IsUnboundGeneric (funcDecl, typeMapper))
						continue;
					if (specialization.IsBoundGeneric (funcDecl, typeMapper) &&
					    IsRedundantConstraint (genericDeclaration, constraint, funcDecl, specialization, typeMapper))
						return true;
				}
				return false;
			}

			var tupleTypeSpec = typeSpec as TupleTypeSpec;
			if (tupleTypeSpec != null) {
				foreach (var spec in tupleTypeSpec.Elements) {
					if (IsRedundantConstraint (genericDeclaration, constraint, funcDecl, spec, typeMapper))
						return true;
				}
				return false;
			}

			var closureTypeSpec = typeSpec as ClosureTypeSpec;
			if (closureTypeSpec != null) {
				if (IsRedundantConstraint (genericDeclaration, constraint, funcDecl, closureTypeSpec.Arguments, typeMapper))
					return true;

				if (IsRedundantConstraint (genericDeclaration, constraint, funcDecl, closureTypeSpec.ReturnType, typeMapper))
					return true;
				return false;
			}
			throw new NotImplementedException ($"Unexpected TypeSpec type in IsRedundantConstraint. Expected a named spec, tuple or closure, but got {typeSpec.GetType ().Name}");
		}

		static bool ConstraintMatchesOnType (BaseConstraint constraint, Entity entity, int index)
		{
			var genParam = entity.Type.Generics [index];
			foreach (var genConstraint in genParam.Constraints) {
				if (constraint.EffectiveTypeName () == genConstraint.EffectiveTypeName ())
					return true;
			}

			return false;
		}



		DelegatedCommaListElemCollection<SLArgument> BuildArguments (BaseDeclaration context, List<SLParameter> parms, List<ParameterItem> original,
										    SLImportModules modules, List<ICodeElement> preMarshalCode)
		{
			var retval = new DelegatedCommaListElemCollection<SLArgument> (SLFunctionCall.WriteElement);
			var uniqueNames = new List<SLParameter> ();
			uniqueNames.AddRange (parms);
			retval.AddRange (parms.Select ((nt, i) => {
				bool parmNameIsRequired = original [i].NameIsRequired;
				var originalTypeSpec = original [i].TypeSpec.ReplaceName ("Self", substituteForSelf);
				if (originalTypeSpec is ClosureTypeSpec) {
					var ct = (ClosureTypeSpec)originalTypeSpec;
					var closureName = new SLIdentifier (GetUniqueNameForFoo ("clos", uniqueNames));
					uniqueNames.Add (new SLParameter (closureName, SLSimpleType.Bool)); // type doesn't matter here

					var origArgs = ct.Arguments as TupleTypeSpec;
					var ids = new List<SLNameTypePair> ();
					int origArgsCount = origArgs != null ? origArgs.Elements.Count : 1;
					var clargTypes = typeMapper.TypeSpecMapper.MapType (context, modules, ct.Arguments, false);
					if (!(clargTypes is SLTupleType)) {
						clargTypes = new SLTupleType (new SLNameTypePair ("_", clargTypes));
					}
					var clargTypesAsTuple = (SLTupleType)clargTypes;
					for (int j = 0; j < origArgsCount; j++) {
						SLIdentifier id = new SLIdentifier (GetUniqueNameForFoo ("arg", uniqueNames));
						uniqueNames.Add (new SLParameter (id, SLSimpleType.Bool)); // type doesn't matter
						ids.Add (new SLNameTypePair (id, clargTypesAsTuple.Elements [j].TypeAnnotation));
					}
					var clparms = new SLTupleType (ids);
					var closureBody = new CodeElementCollection<ICodeElement> ();
					var clretType = typeMapper.TypeSpecMapper.MapType (context, modules, ct.ReturnType, true);

					bool hasReturn = !ct.ReturnType.IsEmptyTuple;
					bool hasArgs = !ct.Arguments.IsEmptyTuple;


					var funcPtrId = new SLIdentifier (GetUniqueNameForFoo (nt.PrivateName.Name + "Ptr", uniqueNames));
					var wrappedClosType = nt.TypeAnnotation as SLFuncType;
					// this strips off the @escaping attribute, if any
					var closTypeNoAttr = new SLFuncType (wrappedClosType.ReturnType, wrappedClosType.Parameters);

					SLType funcPtrType = new SLBoundGenericType ("UnsafeMutablePointer", closTypeNoAttr);
					uniqueNames.Add (new SLParameter (funcPtrId, funcPtrType));
					var funcPtrBinding = new SLBinding (funcPtrId,
									    new SLFunctionCall ($"{funcPtrType.ToString ()}.allocate",
											     false,
											      new SLArgument (new SLIdentifier ("capacity"),
													      SLConstant.Val (1), true)));
					closureBody.Add (new SLLine (new SLDeclaration (true, funcPtrBinding, Visibility.None)));
					closureBody.Add (SLFunctionCall.FunctionCallLine (new SLIdentifier ($"{funcPtrId.Name}.initialize"),
											  new SLArgument (new SLIdentifier ("to"),
													  nt.PrivateName, true)));
					var funcPtrAsOpaquePtr = new SLArgument (null, new SLFunctionCall ("OpaquePointer", true, new SLArgument (null, funcPtrId)));

					SLIdentifier retvalPtrId = null;
					SLIdentifier retvalId = null;

					if (hasReturn) {
						retvalPtrId = new SLIdentifier (GetUniqueNameForFoo ("retvalPtr", uniqueNames));
						uniqueNames.Add (new SLParameter (retvalPtrId, SLSimpleType.Bool));
						retvalId = new SLIdentifier (GetUniqueNameForFoo ("retval", uniqueNames));
						uniqueNames.Add (new SLParameter (retvalId, SLSimpleType.Bool));
						var retvalBinding = new SLBinding (retvalPtrId,
										   new SLFunctionCall (
											   $"UnsafeMutablePointer<{clretType.ToString ()}>.allocate",
											   false,
											   new SLArgument (new SLIdentifier ("capacity"),
													   SLConstant.Val (1), true)));
						closureBody.Add (new SLDeclaration (true, retvalBinding, Visibility.None));
					}
					var argsPtrId = new SLIdentifier (GetUniqueNameForFoo ("argsPtr", uniqueNames));
					uniqueNames.Add (new SLParameter (argsPtrId, SLSimpleType.Bool));
					if (hasArgs) {
						var argsBinding = new SLBinding (argsPtrId,
										 new SLFunctionCall (
											 $"UnsafeMutablePointer<{clargTypes.ToString ()}>.allocate",
											 false,
											 new SLArgument (new SLIdentifier ("capacity"),
													 SLConstant.Val (1), true)));
						closureBody.Add (new SLLine (new SLDeclaration (true, argsBinding, Visibility.None)));
						var argsTupleExpr = new SLTupleExpr (clparms.Elements.Select (parm => parm.Name));
						closureBody.Add (SLFunctionCall.FunctionCallLine (new SLIdentifier ($"{argsPtrId.Name}.initialize"),
												  new SLArgument (new SLIdentifier ("to"),
														  argsTupleExpr, true)));
					}

					if (hasReturn) {
						if (hasArgs) {
							closureBody.Add (new SLLine (new SLFunctionCall (nt.PrivateName.Name, false,
													 new SLArgument (null, retvalPtrId, false),
													 new SLArgument (null, argsPtrId, false),
													 funcPtrAsOpaquePtr)));
						} else {
							closureBody.Add (new SLLine (new SLFunctionCall (nt.PrivateName.Name, false,
													 new SLArgument (null, retvalPtrId, false),
													 funcPtrAsOpaquePtr)));
						}
						SLBinding retvalbinding = new SLBinding (retvalId,
											 new SLFunctionCall ($"{retvalPtrId.Name}.move", false));
						closureBody.Add (new SLLine (new SLDeclaration (true, retvalbinding, Visibility.None)));
						closureBody.Add (new SLLine (new SLFunctionCall ($"{retvalPtrId.Name}.deallocate", false)));
					} else {
						if (hasArgs) {
							closureBody.Add (new SLLine (new SLFunctionCall (nt.PrivateName.Name, false,
													 new SLArgument (null, argsPtrId, false),
													 funcPtrAsOpaquePtr)));
						} else {
							closureBody.Add (new SLLine (new SLFunctionCall (nt.PrivateName.Name, false, funcPtrAsOpaquePtr)));
						}
					}


					closureBody.Add (new SLLine (new SLFunctionCall ($"{funcPtrId.Name}.deinitialize", false,
											 new SLArgument (new SLIdentifier ("count"),
													 SLConstant.Val (1), true))));
					closureBody.Add (new SLLine (new SLFunctionCall ($"{funcPtrId.Name}.deallocate", false)));

					if (hasArgs) {
						closureBody.Add (new SLLine (new SLFunctionCall ($"{argsPtrId.Name}.deinitialize", false,
												 new SLArgument (new SLIdentifier ("count"),
														 SLConstant.Val (1), true))));
						closureBody.Add (new SLLine (new SLFunctionCall ($"{argsPtrId.Name}.deallocate", false)));
					}

					if (hasReturn) {
						closureBody.Add (SLReturn.ReturnLine (retvalId));
					}

					var funcType = new SLFuncType (clargTypes, clretType);
					var closure = new SLClosure (null, clparms, closureBody, false);
					var clBinding = new SLBinding (closureName, closure, funcType);
					preMarshalCode.Add (new SLDeclaration (true, clBinding, Visibility.None));
					var closExpr = ct.IsAutoClosure ? (SLBaseExpr)new SLFunctionCall (closureName.Name, false) : closureName;

					return new SLArgument (nt.PublicName, closExpr, parmNameIsRequired);
				} else {
					bool isGeneric = nt.TypeAnnotation is SLGenericReferenceType;
					if (!isGeneric && (originalTypeSpec is TupleTypeSpec || typeMapper.MustForcePassByReference (context, originalTypeSpec))) {
						AddXamGlueImport (modules);
						var objExpr = nt.PrivateName.Dot (new SLIdentifier ("pointee"));
						if (original [i].IsInOut)
							objExpr = new SLUnaryExpr ("&", objExpr, true);
						return new SLArgument (new SLIdentifier (original [i].PublicName), objExpr, parmNameIsRequired);
					} else {
						SLBaseExpr arg = nt.PrivateName;
						if (nt.ParameterKind == SLParameterKind.InOut)
							arg = new SLUnaryExpr ("&", arg, true);
						return new SLArgument (new SLIdentifier (original [i].PublicName), arg, parmNameIsRequired);
					}
				}
			}));

			return retval;
		}

		void GatherFunctionDeclarationGenerics (FunctionDeclaration funcDecl, SLGenericTypeDeclarationCollection slGenerics)
		{
			slGenerics.AddRange (ToSLGeneric (funcDecl, typeMapper));
		}

		DelegatedCommaListElemCollection<SLArgument> BuildStructArguments (List<SLNameTypePair> parms)
		{
			var retval = new DelegatedCommaListElemCollection<SLArgument> (SLFunctionCall.WriteAllElements);
			retval.AddRange (parms.Select (nt => new SLArgument (nt.Name, nt.Name)));
			return retval;
		}


		DelegatedCommaListElemCollection<SLArgument> BuildArgumentsForStructReturn (List<SLNameTypePair> parms)
		{
			var retval = new DelegatedCommaListElemCollection<SLArgument> (SLFunctionCall.WriteElement);
			retval.AddRange (parms.Select (nt => new SLArgument (nt.Name, nt.Name)));
			return retval;
		}

		public static SLBaseExpr BuildVariadicAdapter (SLBaseExpr callSite, FunctionDeclaration funcToWrap, TypeMapper mapper, SLImportModules imports)
		{
			// given a function with one or more variadic parameters, this returns the following call:
			// unsafeBitCast (callSite, to: ((args)->return).self)

			var parameters = new List<SLUnnamedParameter> ();
			foreach (var parameter in funcToWrap.ParameterLists.Last ()) {
				var type = mapper.TypeSpecMapper.MapType (funcToWrap, imports, parameter.TypeSpec, false);
				var slparm = new SLUnnamedParameter (type, parameter.IsInOut ? SLParameterKind.InOut : SLParameterKind.None);
				parameters.Add (slparm);
			}
			var slreturn = mapper.TypeSpecMapper.MapType (funcToWrap, imports, funcToWrap.ReturnTypeSpec, true);
			var closureType = new SLFuncType (slreturn, parameters);
			var fakeID = new SLIdentifier (closureType.ToString ());
			var typeExpr = new SLParenthesisExpression (fakeID).Dot (new SLIdentifier ("self"));
			var unsafeCast = new SLFunctionCall ("unsafeBitCast", false, new SLArgument (null, callSite, false),
							     new SLArgument (new SLIdentifier ("to"), typeExpr, true));
			return unsafeCast;
		}


		static string GetUniqueNameForInstance (List<SLParameter> parms)
		{
			return GetUniqueNameForFoo ("this", parms);
		}

		static string GetUniqueNameForReturn (List<SLParameter> parms)
		{
			return GetUniqueNameForFoo ("retval", parms);
		}

		static string GetUniqueNameForFoo (string foo, List<SLParameter> parms)
		{
			int i = 0;
			string s = null;
			do {
				s = String.Format ("{0}{1}", foo, i > 0 ? i.ToString () : "");
				i++;
			} while (parms.Exists (np => s == (np.PublicName != null ? np.PublicName.Name : "")));
			return s;
		}

		public static bool IsStructOrEnum (TypeMapper t, ParameterItem item)
		{
			var en = t.GetEntityForTypeSpec (item.TypeSpec);
			if (en == null)
				return false;
			return en.IsStructOrEnum && en.EntityType != EntityType.TrivialEnum;
		}

		public static bool IsProtocol (TypeMapper t, ParameterItem item)
		{
			var en = t.GetEntityForTypeSpec (item.TypeSpec);
			if (en == null)
				return false;
			return en.EntityType == EntityType.Protocol;
		}

		bool MustBeInOut (SwiftType st)
		{
			if (st is SwiftGenericArgReferenceType)
				return true;

			if (st.IsClass)
				return false;
			var tt = st as SwiftTupleType;
			if (tt != null) {
				return tt.Contents.Count > 1;
			}
			return typeMapper.MustForcePassByReference (st);
		}

		bool MustBeInOut (FunctionDeclaration funcDecl, TypeSpec ts)
		{
			if (ts.IsUnboundGeneric (funcDecl, typeMapper) && !(ts is ClosureTypeSpec)) {
				return true;
			}

			if (ts is TupleTypeSpec && ((TupleTypeSpec)ts).Elements.Count > 1)
				return true;

			if (funcDecl.IsProtocolWithAssociatedTypesFullPath (ts as NamedTypeSpec, typeMapper))
				return true;

			var en = typeMapper.GetEntityForTypeSpec (ts);
			if (en == null)
				return false;
			if (en.EntityType == EntityType.Class)
				return false;
			return typeMapper.MustForcePassByReference (funcDecl, ts);
		}

		bool BoundClosureError (FunctionDeclaration fn, BaseDeclaration context, string doingSomething)
		{
			if (fn == null)
				return false;
			if (fn.ContainsBoundGenericClosure ()) {
				RecordWarning (fn, context, doingSomething, ReflectorError.kWrappingBase + 12, "contains a bound generic closure, skipping");
				return true;
			}
			return false;
		}

		void SkipWithWarning (FunctionDeclaration fn, BaseDeclaration context, string doingSomething, Exception err)
		{
			RecordWarning (fn, context, doingSomething, ReflectorError.kWrappingBase + 13, err.Message);
		}

		void RecordWarning (FunctionDeclaration fn, BaseDeclaration context, string doingSomething, int code, string postMessage)
		{
			string whoAmI = null;
			if (context == null) { // top level
				whoAmI = $"top level function {fn.ToFullyQualifiedName ()}";
				doingSomething = "";
			} else {
				whoAmI = $"in {context.ToFullyQualifiedName ()}, {fn.ToFullyQualifiedName ()}";
			}
			errors.SkippedFunctions.Add (fn.ToFullyQualifiedName (true));
			errors.Add (ErrorHelper.CreateWarning (code, $"While {doingSomething} {whoAmI} {postMessage}."));
		}

		static Dictionary<char, string> operatorMap = new Dictionary<char, string> {
			{ '/', "Slash" },
			{ '=', "Equals" },
			{ '+', "Plus" },
			{ '-', "Minus" },
			{ '!', "Bang" },
			{ '*', "Star" },
			{ '%', "Percent" },
			{ '<', "LessThan" },
			{ '>', "GreaterThan" },
			{ '&', "Ampersand" },
			{ '|', "Pipe" },
			{ '^', "Hat" },
			{ '~', "Tilde" },
			{ '?', "QuestionMark"},
			{ '.', "Dot" },
		};

		static string OperatorCharToSafeString (char c)
		{
			string result = null;
			operatorMap.TryGetValue (c, out result);
			return result ?? c.ToString ();
		}

		public string CleanseOperatorName (string s)
		{
			return CleanseOperatorName (typeMapper, s);
		}

		public static string CleanseOperatorName (TypeMapper typeMapper, string s)
		{
			var sb = new StringBuilder ();
			foreach (var c in s) {
				sb.Append (OperatorCharToSafeString (c));
			}
			return typeMapper.SanitizeIdentifier (sb.ToString ());
		}

		static DelegatedCommaListElemCollection<SLArgument> StripArgumentLabels (DelegatedCommaListElemCollection<SLArgument> args)
		{
			var result = new DelegatedCommaListElemCollection<SLArgument> (SLFunctionCall.WriteElement);
			result.AddRange (args.Select (arg => new SLArgument (null, arg.Expr, false)));
			return result;
		}

		static SLArgument [] StripArgumentLabels (SLArgument [] args)
		{
			var result = new SLArgument [args.Length];
			for (int i = 0; i < args.Length; i++) {
				result [i] = new SLArgument (null, args [i].Expr, false);
			}
			return result;
		}

		void EstablishSubtituteForSelf (string substitute)
		{
			substituteForSelf = substitute;
		}

		void RelinquishSubstituteForSelf ()
		{
			substituteForSelf = null;
		}

		static FunctionDeclaration ReplaceAndGenericize (ProtocolDeclaration proto, string substituteForSelf, FunctionDeclaration funcDecl)
		{
			if (proto == null || !proto.IsExistential || String.IsNullOrEmpty (substituteForSelf))
				return funcDecl;

			var replacement = new FunctionDeclaration (funcDecl);
			var generic = new GenericDeclaration (substituteForSelf);
			generic.Constraints.Add (new InheritanceConstraint (substituteForSelf, proto.ToFullyQualifiedName ()));
			replacement.Generics.Add (generic);
			var args = replacement.ParameterLists.Last ();
			foreach (var arg in args) {
				if (arg.TypeSpec.HasDynamicSelf) {
					arg.TypeSpec = arg.TypeSpec.ReplaceName ("Self", substituteForSelf);
				}
			}
			if (replacement.ReturnTypeSpec.HasDynamicSelf)
				replacement.ReturnTypeName = replacement.ReturnTypeSpec.ReplaceName ("Self", substituteForSelf).ToString ();

			if (!replacement.IsStatic && !replacement.IsConstructor && replacement.ParameterLists.Count > 0) {
				replacement.ParameterLists [0] [0].TypeName = substituteForSelf;
			}
			return replacement;
		}
	}
}

