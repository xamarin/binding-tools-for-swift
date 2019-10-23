// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Xml.Linq;
using SwiftReflector.IOUtils;

namespace SwiftReflector.SwiftXmlReflection {
	public class Member : BaseDeclaration, IXElementConvertible {
		protected Member ()
			: base ()
		{
		}

		protected Member (Member other)
			: base (other)
		{
		}

		public bool IsProtocolMember { get { return Parent != null && Parent is ProtocolDeclaration; } }

		#region IXElementConvertible implementation

		public XElement ToXElement ()
		{
			return MakeXElement ();
		}

		protected virtual XElement MakeXElement ()
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}

