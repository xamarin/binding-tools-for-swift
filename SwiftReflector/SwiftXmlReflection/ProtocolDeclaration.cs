// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SwiftReflector.SwiftXmlReflection {
	public class ProtocolDeclaration : ClassDeclaration {
		public ProtocolDeclaration ()
		{
			Kind = TypeKind.Protocol;
			AssociatedTypes = new List<AssociatedTypeDeclaration> ();
		}

		protected override TypeDeclaration UnrootedFactory ()
		{
			return new ProtocolDeclaration ();
		}

		protected override void GatherXObjects (List<XObject> xobjects)
		{
			base.GatherXObjects (xobjects);
			if (AssociatedTypes.Count <= 0)
				return;
			var assocTypes = new List<XObject> ();
			foreach (var assoc in AssociatedTypes) {
				var contents = new List<XObject> ();
				assoc.GatherXObjects (contents);
				assocTypes.Add (new XElement ("associatedtype", contents.ToArray ()));
			}
			xobjects.Add (new XElement ("associatedtypes", assocTypes.ToArray ()));
		}

		protected override void CompleteUnrooting (TypeDeclaration unrooted)
		{
			base.CompleteUnrooting (unrooted);
			if (unrooted is ProtocolDeclaration pd) {
				pd.AssociatedTypes.AddRange (AssociatedTypes);
			}
		}

		public List<AssociatedTypeDeclaration> AssociatedTypes { get; private set; }

		public bool HasAssociatedTypes => AssociatedTypes.Count > 0;

		public AssociatedTypeDeclaration AssociatedTypeNamed (string name)
		{
			return AssociatedTypes.FirstOrDefault (at => at.Name == name);
		}

		public bool HasDynamicSelf {
			// you could cache this, but this type is not mutable, so that would be bad
			get => SearchForDynamicSelf ();
		}

		bool SearchForDynamicSelf ()
		{
			foreach (var member in this.Members) {
				if (member is FunctionDeclaration funcDecl) {
					if (SearchForDynamicSelf (funcDecl))
						return true;
				} else if (member is PropertyDeclaration propDecl) {
					if (SearchForDynamicSelf (propDecl)) {
						return true;
					}
				} else {
					throw new NotImplementedException ($"Unknown MemberDeclaration type {member.GetType ().Name}");
				}
			}
			return false;
		}

		bool SearchForDynamicSelf (FunctionDeclaration funcDecl)
		{
			var types = funcDecl.ParameterLists.Last ().Select (p => p.TypeSpec).ToList ();
			if (!TypeSpec.IsNullOrEmptyTuple (funcDecl.ReturnTypeSpec))
				types.Add (funcDecl.ReturnTypeSpec);
			return SearchForDynamicSelf (types);
		}

		bool SearchForDynamicSelf (PropertyDeclaration propDecl)
		{
			return SearchForDynamicSelf (propDecl.TypeSpec);
		}

		bool SearchForDynamicSelf (List<TypeSpec> types)
		{
			foreach (var type in types) {
				if (SearchForDynamicSelf (type))
					return true;
			}
			return false;
		}

		bool SearchForDynamicSelf (TypeSpec type)
		{
			if (type is NamedTypeSpec ns) {
				if (SearchForDynamicSelf (ns))
					return true;
			} else if (type is TupleTypeSpec tuple) {
				if (SearchForDynamicSelf (tuple.Elements))
					return true;
			} else if (type is ClosureTypeSpec closure) {
				if (SearchForDynamicSelf (closure.Arguments)) {
					return true;
				}
				if (!TypeSpec.IsNullOrEmptyTuple (closure.ReturnType) &&
					SearchForDynamicSelf (closure.ReturnType)) {
					return true;
				}
			}
			// don't care about protocol list
			return false;
		}

		bool SearchForDynamicSelf (NamedTypeSpec namedTypeSpec)
		{
			if (namedTypeSpec.Name == "Self")
				return true;
			return SearchForDynamicSelf (namedTypeSpec.GenericParameters);
		}
	}
}

