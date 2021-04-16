using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DylibBinder;
using SwiftReflector;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;

namespace DylibBinder {
	internal class DBTypeDeclaration {
		public DBTypeDeclaration (ProtocolContents protoContents)
		{
			Kind = TypeKind.Protocol;
			Name = protoContents.Name.ToFullyQualifiedName ();
			Module = protoContents.Name.Module.Name;
			Funcs = new DBFuncs (protoContents);
			AssociatedTypes = new DBAssociatedTypes (this);
			ApplyInouts ();
		}

		public DBTypeDeclaration (ClassContents classContents)
		{
			if (classContents.Name.IsClass)
				Kind = TypeKind.Class;
			else if (classContents.Name.IsEnum)
				Kind = TypeKind.Enum;
			else
				Kind = TypeKind.Struct;

			Name = classContents.Name.ToFullyQualifiedName (false);
			Module = classContents.Name.Module.Name;
			Funcs = new DBFuncs (classContents);
			Properties = new DBProperties (classContents);
			GenericParameters = new DBGenericParameters (this);
			InnerTypes = new DBInnerTypes (classContents.Name);
			AssociatedTypes = new DBAssociatedTypes (this);
			ApplyInouts ();
		}

		public TypeKind Kind { get; }
		public string Name { get; }
		public string Module { get; }
		public TypeAccessibility Accessibility { get; } = TypeAccessibility.Public;
		public bool IsObjC { get; } = false;
		public bool IsFinal { get; } = false;
		public bool IsDeprecated { get; } = false;
		public bool IsUnavailable { get; } = false;

		public DBFuncs Funcs { get; }
		public DBProperties Properties { get; }
		public DBGenericParameters GenericParameters { get; }
		public DBAssociatedTypes AssociatedTypes { get; }
		public DBInnerTypes InnerTypes { get; }

		// we want to go through all the parameters in this DBTypeDeclaration
		// and add the appropriate type to Instances and Constructors
		void ApplyInouts ()
		{
			foreach (var func in Funcs.Funcs) {
				ApplyInoutsToParameterLists (func.ParameterLists);
			}
			// protocols currently only hold funcs so they should finish here
			if (Kind == TypeKind.Protocol)
				return;

			foreach (var property in Properties.Properties) {
				ApplyInoutsToParameterLists (property.Getter.ParameterLists);
				if (property.Setter != null) {
					ApplyInoutsToParameterLists (property.Setter.ParameterLists);
				}
			}
		}

		void ApplyInoutsToParameterLists (DBParameterLists parameterLists)
		{
			foreach (var parameterList in parameterLists.ParameterLists) {
				ApplyInoutsToParameterList (parameterList);
			}
		}

		void ApplyInoutsToParameterList (DBParameterList parameterList)
		{
			foreach (var parameter in parameterList.Parameters) {
				if (parameter.HasInstance || parameter.IsConstructor) {
					ApplyInoutsToParameter (parameter);
				}
			}
		}

		void ApplyInoutsToParameter (DBParameter parameter)
		{
			var sb = new StringBuilder ();
			sb.Append ($"inout {Module}.{Name}{GenericParametersToString ()}");
			parameter.Type = parameter.Type.Insert (0, sb.ToString ());
		}

		string GenericParametersToString ()
		{
			var sb = new StringBuilder ();
			var sortedGenericParameters = GenericParameters.GenericParameters.OrderBy (gp => gp.Index);
			foreach (var gp in sortedGenericParameters) {
				if (sb.Length > 0)
					sb.Append ($", {gp.Name}");
				else
					sb.Append ($"<{gp.Name}");
			}
			if (sb.Length > 0)
				sb.Append ('>');
			return sb.ToString ();
		}
	}

	internal class DBTypeDeclarations {
		public DBTypeDeclarations (ModuleInventory mi) {
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
			if (!TypeDeclarations.ContainsKey (module))
				TypeDeclarations.Add (module, new List<DBTypeDeclaration> ());

			var ignoreListPath = $"{Directory.GetCurrentDirectory ()}/../../IgnoreList.txt";
			var ignoredTypes = SortedSetExtensions.Create<string> ();
			ignoredTypes.AddRange (File.ReadAllLines (ignoreListPath).Where (line => !line.StartsWith ("//", StringComparison.Ordinal) && line != string.Empty).ToList ());

			foreach (var classContentsList in ClassContentListArray) {
				foreach (var classContents in classContentsList) {
					if (classContents.Name.ToFullyQualifiedName ().IsPublic () && InnerXDictionary.ContainsKey (classContents.Name.ToFullyQualifiedName ())
						&& !ignoredTypes.Contains(classContents.Name.ToFullyQualifiedName ()))
						TypeDeclarations [module].Add (new DBTypeDeclaration (classContents));
				}
			}

			foreach (var protocolContents in protocolContentList) {
				if (protocolContents.Name.ToFullyQualifiedName ().IsPublic () && !ignoredTypes.Contains (protocolContents.Name.ToFullyQualifiedName ()))
					TypeDeclarations [module].Add (new DBTypeDeclaration (protocolContents));
			}
		}

		public SortedDictionary<string, List<DBTypeDeclaration>> TypeDeclarations { get; } = new SortedDictionary<string, List<DBTypeDeclaration>> ();
		public static Dictionary<string, List<ClassContents>> InnerXDictionary { get; private set; } = new Dictionary<string, List<ClassContents>> ();
	}

	internal static class DBTypeDeclarationExtensions {
		public static List<DBGenericParameter> ParseTopLevelGenerics (this DBTypeDeclaration typeDeclaration)
		{
			var genericParameters = new List<DBGenericParameter> ();
			foreach (var func in typeDeclaration.Funcs.Funcs) {
				genericParameters.AddRange (GrabTopLevelGenerics (func.GenericParameters));
			}

			foreach (var prop in typeDeclaration.Properties.Properties) {
				genericParameters.AddRange (GrabTopLevelGenerics (prop.GenericParameters));
			}
			return genericParameters;
		}

		public static List<DBGenericParameter> GrabTopLevelGenerics (DBGenericParameters genericParameters)
		{
			var genericParametersList = new List<DBGenericParameter> ();
			foreach (var gp in genericParameters.GenericParameters) {
				if (gp.Depth == 0)
					genericParametersList.Add (gp);
			}
			return genericParametersList;
		}
	}
}
