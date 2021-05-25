﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;
using SwiftRuntimeLibrary;

namespace DylibBinder {
	internal class DBTypeDeclaration {
		public string Name { get; }
		public string Module { get; }
		public bool IsObjC { get; } = false;
		public bool IsFinal { get; } = false;
		public bool IsDeprecated { get; } = false;
		public bool IsUnavailable { get; } = false;
		public TypeKind Kind { get; }
		public TypeAccessibility Accessibility { get; } = TypeAccessibility.Public;

		public DBFuncs Funcs { get; }
		public DBProperties Properties { get; }
		public DBGenericParameters GenericParameters { get; }
		public DBAssociatedTypes AssociatedTypes { get; }
		public DBInnerTypes InnerTypes { get; }

		public DBTypeDeclaration (ProtocolContents protoContents)
		{
			Exceptions.ThrowOnNull (protoContents, nameof (protoContents));
			Kind = TypeKind.Protocol;
			Name = protoContents.Name.ToFullyQualifiedName ();
			Module = protoContents.Name.Module.Name;
			Funcs = new DBFuncs (protoContents);
			AssociatedTypes = new DBAssociatedTypes (this);
			ApplyInouts ();
		}

		public DBTypeDeclaration (ClassContents classContents)
		{
			Exceptions.ThrowOnNull (classContents, nameof (classContents));
			Kind = classContents.Name.IsClass ? TypeKind.Class : classContents.Name.IsEnum ? TypeKind.Enum : TypeKind.Struct;
			Name = classContents.Name.ToFullyQualifiedName (false);
			Module = classContents.Name.Module.Name;
			Funcs = new DBFuncs (classContents);
			Properties = new DBProperties (classContents);
			GenericParameters = new DBGenericParameters (this);
			InnerTypes = new DBInnerTypes (classContents.Name);
			AssociatedTypes = new DBAssociatedTypes (this);
			ApplyInouts ();
		}

		// we want to go through all the parameters in this DBTypeDeclaration
		// and add the appropriate type to Instances and Constructors
		void ApplyInouts ()
		{
			foreach (var func in Funcs.FuncCollection) {
				ApplyInouts (func.ParameterLists);
			}
			// protocols currently only hold Funcs so they should finish here
			if (Kind == TypeKind.Protocol)
				return;

			foreach (var property in Properties.PropertyCollection) {
				ApplyInouts (property.Getter.ParameterLists);
				if (property.Setter != null) {
					ApplyInouts (property.Setter.ParameterLists);
				}
			}
		}

		void ApplyInouts (DBParameterLists parameterLists)
		{
			foreach (var parameterList in parameterLists.ParameterListCollection) {
				ApplyInouts (parameterList);
			}
		}

		void ApplyInouts (DBParameterList parameterList)
		{
			foreach (var parameter in parameterList.ParameterCollection) {
				if (parameter.HasInstance || parameter.IsConstructor) {
					ApplyInouts (parameter);
				}
			}
		}

		void ApplyInouts (DBParameter parameter)
		{
			Exceptions.ThrowOnNull (parameter, nameof (parameter));
			var sb = new StringBuilder ();
			sb.Append ("inout ").Append (Module).Append ('.').Append (Name).Append (GenericParametersToString ());
			parameter.Type = parameter.Type.Insert (0, sb.ToString ());
		}

		string GenericParametersToString ()
		{
			var sb = new StringBuilder ();
			var sortedGenericParameters = GenericParameters.GenericParameterCollection.OrderBy (gp => gp.Index);
			foreach (var gp in sortedGenericParameters) {
				if (sb.Length > 0)
					sb.Append ($", ");
				else
					sb.Append ($"<");
				sb.Append (gp.Name);
			}
			if (sb.Length > 0)
				sb.Append ('>');
			return sb.ToString ();
		}

		public List<DBAssociatedType> GetAssociatedTypes ()
			=> Kind == TypeKind.Protocol ? GetAssociatedTypes (Funcs) : GetAssociatedTypes (Funcs, Properties);

		static List<DBAssociatedType> GetAssociatedTypes (params IAssociatedTypes [] items)
			=> items.SelectMany (t => t.AssociatedTypes.AssociatedTypeCollection).ToList ();
	}

