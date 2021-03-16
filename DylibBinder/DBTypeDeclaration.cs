using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DylibBinder;
using SwiftReflector;
using SwiftReflector.Inventory;

namespace DylibBinder {
	public class DBTypeDeclaration {
		public DBTypeDeclaration (ProtocolContents protoContents)
		{
			Kind = "protocol";
			Name = protoContents.Name.ToString ();
			Funcs = new DBFuncs (protoContents);
			ApplyInouts ();
		}

		public DBTypeDeclaration (ClassContents classContents)
		{
			if (classContents.Name.IsClass)
				Kind = "class";
			else if (classContents.Name.IsEnum)
				Kind = "enum";
			else
				Kind = "struct";

			Name = classContents.Name.ToFullyQualifiedName ();
			Funcs = new DBFuncs (classContents);
			Properties = new DBProperties (classContents);
			GenericParameters = new DBGenericParameters (this);
			InnerTypes = new DBInnerTypes (classContents.Name);
			AssociatedTypes = new DBAssociatedTypes ();
			Elements = new DBElements ();
			ApplyInouts ();
		}

		public string Kind { get; }
		public string Name { get; }
		public string Accessibility { get; } = "Public";
		public bool IsObjC { get; } = false;
		public bool IsFinal { get; } = false;
		public bool IsDeprecated { get; } = false;
		public bool IsUnavailable { get; } = false;

		public DBFuncs Funcs { get; }
		public DBProperties Properties { get; }
		public DBGenericParameters GenericParameters { get; }
		public DBAssociatedTypes AssociatedTypes { get; }
		public DBInnerTypes InnerTypes { get; }
		public DBElements Elements { get; }

		// we want to go through all the parameters in this DBTypeDeclaration
		// and add the appropriate type to Instances and Constructors
		void ApplyInouts ()
		{
			foreach (var func in Funcs.Funcs) {
				ApplyInoutsToParameterLists (func.ParameterLists);
			}
			// protocols currently only hold funcs so they should finish here
			if (Kind == "protocol")
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
			sb.Append ($"inout {Name}{GenericParametersToString ()}");
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

	public class DBTypeDeclarations {
		public DBTypeDeclarations (ModuleInventory mi) {
			var checkInventory = new CheckInventory (mi);
			var innerX = new InnerX ();
			InnerXDictionary = innerX.AddClassContentsList (checkInventory.Classes, checkInventory.Enums, checkInventory.Structs);
			FilterTypeDeclarations (checkInventory.Protocols, checkInventory.Classes, checkInventory.Structs, checkInventory.Enums);
		}

		void FilterTypeDeclarations (List<ProtocolContents> protocolContentList, params List<ClassContents>[] ClassContentListArray)
		{
			foreach (var classContentsList in ClassContentListArray) {
				foreach (var classContents in classContentsList) {
					if (classContents.Name.ToString ().IsPublic () && InnerXDictionary.ContainsKey (classContents.Name.ToString ()))
						TypeDeclarations.Add (new DBTypeDeclaration (classContents));
				}
			}

			foreach (var protocolContents in protocolContentList) {
				if (protocolContents.Name.ToString ().IsPublic ())
					TypeDeclarations.Add (new DBTypeDeclaration (protocolContents));
			}
		}

		public List<DBTypeDeclaration> TypeDeclarations { get; } = new List<DBTypeDeclaration> ();
		public static Dictionary<string, List<ClassContents>> InnerXDictionary { get; private set; } = new Dictionary<string, List<ClassContents>> ();
	}
}
