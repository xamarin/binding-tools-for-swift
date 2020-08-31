// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using SwiftReflector.TypeMapping;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SwiftReflector.SwiftXmlReflection;
using SwiftRuntimeLibrary;

namespace SwiftReflector.Importing {

	public class BindingImporter {
		class TypeDefinitionTypeDeclarationPair {
			public TypeDefinitionTypeDeclarationPair (TypeDefinition typeDefinition, TypeDeclaration typeDeclaration)
			{
				TypeDefinition = Exceptions.ThrowOnNull (typeDefinition, nameof (typeDefinition));
				TypeDeclaration = Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			}
			public TypeDefinition TypeDefinition { get; }
			public TypeDeclaration TypeDeclaration { get; }
		}

		TypeDatabase database = new TypeDatabase ();
		TypeDatabase peerDatabase;
		List<TypeDefinitionTypeDeclarationPair> allDeclarations = new List<TypeDefinitionTypeDeclarationPair> ();
		ErrorHandling errors;
		TypeAggregator aggregator;
		PlatformName platform;

		public BindingImporter (PlatformName platform, ErrorHandling errors, TypeDatabase peerDatabase = null)
			: this (TypeAggregator.PathForPlatform (platform), errors, peerDatabase)
		{
			this.platform = platform;
		}

		BindingImporter (string assemblyPath, ErrorHandling errors, TypeDatabase peerDatabase = null)
		{
			Exceptions.ThrowOnNull (assemblyPath, nameof (assemblyPath));
			this.errors = Exceptions.ThrowOnNull (errors, nameof (errors));
			this.peerDatabase = peerDatabase;
			aggregator = new TypeAggregator (assemblyPath);

			aggregator.IsObjCChecker = (nameSpace, typeName) => {
				if (peerDatabase == null)
					return false;
				var entity = peerDatabase.EntityForDotNetName (new DotNetName (nameSpace, typeName));
				if (entity != null)
					return entity.Type.IsObjC;
				return false;
			};
		}


		public MatchCollection Includes => aggregator.Includes;
		public MatchCollection Excludes => aggregator.Excludes;


		public static bool ImportAndMerge (PlatformName platformName, TypeDatabase peerDatabase, ErrorHandling errors)
		{
			Exceptions.ThrowOnNull (peerDatabase, nameof (peerDatabase));
			var initialErrorCount = errors.ErrorCount;
			var newDb = ImportFrom (platformName, errors, peerDatabase);
			peerDatabase.Merge (newDb, errors);
			return initialErrorCount != errors.ErrorCount;
		}

		public static TypeDatabase ImportFrom (PlatformName platformName, ErrorHandling errors, TypeDatabase peerDatabase = null)
		{
			var importer = new BindingImporter (platformName, errors, peerDatabase);
			return importer.Import ();
		}

		public TypeDatabase Import()
		{
			aggregator.Aggregate ();

			ImportClassesOrStructsOrProtocols (aggregator.PublicClasses);
			ImportClassesOrStructsOrProtocols (aggregator.PublicStructs);
			ImportClassesOrStructsOrProtocols (aggregator.PublicProtocols);
			ImportEnums (aggregator.PublicEnums);

			ImportMembers ();
			return database;
		}

		void ImportMembers ()
		{
			var memberImporter = peerDatabase != null ? new MemberImporter (errors, database, peerDatabase) : new MemberImporter (errors, database);
			foreach (var defDeclPair in allDeclarations)
			{
				if (defDeclPair.TypeDeclaration is ClassDeclaration classDecl) {
					memberImporter.Import (defDeclPair.TypeDefinition, classDecl);
				}
			}
		}

		void ImportEnums (List<TypeDefinition> enums) {
			foreach (var @enum in enums) {
				var entity = ToEnumEntity (@enum);
				if (entity != null) {
					database.Add (entity);
				}
			}
		}

		Entity ToEnumEntity (TypeDefinition @enum) {
			var enumDecl = ToEnumDeclaration (@enum);
			var entity = new Entity {
				SharpNamespace = @enum.Namespace,
				SharpTypeName = @enum.Name,
				EntityType = EntityType.Enum,
				ProtocolProxyModule = null,
				IsDiscretionaryConstraint = false,
				Type = enumDecl
			};
			return entity;
		}

