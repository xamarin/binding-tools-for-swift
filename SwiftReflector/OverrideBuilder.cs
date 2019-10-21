using System;
using SwiftReflector.SwiftXmlReflection;
using Dynamo.SwiftLang;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector.TypeMapping;
using Dynamo;
using SwiftReflector.Exceptions;
using System.Text;
using ObjCRuntime;

namespace SwiftReflector {
	public class OverrideBuilder {
		TypeMapper typeMapper;
		string vtableTypeName;
		string vtableName = null;
		string vtableSetterName;
		string vtableGetterName;
		bool isProtocol;
		const string kCSIntPtr = "csIntPtr";
		static SLIdentifier kClassIsInitialized = new SLIdentifier ("_xamarinClassIsInitialized");

		// This class is going to do double duty.
		// If it's a class, it builds overrides.
		// If it's a protocol, it builds a protocol proxy.
		// The implementations are so close, this code should be able to handle that without
		// getting too gross. Should.

		public OverrideBuilder (TypeMapper typeMapper, ClassDeclaration classToOverride, string overrideName, ModuleDeclaration targetModule)
		{
			isProtocol = classToOverride is ProtocolDeclaration;
			if (classToOverride.IsFinal && !isProtocol)
				throw new ArgumentException (String.Format ("Attempt to attach override to final class {0}.", classToOverride.ToFullyQualifiedName (true)));
			this.typeMapper = Ex.ThrowOnNull (typeMapper, "typeMapper");

			vtableName = classToOverride.ContainsGenericParameters ? classToOverride.Module.Name + "_xamVtableCache" : "_vtable";
			ClassImplementations = new List<SLClass> ();
			Functions = new List<SLFunc> ();
			Declarations = new List<ICodeElement> ();

			ModuleReferences = new HashSet<string> ();
			Imports = new SLImportModules ();
			Imports.OwningModule = targetModule.Name;
			Imports.Add (new SLImport (classToOverride.Module.Name));
			ModuleReferences.Add (classToOverride.Module.Name);
			OriginalClass = classToOverride;
			vtableTypeName = VtableTypeName (classToOverride);
			vtableSetterName = VtableSetterName (classToOverride);
			vtableGetterName = VtableGetterName (classToOverride);
			if (!isProtocol) {
				OverriddenClass = BuildOverrideDefinition (overrideName, targetModule);
				EveryProtocolExtension = null;
			} else {
				EveryProtocolExtension = BuildExtensionDefinition (targetModule);
				OverriddenClass = null;
			}
			OverriddenVirtualMethods = new List<FunctionDeclaration> ();
			if (!isProtocol) {
				CopyConstructors ();
				AddSuperMethods ();
			}
			CopyVirtualMethods ();
			CreateSLImplementation ();
		}


		void AddImportIfNotPresent (string modname)
		{
			Ex.ThrowOnNull (modname, nameof (modname));
			Imports.AddIfNotPresent (modname);
			ModuleReferences.Add (modname);
		}

		void AddXamGlueImport ()
		{
			AddImportIfNotPresent ("XamGlue");
		}


		public static string VtableTypeName (ClassDeclaration decl)
		{
			return decl.Name + "_xam_vtable";
		}

		static string VtableEtterName (ClassDeclaration decl, bool isSet)
		{
			if (decl.ContainsGenericParameters) {
				return decl.Module.Name + (isSet ? "set" : "get") + VtableTypeName (decl);
			} else {
				return (isSet ? "set" : "get") + VtableTypeName (decl);
			}
		}

		public static string VtableSetterName (ClassDeclaration decl)
		{
			return VtableEtterName (decl, true);
		}

		public static string VtableGetterName (ClassDeclaration decl)
		{
			return VtableEtterName (decl, false);
		}


		public static string SuperPrefix { get { return "xam_super_"; } }

		public static string SuperName (FunctionDeclaration func)
		{
			return SuperPrefix + func.Name;
		}

		public static string SuperPropName (FunctionDeclaration func)
		{
			return SuperPrefix + func.PropertyName;
		}

		public static string SuperSubscriptName (FunctionDeclaration func)
		{
			return SuperPrefix + "subscript_" + (func.IsSubscriptGetter ? "get" : "set");
		}

		public static string SubPrefix { get { return "xam_sub_"; } }

		public static string SubclassName (ClassDeclaration decl)
		{
			return SubPrefix + decl.Name;
		}

		public static string ProxyPrefix { get { return "xam_proxy_"; } }

		public static string ProxyClassName (ClassDeclaration decl)
		{
			return ProxyPrefix + decl.Name;
		}

		public static string ProxyFactoryName (ClassDeclaration decl)
		{
			return "make_" + ProxyClassName (decl);
		}

		public ClassDeclaration OriginalClass { get; private set; }
		public ClassDeclaration OverriddenClass { get; private set; }
		public ExtensionDeclaration EveryProtocolExtension { get; private set; }
		public List<SLClass> ClassImplementations { get; private set; }
		public List<ICodeElement> Declarations { get; private set; }
		public List<SLFunc> Functions { get; private set; }
		public List<FunctionDeclaration> OverriddenVirtualMethods { get; private set; }
		public SLImportModules Imports { get; private set; }
		public HashSet<string> ModuleReferences { get; private set; }
		public int IndexOfFirstNewVirtualMethod { get; private set; }

		ClassDeclaration BuildOverrideDefinition (string name, ModuleDeclaration targetModule)
		{
			name = name ?? (isProtocol ? ProxyClassName (OriginalClass) : SubclassName (OriginalClass));

			var decl = new ClassDeclaration ();
			decl.Name = name;
			decl.Module = targetModule;
			if (OriginalClass.ContainsGenericParameters) {
				decl.Generics.AddRange (OriginalClass.Generics);
				var sb = new StringBuilder ().Append (OriginalClass.ToFullyQualifiedName ());
				bool first = true;
				foreach (GenericDeclaration gendecl in decl.Generics) {
					if (first) {
						sb.Append ('<');
						first = false;
					} else {
						sb.Append (", ");
					}
					sb.Append (gendecl.Name);
				}
				sb.Append ('>');
				decl.Inheritance.Add (new Inheritance (sb.ToString (), InheritanceKind.Class));
			} else {
				decl.Inheritance.Add (new Inheritance (OriginalClass.ToFullyQualifiedName (), InheritanceKind.Class));
			}
			decl.IsObjC = OriginalClass.IsObjC;
			return decl.MakeUnrooted () as ClassDeclaration;
		}

		ExtensionDeclaration BuildExtensionDefinition (ModuleDeclaration targetModule)
		{
			AddXamGlueImport ();
			var decl = new ExtensionDeclaration ();
			decl.ExtensionOnTypeName = "XamGlue.EveryProtocol";
			decl.Module = targetModule;
			decl.Inheritance.Add (new Inheritance (OriginalClass.ToFullyQualifiedName (), InheritanceKind.Protocol));
			return decl;
		}

		void CopyConstructors ()
		{
			OverriddenClass.Members.AddRange (OriginalClass.AllConstructors ().Select (c => MarkOverrideSurrogate (c, Reparent (new FunctionDeclaration (c), OverriddenClass))));
		}