	internal class DBTypeDeclarations {
		public SortedDictionary<string, List<DBTypeDeclaration>> TypeDeclarationCollection { get; } = new SortedDictionary<string, List<DBTypeDeclaration>> ();
		public static Dictionary<string, List<ClassContents>> InnerXDictionary { get; private set; } = new Dictionary<string, List<ClassContents>> ();
		public string IgnoreListPath { get; set; }

		public DBTypeDeclarations (ModuleInventory mi, string ignoreListPath) {
			Exceptions.ThrowOnNull (mi, nameof (mi));
			IgnoreListPath = ignoreListPath;
			var checkInventory = new CheckInventoryDictionary (mi);
			var innerX = new InnerX ();

			foreach (var module in checkInventory.CheckInventoryDict.Keys) {
				InnerXDictionary = innerX.AddClassContentsList (checkInventory.CheckInventoryDict[module].Classes,
				                                                checkInventory.CheckInventoryDict [module].Enums,
				                                                checkInventory.CheckInventoryDict [module].Structs);
				FilterTypeDeclarations (module, checkInventory.CheckInventoryDict [module].Protocols,
				                        checkInventory.CheckInventoryDict [module].Classes,
				                        checkInventory.CheckInventoryDict [module].Structs,
				                        checkInventory.CheckInventoryDict [module].Enums);
			}
		}

		void FilterTypeDeclarations (string module, SortedSet<ProtocolContents> protocolContentList, params SortedSet<ClassContents>[] ClassContentListArray)
		{
			Exceptions.ThrowOnNull (module, nameof (module));
			if (!TypeDeclarationCollection.ContainsKey (module))
				TypeDeclarationCollection.Add (module, new List<DBTypeDeclaration> ());

			var ignoredTypes = GetIgnoredTypes ();

			foreach (var classContentsList in ClassContentListArray) {
				foreach (var classContents in classContentsList) {
					if (classContents.Name.ToFullyQualifiedName ().IsPublic () && InnerXDictionary.ContainsKey (classContents.Name.ToFullyQualifiedName ())
						&& !ignoredTypes.Contains(classContents.Name.ToFullyQualifiedName ()))
						TypeDeclarationCollection [module].Add (new DBTypeDeclaration (classContents));
				}
			}

			foreach (var protocolContents in protocolContentList) {
				if (protocolContents.Name.ToFullyQualifiedName ().IsPublic () && !ignoredTypes.Contains (protocolContents.Name.ToFullyQualifiedName ()))
					TypeDeclarationCollection [module].Add (new DBTypeDeclaration (protocolContents));
			}
		}

		SortedSet<string> GetIgnoredTypes ()
		{
			var ignoredTypes = SortedSetExtensions.Create<string> ();
			if (File.Exists (IgnoreListPath)) {
				ignoredTypes.AddRange (File.ReadAllLines (IgnoreListPath).Where (line => !line.StartsWith ("//", StringComparison.Ordinal) && line != string.Empty).ToList ());
			}
			return ignoredTypes;
		}
	}

	internal static class DBTypeDeclarationExtensions {
		public static List<DBGenericParameter> ParseTopLevelGenerics (this DBTypeDeclaration typeDeclaration)
		{
			Exceptions.ThrowOnNull (typeDeclaration, nameof (typeDeclaration));
			var GenericParameters = new List<DBGenericParameter> ();
			foreach (var func in typeDeclaration.Funcs.FuncCollection) {
				GenericParameters.AddRange (GrabTopLevelGenerics (func.GenericParameters));
			}

			foreach (var prop in typeDeclaration.Properties.PropertyCollection) {
				GenericParameters.AddRange (GrabTopLevelGenerics (prop.GenericParameters));
			}
			return GenericParameters;
		}

		public static List<DBGenericParameter> GrabTopLevelGenerics (DBGenericParameters GenericParameterCollection)
		{
			Exceptions.ThrowOnNull (GenericParameterCollection, nameof (GenericParameterCollection));
			var genericParametersList = new List<DBGenericParameter> ();
			foreach (var gp in GenericParameterCollection.GenericParameterCollection) {
				if (gp.Depth == 0)
					genericParametersList.Add (gp);
			}
			return genericParametersList;
		}
	}
}