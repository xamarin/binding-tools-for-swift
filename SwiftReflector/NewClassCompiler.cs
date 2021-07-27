// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Dynamo;
using Dynamo.CSLang;
using SwiftReflector.Demangling;
using SwiftReflector.ExceptionTools;
using SwiftReflector.Inventory;
using SwiftReflector.IOUtils;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;
using Xamarin;
using SwiftReflector.Importing;
using ObjCRuntime;

namespace SwiftReflector {
	public class WrappingResult {
		public WrappingResult (string modulePath, string moduleLibPath,
		                       ModuleContents inventory, ModuleDeclaration declaration, FunctionReferenceCodeMap functionReferenceCodeMap)
		{
			ModulePath = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (modulePath, nameof(modulePath));
			ModuleLibPath = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (moduleLibPath, nameof(moduleLibPath));
			Contents = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (inventory, nameof(inventory));
			Module = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (declaration, nameof(declaration));
			FunctionReferenceCodeMap = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (functionReferenceCodeMap, nameof (functionReferenceCodeMap));
		}
		public string ModulePath { get; set; }
		public string ModuleLibPath { get; set; }
		public ModuleContents Contents { get; set; }
		public ModuleDeclaration Module { get; set; }
		public FunctionReferenceCodeMap FunctionReferenceCodeMap { get; set; }

		static WrappingResult emptyResult = null;

		public static WrappingResult Empty {
			get {
				if (emptyResult == null) {
					emptyResult = new WrappingResult ("", "", new ModuleContents (new SwiftName ("", false), 0), new ModuleDeclaration (), new FunctionReferenceCodeMap ());
				}
				return emptyResult;
			}
		}

		public bool IsEmpty {
			get {
				return this == emptyResult ||
				(String.IsNullOrEmpty (ModulePath) && String.IsNullOrEmpty (ModuleLibPath) &&
				String.IsNullOrEmpty (Contents.Name.Name));
			}
		}
	}

	public class ClassCompilerNames
	{
		public string ModuleName { get; }
		public string WrappingModuleName { get; }
		public string PinvokeClassPrefix { get; }
		public string GlobalFunctionClassName { get; }

		public ClassCompilerNames (string moduleName, string wrappingModuleName, string pinvokeClassPrefix = "NativeMethodsFor", string globalFunctionClassName = "TopLevelEntities")
		{
			ModuleName = moduleName;
			WrappingModuleName = wrappingModuleName;
			PinvokeClassPrefix = pinvokeClassPrefix;
			GlobalFunctionClassName = globalFunctionClassName;
		}
	}

	public class ClassCompilerLocations
	{
		public List<string> ModuleDirectories { get; }
		public List<string> LibraryDirectories { get; }
		public List<string> TypeDatabasePaths { get; }
		public string XamGluePath { get; }

		public ClassCompilerLocations (List<string> moduleDirectories, List<string> libraryDirectories, List<string> typeDatabasePaths, string xamGluePath = null)
		{
			ModuleDirectories = moduleDirectories;
			LibraryDirectories = libraryDirectories;
			TypeDatabasePaths = typeDatabasePaths;
			XamGluePath = xamGluePath;
		}
	}

	public class ClassCompilerOptions
	{
		public bool TargetPlatformIs64Bit { get; }
		public bool Verbose { get; }
		public bool RetainReflectedXmlOutput { get; }
		public bool RetainSwiftWrappers { get; }
		public UniformTargetRepresentation TargetRepresentation { get; }

		public ClassCompilerOptions (bool targetPlatformIs64Bit, bool verbose, bool retainReflectedXmlOutput, bool retainSwiftWrappers,
			UniformTargetRepresentation targetRepresentation)
		{
			TargetPlatformIs64Bit = targetPlatformIs64Bit;
			Verbose = verbose;
			RetainReflectedXmlOutput = retainReflectedXmlOutput;
			RetainSwiftWrappers = retainSwiftWrappers;
			TargetRepresentation = targetRepresentation;
		}
	}

	public class NewClassCompiler {
		public static string kISwiftObjectName = "ISwiftObject";
		public static CSIdentifier kISwiftObject = new CSIdentifier (kISwiftObjectName);
		public static string kSwiftNativeObjectName = "SwiftNativeObject";
		public static CSIdentifier kSwiftNativeObject = new CSIdentifier (kSwiftNativeObjectName);
		public static string kSwiftObjectGetterName = "SwiftObject";
		public static CSIdentifier kSwiftObjectGetter = new CSIdentifier (kSwiftObjectGetterName);
		public static string kObjcHandleGetterName = "Handle";
		public static CSIdentifier kObjcHandleGetter = new CSIdentifier (kObjcHandleGetterName);
		static string kThisName = "this";
		public static string kInterfaceImplName = "xamarinImpl";
		public static CSIdentifier kInterfaceImpl = new CSIdentifier (kInterfaceImplName);
		public static string kContainerName = "xamarinContainer";
		public static CSIdentifier kContainer = new CSIdentifier (kContainerName);
		public static string kProxyExistentialContainerName = "ProxyExistentialContainer";
		public static CSIdentifier kProxyExistentialContainer = new CSIdentifier (kProxyExistentialContainerName);
		public static string kProtocolWitnessTableName = "ProtocolWitnessTable";
		public static CSIdentifier kProtocolWitnessTable = new CSIdentifier (kProtocolWitnessTableName);
		public static string kGenericSelfName = "TSelf";
		public static CSIdentifier kGenericSelf = new CSIdentifier (kGenericSelfName);
		public static CSIdentifier kMobilePlatforms = new CSIdentifier ("__IOS__ || __MACOS__ || __TVOS__ || __WATCHOS__");

		SwiftCompilerLocation SwiftCompilerLocations;
		SwiftCompilerLocation ReflectorLocations;
		ClassCompilerLocations ClassCompilerLocations;
		ClassCompilerNames CompilerNames;
		ClassCompilerOptions Options;
		UnicodeMapper UnicodeMapper;

		bool Verbose => Options.Verbose;

		Version CompilerVersion;
		PlatformName CurrentPlatform;
		bool OutputIsFramework;

		TypeMapper TypeMapper;
		TopLevelFunctionCompiler TLFCompiler;

		public NewClassCompiler (SwiftCompilerLocation swiftCompilerLocations, ClassCompilerOptions options, UnicodeMapper unicodeMapper)
		{
			ReflectorLocations = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (swiftCompilerLocations, nameof (swiftCompilerLocations));
			SwiftCompilerLocations = new SwiftCompilerLocation ("/usr/bin", "/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0/macosx");
			Options = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (options, nameof (options));
			UnicodeMapper = unicodeMapper;

			CompilerVersion = GetCompilerVersion ();
			if (CompilerVersion == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 13, "Unable to determine the version of the supplied Swift compiler.");
		}

		// "This is probably the best button to push."
		public ErrorHandling CompileToCSharp (
			ClassCompilerLocations classCompilerLocations,
			ClassCompilerNames compilerNames,
			CompilationTargetCollection targets,
			string outputDirectory,
			string minimumOSVersion = null,
			string dylibXmlPath = null)
		{
			ClassCompilerLocations = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (classCompilerLocations, nameof (classCompilerLocations));
			CompilerNames = SwiftRuntimeLibrary.Exceptions.ThrowOnNull (compilerNames, nameof (compilerNames));
			var isLibrary = !string.IsNullOrEmpty (dylibXmlPath);

			var errors = new ErrorHandling ();
			CurrentPlatform = targets.OperatingSystem;

			TypeMapper = new TypeMapper (classCompilerLocations.TypeDatabasePaths, UnicodeMapper);
			BindingImporter.ImportAndMerge (CurrentPlatform, TypeMapper.TypeDatabase, errors);
			if (errors.AnyErrors)
				return errors;
			
			TLFCompiler = new TopLevelFunctionCompiler (TypeMapper);

			var moduleNames = new List<string> { CompilerNames.ModuleName };

			if (Verbose)
				Console.WriteLine ("Aggregating swift types");

			OutputIsFramework = UniformTargetRepresentation.ModuleIsFramework (CompilerNames.ModuleName, ClassCompilerLocations.LibraryDirectories);

			var moduleInventory = GetModuleInventories (ClassCompilerLocations.LibraryDirectories, moduleNames, errors);

			// Dylibs may create extra errors when Getting Module Inventories that we will ignore
			if (isLibrary)
				errors = new ErrorHandling ();

			if (errors.AnyErrors)
				return errors;

			var moduleDeclarations = GetModuleDeclarations (ClassCompilerLocations.ModuleDirectories, moduleNames, outputDirectory,
									Options.RetainReflectedXmlOutput, targets, errors, dylibXmlPath);
			if (errors.AnyErrors)
				return errors;

			var declsPerModule = new List<List<BaseDeclaration>> ();
			foreach (ModuleDeclaration moduleDeclaration in moduleDeclarations) {
				TypeMapper.TypeDatabase.ModuleDatabaseForModuleName (moduleDeclaration.Name);
				var allTypesAndTopLevel = moduleDeclaration.AllTypesAndTopLevelDeclarations;
				TypeMapper.RegisterClasses (allTypesAndTopLevel.OfType<TypeDeclaration> ());
				declsPerModule.Add (allTypesAndTopLevel);
				foreach (var op in moduleDeclaration.Operators) {
					TypeMapper.TypeDatabase.AddOperator (op, moduleDeclaration.Name);
				}
			}

			if (errors.AnyErrors)
				return errors;

			if (Verbose)
				Console.WriteLine ("Wrapping swift types");

			try {
				var result = WrapModuleContents (
					moduleDeclarations,
					moduleInventory,
					ClassCompilerLocations.LibraryDirectories,
					ClassCompilerLocations.ModuleDirectories,
					moduleNames,
					outputDirectory,
					targets,
					CompilerNames.WrappingModuleName,
					Options.RetainSwiftWrappers,
					errors, Verbose, OutputIsFramework, minimumOSVersion, isLibrary);
				if (result == null) {
					var ex = ErrorHelper.CreateError (ReflectorError.kWrappingBase, $"Failed to wrap module{(moduleNames.Count > 1 ? "s" : "")} {moduleNames.InterleaveCommas ()}.");
					errors.Add (ex); 
					return errors;
				}
				CompileModules (moduleDeclarations, moduleInventory, ClassCompilerLocations.LibraryDirectories, outputDirectory, result, errors);
			} catch (Exception err) {
				errors.Add (err); 
			}


			return errors;
		}

		void CompileModules (List<ModuleDeclaration> moduleDeclarations, ModuleInventory moduleInventory,
		                        List<string> swiftLibPaths, string outputDirectory,
		                        WrappingResult wrapper, ErrorHandling errors)
		{
			foreach (ModuleDeclaration module in moduleDeclarations) {
				string swiftLibPath = FindFileForModule (module.Name, swiftLibPaths);
				CompileModuleContents (module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
			}
			WriteTypeDataBase (moduleDeclarations, outputDirectory);
		}

		void WriteTypeDataBase (List<ModuleDeclaration> moduleDeclarations, string outputDirectory)
		{
			string bindingsDir = Path.Combine (outputDirectory, "bindings");

			Directory.CreateDirectory (bindingsDir);

			foreach (ModuleDeclaration module in moduleDeclarations)
				TypeMapper.TypeDatabase.Write (Path.Combine (bindingsDir, module.Name), module.Name);
		}

		void CompileModuleContents (ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
					       string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {
				bool successfulOutput = false;
				successfulOutput |= CompileProtocols (module.Protocols, provider, module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
				successfulOutput |= CompileClasses (module.Classes, provider, module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
				successfulOutput |= CompileStructs (module.Structs, provider, module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
				successfulOutput |= CompileEnums (module.Enums, provider, module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
				successfulOutput |= CompileExtensions (module.Extensions, provider, module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
				successfulOutput |= CompileTopLevelEntities (provider, module, moduleInventory, swiftLibPath, outputDirectory, wrapper, errors);
				if (!successfulOutput)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 16, "binding-tools-for-swift could not generate any output. Check the logs and consider using '--verbose' for more information.");
			}
		}

		bool CompileTopLevelEntities (TempDirectoryFilenameProvider provider,
		                              ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                              string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (Verbose)
				Console.Write ("Compiling top-level entities: ");

			var use = new CSUsingPackages ("System", "System.Runtime.InteropServices");

			var picl = new CSClass (CSVisibility.Internal, PIClassName (module.Name + "." + CompilerNames.GlobalFunctionClassName));
			var usedPinvokes = new List<string> ();

			var cl = CompileTLFuncs (module.TopLevelFunctions, module, moduleInventory,
						 swiftLibPath, outputDirectory, wrapper, errors, use, null, picl, usedPinvokes);

			cl = CompileTLProps (module.TopLevelProperties, module, moduleInventory,
					     swiftLibPath, outputDirectory, wrapper, errors, use, cl, picl, usedPinvokes);


			if (cl != null) {
				string nameSpace = TypeMapper.MapModuleToNamespace (module.Name);
				var nm = new CSNamespace (nameSpace);

				var csfile = new CSFile (use, new CSNamespace [] { nm });
				nm.Block.Add (cl);
				nm.Block.Add (picl);
				string csOutputFileName = string.Format ("{1}{0}.cs", nameSpace, cl.Name.Name);
				string csOutputPath = Path.Combine (outputDirectory, csOutputFileName);

				CodeWriter.WriteToFile (csOutputPath, csfile);

				if (Verbose)
					Console.WriteLine ("Success");
			} else {
				if (Verbose)
					Console.WriteLine ("No top-level entities");
				return false;
			}
			return true;
		}

		CSClass CompileTLProps (IEnumerable<PropertyDeclaration> props,
		                        ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                        string outputDirectory, WrappingResult wrapper, ErrorHandling errors,
		                        CSUsingPackages use, CSClass cl, CSClass picl, List<string> usedPinvokes)
		{
			var properties = new List<CSProperty> ();
			if (Verbose)
				Console.WriteLine (props.Select (pd => pd.ToFullyQualifiedName ()).InterleaveCommas ());

			foreach (PropertyDeclaration prop in props) {
				if (prop.IsDeprecated || prop.IsUnavailable)
					continue;
				try {
					cl = cl ?? new CSClass (CSVisibility.Public, CompilerNames.GlobalFunctionClassName);

					// Calculated properties have a matching __method
					string backingMethodName = ("__" + prop.Name);
					CSMethod backingMethod = cl.Methods.FirstOrDefault (x => x.Name.Name == backingMethodName);

					if (backingMethod == null)
						CompileTLProp (prop, module.SwiftCompilerVersion, moduleInventory, use, wrapper, swiftLibPath, properties, cl, picl, usedPinvokes);
					else
						CompileTLCalculatedProp (prop, backingMethod, backingMethodName, cl);

				} catch (Exception e) {
					errors.Add (e);
				}
			}

			if (properties.Count > 0) {
				cl.Properties.AddRange (properties);
			}
			return cl;
		}

		void CompileTLCalculatedProp (PropertyDeclaration prop, CSMethod backingMethod, string methodName, CSClass cl)
		{				
			var getter = prop.GetGetter ();
			if (getter.IsPublicOrOpen) {
				var setter = prop.GetSetter ();
				bool hasSetter = setter != null && setter.IsPublicOrOpen;

				string propName = TypeMapper.SanitizeIdentifier (prop.Name);

				CSCodeBlock getBlock = CSCodeBlock.Create (CSReturn.ReturnLine (CSFunctionCall.Function (methodName)));
				CSCodeBlock setBlock = null;
				if (hasSetter)
					setBlock = CSCodeBlock.Create (CSFunctionCall.FunctionLine (methodName, (CSIdentifier)"value"));

				CSProperty property = new CSProperty (backingMethod.Type, CSMethodKind.Static, (CSIdentifier)prop.Name, CSVisibility.Public, getBlock, CSVisibility.Public, setBlock);
				cl.Properties.Add (property);
			}
		}

		void CompileTLProp (PropertyDeclaration prop, Version swiftLangVersion, ModuleInventory moduleInventory,
		    CSUsingPackages use, WrappingResult wrapper, string swiftLibPath,
		    List<CSProperty> properties, CSClass cl, CSClass picl, List<string> usedPinvokes)
		{
			var getter = prop.GetGetter ();
			var setter = prop.GetSetter ();
			var propType = MethodWrapping.GetPropertyType (prop, moduleInventory);

			TLFunction getterWrapper = null;
			TLFunction setterWrapper = null;
			FunctionDeclaration setterWrapperFunc = null;
			CSMethod piGetter = null;
			CSMethod piSetter = null;
			string piGetterName = null;
			string piSetterName = null;
			string piGetterRef = null;
			string piSetterRef = null;
			string syntheticClassName = prop.Module.Name + "." + CompilerNames.GlobalFunctionClassName;

			string getWrapperName = MethodWrapping.WrapperName (prop.Module.Name, prop.Name, PropertyType.Getter, false, prop.IsExtension, prop.IsStatic);
			getterWrapper = FindTLPropWrapper (prop, getWrapperName, wrapper);
			var getterWrapperFunc = FindEquivalentFunctionDeclarationForWrapperFunction (getterWrapper, TypeMapper, wrapper);

			piGetterName = PIMethodName ((string)null, getterWrapper.Name, PropertyType.Getter);
			piGetterName = Uniqueify (piGetterName, usedPinvokes);
			usedPinvokes.Add (piGetterName);

			piGetterRef = PIClassName (syntheticClassName) + "." + piGetterName;

			piGetter = TLFCompiler.CompileMethod (getterWrapperFunc, use, PInvokeName (wrapper.ModuleLibPath, swiftLibPath),
						getterWrapper.MangledName, piGetterName, true, true, false);
			picl.Methods.Add (piGetter);

			if (!prop.IsLet && (prop.Storage != StorageKind.Computed ||
			                    (prop.Storage == StorageKind.Computed && setter != null))) {
				string setWrapperName = MethodWrapping.WrapperName (prop.Module.Name, prop.Name, PropertyType.Setter, false, prop.IsExtension, prop.IsStatic);
				setterWrapper = FindTLPropWrapper (prop, setWrapperName, wrapper);
				setterWrapperFunc = FindEquivalentFunctionDeclarationForWrapperFunction (setterWrapper, TypeMapper, wrapper);
				
				piSetterName = PIMethodName ((string)null, setterWrapper.Name, PropertyType.Setter);
				piSetterName = Uniqueify (piSetterName, usedPinvokes);
				usedPinvokes.Add (piSetterName);

				piSetterRef = PIClassName (syntheticClassName) + "." + piSetterName;

				piSetter = TLFCompiler.CompileMethod (setterWrapperFunc, use, PInvokeName (wrapper.ModuleLibPath, swiftLibPath),
							    setterWrapper.MangledName, piSetterName, true, true, false);
				picl.Methods.Add (piSetter);
			}


			var propName = prop.Name;
			var isProtocolListType = TypeMapper.IsCompoundProtocolListType (prop.TypeSpec);
			CSProperty wrapperProp = null;
			CSMethod wrapperGetter = null, wrapperSetter = null;

			if (isProtocolListType) {
				// Yay! Grossness ahead.
				// Swift allows properties to be protocol list types, which means that in C# we need a function that returns
				// a generic type with constraints, but C# properties can't be generic with a constraint, so I have to make the
				// properties appear as a pair of generic methods instead. In the case of top-level property, there may not be
				// an accessor function associated with the property, so instead we need to fake the accessor functions and
				// synthesize them if needed.
				if (getter == null) {
					getter = new FunctionDeclaration () {
						Name = "Get" + propName,
						Access = Accessibility.Public,
						IsStatic = true,
						Module = prop.Module,
						OperatorType = OperatorType.None,
						ReturnTypeName = propType.ToString ()
						
					};
					getter.ParameterLists.Add (new List<ParameterItem> ());
				}
				wrapperGetter = TLFCompiler.CompileMethod (getter, use, wrapper.ModuleLibPath, null, "Get" + propName, false, false, true);
				if (piSetter != null) {
					if (setter == null) {
						setter = new FunctionDeclaration () {
							Name = "Set" + propName,
							Access = Accessibility.Public,
							IsStatic = true,
							Module = prop.Module,
							OperatorType = OperatorType.None,
							ReturnTypeName = "()"
						};
						var pi = new ParameterItem () {
							IsInOut = false,
							PrivateName = "value",
							PublicName = "value",
							TypeName = propType.ToString (),
							IsVariadic = false
						};
						setter.ParameterLists.Add (new List<ParameterItem> ());
						setter.ParameterLists [0].Add (pi);
					}
					wrapperSetter = TLFCompiler.CompileMethod (setter, use, wrapper.ModuleLibPath, null, "Set" + propName, false, false, true);
					var oldParam = wrapperSetter.Parameters [0];
					// Why you ask? Because I was too clever for the nonce.
					// In CompileMethod, deep, deep inside, there's code to prevent accepting a parameter named after a C# keyword (possibly
					// legal in swift), but that's not how properties work when we marshal it later, so we force it back to value because
					// it's outside of a property context in this case. We can't fix it inside CompileMethod because there is no context
					// to determine if it's a property context and refactoring to include the context is ornerous.
					wrapperSetter.Parameters [0] = new CSParameter (oldParam.CSType, new CSIdentifier ("value"), oldParam.ParameterKind);
				}

			} else {
				wrapperProp = TLFCompiler.CompileProperty (prop.Name, use,
									       propType, piGetter != null, piSetter != null,
									       prop.IsStatic ? CSMethodKind.Static : CSMethodKind.None);
			}

			if (piGetter != null) {
				var useLocals = new List<string> {
						wrapperGetter?.Name.Name ?? propName,
						cl.Name.Name
					};

				var marshaler = new MarshalEngine (use, useLocals, TypeMapper, swiftLangVersion);

				var codeBlock = wrapperProp != null ? wrapperProp.Getter : wrapperGetter.Body;
				codeBlock.AddRange (marshaler.MarshalFunctionCall (getterWrapperFunc, false,
					piGetterRef, new CSParameterList (), getterWrapperFunc, prop.TypeSpec, 
					wrapperProp?.PropType ?? wrapperGetter.Type, null, new CSSimpleType (cl.Name.Name), false, wrapper));


																			
			}

			if (piSetter != null) {
				var useLocals = new List<string> {
						wrapperSetter?.Name.Name ?? propName,
						cl.Name.Name,
						"value"
					};
				var valParm = new CSParameter (wrapperProp?.PropType ?? wrapperGetter.Type, new CSIdentifier ("value"));

				var marshaler = new MarshalEngine (use, useLocals, TypeMapper, swiftLangVersion);

				var codeBlock = wrapperProp != null ? wrapperProp.Setter : wrapperSetter.Body;
				codeBlock.AddRange (marshaler.MarshalFunctionCall (setterWrapperFunc, false,
					piSetterRef, new CSParameterList (valParm), getterWrapperFunc, null, CSSimpleType.Void,
					null, new CSSimpleType (cl.Name.Name), false, wrapper));

			}

			if (wrapperProp != null) {
				wrapperProp = new CSProperty (wrapperProp.PropType, CSMethodKind.Static, wrapperProp.Name,
					CSVisibility.Public, wrapperProp.Getter, CSVisibility.Public, wrapperProp.Setter);

				cl.Properties.Add (wrapperProp);
			} else {
				cl.Methods.Add (wrapperGetter);
				if (wrapperSetter != null)
					cl.Methods.Add (wrapperSetter);
			}

		}

		TLFunction FindTLPropWrapper (PropertyDeclaration prop, string wrapperName, WrappingResult wrapper)
		{
			OverloadInventory overload = null;
			if (!wrapper.Contents.Functions.TryGetValue (new SwiftName (wrapperName, false), out overload)) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 9, $"Unable to find wrapping function {wrapperName} for {prop.ToFullyQualifiedName ()}.");
			}

			if (overload.Functions.Count > 1) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 10, $"Expected exactly one overload for wrapping function {wrapperName} for {prop.ToFullyQualifiedName ()}, but got {overload.Functions.Count}.");
			}
			var wrapperTlf = overload.Functions [0];
			return wrapperTlf;
		}

		CSClass CompileTLFuncs (IEnumerable<FunctionDeclaration> funcs,
		                        ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                        string outputDirectory, WrappingResult wrapper, ErrorHandling errors,
		                        CSUsingPackages use, CSClass cl, CSClass picl, List<string> usedPinvokeNames)
		{
			if (Verbose)
				Console.WriteLine (funcs.Select (pd => pd.ToFullyQualifiedName ()).InterleaveCommas ());

			var methods = new List<CSMethod> ();

			foreach (FunctionDeclaration func in funcs) {
				try {
					if (func.IsProperty)
						continue;
					
					// error already generated
					if (func.IsDeprecated || func.IsUnavailable)
						continue;
					if (errors.SkippedFunctions.Contains (func.ToFullyQualifiedName (true))) {
						var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerBase + 13, $"Skipping C# wrapping top-level function {func.ToFullyQualifiedName (true)}, due to a previous error.");
						errors.Add (ex);
						continue;
					}

					CompileTopLevelFunction (func, funcs, moduleInventory, use, wrapper, swiftLibPath, methods, picl, usedPinvokeNames);
				} catch (Exception e) {
					errors.Add (e);
				}
			}

			if (methods.Count > 0) {
				cl = cl ?? new CSClass (CSVisibility.Public, CompilerNames.GlobalFunctionClassName);
				cl.Methods.AddRange (methods);
			}
			return cl;
		}

		void CompileTopLevelFunction (FunctionDeclaration func, IEnumerable<FunctionDeclaration> peerFunctions, ModuleInventory moduleInventory, CSUsingPackages use,
		                              WrappingResult wrapper, string swiftLibPath, List<CSMethod> methods, CSClass picl, List<string> usedPinvokeNames)
		{
			if (MethodWrapping.FuncNeedsWrapping (func, TypeMapper)) {
				CompileToWrapperFunction (func, peerFunctions, moduleInventory, use, wrapper, swiftLibPath, methods, picl, usedPinvokeNames);
			} else {
				CompileToDirectFunction (func, peerFunctions, func.Parent, moduleInventory, use, wrapper, swiftLibPath, methods, picl, usedPinvokeNames);
			}
		}

		void CompileToWrapperFunction (FunctionDeclaration func, IEnumerable<FunctionDeclaration> peerFunctions,
		                               ModuleInventory moduleInventory, CSUsingPackages use,
		                               WrappingResult wrapper, string swiftLibPath, List<CSMethod> methods, CSClass picl,
		                               List<string> usedPinvokeNames)
		{
			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
			var wrapperFunc = finder.FindWrapperForTopLevelFunction (func);
			if (wrapperFunc == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 12, $"Unable to find wrapper function for {func.ToFullyQualifiedName ()}.");

			var wrapperFunction = FindEquivalentTLFunctionForWrapperFunction (wrapperFunc, TypeMapper, wrapper);

			var functionPIBaseName = new SwiftName (TypeMapper.SanitizeIdentifier (wrapperFunc.Name), false);
			string operatorFunctionName = func.IsOperator ? ToOperatorName (func) : null;

			var homonymSuffix = Homonyms.HomonymSuffix (func, peerFunctions, TypeMapper);
			var pinvokeMethodName = PIFuncName (functionPIBaseName) + homonymSuffix;
			pinvokeMethodName = MarshalEngine.Uniqueify (pinvokeMethodName, usedPinvokeNames);
			usedPinvokeNames.Add (pinvokeMethodName);

			string pinvokeMethodRef = PIClassName (func.Module.Name + "." + CompilerNames.GlobalFunctionClassName) + "." + pinvokeMethodName + homonymSuffix;

			var piMethod = TLFCompiler.CompileMethod (wrapperFunc, use, PInvokeName (wrapper.ModuleLibPath, swiftLibPath),
							      wrapperFunction.MangledName, pinvokeMethodName, true, true, false);
			picl.Methods.Add (piMethod);

			var publicMethodOrig = TLFCompiler.CompileMethod (func, use, PInvokeName (swiftLibPath),
									  mangledName: "", operatorFunctionName,
									  false, false, false);

			CSIdentifier wrapperName = GetMethodWrapperName (func, publicMethodOrig, homonymSuffix);
			CSVisibility visibility = GetMethodWrapperVisibility (func, publicMethodOrig);

			// rebuild the method as static
			var publicMethod = new CSMethod (visibility, CSMethodKind.Static, publicMethodOrig.Type,
							 wrapperName, publicMethodOrig.Parameters, publicMethodOrig.Body);
			publicMethod.GenericParameters.AddRange (publicMethodOrig.GenericParameters);
			publicMethod.GenericConstraints.AddRange (publicMethodOrig.GenericConstraints);


			var localIdents = new List<string> {
				publicMethod.Name.Name, pinvokeMethodName
			};
			localIdents.AddRange (publicMethod.Parameters.Select (p => p.Name.Name));
			var marshaler = new MarshalEngine (use, localIdents, TypeMapper, wrapper.Module.SwiftCompilerVersion);

			var lines = marshaler.MarshalFunctionCall (wrapperFunc, false, pinvokeMethodRef,
				publicMethod.Parameters, func, func.ReturnTypeSpec, publicMethod.Type,
				null, null, false, wrapper, false, -1, func.HasThrows);

			publicMethod.Body.AddRange (lines);
			methods.Add (publicMethod);
		}


		void CompileToDirectFunction (FunctionDeclaration func, IEnumerable<FunctionDeclaration> peerFunctions, BaseDeclaration context,
					      ModuleInventory moduleInventory, CSUsingPackages use,
					      WrappingResult wrapper, string swiftLibPath, List<CSMethod> methods, CSClass picl,
		                              List<string> usedPinvokeNames)
		{
			var tlf = XmlToTLFunctionMapper.ToTLFunction (func, moduleInventory, TypeMapper);
			if (tlf == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 15, $"Unable to find function for declaration {func.ToFullyQualifiedName (true)}.");
			var homonymSuffix = Homonyms.HomonymSuffix (func, peerFunctions, TypeMapper);
			CompileToDirectFunction (func, tlf, homonymSuffix, context, use, wrapper, swiftLibPath, methods, picl, usedPinvokeNames);
		}

		void CompileToDirectFunction (FunctionDeclaration func, TLFunction tlf, string homonymSuffix, BaseDeclaration context,
					      CSUsingPackages use, WrappingResult wrapper, string swiftLibPath, List<CSMethod> methods, CSClass picl,
		                              List<string> usedPinvokeNames)
		{
			// FIXME - need to do operators
			if (tlf.Operator != OperatorType.None)
				return;

			var baseName = TypeMapper.SanitizeIdentifier (tlf.Name.Name);
			var pinvokeMethodName = PIFuncName (baseName + homonymSuffix);
			pinvokeMethodName = MarshalEngine.Uniqueify (pinvokeMethodName, usedPinvokeNames);
			usedPinvokeNames.Add (pinvokeMethodName);

			var pinvokeMethodRef = PIClassName ($"{func.Module.Name}.{CompilerNames.GlobalFunctionClassName}") + "." + pinvokeMethodName;

			var piMethod = TLFCompiler.CompileMethod (func, use, PInvokeName (swiftLibPath),
				tlf.MangledName, pinvokeMethodName, true, true, false);
			picl.Methods.Add (piMethod);

			var publicMethodOrig = TLFCompiler.CompileMethod (func, use, PInvokeName (swiftLibPath),
				tlf.MangledName, null, false, false, false);

			CSIdentifier wrapperName = GetMethodWrapperName (func, publicMethodOrig, homonymSuffix);
			CSVisibility visibility = GetMethodWrapperVisibility (func, publicMethodOrig);

			// rebuild the method as static
			var publicMethod = new CSMethod (visibility, CSMethodKind.Static, publicMethodOrig.Type,
							 wrapperName, publicMethodOrig.Parameters, publicMethodOrig.Body);
			publicMethod.GenericParameters.AddRange (publicMethodOrig.GenericParameters);
			publicMethod.GenericConstraints.AddRange (publicMethodOrig.GenericConstraints);

			var localIdents = new List<string> {
				publicMethod.Name.Name, pinvokeMethodName
			};
			localIdents.AddRange (publicMethod.Parameters.Select (p => p.Name.Name));

			var marshaler = new MarshalEngine (use, localIdents, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			var lines = marshaler.MarshalFunctionCall (func, false, pinvokeMethodRef, publicMethod.Parameters,
				func, func.ReturnTypeSpec, publicMethod.Type, null, null, false, wrapper,
				false, -1, func.HasThrows);
			publicMethod.Body.AddRange (lines);
			methods.Add (publicMethod);
		}

		CSIdentifier GetMethodWrapperName (FunctionDeclaration func, CSMethod method, string homonymSuffix)
		{
			string prefix = func.IsProperty ? "__" : "";
			return new CSIdentifier (prefix + method.Name.Name + homonymSuffix);
		}

		CSVisibility GetMethodWrapperVisibility (FunctionDeclaration func, CSMethod method)
		{
			return func.IsProperty ? CSVisibility.Private: method.Visibility;
		}

		public static void ReportCompileStatus (IEnumerable<TypeDeclaration> items, string type)
		{
			ReportCompileStatus (items.Select (pd => pd.ToFullyQualifiedName ()), type);
		}

		public static void ReportCompileStatus (IEnumerable<string> items, string type)
		{
			Console.Write ($"Compiling {type}: ");
			if (items.Any ())
				Console.WriteLine (items.InterleaveCommas ());
			else
				Console.WriteLine ($"No {type} detected");
		}

		bool CompileEnums (IEnumerable<EnumDeclaration> enums, TempDirectoryFilenameProvider provider,
		                   ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                   string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (Verbose)
				ReportCompileStatus (enums, "enums");

			if (!enums.Any ())
				return false;

			var trivialEnums = enums.Where (e => (e.Access == Accessibility.Public || e.Access == Accessibility.Open) &&
			                                !(e.IsDeprecated || e.IsUnavailable) &&
			                                (e.IsTrivial || (e.IsIntegral && e.IsHomogenous && e.Inheritance.Count == 0))).ToList ();
			var nontrivialEnums = enums.Where (e => (e.Access == Accessibility.Public || e.Access == Accessibility.Open) &&
			                                   !(e.IsDeprecated || e.IsUnavailable) &&
			                                   !(e.IsTrivial || (e.IsTrivial || (e.IsIntegral && e.IsHomogenous && e.Inheritance.Count == 0)))).ToList ();

			CompileTrivialEnums (trivialEnums, provider, module, moduleInventory, swiftLibPath,
				outputDirectory, wrapper, errors);
			CompileNontrivialEnums (nontrivialEnums, provider, module, moduleInventory, swiftLibPath,
				outputDirectory, wrapper, errors);
			return true;
		}

		void CompileInnerEnumInto (EnumDeclaration enumDecl, CSClass target, ModuleInventory modInventory, CSUsingPackages use,
		                           WrappingResult wrapper, string swiftLibraryPath, List<CSClass> pinvokes, ErrorHandling errors)
		{
			// trivial
			if (enumDecl.IsTrivial || (enumDecl.IsIntegral && enumDecl.IsHomogenous && enumDecl.Inheritance.Count == 0)) {
				var csEnum = CompileTrivialEnum (enumDecl, modInventory, use, wrapper, swiftLibraryPath);
				CSClass picl = null;
				var csExt = CompileTrivialEnumExtensions (enumDecl, modInventory, use, wrapper, swiftLibraryPath, out picl, errors);
				target.InnerEnums.Add (csEnum);
				if (csExt != null) {
					target.InnerClasses.Add (csExt);
					target.InnerClasses.Add (picl);
				}
			} else { // non-trivial
				var swiftEnumName = XmlToTLFunctionMapper.ToSwiftClassName (enumDecl);
				string enumName = StubbedClassName (swiftEnumName);
				string enumCaseName = enumName + "Cases";

				var enumCase = CompileEnumCases (enumDecl, enumCaseName);
				target.InnerEnums.Add (enumCase);

				var enumClass = CompileEnumClass (enumDecl, enumCaseName, enumCase, use, modInventory, swiftLibraryPath, wrapper, errors, pinvokes);
				target.InnerClasses.Add (enumClass);
			}
		}

		void CompileNontrivialEnums (IEnumerable<EnumDeclaration> enums, TempDirectoryFilenameProvider provider,
		                             ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                             string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			foreach (EnumDeclaration enumDecl in enums) {
				if (enumDecl.Access.IsPrivateOrInternal ())
					continue;
				try {
					if (errors.SkippedTypes.Contains (enumDecl.ToFullyQualifiedName (true))) {
						var ex = ErrorHelper.CreateWarning (ReflectorError.kTypeMapBase + 19, $"Skipping C# wrapping enumeration {enumDecl.ToFullyQualifiedName (true)}, due to a previous error.");
						continue;
					}

					var useEnums = new CSUsingPackages ("System", "System.Runtime.InteropServices");
					string nameSpace = TypeMapper.MapModuleToNamespace (module.Name);
					var nmEnums = new CSNamespace (nameSpace);

					var enumFile = new CSFile (useEnums, new CSNamespace [] { nmEnums });
					var swiftEnumName = XmlToTLFunctionMapper.ToSwiftClassName (enumDecl);
					string enumName = StubbedClassName (swiftEnumName);
					string enumCaseName = enumName + "Cases";

					var enumCase = CompileEnumCases (enumDecl, enumCaseName);
					nmEnums.Block.Add (enumCase);
					var pinvokes = new List<CSClass> ();

					var enumClass = CompileEnumClass (enumDecl, enumCaseName, enumCase, useEnums, moduleInventory, swiftLibPath, wrapper, errors, pinvokes);
					nmEnums.Block.Add (enumClass);
					nmEnums.Block.AddRange (pinvokes);


					// FIXME - need to use the name of the CSClass for the enum
					string enumOutFileName = String.Format ("{0}{1}.cs", enumClass.Name.Name, enumDecl.Module.Name);
					WriteCSFile (enumOutFileName, outputDirectory, enumFile);
				} catch (Exception e) {
					errors.Add (e);
				}
			}
		}

		CSClass CompileEnumClass (EnumDeclaration enumDecl, string enumCaseName, CSEnum enumCase, CSUsingPackages use,
		                          ModuleInventory moduleInventory, string swiftLibPath, WrappingResult wrapper,
		                          ErrorHandling errors, List<CSClass> pinvokes)
		{
			var swiftEnumName = XmlToTLFunctionMapper.ToSwiftClassName (enumDecl);
			var classContents = XmlToTLFunctionMapper.LocateClassContents (moduleInventory, swiftEnumName);
			if (classContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 17, $"Unable to find struct contents for {enumDecl.ToFullyQualifiedName ()}.");
			string enumName = StubbedClassName (swiftEnumName);
			var enumClass = new CSClass (CSVisibility.Public, new CSIdentifier (enumName));
			var enumPI = new CSClass (CSVisibility.Internal, PIClassName (swiftEnumName));
			pinvokes.Add (enumPI);
			var usedPinvokeNames = new List<string> ();

			use.AddIfNotPresent (typeof (SwiftNativeValueType));
			enumClass.Inheritance.Add (typeof (SwiftNativeValueType));
			use.AddIfNotPresent (typeof (ISwiftEnum));
			enumClass.Inheritance.Add (typeof (ISwiftEnum));
			var enumContents = moduleInventory.FindClass (enumDecl.ToFullyQualifiedName (true));
			if (enumContents == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 18, $"Unable to location class contents for enum {enumDecl.ToFullyQualifiedName (true)}.");
			}
			AddGenerics (enumClass, enumDecl, enumContents, use);

			string witName = enumContents.WitnessTable.ValueWitnessTable != null ? enumContents.WitnessTable.ValueWitnessTable.MangledName.Substring (1) : "";
			string nomSym = enumContents.TypeDescriptor.MangledName.Substring (1);
			string metaDataSym = enumContents.DirectMetadata != null ? enumContents.DirectMetadata.MangledName.Substring (1) : "";
			use.AddIfNotPresent (typeof (SwiftEnumTypeAttribute));
			MakeSwiftEnumTypeAttribute (PInvokeName (swiftLibPath), nomSym, metaDataSym, witName).AttachBefore (enumClass);

			string libPath = PInvokeName (wrapper.ModuleLibPath, swiftLibPath);

			ImplementValueTypeIDisposable (enumClass, use);
			AddInheritedProtocols (enumDecl, enumClass, enumContents, PInvokeName (swiftLibPath), use, errors);

			ImplementMethods (enumClass, enumPI, usedPinvokeNames, swiftEnumName, classContents, enumDecl, use, wrapper, tlf => true, swiftLibPath, errors);
			ImplementProperties (enumClass, enumPI, usedPinvokeNames, enumDecl, classContents, null, use, wrapper, true, false, tlf => true, swiftLibPath, errors);
			ImplementSubscripts (enumClass, enumPI, usedPinvokeNames, enumDecl, enumDecl.AllSubscripts (), classContents, null, use, wrapper, true, tlf => true, swiftLibPath, errors);

			var usedNames = new List<string> {
				enumCaseName,
				enumName
			};

			for (int i = 0; i < enumDecl.Elements.Count; i++) {
				var elem = enumDecl.Elements [i];
				var factoryFunc = FindEnumFactoryWrapper (enumDecl, elem, wrapper);
				if (factoryFunc == null) {
					ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 19, $"Unable to find a wrapper function for a factory for {enumDecl.ToFullyQualifiedName (true)}.{elem.Name}.");
				}
				var factoryFuncDecl = FindEquivalentFunctionDeclarationForWrapperFunction (factoryFunc, TypeMapper, wrapper);
				var factoryName = PIMethodName (swiftEnumName, factoryFunc.Name);
				factoryName = MarshalEngine.Uniqueify (factoryName, usedPinvokeNames);
				usedPinvokeNames.Add (factoryName);

				var factoryRef = PIClassName (swiftEnumName) + "." + factoryName;
				var factoryPI = TLFCompiler.CompileMethod (factoryFuncDecl, use, libPath, factoryFunc.MangledName, factoryName,
									   true, false, false);
				enumPI.Methods.Add (factoryPI);

				var factoryMethod = CompileEnumFactory (enumDecl, elem, enumCase.Values [i].Name.Name, factoryFuncDecl,
								       factoryRef, use, wrapper);
				enumClass.Methods.Add (factoryMethod);
			}

