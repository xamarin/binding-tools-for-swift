// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
	}
}

