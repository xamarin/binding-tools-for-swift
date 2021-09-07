// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using SwiftReflector.SwiftXmlReflection;
using SwiftRuntimeLibrary;

namespace SwiftReflector {
	public class PatToGenericMap {
		const string kGenPrefix = "AV";
		const int kFormatDigits = 3;
		static readonly string kGenFormat = $"D{kFormatDigits}";
		ProtocolDeclaration protocolDecl;
		Dictionary<string, int> nameIndexMap = new Dictionary<string, int> ();

		public PatToGenericMap (ProtocolDeclaration protocolDecl)
		{
			Exceptions.ThrowOnNull (protocolDecl, nameof (protocolDecl));
			if (!(protocolDecl.HasAssociatedTypes || (protocolDecl.HasDynamicSelf && !protocolDecl.HasDynamicSelfInReturnOnly)))
				throw new ArgumentException ("ProtocolDeclaration has no associated types", nameof (protocolDecl));
			this.protocolDecl = protocolDecl;
			AddAssocTypesToNameMap (this.protocolDecl);
		}

		void AddAssocTypesToNameMap (ProtocolDeclaration decl)
		{
			for (int i = 0; i < decl.AssociatedTypes.Count; i++) {
				nameIndexMap.Add (OverrideBuilder.GenericAssociatedTypeName (decl.AssociatedTypes [i]), i);
			}
			if (decl.HasDynamicSelf) {
				nameIndexMap.Add (OverrideBuilder.GenericAssociatedTypeName ("Self"), decl.AssociatedTypes.Count);
			}
		}

		public string GenericTypeNameFor (string associatedTypeName)
		{
			var index = AssociatedTypeIndex (associatedTypeName);
			// why, you may ask, am I using this scheme for naming associated types?
			// Ordering of the names matters for how the swift type metadata gets passed, so let's use
			// type names that are (1) horrible so that no one is like to use them (2) will come alphabetically after
			// the any associated type generic name which are prefixed with 'AT' and and for the generic specifier for
			// the protocol itself, which will be 'AU'. The digits encode the ordering of the associated type
			// and (3) encode the original type name so there is some readability in there (4) the prefix is a fixed size.

			// "no one will ever need more than 1000 associated types in a protocol."
			if (index >= 1000)
				throw new ArgumentOutOfRangeException (nameof (associatedTypeName), "> 1000 associated types");
			return $"{kGenPrefix}{index.ToString (kGenFormat)}{associatedTypeName}";
		}

		public AssociatedTypeDeclaration FromGenericTypeName (string genericName)
		{
			Exceptions.ThrowOnNull (genericName, nameof (genericName));

			// parsing is hard, yo.
			if (!genericName.StartsWith (kGenPrefix, StringComparison.Ordinal))
				throw new ArgumentOutOfRangeException (nameof (genericName), $"Expecting generic name '{genericName}' to start with {kGenPrefix}");
			var minimumLength = kGenPrefix.Length + kFormatDigits + 1;
			if (genericName.Length < minimumLength)
				throw new ArgumentOutOfRangeException (nameof (genericName), $"Expecting generic name '{genericName}' to have at least {minimumLength} characters");
			var indexString = genericName.Substring (kGenPrefix.Length, kFormatDigits);
			var index = Int32.Parse (indexString);
			if (index < 0)
				throw new ArgumentOutOfRangeException (nameof (genericName), $"Expecting a non-negative number in '{genericName}'");

			if (index == protocolDecl.AssociatedTypes.Count && protocolDecl.HasDynamicSelf)
				return new AssociatedTypeDeclaration () { Name = "Self" };

			if (index >= protocolDecl.AssociatedTypes.Count)
				throw new ArgumentOutOfRangeException (nameof (genericName), $"Index value {index} from generic name '{genericName}' is out of range of the associated type collection");

			return protocolDecl.AssociatedTypes [index];
		}

		public string AssociatedTypeNameFromGenericTypeName (string genericName)
		{
			return FromGenericTypeName (genericName).Name;
		}

		int AssociatedTypeIndex (string associatedTypeName)
		{
			var index = 0;
			if (nameIndexMap.TryGetValue (associatedTypeName, out index))
				return index;
			return protocolDecl.AssociatedTypes.FindIndex (assoc => assoc.Name == associatedTypeName);
		}

		public List<string> UniqueGenericTypeNamesFor (FunctionDeclaration funcDecl)
		{
			HashSet<string> assocTypes = new HashSet<string> ();

			GetUniqueGenericTypeNamesFor (funcDecl.ReturnTypeSpec, assocTypes);
			foreach (var arg in funcDecl.ParameterLists.Last ()) {
				GetUniqueGenericTypeNamesFor (arg.TypeSpec, assocTypes);
			}

			var uniques = assocTypes.ToList ();
			uniques.Sort ();
			return uniques;
		}

		void GetUniqueGenericTypeNamesFor (TypeSpec candidate, HashSet<string> result)
		{
			if (TypeSpec.IsNullOrEmptyTuple (candidate))
				return;
			switch (candidate.Kind) {
			case TypeSpecKind.Closure:
				GetUniqueGenericTypeNamesFor (candidate as ClosureTypeSpec, result);
				break;
			case TypeSpecKind.Named:
				GetUniqueGenericTypeNamesFor (candidate as NamedTypeSpec, result);
				break;
			case TypeSpecKind.Tuple:
				GetUniqueGenericTypeNamesFor (candidate as TupleTypeSpec, result);
				break;
			case TypeSpecKind.ProtocolList:
				GetUniqueGenericTypeNamesFor (candidate as ProtocolListTypeSpec, result);
				break;
			default:
				throw new NotImplementedException ($"Unknown type spec kind {candidate.Kind.ToString ()}");
			}
		}

