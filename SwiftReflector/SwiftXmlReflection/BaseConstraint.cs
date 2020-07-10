// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Xml.Linq;
using SwiftReflector.IOUtils;

namespace SwiftReflector.SwiftXmlReflection {
	public class BaseConstraint : IXElementConvertible {
		protected BaseConstraint (ConstraintKind kind)
		{
			Kind = kind;
		}
		public ConstraintKind Kind { get; private set; }

		public static BaseConstraint FromXElement (XElement elem)
		{
			if (elem == null)
				return null;
			if ((string)elem.Attribute ("relationship") == "inherits") {
				return new InheritanceConstraint ((string)elem.Attribute ("name"), (string)elem.Attribute ("from"));
			} else {
				return new EqualityConstraint ((string)elem.Attribute ("firsttype"), (string)elem.Attribute ("secondtype"));
			}
		}

		public XElement ToXElement ()
		{
			var inh = this as InheritanceConstraint;
			if (inh != null) {
				return new XElement ("where", new XAttribute ("relationship", "inherits"),
									new XAttribute ("from", inh.Inherits));
			} else {
				var eq = (EqualityConstraint)this;
				return new XElement ("where", new XAttribute ("relationship", "equals"),
									new XAttribute ("firstobject", eq.Type1),
									new XAttribute ("secondobject", eq.Type2));
			}
		}

		internal string EffectiveTypeName ()
		{
			var inh = this as InheritanceConstraint;
			if (inh != null)
				return inh.Name;
			var eq = (EqualityConstraint)this;
			string [] pieces = eq.Type1.Split ('.');
			// T, T.U
			if (pieces.Length == 1 || pieces.Length == 2) {
				return pieces [0];
			}
			// Module.T.U
			else if (pieces.Length > 2) {
				return pieces [1];
			}
			return null;
		}

		public static BaseConstraint CopyOf (BaseConstraint baseConstraint)
		{
			if (baseConstraint is InheritanceConstraint inh) {
				return new InheritanceConstraint (inh.Name, inh.Inherits);

			} else if (baseConstraint is EqualityConstraint eq) {
				return new EqualityConstraint (eq.Type1, eq.Type2);
			}
			throw new NotImplementedException ($"Unknown constraint type {baseConstraint.GetType ().Name}");
		}
	}

	public class InheritanceConstraint : BaseConstraint {
		public InheritanceConstraint (string name, string inheritsTypeSpecString)
			: base (ConstraintKind.Inherits)
		{
			Name = Ex.ThrowOnNull (name, nameof (name));
			Inherits = inheritsTypeSpecString;
		}

		public InheritanceConstraint (string name, TypeSpec inheritsTypeSpecString)
			: this (name, inheritsTypeSpecString.ToString ())
		{

		}

		public string Name { get; private set; }
		string inheritsStr;
		TypeSpec inheritsSpec;
		public string Inherits {
			get {
				return inheritsStr;
			}
			set {
				inheritsStr = value;
				if (value != null) {
					inheritsSpec = TypeSpecParser.Parse (value);
				} else {
					inheritsSpec = null;
				}
			}
		}
		public TypeSpec InheritsTypeSpec {
			get {
				return inheritsSpec;
			}
			set {
				inheritsSpec = value;
				if (value != null) {
					inheritsStr = value.ToString ();
				} else {
					inheritsStr = null;
				}
			}
		}
	}

	public class EqualityConstraint : BaseConstraint {
		public EqualityConstraint (string type1, string type2)
			: base (ConstraintKind.Equal)
		{
			Type1 = type1;
			Type2 = type2;
		}
		string type1Str;
		TypeSpec type1Spec;
		public string Type1 {
			get {
				return type1Str;
			}
			set {
				type1Str = value;
				if (value != null) {
					type1Spec = TypeSpecParser.Parse (value);
				} else {
					type1Spec = null;
				}
			}
		}
		public TypeSpec Type1Spec {
			get {
				return type1Spec;
			}
			set {
				type1Spec = value;
				if (value != null) {
					type1Str = value.ToString ();
				} else {
					type1Str = null;
				}
			}
		}
		string type2Str;
		TypeSpec type2Spec;
		public string Type2 {
			get {
				return type2Str;
			}
			set {
				type2Str = value;
				if (value != null) {
					type2Spec = TypeSpecParser.Parse (value);
				} else {
					type2Spec = null;
				}
			}
		}
		public TypeSpec Type2Spec {
			get {
				return type2Spec;
			}
			set {
				type2Spec = value;
				if (value != null) {
					type2Str = value.ToString ();
				} else {
					type2Str = null;
				}
			}
		}


	}
}
