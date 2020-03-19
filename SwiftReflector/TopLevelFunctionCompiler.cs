// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Dynamo;
using Dynamo.CSLang;
using SwiftReflector.Exceptions;
using SwiftReflector.TypeMapping;
using SwiftReflector.SwiftXmlReflection;
using SwiftRuntimeLibrary;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector {
	public class TopLevelFunctionCompiler {
		// utility class
		class GenericReferenceAssociatedTypeProto {
			public NamedTypeSpec GenericPart { get; set; }
			public ProtocolDeclaration Protocol { get; set; }
		}

		TypeMapper typeMap;
		Dictionary<string, string> mangledToCSharp = new Dictionary<string, string> ();

		public TopLevelFunctionCompiler (TypeMapper typeMap)
		{
			this.typeMap = typeMap;
		}

		public CSProperty CompileProperty (string propertyName, CSUsingPackages packs, SwiftType swiftPropertyType, bool hasGetter, bool hasSetter,
						 CSMethodKind methodKind)
		{
			propertyName = typeMap.SanitizeIdentifier (propertyName);
			NetTypeBundle propertyType = typeMap.MapType (swiftPropertyType, false);

			if (!(swiftPropertyType is SwiftGenericArgReferenceType))
				AddUsingBlock (packs, propertyType);
			ICodeElement [] uselessLine = new ICodeElement [] { CSReturn.ReturnLine (new CSIdentifier ("useless")) };

			CSCodeBlock getterBlock = null;
			if (hasGetter)
				getterBlock = new CSCodeBlock (uselessLine);
			CSCodeBlock setterBlock = null;
			if (hasSetter)
				setterBlock = new CSCodeBlock (uselessLine);

			CSProperty theProp = new CSProperty (propertyType.ToCSType (packs), methodKind,
				new CSIdentifier (propertyName), CSVisibility.Public, getterBlock, CSVisibility.Public, setterBlock);
			if (getterBlock != null)
				getterBlock.Clear ();
			if (setterBlock != null)
				setterBlock.Clear ();

			return theProp;
		}

		public CSProperty CompileProperty (CSUsingPackages packs, string propertyName,
			FunctionDeclaration getter, FunctionDeclaration setter, CSMethodKind methodKind = CSMethodKind.None)
		{
			var swiftPropertyType = GetPropertyType (getter, setter);
			NetTypeBundle propertyType = null;
			if (TypeMapper.IsCompoundProtocolListType (swiftPropertyType)) {
				propertyType = new NetTypeBundle ("System", "object", false, false, EntityType.ProtocolList);
			} else {
				propertyType = typeMap.MapType (getter, swiftPropertyType, false, true);
			}
			propertyName = propertyName ?? typeMap.SanitizeIdentifier (getter != null ? getter.PropertyName : setter.PropertyName);
			bool isSubscript = getter != null ? getter.IsSubscript :
				setter.IsSubscript;

			if (!getter.IsTypeSpecGeneric (swiftPropertyType))
				AddUsingBlock (packs, propertyType);

			var uselessLine = new ICodeElement [] { CSReturn.ReturnLine (new CSIdentifier ("useless")) };

			CSCodeBlock getterBlock = null;
			if (getter != null)
				getterBlock = new CSCodeBlock (uselessLine);
			CSCodeBlock setterBlock = null;
			if (setter != null)
				setterBlock = new CSCodeBlock (uselessLine);

			CSProperty theProp = null;
			if (isSubscript) {
				List<ParameterItem> swiftParms = null;
				if (getter != null) {
					swiftParms = getter.ParameterLists [1];
				} else {
					swiftParms = setter.ParameterLists [1].Skip (1).ToList ();
				}
				var args = typeMap.MapParameterList (getter, swiftParms, false, false, null, null);
				args.ForEach (a => AddUsingBlock (packs, a.Type));

				var csParams =
					new CSParameterList (
						args.Select (a =>
							new CSParameter (a.Type.ToCSType (packs),
								new CSIdentifier (a.Name), a.Type.IsReference ? CSParameterKind.Ref : CSParameterKind.None, null)));
				theProp = new CSProperty (propertyType.ToCSType (packs), methodKind, CSVisibility.Public, getterBlock,
					CSVisibility.Public, setterBlock, csParams);


			} else {
				theProp = new CSProperty (propertyType.ToCSType (packs), methodKind,
					new CSIdentifier (propertyName), CSVisibility.Public, getterBlock, CSVisibility.Public, setterBlock);
			}
			if (getterBlock != null)
				getterBlock.Clear ();
			if (setterBlock != null)
				setterBlock.Clear ();

			return theProp;
		}

		SwiftType GetPropertyType (SwiftPropertyType getter, SwiftPropertyType setter)
		{
			if (getter != null) {
				return getter.ReturnType;
			}
			if (setter != null) {
				if (setter.IsSubscript) {
					return ((SwiftTupleType)setter.Parameters).Contents [0];
				} else {
					return setter.Parameters;
				}
			}
			throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 0, "neither getter nor setter provided");
		}

		TypeSpec GetPropertyType (FunctionDeclaration getter, FunctionDeclaration setter)
		{
			if (getter != null) {
				return getter.ReturnTypeSpec;
			}
			if (setter != null) {
				// same for subscript and prop
				return setter.ParameterLists [1] [0].TypeSpec;
			}
			throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 1, "neither getter nor setter provided");
		}

		public CSDelegateTypeDecl CompileToDelegateDeclaration (FunctionDeclaration func, CSUsingPackages packs,
			string mangledName, string delegateName, bool objectsAreIntPtrs, CSVisibility vis, bool isSwiftProtocol)
		{
			bool returnIsGeneric = func.IsTypeSpecGeneric (func.ReturnTypeSpec);
			var args = typeMap.MapParameterList (func, func.ParameterLists.Last (), objectsAreIntPtrs, true, null, null);
			RemapSwiftClosureRepresensation (args);
			var returnType = returnIsGeneric ? null : typeMap.MapType (func, func.ReturnTypeSpec, objectsAreIntPtrs, true);
			delegateName = delegateName ?? typeMap.SanitizeIdentifier (func.Name);

			args.ForEach (a => AddUsingBlock (packs, a.Type));

			if (returnType != null && !returnIsGeneric)
				AddUsingBlock (packs, returnType);

			CSType csReturnType = returnType == null || returnType.IsVoid ? CSSimpleType.Void : returnType.ToCSType (packs);
			var csParams = new CSParameterList ();
			for (int i = 0; i < args.Count; i++) {
				var arg = args [i];
				var argIsGeneric = func.IsTypeSpecGeneric (func.ParameterLists.Last () [i].TypeSpec);
				CSParameter csParam = null;
				var parmType = func.ParameterLists.Last () [i].TypeSpec;
				if (arg.Type.Entity == EntityType.Tuple || (!argIsGeneric && IsObjCStruct (parmType))) {
					csParam = new CSParameter (CSSimpleType.IntPtr, new CSIdentifier (arg.Name), CSParameterKind.None, null);
				} else {
					csParam = new CSParameter (arg.Type.ToCSType (packs), new CSIdentifier (arg.Name),
								   arg.Type.IsReference ? CSParameterKind.Ref : CSParameterKind.None, null);
				}
				csParams.Add (csParam);
			}

			if (isSwiftProtocol) {
				packs.AddIfNotPresent (typeof (SwiftExistentialContainer1));
				csParams.Insert (0, new CSParameter (new CSSimpleType (typeof (SwiftExistentialContainer1)), new CSIdentifier ("self"), CSParameterKind.Ref));
			} else {
				csParams.Insert (0, new CSParameter (CSSimpleType.IntPtr, new CSIdentifier ("self")));
			}

			var retvalName = "xam_retval";
			var retvalID = new CSIdentifier (retvalName);

			if (func.HasThrows || returnIsGeneric || !returnType.IsVoid) { // && func.Signature.ReturnType.IsStruct || func.Signature.ReturnType.IsEnum) {
				if (func.HasThrows) {
					csParams.Insert (0, new CSParameter (CSSimpleType.IntPtr, retvalName, CSParameterKind.None));
					csReturnType = CSSimpleType.Void;
				} else {
					if (!returnIsGeneric) {
						if (!(func.ReturnTypeSpec is ClosureTypeSpec)) {
							Entity ent = typeMap.GetEntityForTypeSpec (func.ReturnTypeSpec);
							if (ent == null && !(func.ReturnTypeSpec is ProtocolListTypeSpec))
								throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 8, $"Unable to find entity for class {csReturnType.ToString ()}.");

							if (ent != null && (ent.IsStructOrEnum || ent.EntityType == EntityType.Protocol)) {
								csParams.Insert (0, new CSParameter (CSSimpleType.IntPtr, retvalID, CSParameterKind.None));
								csReturnType = CSSimpleType.Void;
							} else if (func.ReturnTypeSpec is ProtocolListTypeSpec pl) {
								csParams.Insert (0, new CSParameter (new CSSimpleType ($"SwiftExistentialContainer{pl.Protocols.Count}"), retvalID, CSParameterKind.Ref));
								csReturnType = CSSimpleType.Void;
							}
						}
					} else {
						csParams.Insert (0, new CSParameter (CSSimpleType.IntPtr, retvalID, CSParameterKind.None));
					}
				}
			}

			return new CSDelegateTypeDecl (vis, csReturnType, new CSIdentifier (delegateName), csParams);
		}

		public CSMethod CompileMethod (FunctionDeclaration func, CSUsingPackages packs, string libraryPath,
			string mangledName, string functionName, bool isPinvoke, bool isFinal, bool isStatic)
		{
			isStatic = isStatic || func.IsExtension;
			var extraProtoArgs = new CSGenericTypeDeclarationCollection ();
			var extraProtoConstraints = new CSGenericConstraintCollection ();
			var args = typeMap.MapParameterList (func, func.ParameterLists.Last (), isPinvoke, false, extraProtoArgs, extraProtoConstraints);
			if (isPinvoke && func.ParameterLists.Count > 1) {
				var metaTypeBundle = new NetTypeBundle ("SwiftRuntimeLibrary", "SwiftMetatype", false, false, EntityType.None);
				NetParam p = new NetParam ("metaClass", metaTypeBundle);
				args.Add (p);
			}

			NetTypeBundle returnType = null;
			if (func.ReturnTypeSpec is ProtocolListTypeSpec plitem && !isPinvoke) {
				returnType = new NetTypeBundle ("System", "object", false, false, EntityType.ProtocolList);
			} else {
				returnType = typeMap.MapType (func, func.ReturnTypeSpec, isPinvoke, true);
			}

			string funcName = functionName ?? typeMap.SanitizeIdentifier (func.Name);

			if (isPinvoke && !mangledToCSharp.ContainsKey (mangledName))
				mangledToCSharp.Add (mangledName, funcName);

			args.ForEach (a => AddUsingBlock (packs, a.Type));

			if (returnType != null && !(func.IsTypeSpecGeneric (func.ReturnTypeSpec)))
				AddUsingBlock (packs, returnType);

			CSType csReturnType = returnType.IsVoid ? CSSimpleType.Void : returnType.ToCSType (packs);
			var csParams =
				new CSParameterList (
					args.Select (a =>
						new CSParameter (a.Type.ToCSType (packs),
							new CSIdentifier (a.Name), a.Type.IsReference ? CSParameterKind.Ref : CSParameterKind.None, null)));

			if (isPinvoke) {
				AddExtraGenericArguments (func, csParams, packs);
				return CSMethod.InternalPInvoke (csReturnType, funcName, libraryPath,
					mangledName.Substring (1), csParams);
			} else {
				CSMethod retval = null;
				if (func.IsConstructor) {
					retval = CSMethod.PublicConstructor (funcName, csParams, new CSCodeBlock ());
				} else {
					if (isFinal)
						retval = new CSMethod (CSVisibility.Public, isStatic ? CSMethodKind.Static : CSMethodKind.None, csReturnType, new CSIdentifier (funcName),
							csParams, new CSCodeBlock ());
					else
						retval = new CSMethod (CSVisibility.Public, isStatic ? CSMethodKind.Static : CSMethodKind.Virtual, csReturnType, new CSIdentifier (funcName),
							csParams, new CSCodeBlock ());
				}
				if (func.Generics.Count > 0) {
					foreach (GenericDeclaration genDecl in func.Generics) {
						var depthIndex = func.GetGenericDepthAndIndex (genDecl.Name);
						var genTypeId = new CSIdentifier (CSGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2));
						retval.GenericParameters.Add (new CSGenericTypeDeclaration (genTypeId));
						foreach (var constr in genDecl.Constraints) {
							var inh = constr as InheritanceConstraint;
							if (inh != null) {
								var entity = typeMap.GetEntityForTypeSpec (inh.InheritsTypeSpec);
								if (entity != null && entity.IsDiscretionaryConstraint)
									continue;
								var ntb = typeMap.MapType (func, inh.InheritsTypeSpec, false);
								var proto = GetConstrainedAssociatedTypeProtocol (func, new NamedTypeSpec (inh.Name));
								if (proto != null) {
									// SwiftProto -> ISwiftProto<AT0, AT1, AT2, ...>
									// need to add extra generics:
									// AT0, ...
									// need to add the constraint T : ISwiftProto<AT0, AT1, AT2, ...>
									var assocTypeNames = proto.Protocol.AssociatedTypes.Select (assoc => OverrideBuilder.GenericAssociatedTypeName (assoc));
									retval.GenericParameters.AddRange (assocTypeNames.Select (name => new CSGenericTypeDeclaration (new CSIdentifier (name))));
									var genParts = assocTypeNames.Select (name => new CSSimpleType (name)).ToArray ();
									var genType = new CSSimpleType ($"I{proto.Protocol.Name}", false, genParts);
									var genRef = new CSIdentifier (CSGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2));
									retval.GenericConstraints.Add (new CSGenericConstraint (genTypeId, new CSIdentifier (genType.ToString ())));
								} else {
									packs.AddIfNotPresent (ntb.NameSpace);
									retval.GenericConstraints.Add (new CSGenericConstraint (genTypeId, new CSIdentifier (ntb.Type)));
								}
							} else {
								throw new NotSupportedException ("need to do associated types.");
							}
						}
					}
				}
				if (extraProtoArgs.Count > 0) {
					retval.GenericParameters.AddRange (extraProtoArgs);
					retval.GenericConstraints.AddRange (extraProtoConstraints);
				}
				return retval;
			}
		}

		void AddExtraGenericArguments (FunctionDeclaration func, CSParameterList csParams, CSUsingPackages packs)
		{
			var usedNames = csParams.Select (p => p.Name.Name).ToList ();

			foreach (GenericDeclaration genDecl in func.Generics) {
				if (GenericDeclarationIsReferencedByGenericClassInParameterList (func, genDecl, typeMap))
					continue;
				string name = MarshalEngine.Uniqueify ("mt", usedNames);
				usedNames.Add (name);
				packs.AddIfNotPresent (typeof (SwiftMetatype));
				csParams.Add (new CSParameter (new CSSimpleType (typeof (SwiftMetatype)), name));
				int totalProtocolConstraints = TotalProtocolConstraints (genDecl);
				for (int j = 0; j < totalProtocolConstraints; j++) {
					name = MarshalEngine.Uniqueify ("ct", usedNames);
					usedNames.Add (name);
					csParams.Add (new CSParameter (CSSimpleType.IntPtr, name));
				}
			}
		}

		GenericReferenceAssociatedTypeProto GetConstrainedAssociatedTypeProtocol (BaseDeclaration context, NamedTypeSpec spec)
		{
			// we're looking for the pattern T, where T is a generic or contains a generic (Foo<T>)
			// and there exists a where T : SomeProtocol 
			if (spec == null)
				return null;
			GenericReferenceAssociatedTypeProto result = null;
			if (spec.ContainsGenericParameters) {
				foreach (var gen in spec.GenericParameters) {
					// recurse on generic element
					result = GetConstrainedAssociatedTypeProtocol (context, gen as NamedTypeSpec);
					if (result != null)
						break;
				}
			} else {
				// which declaration has this generic
				var owningContext = FindOwningContext (context, spec);
				if (owningContext != null) {
					foreach (var genPart in context.Generics) {
						if (genPart.Name != spec.Name)
							continue;
						// genPart is the one we care about - now look for a constraint.
						foreach (var constraint in genPart.Constraints) {
							// Is it inheritance?
							if (constraint is InheritanceConstraint inheritance) {
								// Find the entity in the database
								var entity = typeMap.TypeDatabase.EntityForSwiftName (inheritance.Inherits);
								// Is it a protocol and it has associated types
								if (entity != null && entity.Type is ProtocolDeclaration proto && proto.HasAssociatedTypes)
									result = new GenericReferenceAssociatedTypeProto () {
										GenericPart = spec,
										Protocol = proto
									};
							}
						}
						if (result != null)
							break;
					}
				}
			}
			return result;
		}

		BaseDeclaration FindOwningContext (BaseDeclaration context, NamedTypeSpec spec)
		{
			while (context != null) {
				foreach (var genPart in context.Generics) {
					if (genPart.Name == spec.Name)
						return context;
				}
				context = context.Parent;
			}
			return null;
		}

		public static bool GenericArgumentIsReferencedByGenericClassInParameterList (SwiftBaseFunctionType func, GenericArgument arg)
		{
			foreach (SwiftType st in func.EachParameter) {
				if (st is SwiftUnboundGenericType) {
					var sut = (SwiftUnboundGenericType)st;
					if (!sut.DependentType.IsClass)
						continue;
					// there appears to be a bug in the swift compiler that doesn't accept certain
					// generic patterns that will ensure that sut.Arguments won't ever have more than 1
					// element in it in cases that we care about, but what the heck - do the general case.
					foreach (GenericArgument gen in sut.Arguments) {
						if (gen.Depth == arg.Depth && gen.Index == arg.Index)
							return true;
					}
				}
			}
			return false;
		}

		public static bool GenericDeclarationIsReferencedByGenericClassInParameterList (FunctionDeclaration func, GenericDeclaration genDecl, TypeMapper mapper)
		{
			foreach (ParameterItem pi in func.ParameterLists.Last ()) {
				if (pi.TypeSpec is NamedTypeSpec) {
					// this inner section should probably be recursive, but I was unable to
					// even test if SomeClass<SomeOtherClass<T>> is valid because the swift compiler
					// wouldn't take it.
					var ns = (NamedTypeSpec)pi.TypeSpec;
					if (ns.ContainsGenericParameters) {
						Entity en = mapper.GetEntityForTypeSpec (ns);
						if (en != null && en.EntityType == EntityType.Class) {
							foreach (TypeSpec genTS in ns.GenericParameters) {
								var nsGen = genTS as NamedTypeSpec;
								if (nsGen != null) {
									if (genDecl.Name == nsGen.Name)
										return true;
								}
							}
						}
					}
				}
			}
			return false;
		}


		public string CSMethodForMangledName (string mangledName)
		{
			return mangledToCSharp [Ex.ThrowOnNull (mangledName, "mangledName")];
		}

		static void AddUsingBlock (CSUsingPackages packs, NetTypeBundle type)
		{
			if (type.IsVoid || String.IsNullOrEmpty (type.NameSpace))
				return;
			packs.AddIfNotPresent (type.NameSpace);
		}


		int TotalProtocolConstraints (GenericArgument gen)
		{
			int count = 0;
			foreach (var constraint in gen.Constraints) {
				var ct = constraint as SwiftClassType;
				if (ct == null)
					throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 11, $"Expected a SwiftClassType for constraint, but got {constraint.GetType ().Name}.");
				if (ct.EntityKind == MemberNesting.Protocol)
					count++;
			}
			return count;
		}
		int TotalProtocolConstraints (GenericDeclaration gen)
		{
			int count = 0;
			foreach (BaseConstraint constraint in gen.Constraints) {
				var inh = constraint as InheritanceConstraint;
				if (inh == null)
					throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 12, $"Expected a SwiftClassType for constraint, but got {constraint.GetType ().Name}.");
				var en = typeMap.GetEntityForTypeSpec (inh.InheritsTypeSpec);
				if (en.EntityType == EntityType.Protocol)
					count++;
			}
			return count;
		}

		bool IsObjCStruct (NetParam ntb, SwiftType parmType)
		{
			if (ntb.Type.Entity != EntityType.Struct)
				return false;

			// if the Entity is EntityType.Struct, it's guaranteed to be a SwiftClassType
			var structType = parmType as SwiftClassType;
			var entity = typeMap.GetEntityForSwiftClassName (structType.ClassName.ToFullyQualifiedName (true));
			if (entity == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 5, $"Unable to get the entity for struct type {structType.ClassName.ToFullyQualifiedName (true)}");
			return entity.Type.IsObjC;
		}

		bool IsObjCStruct (TypeSpec typeSpec)
		{
			if (!(typeSpec is NamedTypeSpec))
				return false;
			var entity = typeMap.GetEntityForTypeSpec (typeSpec);
			if (entity == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 6, $"Unable to get the entity for type {typeSpec.ToString ()}");
			return entity.IsObjCStruct;
		}

		void RemapSwiftClosureRepresensation (List<NetParam> args)
		{
			for (int i = 0; i < args.Count; i++) {
				if (args[i].Type.FullName == "SwiftRuntimeLibrary.SwiftClosureRepresentation") {
					var bundle = new NetTypeBundle ("SwiftRuntimeLibrary", "BlindSwiftClosureRepresentation", false, args [i].Type.IsReference, EntityType.Closure);
					args [i] = new NetParam (args [i].Name, bundle);
				}
			}
		}
	}
}