		AssociatedTypeDeclaration GetAssociatedTypeNamed (string name)
		{
			var index = 0;
			if (nameIndexMap.TryGetValue (name, out index)) {
				return protocolDecl.AssociatedTypes [index];
			}
			return protocolDecl.AssociatedTypeNamed (name);
		}

		void GetUniqueGenericTypeNamesFor (ClosureTypeSpec candidate, HashSet<string> result)
		{
			GetUniqueGenericTypeNamesFor (candidate.Arguments, result);
			GetUniqueGenericTypeNamesFor (candidate.ReturnType, result);
		}

		void GetUniqueGenericTypeNamesFor (NamedTypeSpec candidate, HashSet<string> result)
		{
			var assocType = GetAssociatedTypeNamed (candidate.Name);
			if (assocType != null)
				result.Add (GenericTypeNameFor (assocType.Name));
			if (candidate.GenericParameters == null)
				return;
			foreach (var gen in candidate.GenericParameters) {
				GetUniqueGenericTypeNamesFor (gen, result);
			}
		}

		void GetUniqueGenericTypeNamesFor (TupleTypeSpec candidate, HashSet<string> result)
		{
			foreach (var element in candidate.Elements) {
				GetUniqueGenericTypeNamesFor (element, result);
			}
		}

		void GetUniqueGenericTypeNamesFor (ProtocolListTypeSpec candidate, HashSet<string> result)
		{
			foreach (var element in candidate.Protocols.Keys) {
				GetUniqueGenericTypeNamesFor (element, result);
			}
		}

		public List<ParameterItem> RebuildParameterListWithGenericTypes (List<ParameterItem> pl)
		{
			var result = new List<ParameterItem> (pl.Count);
			foreach (var item in pl) {
				var newItem = new ParameterItem (item);
				newItem.TypeSpec = RebuildTypeWithGenericType (item.TypeSpec);
				result.Add (newItem);
			}
			return result;
		}

		public TypeSpec RebuildTypeWithGenericType (TypeSpec type)
		{
			bool changed;
			var newType = RebuildTypeWithGenericType (type, out changed);
			return changed ? newType : type;
		}

		TypeSpec RebuildTypeWithGenericType (TypeSpec type, out bool changed)
		{
			if (TypeSpec.IsNullOrEmptyTuple (type)) {
				changed = false;
				return type;
			}
			switch (type.Kind) {
			case TypeSpecKind.Closure:
				return RebuildTypeWithGenericType (type as ClosureTypeSpec, out changed);
			case TypeSpecKind.Named:
				return RebuildTypeWithGenericType (type as NamedTypeSpec, out changed);
			case TypeSpecKind.Tuple:
				return RebuildTypeWithGenericType (type as TupleTypeSpec, out changed);
			case TypeSpecKind.ProtocolList:
				return RebuildTypeWithGenericType (type as ProtocolListTypeSpec, out changed);
			default:
				throw new NotImplementedException ($"Unknown type spec kind {type.Kind.ToString ()}");
			}
		}

		TypeSpec RebuildTypeWithGenericType (ClosureTypeSpec type, out bool changed)
		{
			bool argsChanged, resultChanged;
			var args = RebuildTypeWithGenericType (type.Arguments, out argsChanged);
			var result = RebuildTypeWithGenericType (type.ReturnType, out resultChanged);
			changed = argsChanged || resultChanged;
			if (changed) {
				return new ClosureTypeSpec (args, result);
			}
			return type;
		}

		TypeSpec RebuildTypeWithGenericType (NamedTypeSpec type, out bool changed)
		{
			bool nameChanged = false, genArgsChanged = false;
			string newName = null;
			var assocType = GetAssociatedTypeNamed (type.Name);
			if (assocType != null) {
				nameChanged = true;
				newName = GenericTypeNameFor (assocType.Name);
			}
			TypeSpec [] newGenParms = type.GenericParameters.ToArray ();
			if (type.GenericParameters != null) {
				newGenParms = new TypeSpec [type.GenericParameters.Count];
				for (int i = 0; i < newGenParms.Length; i++) {
					bool genChanged;
					newGenParms [i] = (RebuildTypeWithGenericType (type.GenericParameters [i], out genChanged));
					genArgsChanged = genArgsChanged || genChanged;
				}
			}
			changed = nameChanged || genArgsChanged;
			if (changed) {
				return new NamedTypeSpec (newName ?? type.Name, newGenParms);
			}
			return type;
		}

		TypeSpec RebuildTypeWithGenericType (TupleTypeSpec type, out bool changed)
		{
			changed = false;
			var newTupleElems = new TypeSpec [type.Elements.Count];
			for (int i = 0; i < type.Elements.Count; i++) {
				bool elemChanged;
				newTupleElems [i] = RebuildTypeWithGenericType (type.Elements [i], out elemChanged);
				changed = changed || elemChanged;
			}

			if (changed) {
				return new TupleTypeSpec (newTupleElems);
			}
			return type;
		}
	}
}