		void AddSuperMethods ()
		{
			OverriddenClass.Members.AddRange (VirtualMethodsForClass (OriginalClass).Select (
				m => ToSuperDecl (m)));
		}

		static T Reparent<T> (T decl, BaseDeclaration newParent) where T : BaseDeclaration
		{
			decl.Parent = newParent;
			return decl;
		}

		static FunctionDeclaration MarkOverrideSurrogate (FunctionDeclaration originalFunction, FunctionDeclaration overrideFunction)
		{
			originalFunction.OverrideSurrogateFunction = overrideFunction;
			return overrideFunction;
		}

		void CopyVirtualMethods ()
		{
			if (isProtocol)
				HandleProtocolMethods (OriginalClass);
			else
				HandleSuperClassVirtualMethods (OriginalClass);
			IndexOfFirstNewVirtualMethod = OverriddenVirtualMethods.Count;
			OverriddenVirtualMethods.AddRange (VirtualMethodsForClass (OriginalClass).Select (m => MarkOverrideSurrogate (m, Reparent (new FunctionDeclaration (m), OverriddenClass))));
			var members = isProtocol ? EveryProtocolExtension.Members : OverriddenClass.Members;
			members.AddRange (OverriddenVirtualMethods);
			members.AddRange (VirtualPropertiesForClass (OriginalClass).Select (p => Reparent (new PropertyDeclaration (p), OverriddenClass)));
		}