			for (int i = 0; i < enumDecl.Elements.Count; i++) {
				var elem = enumDecl.Elements [i];
				if (!elem.HasType)
					continue;
				var payloadFunc = FindEnumPayloadWrapper (enumDecl, elem, wrapper);
				if (payloadFunc == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 20, $"Unable to find a wrapper function for a payload for {enumDecl.ToFullyQualifiedName (true)}.{ elem.Name}.");
				}
				var payloadFuncDecl = FindEquivalentFunctionDeclarationForWrapperFunction (payloadFunc, TypeMapper, wrapper);

				string payloadName = PIMethodName (swiftEnumName, payloadFunc.Name);
				payloadName = MarshalEngine.Uniqueify (payloadName, usedPinvokeNames);
				usedPinvokeNames.Add (payloadName);

				string payloadRef = PIClassName (swiftEnumName) + "." + payloadName;
				var payloadPI = TLFCompiler.CompileMethod (payloadFuncDecl, use, libPath, payloadFunc.MangledName, payloadName,
									   true, false, false);
				enumPI.Methods.Add (payloadPI);

				CompileEnumPayload (enumClass, enumDecl, elem, enumClass.ToCSType(),
				                               new CSSimpleType(enumCase.Name.Name),
				                               enumCase.Values [i].Name.Name, payloadFuncDecl,
				                               payloadRef, use, wrapper);
			}

