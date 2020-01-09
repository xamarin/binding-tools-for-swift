// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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

		protected virtual void GatherXObjects (List<XObject> xobjects)
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

		public List<AssociatedTypeDeclaration> AssociatedTypes { get; private set; }
	}
}