		EnumDeclaration ToEnumDeclaration (TypeDefinition definition)
		{
			var name = definition.Name;
			var moduleName = definition.Namespace;

			TypeAggregator.RemapModuleAndName (platform, ref moduleName, ref name, TypeType.Enum);

			var module = ToModuleDeclaration (moduleName);

			var enumDeclaration = new EnumDeclaration {
				Name = name,
				Access = ToAccessibility (definition),
				Module = module,
				ParentExtension = null,
				Kind = TypeKind.Enum,
				Members = new List<Member> (),
				IsObjC = true,
				IsDeprecated = false,
				IsUnavailable = false,
			};
			foreach (var field in definition.Fields) {
				if (field.Name == "value__")
					continue;
				enumDeclaration.Elements.Add (ToEnumElement (field));
			}
			return enumDeclaration.MakeUnrooted () as EnumDeclaration;
		}

		EnumElement ToEnumElement (FieldDefinition definition)
		{
			
			var element = new EnumElement (definition.Name, null, ConvertToLong (definition.Constant));
			return element;
		}

		static long ConvertToLong (object o)
		{
			if (o is ulong)
				return (long)(ulong)o;
			return Convert.ToInt64 (o);
		}


		void ImportClassesOrStructsOrProtocols (List<TypeDefinition> classes)
		{
			foreach (var theClass in classes) {
				var entity = ToEntity (theClass);
				if (entity != null) {
					database.Add (entity);
					allDeclarations.Add (new TypeDefinitionTypeDeclarationPair (theClass, entity.Type));
				}
			}
		}

		Entity ToEntity (TypeDefinition def)
		{
			bool isProtocol = def.IsInterface;
			bool isStruct = def.IsClass && def.IsValueType && !def.IsEnum;
			var classDecl = isProtocol ? ToProtocolDeclaration (def) :
				(isStruct ? ToStructDeclaration (def) : ToClassDeclaration (def));
			if (classDecl == null)
				return null;
			var entity = new Entity {
				SharpNamespace = def.Namespace,
				SharpTypeName = def.Name,
				EntityType = isProtocol ? EntityType.Protocol : (isStruct ? EntityType.Struct : EntityType.Class),
				ProtocolProxyModule = null,
				IsDiscretionaryConstraint = false,
				Type = classDecl
			};
			return entity;
		}

		TypeDeclaration ToStructDeclaration (TypeDefinition definition)
		{
			var name = TypeAggregator.ProtocolAttributeName (definition) ?? definition.Name;
			var moduleName = definition.Namespace;

			if (TypeAggregator.FilterModuleAndName (platform, moduleName, ref name)) {
				TypeAggregator.RemapModuleAndName (platform, ref moduleName, ref name, TypeType.Struct);
				var module = ToModuleDeclaration (moduleName);
				var structDeclaration = new StructDeclaration {
					Name = name,
					Access = ToAccessibility (definition),
					Module = module,
					ParentExtension = null,
					Kind = TypeKind.Struct,
					Members = new List<Member> (),
					IsObjC = true,
					IsFinal = true,
					IsDeprecated = false,
					IsUnavailable = false
				};
				return structDeclaration.MakeUnrooted ();
			}
			return null;
		}

		TypeDeclaration ToProtocolDeclaration (TypeDefinition definition)
		{
			var name = TypeAggregator.ProtocolAttributeName (definition) ?? definition.Name;
			var moduleName = definition.Namespace;

			// FIXME: these cases don't have a clear resolution
			// don't know what to do about this, so skip I guess?
			if (name == "NSDraggingDestination" || name == "NSDraggingInfo")
				return null;

			TypeAggregator.RemapModuleAndName (platform, ref moduleName, ref name, TypeType.Interface);

	    		
			var protocolDeclaration = new ProtocolDeclaration {
				Name = name,
				Access = ToAccessibility (definition),
				Module = ToModuleDeclaration (moduleName),
				ParentExtension = null,
				Kind = TypeKind.Protocol,
				Members = new List<Member> (),
				IsObjC = true,
				IsFinal = definition.IsSealed,
				IsDeprecated = false,
				IsUnavailable = false,
				IsImportedBinding = true
			};
			protocolDeclaration.Inheritance.AddRange (ToInheritance (definition));
			return protocolDeclaration.MakeUnrooted ();
		}