			var caseFinderFunc = FindEnumCaseFinderWrapper (enumDecl, wrapper);
			if (caseFinderFunc == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 21, $"Unable to find a wrapper function for a case finder for {enumDecl.ToFullyQualifiedName (true)}.");
			}
			var caseFinderWrapperFunc = FindEquivalentFunctionDeclarationForWrapperFunction (caseFinderFunc, TypeMapper, wrapper);

			var caseFinderName = PIMethodName (swiftEnumName, caseFinderFunc.Name);
			caseFinderName = MarshalEngine.Uniqueify (caseFinderName, usedPinvokeNames);
			usedPinvokeNames.Add (caseFinderName);

			string caseFinderRef = PIClassName (swiftEnumName) + "." + caseFinderName;
			CSMethod caseFinderPI = TLFCompiler.CompileMethod (caseFinderWrapperFunc, use, libPath, caseFinderFunc.MangledName, caseFinderName,
						      true, false, false);
			enumPI.Methods.Add (caseFinderPI);
			string finderMethodName = "Case";
			var localUsedNames1 = new List<string> (usedNames);
			MarshalEngine marshal1 = new MarshalEngine (use, localUsedNames1, TypeMapper, wrapper.Module.SwiftCompilerVersion);


			var callingCode1 = marshal1.MarshalFunctionCall (caseFinderWrapperFunc, false, caseFinderRef,
				new CSParameterList (), enumDecl, caseFinderWrapperFunc.ReturnTypeSpec, new CSSimpleType (enumCaseName),
				caseFinderWrapperFunc.ParameterLists.Last () [0].TypeSpec, enumClass.ToCSType (), true, wrapper, true).ToList ();

			var caseProp = new CSProperty (new CSSimpleType (enumCaseName), CSMethodKind.None, new CSIdentifier (finderMethodName),
						    CSVisibility.Public, callingCode1, CSVisibility.Public, null);
			enumClass.Properties.Add (caseProp);


			if (enumDecl.ContainsGenericParameters) {
				enumClass.Methods.AddRange (MakeClassConstructor (enumClass, enumPI, usedPinvokeNames, enumDecl, enumContents, use, libPath, false));
			}

			CompileInnerNominalsInto (enumDecl, enumClass, moduleInventory, use, wrapper, swiftLibPath, pinvokes, errors);

			TypeNameAttribute (enumDecl, use).AttachBefore (enumClass);
			return enumClass;
		}


		CSMethod CompileEnumFactory (EnumDeclaration enumDecl, EnumElement element, string csCaseName, FunctionDeclaration wrappingFunc,
					     string pinvokeRef, CSUsingPackages use, WrappingResult wrapper)
		{
			// format should be:
			// public static EnumType NewCaseName ()
			// {
			//     EnumType retval = new Parameter();
			//     unsafe {
			//         fixed (byte *retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType(this)) {
			//              Pinvoke.ToSwift(new IntPtr(retvalSwiftDataPtr));
			//         }
			//     }
			//     return retval;
			// }
			// or
			// public static EnumType NewCaseName(CSType value)
			// {
			//      EnumType retval = new Parameter()
			//      unsafe {
			//          fixed (byte *retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType(this)) {
			//              // marshal code for value
			//              Pinvoke.ToSwift(new IntPtr(retvalSwiftDataPtr), whateverMarshaledValueIs);
			//         }
			//     }
			//     return retval;
			// }
			//
			// How do we do this?
			// The problem is that there is no original function to do this since the underlying code just wrote the
			// wrapper directly.
			// What we're going to do is write a facsimile of what the original function declaration would have been:
			// public static func NewCaseName(val: IfAny) -> SwiftEnumType
			// and then we'll use MarshalFunctionCall to do that work for us.

			var returnTypeSpec = enumDecl.ToTypeSpec ();
			var factoryDecl = new FunctionDeclaration {
				Module = enumDecl.Module,
				Parent = enumDecl,
				Name = $"New{csCaseName}",
				Access = Accessibility.Public,
				IsStatic = true,
				ReturnTypeName = returnTypeSpec.ToString ()
			};
			factoryDecl.Generics.AddRange (enumDecl.Generics);

			var csEnumType = TypeMapper.MapType (factoryDecl, returnTypeSpec, false).ToCSType(use);

			if (element.HasType) {
				ParameterItem pi = new ParameterItem {
					PublicName = "value",
					PrivateName = "value",
					TypeSpec = element.TypeSpec,
				};
				factoryDecl.ParameterLists.Add (new List<ParameterItem> { pi });
			} else {
				factoryDecl.ParameterLists.Add (new List<ParameterItem> ());
			}

			var localUsedNames = new List<string> { factoryDecl.Name };

			var factoryMethod = TLFCompiler.CompileMethod (factoryDecl, use, null,
								       null, factoryDecl.Name, false, false, true);

			var marshal = new MarshalEngine (use, localUsedNames, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			var callingCode = marshal.MarshalFunctionCall (wrappingFunc, false, pinvokeRef, factoryMethod.Parameters, factoryDecl,
			                                               returnTypeSpec, csEnumType, null, null, false, wrapper, false,
			                                              -1, false);
			factoryMethod.Body.AddRange (callingCode);
			return factoryMethod;
		}

		void CompileEnumPayload(CSClass cl, EnumDeclaration enumDecl, EnumElement element, CSType csEnumType, CSType csEnumCaseType, string csCaseName, FunctionDeclaration wrappingFunc,
					     string pinvokeRef, CSUsingPackages use, WrappingResult wrapper)
		{
			// format should be:
			// __GetValueCase ()
			// {
			//          if (Case != CSCaseNames.CaseName) {
			//              throw new ArgumentOutOfRangeException("Expected Case to be Optional.");
			//          // marshaling for return type
			//          unsafe {
			//              fixed (byte *thisSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType(this)) {
			//                  Pinvoke.ToSwift(..., new IntPtr(thisSwiftDataPtr));
			//                  // more marshaling
			//                  return value;
			//          }
			// }
			// public CSType Value {
			//     get {
			//         return __GetValueCase ()
			//     }
			// }
			// How to do?
			// Since there was no original payload function, we make one
			// that has the signature that we need to marshal it properly.
			// Make a property and call the payload.
			// The signature of the function we need to call in swift will be:
			// public func ValueCaseName() -> SwiftCaseType
			// And MarshalFunctionCall will do the work for us.
			var isProtocolList = TypeMapper.IsCompoundProtocolListType (element.TypeSpec);
			var forcePrivate = !isProtocolList;

			var payloadDecl = new FunctionDeclaration {
				Module = enumDecl.Module,
				Parent = enumDecl,
				Name = $"get_Value{csCaseName}",
				Access = forcePrivate ? Accessibility.Private : Accessibility.Public,
				ReturnTypeName = element.TypeName
			};
			ParameterItem pi = new ParameterItem {
				PublicName = "self",
				PrivateName = "self",
				TypeSpec = new NamedTypeSpec (enumDecl.ToFullyQualifiedName (true)),
			};
			payloadDecl.ParameterLists.Add (new List<ParameterItem> { pi });



			var accessorPrefix = isProtocolList ? "" : "__";
			var csFuncImplName = $"{accessorPrefix}GetValue{csCaseName}";
			var localUsedNames = new List<string> { csFuncImplName };

			var payloadFunc = TLFCompiler.CompileMethod (payloadDecl, use, "", "", csFuncImplName, false, true, false);
			if (!isProtocolList)
				payloadFunc = payloadFunc.AsPrivate ();

			payloadFunc.Parameters.Clear ();

			var throwCall = new CSCodeBlock ();
			throwCall.Add (CSThrow.ThrowLine (new ArgumentOutOfRangeException (), $"Expected Case to be {csCaseName}."));
			var guard = new CSIfElse (new CSIdentifier ("Case") != new CSIdentifier ($"{csEnumCaseType.ToString ()}.{csCaseName}"), throwCall);
			payloadFunc.Body.Add (guard);
			var marshal = new MarshalEngine (use, localUsedNames, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			var callingCode = marshal.MarshalFunctionCall (wrappingFunc, false, pinvokeRef, new CSParameterList (),
								       enumDecl, element.TypeSpec, payloadFunc.Type,
								       new NamedTypeSpec (enumDecl.ToFullyQualifiedName (true)),
								       csEnumType, false, wrapper, false);
			payloadFunc.Body.AddRange (callingCode);
			cl.Methods.Add (payloadFunc);

			if (isProtocolList)
				return;

			var csPropName = $"Value{csCaseName}";
			var payloadProperty = TLFCompiler.CompileProperty (use, csPropName, payloadDecl, null);

			payloadProperty.Getter.Add (CSReturn.ReturnLine (new CSFunctionCall (csFuncImplName, false)));

			cl.Properties.Add (payloadProperty);
		}

		static TLFunction FindEnumCaseFinderWrapper (EnumDeclaration enumDecl, WrappingResult wrapper)
		{
			string caseFinderName = MethodWrapping.EnumCaseFinderWrapperName (enumDecl);
			string errorName = String.Format ("a case finder for {0}", enumDecl.ToFullyQualifiedName (true));
			return FindWrapperFor (wrapper, caseFinderName, errorName);
		}

		static TLFunction FindEnumFactoryWrapper (EnumDeclaration enumDecl, EnumElement elem, WrappingResult wrapper)
		{
			string factoryName = MethodWrapping.EnumFactoryCaseWrapperName (enumDecl, elem.Name);
			string errorName = String.Format ("a factory for {0}.{1}", enumDecl.ToFullyQualifiedName (), elem.Name);
			return FindWrapperFor (wrapper, factoryName, errorName);
		}

		static TLFunction FindEnumPayloadWrapper (EnumDeclaration enumDecl, EnumElement elem, WrappingResult wrapper)
		{
			string factoryName = MethodWrapping.EnumPayloadWrapperName (enumDecl, elem.Name);
			string errorName = String.Format ("a payload for {0}.{1}", enumDecl.ToFullyQualifiedName (), elem.Name);
			return FindWrapperFor (wrapper, factoryName, errorName);
		}

		static TLFunction FindWrapperFor (WrappingResult wrapper, string funcName, string errorName)
		{
			var overloads = wrapper.Contents.Functions.Values.Where (oi => oi.Name.Name == funcName).FirstOrDefault ();
			if (overloads == null)
				return null;
			if (overloads.Functions.Count > 1)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 22, $"Unheard of! Found more that one wrapper function for {errorName} with name {funcName}.");
			return overloads.Functions [0];
		}

		FunctionDeclaration FindProtocolWrapperFunction (FunctionDeclaration func, WrappingResult wrapper)
		{
			return FindSuperWrapper (func, wrapper);
		}

		FunctionDeclaration FindSuperWrapper (FunctionDeclaration superFunc, WrappingResult wrapper)
		{
			string wrapperName = null;
			var namePrefix = String.Empty;
			if (superFunc.Parent is ProtocolDeclaration proto && !proto.IsExistential)
				namePrefix = OverrideBuilder.ProxyPrefix;
			var parentName = PrefixInsertedBeforeName (superFunc.Parent, superFunc.IsProperty, namePrefix);
			if (superFunc.IsProperty) {
				wrapperName = MethodWrapping.WrapperName (parentName,
					superFunc.PropertyName, superFunc.IsGetter ? PropertyType.Getter : PropertyType.Setter, superFunc.IsSubscript, false, superFunc.IsStatic);
			} else {
				wrapperName = MethodWrapping.WrapperName (parentName, superFunc.Name, false, superFunc.IsStatic);
			}
			var referenceCode = wrapper.FunctionReferenceCodeMap.ReferenceCodeFor (superFunc);
			if (referenceCode != null) {
				wrapperName = MethodWrapping.FuncNameWithReferenceCode (wrapperName, referenceCode.Value);
			}

			var funcsToSearch = wrapper.Module.Functions.Where (fn => fn.Name == wrapperName).ToList ();
			var listToMatch = BuildSwiftMarshaledParameterList (superFunc);
			TypeSpec returnType = TypeSpec.IsNullOrEmptyTuple (superFunc.ReturnTypeSpec) ||
						      superFunc.IsTypeSpecGeneric (superFunc.ReturnTypeSpec) ||
						      TypeMapper.MustForcePassByReference (superFunc, superFunc.ReturnTypeSpec) ||
						      superFunc.HasThrows ||
						      superFunc.ReturnTypeSpec is ProtocolListTypeSpec ?
						      TupleTypeSpec.Empty : superFunc.ReturnTypeSpec;
			if (returnType is ClosureTypeSpec clos) {
				returnType = MarshaledClosureType (clos, false);
			}
			return funcsToSearch.FirstOrDefault (fn => {
				bool retsEqual = TypeSpec.BothNullOrEqual (returnType, fn.ReturnTypeSpec);
				bool paramsEqual = ParameterItem.AreEqualIgnoreNamesReferencesInvariant (superFunc, listToMatch, fn, fn.ParameterLists.Last (), true);
				return retsEqual && paramsEqual;
			});
		}

		static string PrefixInsertedBeforeName (BaseDeclaration decl, bool includeModule, string prefix)
		{
			var fullyQualified = decl.ToFullyQualifiedName (includeModule);
			if (prefix == String.Empty)
				return fullyQualified;
			var name = decl.Name;
			return $"{fullyQualified.Substring(0, fullyQualified.Length - name.Length)}{prefix}{name}";
		}

		List<ParameterItem> BuildSwiftMarshaledParameterList (FunctionDeclaration decl)
		{
			var target = new List<ParameterItem> ();
			int payloadParameterList = decl.ParameterLists.Count == 1 ? 0 : 1;
			target.AddRange (decl.ParameterLists [payloadParameterList].Select (pi => {
				if ((!decl.IsTypeSpecGeneric (pi)) && (!pi.IsInOut && TypeMapper.MustForcePassByReference (decl, pi.TypeSpec))) {
					var alt = new ParameterItem (pi) { IsInOut = true };
					return alt;
				} else {
					if (pi.TypeSpec is ClosureTypeSpec clos) {
						var altType = MarshaledClosureType (clos);
						var altParam = new ParameterItem (pi) { TypeSpec = altType };
						return altParam;
					} else {
						return pi;
					}
				}
			}));
			if (payloadParameterList > 0) {
				TypeSpec parmSpec = null;
				if (decl.Parent.ContainsGenericParameters) {
					var sb = new StringBuilder ();

					foreach (string s in decl.Parent.Generics.Select (gen => gen.Name).Interleave (", ")) {
						sb.Append (s);
					}
					string typeName = String.Format ("{0}<{1}>", decl.Parent.ToFullyQualifiedName (), sb.ToString ());
					parmSpec = TypeSpecParser.Parse (typeName);
				} else {
					parmSpec = decl.ParameterLists [0] [0].TypeSpec;
				}
				var pi = new ParameterItem (decl.ParameterLists [0] [0]);
				pi.TypeSpec = parmSpec;
				if (decl.IsSetter && (MethodWrapping.IsProtocol (TypeMapper, pi) || MethodWrapping.IsProtocol (TypeMapper, pi))) {
					pi.IsInOut = true;
				}
				target.Insert (0, pi);
			}
			if (decl.HasThrows || decl.IsTypeSpecGeneric (decl.ReturnTypeSpec) || TypeMapper.MustForcePassByReference (decl, decl.ReturnTypeSpec)
				|| decl.ReturnTypeSpec is ProtocolListTypeSpec) {
				var alt = new ParameterItem ();
				if (decl.HasThrows) {
					alt.PublicName = alt.PrivateName = "retval";
					alt.IsInOut = false;
					var spec = new TupleTypeSpec ();
					if (decl.ReturnTypeSpec == null || decl.ReturnTypeSpec.IsEmptyTuple) {
						spec.Elements.Add (TupleTypeSpec.Empty);
					} else {
						spec.Elements.Add (decl.ReturnTypeSpec);
						spec.Elements.Add (new NamedTypeSpec ("Swift.Error"));
						spec.Elements.Add (new NamedTypeSpec ("Swift.Bool"));
						var ptrSpec = new NamedTypeSpec ("Swift.UnsafeMutablePointer");
						ptrSpec.GenericParameters.Add (spec);
						alt.TypeSpec = ptrSpec;
					}
				} else {
					alt.PublicName = alt.PrivateName = "retval";
					alt.IsInOut = true;
					alt.TypeSpec = decl.ReturnTypeSpec;
				}
				target.Insert (0, alt);
			}
			return target;
		}

		ClosureTypeSpec MarshaledClosureType (ClosureTypeSpec clos, bool addOpaque = true)
		{
			// ()->()  => (Swift.OpaquePointer)->()
			// (args)->() => (Swift.UnsafeMutablePointer<(args, Swift.OpaquePointer)>) -> ()
			// (args)->return => (Swift.UnsafeMutablePointer<(Swift.UnsafeMutablePointer<return>, Swift.UnsafeMutablePointer<(args)>, Swift.OpaquePointer)>)->()
			if (!addOpaque && clos.Arguments.IsEmptyTuple && clos.ReturnType.IsEmptyTuple)
				return clos;

			var args = new TupleTypeSpec ();
			if (clos.HasArguments ())
				args.Elements.Add (new NamedTypeSpec ("Swift.UnsafeMutablePointer", clos.Arguments));
			if (addOpaque)
				args.Elements.Add (new NamedTypeSpec ("Swift.OpaquePointer"));

			ClosureTypeSpec retval = null;
			if (clos.ReturnType.IsEmptyTuple) {
				retval = new ClosureTypeSpec (args, clos.ReturnType);
			} else {
				var returnType = new NamedTypeSpec ("Swift.UnsafeMutablePointer", clos.ReturnType);
				args.Elements.Insert (0, returnType);
				retval = new ClosureTypeSpec (args, TupleTypeSpec.Empty);
			}
			retval.Attributes.AddRange (clos.Attributes);
			return retval;
		}

		CSEnum CompileEnumCases (EnumDeclaration enumDecl, string enumCaseName)
		{
			var csEnum = new CSEnum (CSVisibility.Public, enumCaseName, null);
			foreach (EnumElement elem in enumDecl.Elements) {
				string enumCase = TypeMapper.SanitizeIdentifier (elem.Name);
				var binding = new CSBinding (new CSIdentifier (enumCase));
				csEnum.Values.Add (binding);
			}
			return csEnum;
		}


		void CompileTrivialEnums (List<EnumDeclaration> enums, TempDirectoryFilenameProvider provider,
		                          ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                          string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (enums.Count == 0)
				return;
			bool needsMethods = enums.FirstOrDefault (e => e.AllMethodsNoCDTor ().Count > 0) != null;
			var useEnums = new CSUsingPackages ("System", "System.Runtime.InteropServices");
			var useExtensions = new CSUsingPackages ("System", "System.Runtime.InteropServices");
			string nameSpace = TypeMapper.MapModuleToNamespace (module.Name);
			var nmEnums = new CSNamespace (nameSpace);
			var nmExtensions = new CSNamespace (nameSpace);

			var enumFile = new CSFile (useEnums, new CSNamespace [] { nmEnums });
			var extensionFile = needsMethods ? new CSFile (useExtensions, new CSNamespace [] { nmExtensions }) : null;

			foreach (EnumDeclaration enumDecl in enums) {
				try {
					if (enumDecl.Access.IsPrivateOrInternal ())
						continue;
					var csEnum = CompileTrivialEnum (enumDecl, moduleInventory, useEnums, wrapper, swiftLibPath);
					CSClass picl = null;
					var csExt = CompileTrivialEnumExtensions (enumDecl, moduleInventory, useExtensions, wrapper,
								swiftLibPath, out picl, errors);
					nmEnums.Block.Add (csEnum);
					if (csExt != null) {
						nmExtensions.Block.Add (csExt);
						nmExtensions.Block.Add (picl);
					}
				} catch (Exception e) {
					errors.Add (e);
				}
			}
			try {
				string csEnumOutputFile = String.Format ("{0}Enums.cs", module.Name);
				WriteCSFile (csEnumOutputFile, outputDirectory, enumFile);
				if (nmExtensions.Block.Count > 0) {
					string csExtOutputFile = String.Format ("{0}EnumExtensions.cs", module.Name);
					WriteCSFile (csExtOutputFile, outputDirectory, extensionFile);
				}
			} catch (Exception e) {
				errors.Add (e);
			}
		}

		CSEnum CompileTrivialEnum (EnumDeclaration enumDecl, ModuleInventory module, CSUsingPackages use,
			WrappingResult wrapper, string swiftLibraryPath)
		{
			var swiftEnumName = XmlToTLFunctionMapper.ToSwiftClassName (enumDecl);
			string enumName = StubbedClassName (swiftEnumName);

			bool requiresRawValue = !enumDecl.IsObjC;
			CSType actualBackingType = null;

			CSType enumType = null;
			if (enumDecl.HasRawType || (enumDecl.IsHomogenous && enumDecl.Elements.Count > 0)) {
				var typeSpec = enumDecl.RawTypeSpec ?? enumDecl.Elements [0].TypeSpec;
				if (typeSpec != null) {
					var bundle = TypeMapper.MapType (enumDecl, typeSpec, false);
					if (bundle != null) {
						if (bundle.FullName == "System.nint" || bundle.FullName == "System.nuint") {
							enumType = CSSimpleType.Long;
							actualBackingType = bundle.ToCSType (use);
						} else {
							use.AddIfNotPresent (bundle.NameSpace);
							enumType = bundle.ToCSType (use);
						}
					}
				}
			}

			var csEnum = new CSEnum (CSVisibility.Public, enumName, enumType);
			if (actualBackingType != null) {
				use.AddIfNotPresent (typeof (SwiftEnumBackingTypeAttribute));
				var al = new CSArgumentList ();
				al.Add (actualBackingType.Typeof ());
				CSAttribute.FromAttr (typeof (SwiftEnumBackingTypeAttribute), al, true).AttachBefore (csEnum);
			}
			if (requiresRawValue) {
				use.AddIfNotPresent (typeof (SwiftEnumHasRawValueAttribute));
				var al = new CSArgumentList ();
				al.Add (enumType != null ? enumType.Typeof () : CSSimpleType.Long.Typeof ());
				CSAttribute.FromAttr (typeof (SwiftEnumHasRawValueAttribute), al, true).AttachBefore (csEnum);
			}
			var enumCont = module.Classes.FirstOrDefault (cc => cc.Name.Equals (swiftEnumName));

			string witName = enumCont.WitnessTable.ValueWitnessTable != null ? enumCont.WitnessTable.ValueWitnessTable.MangledName.Substring (1) : "";
			string nomSym = enumCont.TypeDescriptor.MangledName.Substring (1);
			string metaDataSym = enumCont.DirectMetadata != null ? enumCont.DirectMetadata.MangledName.Substring (1) : "";
			use.AddIfNotPresent (typeof (SwiftEnumTypeAttribute));
			MakeSwiftEnumTypeAttribute (PInvokeName (swiftLibraryPath), nomSym, metaDataSym, witName).AttachBefore (csEnum);


			long currentRawValue = 0;
			foreach (EnumElement elem in enumDecl.Elements) {
				string enumCaseName = TypeMapper.SanitizeIdentifier (elem.Name);
				CSConstant enumValue = null;
				if (!requiresRawValue) {
					if (elem.Value != null) {
						enumValue = CSConstant.Val (elem.Value.Value);
					}
				}
				var enumBinding = new CSBinding (enumCaseName, enumValue, true);
				currentRawValue++;
				csEnum.Values.Add (enumBinding);
			}

			TypeNameAttribute (enumDecl, use).AttachBefore (csEnum);
			return csEnum;
		}

		CSClass CompileTrivialEnumExtensions (EnumDeclaration enumDecl, ModuleInventory modInventory, CSUsingPackages use,
		                                      WrappingResult wrapper, string swiftLibraryPath, out CSClass picl, ErrorHandling errors)
		{
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (enumDecl);
			var classContents = XmlToTLFunctionMapper.LocateClassContents (modInventory, swiftClassName);
			if (classContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 25, $"Unable to find enum contents for {enumDecl.ToFullyQualifiedName ()}.");

			string className = StubbedClassName (swiftClassName) + "Extensions";
			use.AddIfNotPresent ("SwiftRuntimeLibrary");

			if (classContents.Properties.Values.Count () == 0) {
				picl = null;
				return null;
			}

			var en = new CSClass (CSVisibility.Public, className, null, true);
			picl = new CSClass (CSVisibility.Internal, PIClassName (swiftClassName));
			var usedPinvokeNames = new List<string> ();


			ImplementProperties (en, picl, usedPinvokeNames, enumDecl, classContents, null, use, wrapper, false, true, tlf => true, swiftLibraryPath, errors);
			ImplementMethods (en, picl, usedPinvokeNames, swiftClassName, classContents, enumDecl, use, wrapper, tlf => true, swiftLibraryPath, errors);
			ImplementTrivialEnumCtors (en, picl, usedPinvokeNames, enumDecl, classContents, use, wrapper, swiftLibraryPath, errors);
			return en;
		}


		bool CompileStructs (IEnumerable<StructDeclaration> structs, TempDirectoryFilenameProvider provider,
		                     ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                     string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (Verbose)
				ReportCompileStatus (structs, "structs");

			if (!structs.Any ())
				return false;
		
			foreach (StructDeclaration decl in structs) {
				try {
					if (decl.IsDeprecated || decl.IsUnavailable)
						continue;
					if (decl.Access.IsPrivateOrInternal ())
						continue;
					if (errors.SkippedTypes.Contains (decl.ToFullyQualifiedName (true))) {
						var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerBase + 23, $"Skipping C# wrapping struct {decl.ToFullyQualifiedName (true)}, due to a previous error.");
						errors.Add (ex);
						continue;
					}
					var use = new CSUsingPackages ("System", "System.Runtime.InteropServices");
					string nameSpace = TypeMapper.MapModuleToNamespace (decl.Module.Name);
					var nm = new CSNamespace (nameSpace);

					var pinvokes = new List<CSClass> ();
					var cl = CompileFinalStruct (decl, moduleInventory, use, wrapper, swiftLibPath, pinvokes, errors);
					nm.Block.Add (cl);
					nm.Block.AddRange (pinvokes);

					var csfile = new CSFile (use, new CSNamespace [] { nm });

					string csOutputFileName = string.Format ("{1}{0}.cs", nameSpace, cl.Name.Name);

					WriteCSFile (csOutputFileName, outputDirectory, csfile);
				} catch (Exception e) {
					errors.Add (e);
				}
			}
			return true;
		}

		CSClass CompileFinalStruct (StructDeclaration structDecl, ModuleInventory modInventory, CSUsingPackages use,
		                            WrappingResult wrapper, string swiftLibraryPath, List<CSClass> pinvokes, ErrorHandling errors)
		{
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (structDecl);
			var classContents = XmlToTLFunctionMapper.LocateClassContents (modInventory, swiftClassName);
			if (classContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 26, $"Unable to find struct contents for {structDecl.ToFullyQualifiedName ()}.");

			string className = StubbedClassName (swiftClassName);
			use.AddIfNotPresent ("SwiftRuntimeLibrary");


			var st = new CSClass (CSVisibility.Public, className, null);
			var picl = new CSClass (CSVisibility.Internal, PIClassName (swiftClassName));
			var usedPinvokeNames = new List<string> ();

			use.AddIfNotPresent (typeof (SwiftNativeValueType));
			st.Inheritance.Add (typeof (SwiftNativeValueType));
			use.AddIfNotPresent (typeof (ISwiftStruct));
			st.Inheritance.Add (typeof (ISwiftStruct));
			pinvokes.Add (picl);
			AddGenerics (st, structDecl, classContents, use);
			ImplementValueTypeIDisposable (st, use);

			CompileInnerNominalsInto (structDecl, st, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors);

			string nomSym = classContents.TypeDescriptor.MangledName.Substring (1);
			string metaDataSym = classContents.DirectMetadata != null ? classContents.DirectMetadata.MangledName.Substring (1) : "";
			// too many underscores - lop of that first one
			// Generics don't have a witness table, it has to be gotten at runtime.
			// If ValueWitnessTable is null, guess what?
			string witSym = classContents.WitnessTable.ValueWitnessTable != null ?
			                             classContents.WitnessTable.ValueWitnessTable.MangledName.Substring (1) :
			                             "";
			MakeSwiftStructTypeAttribute (PInvokeName (swiftLibraryPath),
			                              nomSym, metaDataSym, witSym, use).AttachBefore (st);

			var ctors = MakeStructConstructors (st, picl, usedPinvokeNames, structDecl, classContents,
			                                    use, st.ToCSType (), wrapper, swiftLibraryPath, errors);

			var cctors = MakeClassConstructor (st, picl, usedPinvokeNames, structDecl, classContents,
			                                   use, PInvokeName (swiftLibraryPath), false);


			AddInheritedProtocols (structDecl, st, classContents, PInvokeName (swiftLibraryPath), use, errors);

			st.Constructors.AddRange (ctors);
			st.Constructors.AddRange (cctors);
			ImplementMethods (st, picl, usedPinvokeNames, swiftClassName, classContents, structDecl, use, wrapper, tlf => true, swiftLibraryPath, errors);
			ImplementProperties (st, picl, usedPinvokeNames, structDecl, classContents, null, use, wrapper, true, false, tlf => true, swiftLibraryPath, errors);
			ImplementSubscripts (st, picl, usedPinvokeNames, structDecl, structDecl.AllSubscripts (), classContents, null, use, wrapper, true, tlf => true, swiftLibraryPath, errors);

			TypeNameAttribute (structDecl, use).AttachBefore (st);
			return st;
		}

		bool IsPublicUsableTypeDeclaration (TypeDeclaration decl)
		{
			return (decl.Access == Accessibility.Public || decl.Access == Accessibility.Open) &&
				!decl.IsUnavailable && !decl.IsDeprecated;
		}

		void CompileInnerNominalsInto (TypeDeclaration decl, CSClass target, ModuleInventory modInventory, CSUsingPackages use,
					    WrappingResult wrapper, string swiftLibraryPath, List<CSClass> pinvokes, ErrorHandling errors)
		{
			foreach (ClassDeclaration innerClass in decl.InnerClasses.Where (cd => IsPublicUsableTypeDeclaration (cd))) {
				var inner = innerClass.IsFinal ?
						      CompileFinalClass (innerClass, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors) :
						      CompileVirtualClass (innerClass, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors);
				target.InnerClasses.Add (inner);
			}
			foreach (StructDeclaration innerStruct in decl.InnerStructs.Where (cd => IsPublicUsableTypeDeclaration (cd))) {
				var inner = CompileFinalStruct (innerStruct, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors);
				target.InnerClasses.Add (inner);
			}
			foreach (EnumDeclaration innerEnum in decl.InnerEnums.Where (cd => IsPublicUsableTypeDeclaration (cd))) {
				CompileInnerEnumInto (innerEnum, target, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors);
			}
		}


		bool CompileProtocols (IEnumerable<ProtocolDeclaration> protocols, TempDirectoryFilenameProvider provider,
		                       ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                       string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (Verbose)
				ReportCompileStatus (protocols, "protocols");

			if (!protocols.Any ())
				return false;

			var objCompiler = new ObjCProtocolCompiler (CurrentPlatform, protocols.Where(p => p.IsObjC && !p.IsDeprecated && !p.IsUnavailable), provider, module, swiftLibPath,
			                                            outputDirectory, TypeMapper, wrapper, errors, Verbose);
			objCompiler.Compile (); 

			foreach (ProtocolDeclaration decl in protocols) {
				try {
					if (decl.IsDeprecated || decl.IsUnavailable || decl.IsObjC)
						continue;
					if (decl.Access.IsPrivateOrInternal ())
						continue;
					if (errors.SkippedTypes.Contains(decl.ToFullyQualifiedName(true))) {
						var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerBase + 24, $"Skipping C# wrapping protocol {decl.ToFullyQualifiedName (true)}, due to a previous error.");
						errors.Add (ex);
						continue;
					}
					var use = new CSUsingPackages ("System", "System.Runtime.InteropServices");
					string nameSpace = TypeMapper.MapModuleToNamespace (decl.Module.Name);
					var nm = new CSNamespace (nameSpace);
					CSInterface iface = null;
					CSClass cl = null;
					CSClass picl = null;
					CSStruct vtable = null;
					iface = CompileInterfaceAndProxy (decl, moduleInventory, use, wrapper, swiftLibPath,
						out cl, out picl, out vtable, errors);
					nm.Block.Add (iface);
					nm.Block.Add (cl);
					nm.Block.Add (picl);
					if (vtable?.Delegates.Count > 0)
						nm.Block.Add (vtable);
					CSFile csfile = new CSFile (use, new CSNamespace [] { nm });

					string csOutputFileName = string.Format ("{1}{0}.cs", nameSpace, iface.Name.Name);
					WriteCSFile (csOutputFileName, outputDirectory, csfile);
				} catch (Exception e) {
					errors.Add (e);
				}
			}
			return true;
		}

		bool CompileExtensions (IList<ExtensionDeclaration> extensions, TempDirectoryFilenameProvider provider,
				     ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
				     string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (Verbose)
				ReportCompileStatus (extensions.Select ((ed, i) => $"extension {i} on {ed.ExtensionOnTypeName}"), "extension");

			if (!extensions.Any ())
				return false;

			for (int i = 0; i < extensions.Count (); i++) {
				var extension = extensions [i];
				try {
					if (extension.Members.All (x => x.Access.IsPrivateOrInternal ()))
						continue;
	
					var use = new CSUsingPackages ("System", "System.Runtime.InteropServices");
					string nameSpace = TypeMapper.MapModuleToNamespace (extension.Module.Name);
					var nm = new CSNamespace (nameSpace);

					var pinvokes = new List<CSClass> ();
					var cl = CompileExtensions (extension, i, moduleInventory, use, wrapper, swiftLibPath, pinvokes, errors);
					nm.Block.Add (cl);
					nm.Block.AddRange (pinvokes);

					var csfile = new CSFile (use, new CSNamespace [] { nm });

					string csOutputFileName = string.Format ("{1}{0}.cs", nameSpace, cl.Name.Name);
					WriteCSFile (csOutputFileName, outputDirectory, csfile);
				} catch (Exception e) {
					errors.Add (e);
				}
			}
			return true;
		}

		bool CompileClasses (IEnumerable<ClassDeclaration> classes, TempDirectoryFilenameProvider provider,
		                     ModuleDeclaration module, ModuleInventory moduleInventory, string swiftLibPath,
		                     string outputDirectory, WrappingResult wrapper, ErrorHandling errors)
		{
			if (Verbose)
				ReportCompileStatus (classes, "classes");

			if (!classes.Any ())
				return false;

			foreach (ClassDeclaration decl in classes) {
				if (decl.IsDeprecated || decl.IsUnavailable)
					continue;
				if (decl.Access.IsPrivateOrInternal ())
					continue;
				try {
					if (errors.SkippedTypes.Contains (decl.ToFullyQualifiedName (true))) {
						var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerBase + 25, $"Skipping C# wrapping class {decl.ToFullyQualifiedName (true)}, due to a previous error.");
						errors.Add (ex);
						continue;
					}
					var use = new CSUsingPackages ("System", "System.Runtime.InteropServices");
					string nameSpace = TypeMapper.MapModuleToNamespace (decl.Module.Name);
					var nm = new CSNamespace (nameSpace);

					CSClass cl = null;
					var pinvokes = new List<CSClass> ();
					if (decl.IsFinal) {
						cl = CompileFinalClass (decl, moduleInventory, use, wrapper, swiftLibPath, pinvokes, errors);
					} else {
						cl = CompileVirtualClass (decl, moduleInventory, use, wrapper, swiftLibPath, pinvokes, errors);
					}
					nm.Block.Add (cl);
					nm.Block.AddRange (pinvokes);

					var csfile = new CSFile (use, new CSNamespace [] { nm });

					string csOutputFileName = string.Format ("{1}{0}.cs", nameSpace, cl.Name.Name);
					WriteCSFile (csOutputFileName, outputDirectory, csfile);
				} catch (Exception e) {
					errors.Add (e);
				}
			}
			return true;
		}

		public static string InterfaceNameForProtocol (SwiftClassName cn, TypeMapper mapper)
		{
			return StubbedClassName (cn, mapper);
		}

		public static string InterfaceNameForProtocol (string fullSwiftClassName, TypeMapper mapper)
		{
			return StubbedClassName (fullSwiftClassName, mapper);
		}

		public static string CSProxyNameForProtocol (SwiftClassName cn, TypeMapper mapper)
		{
			string ifacename = InterfaceNameForProtocol (cn, mapper);
			return ifacename.Substring (1) + "XamProxy";
		}

		public static string CSProxyNameForProtocol (string fullSwiftClassName, TypeMapper mapper)
		{
			string ifacename = InterfaceNameForProtocol (fullSwiftClassName, mapper);
			if (ifacename == null)
				return null;
			return ifacename.Substring (1) + "XamProxy";
		}

		public string ToOperatorName (FunctionDeclaration function)
		{
			if (!function.IsOperator)
				throw new ArgumentOutOfRangeException (nameof (function), "supplied function must be an operator");

			var prefix = "";
			switch (function.OperatorType) {
			case OperatorType.Infix:
				prefix = "InfixOperator";
				break;
			case OperatorType.Prefix:
				prefix = "PrefixOperator";
				break;
			case OperatorType.Postfix:
				prefix = "PostfixOperator";
				break;
			case OperatorType.Unknown:
				prefix = "UnknownOperator";
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (function), $"unknown operator type {function.OperatorType.ToString ()}");
			}

			var operatorName = TypeMapper.SanitizeIdentifier (MethodWrapping.CleanseOperatorName (TypeMapper, function.Name));
			return prefix + operatorName;
		}

		CSInterface CompileInterfaceAndProxy (ProtocolDeclaration protocolDecl, ModuleInventory modInventory, CSUsingPackages use,
			WrappingResult wrapper, string swiftLibraryPath, out CSClass proxyClass, out CSClass picl, out CSStruct vtable,
			ErrorHandling errors)
		{
			var wrapperClass = protocolDecl.HasAssociatedTypes || protocolDecl.HasDynamicSelfInArguments ? ProtocolMethodMatcher.FindWrapperClass (wrapper, protocolDecl) : null;
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (protocolDecl);
			var protocolContents = XmlToTLFunctionMapper.LocateProtocolContents (modInventory, swiftClassName);
			var hasDynamicSelf = protocolDecl.HasDynamicSelf;
			var hasDynamicSelfInReturnOnly = protocolDecl.HasDynamicSelfInReturnOnly;
			var hasDynamicSelfInArgs = protocolDecl.HasDynamicSelfInArguments;
			var isExistential = protocolDecl.IsExistential;
			if (protocolContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 27, $"Unable to find class contents for protocol {protocolDecl.ToFullyQualifiedName ()}.");

			string ifaceName = InterfaceNameForProtocol (swiftClassName, TypeMapper);
			string className = isExistential ? CSProxyNameForProtocol (swiftClassName, TypeMapper) : OverrideBuilder.AssociatedTypeProxyClassName (protocolDecl);
			string classNameSuffix = String.Empty;

			if (protocolDecl.HasAssociatedTypes || hasDynamicSelf) {
				// generates <, , ,>
				var genParts = new StringBuilder ();
				genParts.Append ('<');
				var nGenerics = protocolDecl.AssociatedTypes.Count - (hasDynamicSelf ? 0 : 1);
				for (int i = 0; i < nGenerics; i++) {
					if (i > 0)
						genParts.Append (' ');
					genParts.Append (',');
				}
				genParts.Append ('>');
				classNameSuffix = genParts.ToString ();
			}
			use.AddIfNotPresent ("SwiftRuntimeLibrary");
			var iface = new CSInterface (CSVisibility.Public, ifaceName);

			string swiftProxyClassName = protocolDecl.HasAssociatedTypes || hasDynamicSelfInArgs ? OverrideBuilder.AssociatedTypeProxyClassName (protocolDecl) :
				OverrideBuilder.ProxyClassName (protocolDecl);

			use.AddIfNotPresent (typeof (SwiftProtocolTypeAttribute));
			MakeProtocolTypeAttribute (className + classNameSuffix, PInvokeName (swiftLibraryPath),
				protocolContents.TypeDescriptor.MangledName.Substring (1), protocolDecl.HasAssociatedTypes).AttachBefore (iface);

			proxyClass = new CSClass (CSVisibility.Public, className);
			var piClassName = isExistential ? PIClassName (swiftClassName) : PIClassName (wrapperClass.ToFullyQualifiedName ());
			picl = new CSClass (CSVisibility.Internal, piClassName);
			var usedPinvokeNames = new List<string> ();

			if (protocolDecl.HasAssociatedTypes || hasDynamicSelf) {
				AddGenericsToInterface (iface, protocolDecl, use);
				if (hasDynamicSelf) {
					AddDynamicSelfGenericToInterface (iface);
				}
				proxyClass.GenericParams.AddRange (iface.GenericParams);
				proxyClass.GenericConstraints.AddRange (iface.GenericConstraints);
				if (protocolDecl.HasAssociatedTypes || hasDynamicSelfInArgs) {
					proxyClass.Inheritance.Add (typeof (BaseAssociatedTypeProxy));
					var entity = SynthesizeEntityFromWrapperClass (swiftProxyClassName, protocolDecl, wrapper);
					if (!TypeMapper.TypeDatabase.Contains (entity.Type.ToFullyQualifiedName ()))
						TypeMapper.TypeDatabase.Add (entity);
				} else {
					proxyClass.Inheritance.Add (typeof (BaseProxy));
				}
			} else {
				proxyClass.Inheritance.Add (typeof (BaseProxy));
			}
			var inheritance = iface.ToCSType ().ToString ();
			proxyClass.Inheritance.Add (new CSIdentifier (inheritance));

			iface.Inheritance.AddRange (protocolDecl.Inheritance.Select (inh => {
				var netIface = TypeMapper.GetDotNetNameForTypeSpec (inh.InheritedTypeSpec);
				use.AddIfNotPresent (netIface.Namespace);
				return new CSIdentifier (netIface.TypeName);
			}));

			vtable = null;
			var hasVtable = ImplementProtocolDeclarationsVTableAndCSProxy (modInventory, wrapper,
			                                               protocolDecl, protocolContents, iface, 
			                                               proxyClass, picl, usedPinvokeNames, use, swiftLibraryPath,
								       out vtable);


			if (!isExistential) {
				var classContents = wrapper.Contents.Classes.Values.FirstOrDefault (cl => cl.Name.ToFullyQualifiedName () == wrapperClass.ToFullyQualifiedName ());
				if (classContents == null)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 70, $"Unable to find wrapper class contents for protocol {protocolDecl.ToFullyQualifiedName ()}");

				var genericNamer = MakeAssociatedTypeNamer (protocolDecl);

				var ctors = MakeConstructors (proxyClass, picl, usedPinvokeNames, wrapperClass, classContents, null, null, null, use,
					new CSSimpleType (className), wrapper, swiftLibraryPath, true, errors, true, genericNamer,
					isAssociatedTypeProxy: true, hasDynamicSelf: hasDynamicSelf);

				proxyClass.Constructors.AddRange (ctors);

				var cctors = MakeClassConstructor (proxyClass, picl, usedPinvokeNames, wrapperClass, classContents,
				   use, PInvokeName (wrapper.ModuleLibPath), false, hasDynamicSelfInReturnOnly);
				proxyClass.Constructors.AddRange (cctors);


				// this could be done after the if/else, passing in HasAssociateTypes as the last arg,
				// but the ordering of the fields in the previous line and this are important, so to
				// prevent future bugs, keep ImplementMTFields and this in the same order always.
				ImplementProtocolWitnessTableAccessor (proxyClass, iface, protocolDecl, wrapper, use, swiftLibraryPath);
				ImplementProxyConstructorAndFields (proxyClass, use, hasVtable, iface, true);
			} else {
				ImplementProtocolWitnessTableAccessor (proxyClass, iface, protocolDecl, wrapper, use, swiftLibraryPath);
				ImplementProxyConstructorAndFields (proxyClass, use, hasVtable, iface, false);
			}

			TypeNameAttribute (protocolDecl, use).AttachBefore (iface);
			return iface;
		}

		Entity SynthesizeEntityFromWrapperClass (string csClassName, ProtocolDeclaration protocol, WrappingResult wrapper)
		{
			var className = protocol.IsExistential ? OverrideBuilder.ProxyClassName (protocol) : OverrideBuilder.AssociatedTypeProxyClassName (protocol);
			var theClass = wrapper.Module.Classes.FirstOrDefault (cl => cl.Name == className);
			var wrapperclass = wrapper.FunctionReferenceCodeMap.OriginalOrReflectedClassFor (theClass) as ClassDeclaration;
			var entity = new Entity ();
			entity.EntityType = EntityType.Class;
			entity.SharpNamespace = protocol.Module.Name;
			entity.SharpTypeName = csClassName;
			entity.Type = wrapperclass;
			entity.ProtocolProxyModule = protocol.Module.Name;

			return entity;
		}

		internal static Entity SynthesizeEntityFromWrapperClass (string csNamespace, ClassDeclaration cl)
		{
			var entity = new Entity ();
			entity.EntityType = EntityType.Class;
			entity.SharpNamespace = csNamespace;
			entity.SharpTypeName = cl.Name;
			entity.Type = cl;
			entity.ProtocolProxyModule = cl.Module.Name;
			return entity;
		}

		CSClass CompileVirtualClass (ClassDeclaration classDecl, ModuleInventory modInventory, CSUsingPackages use,
		                             WrappingResult wrapper, string swiftLibraryPath, List<CSClass> pinvokes, ErrorHandling errors)
		{
			var isObjC = classDecl.IsObjCOrInheritsObjC (TypeMapper);
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (classDecl);
			var classContents = XmlToTLFunctionMapper.LocateClassContents (modInventory, swiftClassName);
			if (classContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 30, $"Unable to find class contents for {classDecl.ToFullyQualifiedName ()}.");


			string subclassName = String.Format ("{0}.{1}", wrapper.Module.Name, OverrideBuilder.SubclassName (classDecl));
			var subclassEntity = TypeMapper.GetEntityForSwiftClassName (subclassName);
			if (subclassEntity == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 31, $"Unable to find subclass definition {subclassName} for class {classDecl.ToFullyQualifiedName ()}.");
			}
			ClassDeclaration subclassDecl = subclassEntity.Type as ClassDeclaration;
			if (subclassDecl == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 32, $"Unable to find ClassDeclaration for subclass {subclassName}. Found a TypeDeclaration which is a {(subclassEntity.Type == null ? "null value" : subclassEntity.Type.Kind.ToString ())}.");
			}
			var subclassSwiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (subclassDecl);
			var subclassContents = XmlToTLFunctionMapper.LocateClassContents (wrapper.Contents, subclassSwiftClassName);


			string className = StubbedClassName (swiftClassName);
			use.AddIfNotPresent ("SwiftRuntimeLibrary");
			use.AddIfNotPresent (typeof (SwiftValueTypeAttribute));

			var cl = new CSClass (CSVisibility.Public, className, null);
			CSAttribute.FromAttr (typeof (SwiftNativeObjectTagAttribute), new CSArgumentList (), true).AttachBefore (cl);

			var picl = new CSClass (CSVisibility.Internal, PIClassName (subclassSwiftClassName));
			pinvokes.Add (picl);
			var usedPinvokeNames = new List<string> ();

			AddGenerics (cl, classDecl, classContents, use);

			bool inheritsISwiftObject = false;
			DotNetName sharpInherit = null;
			if (classDecl.Inheritance.Count > 0) {
				List<SwiftReflector.SwiftXmlReflection.Inheritance> inheritedClasses = classDecl.Inheritance.Where (inh => inh.InheritanceKind == InheritanceKind.Class).ToList ();
				if (inheritedClasses.Count > 1)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 26, $"Class {classDecl.ToFullyQualifiedName (true)} has more than one class inheritance - not supported in C#.");
				if (inheritedClasses.Count > 0) {
					var inheritedEntity = TypeMapper.GetEntityForTypeSpec (inheritedClasses [0].InheritedTypeSpec);
					inheritsISwiftObject = !inheritedEntity.Type.IsObjC;
					sharpInherit = TypeMapper.GetDotNetNameForTypeSpec (inheritedClasses [0].InheritedTypeSpec);
				}
			}
			if (sharpInherit != null) {
				use.AddIfNotPresent (sharpInherit.Namespace);
				cl.Inheritance.Add (new CSIdentifier (sharpInherit.TypeName));
			} else {
				ImplementFinalizer (cl);
			}

			AddInheritedProtocols (classDecl, cl, classContents, PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath), use, errors);

			if (!classDecl.IsObjCOrInheritsObjC (TypeMapper) && !inheritsISwiftObject)
				cl.Inheritance.Insert (0, kSwiftNativeObject);

			int vtableSize = ImplementVtableMethodsSuperMethodsAndVtable (modInventory, wrapper, subclassDecl, 
			                                                              subclassContents,
			                                                              classDecl, classContents, 
			                                                              cl, picl, usedPinvokeNames, use, swiftLibraryPath);

			IEnumerable<CSMethod> cctor = MakeClassConstructor (cl, picl, usedPinvokeNames, subclassDecl, subclassContents, use,
			                                                    PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath), inheritsISwiftObject);


			IEnumerable<CSMethod> ctors = MakeConstructors (cl, picl, usedPinvokeNames, subclassDecl, subclassContents, classDecl, classContents, swiftClassName, use,
			                                                new CSSimpleType (className), wrapper, swiftLibraryPath, true, errors, inheritsISwiftObject);

			Func<FunctionDeclaration, bool> filter = fn => {
				return fn.Access == Accessibility.Public || fn.IsFinal || fn.IsStatic;
			};

			if (isObjC)
				ImplementObjCClassField (cl);


			ImplementMethods (cl, picl, usedPinvokeNames, subclassSwiftClassName, classContents, classDecl, use, wrapper, filter, swiftLibraryPath, errors);
			ImplementProperties (cl, picl, usedPinvokeNames, classDecl, classContents, subclassSwiftClassName, use, wrapper, false, false, filter, swiftLibraryPath, errors);
			ImplementSubscripts (cl, picl, usedPinvokeNames, classDecl, classDecl.AllSubscripts (), classContents, subclassSwiftClassName, use, wrapper, true, null, swiftLibraryPath, errors);


			cl.Constructors.AddRange (cctor);
			cl.Constructors.AddRange (ctors);

			CompileInnerNominalsInto (classDecl, cl, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors);

			if (vtableSize > 0)
				cl.StaticConstructor.Add (CallToSetVTable ());

			// we manage to fool ourselves - we're compiling a class that is supposed to look like it is
			// in the original Module, but we can't do that - we end up making a wrapper module and as a result
			// when we reference the type, we think that it's in a namespace that doesn't really exist.
			var wrapUse = use.FirstOrDefault (ause =>
				ause.Package == subclassDecl.Module.Name);
			if (wrapUse != null)
				use.Remove (wrapUse);

			TypeNameAttribute (classDecl, use).AttachBefore (cl);
			return cl;
		}

		bool ImplementProtocolDeclarationsVTableAndCSProxy (ModuleInventory modInventory, WrappingResult wrapper,
		                                                    ProtocolDeclaration protocolDecl, ProtocolContents protocolContents, CSInterface iface,
		                                                    CSClass proxyClass, CSClass picl, List<string> usedPinvokeNames, CSUsingPackages use, string swiftLibraryPath,
								    out CSStruct vtable)
		{
			List<FunctionDeclaration> assocWrapperFunctions = null;
			var virtFunctions = new List<FunctionDeclaration> ();
			CollectAllProtocolMethods (virtFunctions, protocolDecl);
			string vtableName = "xamVtable" + iface.Name;
			string vtableTypeName = OverrideBuilder.VtableTypeName (protocolDecl);
			if (virtFunctions.Count > 0)
				proxyClass.Fields.Add (CSFieldDeclaration.FieldLine (new CSSimpleType (vtableTypeName), vtableName, null, CSVisibility.None, true));
			var vtableAssignments = new List<CSLine> ();

			vtable = new CSStruct (CSVisibility.Internal, new CSIdentifier (vtableTypeName));
			int vtableEntryIndex = 0;

			if (!protocolDecl.IsExistential) {
				var matcher = new ProtocolMethodMatcher (protocolDecl, virtFunctions, wrapper);
				assocWrapperFunctions = new List<FunctionDeclaration> ();
				matcher.MatchFunctions (assocWrapperFunctions);
				virtFunctions = assocWrapperFunctions;
			}

			for (int i = 0; i < virtFunctions.Count; i++) {
				if (virtFunctions [i].IsSetter || virtFunctions [i].IsMaterializer)
					continue;

				if (virtFunctions [i].IsProperty) {
					if (virtFunctions [i].IsSubscript) {
						vtableEntryIndex = ImplementPropertyVtableForProtocolVirtualSubscripts (wrapper, protocolDecl, protocolContents, iface, proxyClass, picl, usedPinvokeNames,
															virtFunctions, virtFunctions [i], vtableEntryIndex, vtableName, vtable, vtableAssignments, use, swiftLibraryPath);
					} else {
						vtableEntryIndex = ImplementPropertyVtableForProtocolProperties (wrapper, protocolDecl, protocolContents, iface, proxyClass, picl, usedPinvokeNames,
															virtFunctions, virtFunctions [i], vtableEntryIndex, vtableName, vtable, vtableAssignments, use, swiftLibraryPath);
					}
				} else {
					vtableEntryIndex = ImplementMethodVtableForProtocolVirtualMethods (wrapper, protocolDecl, protocolContents, iface, proxyClass, picl, usedPinvokeNames,
					                                                                   virtFunctions, virtFunctions [i], vtableEntryIndex, vtableName, vtable, vtableAssignments, use, swiftLibraryPath);
				}
			}
			ImplementVTableInitializer (proxyClass, picl, usedPinvokeNames, protocolDecl, vtableAssignments, wrapper, vtableName, swiftLibraryPath);
			return vtable.Delegates.Count > 0;
		}

		void CollectAllProtocolMethods (List<FunctionDeclaration> functions, ProtocolDeclaration decl)
		{
			foreach (TypeSpec spec in decl.Inheritance.Where (inh => inh.InheritanceKind == InheritanceKind.Protocol).Select (inh => inh.InheritedTypeSpec)) {
				Entity entity = TypeMapper.GetEntityForTypeSpec (spec);
				if (entity == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 34, $"Unable to find protocol definition for {spec.ToString ()}, which is a parent of {decl.ToFullyQualifiedName (true)}.");
				}

				ProtocolDeclaration superProtocol = entity.Type as ProtocolDeclaration;
				if (superProtocol == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 35, $"Found an entity for {entity.Type.Name}, but it was a {entity.Type.ToString ()} instead of a protocol.");
				}
				functions.AddRange (superProtocol.AllVirtualMethods ().Where(func => !(func.IsDeprecated || func.IsUnavailable)));
				CollectAllProtocolMethods (functions, superProtocol);
			}
			functions.AddRange (decl.AllVirtualMethods ().Where (func => !(func.IsDeprecated || func.IsUnavailable)));

		}

		int ImplementVtableMethodsSuperMethodsAndVtable (ModuleInventory modInventory, WrappingResult wrapper,
		                                                 ClassDeclaration subclassDecl, ClassContents subclassContents, ClassDeclaration classDecl,
		                                                 ClassContents classContents, CSClass cl, CSClass picl, List<string> usedPinvokeNames, CSUsingPackages use, string swiftLibraryPath)
		{
			var virtFuncs = classDecl.AllVirtualMethods ().Where (func => !(func.IsDeprecated || func.IsUnavailable)).ToList();
			if (virtFuncs.Count == 0)
				return 0;
			string vtableName = "xamVtable" + cl.Name;
			string vtableTypeName = OverrideBuilder.VtableTypeName (classDecl);
			cl.Fields.Add (CSFieldDeclaration.FieldLine (new CSSimpleType (vtableTypeName), vtableName, null, CSVisibility.None, true));
			var vtableAssignments = new List<CSLine> ();


			var vtable = new CSStruct (CSVisibility.None, new CSIdentifier (vtableTypeName));

			int vtableEntryIndex = 0;
			for (int i = 0; i < virtFuncs.Count; i++) {
				if (virtFuncs [i].IsSetter || virtFuncs [i].IsMaterializer || virtFuncs [i].Access != Accessibility.Open)
					continue;

				if (virtFuncs [i].IsProperty) {
					if (virtFuncs [i].IsSubscript) {
						vtableEntryIndex = ImplementPropertyVtableForVirtualSubscripts (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames,
														virtFuncs, virtFuncs [i], vtableEntryIndex, vtableName, vtable, vtableAssignments, use, swiftLibraryPath);
					} else {
						vtableEntryIndex = ImplementPropertyVtableForVirtualProperties (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames,
														virtFuncs, virtFuncs [i], vtableEntryIndex, vtableName, vtable, vtableAssignments, use, swiftLibraryPath);
					}
				} else {
					vtableEntryIndex = ImplementMethodVtableForVirtualMethods (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames,
												   virtFuncs, virtFuncs [i], vtableEntryIndex, vtableName, vtable, vtableAssignments, use, swiftLibraryPath);
				}
			}
			if (vtable.Delegates.Count > 0) {
				cl.InnerClasses.Add (vtable);
			}
			ImplementVTableInitializer (cl, picl, usedPinvokeNames, classDecl, vtableAssignments, wrapper, vtableName, swiftLibraryPath);
			return virtFuncs.Count;
		}

		int ImplementMethodVtableForProtocolVirtualMethods (WrappingResult wrapper,
		                                                    ProtocolDeclaration protocolDecl, ProtocolContents protocolContents,
		                                                    CSInterface iface, CSClass proxyClass, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFunctions,
		                                                    FunctionDeclaration func, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                                    List<CSLine> vtableAssignments, CSUsingPackages use, string swiftLibraryPath)
		{
			var homonymSuffix = Homonyms.HomonymSuffix (func, protocolDecl.Members.OfType<FunctionDeclaration>(), TypeMapper);
			var wrapperFunction = FindProtocolWrapperFunction (func, wrapper);
			if (wrapperFunction == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 36, $"Unable to find wrapper function for {func.Name} in protocol {protocolDecl.ToFullyQualifiedName (true)}.");
			}
			var delegateDecl = TLFCompiler.CompileToDelegateDeclaration (func, use, null, "Del" + OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex),
				true, CSVisibility.Public, !protocolDecl.IsObjC && protocolDecl.IsExistential);
			vtable.Delegates.Add (delegateDecl);
			var field = new CSFieldDeclaration (new CSSimpleType (delegateDecl.Name.Name), OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), null, CSVisibility.Public);
			CSAttribute.MarshalAsFunctionPointer ().AttachBefore (field);
			vtable.Fields.Add (new CSLine (field));


			var tlWrapperFunction = XmlToTLFunctionMapper.ToTLFunction (wrapperFunction, wrapper.Contents, TypeMapper);
			if (tlWrapperFunction == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 37, $"Unable to find TL wrapper function {wrapperFunction.Name} for function {func.Name} in protocol {protocolDecl.ToFullyQualifiedName (true)}.");
			}

			var selfFunc = func.MacroReplaceType (func.Parent.ToFullyQualifiedName (), "Self", false);


			var publicMethod = ImplementOverloadFromKnownWrapper (proxyClass, picl, usedPinvokeNames, selfFunc, use, true, wrapper, swiftLibraryPath,
			                                                      tlWrapperFunction, false, MakeAssociatedTypeNamer (protocolDecl),
									      restoreDynamicSelf: protocolDecl.HasDynamicSelfInArguments);

			if (!protocolDecl.IsExistential) {
				SubstituteAssociatedTypeNamer (protocolDecl, publicMethod);
			}

			var ifaceMethod = new CSMethod (CSVisibility.None, CSMethodKind.Interface, publicMethod.Type,
						     publicMethod.Name, publicMethod.Parameters, null);
			ifaceMethod.GenericParameters.AddRange (publicMethod.GenericParameters);
			ifaceMethod.GenericConstraints.AddRange (publicMethod.GenericConstraints);

			iface.Methods.Add (ifaceMethod);

			var proxyName =  proxyClass.ToCSType ().ToString ();

			var staticRecv = ImplementVirtualMethodStaticReceiver (iface.ToCSType (), proxyName,
			                                                       delegateDecl, use, func, publicMethod, vtable.Name, homonymSuffix, protocolDecl.IsObjC, !protocolDecl.IsExistential);
			proxyClass.Methods.Add (staticRecv);
			vtableAssignments.Add (CSAssignment.Assign (String.Format ("{0}.{1}", vtableName, OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex)),
								  staticRecv.Name));
			return vtableEntryIndex + 1;
		}

		int ImplementMethodVtableForVirtualMethods (WrappingResult wrapper,
		                                            ClassDeclaration classDecl, ClassDeclaration subclassDecl,
		                                            ClassContents classContents, CSClass cl, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                            FunctionDeclaration func, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                            List<CSLine> vtableAssignments, CSUsingPackages use, string swiftLibraryPath)
		{
			var homonymSuffix = Homonyms.HomonymSuffix (func, classDecl.Members.OfType<FunctionDeclaration> (), TypeMapper);
			var subClassSwiftName = XmlToTLFunctionMapper.ToSwiftClassName (subclassDecl);

			var delegateDecl = TLFCompiler.CompileToDelegateDeclaration (func, use, null, "Del" + OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), true, CSVisibility.Public, false);
			vtable.Delegates.Add (delegateDecl);
			var field = new CSFieldDeclaration (new CSSimpleType (delegateDecl.Name.Name), OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), null, CSVisibility.Public);
			CSAttribute.MarshalAsFunctionPointer ().AttachBefore (field);
			vtable.Fields.Add (new CSLine (field));

			var superFunc = subclassDecl.AllMethodsNoCDTor ().Where (fn =>
			                                                         fn.Access == Accessibility.Internal && fn.Name == OverrideBuilder.SuperName (func) &&
			                                                         fn.MatchesSignature (func, true)).FirstOrDefault ();

			if (superFunc == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 39, $"Unable to find super function declaration matching virtual function {func.Name} in class {classDecl.ToFullyQualifiedName (true)}.");
			}

			var superWrapperFunc = FindSuperWrapper (superFunc, wrapper);

			string superMethodName = "Base" + TypeMapper.SanitizeIdentifier (func.Name);

			var superFuncWrapper = XmlToTLFunctionMapper.ToTLFunction (superWrapperFunc, wrapper.Contents, TypeMapper);
			if (superFuncWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 40, $"Unable to find wrapper for super function implementation matching virtual function {func.Name} in class {classDecl.ToFullyQualifiedName (true)}.");
			}

			CSMethod publicOverload = null;
			ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, subClassSwiftName, superFunc, use, true, wrapper,
			                                   swiftLibraryPath, superFuncWrapper, homonymSuffix, true, alternativeName: superMethodName);
			                                   
			ImplementVirtualMethod (cl, func, superMethodName, use, wrapper, ref publicOverload, swiftLibraryPath, homonymSuffix);

			var existsInParent = classDecl.VirtualMethodExistsInInheritedBoundType (func, TypeMapper) || IsImportedInherited (classDecl, publicOverload);
			if (existsInParent) {
				cl.Methods.Remove (publicOverload);
				var virtualOverload = new CSMethod (publicOverload.Visibility, CSMethodKind.Override, publicOverload.Type,
								    publicOverload.Name, publicOverload.Parameters, publicOverload.BaseOrThisCallParameters,
								    publicOverload.CallsBase, publicOverload.Body);
				cl.Methods.Add (virtualOverload);
				publicOverload = virtualOverload;
			}


			CSMethod staticRecv = ImplementVirtualMethodStaticReceiver (cl.ToCSType (), null, delegateDecl, use, func, publicOverload, vtable.Name, "", classDecl.IsObjCOrInheritsObjC (TypeMapper), false);
			cl.Methods.Add (staticRecv);
			vtableAssignments.Add (CSAssignment.Assign (String.Format ("{0}.{1}", vtableName, OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex)),
								  staticRecv.Name));

			return vtableEntryIndex + 1;
		}


		int ImplementPropertyVtableForProtocolVirtualSubscripts (WrappingResult wrapper,
									 ProtocolDeclaration protocolDecl, ProtocolContents protocolContents,
									 CSInterface iface, CSClass proxyClass, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
									 FunctionDeclaration getterFunc, int vtableEntryIndex, string vtableName, CSStruct vtable,
									 List<CSLine> vtableAssignments, CSUsingPackages use, string swiftLibraryPath)
		{
			FunctionDeclaration setterFunc = getterFunc.MatchingSetter (virtFuncs);
			bool hasSetter = setterFunc != null;

			TLFunction tlSetter = null;
			if (hasSetter) {
				FunctionDeclaration setterWrapperFunc = FindProtocolWrapperFunction (setterFunc, wrapper);
				tlSetter = XmlToTLFunctionMapper.ToTLFunction (setterWrapperFunc, wrapper.Contents, TypeMapper);
				if (tlSetter == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 41, $"Unable to find TL wrapper function for setter for subscript in protocol {protocolDecl.ToFullyQualifiedName (true)}.");
				}
			}
			var wrapperProp = ImplementProtocolSubscriptEtter (wrapper, protocolDecl, protocolContents, iface, proxyClass,
								   picl, usedPinvokeNames, virtFuncs, getterFunc, setterFunc, vtableEntryIndex,
			                                           vtableName, vtable, vtableAssignments, use,
								   false, null, swiftLibraryPath);

			if (hasSetter) {
				wrapperProp = ImplementProtocolSubscriptEtter (wrapper, protocolDecl, protocolContents, iface, proxyClass,
								       picl, usedPinvokeNames, virtFuncs, setterFunc, null, vtableEntryIndex + 1,
								       vtableName, vtable, vtableAssignments, use,
								       true, wrapperProp, swiftLibraryPath);
			}
			proxyClass.Properties.Add (wrapperProp);
			// add to interface
			return vtableEntryIndex + (hasSetter ? 2 : 1);
		}

		int ImplementPropertyVtableForVirtualSubscripts (WrappingResult wrapper,
		                                                 ClassDeclaration classDecl, ClassDeclaration subclassDecl,
		                                                 ClassContents classContents, CSClass cl, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                                 FunctionDeclaration getterFunc, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                                 List<CSLine> vtableAssignments, CSUsingPackages use, string swiftLibraryPath)
		{
			var setterFunc = getterFunc.MatchingSetter (virtFuncs);
			bool hasSetter = setterFunc != null;

			TLFunction tlSetter = null;
			if (hasSetter) {
				tlSetter = XmlToTLFunctionMapper.ToTLFunction (setterFunc, classContents, TypeMapper);
			}
			var wrapperProp = ImplementSubscriptEtter (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames, virtFuncs,
				getterFunc, tlSetter, vtableEntryIndex, vtableName, vtable, vtableAssignments, use,
				false, null, swiftLibraryPath);

			if (hasSetter) {
				wrapperProp = ImplementSubscriptEtter (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames, virtFuncs,
					setterFunc, null, vtableEntryIndex + 1, vtableName, vtable, vtableAssignments, use,
					true, wrapperProp, swiftLibraryPath);
			}
			if (wrapperProp != null)
				cl.Properties.Add (wrapperProp);

			return vtableEntryIndex + (hasSetter ? 2 : 1);
		}

		CSProperty ImplementProtocolSubscriptEtter (WrappingResult wrapper,
		                                  ProtocolDeclaration protocolDecl, ProtocolContents protocolContents,
		                                  CSInterface iface, CSClass proxyClass, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                  FunctionDeclaration etterFunc, FunctionDeclaration setter, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                  List<CSLine> vtableAssignments, CSUsingPackages use, bool isSetter, CSProperty wrapperProp, string swiftLibraryPath)
		{
			CSDelegateTypeDecl etterDelegateDecl = DefineDelegateAndAddToVtable (vtable, etterFunc, use,
			                                                                     OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), !protocolDecl.HasAssociatedTypes);
			vtable.Delegates.Add (etterDelegateDecl);

			var etterWrapperFunc = FindProtocolWrapperFunction (etterFunc, wrapper);
			if (etterWrapperFunc == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 42, $"Unable to find wrapper function for {(isSetter ? "setter" : "getter")} for subscript in {protocolDecl.ToFullyQualifiedName (true)}.");
			}

			var etterWrapper = XmlToTLFunctionMapper.ToTLFunction (etterWrapperFunc, wrapper.Contents, TypeMapper);
			if (etterWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 43, $"Unable to find TL wrapper function for {(isSetter ? "setter" : "getter")} for subscript in {protocolDecl.ToFullyQualifiedName (true)}.");
			}

			var piEtterName = PIMethodName (protocolDecl.ToFullyQualifiedName (true), etterWrapper.Name, isSetter ? PropertyType.Setter : PropertyType.Getter);
			piEtterName = Uniqueify (piEtterName, usedPinvokeNames);
			usedPinvokeNames.Add (piEtterName);

			string piEtterRef = picl.Name.Name + "." + piEtterName;
			var piGetter = TLFCompiler.CompileMethod (etterWrapperFunc, use,
				PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
				etterWrapper.MangledName, piEtterName, true, true, false);
			picl.Methods.Add (piGetter);

			if (wrapperProp == null) {
				wrapperProp = TLFCompiler.CompileProperty (use, "this", etterFunc,
					setter, CSMethodKind.None);
				if (protocolDecl.HasAssociatedTypes) {
					SubstituteAssociatedTypeNamer (protocolDecl, wrapperProp);
					SubstituteAssociatedTypeNamer (protocolDecl, wrapperProp.IndexerParameters);
				}

				CSProperty ifaceProp = new CSProperty (wrapperProp.PropType, CSMethodKind.None,
					CSVisibility.None, new CSCodeBlock (), CSVisibility.None, setter != null ? new CSCodeBlock () : null,
					wrapperProp.IndexerParameters
				);
				iface.Properties.Add (ifaceProp);
			}

			var usedIds = new List<string> ();
			usedIds.Add ("this");
			usedIds.AddRange (wrapperProp.IndexerParameters.Select (p => p.Name.Name));

			var marshal = new MarshalEngine (use, usedIds, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			if (protocolDecl.HasAssociatedTypes)
				marshal.GenericReferenceNamer = MakeAssociatedTypeNamer (protocolDecl);

			var ifTest = kInterfaceImpl != CSConstant.Null;
			var ifBlock = new CSCodeBlock ();
			var elseBlock = new CSCodeBlock ();
			CSCodeBlock target = null;
			var indexerExpr = BuildIndexer (kInterfaceImpl, wrapperProp, proxyClass.Name.Name);

			if (isSetter) {
				target = wrapperProp.Setter;
				ifBlock.Add (CSAssignment.Assign (indexerExpr, new CSIdentifier ("value")));

				CSParameterList pl = new CSParameterList ();
				pl.AddRange (wrapperProp.IndexerParameters);
				pl.Insert (0, new CSParameter (wrapperProp.PropType, new CSIdentifier ("value")));
				elseBlock.AddRange (marshal.MarshalFunctionCall (etterWrapperFunc, false, piEtterRef, pl,
				                                                          etterFunc, null, CSSimpleType.Void, etterFunc.ParameterLists [0] [0].TypeSpec, iface.ToCSType (), false, wrapper,
											  etterFunc.HasThrows));
			} else {
				ifBlock.Add (CSReturn.ReturnLine (indexerExpr));
				target = wrapperProp.Getter;
				elseBlock.AddRange (marshal.MarshalFunctionCall (etterWrapperFunc, false, piEtterRef, wrapperProp.IndexerParameters,
				                                                          etterFunc, etterFunc.ReturnTypeSpec, wrapperProp.PropType,
				                                                          etterFunc.ParameterLists [0] [0].TypeSpec,
				                                                          iface.ToCSType (), false, wrapper,
											  etterFunc.HasThrows));
			}

			target.Add (new CSIfElse (ifTest, ifBlock, elseBlock));

			var renamer = protocolDecl.HasAssociatedTypes ? MakeAssociatedTypeNamer (protocolDecl) : null;
			var proxyName = proxyClass.ToCSType ().ToString ();

			var recv = ImplementVirtualSubscriptStaticReceiver (new CSSimpleType (iface.Name.Name),
			                                                    proxyName, etterDelegateDecl, use,
			                                                    etterFunc, wrapperProp, null, vtable.Name, protocolDecl.IsObjC,
									    renamer, protocolDecl.HasAssociatedTypes);
			proxyClass.Methods.Add (recv);

			vtableAssignments.Add (CSAssignment.Assign (String.Format ("{0}.{1}",
										 vtableName, OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex)), recv.Name));
			return wrapperProp;
		}

		CSBaseExpression BuildIndexer (CSIdentifier identifier, CSProperty prop, string errorContext)
		{
			var parameters = ParametersToParameterExpressions ($"build index expression for indexer in {errorContext}", prop.IndexerParameters).ToArray ();
			var indexer = new CSIndexExpression (identifier, false, parameters);
			return indexer;
		}

		IEnumerable<CSBaseExpression> ParametersToParameterExpressions (string errorContext, CSParameterList pl)
		{
			return pl.Select (parm => {
				switch (parm.ParameterKind) {
				case CSParameterKind.None:
					return (CSBaseExpression)parm.Name;
				case CSParameterKind.Out:
					return (CSBaseExpression)new CSUnaryExpression (CSUnaryOperator.Out, parm.Name);
				case CSParameterKind.Ref:
					return (CSBaseExpression)new CSUnaryExpression (CSUnaryOperator.Ref, parm.Name);
				default:
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 84,
						$"Unable to {errorContext} with a {parm.ParameterKind} parameter type on parameter {parm.Name}");
				}
			});
		}

		CSProperty ImplementSubscriptEtter (WrappingResult wrapper,
		                                  ClassDeclaration classDecl, ClassDeclaration subclassDecl,
		                                  ClassContents classContents, CSClass cl, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                  FunctionDeclaration etterFunc, TLFunction tlSetter, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                  List<CSLine> vtableAssignments, CSUsingPackages use, bool isSetter, CSProperty wrapperProp, string swiftLibraryPath)
		{
			var setErrorType = isSetter ? "setter" : "getter";
			SwiftClassName subClassSwiftName = XmlToTLFunctionMapper.ToSwiftClassName (subclassDecl);
			TLFunction tlEtter = XmlToTLFunctionMapper.ToTLFunction (etterFunc, classContents, TypeMapper);
			if (tlEtter == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 44, $"Unable to find wrapper function for {setErrorType} for subscript in {classDecl.ToFullyQualifiedName (true)} ");

			var etterDelegateDecl = DefineDelegateAndAddToVtable (vtable, etterFunc, use,
			                                                      OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), false);
			vtable.Delegates.Add (etterDelegateDecl);


			TLFunction etterWrapper = null;
			etterWrapper = FindSubscriptFoo (etterFunc, classContents.Subscripts, isSetter ? PropertyType.Setter : PropertyType.Getter);
			if (etterWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 45, $"Unable to find wrapper function for {setErrorType} for subscript in {classDecl.ToFullyQualifiedName (true)}.");
			}
			var etterWrapperFunc = FindEquivalentFunctionDeclarationForWrapperFunction (etterWrapper, TypeMapper, wrapper) ?? etterFunc;
			string piEtterName = PIMethodName (tlEtter.Class.ClassName, etterWrapper.Name, isSetter ? PropertyType.Setter : PropertyType.Getter);
			piEtterName = Uniqueify (piEtterName, usedPinvokeNames);
			usedPinvokeNames.Add (piEtterName);

			var piGetter = TLFCompiler.CompileMethod (etterWrapperFunc, use,
								  PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
								  etterWrapper.MangledName, piEtterName, true, true, false); picl.Methods.Add (piGetter);

			string xxsuperEtterName = OverrideBuilder.SuperSubscriptName (etterFunc);
			var superEtterFunc = subclassDecl.AllMethodsNoCDTor ().Where (fn =>
				fn.Access == Accessibility.Internal &&
				fn.Name == xxsuperEtterName &&
				fn.MatchesSignature (etterFunc, true)).FirstOrDefault ();

			if (superEtterFunc == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 46, $"Unable to find super {setErrorType} function for subscript in class {classDecl.ToFullyQualifiedName (true)}.");
			}

			var superEtterWrapperFunc = FindSuperWrapper (superEtterFunc, wrapper);
			string superEtterName = "Base" + TypeMapper.SanitizeIdentifier (tlEtter.Name.Name);

			var superEtterFuncWrapper = XmlToTLFunctionMapper.ToTLFunction (superEtterWrapperFunc, wrapper.Contents, TypeMapper);
			if (superEtterFuncWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 47, $"Unable to find wrapper for super function implementation matching virtual property {etterFunc.Name} in class {classDecl.ToFullyQualifiedName (true)}.");
			}

			var isAnyProtocolList = FuncArgsOrReturnAreProtocolListTypes (etterFunc);
			CSMethod protocolMethod = null;
			CSParameterList callingArgList = null;

			ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, subClassSwiftName, superEtterFunc, use, true, wrapper, swiftLibraryPath,
				superEtterFuncWrapper, "", true, alternativeName: superEtterName);
			var superEtterMethod = cl.Methods.Last ();

			if (isAnyProtocolList) {
				var methodName = $"{(isSetter ? "Set" : "Get")}Subscript";
				protocolMethod = TLFCompiler.CompileMethod (etterFunc, use, null, null, methodName, false, false, false);
				callingArgList = protocolMethod.Parameters;

			} else {
				if (wrapperProp == null) {
					// can't be null
					var parentSubscript = classDecl.AllSubscripts ().FirstOrDefault (sub => sub.Getter == etterFunc);
					var setter = parentSubscript.Setter; // can be null
					wrapperProp = TLFCompiler.CompileProperty (use, "this", etterFunc, setter, CSMethodKind.Virtual);
				}
				callingArgList = wrapperProp.IndexerParameters;
			}


			var superEtterArgs = ParametersToParameterExpressions ($"building a subscript in class {classDecl.Name}", callingArgList).ToList ();
			if (isSetter && !isAnyProtocolList)
				superEtterArgs.Insert (0, new CSIdentifier ("value"));

			if (isAnyProtocolList) {
				var protoArgs = new StringBuilder ();
				foreach (var genType in superEtterMethod.GenericParameters) {
					if (protoArgs.Length > 0)
						protoArgs.Append (", ");
					protoArgs.Append (genType.Name.Name);
				}

				if (isSetter) {
					protocolMethod.Body.Add (CSFunctionCall.FunctionCallLine (superEtterName, false, superEtterArgs.ToArray ()));
				} else {
					protocolMethod.Body.Add (CSReturn.ReturnLine (new CSFunctionCall (superEtterName, false, superEtterArgs.ToArray ())));
				}
				cl.Methods.Add (protocolMethod);
			} else {
				if (isSetter) {
					wrapperProp.Setter.Add (CSFunctionCall.FunctionCallLine (superEtterName, false, superEtterArgs.ToArray ()));
				} else {
					wrapperProp.Getter.Add (CSReturn.ReturnLine (new CSFunctionCall (superEtterName, false, superEtterArgs.ToArray ())));
				}
			}

			var recv = ImplementVirtualSubscriptStaticReceiver (cl.ToCSType (), null, etterDelegateDecl, use, etterFunc,wrapperProp, protocolMethod, vtable.Name, classDecl.IsObjCOrInheritsObjC (TypeMapper));
			cl.Methods.Add (recv);

			vtableAssignments.Add (CSAssignment.Assign (String.Format ("{0}.{1}",
										 vtableName, OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex)), recv.Name));
			return wrapperProp;
		}

		static bool FuncArgsOrReturnAreProtocolListTypes (FunctionDeclaration funcDecl)
		{
			foreach (var parameter in funcDecl.ParameterLists.Last ()) {
				if (parameter.TypeSpec is ProtocolListTypeSpec)
					return true;
			}
			return funcDecl.ReturnTypeSpec is ProtocolListTypeSpec;
		}
		    

		int ImplementPropertyVtableForProtocolProperties (WrappingResult wrapper,
		                                                  ProtocolDeclaration protocolDecl, ProtocolContents protocolContents, CSInterface iface,
		                                                  CSClass proxyClass, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                                  FunctionDeclaration getterFunc, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                                  List<CSLine> vtableAssignments, CSUsingPackages use, string swiftLibraryPath)
		{
			var setterFunc = virtFuncs.Where (f => f.IsSetter && f.PropertyName == getterFunc.PropertyName).FirstOrDefault ();
			bool hasSetter = setterFunc != null;

			var wrapperProp = ImplementProtocolPropertyEtter (wrapper, protocolDecl, protocolContents, iface, proxyClass, picl, usedPinvokeNames, virtFuncs,
			                                          getterFunc, setterFunc, vtableEntryIndex, vtableName, vtable, vtableAssignments, use, false, protocolDecl.IsObjC, null, swiftLibraryPath);

			if (hasSetter) {
				wrapperProp = ImplementProtocolPropertyEtter (wrapper, protocolDecl, protocolContents, iface, proxyClass, picl, usedPinvokeNames, virtFuncs,
				                                      setterFunc, null, vtableEntryIndex + 1, vtableName, vtable, vtableAssignments, use, true, protocolDecl.IsObjC, wrapperProp, swiftLibraryPath);
			}

			proxyClass.Properties.Add (wrapperProp);

			return vtableEntryIndex + (hasSetter ? 2 : 1);
		}


		int ImplementPropertyVtableForVirtualProperties (WrappingResult wrapper,
		                                                 ClassDeclaration classDecl, ClassDeclaration subclassDecl,
		                                                 ClassContents classContents, CSClass cl, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                                 FunctionDeclaration getterFunc, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                                 List<CSLine> vtableAssignments, CSUsingPackages use, string swiftLibraryPath)
		{
			var setterFunc = virtFuncs.Where (f => f.IsSetter && f.PropertyName == getterFunc.PropertyName).FirstOrDefault ();
			bool hasSetter = setterFunc != null;

			TLFunction tlSetter = null;
			if (hasSetter) {
				tlSetter = XmlToTLFunctionMapper.ToTLFunction (setterFunc, classContents, TypeMapper);
			}
			var wrapperProp = ImplementPropertyEtter (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames, virtFuncs,
			                                          getterFunc, tlSetter, vtableEntryIndex, vtableName, vtable, vtableAssignments, use,
			                                          false, null, swiftLibraryPath, null);

			if (hasSetter) {
				wrapperProp = ImplementPropertyEtter (wrapper, classDecl, subclassDecl, classContents, cl, picl, usedPinvokeNames, virtFuncs,
				                                      setterFunc, null, vtableEntryIndex + 1, vtableName, vtable, vtableAssignments, use,
				                                      true, wrapperProp, swiftLibraryPath);
			}
			if (wrapperProp != null)
				cl.Properties.Add (wrapperProp);

			return vtableEntryIndex + (hasSetter ? 2 : 1);
		}

		CSProperty ImplementProtocolPropertyEtter (WrappingResult wrapper,
		                                 ProtocolDeclaration protocolDecl, ProtocolContents protocolContents,
		                                 CSInterface iface, CSClass proxyClass, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                 FunctionDeclaration etterFunc, FunctionDeclaration setter, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                 List<CSLine> vtableAssignments, CSUsingPackages use, bool isSetter, bool isObjC, CSProperty wrapperProp, string swiftLibraryPath)
		{
			var etterDelegateDecl = DefineDelegateAndAddToVtable (vtable, etterFunc, use,
			                                                      OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), protocolDecl.IsExistential);
			vtable.Delegates.Add (etterDelegateDecl);

			var etterWrapperFunc = FindProtocolWrapperFunction (etterFunc, wrapper);
			if (etterWrapperFunc == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 48, $"Unable to find wrapper function for {(isSetter ? "setter" : "getter")} for property {etterFunc.PropertyName} in {protocolDecl.ToFullyQualifiedName (true)}.");
			}

			var etterWrapper = XmlToTLFunctionMapper.ToTLFunction (etterWrapperFunc, wrapper.Contents, TypeMapper);
			if (etterWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 49, $"Unable to find TL wrapper function for {(isSetter ? "setter" : "getter")} for property {etterFunc.PropertyName} in {protocolDecl.ToFullyQualifiedName (true)}.");
			}

			var piEtterName = PIMethodName (protocolDecl.ToFullyQualifiedName (true), etterWrapper.Name, isSetter ? PropertyType.Setter : PropertyType.Getter);
			piEtterName = Uniqueify (piEtterName, usedPinvokeNames);
			usedPinvokeNames.Add (piEtterName);

			string piEtterRef = picl.Name + "." + piEtterName;
			var piGetter = TLFCompiler.CompileMethod (etterWrapperFunc, use,
								  PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
								  etterWrapper.MangledName, piEtterName, true, true, false);
			picl.Methods.Add (piGetter);
			string propName = TypeMapper.SanitizeIdentifier (etterFunc.PropertyName);

			if (wrapperProp == null) {
				var selfFunc = etterFunc.MacroReplaceType (etterFunc.Parent.ToFullyQualifiedName (), "Self", false);

				wrapperProp = TLFCompiler.CompileProperty (use, propName, selfFunc, setter, CSMethodKind.None);
				if (protocolDecl.HasAssociatedTypes) {
					SubstituteAssociatedTypeNamer (protocolDecl, wrapperProp);
				}

				var ifaceProp = new CSProperty (wrapperProp.PropType, CSMethodKind.None,
				                              new CSIdentifier (propName),
				                              CSVisibility.None, new CSCodeBlock (), CSVisibility.None,
				                              setter != null ? new CSCodeBlock () : null);
				iface.Properties.Add (ifaceProp);
			}

			var usedIds = new List<string> { propName };
			var marshal = new MarshalEngine (use, usedIds, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			marshal.ProtocolInterfaceType = iface.ToCSType ();
			if (protocolDecl.HasAssociatedTypes)
				marshal.GenericReferenceNamer = MakeAssociatedTypeNamer (protocolDecl);

			var ifTest = kInterfaceImpl != CSConstant.Null;
			var ifBlock = new CSCodeBlock ();
			var elseBlock = new CSCodeBlock ();
			CSCodeBlock target;

			if (isSetter) {
				ifBlock.Add (CSAssignment.Assign (kInterfaceImpl.Dot (wrapperProp.Name), new CSIdentifier ("value")));
				target = wrapperProp.Setter;
				var p = new CSParameter (wrapperProp.PropType, new CSIdentifier ("value"));
				var pl = new CSParameterList (p);
				elseBlock.AddRange (marshal.MarshalFunctionCall (etterWrapperFunc, false, piEtterRef, pl,
											  etterFunc, null, CSSimpleType.Void, etterFunc.ParameterLists [0] [0].TypeSpec, proxyClass.ToCSType (), false, wrapper, etterFunc.HasThrows,
											  restoreDynamicSelf: !protocolDecl.IsExistential));
			} else {
				ifBlock.Add (CSReturn.ReturnLine (kInterfaceImpl.Dot (wrapperProp.Name)));
				target = wrapperProp.Getter;
				elseBlock.AddRange (marshal.MarshalFunctionCall (etterWrapperFunc, false, piEtterRef, new CSParameterList (),
											  etterFunc, etterFunc.ReturnTypeSpec, wrapperProp.PropType, etterFunc.ParameterLists [0] [0].TypeSpec, proxyClass.ToCSType (), false, wrapper, etterFunc.HasThrows,
											  restoreDynamicSelf: !protocolDecl.IsExistential));
			}

			var ifElse = new CSIfElse (ifTest, ifBlock, elseBlock);
			target.Add (ifElse);

			var renamer = protocolDecl.HasAssociatedTypes ? MakeAssociatedTypeNamer (protocolDecl) : null;

			var recvr = ImplementVirtualPropertyStaticReceiver (iface.ToCSType (),
			                                                    proxyClass.ToCSType ().ToString (), etterDelegateDecl, use,
			                                                    etterFunc, wrapperProp, null, vtable.Name, isObjC, !protocolDecl.IsExistential,
									    renamer);
			proxyClass.Methods.Add (recvr);

			vtableAssignments.Add (CSAssignment.Assign (String.Format ("{0}.{1}",
										 vtableName, OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex)), recvr.Name));
			return wrapperProp;
		}

		CSProperty ImplementPropertyEtter (WrappingResult wrapper,
		                                 ClassDeclaration classDecl, ClassDeclaration subclassDecl,
		                                 ClassContents classContents, CSClass cl, CSClass picl, List<string> usedPinvokeNames, List<FunctionDeclaration> virtFuncs,
		                                 FunctionDeclaration etterFunc, TLFunction tlSetter, int vtableEntryIndex, string vtableName, CSStruct vtable,
		                                 List<CSLine> vtableAssignments, CSUsingPackages use, bool isSetter, CSProperty wrapperProp, string swiftLibraryPath,
						 Func<int, int, string> genericRenamer = null)
		{
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (subclassDecl);
			var etterDelegateDecl = DefineDelegateAndAddToVtable (vtable, etterFunc, use,
			                                                      OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex), false);
			vtable.Delegates.Add (etterDelegateDecl);

			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
			var etterWrapperFunc = finder.FindWrapperForMethod (classDecl ?? subclassDecl, etterFunc, isSetter ? PropertyType.Setter : PropertyType.Getter);

			var etterWrapper = etterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (etterWrapperFunc, TypeMapper, wrapper) : null;

			if (etterWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 50, $"Unable to find wrapper function for {(isSetter ? "setter" : "getter")} for property {etterFunc.Name} in {classDecl.ToFullyQualifiedName (true)}.");
			}

			var piEtterName = PIMethodName (swiftClassName, etterWrapper.Name, isSetter ? PropertyType.Setter : PropertyType.Getter);
			piEtterName = Uniqueify (piEtterName, usedPinvokeNames);
			usedPinvokeNames.Add (piEtterName);

			var piGetter = TLFCompiler.CompileMethod (etterWrapperFunc, use,
								  PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
								  etterWrapper.MangledName, piEtterName, true, true, false);
			picl.Methods.Add (piGetter);

			var superEtterFunc = subclassDecl.AllMethodsNoCDTor ().Where (fn =>
				fn.Access == Accessibility.Internal && fn.IsProperty &&
				fn.PropertyName == OverrideBuilder.SuperPropName (etterFunc) &&
				fn.MatchesSignature (etterFunc, true)).FirstOrDefault ();

			if (superEtterFunc == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 51, $"Unable to find super {(isSetter ? "setter" : "getter")} function for property {etterFunc.PropertyName} in class {classDecl.ToFullyQualifiedName (true)}.");
			}

			var superEtterWrapperFunc = FindSuperWrapper (superEtterFunc, wrapper);
			string superEtterName = "Base" + TypeMapper.SanitizeIdentifier (etterFunc.PropertyName);

			var superEtterFuncWrapper = XmlToTLFunctionMapper.ToTLFunction (superEtterWrapperFunc, wrapper.Contents, TypeMapper);
			if (superEtterFuncWrapper == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 52, $"Unable to find wrapper for super function implementation matching virtual property {etterFunc.Name} in class {classDecl.ToFullyQualifiedName (true)}.");
			}

			var propType = isSetter ? etterFunc.PropertyType : etterFunc.ReturnTypeSpec;
			var returnIsProtocolList = propType is ProtocolListTypeSpec;

			if (wrapperProp == null && !returnIsProtocolList) {
				var propName = TypeMapper.SanitizeIdentifier (etterFunc.PropertyName);
				// can't be null
				var prop = classDecl.AllProperties ().FirstOrDefault (p => p.Name == etterFunc.PropertyName);
				var setter = prop.GetSetter (); // may be null

				var selfFunc = etterFunc.MacroReplaceType (etterFunc.Parent.ToFullyQualifiedName (), "Self", false);

				wrapperProp = TLFCompiler.CompileProperty (use, propName, selfFunc, setter, CSMethodKind.Virtual);

				var existsInParent = classDecl.VirtualMethodExistsInInheritedBoundType (etterFunc, TypeMapper) || IsImportedInherited (classDecl, wrapperProp);
				if (existsInParent) {
					// recast as override
					wrapperProp = new CSProperty (wrapperProp.PropType, CSMethodKind.Override, wrapperProp.Name, wrapperProp.GetterVisibility, wrapperProp.Getter,
								      wrapperProp.SetterVisibility, wrapperProp.Setter);
				}
			}

			ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, swiftClassName, superEtterFunc, use, true, wrapper, swiftLibraryPath,
				superEtterFuncWrapper, homonymSuffix:"", true, alternativeName: superEtterName, genericRenamer: genericRenamer);
			var propertyImplMethod = cl.Methods.Last ();
			CSMethod protoListMethod = null;
			if (returnIsProtocolList) {
				var funcPrefix = isSetter ? "Set" : "Get";
				var funcName = $"{funcPrefix}{TypeMapper.SanitizeIdentifier (etterFunc.PropertyName)}";
				protoListMethod = new CSMethod (CSVisibility.Public, CSMethodKind.Virtual, propertyImplMethod.Type, new CSIdentifier (funcName),
					propertyImplMethod.Parameters, new CSCodeBlock ());
				protoListMethod.GenericParameters.AddRange (propertyImplMethod.GenericParameters);
				protoListMethod.GenericConstraints.AddRange (propertyImplMethod.GenericConstraints);

				if (isSetter) {
					protoListMethod.Body.Add (CSFunctionCall.FunctionCallLine (superEtterName, false, protoListMethod.Parameters[0].Name));
				} else {
					protoListMethod.Body.Add (CSReturn.ReturnLine (new CSFunctionCall (superEtterName, false)));
				}
				cl.Methods.Add (protoListMethod);
			} else {
				if (isSetter) {
					wrapperProp.Setter.Add (CSFunctionCall.FunctionCallLine (superEtterName, false, new CSIdentifier ("value")));
				} else {
					wrapperProp.Getter.Add (CSReturn.ReturnLine (new CSFunctionCall (superEtterName, false)));
				}
			}

			var recvr = ImplementVirtualPropertyStaticReceiver (cl.ToCSType (), null, etterDelegateDecl, use, etterFunc,
			                                                    wrapperProp, protoListMethod, vtable.Name, classDecl.IsObjC, false);
			cl.Methods.Add (recvr);

			vtableAssignments.Add (CSAssignment.Assign (String.Format ("{0}.{1}",
										 vtableName, OverrideBuilder.VTableEntryIdentifier (vtableEntryIndex)), recvr.Name));
			return wrapperProp;
		}

		CSDelegateTypeDecl DefineDelegateAndAddToVtable (CSStruct vtable, FunctionDeclaration func, CSUsingPackages use, string entryID, bool isProtocol)
		{
			var decl = TLFCompiler.CompileToDelegateDeclaration (func, use, null, "Del" + entryID,
			                                                     true, CSVisibility.Public, isProtocol);
			var field = new CSFieldDeclaration (new CSSimpleType (decl.Name.Name), entryID, null, CSVisibility.Public);
			CSAttribute.MarshalAsFunctionPointer ().AttachBefore (field);
			vtable.Fields.Add (new CSLine (field));
			return decl;
		}

		void ImplementVTableInitializer (CSClass cl, CSClass picl, List<string> usedPinvokeNames, ClassDeclaration classDecl,
		                                 List<CSLine> vtableAssignments, WrappingResult wrapper, string vtableName,
		                                 string swiftLibraryPath)
		{
			if (vtableAssignments.Count == 0)
				return;
			var func = wrapper.Contents.Functions.MethodsWithName (OverrideBuilder.VtableSetterName (classDecl)).FirstOrDefault ();
			if (func == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 53, $"Unable to find wrapper function for vtable setter for {classDecl.ToFullyQualifiedName (true)}");

			var protocol = classDecl as ProtocolDeclaration;
			var hasRealGenericArguments = !(protocol != null && !protocol.HasAssociatedTypes);

			var setterName = Uniqueify ("SwiftXamSetVtable", usedPinvokeNames);
			usedPinvokeNames.Add (setterName);

			var swiftSetter = new CSMethod (CSVisibility.Internal, CSMethodKind.StaticExtern, CSSimpleType.Void,
			                                new CSIdentifier (setterName), new CSParameterList (), null);
			CSAttribute.DllImport (PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath), func.MangledName.Substring (1)).AttachBefore (swiftSetter);
			picl.Methods.Add (swiftSetter);

			swiftSetter.Parameters.Add (new CSParameter (CSSimpleType.IntPtr, new CSIdentifier ("vt"), CSParameterKind.None));
			if (hasRealGenericArguments) {
				var start = (protocol != null && protocol.HasDynamicSelf ? 1 : 0);
				for (var i = start; i < cl.GenericParams.Count; i++) {
					swiftSetter.Parameters.Add (new CSParameter (new CSSimpleType ("SwiftMetatype"), new CSIdentifier ($"t{i}")));
				}
			}

			var setVTable = new CSMethod (CSVisibility.None, CSMethodKind.Static,
						   CSSimpleType.Void, new CSIdentifier ("XamSetVTable"), new CSParameterList (), new CSCodeBlock ());
			setVTable.Body.AddRange (vtableAssignments);

			// unsafe {
			//   vtData = stackalloc byte[Marshal.SizeOf(Monty_xam_vtable)];
			//   vtPtr = new IntPtr(vtPtrData);
			// this repeats n time.
			//   Marshal.WriteIntPtr(vtPtr + (IntPtr.Size * n), Marshal.GetFunctionPointerForDelegate(vtableName.funcn));
			//   Pinvokes.SwiftXamSetVTable(vPtr [, StructMarshal.Marsahler.Metatypeof(gen0), StructMarshal.Marsahler.Metatypeof(gen0)...);
			// }

			var vtName = new CSIdentifier (vtableName);
			var unsafeBlock = new CSUnsafeCodeBlock (null);
			var vtData = new CSIdentifier ("vtData");
			var vtableSize = new CSFunctionCall ("Marshal.SizeOf", false, vtName);
			var vtDataLine = CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, vtData, CSArray1D.New (CSSimpleType.Byte, true, vtableSize));
			unsafeBlock.Add (vtDataLine);

			var vtPtr = new CSIdentifier ("vtPtr");
			var varLine = CSVariableDeclaration.VarLine (
				CSSimpleType.IntPtr, vtPtr,
				new CSFunctionCall ("IntPtr", true, vtData));
			unsafeBlock.Add (varLine);

			for (int i = 0; i < vtableAssignments.Count (); i++) {
				unsafeBlock.Add (CallToWriteIntPtr (vtPtr, i, vtableAssignments [i]));
			}

			var args = new List<CSBaseExpression> ();
			args.Add (vtPtr);
			if (hasRealGenericArguments) {
				var start = (protocol != null && protocol.HasDynamicSelf ? 1 : 0);
				for (var i = start; i < cl.GenericParams.Count; i++) {
					var p = cl.GenericParams [i];
					args.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, new CSSimpleType (p.Name.Name).Typeof ()));
				}
			}

			unsafeBlock.Add (CSFunctionCall.FunctionCallLine (String.Format ("{0}.{1}",
										    picl.Name.Name, "SwiftXamSetVtable"), false, args.ToArray ()));

			setVTable.Body.Add (unsafeBlock);

			cl.Methods.Add (setVTable);
		}

		static CSLine CallToWriteIntPtr (CSIdentifier vtPtrName, int index, CSLine assignLine)
		{
			var assign = assignLine.Contents as CSAssignment;
			if ((object)assign == null)
				throw new ArgumentException ($"Expecting an Assignment, but got {assignLine.Contents.GetType ()}", nameof (assignLine));
			var vtEl = assign.Target;
			var getDel = new CSFunctionCall ("Marshal.GetFunctionPointerForDelegate", false, vtEl);
			var ptrExp = index == 0 ? vtPtrName : vtPtrName + new CSParenthesisExpression (CSConstant.Val (index) * new CSIdentifier ("IntPtr.Size"));
			return CSFunctionCall.FunctionCallLine ("Marshal.WriteIntPtr", false, ptrExp, getDel);
		}

		CSLine CallToSetVTable ()
		{
			return CSFunctionCall.FunctionCallLine (new CSIdentifier ("XamSetVTable"), false);
		}

		CSMethod ImplementVirtualSubscriptStaticReceiver (CSType thisType, string csProxyName, CSDelegateTypeDecl delType, CSUsingPackages use,
		                                                FunctionDeclaration funcDecl, CSProperty prop, CSMethod protoListMethod, CSIdentifier vtableName, bool isObjC,
								Func<int, int, string> genericRenamer = null, bool hasAssociatedTypes = false)
		{
			var returnType = funcDecl.IsSubscriptGetter ? delType.Type : CSSimpleType.Void;

			CSParameterList pl = delType.Parameters;
			var usedIDs = new List<string> ();
			usedIDs.AddRange (pl.Select (p => p.Name.Name));
			CSMethod recvr = null;
			var body = new CSCodeBlock ();
			string recvrName = null;

			if (protoListMethod != null) {
				body.Add (CSFunctionCall.FunctionCallLine ("throw new NotImplementedException", false, CSConstant.Val ($"In Subscript method {protoListMethod.Name.Name} protocol list type is not supported yet")));
				recvrName = "xamVtable_recv_" + (funcDecl.IsGetter ? "get_" : "set_") + protoListMethod.Name.Name;
			} else {
				var marshaler = new MarshalEngineCSafeSwiftToCSharp (use, usedIDs, TypeMapper);
				marshaler.GenericRenamer = genericRenamer;

				var bodyContents = marshaler.MarshalFromLambdaReceiverToCSFunc (thisType, csProxyName, pl, funcDecl,
												funcDecl.IsSubscriptGetter ? prop.PropType : CSSimpleType.Void, prop.IndexerParameters, null, isObjC, hasAssociatedTypes);
				body.AddRange (bodyContents);
				recvrName = "xamVtable_recv_" + (funcDecl.IsSubscriptGetter ? "index_get_" : "index_set_") + prop.Name.Name;
			}

			recvr = new CSMethod (CSVisibility.None, CSMethodKind.Static, returnType,
					new CSIdentifier (recvrName),
					pl, body);

			use.AddIfNotPresent (typeof (Xamarin.iOS.MonoPInvokeCallbackAttribute), kMobilePlatforms);
			var args = new CSArgumentList ();
			args.Add (new CSFunctionCall ("typeof", false, new CSIdentifier (vtableName.Name + "." + delType.Name.Name)));
			var attr = CSAttribute.FromAttr (typeof (Xamarin.iOS.MonoPInvokeCallbackAttribute), args, true);
			CSConditionalCompilation.ProtectWithIfEndif (kMobilePlatforms, attr);
			attr.AttachBefore (recvr);
			return recvr;
		}


		CSMethod ImplementVirtualPropertyStaticReceiver (CSType thisType, string csProxyName, CSDelegateTypeDecl delType,
		                                               CSUsingPackages use, FunctionDeclaration funcDecl, CSProperty prop, CSMethod protoListMethod,
							       CSIdentifier vtableName, bool isObjC, bool hasAssociatedTypes, Func<int, int, string> genericRenamer = null)
		{
			var returnType = funcDecl.IsGetter ? delType.Type : CSSimpleType.Void;
			CSParameterList pl = delType.Parameters;

			var usedIDs = new List<string> ();
			usedIDs.AddRange (pl.Select (p => p.Name.Name));
			CSMethod recvr = null;
			var body = new CSCodeBlock ();
			string recvrName = null;

			if (protoListMethod != null) {
				body.Add (CSFunctionCall.FunctionCallLine ("throw new NotImplementedException", false, CSConstant.Val ($"Property method {protoListMethod.Name.Name} protocol list type is not supported yet")));
				recvrName = "xamVtable_recv_" + (funcDecl.IsGetter ? "get_" : "set_") + protoListMethod.Name.Name;
			} else {
				var marshaler = new MarshalEngineCSafeSwiftToCSharp (use, usedIDs, TypeMapper);
				marshaler.GenericRenamer = genericRenamer;

				var bodyContents = marshaler.MarshalFromLambdaReceiverToCSProp (prop, thisType, csProxyName,
												delType.Parameters,
												funcDecl, prop.PropType, isObjC, hasAssociatedTypes);
				body.AddRange (bodyContents);
				recvrName = "xamVtable_recv_" + (funcDecl.IsGetter ? "get_" : "set_") + prop.Name.Name;
			}

			recvr = new CSMethod (CSVisibility.None, CSMethodKind.Static, returnType, new CSIdentifier (recvrName), pl, body);

			use.AddIfNotPresent ("ObjCRuntime", kMobilePlatforms);
			var args = new CSArgumentList ();
			args.Add (new CSFunctionCall ("typeof", false, new CSIdentifier (vtableName.Name + "." + delType.Name.Name)));
			var attr = CSAttribute.FromAttr (typeof (Xamarin.iOS.MonoPInvokeCallbackAttribute), args, true);
			CSConditionalCompilation.ProtectWithIfEndif (kMobilePlatforms, attr);
			attr.AttachBefore (recvr);
			return recvr;
		}


		CSMethod ImplementVirtualMethodStaticReceiver (CSType thisType, string csProxyName, CSDelegateTypeDecl delType, CSUsingPackages use,
		                                               FunctionDeclaration funcDecl, CSMethod publicMethod, CSIdentifier vtableName,
		                                               string homonymSuffix, bool isObjC, bool hasAssociatedTypes)
		{
			var pl = delType.Parameters;
			var usedIDs = new List<string> ();
			usedIDs.AddRange (pl.Select (p => p.Name.Name));


			var marshal = new MarshalEngineCSafeSwiftToCSharp (use, usedIDs, TypeMapper);

			var bodyContents = marshal.MarshalFromLambdaReceiverToCSFunc (thisType, csProxyName, pl, funcDecl, publicMethod.Type,
			                                                              publicMethod.Parameters, publicMethod.Name.Name, isObjC, hasAssociatedTypes);

			var body = new CSCodeBlock (bodyContents);

			var recvr = new CSMethod (CSVisibility.None, CSMethodKind.Static, delType.Type,
			                          new CSIdentifier ("xamVtable_recv_" + publicMethod.Name.Name + homonymSuffix),
										      pl, body);

			use.AddIfNotPresent ("ObjCRuntime", kMobilePlatforms);
			var args = new CSArgumentList ();
			args.Add (new CSFunctionCall ("typeof", false, new CSIdentifier (vtableName.Name + "." + delType.Name.Name)));
			var attr = CSAttribute.FromAttr (typeof (Xamarin.iOS.MonoPInvokeCallbackAttribute), args, true);
			CSConditionalCompilation.ProtectWithIfEndif (kMobilePlatforms, attr);
			attr.AttachBefore (recvr);
			return recvr;
		}


		CSClass CompileExtensions (ExtensionDeclaration extension, int index, ModuleInventory modInventory, CSUsingPackages use,
					   WrappingResult wrapper, string swiftLibraryPath, List<CSClass> pinvokes, ErrorHandling errors)
		{
			var moduleContents = modInventory.Values.Where (mod => mod.Name.Name == extension.Module.Name).FirstOrDefault ();
			if (moduleContents == null)
				throw ErrorHelper.CreateWarning (ReflectorError.kCompilerReferenceBase + 54, $"Unable to find parent module {extension.Module.Name} while compiling extension on {extension.ExtensionOnTypeName}");
			var entity = TypeMapper.GetEntityForTypeSpec (extension.ExtensionOnType);
			if (entity == null)
				throw ErrorHelper.CreateWarning (ReflectorError.kCompilerReferenceBase + 55, $"Unable to find mapped C# type for extension on {extension.ExtensionOnTypeName} while compiling extensions.");

			var extensionOnDeclaration = entity.Type;
			PatchExtensionTypeIfNeeded (extension, extensionOnDeclaration);

			var csName = (entity.SharpNamespace + "Dot" + entity.SharpTypeName).Replace (".", "Dot");
			var extensionName = $"ExtensionsFor{csName}{index}";
			var cl = new CSClass (CSVisibility.Public, extensionName, null, true);

			var picl = new CSClass (CSVisibility.Public, CompilerNames.PinvokeClassPrefix + extensionName);
			pinvokes.Add (picl);
			var usedPinvokeNames = new List<string> ();

			var functions = extension.Members.OfType<FunctionDeclaration> ().Where (fn => fn.IsPublicOrOpen && !fn.IsMaterializer).ToList ();

			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);

			foreach (var funcDecl in functions) {
				var wrapperFunc = finder.FindWrapperForExtension (funcDecl, extensionOnDeclaration);
				if (wrapperFunc == null)
					continue;
				var wrapperTLF = FindEquivalentTLFunctionForWrapperFunction (wrapperFunc, TypeMapper, wrapper);
				if (wrapperTLF == null) // if there's no wrapper, we had to skip it.
					continue;
				var method = ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, funcDecl, use, false, wrapper, swiftLibraryPath, wrapperTLF);
			}
			return cl;
		}

		void PatchExtensionTypeIfNeeded (ExtensionDeclaration extension, TypeDeclaration extensionOnDeclaration)
		{
			// new to Swift 5, the ExtensionOnType in the extension contains 'interesting' parameters.
	    		// We don't have the ability to reasonably fix it at the point where it's generated, so we detect the
			// this case and patch it. The extensionOnDeclaration is the actual TypeDeclaration for the type.
			if (extension.ExtensionOnType.ContainsGenericParameters) {
				extension.ExtensionOnTypeName = extensionOnDeclaration.ToTypeSpec ().ToString ();
			}
		}

		CSClass CompileFinalClass (ClassDeclaration classDecl, ModuleInventory modInventory, CSUsingPackages use,
		                           WrappingResult wrapper, string swiftLibraryPath, List<CSClass> pinvokes, ErrorHandling errors)
		{
			var isObjC = classDecl.IsObjCOrInheritsObjC (TypeMapper);
			var swiftClassName = XmlToTLFunctionMapper.ToSwiftClassName (classDecl);
			var classContents = XmlToTLFunctionMapper.LocateClassContents (modInventory, swiftClassName);
			if (classContents == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 57, $"Unable to find class contents for {classDecl.ToFullyQualifiedName ()}");

			string className = StubbedClassName (swiftClassName);
			use.AddIfNotPresent ("SwiftRuntimeLibrary");
			use.AddIfNotPresent (typeof (SwiftValueTypeAttribute));


			var cl = new CSClass (CSVisibility.Public, className, null);
			CSAttribute.FromAttr (typeof (SwiftNativeObjectTagAttribute), new CSArgumentList (), true).AttachBefore (cl);

			var picl = new CSClass (CSVisibility.Public, PIClassName (swiftClassName));
			pinvokes.Add (picl);
			var usedPinvokeNames = new List<string> ();

			AddGenerics (cl, classDecl, classContents, use);

			bool inheritsISwiftObject = false;
			DotNetName sharpInherit = null;
			if (classDecl.Inheritance.Count > 0) {
				var inheritedClasses = classDecl.Inheritance.Where (inh => inh.InheritanceKind == InheritanceKind.Class).ToList ();
				if (inheritedClasses.Count > 1)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 28, $"Class {classDecl.ToFullyQualifiedName (true)} has more than one class inheritance - not supported in C#.");
				if (inheritedClasses.Count > 0) {
					var inheritedEntity = TypeMapper.GetEntityForTypeSpec (inheritedClasses [0].InheritedTypeSpec);
					inheritsISwiftObject = !inheritedEntity.Type.IsObjC;
					sharpInherit = TypeMapper.GetDotNetNameForTypeSpec (inheritedClasses [0].InheritedTypeSpec);
				}
				if (inheritedClasses.Count > 0)
					sharpInherit = TypeMapper.GetDotNetNameForTypeSpec (inheritedClasses [0].InheritedTypeSpec);
			}
			if (sharpInherit != null) {
				use.AddIfNotPresent (sharpInherit.Namespace);
				cl.Inheritance.Add (new CSIdentifier (sharpInherit.TypeName));
			} else {
				ImplementFinalizer (cl);
			}

			var cctor = MakeClassConstructor (cl, picl, usedPinvokeNames, classDecl, classContents, use,
			                                  PInvokeName (swiftLibraryPath), inheritsISwiftObject);

			var ctors = MakeConstructors (cl, picl, usedPinvokeNames, classDecl, classContents, null, null, null, use,
			                              new CSSimpleType (className), wrapper, swiftLibraryPath, false, errors, inheritsISwiftObject);

			AddInheritedProtocols (classDecl, cl, classContents, PInvokeName (swiftLibraryPath), use, errors);

			if (!classDecl.IsObjCOrInheritsObjC (TypeMapper))
				cl.Inheritance.Insert (0, kSwiftNativeObject);

			CompileInnerNominalsInto (classDecl, cl, modInventory, use, wrapper, swiftLibraryPath, pinvokes, errors);

			cl.Constructors.AddRange (cctor);
			cl.Constructors.AddRange (ctors);
			if (isObjC)
				ImplementObjCClassField (cl);

			ImplementMethods (cl, picl, usedPinvokeNames, swiftClassName, classContents, classDecl, use, wrapper, tlf => true, swiftLibraryPath, errors);
			ImplementProperties (cl, picl, usedPinvokeNames, classDecl, classContents, null, use, wrapper, false, false, tlf => true, swiftLibraryPath, errors);
			ImplementSubscripts (cl, picl, usedPinvokeNames, classDecl, classDecl.AllSubscripts (), classContents, null, use, wrapper, true, tlf => true, swiftLibraryPath, errors);

			TypeNameAttribute (classDecl, use).AttachBefore (cl);
			return cl;
		}

		void AddGenerics (CSClass cl, TypeDeclaration classDecl, ClassContents classContents, CSUsingPackages use)
		{
			if (classDecl.ContainsGenericParameters) {
				foreach (GenericDeclaration gen in classDecl.Generics) {
					var depthIndex = classDecl.GetGenericDepthAndIndex (gen.Name);

					var genRef = new CSGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
					var genDecl = new CSGenericTypeDeclaration (new CSIdentifier (genRef.Name));
					cl.GenericParams.Add (genDecl);

					var validConstraints = gen.Constraints.FindAll (constraint => !IsDiscretionaryConstraint (constraint));

					if (validConstraints.Count > 0) {
						var constraint = new CSGenericConstraint (new CSIdentifier (CSGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2)),
						validConstraints.Select (bc => {
							var ic = bc as InheritanceConstraint;
							if (ic != null) {
								var cstype = TypeMapper.MapType (classDecl, ic.InheritsTypeSpec, false);
								// special cases? Yes, we like special cases.
								// AnyObject is technically an empty protocol, so we map
								// it to ISwiftObject.
								if (cstype.FullName == "SwiftRuntimeLibrary.SwiftAnyObject") {
									use.AddIfNotPresent (typeof (ISwiftObject));
									return new CSIdentifier ("ISwiftObject");
								} else {
									use.AddIfNotPresent (cstype.NameSpace);
									return new CSIdentifier (cstype.Type);
								}
							} else {
								throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 26, "Equality constraints not supported yet.");
							}
						}));
						cl.GenericConstraints.Add (constraint);
					}
				}
			}
		}

		bool IsDiscretionaryConstraint (BaseConstraint bc)
		{
			var ic = bc as InheritanceConstraint;
			if (ic != null) {
				var entity = TypeMapper.GetEntityForTypeSpec (ic.InheritsTypeSpec);
				return entity != null && entity.IsDiscretionaryConstraint;
			} else {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 27, "Equality constraints not supported yet.");
			}
		}

		static CSAttribute ProtocolConstraintAttributeFromProtocol (TypeSpec inh, CSType csType, ClassContents contents,
		                                                            string libraryName)
		{
			var ns = inh as NamedTypeSpec;
			if (ns == null)
				return null;
			var theFunc = contents.WitnessTable.Functions.Where (tlf => {
				var wt = tlf.Signature as SwiftWitnessTableType;
				if (wt == null) return false;
				return wt.WitnessType == WitnessType.Protocol &&
					 wt.ProtocolType.ClassName.ToFullyQualifiedName () == ns.Name;
			}).FirstOrDefault ();
			if (theFunc == null) return null;
			return MakeProtocolConstraintAttribute (csType, libraryName, theFunc.MangledName.Substring (1));
		}


		static string PInvokeName (string libFullPath, string originalLibrary = null)
		{
			SwiftRuntimeLibrary.Exceptions.ThrowOnNull (libFullPath, nameof (libFullPath));
			// if the original library is a framework, we need to treat
			// the given library as a framework.
			if (originalLibrary != null) {
				string directory = Path.GetDirectoryName (originalLibrary);
				string file = Path.GetFileName (originalLibrary);
				if (UniformTargetRepresentation.ModuleIsFramework (file, new List<string> { directory })) {
					file = Path.GetFileName (libFullPath);
					return $"@rpath/{file}.framework/{file}";
				}
				return LibOrFrameworkFromPath (libFullPath);
			} else {
				return LibOrFrameworkFromPath (libFullPath);
			}
		}

		static string LibOrFrameworkFromPath (string libFullPath)
		{
			string directory = Path.GetDirectoryName (libFullPath);
			string file = Path.GetFileName (libFullPath);
			if (UniformTargetRepresentation.ModuleIsFramework (file, new List<string> { directory })) {
				string parent = Path.GetFileName (directory);
				return $"@rpath/{file}.framework/{file}";
			} else
				return file;
		}

		string StubbedClassName (SwiftClassName name)
		{
			return StubbedClassName (name, TypeMapper);
		}

		public static string StubbedClassName (SwiftClassName name, TypeMapper mapper)
		{
			string className = mapper.GetDotNetNameForSwiftClassName (name).TypeName;
			// class name
			string consName = className.Substring (className.LastIndexOf ('.') + 1);
			return consName;
		}

		public static string StubbedClassName (string fullSwiftClassName, TypeMapper mapper)
		{
			string className = mapper.GetDotNetNameForSwiftClassName (fullSwiftClassName).TypeName;
			if (className == null)
				return null;
			// class name
			string consName = className.Substring (className.LastIndexOf ('.') + 1);
			return consName;
		}

		string FieldName (string name)
		{
			return TypeMapper.SanitizeIdentifier (name);
		}

		string PIClassName (string fullClassName)
		{
			if (!fullClassName.Contains ('.'))
				throw new ArgumentOutOfRangeException (nameof (fullClassName), String.Format ("Class name {0} should be a full class name.", fullClassName));
			fullClassName = fullClassName.Substring (fullClassName.IndexOf ('.') + 1).Replace ('.', '_');
			return CompilerNames.PinvokeClassPrefix + fullClassName;
		}

		string PIClassName (DotNetName fullClassName)
		{
			return PIClassName (fullClassName.Namespace + "." + fullClassName.TypeName);
		}

		string PIClassName (SwiftClassName name)
		{
			return PIClassName (TypeMapper.GetDotNetNameForSwiftClassName (name));
		}

		string PICCTorName (SwiftClassName name)
		{
			return "PIMetadataAccessor_" + StubbedClassName (name);
		}

		string PICCTorName (string fullClassName, TypeMapper mapper)
		{
			return "PIMetadataAccessor_" + StubbedClassName (fullClassName, mapper);
		}

		string PICCTorReference (SwiftClassName name)
		{
			return PIClassName (name) + "." + PICCTorName (name);
		}

		string PICTorName (SwiftClassName name)
		{
			return "PI_" + StubbedClassName (name);
		}

		string PICTorName (string fullClassName, TypeMapper mapper)
		{
			return "PI_" + StubbedClassName (fullClassName, mapper);
		}

		string PICTorReference (SwiftClassName name)
		{
			return PIClassName (name) + "." + PICTorName (name);
		}

		string PIDTorName (SwiftClassName name)
		{
			return "PIdtor_" + StubbedClassName (name);
		}

		string PIDTorReference (SwiftClassName name)
		{
			return PIClassName (name) + "." + PIDTorName (name);
		}

		string PIMethodName (SwiftClassName name, SwiftName functionName)
		{
			return String.Format ("PImethod_{0}{1}", StubbedClassName (name, TypeMapper), TypeMapper.SanitizeIdentifier (functionName.Name));
		}

		string PIMethodName (string name, SwiftName functionName)
		{
			var stubName = StubbedClassName (name, TypeMapper);
			if (stubName == null)
				return null;
			return String.Format ("PImethod_{0}{1}", stubName, TypeMapper.SanitizeIdentifier (functionName.Name));
		}

		string PIMethodReference (string name, SwiftName functionName)
		{
			var stubName = StubbedClassName (name, TypeMapper);
			if (stubName == null)
				return null;
			return PIClassName (stubName) + "." + PIMethodName (name, functionName);
		}

		string PIMethodName (SwiftClassName name, SwiftName functionName, PropertyType propType)
		{
			return String.Format ("PIprop{0}_{1}{2}", PropPrefix (propType), name != null ? StubbedClassName (name, TypeMapper) : "", TypeMapper.SanitizeIdentifier (functionName.Name));
		}

		string PIMethodReference (SwiftClassName name, SwiftName functionName, PropertyType propType)
		{
			return PIClassName (name) + "." + PIMethodName (name, functionName, propType);
		}

		string PIMethodName (string name, SwiftName functionName, PropertyType propType)
		{
			var stubName = name != null ? StubbedClassName (name, TypeMapper) : "";
			if (stubName == null && name == null)
				return null;
			return String.Format ("PIprop{0}_{1}{2}", PropPrefix (propType), name != null ? stubName : "", TypeMapper.SanitizeIdentifier (functionName.Name));
		}

		string PIMethodReference (string fullclassName, SwiftName functionName, PropertyType propType)
		{
			return PIClassName (fullclassName) + "." + PIMethodName (fullclassName, functionName, propType);
		}

		string PIFuncName (SwiftName functionName)
		{
			return String.Format ("PIfunc_{0}", functionName.Name);
		}

		string PIFuncName (string functionName)
		{
			return $"PIfunc_{functionName}";
		}

		char PropPrefix (PropertyType propType)
		{
			switch (propType) {
			case PropertyType.Getter:
				return 'g';
			case PropertyType.Setter:
				return 's';
			case PropertyType.Materializer:
				return 'm';
			default:
				throw new ArgumentOutOfRangeException ("propType");
			}
		}

		void ImplementProxyConstructorAndFields (CSClass cl, CSUsingPackages use, bool hasVtable, CSInterface iface, bool hasAssociatedTypes)
		{
			if (hasVtable || hasAssociatedTypes)
				cl.Fields.Add (CSFieldDeclaration.FieldLine (iface.ToCSType (), kInterfaceImpl));
			if (!hasAssociatedTypes) {
				cl.Fields.Add (CSFieldDeclaration.FieldLine (new CSSimpleType (typeof (SwiftExistentialContainer1)), kContainer));
				var prop = CSProperty.PublicGetBacking (new CSSimpleType (typeof (ISwiftExistentialContainer)), new CSIdentifier ("ProxyExistentialContainer"), kContainer, false, CSMethodKind.Override);
				cl.Properties.Add (prop);
				ImplementProtocolProxyConstructors (cl, use, hasVtable, iface);
			} else {
				ImplementProtocolProxyConstructorAssociatedTypes (cl, iface);
			}
		}

		void ImplementProtocolProxyConstructors (CSClass cl, CSUsingPackages use, bool hasVtable, CSInterface iface)
		{
			// ctor 1:
			// public TypeName (InterfaceName actualImplementation, EveryProtocol everyProtocol)
			// : base (typeof (InterfaceName), everyProtocol)
			// {
			//      kInterfaceImpl = actualImplementation; // only if there's vtable
			//      kContainer = new SwiftExistentialContainer1 (everyProtocol, ProtocolWitnessTable);
			//      ProxyExistentialContainer = kContainer;
			// }
			//

			use.AddIfNotPresent (typeof (SwiftExistentialContainer1));
			use.AddIfNotPresent (typeof (ISwiftExistentialContainer));
			use.AddIfNotPresent (typeof (EveryProtocol));

			var actualImplId = new CSIdentifier ("actualImplementation");
			var everyProtocolId = new CSIdentifier ("everyProtocol");
			var parms = new CSParameterList ();

			parms.Add (new CSParameter (iface.ToCSType (), actualImplId));
			parms.Add (new CSParameter (new CSSimpleType (typeof (EveryProtocol)), everyProtocolId));

			var body = new CSCodeBlock ();
			if (hasVtable)
				body.Add (CSAssignment.Assign (kInterfaceImpl, actualImplId));
			body.And (CSAssignment.Assign (kContainer,
					new CSFunctionCall (nameof (SwiftExistentialContainer1), true, everyProtocolId, kProtocolWitnessTable)));

			var baseParms = new CSBaseExpression [] { iface.ToCSType ().Typeof (), everyProtocolId };

			var m = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, cl.Name, parms, baseParms, true, body);
			cl.Constructors.Add (m);

			// ctor 2:
			// public TypeName (ISwiftExistentialContainer container)
			// : base (typeof (InterfaceName), null)
			// {
			//      this.kContainer = new SwiftExistentialContainer1 (container);
			//      ProxyExistentialContainer = kContainer;
			// }

			var containerId = new CSIdentifier ("container");
			parms = new CSParameterList ();
			parms.Add (new CSParameter (new CSSimpleType (typeof (ISwiftExistentialContainer)), containerId));
			body = new CSCodeBlock ();
			body.Add (CSAssignment.Assign (kContainer, new CSFunctionCall (nameof (SwiftExistentialContainer1), true, containerId)));

			baseParms = new CSBaseExpression [] { iface.ToCSType ().Typeof (), CSConstant.Null };
			m = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, cl.Name, parms, baseParms, true, body);
			cl.Constructors.Add (m);

			if (hasVtable)
				cl.StaticConstructor.Add (CallToSetVTable ());
		}

		void ImplementProtocolProxyConstructorAssociatedTypes (CSClass cl, CSInterface iface)
		{
			// ctor:
			// public TypeName(IterfaceName actualImplementation)
			//    : this ()
			// {
			//     kInterfaceImple = actualImplementation;
			// }

			var actualImplId = new CSIdentifier ("actualImplementation");
			var parms = new CSParameterList ();

			parms.Add (new CSParameter (iface.ToCSType (), actualImplId));

			var body = new CSCodeBlock ();
			body.Add (CSAssignment.Assign (kInterfaceImpl, actualImplId));
			var thisParms = new CSBaseExpression [0];
			var m = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, cl.Name, parms, thisParms, false, body);
			cl.Constructors.Add (m);
			cl.StaticConstructor.Add (CallToSetVTable ());
		}


		void ImplementProtocolWitnessTableAccessor (CSClass proxyClass, CSInterface iface, ProtocolDeclaration protocolDecl, WrappingResult wrapper,
								CSUsingPackages use, string swiftLibraryPath)
		{
			// static IntrPtr protocolWitnessTable;
			// public static IntPtr ProtocolWitnessTable {
			//	get {
			//			if (protocolWitnessTable == IntPtr.Zero)
			//				protocolWitnessTable = SwiftCore.ProtocolWitnessTableFromFile (dylibFile, conformanceIdentifier,
			//							EveryProtocol.GetSwiftMetatype());
			//			return protocolWitnessTable;
			//	}
			// }

			var proxyName = protocolDecl.IsExistential ? "EveryProtocol" : OverrideBuilder.AssociatedTypeProxyClassName (protocolDecl);

			ClassContents swiftProxy = null;
			foreach (var cl in wrapper.Contents.Classes.Values) {
				if (cl.Name.Terminus.Name == proxyName) {
					swiftProxy = cl;
					break;
				}
			}
			if (swiftProxy == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 30, $"Unable to find swift proxy class for protocol {protocolDecl.ToFullyQualifiedName (true)}");

			string conformanceSymbol = null;
			foreach (var conform in swiftProxy.ProtocolConformanceDescriptors) {
				if (conform.Protocol.ClassName.ToFullyQualifiedName () == protocolDecl.ToFullyQualifiedName (true)) {
					conformanceSymbol = conform.MangledName;
					break;
				}
			}

			if (String.IsNullOrEmpty (conformanceSymbol))
				throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 31, $"Unable to find protocol conformance descriptor for {protocolDecl.ToFullyQualifiedName (true)}");

			if (conformanceSymbol.StartsWith ("_$", StringComparison.Ordinal) || conformanceSymbol.StartsWith ("__T0", StringComparison.Ordinal))
				conformanceSymbol = conformanceSymbol.Substring (1);

			var fieldName = new CSIdentifier ("protocolWitnessTable");
			var field = new CSFieldDeclaration (CSSimpleType.IntPtr, fieldName, isStatic:true);
			proxyClass.Fields.Add (CSFieldDeclaration.FieldLine (CSSimpleType.IntPtr, fieldName, isStatic: true));
			var condition = fieldName == new CSIdentifier ("IntPtr.Zero");
			var ifBlock = new CSCodeBlock ();
			var ifTest = new CSIfElse (condition, ifBlock);
			var metaAccessor = protocolDecl.HasAssociatedTypes ? "GetSwiftMetatype" : "EveryProtocol.GetSwiftMetatype";
			var assign = CSAssignment.Assign (fieldName, new CSFunctionCall ("SwiftCore.ProtocolWitnessTableFromFile", false,
				CSConstant.Val (PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath)), CSConstant.Val (conformanceSymbol),
				new CSFunctionCall (metaAccessor, false)));
			ifBlock.Add (assign);

			var getterBlock = new CSCodeBlock ();
			getterBlock.Add (ifTest);
			getterBlock.Add (CSReturn.ReturnLine (fieldName));
			var prop = new CSProperty (CSSimpleType.IntPtr, CSMethodKind.Static, kProtocolWitnessTable,
				CSVisibility.Public, getterBlock, CSVisibility.Public, null);
			proxyClass.Properties.Add (prop);
		}

		void ImplementObjCClassField (CSClass cl)
		{
			var fieldID = new CSIdentifier ("class_ptr");
			var field = new CSFieldDeclaration (CSSimpleType.IntPtr, fieldID, new CSFunctionCall ("GetSwiftMetatype", false).Dot (new CSIdentifier ("Handle")),
			                                    CSVisibility.None, true, true);
			cl.Fields.Add (new CSLine (field));
			var getter = new CSCodeBlock ().And (CSReturn.ReturnLine (fieldID));
			var prop = new CSProperty (CSSimpleType.IntPtr, CSMethodKind.Override, new CSIdentifier ("ClassHandle"), CSVisibility.Public,
						   getter, CSVisibility.Public, null);
			cl.Properties.Add (prop);
		}

		void ImplementValueTypeIDisposable (CSClass cl, CSUsingPackages use)
		{
			ImplementFinalizer (cl); 
		}

		void ImplementFinalizer (CSClass cl)
		{
			//~Type()
			//{
			//    Dispose(false);
			//}
			var disposeID = new CSIdentifier ("Dispose");
			var csDestructorIdent = new CSIdentifier ($"~{cl.Name.Name}");
			var destructor = new CSMethod (CSVisibility.None, CSMethodKind.None, null, csDestructorIdent, new CSParameterList (),
				CSCodeBlock.Create (CSFunctionCall.FunctionCallLine (disposeID, false, CSConstant.Val (false)))
				);
			cl.Methods.Add (destructor);
		}

		IEnumerable<CSMethod> MakeClassConstructor (CSClass cl, CSClass picl, List<string> usedPinvokeNames, TypeDeclaration classDecl, ClassContents classContents, CSUsingPackages use,
		                                            string libraryPath, bool inheritsISwiftObject, bool hasDynamicSelf = false)
		{
			var pinvokeCCTorName = PICCTorName (classContents.Name);
			pinvokeCCTorName = Uniqueify (pinvokeCCTorName, usedPinvokeNames);
			usedPinvokeNames.Add (pinvokeCCTorName);

			var pinvokeCCTorRef = PIClassName (classContents.Name) + "." + pinvokeCCTorName;
			var tlf = classContents.ClassConstructor.Values.ElementAt (0).Functions [0];


			var pl = new CSParameterList ();
			use.AddIfNotPresent (typeof (SwiftMetadataRequest));
			pl.Add (new CSParameter (new CSSimpleType (typeof (SwiftMetadataRequest)), new CSIdentifier ("request")));

			if (hasDynamicSelf) {
				use.AddIfNotPresent (typeof (SwiftMetatype));
				pl.Add (new CSParameter (new CSSimpleType (typeof (SwiftMetatype)), new CSIdentifier ("mtself")));
			}
			for (int i = 0; i < classDecl.Generics.Count; i++) {
				if (i == 0)
					use.AddIfNotPresent (typeof (SwiftMetatype));
				pl.Add (new CSParameter (new CSSimpleType (typeof (SwiftMetatype)), new CSIdentifier (String.Format ("mt{0}", i))));
			}
			foreach (GenericDeclaration genDecl in classDecl.Generics) {
				int k = 0;
				foreach (var constr in genDecl.Constraints.OfType<InheritanceConstraint> ()) {
					Entity en = TypeMapper.GetEntityForTypeSpec (constr.InheritsTypeSpec);
					if (en != null && en.EntityType == EntityType.Protocol) {
						pl.Add (new CSParameter (CSSimpleType.IntPtr, new CSIdentifier ($"wt{k}")));
						k++;
					}
				}
			}

			use.AddIfNotPresent (typeof (SwiftMetatype));
			var picctor = new CSMethod (CSVisibility.Internal, CSMethodKind.StaticExtern, new CSSimpleType (typeof (SwiftMetatype)),
						    new CSIdentifier (pinvokeCCTorName), pl, null);
			// pre-trimmed
			CSAttribute.DllImport (libraryPath, tlf.MangledName.Substring (1)).AttachBefore (picctor);
			picl.Methods.Add (picctor);


			CSParameterList pl1 = new CSParameterList ();
			var arrElems = new List<CSBaseExpression> (cl.GenericParams.Count + 1);
			arrElems.Add (new CSIdentifier ("SwiftMetadataRequest.Complete"));
			if (hasDynamicSelf) {
				arrElems.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false,
					new CSFunctionCall ("typeof", false, NewClassCompiler.kGenericSelf)));
			}
			var constrainElems = new List<CSBaseExpression> ();
			if (cl.GenericParams.Count > 0) {
				for (int i = 0; i < cl.GenericParams.Count; i++) {
					if (cl.GenericParams [i].Name.Name == kGenericSelfName)
						continue;
					if (classDecl.Generics [i].IsProtocolConstrained (TypeMapper)) {
						var protConstraints =
							classDecl.Generics [i].Constraints.
							         OfType<InheritanceConstraint> ().
								 Select (inh => TypeMapper.MapType (classDecl, inh.InheritsTypeSpec, false).ToCSType (use)).
									 ToList ();
						foreach (CSType protConst in protConstraints) {
							constrainElems.Add (new CSFunctionCall ("StructMarshal.Marshaler.ProtocolWitnessof",
							                                      false,
							                                      protConst.Typeof (),
							                                      new CSSimpleType (cl.GenericParams [i].Name.Name).Typeof ()));
						}
						var protHints = protConstraints.Select (ct => ct.Typeof ()).ToArray ();
						arrElems.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false,
						                                new CSFunctionCall ("typeof", false, cl.GenericParams [i].Name),
						                                new CSArray1DInitialized (CSSimpleType.Type, protHints)));
					} else {
						arrElems.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false,
						                                new CSFunctionCall ("typeof", false, cl.GenericParams [i].Name)));
					}
				}
				if (constrainElems.Count > 0) {
					arrElems.AddRange (constrainElems);
				}
			}

			var body = new CSCodeBlock ();
			body.Add (CSReturn.ReturnLine (new CSFunctionCall (pinvokeCCTorRef, false, arrElems.ToArray ())));

			var methodKind = inheritsISwiftObject ? CSMethodKind.StaticNew : CSMethodKind.Static;
			var staticMetaTypeAccessor = new CSMethod (CSVisibility.Public, methodKind,
			                                           new CSSimpleType (typeof (SwiftMetatype)), new CSIdentifier ("GetSwiftMetatype"),
			                                           pl1, body);
			yield return staticMetaTypeAccessor;
		}

		IEnumerable<CSMethod> MakeConstructors (CSClass cl, CSClass picl, List<string> usedPinvokeNames,
							  TypeDeclaration classDecl, ClassContents classContents,
							  ClassDeclaration superClassDecl, ClassContents superClassContents,
							  SwiftClassName superClassName,
							  CSUsingPackages use, CSType csClassType, WrappingResult wrapper,
							  string swiftLibraryPath, bool isSubclass, ErrorHandling errors, bool inheritsISwiftObject,
							  Func<int, int, string> genericNamer = null, bool isAssociatedTypeProxy = false,
							  bool hasDynamicSelf = false)
		{
			var allCtors = classDecl.AllConstructors ().Where (cn => cn.Access == Accessibility.Public || cn.Access == Accessibility.Open).ToList ();

			foreach (var funcDecl in allCtors) {
				if (funcDecl.IsOptionalConstructor) {
					MakeOptionalConstructor (cl, picl, usedPinvokeNames, classDecl, superClassDecl, superClassContents ?? classContents, use, csClassType, wrapper, swiftLibraryPath, funcDecl, errors);
					continue;
				}
				foreach (var m in ConstructorWrapperToMethod (classDecl, superClassDecl, funcDecl, cl, picl, usedPinvokeNames, csClassType,
								superClassName, use, wrapper, superClassContents ?? classContents,
								PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath), errors, genericNamer, isAssociatedTypeProxy))
					yield return m;
			}
			string className = StubbedClassName (superClassName ?? classContents.Name);
			if (!inheritsISwiftObject && classDecl.IsObjCOrInheritsObjC (TypeMapper))
				yield return MakeProtectedConstructor (classDecl, className, use);
			if (!classDecl.IsObjCOrInheritsObjC (TypeMapper))
				yield return MakeProtectedFactoryConstructor (classDecl, superClassDecl, className, use, isAssociatedTypeProxy);
			yield return MakePublicFactory (classDecl, className, use, inheritsISwiftObject, hasDynamicSelf);
		}


		void MakeOptionalConstructor (CSClass cl, CSClass picl, List<string> usedPinvokeNames,
		                              TypeDeclaration classDecl, TypeDeclaration superClassDecl, ClassContents classContents,
		                              CSUsingPackages use, CSType csClassType, WrappingResult wrapper,
		                              string swiftLibraryPath, FunctionDeclaration funcDecl, ErrorHandling errors)
		{
			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
			var wrapperFuncDecl = finder.FindWrapperForConstructor (superClassDecl ?? classDecl, funcDecl);
			var wrapperFunc = wrapperFuncDecl != null ? FindEquivalentTLFunctionForWrapperFunction (wrapperFuncDecl, TypeMapper, wrapper) : null;

			if (wrapperFunc == null) {
				var ex = ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 60, $"Unable to find wrapper for optional constructor in {funcDecl.ToFullyQualifiedName (true)}");
				errors.Add (ex);
				return;
			}
			// we're given a func in the form .ctor( ...parameters... ) -> Swift.Optional<ClassName>
			// we need to transform it into: static ClassNameOptional( ...parameters...) -> Swift.Optional<ClassName>

			var returnTypeSpec = funcDecl.ReturnTypeSpec;
			var actualFunc = new FunctionDeclaration {
				Module = classDecl.Module,
				Parent = classDecl,
				Name = $"{cl.Name}Optional",
				Access = Accessibility.Public,
				IsStatic = true,
				ReturnTypeName = returnTypeSpec.ToString ()
			};
			actualFunc.Generics.AddRange (funcDecl.Generics);
			actualFunc.ParameterLists.Add (funcDecl.ParameterLists.Last ());


			ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, actualFunc, use, false, wrapper, swiftLibraryPath, wrapperFunc);
		}

		FunctionDeclaration CorrespondingConstructor (TLFunction tlf, ClassContents contents, List<FunctionDeclaration> allCtors)
		{
			foreach (FunctionDeclaration funcDecl in allCtors) {
				if (funcDecl.ParameterLists.Last ().Count () != tlf.Signature.ParameterCount)
					continue;
				var possibleMatch = XmlToTLFunctionMapper.ToTLFunction (funcDecl, contents, TypeMapper);
				if (possibleMatch != null && possibleMatch == tlf)
					return funcDecl;
			}
			return null;
		}

		CSMethod MakeProtectedConstructor (TypeDeclaration classDecl, string constructorName, CSUsingPackages use)
		{
			// objc
			// protected constructorName (IntPtr handle, SwiftMetatype classHandle)
			// {
			//     base.InitializeHandle (handle);
			// }

			var handleParmId = new CSIdentifier ("handle");
			use.AddIfNotPresent (typeof (SwiftObjectRegistry));
			use.AddIfNotPresent (typeof (SwiftObjectFlags));
			var parms =
				new CSParameter [] {
					new CSParameter (CSSimpleType.IntPtr, handleParmId),
				};
			var body = new CSCodeBlock ();

			body.Add (ImplementNativeObjectCheck ());

			return new CSMethod (CSVisibility.Protected, CSMethodKind.None, null, new CSIdentifier (constructorName), new CSParameterList (parms),
						new CSBaseExpression [] { handleParmId }, true, body);
		}

		CSMethod MakeProtectedFactoryConstructor (TypeDeclaration classDecl, TypeDeclaration superClassDecl, string constructorName, CSUsingPackages use,
			bool forceBaseCall)
		{
			//			protected constructorName (IntPtr p, SwiftMetatype mt, SwiftObjectRegistry registry)
			//			{
			//				_swiftObject = p;
			//				registry.Add (this);
			//			}
			var handleId = new CSIdentifier ("handle");
			var mtId = new CSIdentifier ("mt");
			var registryId = new CSIdentifier ("registry");
			use.AddIfNotPresent (typeof (SwiftObjectRegistry));
			var isObjC = classDecl.IsObjCOrInheritsObjC (TypeMapper);
			if (isObjC)
				throw new NotImplementedException ("The private factory constructor is not supported for ObjC classes");
			var parms = new CSParameter [] {
				new CSParameter (CSSimpleType.IntPtr, handleId),
				new CSParameter (new CSSimpleType (typeof (SwiftMetatype).Name), mtId),
				new CSParameter (new CSSimpleType (typeof (SwiftObjectRegistry).Name), registryId)
			};


			var thisExprs = new CSBaseExpression [] {
				handleId, mtId, registryId
			};

			return new CSMethod (CSVisibility.Protected, CSMethodKind.None, null, new CSIdentifier (constructorName),
			                     new CSParameterList (parms), thisExprs, callsBase: true, new CSCodeBlock ());
		}

		IEnumerable<CSMethod> MakeStructConstructors (CSClass st, CSClass picl, List<string> usedPinvokeNames, StructDeclaration structDecl,
		                                              ClassContents classContents, CSUsingPackages use, CSType csStructType,
		                                              WrappingResult wrapper, string swiftLibraryPath, ErrorHandling errors)
		{
			var allCtors = structDecl.AllConstructors ().Where (ctor => ctor.IsPublicOrOpen).ToList ();

			foreach (var funcDecl in allCtors) {
				if (funcDecl.IsOptionalConstructor) {
					MakeOptionalConstructor (st, picl, usedPinvokeNames, structDecl, superClassDecl: null, classContents, use, csStructType, wrapper, swiftLibraryPath,
					                         funcDecl, errors);
					continue;
				}
				foreach (CSMethod m in StructConstructorToMethod (structDecl, funcDecl, st, picl, usedPinvokeNames, csStructType, classContents, use, wrapper, swiftLibraryPath, errors))
					yield return m;
			}
			yield return ValueTypeDefaultConstructor (csStructType, classContents, use);
		}

		CSMethod ValueTypeDefaultConstructor (CSType structType, ClassContents classContents, CSUsingPackages use)
		{
			var parms = new CSParameterList ();
			use.AddIfNotPresent (typeof (SwiftValueTypeCtorArgument));
			parms.Add (new CSParameter (new CSSimpleType (typeof (SwiftValueTypeCtorArgument)), "unused"));
			var body = new CSCodeBlock ();

			string consName = StubbedClassName (classContents.Name);

			var ctor = new CSMethod (CSVisibility.Internal, CSMethodKind.None, null, new CSIdentifier (consName), parms, new CSBaseExpression [0], true, body);

			return ctor;
		}

		IEnumerable<CSMethod> StructConstructorToMethod (StructDeclaration structDecl, FunctionDeclaration funcDecl, CSClass st, CSClass picl, List<string> usedPinvokeNames,
		                                                 CSType csType, ClassContents classContents,
		                                                 CSUsingPackages use, WrappingResult wrapper, string swiftLibraryPath, ErrorHandling errors)
		{
			CSMethod publicCons = null, piConstructor = null;

			try {
				var homonymSuffix = Homonyms.HomonymSuffix (funcDecl, structDecl.Members.OfType<FunctionDeclaration> (), TypeMapper);
				var isHomonym = homonymSuffix.Length > 0;
				var libraryPath = PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath);
				var className = structDecl.ToFullyQualifiedName ();
				var consName = StubbedClassName (className, TypeMapper) + homonymSuffix;
				var pinvokeConsName = PICTorName (className, TypeMapper) + homonymSuffix;
				pinvokeConsName = Uniqueify (pinvokeConsName, usedPinvokeNames);
				usedPinvokeNames.Add (pinvokeConsName);

				var pinvokeConsRef = PIClassName (TypeMapper.GetDotNetNameForSwiftClassName(className)) + "." + pinvokeConsName;

				var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
				var wrapperFunc = finder.FindWrapperForConstructor (structDecl, funcDecl);
				var wrapperFunction = wrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (wrapperFunc, TypeMapper, wrapper) : null;

				if (wrapperFunction == null) {
					var ex = ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 63, $"Unable to find matching constructor declaration for {funcDecl} in struct {structDecl.ToFullyQualifiedName ()}, skipping.");
					errors.Add (ex);
					yield break;
				}
				piConstructor = TLFCompiler.CompileMethod (wrapperFunc, use,
					libraryPath, wrapperFunction.MangledName, pinvokeConsName, true, true, false);

				publicCons = TLFCompiler.CompileMethod (funcDecl, use, libraryPath, wrapperFunction.MangledName, consName, false, true, false);
				if (isHomonym) {
					publicCons = new CSMethod (publicCons.Visibility, CSMethodKind.Static, new CSSimpleType (st.Name.Name), publicCons.Name,
								  publicCons.Parameters, publicCons.Body);
				}

				var piCCTorName = PICCTorName (className, TypeMapper);

				var localIdents = new List<String> {
					publicCons.Name.Name,
					piConstructor.Name.Name,
					piCCTorName,
					kThisName
				};

				var engine = new MarshalEngine (use, localIdents, TypeMapper, wrapper.Module.SwiftCompilerVersion);

				publicCons.Body.AddRange (engine.MarshalConstructor (structDecl, st, funcDecl, wrapperFunc, pinvokeConsRef, publicCons.Parameters,
							funcDecl.ReturnTypeSpec, csType, piCCTorName, null, wrapper, isHomonym));
			     


			} catch (RuntimeException err) {
				var args = funcDecl.ParameterLists.Last ().Select (parm => $"{parm.PublicName ?? parm.PrivateName}: {parm.TypeName}").InterleaveCommas ();
				var message = $"Unable to build C# constructor for struct {funcDecl.ToFullyQualifiedName ()} ({args}), skipping ({err.Message})";
				err = new RuntimeException (err.Code, false, err, message);
				errors.Add (err); 
				yield break;
			} catch (Exception anythingElse) {
				var args = funcDecl.ParameterLists.Last ().Select (parm => $"{parm.PublicName ?? parm.PrivateName}: {parm.TypeName}").InterleaveCommas ();
				var message = $"Unable to build C# constructor for struct {funcDecl.ToFullyQualifiedName ()} ({args}), skipping ({anythingElse.Message})";
				var err = ErrorHelper.CreateWarning (ReflectorError.kCompilerBase + 28, message);
				errors.Add (err);
				yield break;
			}
			picl.Methods.Add (piConstructor);
			yield return publicCons;
		}


		CSMethod MakePublicFactory (TypeDeclaration classDecl, string className, CSUsingPackages use, bool inheritsISwiftObject, bool hasDynamicSelf)
		{
			//			public static type XamarinFactory(IntPtr p)
			//			{
			//				return new type (p, GetSwiftMetatype (), SwiftObjectRegistry.Registry);
			//			}
			// or
			//          public static object XamarinFactory(IntPtr p, Type[] genericTypes)
			//			{
			//              Type t = typeof(className<,,,>).MakeGenericType(genericTypes);
			//				ConstructorInfo ci = t.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
			//					null, new Type[] { typeof(IntPtr), typeof(SwiftObjectRegistry), null);
			//			    return ci.Invoke(new object[] { p, SwiftObjectRegistry.Registry }, null);
			//          }

			var extraFactoryParam = classDecl.IsObjCOrInheritsObjC (TypeMapper) ? null :
							 new CSIdentifier ("SwiftObjectRegistry").Dot (new CSIdentifier ("Registry"));

			if (classDecl.ContainsGenericParameters || hasDynamicSelf) {
				var parms = new CSParameter [] {
					new CSParameter(CSSimpleType.IntPtr, new CSIdentifier("p")),
					new CSParameter(new CSSimpleType("Type", true), new CSIdentifier("genericTypes"))
				};
				var count = classDecl.Generics.Count - (hasDynamicSelf ? 0 : 1);
				var sb = new StringBuilder ().Append (className).Append ("<");
				for (int i = 0; i < classDecl.Generics.Count - 1; i++) {
					sb.Append (',');
				}
				sb.Append (">");
				var tLine = CSVariableDeclaration.VarLine (new CSSimpleType (typeof (Type)), "t",
				                                         new CSFunctionCall (String.Format ("typeof({0}).MakeGenericType", sb.ToString ()),
																		  false, parms [1].Name));
				use.AddIfNotPresent (typeof (ConstructorInfo));
				var ciLine = CSVariableDeclaration.VarLine (new CSSimpleType (typeof (ConstructorInfo)), "ci",
				                                          new CSFunctionCall ("t.GetConstructor", false,
				                                                            new CSIdentifier ("BindingFlags.Instance") | new CSIdentifier ("BindingFlags.NonPublic"),
				                                                            CSConstant.Null,
				                                                            new CSArray1DInitialized (new CSSimpleType (typeof (Type)),
				                                                                                      new CSSimpleType (typeof (IntPtr)).Typeof (),
				                                                                                      new CSSimpleType (typeof (SwiftObjectRegistry)).Typeof ()),
				                                                            CSConstant.Null));
				var retLine = CSReturn.ReturnLine (new CSFunctionCall ("ci.Invoke", false,
				                                                    new CSArray1DInitialized (CSSimpleType.Object, parms [0].Name,  extraFactoryParam)));
				var meth = new CSMethod (CSVisibility.Public, CSMethodKind.Static, CSSimpleType.Object,
				                       new CSIdentifier (SwiftObjectRegistry.kXamarinFactoryMethodName), new CSParameterList (parms), new CSCodeBlock ()
				                       .And (tLine).And (ciLine).And (retLine));
				return meth;
			} else {
				var parms = new CSParameter [] {
					new CSParameter(CSSimpleType.IntPtr, new CSIdentifier("p"))
				};

				var methodKind = inheritsISwiftObject ? CSMethodKind.StaticNew : CSMethodKind.Static;
				var body = new CSCodeBlock ();
				if ((object)extraFactoryParam != null) {
					body.And (CSReturn.ReturnLine (new CSFunctionCall (className, true, parms [0].Name, new CSFunctionCall ("GetSwiftMetatype", false), extraFactoryParam)));
				} else {
					body.And (CSReturn.ReturnLine (new CSFunctionCall (className, true, parms [0].Name)));
				}
				var meth = new CSMethod (CSVisibility.Public, methodKind, new CSSimpleType (className),
				                         new CSIdentifier (SwiftObjectRegistry.kXamarinFactoryMethodName), new CSParameterList (parms), body);
				return meth;
			}
		}

		IEnumerable<CSMethod> ConstructorWrapperToMethod (TypeDeclaration classDecl, TypeDeclaration superClassDecl, FunctionDeclaration funcDecl, CSClass cl, CSClass picl, List<string> usedPinvokeNames,
								  CSType csType, SwiftClassName superClassName,
								  CSUsingPackages use, WrappingResult wrapper, ClassContents contents, string libraryPath,
								  ErrorHandling errors, Func<int, int, string> genericNamer, bool isAssociatedTypeProxy)
		{
			var homonymSuffix = Homonyms.HomonymSuffix (funcDecl, classDecl.Members.OfType<FunctionDeclaration> (), TypeMapper);
			var isHomonym = homonymSuffix.Length > 0;
			var referentialClass = superClassDecl ?? classDecl;

			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
			var wrapperFunc = finder.FindWrapperForConstructor (referentialClass, funcDecl);

			var wrapperTlf = wrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (wrapperFunc, TypeMapper, wrapper) : null;
			if (wrapperTlf == null) {
				var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerReferenceBase + 64, $"Unable to find wrapper for constructor {funcDecl} in class {classDecl.ToFullyQualifiedName ()}, skipping.");
				errors.Add (ex);
				yield break;
			}

			CSMethod publicCons = null, privateConsImpl = null;
			try {
				var piCtorName = PICTorName (referentialClass.ToFullyQualifiedName (), TypeMapper) + homonymSuffix;
				piCtorName = Uniqueify (piCtorName, usedPinvokeNames);
				usedPinvokeNames.Add (piCtorName);

				var pictorRef = PIClassName (TypeMapper.GetDotNetNameForSwiftClassName (classDecl.ToFullyQualifiedName ())) + "." + piCtorName;
				var piConstructor = TLFCompiler.CompileMethod (wrapperFunc, use, libraryPath, wrapperTlf.MangledName,
									       piCtorName, true, true, true);
				picl.Methods.Add (piConstructor);
				var className = superClassName != null ? superClassName.ToFullyQualifiedName () : classDecl.ToFullyQualifiedName ();
				string stubbedName = StubbedClassName (className, TypeMapper);

				publicCons = TLFCompiler.CompileMethod (funcDecl, use, libraryPath: "", mangledName: "", stubbedName,
									    false, true, false);

				if (isHomonym) {
					publicCons = new CSMethod (publicCons.Visibility, CSMethodKind.Static, cl.ToCSType (),
								   new CSIdentifier (stubbedName + homonymSuffix), publicCons.Parameters, publicCons.Body);
				}

				string privateConstructorImplName = $"_Xam{stubbedName}CtorImpl" + homonymSuffix;
				var nameIdent = new CSIdentifier (privateConstructorImplName);

				privateConsImpl = new CSMethod (CSVisibility.None, CSMethodKind.Static, CSSimpleType.IntPtr,
								  nameIdent, publicCons.Parameters, new CSCodeBlock ());

				var localIdents = new List<String> {
					publicCons.Name.Name,
					kThisName
				};


				var engine = new MarshalEngine (use, localIdents, TypeMapper, wrapper.Module.SwiftCompilerVersion);
				engine.GenericReferenceNamer = genericNamer;

				privateConsImpl.Body.AddRange(engine.MarshalFunctionCall (wrapperFunc, false, pictorRef,
					publicCons.Parameters, funcDecl, funcDecl.ReturnTypeSpec, CSSimpleType.IntPtr, null, null, false, wrapper));
					
				var privateConsImplID = $"{cl.ToCSType ().ToString ()}.{privateConsImpl.Name.Name}";

				if (isHomonym) {
					// this calls the private constructor implementation which returns a handle to the Swift object.
					// we then pass that to the private factor constructor, which will handle the rest of the work.
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));
					publicCons.Body.Add (CSReturn.ReturnLine (new CSFunctionCall (cl.ToCSType ().ToString (), true,
												      new CSFunctionCall (privateConsImplID, false,
															  publicCons.Parameters.Select (parm => (CSBaseExpression)parm.Name).ToArray ()),
												      new CSFunctionCall ($"{cl.ToCSType ().ToString ()}.GetSwiftMetatype", false),
												      new CSIdentifier ("SwiftObjectRegistry.Registry"))));
				} else {
					// every class should have a protected constructor with the signature
					// swift:
					// .ctor (IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
					// objc:
					// .ctor (IntPtr handle)

					// our implementation should look like this:
					// swift
					// .ctor (args) : base (privateConsCall (), GetSwiftMetatype (), SwiftObjectRegistry.Registry) { }
					// objc
					// .ctor (args) : base (privateConsCall ()) { }


					var isObjC = classDecl.IsObjCOrInheritsObjC (TypeMapper);
					if (!isAssociatedTypeProxy && isObjC)
						publicCons.Body.Add (ImplementNativeObjectCheck ());

					var privateConsCall = new CSFunctionCall (privateConsImplID, false,
										  publicCons.Parameters.Select (parm => (CSBaseExpression)parm.Name).ToArray ());
					var parms = isObjC ?
						new CSBaseExpression [] { privateConsCall } :
						new CSBaseExpression [] {
							privateConsCall,
							new CSFunctionCall ("GetSwiftMetatype", false),
							new CSIdentifier ("SwiftObjectRegistry.Registry")
						};

					var isThisCall = true;
					if (isObjC) {
						var superAsClassDecl = superClassDecl as ClassDeclaration;
						var classAsClassDecl = classDecl as ClassDeclaration;
						// Steve sez: this should never ever ever happen. Ever.
						if (superAsClassDecl == null && classAsClassDecl == null)
							throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 29, "Inconceivable! One of the class or super class should be a class declaration");
						isThisCall = (superAsClassDecl ?? classAsClassDecl).ProtectedObjCCtorIsInThis (TypeMapper);
					}

					publicCons = new CSMethod (publicCons.Visibility, publicCons.Kind, publicCons.Type, publicCons.Name,
								   publicCons.Parameters, parms, isThisCall, publicCons.Body);
				}
			} catch (Exception err) {
				var args = funcDecl.ParameterLists.Last ().Select (parm => $"{parm.PublicName ?? parm.PrivateName}: {parm.TypeName}").InterleaveCommas ();
				var message = $"Error making constructor {classDecl.ToFullyQualifiedName ()} ({args}), skipping. ({err.Message})";
				errors.Add (ErrorHelper.CreateWarning (ReflectorError.kCantHappenBase + 61, err, message));
				yield break;
			}
			yield return privateConsImpl;
			yield return publicCons;


		}

		IEnumerable<CSMethod> ConstructorToMethod (TypeDeclaration classDecl, TypeDeclaration superClassDecl, FunctionDeclaration funcDecl, CSClass cl, CSClass picl, List<string> usedPinvokeNames,
		                                           CSType csType, TLFunction tlf, SwiftClassName superClassName,
		                                           CSUsingPackages use, string libraryPath, WrappingResult wrapper, ErrorHandling errors,
							   bool isAssociatedProxy)
		{
			CSMethod privateCons = null, piConstructor = null, publicCons = null;
			try {
				var homonymSuffix = Homonyms.HomonymSuffix (funcDecl, classDecl.Members.OfType<FunctionDeclaration> (), TypeMapper);
				var classType = tlf.Class;
				var consName = StubbedClassName (superClassName ?? classType.ClassName) + homonymSuffix;
				var pinvokeConsName = PICTorName (classType.ClassName) + homonymSuffix;
				pinvokeConsName = Uniqueify (pinvokeConsName, usedPinvokeNames);
				usedPinvokeNames.Add (pinvokeConsName);

				var pinvokeConsRef = PIClassName (classType.ClassName) + "." + pinvokeConsName;

				piConstructor = TLFCompiler.CompileMethod (funcDecl, use, libraryPath, tlf.MangledName, pinvokeConsName, true, true, false);

				privateCons = TLFCompiler.CompileMethod (funcDecl, use, libraryPath, tlf.MangledName, consName, false, true, false);
				string piCCTorRef = PICCTorReference (classType.ClassName);

				var wrapperFunc = FindEquivalentFunctionDeclarationForWrapperFunction (tlf, TypeMapper, wrapper) ?? funcDecl;

				var swiftCons = tlf.Signature as SwiftConstructorType;

				var localIdents = new List<String> {
					privateCons.Name.Name,
					kThisName
				};

				var engine = new MarshalEngine (use, localIdents, TypeMapper, wrapper.Module.SwiftCompilerVersion);
				engine.MarshalingConstructor = true;

				var returnClassType = swiftCons.ReturnType is SwiftBoundGenericType ?
							       ((SwiftBoundGenericType)swiftCons.ReturnType).BaseType as SwiftClassType :
							       swiftCons.ReturnType as SwiftClassType;

				var isHomonym = homonymSuffix.Length > 0;

				if (isHomonym) {
					privateCons = new CSMethod (privateCons.Visibility, CSMethodKind.Static, csType, privateCons.Name, privateCons.Parameters, privateCons.Body);
				} else {
					string stubbedName = StubbedClassName (superClassName ?? tlf.Class.ClassName);

					string privateConstructorImplName = $"_Xam{stubbedName}CtorImpl" + homonymSuffix;
					var nameIdent = new CSIdentifier (privateConstructorImplName);
					var oldPrivateCons = privateCons;
					privateCons = new CSMethod (CSVisibility.None, CSMethodKind.Static, CSSimpleType.IntPtr, nameIdent, privateCons.Parameters, new CSCodeBlock ());
					var constArgs = oldPrivateCons.Parameters.Select (p => (CSBaseExpression)p.Name).ToArray ();
					var isObjC = classDecl.IsObjCOrInheritsObjC (TypeMapper);
					var parms = isObjC ?
						new CSBaseExpression [] { new CSFunctionCall (privateConstructorImplName, false, constArgs) } :
						new CSBaseExpression [] {
							new CSFunctionCall (privateConstructorImplName, false, constArgs),
							new CSFunctionCall ("GetSwiftMetatype", false),
							new CSIdentifier ("SwiftObjectRegistry.Registry")
						};

					var isThisCall = true;
					if (isObjC) {
						var superAsClassDecl = superClassDecl as ClassDeclaration;
						var classAsClassDecl = classDecl as ClassDeclaration;
						// Steve sez: this should never ever ever happen. Ever.
						if (superAsClassDecl == null && classAsClassDecl == null)
							throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 30, "Inconceivable! One of the class or super class should be a class declaration");
						isThisCall = (superAsClassDecl ?? classAsClassDecl).ProtectedObjCCtorIsInThis (TypeMapper);
					}

					publicCons = new CSMethod (CSVisibility.Public, CSMethodKind.None, null, oldPrivateCons.Name,
					                               oldPrivateCons.Parameters, parms, isThisCall, new CSCodeBlock ());
				}

				privateCons.Body.AddRange (engine.MarshalConstructor (classDecl, cl, funcDecl, wrapperFunc, pinvokeConsRef, privateCons.Parameters,
									funcDecl.ReturnTypeSpec, csType, piCCTorRef, kSwiftObjectGetter, wrapper, isHomonym));
			} catch (Exception err) {
				var args = funcDecl.ParameterLists.Last ().Select (parm => $"{parm.PublicName ?? parm.PrivateName}: {parm.TypeName}").InterleaveCommas ();
				var message = $"Error making constructor {classDecl.ToFullyQualifiedName ()} ({args}), skipping. ({err.Message})";
				errors.Add (ErrorHelper.CreateWarning (ReflectorError.kCantHappenBase + 62, err, message));
				yield break;
			}

			picl.Methods.Add (piConstructor);
			yield return privateCons;
			if (publicCons != null)
				yield return publicCons;
		}

		ICodeElement ImplementNativeObjectCheck ()
		{
			return CSAssignment.Assign ("base.IsDirectBinding", new CSFunctionCall ("SwiftNativeObjectTagAttribute.IsSwiftNativeObject",
												false, new CSIdentifier (kThisName)));
		}

		public static CSBaseExpression BackingFieldAccessor (CSParameter parm)
		{
			return BackingFieldAccessor (parm.Name);
		}

		public static CSBaseExpression BackingFieldAccessor (CSIdentifier id)
		{
			return id.Dot (kSwiftObjectGetter);
		}

		public static CSBaseExpression SafeBackingFieldAccessor (CSIdentifier variable, CSUsingPackages use, string fullClassName, TypeMapper typeMapper)
		{
			var entity = PrepareAndGetEntity (use, fullClassName, typeMapper);
			if (entity == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 65, $"Unable to find entity for type {fullClassName} while accessing backing field.");
			}
			var call = entity.Type.IsObjCOrInheritsObjC (typeMapper) ? "StructMarshal.RetainNSObject" : "StructMarshal.RetainSwiftObject";
			return new CSFunctionCall (call, false, variable);
		}

		public static CSLine SafeReleaser (CSIdentifier varPtr, CSUsingPackages use, string fullClassName, TypeMapper typeMapper)
		{
			var entity = PrepareAndGetEntity (use, fullClassName, typeMapper);
			if (entity == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 82, $"Unable to find entity for type {fullClassName} while emitting object release code.");
			}
			use.AddIfNotPresent (typeof (SwiftCore));
			var call = entity.Type.IsObjCOrInheritsObjC (typeMapper) ? "SwiftCore.ReleaseObjC" : "SwiftCore.Release";
			return CSFunctionCall.FunctionCallLine (call, varPtr);
		}

		public static CSBaseExpression SafeMarshalClassFromIntPtr (CSBaseExpression expr, CSType expectedType, CSUsingPackages use, string fullClassName, TypeMapper typeMapper, bool isObjCProtocol)
		{
			var entity = PrepareAndGetEntity (use, fullClassName, typeMapper);
			if (entity == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 66, $"Unable to find entity for type {fullClassName} while marshaling from swift.");
			}
			if (isObjCProtocol) {
				return new CSFunctionCall ($"ObjCRuntime.Runtime.GetINativeObject<{expectedType.ToString ()}>", false, expr, CSConstant.Val (false));
			} else {
				var call = entity.Type.IsObjCOrInheritsObjC (typeMapper) ? "ObjCRuntime.Runtime.GetNSObject<{0}>" : "SwiftObjectRegistry.Registry.CSObjectForSwiftObject <{0}>";
				return new CSFunctionCall (String.Format (call, expectedType.ToString ()), false, expr);
			}
		}

		public static CSBaseExpression SafeHandleAccessor (CSIdentifier identifier)
		{
			return new CSTernary (identifier != CSConstant.Null, identifier.Dot (new CSIdentifier ("Handle")), new CSIdentifier ("IntPtr.Zero"), false);
		}

		static Entity PrepareAndGetEntity (CSUsingPackages use, string fullClassName, TypeMapper typeMapper)
		{
			use.AddIfNotPresent (typeof (SwiftObjectRegistry));
			use.AddIfNotPresent (typeof (StructMarshal));
			return typeMapper.GetEntityForSwiftClassName (fullClassName);
		}

		public static CSBaseExpression BackingProxyExistentialContainerAccessor (CSParameter parm)
		{
			return parm.Name.Dot (kProxyExistentialContainer);
		}

		List<CSType> GetStructFieldTypes (StructDeclaration structDecl, CSUsingPackages use)
		{
			return structDecl.Members.OfType<PropertyDeclaration> ().Select (pd => TypeMapper.MapType (structDecl, pd.TypeSpec, false, true).ToCSType (use))
					  .ToList ();
		}


		NominalStructTypeDescriptor LoadTypeDescriptor (StructDeclaration structDecl, ModuleInventory modInventory, ClassContents contents, string libPath)
		{
			var stm = new FileStream (libPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			MachOFile macho = null;
			try {
				macho = MachO.Read (stm, null).FirstOrDefault (mf => mf.Architecture == modInventory.Architecture);
				// MachO.Read causes the stm to be closed/disposed
			} catch (Exception e) {
				stm.Close ();
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 31, e, $"Error reading '{libPath}': {e.Message}");
			}
			if (macho == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 32, "Unable to find a library of architecture " + modInventory.Architecture);
			using (stm = new FileStream (libPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				var osstm = new OffsetStream (stm, macho.StartOffset);

				var desc = NominalStructTypeDescriptor.FromStream (osstm, contents.TypeDescriptor,
				                                                   contents.DirectMetadata,
				                                                   MachinePointerSize (macho.Architecture));
				return desc;
			}
		}

		static int MachinePointerSize (MachO.Architectures arch)
		{
			switch (arch) {
			case MachO.Architectures.ARM64:
			case MachO.Architectures.x86_64:
				return 8;
			default:
				return 4;
			}
		}


		void ImplementTrivialEnumCtors (CSClass en, CSClass picl, List<string> usedPinvokeNames, EnumDeclaration enumDecl,
							      ClassContents classContents, CSUsingPackages use,
							      WrappingResult wrapper, string swiftLibraryPath, ErrorHandling errors)
		{
			var allCtors = enumDecl.AllConstructors ().Where (ct => ct.IsPublicOrOpen).ToList ();
			foreach (var funcDecl in allCtors) {
				var recastCtor = RecastEnumCtorAsStaticFactory (enumDecl, funcDecl);
				var isOptional = IsOptional (funcDecl.ReturnTypeSpec);
				var optionalSuffix = isOptional ? "Optional" : "";

				var homonymSuffix = Homonyms.HomonymSuffix (funcDecl, enumDecl.Members.OfType<FunctionDeclaration> (), TypeMapper);
				var libraryPath = PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath);
				var consName = "Init" + optionalSuffix + homonymSuffix;
				var pinvokeConsName = PICTorName (enumDecl.ToFullyQualifiedName (), TypeMapper) + optionalSuffix + homonymSuffix;
				var csReturnType = TypeMapper.MapType (funcDecl, funcDecl.ReturnTypeSpec, isPinvoke: false, isReturnValue: true).ToCSType (use);

				pinvokeConsName = Uniqueify (pinvokeConsName, usedPinvokeNames);
				usedPinvokeNames.Add (pinvokeConsName);

				var pinvokeConsRef = PIClassName (TypeMapper.GetDotNetNameForSwiftClassName (enumDecl.ToFullyQualifiedName ())) + "." + pinvokeConsName;

				var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
				var wrapperFunc = finder.FindWrapperForConstructor (enumDecl, funcDecl);
				if (wrapperFunc == null) {
					var ex = ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 88, $"Unable to find wrapper mapper.GetDotNetNameForSwiftClassName (fullSwiftClassName)FunctionDeclaration for wrapper function {funcDecl} in enum {enumDecl.ToFullyQualifiedName ()}, skipping.");
					errors.Add (ex);
					continue;
				}

				var wrapperFunction = FindEquivalentTLFunctionForWrapperFunction (wrapperFunc, TypeMapper, wrapper);
				if (wrapperFunction == null) {
					var ex = ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 87, $"Unable to find matching constructor declaration for {funcDecl} in enum {enumDecl.ToFullyQualifiedName ()}, skipping.");
					errors.Add (ex);
					continue;
				}
				try {
					var piConstructor = TLFCompiler.CompileMethod (wrapperFunc, use,
						libraryPath, wrapperFunction.MangledName, pinvokeConsName, true, true, false);

					var publicCons = TLFCompiler.CompileMethod (recastCtor, use, libraryPath, wrapperFunction.MangledName,
						consName, isPinvoke: false, isFinal: true, isStatic: true);
					var piCCTorName = PICCTorName (enumDecl.ToFullyQualifiedName (), TypeMapper);

					var localIdents = new List<String> {
						publicCons.Name.Name,
						piConstructor.Name.Name,
						piCCTorName,
						kThisName
					};

					var engine = new MarshalEngine (use, localIdents, TypeMapper, wrapper.Module.SwiftCompilerVersion);

					publicCons.Body.AddRange (engine.MarshalFunctionCall (wrapperFunc, false, pinvokeConsRef,
						publicCons.Parameters, enumDecl, funcDecl.ReturnTypeSpec, csReturnType, swiftInstanceType: null,
						instanceType: null, includeCastToReturnType: true, wrapper, includeIntermediateCastToLong: true));

					en.Methods.Add (publicCons);
					picl.Methods.Add (piConstructor);
				} catch (Exception err) {
					var ex = ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 88, err, $"Exception thrown marshaling to swift in enum constructor {recastCtor}");
					errors.Add (ex);
				}
			}
		}


		void ImplementMethods (CSClass cl, CSClass picl, List<string> usedPinvokeNames, SwiftClassName piClassName, ClassContents contents,
		                       TypeDeclaration typeDecl, CSUsingPackages use, WrappingResult wrapper, Func<FunctionDeclaration, bool> funcFilter,
		                       string swiftLibraryPath, ErrorHandling errors)
		{

			foreach (var funcDecl in typeDecl.AllMethodsNoCDTor ()) {
				if (funcDecl.IsProperty || funcDecl.IsSubscript)
					continue;
				if (funcDecl.Access.IsPrivateOrInternal ())
					continue;
				if (funcDecl.IsDeprecated || funcDecl.IsUnavailable)
					continue;

				bool isFinal = funcDecl.IsFinal;
				var ent = TypeMapper.GetEntityForSwiftClassName (funcDecl.Parent.ToFullyQualifiedName (true));
				if (ent.IsStructOrEnum && (funcDecl.Name == "__derived_enum_equals" || funcDecl.Name == "__derived_struct_equals"))
					continue;
				if (ent.Type.Access != Accessibility.Open)
					isFinal = true;

				try {
					string homonymSuffix = Homonyms.HomonymSuffix (funcDecl, typeDecl.Members.OfType<FunctionDeclaration> (), TypeMapper);
					ImplementOverload (cl, picl, usedPinvokeNames, piClassName, funcDecl, use, isFinal, wrapper, funcFilter, swiftLibraryPath, homonymSuffix);
				} catch (RuntimeException err) {
					var message = $"An error occurred while creating C# function for {funcDecl.ToFullyQualifiedName ()}, skipping. ({err.Message})";
					err = new RuntimeException (err.Code, false, err, message);
					errors.Add (err);
				} catch (Exception anythingElse) {
					var message = $"An error occurred while creating C# function for {funcDecl.ToFullyQualifiedName ()}, skipping. ({anythingElse.Message})";
					var err = new RuntimeException (ReflectorError.kCantHappenBase + 33, false, anythingElse, message);
					errors.Add (err);
				}
			}
		}

		void ImplementProperties (CSClass cl, CSClass picl, List<string> usedPinvokeNames, TypeDeclaration decl, ClassContents contents, SwiftClassName pinvokeName,
		                          CSUsingPackages use, WrappingResult wrapper, bool isStruct, bool isTrivialEnum,
		                          Func<FunctionDeclaration, bool> funcFilter, string swiftLibraryPath, ErrorHandling errors)
		{
			var usedIdentifiers = new List<string> { cl.Name.Name };

			foreach (var propDecl in decl.AllProperties ().Where (p => p.IsPublicOrOpen)) {
				if (propDecl.IsDeprecated || propDecl.IsUnavailable)
					continue;
				var propInventory = propDecl.IsStatic ? contents.StaticProperties : contents.Properties;
				var prop = propInventory.PropertyWithName (propDecl.Name);
				if (prop == null) {
					var message = $"Unable to find PropertyInventory for property declaration {propDecl.ToFullyQualifiedName (true)}";
					var ex = ErrorHelper.CreateWarning (ReflectorError.kCompilerReferenceBase + 68, message);
					errors.Add (ex);
					continue;
				}
				try {
					if (isTrivialEnum) {
						ImplementTrivialEnumProperty (cl, picl, usedPinvokeNames, decl, propDecl, use, wrapper, usedIdentifiers, swiftLibraryPath);
					} else {
						ImplementProperty (cl, picl, usedPinvokeNames, pinvokeName ?? contents.Name, propDecl, use, wrapper, isStruct, funcFilter, usedIdentifiers, swiftLibraryPath);
					}
				} catch (RuntimeException err) {
					var message = $"An error occurred while creating C# property for {prop.Class.ClassName.ToString ()}.{prop.Name.Name}, skipping. ({err.Message})";
					err = new RuntimeException (err.Code, false, err, message);
					errors.Add (err);
				} catch (Exception anythingElse) {
					var message = $"An error occurred while creating C# property for {prop.Class.ClassName.ToString ()}.{prop.Name.Name}, skipping. ({anythingElse.Message})";
					var err = new RuntimeException (ReflectorError.kCompilerReferenceBase + 69, false, anythingElse, message);
					errors.Add (err);
				}
			}
		}

		void ImplementSubscripts (CSClass cl, CSClass picl, List<string> usedPinvokeNames, BaseDeclaration parent, List<SubscriptDeclaration> subScripts, ClassContents contents,
		                          SwiftClassName pinvokeName, CSUsingPackages use, WrappingResult wrapper, bool isStruct, Func<FunctionDeclaration, bool> funcFilter,
		                          string swiftLibraryPath, ErrorHandling errors)
		{
			foreach (SubscriptDeclaration subDecl in subScripts) {
				try {
					if ((subDecl.Getter != null && subDecl.Getter.Access == Accessibility.Public || subDecl.Getter.Access == Accessibility.Open) ||
					    (subDecl.Setter != null && subDecl.Setter.Access == Accessibility.Public || subDecl.Setter.Access == Accessibility.Open))
						ImplementSubscript (cl, picl, usedPinvokeNames, parent, subDecl, contents, pinvokeName, use, wrapper, isStruct, funcFilter, swiftLibraryPath);
				} catch (RuntimeException err) {
					var message = $"An error occurred while creating C# indexer for {contents.Name.ToFullyQualifiedName ()}, skipping. ({err.Message}).";
					err = new RuntimeException (err.Code, false, err, err.Message);
					errors.Add (err);
				} catch (Exception anythingElse) {
					var message = $"An error occurred while creating C# indexer for {contents.Name.ToFullyQualifiedName ()}, skipping. ({anythingElse.Message}).";
					var err = new RuntimeException (ReflectorError.kCompilerReferenceBase + 70, false, anythingElse, anythingElse.Message);
					errors.Add (err);
				}
			}
		}

		void ImplementSubscript (CSClass cl, CSClass picl, List<string> usedPinvokeNames, BaseDeclaration parent, SubscriptDeclaration subDecl, ClassContents contents,
		                         SwiftClassName pinvokeName, CSUsingPackages use, WrappingResult wrapper, bool isStruct,
		                         Func<FunctionDeclaration, bool> funcFilter, string swiftLibraryPath)
		{
			funcFilter = funcFilter ?? (tlf => true);
			if (subDecl.Getter == null && subDecl.Setter == null)
				return; // uhhh...should never happen?
			// this method is only for non-virtual subscripts
			if (subDecl.Getter?.Access == Accessibility.Open || subDecl.Setter?.Access == Accessibility.Open)
				return;

			// need to check the return type of the getter (always present) and any of its arguments
			var anyProtoList = false;
			if (subDecl.Getter != null) {
				anyProtoList = subDecl.Getter.ReturnTypeSpec is ProtocolListTypeSpec ||
								subDecl.Getter.ParameterLists.Last ().Any (p => p.TypeSpec is ProtocolListTypeSpec);
			} else {
				anyProtoList = subDecl.Setter.ParameterLists.Last ().Any (p => p.TypeSpec is ProtocolListTypeSpec);
			}

			var forcePrivate = !anyProtoList;

			TLFunction getterWrapper = null;
			TLFunction setterWrapper = null;

			var propName = "this";
			var subscriptPrefix = anyProtoList ? "" : "__";
			var propGetName = $"{subscriptPrefix}GetSubscript";
			propGetName = Uniqueify (propGetName, usedPinvokeNames);
			usedPinvokeNames.Add (propGetName);

			var propSetName = $"{subscriptPrefix}SetSubscript";
			propSetName = Uniqueify (propSetName, usedPinvokeNames);
			usedPinvokeNames.Add (propSetName);

			if ((subDecl.Getter != null && !funcFilter (subDecl.Getter)) ||
				(subDecl.Setter != null && !funcFilter (subDecl.Setter)))
				return;

			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);

			if (subDecl.Getter != null && funcFilter (subDecl.Getter)) {
				var getterWrapperFunc = finder.FindWrapperForMethod (parent, subDecl.Getter, PropertyType.Getter);

				getterWrapper = getterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (getterWrapperFunc, TypeMapper, wrapper) : null;
				if (getterWrapper == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 71, $"Unable to find wrapper function for subscript getter in class {parent.ToFullyQualifiedName (true)}.");
				}

				ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, pinvokeName ?? contents.Name, subDecl.Getter, use, true, wrapper, swiftLibraryPath,
								  getterWrapper, "", forcePrivate, propGetName);
			}

			if (subDecl.Setter != null && funcFilter (subDecl.Setter)) {
				var setterWrapperFunc = finder.FindWrapperForMethod (parent, subDecl.Setter, PropertyType.Setter);

				setterWrapper = setterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (setterWrapperFunc, TypeMapper, wrapper) : null;
				if (setterWrapper == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 72, $"Wnable to find wrapper function for subscript setter in class {parent.ToFullyQualifiedName (true)}.");
				}

				ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, pinvokeName ?? contents.Name, subDecl.Setter, use, true, wrapper, swiftLibraryPath,
												  setterWrapper, "", forcePrivate, propSetName);
			}

			if (anyProtoList)
				return;

			var wrapperProp = TLFCompiler.CompileProperty (use, propName, subDecl.Getter, subDecl.Setter);


			if (subDecl.Getter != null) {
				wrapperProp.Getter.Add (CSReturn.ReturnLine (
					new CSFunctionCall (propGetName, false, wrapperProp.IndexerParameters.Select (p => (CSBaseExpression)p.Name).ToArray ())));
			}

			if (subDecl.Setter != null) {
				var callParms = wrapperProp.IndexerParameters.Select (p => (CSBaseExpression)p.Name).ToList ();
				callParms.Insert (0, new CSIdentifier ("value"));
				wrapperProp.Setter.Add (CSFunctionCall.FunctionCallLine (propSetName, false, callParms.ToArray ()));
			}

			cl.Properties.Add (wrapperProp);

		}

		void AddDynamicSelfGenericToInterface (CSInterface iface)
		{
			// inserts TSelf to interface:
			// public interface ISomeSelf<TSelf, More, Generics> where TSelf: ISomeSelf<TSelf, More, Generics> {
			// }
			iface.GenericParams.Insert (0, new CSGenericTypeDeclaration (kGenericSelf));
			var constraintType = iface.ToCSType ().ToString ();
			iface.GenericConstraints.Insert (0, new CSGenericConstraint (kGenericSelf, new CSIdentifier (constraintType)));
		}

		void AddGenericsToInterface (CSInterface iface, ProtocolDeclaration proto, CSUsingPackages use)
		{
			if (!proto.HasAssociatedTypes)
				return;

			foreach (var assocType in proto.AssociatedTypes) {
				var genName = new CSIdentifier (OverrideBuilder.GenericAssociatedTypeName (assocType));
				iface.GenericParams.Add (new CSGenericTypeDeclaration (genName));
				if (assocType.SuperClass != null) {
					var csTypeName = CSTypeNameFromTypeSpec (proto, assocType.SuperClass, use);
					iface.GenericConstraints.Add (new CSGenericConstraint (genName, new CSIdentifier (csTypeName)));
				} else if (assocType.ConformingProtocols.Count > 0) {
					var constraints = new List<CSIdentifier> ();
					foreach (var conformProto in assocType.ConformingProtocols) {
						var csTypeName = CSTypeNameFromTypeSpec (proto, conformProto, use);
						constraints.Add (new CSIdentifier (csTypeName));
					}
					iface.GenericConstraints.Add (new CSGenericConstraint (genName, constraints));
				}
			}
		}

		string CSTypeNameFromTypeSpec (BaseDeclaration context, TypeSpec spec, CSUsingPackages use)
		{
			var ntb = TypeMapper.MapType (context, spec, false);
			var csType = ntb.ToCSType (use);
			var csTypeName = csType.ToString ();
			return csTypeName;
		}

		TLFunction FindSubscriptFoo (FunctionDeclaration decl, List<TLFunction> funcs, PropertyType propType)
		{
			if (decl == null)
				return null;
			var actualParams = decl.ParameterLists [decl.ParameterLists.Count == 1 ? 0 : 1];

			foreach (TLFunction target in funcs) {
				var prop = target.Signature as SwiftPropertyType;
				if (prop.PropertyType != propType)
					continue;
				if (prop.ParameterCount != actualParams.Count)
					continue;
				if (!ParamsMatch (decl, actualParams, prop))
					continue;
				if (TypesMatch (decl, decl.ReturnTypeSpec, prop.ReturnType))
					return target;
			}
			return null;
		}

		static bool ParamsMatch (FunctionDeclaration parentDecl, List<ParameterItem> parms, SwiftPropertyType prop)
		{
			for (int i = 0; i < parms.Count; i++) {
				if (!TypesMatch (parentDecl, parms [i].TypeSpec, prop.GetParameter (i)))
					return false;
			}
			return true;
		}

		static bool TypesMatch (FunctionDeclaration parentDecl, TypeSpec spec, SwiftType type)
		{
			if (spec == null && type == null)
				return true;
			if (spec == null || type == null)
				return false;
			if (spec.IsInOut != type.IsReference)
				return false;
			switch (spec.Kind) {
			case TypeSpecKind.Tuple:
				return TypesMatch (parentDecl, spec as TupleTypeSpec, type as SwiftTupleType);
			case TypeSpecKind.Closure:
				return TypesMatch (parentDecl, spec as ClosureTypeSpec, type as SwiftBaseFunctionType);
			case TypeSpecKind.Named:
				return TypesMatch (parentDecl, spec as NamedTypeSpec, type);
			case TypeSpecKind.ProtocolList:
				return TypesMatch (parentDecl, spec as ProtocolListTypeSpec, type as SwiftProtocolListType);
			default:
				throw new ArgumentOutOfRangeException (nameof (spec));
			}
		}

		static bool TypesMatch (FunctionDeclaration parentDecl, TupleTypeSpec spec, SwiftTupleType tuple)
		{
			if (tuple == null)
				return false;

			if (spec.Elements.Count != tuple.Contents.Count)
				return false;

			for (int i = 0; i < spec.Elements.Count; i++) {
				if (!TypesMatch (parentDecl, spec.Elements [i], tuple.Contents [i]))
					return false;
			}
			return true;
		}

		static bool TypesMatch (FunctionDeclaration parentDecl, ProtocolListTypeSpec spec, SwiftProtocolListType protos)
		{
			if (protos == null)
				return false;
			if (spec.Protocols.Count != protos.Protocols.Count)
				return false;

			var protoTypes = spec.Protocols.Keys;
			for (int i=0; i < protoTypes.Count; i++) {
				if (!TypesMatch (parentDecl, protoTypes [i], protos.Protocols [i]))
					return false;
			}
			return true;
		}

		static bool TypesMatch (FunctionDeclaration parentDecl, ClosureTypeSpec spec, SwiftBaseFunctionType func)
		{
			if (func == null)
				return false;
			if (!TypesMatch (parentDecl, spec.ReturnType, func.ReturnType))
				return false;
			return TypesMatch (parentDecl, spec.Arguments, func.Parameters);
		}

		static bool TypesMatch (FunctionDeclaration parentDecl, NamedTypeSpec spec, SwiftType type)
		{
			switch (type.Type) {
			case CoreCompoundType.Scalar:
				{
					var bit = type as SwiftBuiltInType;
					switch (bit.BuiltInType) {
					case CoreBuiltInType.Bool:
						return spec.Name == "Swift.Bool";
					case CoreBuiltInType.Double:
						return spec.Name == "Swift.Double";
					case CoreBuiltInType.Float:
						return spec.Name == "Swift.Float";
					case CoreBuiltInType.Int:
						return spec.Name == "Swift.Int";
					case CoreBuiltInType.UInt:
						return spec.Name == "Swift.UInt";
					default:
						return false;
					}
				}
			case CoreCompoundType.Class:
			case CoreCompoundType.Struct:
				{
					var cl = type as SwiftClassType;
					return spec.Name == cl.ClassName.ToFullyQualifiedName ();
				}
			case CoreCompoundType.BoundGeneric:
				{
					var bgt = type as SwiftBoundGenericType;
					return TypesMatch (parentDecl, spec, bgt);
				}
			case CoreCompoundType.GenericReference:
				{
					if (!parentDecl.IsTypeSpecGeneric (spec))
						return false;
					var argRef = type as SwiftGenericArgReferenceType;
					var depthIndex = parentDecl.GetGenericDepthAndIndex (spec);
					return argRef.Depth == depthIndex.Item1 && argRef.Index == depthIndex.Item2;
				}
			default:
				return false;
			}
		}

		static bool TypesMatch (FunctionDeclaration parentDecl, NamedTypeSpec spec, SwiftBoundGenericType boundGenericType)
		{
			if (spec.GenericParameters.Count != boundGenericType.BoundTypes.Count)
				return false;
			var baseClass = boundGenericType.BaseType as SwiftClassType;
			if (baseClass == null)
				return false;
			if (spec.Name != baseClass.ClassName.ToFullyQualifiedName ())
				return false;
			for (int i = 0; i < spec.GenericParameters.Count; i++) {
				if (!TypesMatch (parentDecl, spec.GenericParameters [i], boundGenericType.BoundTypes [i]))
					return false;
			}
			return true;
		}

		void ImplementTrivialEnumProperty (CSClass cl, CSClass picl, List<string> usedPinvokeNames, BaseDeclaration parent, PropertyDeclaration propDecl, 
		                                   CSUsingPackages use, WrappingResult wrapper, List<string> usedIdentifiers, string swiftLibraryPath)
		{
			var propGetter = propDecl.GetGetter ();
			var propSetter = propDecl.GetSetter ();
			if (propGetter == null && propSetter == null)
				return; // uhhh...should never happen?



			TLFunction getterWrapper = null;
			FunctionDeclaration getterWrapperFunc = null;
			TLFunction setterWrapper = null;
			FunctionDeclaration setterWrapperFunc = null;
			CSMethod piGetter = null;
			CSMethod piSetter = null;
			string piGetterName = null;
			string piSetterName = null;
			string piGetterRef = null;

			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);

			if (propGetter != null) {
				getterWrapperFunc = finder.FindWrapperForMethod (parent, propGetter, PropertyType.Getter);
				getterWrapper = getterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (getterWrapperFunc, TypeMapper, wrapper) : null;
				if (getterWrapper == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 73, $"Unable to find wrapper function for getter for property {propGetter.PropertyName} in class {parent.ToFullyQualifiedName (true)}.");
				}

				piGetterName = PIMethodName (parent.ToFullyQualifiedName (), getterWrapper.Name, PropertyType.Getter);
				piGetterName = Uniqueify (piGetterName, usedPinvokeNames);
				usedPinvokeNames.Add (piGetterName);

				piGetterRef = PIClassName (parent.ToFullyQualifiedName ()) + "." + piGetterName;

				piGetter = TLFCompiler.CompileMethod (getterWrapperFunc, use, PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
							getterWrapper.MangledName, piGetterName, true, true, true);
			}

			if (propSetter != null) {
				setterWrapperFunc = finder.FindWrapperForMethod (parent, propSetter, PropertyType.Setter);
				setterWrapper = setterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (setterWrapperFunc, TypeMapper, wrapper) : null;
				if (setterWrapper == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 74, $"Unable to find wrapper function for setter for property {propGetter.PropertyName} in class {parent.ToFullyQualifiedName (true)}.");
				}

				piSetterName = PIMethodName (parent.ToFullyQualifiedName (), setterWrapper.Name, PropertyType.Setter);
				piSetterName = Uniqueify (piSetterName, usedPinvokeNames);
				usedPinvokeNames.Add (piSetterName);

				piSetter = TLFCompiler.CompileMethod (setterWrapperFunc, use, wrapper.ModuleLibPath,
					setterWrapper.MangledName, piSetterName, true, true, true);
			}


			string propName = TypeMapper.SanitizeIdentifier (propGetter.PropertyName);
			propName = MarshalEngine.Uniqueify (propName, usedIdentifiers);
			usedIdentifiers.Add (propName);

			string getterName = propName;
			string setterName = "Set" + propName;
			var useLocals = new List<string> {
				getterName,
				setterName,
				cl.Name.Name
			};

			if (piGetter != null)
				picl.Methods.Add (piGetter);
			if (piSetter != null)
				picl.Methods.Add (piSetter);


			if (propGetter != null) {
				var marshaler = new MarshalEngine (use, useLocals, TypeMapper, wrapper.Module.SwiftCompilerVersion);

				var getter = TLFCompiler.CompileMethod (propDecl.GetGetter (), use, null, null, getterName, false, false, true);

				var thisTypeSpec = new NamedTypeSpec (propDecl.Parent.ToFullyQualifiedNameWithGenerics ());
				var ntb = TypeMapper.MapType (propDecl, thisTypeSpec, false);
				var thisCSType = ntb.ToCSType (use);

				getter.Parameters.Add (new CSParameter (thisCSType, new CSIdentifier ("this0"), propDecl.Parent.IsNested ? CSParameterKind.None : CSParameterKind.This));

				getter.Body.AddRange (marshaler.MarshalFunctionCall (getterWrapperFunc, false, piGetterRef, getter.Parameters,
					propDecl.GetGetter (), propDecl.GetGetter ().ReturnTypeSpec, getter.Type, null, null, false, wrapper));

				cl.Methods.Add (getter);

			}

		}

		void ImplementProperty (CSClass cl, CSClass picl, List<string> usedPinvokeNames, SwiftClassName pinvokeName, PropertyDeclaration propDecl,
				       CSUsingPackages use, WrappingResult wrapper, bool isStruct, Func<FunctionDeclaration, bool> funcFilter,
		                        List<string> usedIdentifiers, string swiftLibraryPath)
		{
			funcFilter = funcFilter ?? (tlf => true);

			var propGetter = propDecl.GetGetter ();
			var propSetter = propDecl.GetSetter ();

			if (propGetter == null && propSetter == null)
				return; // uhhh...should never happen? - verified - all props in swift must have at least a getter

			TLFunction getterWrapper = null;
			TLFunction setterWrapper = null;
			string propName = TypeMapper.SanitizeIdentifier (propGetter.PropertyName);
			propName = MarshalEngine.Uniqueify (propName, usedIdentifiers);
			usedIdentifiers.Add (propName);
			var getterName = $"__Get{propName}";
			getterName = Uniqueify (getterName, usedIdentifiers);
			usedIdentifiers.Add (getterName);

			var setterName = $"__Set{propName}";
			setterName = Uniqueify (setterName, usedIdentifiers);
			usedIdentifiers.Add (setterName);

			if ((propGetter != null && !funcFilter (propGetter)) ||
				propSetter != null && !funcFilter (propSetter))
				return;

			if (propDecl.IsDeprecated || propDecl.IsUnavailable)
				return;

			if (!propDecl.IsPublicOrOpen)
				return;


			var finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);

			if (propGetter != null && propGetter.IsPublicOrOpen) {
				var getterWrapperFunc = finder.FindWrapperForMethod (propDecl.Parent, propGetter, PropertyType.Getter);
				getterWrapper = getterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (getterWrapperFunc, TypeMapper, wrapper) : null;
				if (getterWrapper == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 75, $"Unable to find wrapper function for getter for property {propDecl.Name} in class {propDecl.Parent.ToFullyQualifiedName (true)}.");
				}
				ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, pinvokeName, propDecl.GetGetter (), use, true, wrapper, swiftLibraryPath,
				                                   getterWrapper, "", true, getterName);
			}

			if (propSetter != null && propSetter.IsPublicOrOpen) {
				var setterWrapperFunc = finder.FindWrapperForMethod (propDecl.Parent, propSetter, PropertyType.Setter);
				setterWrapper = setterWrapperFunc != null ? FindEquivalentTLFunctionForWrapperFunction (setterWrapperFunc, TypeMapper, wrapper) : null;

				if (setterWrapper == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 76, $"Unable to find wrapper function for setter for property {propDecl.Name} in class {propDecl.Parent.ToFullyQualifiedName (true)}.");
				}

				var csSetterImpl = ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, pinvokeName, propDecl.GetSetter (), use, true, wrapper, swiftLibraryPath,
				                                   setterWrapper, "", true, setterName);
				if (propGetter.ReturnTypeSpec is ProtocolListTypeSpec) {
					// in the case of protocol list type, we need to change the implementation to
					// a non-generic version with the value type of object.
					var nonGeneric = CSMethod.RemoveGenerics (csSetterImpl);
					nonGeneric.Parameters [0] = new CSParameter (CSSimpleType.Object, nonGeneric.Parameters [0].Name, nonGeneric.Parameters [0].ParameterKind);
					cl.Methods.Remove (csSetterImpl);
					cl.Methods.Add (nonGeneric);
				}
			}

			var wrapperProp = TLFCompiler.CompileProperty (use, propName, propGetter, propSetter, propGetter.IsStatic ? CSMethodKind.Static : CSMethodKind.None);


			if (propGetter != null) {
				wrapperProp.Getter.Add (CSReturn.ReturnLine (new CSFunctionCall (getterName, false)));
			}

			if (propSetter != null) {
				wrapperProp.Setter.Add (CSFunctionCall.FunctionCallLine (setterName, false, new CSIdentifier ("value")));
			}

			cl.Properties.Add (wrapperProp);
		}


		void ImplementOverload (CSClass cl, CSClass picl, List<string> usedPinvokeNames, SwiftClassName piClassName, FunctionDeclaration funcToWrap,
		                        CSUsingPackages use, bool isFinal, WrappingResult wrapper, Func<FunctionDeclaration, bool> funcFilter,
		                        string swiftLibraryPath, string homonymSuffix)
		{
			funcFilter = funcFilter ?? (tlf => true);
			if (!funcFilter (funcToWrap))
				return;

			FunctionDeclarationWrapperFinder finder = new FunctionDeclarationWrapperFinder (TypeMapper, wrapper);
			var wrapperFunc = finder.FindWrapper (funcToWrap);

			var wrapperFunction = FindEquivalentTLFunctionForWrapperFunction (wrapperFunc, TypeMapper, wrapper);

			ImplementOverloadFromKnownWrapper (cl, picl, usedPinvokeNames, piClassName, funcToWrap, use, isFinal, wrapper, swiftLibraryPath, wrapperFunction, homonymSuffix);
		}

		void ImplementVirtualMethod (CSClass cl, FunctionDeclaration funcDecl, string superCallName, CSUsingPackages use,
		                             WrappingResult wrapper, ref CSMethod publicMethod, string swiftLibraryPath, string homonymSuffix)
		{
			publicMethod = TLFCompiler.CompileMethod (funcDecl, use, PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
				mangledName: "", null, false, false, false);

			var genericParameters = publicMethod.GenericParameters;
			var genericConstraints = publicMethod.GenericConstraints;


			publicMethod = CSMethod.CopyGenerics (publicMethod, new CSMethod (publicMethod.Visibility, CSMethodKind.Virtual, publicMethod.Type,
			                             new CSIdentifier(publicMethod.Name.Name + homonymSuffix), publicMethod.Parameters,
			                             publicMethod.Body));
			var call = new CSFunctionCall (superCallName + homonymSuffix, false, publicMethod.Parameters.Select (p => p.Name).ToArray ());
			publicMethod.Body.Add (publicMethod.Type == null || publicMethod.Type == CSSimpleType.Void ? new CSLine (call) : CSReturn.ReturnLine (call));
			cl.Methods.Add (publicMethod);
		}

		CSMethod ImplementOverloadFromKnownWrapper (CSClass cl, CSClass picl, List<string> usedPinvokeNames, FunctionDeclaration methodToWrap, CSUsingPackages use, bool isFinal,
			WrappingResult wrapper, string swiftLibraryPath, TLFunction wrapperFunction, bool forcePrivate = false,
			Func<int, int, string> genericReferenceNamer = null, bool restoreDynamicSelf = false)
		{
			var wrapperFunctionDecl = FindEquivalentFunctionDeclarationForWrapperFunction (wrapperFunction, TypeMapper, wrapper);
			var className = TypeMapper.GetDotNetNameForSwiftClassName (methodToWrap.Parent.ToFullyQualifiedName (true));
			if (className == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 78, $"Unable to find C# class name for swift class name '{methodToWrap.Parent.ToFullyQualifiedName ()}'.");
			}
			var pinvokeMethodName = PIMethodName (methodToWrap.Parent.ToFullyQualifiedName (true), wrapperFunction.Name);
			pinvokeMethodName = Uniqueify (pinvokeMethodName, usedPinvokeNames);
			usedPinvokeNames.Add (pinvokeMethodName);

			string pinvokeMethodRef = picl.Name + "." + pinvokeMethodName;

			bool isStatic = methodToWrap.IsStatic || methodToWrap.IsExtension;

			var piMethod = TLFCompiler.CompileMethod (wrapperFunctionDecl, use, PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
				wrapperFunction.MangledName, pinvokeMethodName, true, true, isStatic);

			string methodName = null;
			if (methodToWrap.IsExtension && methodToWrap.IsProperty) {
				methodName = (methodToWrap.IsGetter ? "Get" : "Set") + TypeMapper.SanitizeIdentifier (methodToWrap.PropertyName);
			} else {
				methodName = TypeMapper.SanitizeIdentifier (methodToWrap.Name);
			}

			var publicMethod = TLFCompiler.CompileMethod (methodToWrap, use, PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
				null, methodName, false, isFinal, isStatic);

			if (forcePrivate) {
				publicMethod = new CSMethod (CSVisibility.None, publicMethod.Kind, publicMethod.Type,
							publicMethod.Name, publicMethod.Parameters, publicMethod.Body);
			}

			var localIdentifiers = new List<string> {
				pinvokeMethodName,
				cl.Name.Name,
				publicMethod.Name.Name
			};

			var marshaler = new MarshalEngine (use, localIdentifiers, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			marshaler.GenericReferenceNamer = genericReferenceNamer;
			var instanceTypeSpec = methodToWrap.ParameterLists.Count > 1 ? methodToWrap.ParameterLists [0] [0].TypeSpec : null;
			CSType csInstanceType = null;
			if (instanceTypeSpec != null) {
				if (methodToWrap.IsExtension) {
					if (methodToWrap.IsStatic) {
						instanceTypeSpec = null; // no instance on static
					} else {
						var entity = TypeMapper.GetEntityForTypeSpec (methodToWrap.ParentExtension.ExtensionOnType);
						if (entity == null)
							throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 79, $"Unable to find entity for extention on {methodToWrap.ParentExtension.ExtensionOnTypeName} while creating C# method");
						csInstanceType = TypeMapper.MapType (methodToWrap, instanceTypeSpec, false).ToCSType (use);
					}
					
				} else {
					if (methodToWrap.IsProtocolMember) {
						csInstanceType = TypeMapper.MapType (methodToWrap, instanceTypeSpec, false).ToCSType (use);
					} else {
						csInstanceType = cl.ToCSType ();
					}
				}
			}

			var methodContents = marshaler.MarshalFunctionCall (wrapperFunctionDecl, methodToWrap.IsExtension, pinvokeMethodRef, publicMethod.Parameters,
										   methodToWrap, methodToWrap.ReturnTypeSpec, publicMethod.Type,
										   instanceTypeSpec, csInstanceType,
										   false, wrapper, methodToWrap.HasThrows, restoreDynamicSelf: restoreDynamicSelf);

			if (methodToWrap.IsProtocolMember || genericReferenceNamer != null) {
				var ifRedirect = InterfaceMethodRedirect (publicMethod, methodContents);
				publicMethod.Body.Add (ifRedirect);
			} else {
				publicMethod.Body.AddRange (methodContents);
			}


			picl.Methods.Add (piMethod);

			if (methodToWrap.IsExtension && !methodToWrap.IsStatic) {
				var thisOrRef = methodToWrap.IsSubscriptSetter ? "ref " : "this ";
				var selfType = new CSSimpleType (thisOrRef + csInstanceType.ToString ());
				var selfParam = new CSParameter (selfType, new CSIdentifier ("self"));
				publicMethod.Parameters.Insert (0, selfParam);
			}

			cl.Methods.Add (publicMethod);
			return publicMethod;
		}

		CSIfElse InterfaceMethodRedirect (CSMethod publicMethod, IEnumerable<ICodeElement> elseContents)
		{
			var test = kInterfaceImpl != CSConstant.Null;
			var callArgs = ParametersToParameterExpressions ($"redirecting to interface method {publicMethod.Name}", publicMethod.Parameters).ToArray ();
			var callSite = new CSFunctionCall ($"{kInterfaceImplName}.{publicMethod.Name.Name}", false, callArgs);

			var ifBody = new CSCodeBlock ();
			var elseBody = new CSCodeBlock (elseContents);

			if (publicMethod.Type != null && publicMethod.Type != CSSimpleType.Void)
				ifBody.Add (CSReturn.ReturnLine (callSite));
			else
				ifBody.Add (new CSLine (callSite));
			return new CSIfElse (test, ifBody, elseBody);
		}


		CSMethod ImplementOverloadFromKnownWrapper (CSClass cl, CSClass picl, List<string> usedPinvokeNames, SwiftClassName classForPI,
		                                        FunctionDeclaration funcToWrap, CSUsingPackages use, bool isFinal,
		                                        WrappingResult wrapper, string swiftLibraryPath, TLFunction wrapperFunction,
		                                        string homonymSuffix, bool forcePrivate = false, string alternativeName = null,
							Func<int, int, string> genericRenamer = null)
		{
			var wrapperFuncDecl = FindEquivalentFunctionDeclarationForWrapperFunction (wrapperFunction, TypeMapper, wrapper);
			var pinvokeMethodName = PIMethodName (classForPI, wrapperFunction.Name) + homonymSuffix;
			pinvokeMethodName = Uniqueify (pinvokeMethodName, usedPinvokeNames);
			usedPinvokeNames.Add (pinvokeMethodName);

			string pinvokeMethodRef = PIClassName (classForPI) + "." + pinvokeMethodName;

			var piMethod = TLFCompiler.CompileMethod (wrapperFuncDecl, use,
								  PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
								  wrapperFunction.MangledName, pinvokeMethodName, true, true, false);
			alternativeName = alternativeName ?? (funcToWrap.IsOperator ? ToOperatorName (funcToWrap) : null);

			bool isTrivialEnum = funcToWrap.Parent != null && funcToWrap.Parent is EnumDeclaration en && en.IsTrivial;

			var methodIsStatic = funcToWrap.IsStatic;

			// for a trivial enum, the code goes into a static class so we turn it into a static
			// method that we'll later make into an extension
			if (isTrivialEnum && !methodIsStatic)
				funcToWrap = RecastInstanceMethodAsStatic (funcToWrap);

			var methodName = funcToWrap.IsProperty ? funcToWrap.PropertyName : funcToWrap.Name;
			alternativeName = alternativeName ?? TypeMapper.SanitizeIdentifier (methodName);

			var publicMethod = TLFCompiler.CompileMethod (funcToWrap, use, PInvokeName (wrapper.ModuleLibPath, swiftLibraryPath),
									mangledName: "", alternativeName, false, isFinal, methodIsStatic || isTrivialEnum);
				
			if (isTrivialEnum && !methodIsStatic) {
				// abracadabra! you're an extension!
				var instance = publicMethod.Parameters [0];
				publicMethod.Parameters [0] = new CSParameter (instance.CSType, instance.Name,
					funcToWrap.Parent.IsNested ? CSParameterKind.None : CSParameterKind.This);
			}

			var genericParameters = publicMethod.GenericParameters;
			var genericConstraints = publicMethod.GenericConstraints;

			publicMethod = CSMethod.CopyGenerics (publicMethod, new CSMethod (forcePrivate ? CSVisibility.None : publicMethod.Visibility, publicMethod.Kind, publicMethod.Type,
			                                                                  new CSIdentifier(publicMethod.Name.Name + homonymSuffix), publicMethod.Parameters, publicMethod.Body));

			if (funcToWrap.Parent is ClassDeclaration classDeclaration) {
				if (classDeclaration.HasImportedOverride (publicMethod, TypeMapper)) {
					publicMethod = publicMethod.AsOverride ();
				}
			}


			var instanceTypeSpec = (!funcToWrap.IsStatic && funcToWrap.ParameterLists.Count > 0) ? 
				funcToWrap.ParameterLists [0] [0].TypeSpec : null;

			var localIdentifiers = new List<string> {
				pinvokeMethodName,
				cl.Name.Name,
				publicMethod.Name.Name
			};

			var marshaler = new MarshalEngine (use, localIdentifiers, TypeMapper, wrapper.Module.SwiftCompilerVersion);
			marshaler.GenericReferenceNamer = genericRenamer;

			publicMethod.Body.AddRange (marshaler.MarshalFunctionCall (wrapperFuncDecl, false, pinvokeMethodRef,
				publicMethod.Parameters, funcToWrap, funcToWrap.ReturnTypeSpec, publicMethod.Type, instanceTypeSpec, cl.ToCSType (),
				false, wrapper, false, -1, funcToWrap.HasThrows));


			picl.Methods.Add (piMethod);
			cl.Methods.Add (publicMethod);
			return publicMethod;
		}

		static bool ParametersMatch (FunctionDeclaration decl, TLFunction tlf, TypeMapper mapper, bool ignoreParameterNames = false)
		{
			if (decl.ParameterLists.Last ().Count () != tlf.Signature.ParameterCount)
				return false;
			for (int i = 0; i < tlf.Signature.ParameterCount; i++) {
				var st = tlf.Signature.GetParameter (i);
				var sp = decl.ParameterLists.Last () [i].TypeSpec;

				// match parameter names (thanks, swift)
				// if the names are both null (no name) it will still match
				var name1 = st.Name != null ? st.Name.Name : null;
				var name2 = decl.ParameterLists.Last () [i].PublicName;
				if (!ignoreParameterNames && name1 != null && name2 != null && name1 != name2)
					return false;


				if (!sp.ContainsGenericParameters && decl.IsTypeSpecGeneric (sp) && !decl.IsTypeSpecGenericMetatypeReference (sp)) {
					if (sp is NamedTypeSpec && !(st is SwiftGenericArgReferenceType))
						return false;
					continue;
				}

				if (IsCEnumOrObjEnum (st) && JustTheTypeNamesMatch (st, sp))
					continue;
				var ntb1 = mapper.MapType (st, true);
				var ntb2 = mapper.MapType (decl, sp, true);

				// match types
				if (!NetTypeBundleMatch (ntb1, ntb2))
					return false;
			}
			return true;
		}

		public static FunctionDeclaration FindFunctionDeclarationForTLFunction(TLFunction func, TypeMapper mapper, IEnumerable<FunctionDeclaration> coll)
		{
			var allWrappers = coll.Where(fn => fn.Name == func.Name.Name && func.Signature.ParameterCount ==
												fn.ParameterLists.Last ().Count).ToList ();
			
			foreach (FunctionDeclaration decl in allWrappers) {
				if (FunctionDeclarationMatchesTLFunction (func, decl, mapper))
					return decl;
			}
			return null;

		}

		public static bool FunctionDeclarationMatchesTLFunction (TLFunction func, FunctionDeclaration decl, TypeMapper mapper)
		{
			if (!ParametersMatch (decl, func, mapper, decl.IsSubscript))
				return false;

			if (!decl.ReturnTypeSpec.ContainsGenericParameters && decl.IsTypeSpecGeneric (decl.ReturnTypeSpec)) {
				if (decl.ReturnTypeSpec is NamedTypeSpec && !(func.Signature.ReturnType is SwiftGenericArgReferenceType)) {
					return false;
				}
				return true;
			}

			// match return type (thanks again, swift)
			var returnntb1 = mapper.MapType (func.Signature.ReturnType ?? SwiftTupleType.Empty, true, true);
			var returnntb2 = mapper.MapType (decl, decl.ReturnTypeSpec ?? TupleTypeSpec.Empty, true, true);
			return NetTypeBundleMatch (returnntb1, returnntb2);
		}

		static bool NetTypeBundleMatch(NetTypeBundle ntb1, NetTypeBundle ntb2)
		{
			if (ntb1.FullName != ntb2.FullName) {
				if (!((ntb2.FullName == "System.IntPtr" && ntb1.IsReference) || (ntb1.FullName == "System.IntPtr" && ntb2.IsReference))) {
					return false;
				}
			}
			return true;
		}

		public static TLFunction FindTLFunctionForFunctionDeclaration (FunctionDeclaration funcDecl, TypeMapper mapper, FunctionInventory coll)
		{
			var allWrappers = coll.MethodsWithName (funcDecl.Name).Where (fn => fn.Signature.ParameterCount == funcDecl.ParameterLists.Last ().Count).ToList ();
			foreach (var func in allWrappers) {
				if (FunctionDeclarationMatchesTLFunction (func, funcDecl, mapper))
					return func;
			}
			return null;
		}

		public static FunctionDeclaration FindEquivalentFunctionDeclarationForWrapperFunction (TLFunction func, TypeMapper mapper, WrappingResult wrapper)
		{
			return FindFunctionDeclarationForTLFunction (func, mapper, wrapper.Module.Functions);
		}

		public static TLFunction FindEquivalentTLFunctionForWrapperFunction (FunctionDeclaration funcDecl, TypeMapper mapper, WrappingResult wrapper)
		{
			return FindTLFunctionForFunctionDeclaration (funcDecl, mapper, wrapper.Contents.Functions);
		}

		static bool ParametersMatchExceptSkippingFirst (SwiftType wrapper, SwiftType toWrap, TypeMapper typeMapper)
		{
			var wrapfn = wrapper as SwiftBaseFunctionType;
			if (wrapfn == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 42, "Expected a SwiftFunctionType for wrapper, but got " + wrapper.GetType ().Name);
			var towrapfn = toWrap as SwiftBaseFunctionType;
			if (towrapfn == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 43, "expected a SwiftFunctionType for wrapper, but got " + toWrap.GetType ().Name);

			var wrapTupleArgs = wrapfn.Parameters as SwiftTupleType;
			// there are two cases.
			// Either the wrapper function has 1 arg or more than 1 arg.
			// if it has more than 1 arg, the parameters is a tuple.
			// otherwise is it is singleton

			if (wrapTupleArgs != null) {
				// If it's a tuple, deep compare the types for matching
				var wrapArgs = wrapTupleArgs.AllButFirst ();
				if (!(wrapArgs is SwiftTupleType)) {
					wrapArgs = new SwiftTupleType (new SwiftType [] { wrapArgs }, false);
				}
				var toWrapArgs = towrapfn.Parameters;
				if (!(toWrapArgs is SwiftTupleType)) {
					toWrapArgs = new SwiftTupleType (new SwiftType [] { toWrapArgs }, false);
				}
				return TuplesMatch (wrapArgs as SwiftTupleType, toWrapArgs as SwiftTupleType, typeMapper, true);
			} else {
				// If it's a singleton, then the function to
				// wrap needs to have 0 args, which is an empty tuple.
				var toWrapArgs = towrapfn.Parameters as SwiftTupleType;
				return toWrapArgs != null && toWrapArgs.IsEmpty;
			}
		}
		static bool ParametersMatchExceptSkippingFirstN (SwiftType wrapper, SwiftType toWrap, int n, TypeMapper typeMapper)
		{
			var wrapfn = wrapper as SwiftBaseFunctionType;
			if (wrapfn == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 44, "Expected a SwiftFunctionType for wrapper, but got " + wrapper.GetType ().Name);
			var towrapfn = toWrap as SwiftBaseFunctionType;
			if (towrapfn == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 45, "Expected a SwiftFunctionType for wrapper, but got " + toWrap.GetType ().Name);

			var wrapTupleArgs = wrapfn.Parameters as SwiftTupleType;
			// there are two cases.
			// Either the wrapper function has 1 arg or more than 1 arg.
			// if it has more than 1 arg, the parameters is a tuple.
			// otherwise is it is singleton

			if (wrapTupleArgs != null) {
				// If it's a tuple, deep compare the types for matching
				var wrapArgs = n > 0 ? wrapTupleArgs.AllButFirstN (n) : wrapTupleArgs;
				if (!(wrapArgs is SwiftTupleType)) {
					wrapArgs = new SwiftTupleType (new SwiftType [] { wrapArgs }, false);
				}
				var toWrapArgs = towrapfn.Parameters;
				if (!(toWrapArgs is SwiftTupleType)) {
					toWrapArgs = new SwiftTupleType (new SwiftType [] { toWrapArgs }, false);
				}
				var matchNames = !(towrapfn is SwiftPropertyType);
				return TuplesMatch (wrapArgs as SwiftTupleType, toWrapArgs as SwiftTupleType, typeMapper, matchNames);
			} else {
				// If it's a singleton, then the function to
				// wrap needs to have 0 args, which is an empty tuple.
				if (n > 0) {
					var toWrapArgs = towrapfn.Parameters as SwiftTupleType;
					return toWrapArgs != null && toWrapArgs.IsEmpty;
				} else {
					var toWrapArgs = towrapfn.Parameters as SwiftTupleType ??
					                         new SwiftTupleType (new SwiftType [] { towrapfn.Parameters }, false);
					return TuplesMatch (new SwiftTupleType (new SwiftType [] { wrapfn.Parameters }, false),
					                    toWrapArgs, typeMapper, true);
				}
			}
		}

		static bool TuplesMatch (SwiftTupleType wrapArgs, SwiftTupleType towrapArgs, TypeMapper typeMapper, bool matchNames)
		{
			// special version
			if ((wrapArgs == null && towrapArgs != null) || (wrapArgs != null && towrapArgs == null))
				return false;
			if (wrapArgs.Contents.Count != towrapArgs.Contents.Count)
				return false;
			for (int i = 0; i < wrapArgs.Contents.Count; i++) {
				if (towrapArgs.Contents [i] is SwiftBaseFunctionType && wrapArgs.Contents [i] is SwiftBaseFunctionType) {
					return WrappedClosuresMatch ((SwiftBaseFunctionType)wrapArgs.Contents [i], (SwiftBaseFunctionType)towrapArgs.Contents [i], typeMapper);
				}
				if (!MatchSimple (towrapArgs.Contents [i], wrapArgs.Contents [i], typeMapper))
					return false;
				if (matchNames && !NamesMatch (i, towrapArgs.Contents [i].Name, wrapArgs.Contents [i].Name))
					return false;
			}
			return true;
		}

		static bool NamesMatch (int index, SwiftName originalName, SwiftName wrappedName)
		{
			if (originalName == null || wrappedName == null)
				return true; // match on either or both names optional
			return originalName.Name == wrappedName.Name;
		}

		static bool MatchSimple (SwiftType a, SwiftType b, TypeMapper typeMapper)
		{
			bool aIsGeneric = a is SwiftGenericArgReferenceType;
			bool bIsGeneric = b is SwiftGenericArgReferenceType;
			if ((aIsGeneric && !bIsGeneric) || (!aIsGeneric && bIsGeneric))
				return false;
			if (!aIsGeneric && typeMapper.MustForcePassByReference (a)) {
				if (!a.EqualsReferenceInvaraint (b)) {
					return false;
				}
			} else {
				if (!a.Equals (b)) {
					return false;
				}
			}
			return true;
		}

		static bool WrappedClosuresMatch (SwiftBaseFunctionType wrapperFunc, SwiftBaseFunctionType toWrapFunc, TypeMapper typeMapper)
		{
			string funcName = wrapperFunc.Name != null ? wrapperFunc.Name.Name : "unknown name";
			if (wrapperFunc.ParameterCount < 1 || wrapperFunc.ParameterCount > 3)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 46, $"Unexpected parameter count in wrapper function {funcName}");
			var opaquePointerType = wrapperFunc.GetParameter (wrapperFunc.ParameterCount - 1) as SwiftClassType; // last
			if (opaquePointerType == null || opaquePointerType.ClassName.ToFullyQualifiedName (true) != "Swift.OpaquePointer")
				return false;
				
			if (toWrapFunc.ParameterCount > 0) {
				var wrapperArg = wrapperFunc.GetParameter (wrapperFunc.ParameterCount - 2); // second from last
				var bgt = wrapperArg as SwiftBoundGenericType;
				if (bgt == null) return false;
				if (!IsUnsafeMutablePointer (bgt))
					return false;
				var actualParms = bgt.BoundTypes [0];
				if (!(actualParms is SwiftTupleType))
					actualParms = new SwiftTupleType (new SwiftType [] { actualParms }, false);
				var toWrapParms = toWrapFunc.Parameters;
				if (!(toWrapParms is SwiftTupleType))
					toWrapParms = new SwiftTupleType (new SwiftType [] { toWrapParms }, false);
				bool argsMatch = TuplesMatch ((SwiftTupleType)actualParms, (SwiftTupleType)toWrapParms, typeMapper, false);
				if (!argsMatch)
					return false;
			}
			if (wrapperFunc.ParameterCount == 2 && toWrapFunc.ReturnType == null || toWrapFunc.ReturnType.IsEmptyTuple)
				return true;
			var wrapperReturn = wrapperFunc.GetParameter (0);
			var rbgt = wrapperReturn as SwiftBoundGenericType;
			if (rbgt == null) return false;
			if (!IsUnsafeMutablePointer (rbgt))
				return false;
			var actualReturn = rbgt.BoundTypes [0];
			if (!(actualReturn is SwiftTupleType))
				actualReturn = new SwiftTupleType (new SwiftType [] { actualReturn }, false);
			var toWrapReturn = toWrapFunc.ReturnType;
			if (!(toWrapReturn is SwiftTupleType))
				toWrapReturn = new SwiftTupleType (new SwiftType [] { toWrapReturn }, false);
			return TuplesMatch ((SwiftTupleType)actualReturn, (SwiftTupleType)toWrapReturn, typeMapper, false);
		}

		static bool IsUnsafeMutablePointer (SwiftBoundGenericType bgt)
		{
			if (bgt == null) return false;
			SwiftClassType ct = bgt.BaseType as SwiftClassType;
			if (ct == null)
				return false;
			return ct.ClassName.ToFullyQualifiedName (true) == "Swift.UnsafeMutablePointer";
		}

		WrappingResult WrapModuleContents (
			List<ModuleDeclaration> moduleDecls,
			ModuleInventory moduleInventory,
			List<string> libraryPaths,
			List<string> modulePaths,
			List<string> moduleNames,
			string outputDirectory,
			CompilationTargetCollection targets,
			string wrappingModuleName,
			bool retainSwiftWrappers,
			ErrorHandling errors, bool verbose,
			bool outputIsFramework, string minimumOSVersion = null, bool isLibrary = false)
		{
			wrappingModuleName = wrappingModuleName ?? "XamWrapping";

			var wrappingCompiler = new WrappingCompiler (outputDirectory, SwiftCompilerLocations, retainSwiftWrappers,
			                                             TypeMapper, verbose, errors, Options.TargetRepresentation);

			bool noWrappersNeeded = false;
			Tuple<string, HashSet<string>> wrapStuff = null;
			try {
				wrapStuff = wrappingCompiler.CompileWrappers (
					libraryPaths.ToArray (),
					modulePaths.ToArray (),
					moduleDecls, moduleInventory, targets, wrappingModuleName, outputIsFramework,
					minimumOSVersion, isLibrary);
				noWrappersNeeded = wrapStuff.Item1 == null;
			} catch (Exception e) {
				errors.Add (e);
				return null;
			}

			if (noWrappersNeeded) {
				return WrappingResult.Empty;
			}


			var targetRepresentation = UniformTargetRepresentation.FromPath (wrappingModuleName, new List<string> () { outputDirectory }, errors);

			var compilationTarget = targets [0];
			string wrapperLibraryPath = targetRepresentation.PathToDylib (compilationTarget);
			var wrapperModulePath = targetRepresentation.PathToSwiftModule (compilationTarget);

			var wrapperModuleInventory = ModuleInventory.FromFile (wrapperLibraryPath, errors);
			if (errors.AnyErrors) {
				return null;
			}
			var allModules = wrapperModuleInventory.Values.Select (mc => mc.Name.Name).ToList ();
			if (allModules.Count > 1) {
				var e = ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 47, $"Unable to determine wrapper module name for {wrapperLibraryPath}. Expected exactly one module, but got {allModules.Count}");
				errors.Add (e);
				return null;
			}
			var wrapperModuleContents = wrapperModuleInventory.Values.FirstOrDefault () ??
			                                                  new ModuleContents (new SwiftName (wrappingModuleName, false), wrapperModuleInventory.SizeofMachinePointer);

			var mods = new List<string> (modulePaths);
			mods.Add (targetRepresentation.Path);
			mods.Add (targetRepresentation.ParentPath);
			wrapStuff.Item2.Add (wrappingModuleName);

			var targetRepresentations = UniformTargetRepresentation.GatherAllReferencedModules (wrapStuff.Item2, mods);

			mods.Clear ();
			mods.AddRange (targetRepresentations.Select (tar => tar.ParentPath));

			var targetInfo = ReflectorLocations.GetTargetInfo (targets [0].ToString ());
			using (CustomSwiftCompiler compiler = new CustomSwiftCompiler (targetInfo, null, true)) {
				compiler.Verbose = verbose;
				compiler.ReflectionTypeDatabase = TypeMapper.TypeDatabase;
				var libs = new List<string> (libraryPaths);
				libs.Add (outputDirectory);
				using (DisposableTempDirectory dir = new DisposableTempDirectory ("wrapreflect", true)) {
					string outputPath = dir.UniquePath (wrappingModuleName, "reflect", "xml");
					var mdecl = compiler.ReflectToModules (mods, libs, "", wrappingModuleName).FirstOrDefault ();
					return new WrappingResult (wrapperModulePath, wrapperLibraryPath, wrapperModuleContents, mdecl, wrappingCompiler.FunctionReferenceCodeMap);
				}
			}
		}

		void AddInheritedProtocols (TypeDeclaration decl, CSClass cl, ClassContents classContents, string trimmedLibPath,
		                            CSUsingPackages use, ErrorHandling errors)
		{
			var inheritedProtocols = new List<SwiftReflector.SwiftXmlReflection.Inheritance> ();
			inheritedProtocols.AddRange (decl.Inheritance.Where (inh => inh.InheritanceKind == InheritanceKind.Protocol));
			for (int i = inheritedProtocols.Count - 1; i >= 0; i--) {
				if (TypeMapper.GetDotNetNameForTypeSpec (inheritedProtocols [i].InheritedTypeSpec) == null) {
					var ex = ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 80, $"Unable to find interface for protocol {inheritedProtocols [i].InheritedTypeSpec.ToString ()}");
					errors.Add (ex);
					inheritedProtocols.RemoveAt (i);
				}
			}
			cl.Inheritance.AddRange (inheritedProtocols.Select (inh => {
				var iface = TypeMapper.GetDotNetNameForTypeSpec (inh.InheritedTypeSpec);
				string itemName = null;
				if (iface.Namespace == typeof (SwiftError).Namespace && iface.TypeName == typeof (SwiftError).Name)
					itemName = "ISwiftError";
				else
					itemName = iface.TypeName;
				return new CSIdentifier (itemName);
			}));

			foreach (var inh in inheritedProtocols) {
				CSAttribute protoConstrAttr = null;
				if (inh.InheritedTypeName == "Swift.Error") {
					use.AddIfNotPresent (typeof (ISwiftError));
					CSType inhType = new CSSimpleType (typeof (ISwiftError));
					protoConstrAttr = ProtocolConstraintAttributeFromProtocol (inh.InheritedTypeSpec,
												  inhType,
												  classContents,
												  trimmedLibPath);
				} else {
					var cstypeBundle = TypeMapper.MapType (decl, inh.InheritedTypeSpec, false);
					// need to attach protocol constraints
					if (cstypeBundle.Entity == EntityType.Protocol) {
						protoConstrAttr = ProtocolConstraintAttributeFromProtocol (inh.InheritedTypeSpec,
														      cstypeBundle.ToCSType (use),
																							  classContents,
														      trimmedLibPath);
					}
				}
				if (protoConstrAttr != null) {
					protoConstrAttr.AttachBefore (cl);
				}
			}
		}


		static ModuleInventory GetModuleInventory (string libPath, ErrorHandling errors)
		{
			return ModuleInventory.FromFile (libPath, errors);
		}

		static ModuleInventory GetModuleInventories (List<string> libraryDirectories,
		                                             List<string> moduleNames, ErrorHandling errors)
		{
			var dylibPaths = GetDylibs (libraryDirectories, moduleNames);
			return ModuleInventory.FromFiles (dylibPaths, errors);
		}

		static IEnumerable<string> GetDylibs (IEnumerable<string> libPaths, IEnumerable<string> modNames)
		{
			foreach (string modname in modNames) {
				string path = LocateFile (libPaths, modname);
				if (path != null)
					yield return path;
			}
		}

		static string LocateFile (IEnumerable<string> paths, string modName)
		{
			var errors = new ErrorHandling ();
			var rep = UniformTargetRepresentation.FromPath (modName, paths.ToList (), errors);
			if (rep == null)
				return null;
			return rep.Path;

			//string dylibFile = $"lib{modName}.dylib";
			//foreach (string dir in paths) {
			//	string dylib = Path.Combine (dir, dylibFile);
			//	if (File.Exists (dylib))
			//		return dylib;
			//	if (SwiftModuleFinder.IsAppleFramework (dir, modName + ".swiftmodule")) {
			//		return Path.Combine (dir, modName);
			//	}
			//}
			//return null;
		}

		List<ModuleDeclaration> GetModuleDeclarations (List<string> moduleDirectories, List<string> moduleNames,
		                                               string outputDirectory, bool retainReflectedXmlOutput,
		                                               CompilationTargetCollection targets, ErrorHandling errors, string dylibXmlPath = null)
		{
			try {
				var bestTarget = ChooseBestTarget (targets);

				// If we have a dylib, we will be using our already generated xml
				if (!string.IsNullOrEmpty (dylibXmlPath)) {
					var typeDatabase = CreateTypeDatabase ();
					var decls = Reflector.FromXmlFile (dylibXmlPath, typeDatabase);
					return decls;
				} else {
					using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {
						var targetInfo = ReflectorLocations.GetTargetInfo (bestTarget.ToString ());
						using (CustomSwiftCompiler compiler = new CustomSwiftCompiler (targetInfo, provider, false)) {
							compiler.Verbose = Verbose;
							compiler.ReflectionTypeDatabase = TypeMapper.TypeDatabase;

							var decls = compiler.ReflectToModules (
								moduleDirectories.ToArray (), moduleDirectories.ToArray (),
								"-framework XamGlue", moduleNames.ToArray ());

							foreach (var mdecl in decls) {
								if (!mdecl.IsCompilerCompatibleWith (CompilerVersion)) {
									throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 49, $"The module {mdecl.Name} was compiled with the swift compiler version {mdecl.SwiftCompilerVersion}. It is incompatible with the compiler in {SwiftCompilerLocations.SwiftCompilerBin} which is version {CompilerVersion}");
								}
							}

							if (retainReflectedXmlOutput) {
								var files = Directory.GetFiles (provider.DirectoryPath).Where (fileName => fileName.EndsWith (".xml", StringComparison.OrdinalIgnoreCase)).ToList ();
								CopyXmlOutput (outputDirectory, files);
							}
							return decls;
						}
					}
				}
			} catch (Exception e) {
				errors.Add (e);
				return null;
			}
		}

		TypeDatabase CreateTypeDatabase ()
		{
			var typeDatabase = new TypeDatabase ();
			var dbPath = GetBindingsPath ();
			if (dbPath != null) {
				foreach (var dbFile in Directory.GetFiles (dbPath, "*.xml")) {
					typeDatabase.Read (dbFile);
				}
			}
			return typeDatabase;
		}

		static readonly string [] BindingPaths = {
			"bindings",
			"../bindings",
			"../../bindings",
			"../../../bindings"
		};

		string GetBindingsPath ()
		{
			foreach (var bindingPath in BindingPaths) {
				var wholePath = Path.Combine (Directory.GetCurrentDirectory (), bindingPath);
				if (Directory.Exists (wholePath))
					return wholePath;
			}
			throw new FileNotFoundException ("bindings directory was not found");
		}


		CompilationTarget ChooseBestTarget (CompilationTargetCollection targets)
		{
			if (targets.Count == 1)
				return targets [0];
			foreach (var target in targets) {
				if (IsDeviceTarget (target)) {
					if (target.Cpu == TargetCpu.X86_64)
						continue;
					return target;
				} else {
					return target; // OK for mac 
				}
			}
			throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 50, "Can't find a decent architecture to reflect on among " +
						    targets.Select (t => t.ToString ()).InterleaveCommas ());
		}


		static bool IsDeviceTarget (CompilationTarget target)
		{
			var os = target.OperatingSystem;
			return target.Environment == TargetEnvironment.Device &&
				(os == PlatformName.iOS || os == PlatformName.tvOS || os == PlatformName.watchOS);
		}

		ModuleDeclaration GetModuleDeclaration (string moduleName, List<string> moduleSeachPaths, string outputDirectory, bool retainReflectedOutput, List<ReflectorError> errors)
		{
			try {
				using (TempDirectoryFilenameProvider provider = new TempDirectoryFilenameProvider (null, true)) {
					using (CustomSwiftCompiler compiler = new CustomSwiftCompiler (ReflectorLocations.GetTargetInfo (null), provider, false)) {
						compiler.Verbose = Verbose;
						compiler.ReflectionTypeDatabase = TypeMapper.TypeDatabase;

						var decls = compiler.ReflectToModules (moduleSeachPaths, null, "-f XamGlue", moduleName);
						if (decls.Count != 1) {
							throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 51, $"Error reflecting module '{moduleName}'. Expected 1 module but got {decls.Count}");
						}

						if (!decls [0].IsCompilerCompatibleWith (CompilerVersion)) {
							throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 52, $"The module {decls [0].Name} was compiled with the swift compiler version {decls [0].SwiftCompilerVersion}. It is incompatible with the compiler in {SwiftCompilerLocations.SwiftCompilerBin} which is version {CompilerVersion}");
						}

						if (retainReflectedOutput) {
							CopyXmlOutput (outputDirectory, moduleName, Directory.GetFiles (provider.DirectoryPath, "*.xml").ToList ());
						}
						return decls [0];
					}
				}
			} catch (Exception e) {
				errors.Add (new ReflectorError (e));
				return null;
			}
		}

		void CopyXmlOutput (string outputDirectory, string moduleName, List<string> xmlPathNames)
		{
			string reflectionDir = Path.Combine (outputDirectory, "XmlReflection");
			if (!Directory.Exists (reflectionDir))
				Directory.CreateDirectory (reflectionDir);

			for (int i = 0; i < xmlPathNames.Count; i++) {
				string reflectionOutputPath = Path.Combine (reflectionDir,
					String.Format ("{0}_xamreflect{1}.xml", moduleName, xmlPathNames.Count <= 1 ? "" : i.ToString ()));
				if (File.Exists (reflectionOutputPath))
					File.Delete (reflectionOutputPath);
				File.Copy (xmlPathNames [i], reflectionOutputPath);

			}
		}

		void CopyXmlOutput (string outputDirectory, List<string> xmlPathNames)
		{
			string reflectionDir = Path.Combine (outputDirectory, "XmlReflection");
			if (!Directory.Exists (reflectionDir))
				Directory.CreateDirectory (reflectionDir);

			for (int i = 0; i < xmlPathNames.Count; i++) {
				string reflectionOutputPath = Path.Combine (reflectionDir, "Swift_XamReflect.xml");
				if (File.Exists (reflectionOutputPath))
					File.Delete (reflectionOutputPath);
				File.Copy (xmlPathNames [i], reflectionOutputPath);
			}
		}


		static string Uniqueify (string name, IEnumerable<string> names)
		{
			int thisTime = 0;
			var sb = new StringBuilder (name);
			while (names.Contains (sb.ToString ())) {
				sb.Clear ().Append (name).Append (thisTime++);
			}
			return sb.ToString ();
		}

		static string FindFileForModule (string modName, IEnumerable<string> paths)
		{
			var errors = new ErrorHandling ();
			var rep = UniformTargetRepresentation.FromPath (modName, paths.ToList (), errors);
			if (rep == null)
				return null;
			return rep.Path;
		}

		static string GetClassName (SwiftType st)
		{
			var sct = st as SwiftClassType;
			if (sct != null) {
				return sct.ClassName.ToFullyQualifiedName ();
			} else {
				return "";
			}
		}


		public static void WriteCSFile (string csOutputFileName, string outputDirectory, ICodeElement codeToWrite)
		{
			string csOutputPath = Path.Combine (outputDirectory, csOutputFileName);
			CodeWriter.WriteToFile (csOutputPath, codeToWrite);
		}

		static CSAttribute MakeNominalTypeAttribute (string library, string nominalSym, string metaSym)
		{
			var al = new CSArgumentList ();
			al.Add (new CSArgument (CSConstant.Val (library)));
			al.Add (new CSArgument (CSConstant.Val (nominalSym)));
			al.Add (new CSArgument (CSConstant.Val (metaSym)));
			return CSAttribute.FromAttr (typeof (SwiftValueTypeAttribute), al, true);
		}

		static CSAttribute MakeSwiftStructTypeAttribute (string library, string nominalSym, string metaSym, string witnessSym, CSUsingPackages use)
		{
			use.AddIfNotPresent (typeof (SwiftStructAttribute));
			var al = new CSArgumentList ();
			al.Add (CSConstant.Val (library));
			al.Add (CSConstant.Val (nominalSym));
			al.Add (CSConstant.Val (metaSym));
			al.Add (CSConstant.Val (witnessSym));
			return CSAttribute.FromAttr (typeof (SwiftStructAttribute), al, true);
		}

		static CSAttribute MakeSwiftEnumTypeAttribute (string library, string nomSym, string metaSym, string witnessSym)
		{
			var al = new CSArgumentList ();
			al.Add (CSConstant.Val (library));
			al.Add (CSConstant.Val (nomSym));
			al.Add (CSConstant.Val (metaSym));
			al.Add (CSConstant.Val (witnessSym));
			return CSAttribute.FromAttr (typeof (SwiftEnumTypeAttribute), al, true);
		}

		static CSAttribute MakeProtocolTypeAttribute (string proxyNameClassName, string library, string protocolDesc, bool isAssociatedType)
		{
			var al = new CSArgumentList ();
			al.Add (new CSSimpleType (proxyNameClassName).Typeof ());
			al.Add (CSConstant.Val (library));
			al.Add (CSConstant.Val (protocolDesc));
			if (isAssociatedType)
				al.Add (CSConstant.Val (true));
			return CSAttribute.FromAttr (typeof (SwiftProtocolTypeAttribute), al, true);
		}

		static CSAttribute MakeProtocolConstraintAttribute (CSType interfaceType, string library, string witnessName)
		{
			var al = new CSArgumentList ();
			al.Add (interfaceType.Typeof ());
			al.Add (CSConstant.Val (library));
			al.Add (CSConstant.Val (witnessName));
			return CSAttribute.FromAttr (typeof (SwiftProtocolConstraintAttribute), al, true);
		}

		static string GetSwiftLibraryFullPath (string moduleName, IEnumerable<string> directories)
		{
			SwiftRuntimeLibrary.Exceptions.ThrowOnNull (moduleName, nameof (moduleName));
			SwiftRuntimeLibrary.Exceptions.ThrowOnNull (directories, nameof (directories));
			foreach (string dir in directories) {
				string candidate = Path.Combine (dir, moduleName);
				if (File.Exists (candidate) && MachO.IsMachoFile (candidate))
					return candidate;
			}
			if (moduleName.EndsWith (".dylib", StringComparison.Ordinal)) {
				return null;
			}
			return GetSwiftLibraryFullPath ("lib" + moduleName + ".dylib", directories);
		}

		Version GetCompilerVersion()
		{
			var compiler = new CustomSwiftCompiler (SwiftCompilerLocations.GetTargetInfo (null), null, false);
			return compiler.GetCompilerVersion ();
		}

		ModuleContents ModuleContentsForModuleDeclaration (ModuleDeclaration declaration, ModuleInventory inventory)
		{
			ModuleContents result = null;

			if (inventory.TryGetValue (declaration.Name, out result))
				return result;
			if (declaration.IsEmpty ())
				return null;
			// if the module declaration is not empty, it could very well be there are entities that
			// are not inherently code-generating in it, such as ObjC protocols.
			result = new ModuleContents (new SwiftName (declaration.Name, false), IntPtr.Size);
			return result;
		}

		PlatformName PlatformFromTargets (IEnumerable<string> targets)
		{
			var osString = targets.Select (target => target.ClangOSNoVersion ()).Distinct ().ToList ();
			if (osString.Count != 1)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 53, $"Expecting a unique target but got {osString.InterleaveCommas ()}");
			if (osString [0].StartsWith ("macos", StringComparison.OrdinalIgnoreCase))
				return PlatformName.macOS;
			else if (osString [0].StartsWith ("ios", StringComparison.OrdinalIgnoreCase))
				return PlatformName.iOS;
			else if (osString [0].StartsWith ("tvos", StringComparison.OrdinalIgnoreCase))
				return PlatformName.tvOS;
			else if (osString [0].StartsWith ("watchos", StringComparison.OrdinalIgnoreCase))
				return PlatformName.watchOS;
			return PlatformName.None;
		}

		bool IsImportedInherited (ClassDeclaration classDecl, CSProperty publicOverload)
		{
			var inheritedClass = InheritedClass (classDecl);
			if (inheritedClass == null)
				return false;

			var property = inheritedClass.CSharpProperties.FirstOrDefault (prop => prop.Name.Name == publicOverload.Name.Name);
			if (property != null && CSTypesEqual (property.PropType, publicOverload.PropType))
				return true;
			return IsImportedInherited (inheritedClass, publicOverload);
		}

		bool IsImportedInherited (ClassDeclaration classDecl, CSMethod publicOverload)
		{
			var inheritedClass = InheritedClass (classDecl);
			if (inheritedClass == null)
				return false;

			var methods = inheritedClass.CSharpMethods.Where (m => m.Name.Name == publicOverload.Name.Name &&
									  m.Parameters.Count == publicOverload.Parameters.Count).ToList ();

			foreach (var method in methods) {
				var returnsEq = CSTypesEqual (method.Type, publicOverload.Type);
				if (!returnsEq)
					continue;
				var parmTypesEqual = CSParametersEqual (method.Parameters, publicOverload.Parameters);
				if (parmTypesEqual)
					return true;
			}
			return IsImportedInherited (inheritedClass, publicOverload);
		}

		ClassDeclaration InheritedClass (ClassDeclaration classDecl)
		{
			var classInheritance = classDecl.Inheritance.FirstOrDefault (inh => inh.InheritanceKind == InheritanceKind.Class);
			if (classInheritance == null)
				return null;

			var inheritedEntity = TypeMapper.GetEntityForTypeSpec (classInheritance.InheritedTypeSpec);
			if (inheritedEntity == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 81, $"Unable to find type database entry for class {classInheritance.InheritedTypeName} while searching inheritance.");

			// if we get here, the Type has to be a ClassDeclaration
			return inheritedEntity.Type as ClassDeclaration;
		}

		static bool CSParametersEqual (CSParameterList pl1, CSParameterList pl2)
		{
			if (pl1.Count != pl2.Count)
				return false;
			for (int i = 0; i < pl1.Count; i++) {
				var p1 = pl1 [i];
				var p2 = pl2 [i];
				if (p1.ParameterKind != p2.ParameterKind)
					return false;
				if (!CSTypesEqual (p1.CSType, p2.CSType))
					return false;
			}
			return true;
		}

		static bool CSTypesEqual (CSType t1, CSType t2)
		{
			if (t1 == t2)
				return true;
			if (t1 == null || t2 == null)
				return false;
			return t1.ToString () == t2.ToString ();
		}

		static TLFunction RecastInstanceMethodAsStatic (TLFunction func)
		{
			// given an uncurried function:
			// (FooType)(args) -> return
			// transform into:
			// (self: FooType, args) -> return

			var oldSig = func.Signature as SwiftUncurriedFunctionType;
			if (oldSig == null)
				return func;

			var newArgsTuple = new SwiftTupleType (false);
			if (oldSig.Parameters is SwiftTupleType oldArgs)
				newArgsTuple.Contents.AddRange (oldArgs.Contents);
			else if (!oldSig.Parameters.IsEmptyTuple)
				newArgsTuple.Contents.Add (oldSig.Parameters);
			var ofClass = oldSig.UncurriedParameter as SwiftClassType;
			if (ofClass == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 65, $"Expected a SwiftClassType as a SwiftUncurried parameter in function {func.MangledName} but got a {oldSig.UncurriedParameter.GetType ().Name}");
			ofClass = new SwiftClassType (ofClass.ClassName, ofClass.IsReference, new SwiftName ("self", false));

			newArgsTuple.Contents.Insert (0, ofClass);

			var newArgs = newArgsTuple.Contents.Count == 1 ? newArgsTuple.Contents [0] : newArgsTuple;

			var newSig = new SwiftStaticFunctionType (newArgs, oldSig.ReturnType, oldSig.IsReference, oldSig.CanThrow, ofClass, oldSig.Name);
			newSig.GenericArguments.AddRange (oldSig.GenericArguments);

			var newFunc = new TLFunction (func.MangledName, func.Module, func.Name, ofClass, newSig, func.Offset);
			return newFunc;
		}

		static FunctionDeclaration RecastInstanceMethodAsStatic (FunctionDeclaration func)
		{
			// given an uncurried function:
	    		// func x(FooType)(args) -> return
			// transforms it into:
	    		// func x(self: FooType, args) -> return
			if (func.IsStatic)
				return func;
			var newFunc = new FunctionDeclaration (func);
			newFunc.IsStatic = true;
			newFunc.IsFinal = false;
			newFunc.ParameterLists.Last ().Insert (0, newFunc.ParameterLists [0] [0]);
			newFunc.ParameterLists.RemoveAt (0);
			return newFunc;
		}

		static bool IsCEnumOrObjEnum (SwiftType st)
		{
			return st is SwiftClassType ct && ct.IsEnum && ModuleIsCOrObjC (ct);
		}

		static bool ModuleIsCOrObjC (SwiftClassType ct)
		{
			var module = ct.ClassName.Module.Name;
			return module == "__C" || module == "__ObjC";
		}

		static bool JustTheTypeNamesMatch (SwiftType st, TypeSpec sp)
		{
			var ct = st as SwiftClassType;
			var named = sp as NamedTypeSpec;

			if (ct == null || named == null)
				return false;

			return ct.ClassName.ToFullyQualifiedName (false) == named.NameWithoutModule;
		}

		static CSAttribute TypeNameAttribute (BaseDeclaration decl, CSUsingPackages use)
		{
			use.AddIfNotPresent (typeof (SwiftTypeNameAttribute));
			var argList = new CSArgumentList ();
			argList.Add (new CSArgument (CSConstant.Val (decl.ToFullyQualifiedName ())));
			return CSAttribute.FromAttr (typeof (SwiftTypeNameAttribute), argList);
		}

		public static Func<int, int, string> MakeAssociatedTypeNamer (ProtocolDeclaration protocolDecl)
		{
			return (depth, index) => {
				if (depth != 0)
					throw new NotImplementedException ($"Depth for associated type reference in protocol {protocolDecl.ToFullyQualifiedName ()} should always be 0");
				if (protocolDecl.HasDynamicSelf) {
					if (index == 0)
						return kGenericSelfName;
					return OverrideBuilder.GenericAssociatedTypeName (protocolDecl.AssociatedTypes [index - 1]);
				} else {
					return OverrideBuilder.GenericAssociatedTypeName (protocolDecl.AssociatedTypes [index]);
				}
			};
		}

		void SubstituteAssociatedTypeNamer (ProtocolDeclaration protocolDecl, CSMethod publicMethod)
		{
			var namer = MakeAssociatedTypeNamer (protocolDecl);
			SubstituteAssociatedTypeNamer (namer, publicMethod.Type);
			SubstituteAssociatedTypeNamer (namer, publicMethod.Parameters);
		}

		void SubstituteAssociatedTypeNamer (ProtocolDeclaration protocolDecl, CSParameterList parameters)
		{
			var namer = MakeAssociatedTypeNamer (protocolDecl);
			SubstituteAssociatedTypeNamer (namer, parameters);
		}

		void SubstituteAssociatedTypeNamer (Func<int, int, string> namer, CSParameterList parameters)
		{
			foreach (var parm in parameters)
				SubstituteAssociatedTypeNamer (namer, parm.CSType);
		}

		void SubstituteAssociatedTypeNamer (ProtocolDeclaration protocolDecl, CSProperty publicProperty)
		{
			var namer = MakeAssociatedTypeNamer (protocolDecl);
			SubstituteAssociatedTypeNamer (namer, publicProperty.PropType);
		}

		void SubstituteAssociatedTypeNamer (Func<int, int, string> namer, CSType ty)
		{
			if (ty is CSGenericReferenceType genType) {
				genType.ReferenceNamer = namer;
			} else if (ty is CSSimpleType simpleType) {
				if (simpleType.GenericTypes != null) {
					foreach (var genSubType in simpleType.GenericTypes)
						SubstituteAssociatedTypeNamer (namer, genSubType);
				}
			} else throw new NotImplementedException ($"Unknown type {ty.GetType ().Name} ({ty.ToString ()})");
		}

		static FunctionDeclaration RecastEnumCtorAsStaticFactory (EnumDeclaration enumDecl, FunctionDeclaration funcDecl)
		{
			var copy = new FunctionDeclaration (funcDecl);
			copy.IsStatic = true;
			if (copy.ParameterLists.Count > 0)
				copy.ParameterLists.RemoveAt (0);
			copy.Name = "init";

			return copy;
		}

		static bool IsOptional (TypeSpec spec)
		{
			return spec is NamedTypeSpec namedSpec && namedSpec.ContainsGenericParameters && namedSpec.Name == "Swift.Optional";
		}
	}
}