		void HandleProtocolMethods (ClassDeclaration decl)
		{
			foreach (TypeSpec spec in decl.Inheritance.Where (inh => inh.InheritanceKind == InheritanceKind.Protocol).Select (inh => inh.InheritedTypeSpec)) {
				var entity = typeMapper.GetEntityForTypeSpec (spec);
				if (entity == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 13, $"Unable to find protocol definition for {spec.ToString ()}, which is a parent of {decl.ToFullyQualifiedName (true)}.");
				}

				var superProtocol = entity.Type as ProtocolDeclaration;
				if (superProtocol == null) {
					throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 14, $"Found and entity for {entity.Type.Name}, but it was a {entity.Type.GetType ().Name} instead of a protocol.");
				}
				OverriddenVirtualMethods.AddRange (VirtualMethodsForClass (superProtocol).Select (m => MarkOverrideSurrogate (m, Reparent (new FunctionDeclaration (m), OverriddenClass))));
				HandleProtocolMethods (superProtocol);
			}
		}

		void HandleSuperClassVirtualMethods (ClassDeclaration decl)
		{
			var parentType = decl.Inheritance.Where (inh => inh.InheritanceKind == InheritanceKind.Class).Select (inh => inh.InheritedTypeSpec).FirstOrDefault ();
			if (parentType == null)
				return;
			var entity = typeMapper.GetEntityForTypeSpec (parentType);
			if (entity == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 16, $"Unable to find class definition for {parentType.ToString ()}, which is the parent of {decl.ToFullyQualifiedName (true)}.");
			}

			var superClass = entity.Type as ClassDeclaration;
			if (superClass == null) {
				throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 17, $"Found and entity for {entity.Type.Name}, but it was a {entity.Type.GetType ().Name} instead of a class.");
			}
			OverriddenVirtualMethods.AddRange (VirtualMethodsForClass (superClass).Select (m => MarkOverrideSurrogate (m, Reparent (new FunctionDeclaration (m), OverriddenClass))));
			HandleSuperClassVirtualMethods (superClass);
		}

		FunctionDeclaration ToSuperDecl (FunctionDeclaration func)
		{
			var superFunc = new FunctionDeclaration (func);
			superFunc.Access = Accessibility.Internal;
			if (func.IsProperty) {
				superFunc.Name = String.Format ("{0}{1}{2}",
					func.IsGetter ? FunctionDeclaration.kPropertyGetterPrefix : FunctionDeclaration.kPropertySetterPrefix,
					SuperPrefix, func.PropertyName);
			} else {
				superFunc.Name = SuperName (func);
			}
			superFunc.Parent = OverriddenClass;
			var pi = new ParameterItem (func.ParameterLists [0] [0]);
			pi.TypeName = OverriddenClass.ToFullyQualifiedName (true);
			superFunc.ParameterLists [0] [0] = pi;
			return superFunc;
		}

		public List<FunctionDeclaration> VirtualMethodsForClass (ClassDeclaration decl)
		{
			return decl.AllVirtualMethods ().Where (func => !(func.IsDeprecated || func.IsUnavailable)).ToList ();
		}

		List<PropertyDeclaration> VirtualPropertiesForClass (ClassDeclaration decl)
		{
			return decl.AllVirtualProperties ().Where (prop => !(prop.IsDeprecated || prop.IsUnavailable)).ToList ();
		}

		static string GenericName (BaseDeclaration bd, string name)
		{
			var depthIndex = bd.GetGenericDepthAndIndex (name);
			return SLGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2);
		}

		void CreateSLImplementation ()
		{
			SLClass cl = null;
			if (isProtocol) {
				cl = new SLClass (Visibility.None, new SLIdentifier ("EveryProtocol"), namedType: NamedType.Extension);
			} else {
				cl = new SLClass (Visibility.Public, OverriddenClass.Name);
				cl.Fields.Add (SLDeclaration.VarLine (kClassIsInitialized, SLSimpleType.Bool, SLConstant.Val (false)));
			}

			if (OriginalClass.ContainsGenericParameters) {
				if (OriginalClass.Generics.Count > 0) {
					cl.Generics.AddRange (OriginalClass.Generics.Select (gen => {
						SLGenericTypeDeclaration genDecl = new SLGenericTypeDeclaration (new SLIdentifier (GenericName (OriginalClass, gen.Name)));
						genDecl.Constraints.AddRange (gen.Constraints.Select (bc => {
							EqualityConstraint eq = bc as EqualityConstraint;
							if (eq != null) {
								SLType secondType = OriginalClass.IsTypeSpecGeneric (eq.Type2Spec) ?
								                                 new SLSimpleType (GenericName (OriginalClass, eq.Type2)) :
								                                 typeMapper.OverrideTypeSpecMapper.MapType (OriginalClass, Imports, eq.Type2Spec, false);
								return new SLGenericConstraint (false, new SLSimpleType (GenericName (OriginalClass, gen.Name)),
															   secondType);
							} else {
								var inh = bc as InheritanceConstraint;
								if (inh == null)
									throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 40, $"Unexpected constraint type {bc.GetType ().Name}");
								SLType secondType = OriginalClass.IsTypeSpecGeneric (inh.InheritsTypeSpec) ?
								                                 new SLSimpleType (GenericName (OriginalClass, inh.Inherits)) :
								                                 typeMapper.OverrideTypeSpecMapper.MapType (OriginalClass, Imports, inh.InheritsTypeSpec, false);
								return new SLGenericConstraint (true, new SLSimpleType (GenericName (OriginalClass, gen.Name)),
															   secondType);
							}

						}));
						return genDecl;
					}));

				}
				var sb = new StringBuilder ().Append (OriginalClass.Name);
				foreach (string s in OriginalClass.Generics.Select (gen => GenericName (OriginalClass, gen.Name)).BracketInterleave ("<", ">", ", "))
					sb.Append (s);
				cl.Inheritance.Add (new SLIdentifier (sb.ToString ()));
				var vtableStruct = DefineGenericVtableStruct ();
				if (vtableStruct.Fields.Count > 0) {
					ClassImplementations.Add (vtableStruct);
					Declarations.Add (DefineGenericVtableDeclaration ());
					Functions.Add (DefineGenericVtableSetter ());
					Functions.Add (DefineGenericVtableGetter ());
				}
			} else {
				cl.Inheritance.Add (new SLIdentifier (OriginalClass.Name));
				var vtableStruct = DefineInnerVtableStruct ();
				if (vtableStruct.Fields.Count > 0) {
					var vtableDecl = DefineVTableVariableDeclaration ();
					if (isProtocol) {
						Declarations.Add (vtableStruct);
						Declarations.Add (vtableDecl);
					} else {
						cl.InnerClasses.Add (vtableStruct);
						cl.Fields.Add (vtableDecl);
					}
					if (!isProtocol)
						cl.Methods.Add (DefineInternalVTableSetter ());
					Functions.Add (DefinePublicVTableSetter ());
				}
			}

			if (!isProtocol) {
				foreach (FunctionDeclaration func in OverriddenClass.AllConstructors ().Where (f => f.Access == Accessibility.Public && !f.IsConvenienceInit)) {
					cl.Methods.Add (ToConstructor (func));
				}
			}
			ClassImplementations.Add (cl);
			var allOvers = DefineOverridesAndSupers ().ToList ();
			cl.Methods.AddRange (allOvers.OfType<SLFunc> ());
			cl.Properties.AddRange (allOvers.OfType<SLProperty> ());
			cl.Subscripts.AddRange (allOvers.OfType<SLSubscript> ());
		}

		IEnumerable<ICodeElementSet> DefineOverridesAndSupers ()
		{
			int i = 0;
			foreach (FunctionDeclaration func in OverriddenVirtualMethods) {
				if (func.IsProperty) {
					if (func.IsSetter || func.IsMaterializer || func.IsSubscriptSetter || func.IsSubscriptMaterializer)
						continue;
					if (func.IsSubscript) {
						var setterFunc = func.MatchingSetter (OverriddenVirtualMethods);
						if (!isProtocol) {
							var supers = InternalSuperSubscriptFromGetterSetter (func, setterFunc);
							OverriddenClass.Members.AddRange (supers);
							var superSub = DefineSuperSubscript (func, setterFunc);
							yield return superSub [0];
							if (superSub.Length == 2)
								yield return superSub [1];
						}
						yield return DefineOverrideSubscript (func, setterFunc, i);
						if (setterFunc != null)
							i++;
					} else {
						var setterFunc = func.MatchingSetter (OverriddenVirtualMethods);
						var propDecl = InternalSuperPropertyFromGetterSetter (func, setterFunc);
						if (OverriddenClass != null)
							OverriddenClass.Members.Add (propDecl);
						if (!isProtocol) {
							yield return DefineSuperProperty (func, setterFunc);
						}
						yield return DefineOverrideProperty (func, setterFunc, i);
						if (setterFunc != null)
							i++;
					}
				} else {
					var parameters = ConvertMethodParameters (func);
					if (!isProtocol) {
						SLFunc superFunc = DefineSuper (func, parameters);
						yield return superFunc;
					}
					var overrideFunc = DefineOverride (func, i, parameters);
					yield return overrideFunc;
				}
				i++;
			}
		}


		PropertyDeclaration InternalSuperPropertyFromGetterSetter (FunctionDeclaration getter, FunctionDeclaration setter)
		{
			var prop = new PropertyDeclaration ();
			prop.Access = Accessibility.Internal;
			prop.IsLet = setter == null;
			prop.IsStatic = getter.IsStatic;
			prop.Module = getter.Module;
			prop.Name = SuperPropName (getter);
			prop.Parent = OverriddenClass;
			prop.Storage = StorageKind.Computed;
			prop.TypeName = getter.ReturnTypeName;
			return prop;
		}

		FunctionDeclaration [] InternalSuperSubscriptFromGetterSetter (FunctionDeclaration getter, FunctionDeclaration setter)
		{
			var supGet = CopyOfSubscriptEtter (getter);

			var supSet = setter != null ? CopyOfSubscriptEtter (setter) : null;

			return supSet != null ? new FunctionDeclaration [] { supGet, supSet } : new FunctionDeclaration [] { supGet };
		}

		FunctionDeclaration CopyOfSubscriptEtter (FunctionDeclaration etter)
		{
			var supEt = new FunctionDeclaration (etter);
			supEt.Access = Accessibility.Internal;
			supEt.Name = SuperSubscriptName (etter);
			supEt.IsProperty = false;
			supEt.Parent = OverriddenClass;
			supEt.ParameterLists [0] [0].TypeName = OverriddenClass.ToFullyQualifiedName ();
			return supEt;
		}

		SLFunc [] DefineSuperSubscript (FunctionDeclaration getter, FunctionDeclaration setter)
		{
			// internal final func xam_super_subscript_get(parameters) -> type
			// {
			//     return super[parameters]
			// }
			// internal final func xam_super_subscript_set(newValue:type, parameters)
			// {
			//    super[parameters] = newValue;
			// }
			var funcs = new SLFunc [setter == null ? 1 : 2];
			SLType propType = null; //_typeMapper.TypeSpecMapper.MapType (Imports, getter.ReturnTypeSpec);

			if (getter.IsTypeSpecGeneric (getter.ReturnTypeSpec)) {
				var ns = (NamedTypeSpec)getter.ReturnTypeSpec;
				var depthIndex = getter.GetGenericDepthAndIndex (ns.Name);
				propType = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
			} else {
				propType = typeMapper.OverrideTypeSpecMapper.MapType (getter, Imports, getter.ReturnTypeSpec, true);
			}

			var propParams = new List<SLParameter> ();
			typeMapper.OverrideTypeSpecMapper.MapParams (typeMapper, getter, Imports, propParams, getter.ParameterLists [1], true);
			var propArgs = propParams.Select (nameTypePair => !nameTypePair.PublicNameIsOptional ? nameTypePair.PublicName : nameTypePair.PrivateName);
			var getterBody = new SLCodeBlock (null);
			getterBody.Add (SLReturn.ReturnLine (new SLSubscriptExpr (SLIdentifier.Super,
				propArgs)));
			var fnGetter = new SLFunc (Visibility.Internal, propType, new SLIdentifier (SuperSubscriptName (getter)),
			                           new SLParameterList (propParams), getterBody);
			funcs [0] = fnGetter;

			if (setter != null) {
				var newValue = new SLIdentifier ("newValue");
				propParams.Insert (0, new SLParameter (newValue, propType));
				var setterBody = new SLCodeBlock (null);
				setterBody.Add (new SLLine (new SLBinding (new SLSubscriptExpr (SLIdentifier.Super,
				                                                                propArgs.Skip (1)), newValue)));
				SLFunc fnSetter = new SLFunc (Visibility.Internal, null, new SLIdentifier (SuperSubscriptName (setter)),
				                              new SLParameterList (propParams), setterBody);
				funcs [1] = fnSetter;
			}

			return funcs;
		}

		SLSubscript DefineOverrideSubscript (FunctionDeclaration getter, FunctionDeclaration setter, int index)
		{
			// public override subscript(parms) -> type {
			//   get {
			//     if _xamarinClassIsInitialized && _vtable[funcIndex] != null {
			//       ...pre-marshalling code
			//       [retval=] _vtable[funcIndex]!(returnCode,toIntPtr(self), parameters);
			//       ...post marshal code...
			//     }
			//     else return super[parameters];
			//   }
			//   set {
			//     if _xamarinClassIsInitialized && _vtable[funcIndex] != null {
			//        _vtable[funcOmdex]!(toIntPtrSelf, newValue, parameters);
			//     }
			//     else {
			//       super[parameters] = newValue;
			//     }
			//   }
			// }
			//
			var getParams = ConvertMethodParameters (getter);

			var idents = new List<string> ();
			idents.Add ("self");
			var getBlock = new SLCodeBlock (null);

			{
				SLBaseExpr vtFuncExpr = null;
				string vtRef = null;
				SLLine vtDef = null;
				var vtId = new SLIdentifier (MarshalEngine.Uniqueify ("vt", idents));
				idents.Add (vtId.Name);

				if (getter.Parent != null && getter.Parent.ContainsGenericParameters) {
					var parms = ToGenericParms (getter.Parent);
					vtDef = SLDeclaration.LetLine (vtId, new SLSimpleType (vtableTypeName),
					                               new SLPostBang (new SLFunctionCall (new SLIdentifier (vtableGetterName), parms), false),
					                               Visibility.None);
					getBlock.Add (vtDef);
					vtFuncExpr = vtId.Dot (new SLIdentifier (VTableEntryIdentifier (index)));
					vtRef = vtId.Name;
				} else {
					var vtablefuncexpr = VtableFuncExpr (index);
					vtFuncExpr = new SLIdentifier (vtablefuncexpr);
					vtRef = VtableReference ();
				}


				var condition = InitializedAndNullCheck (vtFuncExpr);
				var ifblock = new SLCodeBlock (null);

				var marshal = new MarshalEngineSwiftToCSharp (Imports, idents, typeMapper);
				ifblock.AddRange (marshal.MarshalFunctionCall (getter,
					vtRef, VTableEntryIdentifier (index)));

				var elseblock = new SLCodeBlock (null);
				elseblock.Add (SLReturn.ReturnLine (new SLSubscriptExpr (SLIdentifier.Super,
					getParams.Select (nameTypePair => nameTypePair.PrivateName))));
				var ifelse = new SLIfElse (condition, ifblock, elseblock);
				if (isProtocol)
					getBlock = ifblock;
				else
					getBlock.And (ifelse);
			}
			SLCodeBlock setBlock = null;

			if (setter != null) {
				setBlock = new SLCodeBlock (null);
				idents.Clear ();
				idents.Add ("newValue");
				SLBaseExpr vtFuncExpr = null;
				string vtRef = null;
				SLLine vtDef = null;
				var vtId = new SLIdentifier (MarshalEngine.Uniqueify ("vt", idents));
				idents.Add (vtId.Name);

				if (getter.Parent != null && getter.Parent.ContainsGenericParameters) {
					var parms = ToGenericParms (getter.Parent);
					vtDef = SLDeclaration.LetLine (vtId, new SLSimpleType (vtableTypeName),
					                               new SLPostBang (new SLFunctionCall (new SLIdentifier (vtableGetterName), parms), false),
					                               Visibility.None);
					setBlock.Add (vtDef);
					vtFuncExpr = vtId.Dot (new SLIdentifier (VTableEntryIdentifier (index)));
					vtRef = vtId.Name;
				} else {
					string vtablefuncexpr = VtableFuncExpr (index);
					vtFuncExpr = new SLIdentifier (vtablefuncexpr);
					vtRef = VtableReference ();
				}
				var condition = InitializedAndNullCheck (vtFuncExpr);
				var ifblock = new SLCodeBlock (null);

				var marshal = new MarshalEngineSwiftToCSharp (Imports, idents, typeMapper);
				ifblock.AddRange (marshal.MarshalFunctionCall (setter,
									      vtRef, VTableEntryIdentifier (index + 1)));

				var elseblock = new SLCodeBlock (null);
				var superAssign = new SLLine (new SLBinding (new SLSubscriptExpr (SLIdentifier.Super,
				                                                                  getParams.Select (nameTypePair => nameTypePair.PrivateName)), new SLIdentifier ("newValue")));
				elseblock.Add (superAssign);
				var ifelse = new SLIfElse (condition, ifblock, elseblock);
				if (isProtocol)
					setBlock = ifblock;
				else
					setBlock.And (ifelse);
			}
			SLType returnType = null;
			if (getter.IsTypeSpecGeneric (getter.ReturnTypeSpec)) {
				var ns = (NamedTypeSpec)getter.ReturnTypeSpec;
				var depthIndex = getter.GetGenericDepthAndIndex (ns.Name);
				returnType = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
			} else {
				returnType = typeMapper.OverrideTypeSpecMapper.MapType (getter, Imports, getter.ReturnTypeSpec, true);
			}

			return new SLSubscript (Visibility.Public, isProtocol ? FunctionKind.None : FunctionKind.Override, returnType,
			                        new SLParameterList (getParams), getBlock, setBlock);
		}

		SLProperty DefineSuperProperty (FunctionDeclaration getter, FunctionDeclaration setter)
		{
			// internal final var xam_super_name {
			// get {
			//     return super.name
			// }
			// set {
			//    super.name = newValue;
			// }

			// property accessor functions are named "get_NAME" or "set_NAME".
			var superPropName = new SLIdentifier (String.Format ("super.{0}", getter.PropertyName));
			var getterCode = new SLCodeBlock (null);
			getterCode.Add (SLReturn.ReturnLine (superPropName));
			SLCodeBlock setterCode = null;
			if (setter != null) {
				setterCode = new SLCodeBlock (null);
				setterCode.Add (new SLBinding (superPropName, new SLIdentifier ("newValue")));
			}
			SLType returnType = null;
			if (getter.IsTypeSpecGeneric (getter.ReturnTypeSpec)) {
				var ns = (NamedTypeSpec)getter.ReturnTypeSpec;
				var depthIndex = getter.GetGenericDepthAndIndex (ns.Name);
				returnType = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
			} else {
				returnType = typeMapper.OverrideTypeSpecMapper.MapType (getter, Imports, getter.ReturnTypeSpec, true);
			}

			return new SLProperty (Visibility.Internal, FunctionKind.Final, returnType,
			                       new SLIdentifier (SuperPropName (getter)), getterCode, setterCode);

		}

		SLProperty DefineOverrideProperty (FunctionDeclaration getter, FunctionDeclaration setter, int index)
		{
			// public override var NAME : type {
			//     get {
			//         if _xamarinClassIsInitialized && _vtable.funcIndex != nil {
			//             ...pre-marshalling code...
			//             [retval = ]_vtable.funcindex!([returncode,],toIntPtr(self))
			//             ...post-marshal code...
			//             [return retval]
			//         }
			//         else {
			//             return super.NAME
			//         }
			//     }
			//     set {
			//          if _xamarinClassIsInitialized && _vtable.funcIndex+1 != nil {
			//              ...pre-marshalling code...
			//              _vtable.funindex+1!(toIntPtr(self), newValue)
			//              ...post-marshalling code...
			//          }
			//          else {
			//              super.NAME = newValue
			//          }
			//     }
			//


			/*List<SLNameTypePair> getParams = */
			ConvertMethodParameters (getter);

			var idents = new List<string> ();
			idents.Add ("self");

			var getBlock = new SLCodeBlock (null);
			{

				var vtId = new SLIdentifier (MarshalEngine.Uniqueify ("vt", idents));
				idents.Add (vtId.Name);

				SLBaseExpr vtFuncExpr = null;
				string vtRef = null;
				SLLine vtDef = null;

				if (getter.Parent != null && getter.Parent.ContainsGenericParameters) {
					var parms = ToGenericParms (getter.Parent);
					vtDef = SLDeclaration.LetLine (vtId, new SLSimpleType (vtableTypeName),
					                               new SLPostBang (new SLFunctionCall (new SLIdentifier (vtableGetterName), parms), false),
					                               Visibility.None);
					getBlock.Add (vtDef);
					vtFuncExpr = vtId.Dot (new SLIdentifier (VTableEntryIdentifier (index)));
					vtRef = vtId.Name;
				} else {
					var vtablefuncexpr = VtableFuncExpr (index);
					vtFuncExpr = new SLIdentifier (vtablefuncexpr);
					vtRef = VtableReference ();
				}



				SLBaseExpr condition = InitializedAndNullCheck (vtFuncExpr);
				var ifblock = new SLCodeBlock (null);

				var marshal = new MarshalEngineSwiftToCSharp (Imports, idents, typeMapper);
				ifblock.AddRange (marshal.MarshalFunctionCall (getter, vtRef, VTableEntryIdentifier (index)));

				var elseblock = new SLCodeBlock (null);
				elseblock.Add (SLReturn.ReturnLine (new SLIdentifier (String.Format ("super.{0}", getter.PropertyName))));
				var ifelse = new SLIfElse (condition, ifblock, elseblock);

				if (isProtocol) {
					getBlock = ifblock;
				} else {
					getBlock.And (ifelse);
				}
			}

			SLCodeBlock setBlock = null;

			if (setter != null) {
				setBlock = new SLCodeBlock (null);
				idents.Clear ();
				idents.Add ("newValue");


				var vtId = new SLIdentifier (MarshalEngine.Uniqueify ("vt", idents));
				idents.Add (vtId.Name);

				SLBaseExpr vtFuncExpr = null;
				string vtRef = null;
				SLLine vtDef = null;

				if (setter.Parent != null && setter.Parent.ContainsGenericParameters) {
					var parms = ToGenericParms (setter.Parent);
					vtDef = SLDeclaration.LetLine (vtId, new SLSimpleType (vtableTypeName),
					                               new SLPostBang (new SLFunctionCall (new SLIdentifier (vtableGetterName), parms), false),
					                               Visibility.None);
					setBlock.Add (vtDef);
					vtFuncExpr = vtId.Dot (new SLIdentifier (VTableEntryIdentifier (index)));
					vtRef = vtId.Name;
				} else {
					var vtablefuncexpr = VtableFuncExpr (index);
					vtFuncExpr = new SLIdentifier (vtablefuncexpr);
					vtRef = VtableReference ();
				}




				var condition = InitializedAndNullCheck (vtFuncExpr);
				var ifblock = new SLCodeBlock (null);

				var marshal = new MarshalEngineSwiftToCSharp (Imports, idents, typeMapper);
				ifblock.AddRange (marshal.MarshalFunctionCall (setter, vtRef, VTableEntryIdentifier (index + 1)));

				var elseblock = new SLCodeBlock (null);
				var superAssign = new SLLine (new SLBinding (String.Format ("super.{0}", setter.PropertyName), new SLIdentifier ("newValue")));
				elseblock.Add (superAssign);
				var ifelse = new SLIfElse (condition, ifblock, elseblock);

				if (isProtocol)
					setBlock = ifblock;
				else
					setBlock.And (ifelse);
			}
			SLType returnType = null;
			if (getter.IsTypeSpecGeneric (getter.ReturnTypeSpec)) {
				var ns = (NamedTypeSpec)getter.ReturnTypeSpec;
				var depthIndex = getter.GetGenericDepthAndIndex (ns.Name);
				returnType = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
			} else {
				returnType = typeMapper.OverrideTypeSpecMapper.MapType (getter, Imports, getter.ReturnTypeSpec, true);
			}

			return new SLProperty (Visibility.Public, isProtocol ? FunctionKind.None : FunctionKind.Override,
			                       returnType, new SLIdentifier (getter.PropertyName), getBlock, setBlock);
		}


		SLFunc DefineSuper (FunctionDeclaration func, List<SLParameter> parameters)
		{
			// internal final func xam_super_functionName([parameters]) [ -> returnType]
			// {
			//     [return] super.([parameter set]);
			// }
			var body = new SLCodeBlock (null);
			var usedIds = new List<string> ();
			usedIds.AddRange (parameters.Select (p => p.PrivateName.Name));
			usedIds.Add (func.Name);

			SLBaseExpr call = null;
			if (func.IsVariadic) {
				var variadicAdapter = MethodWrapping.BuildVariadicAdapter (new SLIdentifier ("super." + func.Name), func, typeMapper, Imports);
				var varId = new SLIdentifier (MarshalEngine.Uniqueify ("variadicAdapter", usedIds));
				usedIds.Add (varId.Name);
				var varDecl = SLDeclaration.LetLine (varId, null, variadicAdapter, Visibility.None);
				body.Add (varDecl);
				call = new SLFunctionCall (varId.Name, false, parameters.Select (p => new SLArgument (null, p.PrivateName, false)).ToArray ());
			} else {
				call = new SLFunctionCall ("super." + func.Name, false, parameters.Select ((p, i) => {
					var arg = func.ParameterLists.Last () [i];
					var argName = String.IsNullOrEmpty (arg.PublicName) ? arg.PrivateName : arg.PublicName;
					return new SLArgument (new SLIdentifier (argName),
							       p.PrivateName, func.ParameterLists.Last () [i].NameIsRequired);
				}).ToArray ());
			}
			if (func.HasThrows) {
				call = new SLTry (call, false);
			}
			body.Add (func.ReturnTypeSpec.IsEmptyTuple ? new SLLine (call) : SLReturn.ReturnLine (call));

			SLType returnType = null;
			if (func.IsTypeSpecGeneric (func.ReturnTypeSpec)) {
				var ns = (NamedTypeSpec)func.ReturnTypeSpec;
				var depthIndex = func.GetGenericDepthAndIndex (ns.Name);
				returnType = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
			} else {
				returnType = typeMapper.OverrideTypeSpecMapper.MapType (func, Imports, func.ReturnTypeSpec, true);
			}

			var funcKind = FunctionKind.Final;
			if (func.HasThrows)
				funcKind |= FunctionKind.Throws;
			var super = new SLFunc (Visibility.Internal, funcKind, returnType,
			                        new SLIdentifier (SuperName (func)), new SLParameterList (parameters), body);

			return super;
		}

		DelegatedCommaListElemCollection<SLArgument> ToGenericParms (BaseDeclaration par)
		{
			var parms = new DelegatedCommaListElemCollection<SLArgument> (SLFunctionCall.WriteElement);

			for (int i = 0; i < par.Generics.Count; i++) {
				var decl = par.Generics [i];
				string parmName = String.Format ("t{0}", i);
				Tuple<int, int> depthIndex = par.GetGenericDepthAndIndex (decl.Name);
				var typeID = new SLIdentifier (SLGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2));
				var arg = new SLArgument (new SLIdentifier (parmName), typeID.Dot (new SLIdentifier ("self")));
				parms.Add (arg);
			}

			return parms;
		}

		string VtableFuncExpr (int index)
		{
			return isProtocol ? $"{vtableName}.{VTableEntryIdentifier (index)}" :
						$"{OverriddenClass.Name}.{vtableName}.{VTableEntryIdentifier (index)}";
		}

		string VtableReference ()
		{
			return isProtocol ? vtableName : $"{OverriddenClass.Name}.{vtableName}";
		}

		SLFunc DefineOverride (FunctionDeclaration func, int index, List<SLParameter> parameters)
		{
			// public override func functionName([parameters]) [ -> returnType]
			// {
			//       if _xamarinClassIsInitialized && _vtable.funcindex != nil {
			//           ...pre-marshalling code...
			//           [retval=]_vtable.funcindex([returncode,]toIntPtr(self), parameters)
			//           ...post-marshalling code...
			//       }
			//       else {
			//           [return] super.functionName([parameter set]);
			//       }

			// if generic:
			// public override func functionName([parameters]) [ -> returnType]
			// {
			//	     let vt = vtableGetter(T0.self,...)!
			//       if _xamarinClassIsInitialized && vt.funcindex != nil {
			//           ...pre-marshalling code...
			//           [retval=]vt.funcindex([returncode,]toIntPtr(self), parameters)
			//           ...post-marshalling code...
			//       }
			//       else {
			//           [return] super.functionName([parameter set]);
			//       }

			// if throws:
			// public override func functionName([parameters]) [ -> returnType]
			// {
			//		if _xamarinClassIsInitialized && vt.funcIndex != nil {
			//			let retval = UnsafeMutablePointer<(returnType, Error, Bool)>.allocate(1)
			//			...pre-marshaling code...
			//			vt.funcindex!(toIntPtr(retval), toIntPtr(self), parameters)
			//			...post-marshalling code...
			//			let err = getExceptionThrow(retval:retval)
			//			if err == nil {
			//				let retvalval = getExceptionNotThrown(retval:retval)
			//				retval.deallocate()
			//				return retval
			//			}
			//			else {
			//				retval.deallocate()
			//				throw err!
			//			}
			//		}
			//		else {
			//			[return] super.functionName([parameter set]);
			//		}
			var body = new SLCodeBlock (null);

			var idents = new List<string> ();
			idents.Add ("self");
			foreach (var parm in parameters) {
				if (!parm.PublicNameIsOptional)
					idents.Add (parm.PublicName.Name);
				idents.Add (parm.PrivateName.Name);
			}

			var vtId = new SLIdentifier (MarshalEngine.Uniqueify ("vt", idents));
			idents.Add (vtId.Name);

			SLBaseExpr vtFuncExpr = null;
			string vtRef = null;

			if (func.Parent != null && func.Parent.ContainsGenericParameters) {
				var parms = ToGenericParms (func.Parent);
				var vtDef = SLDeclaration.LetLine (vtId, new SLSimpleType (vtableTypeName),
				                                   new SLPostBang (new SLFunctionCall (new SLIdentifier (vtableGetterName), parms), false),
				                                   Visibility.None);
				body.Add (vtDef);
				vtFuncExpr = vtId.Dot (new SLIdentifier (VTableEntryIdentifier (index)));
				vtRef = vtId.Name;
			} else {
				var vtablefuncexpr = VtableFuncExpr (index);
				vtFuncExpr = new SLIdentifier (vtablefuncexpr);
				vtRef = VtableReference ();
			}

			var condition = InitializedAndNullCheck (vtFuncExpr);

			var ifblock = new SLCodeBlock (null);

			var marshal = new MarshalEngineSwiftToCSharp (Imports, idents, typeMapper);
			ifblock.AddRange (marshal.MarshalFunctionCall (func,
								      vtRef, VTableEntryIdentifier (index)));

			var elseblock = new SLCodeBlock (null);
			var callSite = $"super.{func.Name}";
			SLBaseExpr funcCall = null;
			if (func.IsVariadic) {
				var variadicAdapter = MethodWrapping.BuildVariadicAdapter (new SLIdentifier (callSite), func, typeMapper, Imports);
				var adapterID = new SLIdentifier (MarshalEngine.Uniqueify ("variadicAdapter", idents));
				idents.Add (adapterID.Name);
				body.Add (SLDeclaration.LetLine (adapterID, null, variadicAdapter, Visibility.None));
				funcCall = new SLFunctionCall (adapterID.Name, false, parameters.Select (parm => new SLArgument (null, parm.PrivateName, false)).ToArray ());
			} else {
				funcCall = new SLFunctionCall (callSite, false,
				                               parameters.Select ((p, i) =>
				                                                  new SLArgument (p.PublicName, p.PrivateName, func.ParameterLists.Last () [i].NameIsRequired)).ToArray ());
			}
			if (func.HasThrows)
				funcCall = new SLTry (funcCall, false);
			if (func.ReturnTypeSpec == null || func.ReturnTypeSpec.IsEmptyTuple) {
				elseblock.Add (new SLLine (funcCall));
			} else {
				elseblock.Add (SLReturn.ReturnLine (funcCall));
			}


			if (isProtocol) {
				body = ifblock;
			} else {
				var ifelse = new SLIfElse (condition, ifblock, elseblock);
				body.Add (ifelse);
			}

			var outputParams = new List<SLParameter> ();
			typeMapper.OverrideTypeSpecMapper.MapParams (typeMapper, func, Imports, outputParams, func.ParameterLists [1], true);

			SLType returnType = null;
			if (func.IsTypeSpecGeneric (func.ReturnTypeSpec)) {
				var ns = (NamedTypeSpec)func.ReturnTypeSpec;
				var depthIndex = func.GetGenericDepthAndIndex (ns.Name);
				returnType = new SLGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
			} else {
				returnType = typeMapper.OverrideTypeSpecMapper.MapType (func, Imports, func.ReturnTypeSpec, true);
			}

			var funcKind = isProtocol ? FunctionKind.None : FunctionKind.Override;
			if (func.HasThrows)
				funcKind |= FunctionKind.Throws;
			return new SLFunc (Visibility.Public, funcKind, returnType,
			                   new SLIdentifier (func.Name), new SLParameterList (outputParams), body);
		}

		SLLine DefineGenericVtableDeclaration ()
		{
			AddXamGlueImport ();
			var fieldLine = SLDeclaration.VarLine (vtableName, new SLDictionaryType (new SLSimpleType ("TypeCacheKey"),
			                                                                         new SLSimpleType (vtableTypeName)),
			                                       new SLFunctionCall (String.Format ("[TypeCacheKey : {0}]", vtableTypeName), true),
			                                       Visibility.Internal);
			return fieldLine;
		}

		SLLine DefineVTableVariableDeclaration ()
		{
			var fieldLine = SLDeclaration.VarLine (vtableName, new SLSimpleType (vtableTypeName),
				new SLFunctionCall (vtableTypeName, true), Visibility.Private, !isProtocol);
			return fieldLine;
		}

		SLLine DefineCSIntPtrDeclaration ()
		{
			var fieldLine = SLDeclaration.VarLine (kCSIntPtr, new SLSimpleType ("UnsafeRawPointer"),
				new SLFunctionCall ("UnsafeRawPointer", true), Visibility.Private, false);
			return fieldLine;
		}

		SLFunc DefineGenericVtableGetter ()
		{
			// internal func _vtableGetterName(t0: Any.Type /* ... */) -> _vtableType?
			// {
			//     return _vtableName[TypeCacheKey(types:ObjectIdentifier(t0))]
			// }
			var parms = new List<SLParameter> ();
			var exprs = new SLBaseExpr [OriginalClass.Generics.Count];
			for (int i = 0; i < OriginalClass.Generics.Count; i++) {
				var id = new SLIdentifier (String.Format ("t{0}", i));
				exprs [i] = new SLFunctionCall ("ObjectIdentifier", true,
				                                new SLArgument (new SLIdentifier ("_"), id));
				parms.Add (new SLParameter (id, new SLSimpleType ("Any.Type")));
			}
			var typeCacheKeyExpr =
				new SLFunctionCall ("TypeCacheKey", true, true,
				                    new SLArgument (new SLIdentifier ("types"), new SLCommaListExpr (exprs)));

			SLCodeBlock body = new SLCodeBlock (null);
			body.Add (SLReturn.ReturnLine (new SLSubscriptExpr (vtableName, typeCacheKeyExpr)));
			var func = new SLFunc (Visibility.Internal,
						 new SLOptionalType (new SLSimpleType (vtableTypeName)),
						 new SLIdentifier (vtableGetterName),
			                       new SLParameterList (parms), body);
			return func;
		}

		SLFunc DefineGenericVtableSetter ()
		{
			// public func _vtableSetterName(uvt: UnsafeRawPointer, t0: Any.Type) // t1, t2, t3... {
			// let vt:UnsafePointer<_vtableType> = fromIntPtr(uvt);
			// _vtableName[TypeCacheKey(types:ObjectIdentifier(t0))] = vt 
			// }
			var parms = new List<SLParameter> ();
			var uvtID = new SLIdentifier ("uvt");
			var vtID = new SLIdentifier ("vt");
			parms.Add (new SLParameter (uvtID, new SLSimpleType ("UnsafeRawPointer")));
			var exprs = new SLBaseExpr [OriginalClass.Generics.Count];
			for (int i = 0; i < OriginalClass.Generics.Count; i++) {
				var id = new SLIdentifier (String.Format ("t{0}", i));
				exprs [i] = new SLFunctionCall ("ObjectIdentifier", true,
				                                new SLArgument (new SLIdentifier ("_"), id));
				parms.Add (new SLParameter (id, new SLSimpleType ("Any.Type")));
			}
			var typeCacheKeyExpr =
				new SLFunctionCall ("TypeCacheKey", true, true,
				                    new SLArgument (new SLIdentifier ("types"), new SLCommaListExpr (exprs)));

			var body = new SLCodeBlock (null);
			body.Add (SLDeclaration.LetLine (vtID,
			                                 new SLSimpleType (String.Format ("UnsafePointer<{0}>", vtableTypeName)),
			                                 new SLFunctionCall ("fromIntPtr", false, new SLArgument (new SLIdentifier ("ptr"), uvtID, true)),
			                                 Visibility.None));
			body.Add (new SLLine (new SLBinding (new SLSubscriptExpr (vtableName, typeCacheKeyExpr),
			                                     vtID.Dot (new SLIdentifier ("pointee")))));
			var func = new SLFunc (Visibility.Public, null, new SLIdentifier (vtableSetterName),
			                       new SLParameterList (parms), body);
			return func;
		}

		SLFunc DefineInternalVTableSetter ()
		{
			var parms = new List<SLParameter> ();
			var vtableID = new SLIdentifier ("vtable");
			parms.Add (new SLParameter (vtableID, new SLSimpleType (vtableTypeName)));
			var body = new SLCodeBlock (null);
			body.Add (new SLLine (new SLBinding (vtableName, vtableID, null)));
			var func = new SLFunc (Visibility.Internal, FunctionKind.Static, null,
			                       new SLIdentifier (vtableSetterName), new SLParameterList (parms), body);
			return func;
		}


		SLFunc DefinePublicVTableSetter ()
		{
			// public func setTYPE_xam_vtable(uvt:UnsafeRawPointer)
			// {
			//    let vt: UnsafePointer<TYPE.VTABLETYPE> = fromIntPtr(uvt);
			//    TYPE.setTYPE_xam_vtable(vt.memory);
			// }
			// OR
			// public func setTYPE_xaml_vtable(uvt: UnsafeRawPointer)
			// {
			//      let vt: UnsafePointer<VTABLETYPE> = fromIntPtr(uvt)
			//      _vtable = uvt.pointee;
			// }

			AddXamGlueImport ();
			var parms = new List<SLParameter> ();
			var unsafeVTID = new SLIdentifier ("uvt");
			parms.Add (new SLParameter (unsafeVTID, new SLSimpleType ("UnsafeRawPointer")));
			var body = new SLCodeBlock (null);
			var vtID = new SLIdentifier ("vt");

			var vtPointerType = isProtocol ? new SLSimpleType ($"UnsafePointer<{vtableTypeName}>")
				: new SLSimpleType ($"UnsafePointer<{OverriddenClass.Name}.{vtableTypeName}>");

			body.Add (SLDeclaration.LetLine (vtID, vtPointerType,
			                                 new SLFunctionCall ("fromIntPtr", false, new SLArgument (new SLIdentifier ("ptr"), unsafeVTID, true)), Visibility.None));

			if (isProtocol) {
				body.Add (new SLLine (new SLBinding (vtableName, vtID.Dot (new SLIdentifier ("pointee")))));
			} else {
				body.Add (SLFunctionCall.FunctionCallLine (String.Format ("{0}.{1}", OverriddenClass.Name, vtableSetterName),
									   new SLArgument (new SLIdentifier ("vtable"), vtID.Dot (new SLIdentifier ("pointee")), true)));
			}
			SLFunc func = new SLFunc (Visibility.Public, FunctionKind.None, null, new SLIdentifier (vtableSetterName),
			                          new SLParameterList (parms), body);
			return func;
		}

		SLClass DefineGenericVtableStruct ()
		{
			return DefineInnerVtableStruct ();
		}

		SLClass DefineInnerVtableStruct ()
		{
			// this defines the type of the struct that will hold the vtable
			var cl = new SLClass (Visibility.Internal, vtableTypeName, null, false, false, NamedType.Struct);
			var fieldIdentifiers = new List<string> ();
			int j = 0;
			foreach (FunctionDeclaration func in OverriddenVirtualMethods) {
				if (func.IsSetter || func.IsMaterializer || func.IsSubscriptSetter || func.IsSubscriptMaterializer)
					continue;

				cl.Fields.Add (ToFieldDeclaration (func, j, fieldIdentifiers));
				j++;
				if (func.IsGetter) {
					FunctionDeclaration setFunc = OverriddenVirtualMethods.Where (f => f.IsSetter && f.PropertyName == func.PropertyName).FirstOrDefault ();
					if (setFunc != null) {
						cl.Fields.Add (ToFieldDeclaration (setFunc, j, fieldIdentifiers));
						j++;
					}
				} else if (func.IsSubscriptGetter) {
					FunctionDeclaration setFunc = OverriddenVirtualMethods.Where (f => f.IsSubscriptSetter).FirstOrDefault ();
					if (setFunc != null) {
						cl.Fields.Add (ToFieldDeclaration (setFunc, j, fieldIdentifiers));
						j++;
					}
				}
			}
			return cl;
		}

		SLLine ToFieldDeclaration (FunctionDeclaration func, int index, List<string> fieldIdentifiers)
		{
			// this defines the type and member of a vtable entry
			var closureType = ToClosureType (func);
			SLAttribute.ConventionC ().AttachBefore (closureType);

			var fieldLine = SLDeclaration.VarLine (VTableEntryIdentifier (index), new SLOptionalType (closureType), null, Visibility.Internal);
			return fieldLine;
		}

		public static string VTableEntryIdentifier (int index)
		{
			return String.Format ("func{0}", index);
		}

		SLFuncType ToClosureType (FunctionDeclaration func)
		{
			var parameters = new List<SLParameter> ();
			var arguments = new List<SLNameTypePair> ();
			typeMapper.OverrideTypeSpecMapper.MapParamsToCSharpTypes (Imports, parameters, func.ParameterLists [1]);
			for (int i = 0; i < parameters.Count; i++) {
				arguments.Add (new SLNameTypePair (parameters [i].ParameterKind, "_", parameters [i].TypeAnnotation));
			}

			// insert a reference to self.
			// or in the case of protocols, the C# IntPtr
			arguments.Insert (0, new SLNameTypePair ("_", new SLSimpleType ("UnsafeRawPointer")));

			SLType returnType = new SLTupleType ();
			// we have a return type which is NOT an empty tuple
			if ((func.ReturnTypeSpec != null && !func.ReturnTypeSpec.IsEmptyTuple) || func.HasThrows) {
				var localReturnType = !func.HasThrows ? typeMapper.OverrideTypeSpecMapper.MapType (func, Imports, func.ReturnTypeSpec, true) : null;

				bool isGenericReturn = func.ReturnTypeSpec != null && func.IsTypeSpecGeneric (func.ReturnTypeSpec);
				// if the return type must by pass by reference, make it a parameter instead
				var entType = isGenericReturn ? EntityType.None : typeMapper.GetEntityTypeForTypeSpec (func.ReturnTypeSpec);
				if (func.HasThrows || isGenericReturn || typeMapper.MustForcePassByReference (func, func.ReturnTypeSpec) || func.ReturnTypeSpec is ClosureTypeSpec
					|| func.ReturnTypeSpec is ProtocolListTypeSpec) {
					arguments.Insert (0, new SLNameTypePair ("_", new SLSimpleType ("UnsafeRawPointer")));
				} else {
					if (entType == EntityType.Class) {
						returnType = new SLSimpleType ("UnsafeRawPointer");
					} else if (entType == EntityType.TrivialEnum) {
						var entity = typeMapper.GetEntityForTypeSpec (func.ReturnTypeSpec);
						var enumSpec = entity.Type as EnumDeclaration;
						var rawType = enumSpec.HasRawType ? enumSpec.RawTypeSpec : new NamedTypeSpec ("Swift.Int");
						returnType = typeMapper.OverrideTypeSpecMapper.MapType (func, Imports, rawType, true);
					} else {
						returnType = localReturnType;
					}
				}
			}
			return new SLFuncType (new SLTupleType (arguments), returnType);
		}

		SLFunc ToConstructor (FunctionDeclaration func)
		{
			var isOptionalConstructor = func.IsOptionalConstructor;

			var parameters = ConvertConstructorParameters (func);
			var body = new SLCodeBlock (null);
			var superCall = new SLFunctionCall ("super.init", false, true, ConvertArguments (parameters, func));

			body.Add (new SLLine (superCall));
			body.Add (new SLLine (new SLBinding (kClassIsInitialized, SLConstant.Val (true))));

			var overrideType = func.IsRequired ? FunctionKind.Required : FunctionKind.Override;
			var slfunc = new SLFunc (Visibility.Public, FunctionKind.Constructor | overrideType,
			                         null, null, new SLParameterList (parameters), body, isOptionalConstructor);
			return slfunc;
		}

		SLArgument [] ConvertArguments (List<SLParameter> parameters, FunctionDeclaration func)
		{
			return parameters.Select ((parm, i) => new SLArgument (new SLIdentifier (func.ParameterLists [1] [i].PublicName), parm.PrivateName, func.ParameterLists [1] [i].NameIsRequired)).ToArray ();
		}

		List<SLParameter> ConvertConstructorParameters (FunctionDeclaration func)
		{
			if (func.ParameterLists.Count != 2)
				throw new ArgumentException (String.Format ("Expected two parameter lists in a constructor, but got {0}.",
				                                            func.ParameterLists.Count), nameof(func));
			return ConvertParametersImpl (func);
		}

		List<SLParameter> ConvertMethodParameters (FunctionDeclaration func)
		{
			if (func.ParameterLists.Count != 2 || func.ParameterLists [0].Count != 1)
				throw new ArgumentException (String.Format ("Unable to wrap function {0}, require a normal instance method not a partially applicable function.",
					func.ToFullyQualifiedName (true)));
			return ConvertParametersImpl (func);
		}

		List<SLParameter> ConvertParametersImpl (FunctionDeclaration func)
		{
			var outParams = new List<SLParameter> ();
			typeMapper.OverrideTypeSpecMapper.MapParams (typeMapper, func, Imports, outParams, func.ParameterLists [1], true);
			return outParams;
		}

		static SLBaseExpr InitializedAndNullCheck (SLBaseExpr vtExpr)
		{
			return new SLBinaryExpr (BinaryOp.And, kClassIsInitialized, new SLBinaryExpr (BinaryOp.NotEqual, vtExpr, SLConstant.Nil));
		}
	}
}