		TypeDeclaration ToClassDeclaration (TypeDefinition definition)
		{
			var name = TypeAggregator.RegisterAttributeName (definition) ?? definition.Name;
			var moduleName = definition.Namespace;
			TypeAggregator.RemapModuleAndName (platform, ref moduleName, ref name, TypeType.Class);
			var classDecl = new ClassDeclaration {
				Name = name,
				Access = ToAccessibility (definition),
				Module = ToModuleDeclaration (moduleName),
				ParentExtension = null,
				Kind = TypeKind.Class,
				Members = new List<Member> (),
				InnerClasses = new List<ClassDeclaration> (),
				InnerStructs = new List<StructDeclaration> (),
				InnerEnums = new List<EnumDeclaration> (),
				IsObjC = true,
				IsFinal = definition.IsSealed,
				IsDeprecated = false,
				IsUnavailable = false,
				IsImportedBinding = true
			};
			classDecl.Inheritance.AddRange (ToInheritance (definition));
			return classDecl.MakeUnrooted ();
		}

		IEnumerable <Inheritance> ToInheritance (TypeDefinition definition)
		{
			if (definition.BaseType != null)
			{
				var baseType = definition.BaseType.Resolve ();
				if (baseType != null && !baseType.HasGenericParameters && IsObjCClassCandidate (baseType))
					yield return new Inheritance (baseType.FullName, InheritanceKind.Class);
			}
			if (definition.HasInterfaces) {
				foreach (var iface in definition.Interfaces) {
					var ifaceType = iface.InterfaceType.Resolve ();
					if (ifaceType == null)
						continue;
					if (IsObjCProtocolCandidate (ifaceType))
						yield return new Inheritance (iface.InterfaceType.FullName, InheritanceKind.Protocol);
				}
			}
		}

		Accessibility ToAccessibility (TypeDefinition definition)
		{
			if (!definition.IsPublic)
				throw new NotSupportedException ($"Unsupported non-public accessiblity.");
			return Accessibility.Public;
		}

		ModuleDeclaration ToModuleDeclaration (string moduleName)
		{
			var module = new ModuleDeclaration (moduleName);
			return module;
		}


		List<TypeDefinition> ApplyIncludeExcludeRules (IEnumerable<TypeDefinition> typeList)
		{
			var types = new HashSet<TypeDefinition> ();
			var notExcluded = Excludes.Excluding (typeList, typeDefinition => typeDefinition.FullName);
			foreach (var type in notExcluded)
				types.Add (type);
			var reallyInclude = Includes.Including (typeList, typeDefinition => typeDefinition.FullName);
			foreach (var type in reallyInclude)
				types.Add (type);
			return types.ToList ();
		}

		public static bool CacheModules { get; set; }

		static Dictionary<string, ModuleDefinition> module_cache = new Dictionary<string, ModuleDefinition> ();



		bool IsObjCProtocolCandidate (TypeDefinition definition)
		{
			if (!definition.IsPublic)
				return false;
			if (!definition.IsInterface)
				return false;
			if (TypeAggregator.HasProtocolAttribute (definition))
				return true;
			return ImplementsINativeObject (definition);
		}

		bool IsObjCClassCandidate (TypeDefinition definition)
		{
			if (!definition.IsPublic)
				return false;
			if (!definition.IsClass || definition.IsValueType)
				return false;
			if (TypeAggregator.HasSkipRegistration (definition) == true)
				return false;
			if (TypeAggregator.HasModelAttribute (definition))
				return false;
			return ImplementsINativeObject (definition) || IsNSObject (definition);
		}

		bool IsNSObject (TypeDefinition definition)
		{
			if (definition == null)
				return false;
			if (definition.FullName == TypeAggregator.kNSObject)
				return true;
			return IsNSObject (definition.BaseType);
		}

		bool IsNSObject (TypeReference reference)
		{
			if (reference == null)
				return false;
			
			if (reference.FullName == TypeAggregator.kNSObject)
				return true;

			if (reference.Module != null) {
				return IsNSObject (reference.Resolve ());
			}

			if (peerDatabase == null)
				return false;
			var entity = peerDatabase.EntityForDotNetName (new DotNetName (reference.Namespace, reference.Name));
			if (entity != null)
				return entity.Type.IsObjC;
			return false;
		}

		bool ImplementsINativeObject (TypeDefinition definition)
		{
			if (definition == null)
				return false;
			
			if (definition.FullName == TypeAggregator.kINativeObject)
				return true;

			if (!definition.HasInterfaces)
				return false;
			
			return definition.Interfaces.Any (iface => IsINativeObject (iface.InterfaceType));
		}

		bool IsINativeObject (TypeReference reference)
		{
			if (reference == null)
				return false;
			
			if (reference.FullName == TypeAggregator.kINativeObject)
				return true;
			
			if (reference.Module != null) {
				var resolvedReference = reference.Resolve ();
				return ImplementsINativeObject (resolvedReference);
			}
			return false;
		}
	}
}
