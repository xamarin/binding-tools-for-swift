// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using SwiftReflector.Exceptions;
using ObjCRuntime;

namespace SwiftReflector.SwiftXmlReflection {
	public class BaseDeclaration {
		protected BaseDeclaration ()
		{
			Generics = new GenericDeclarationCollection ();
		}

		protected BaseDeclaration (BaseDeclaration other)
		{
			Name = other.Name;
			Access = other.Access;
			Module = other.Module;
			Parent = other.Parent;
			ParentExtension = other.ParentExtension;
			Generics = new GenericDeclarationCollection ();
		}

		public string Name { get; set; }
		public Accessibility Access { get; set; }
		public ModuleDeclaration Module { get; set; }
		public BaseDeclaration Parent { get; set; }
		public GenericDeclarationCollection Generics { get; private set; }
		public ExtensionDeclaration ParentExtension { get; set; }
		public bool IsExtension { get { return ParentExtension != null; } }
		public bool ContainsGenericParameters {
			get {
				return Generics.Count () > 0;
			}
		}

		public bool IsTypeSpecBoundGeneric (TypeSpec sp)
		{
			if (sp.ContainsGenericParameters) {
				foreach (var gen in sp.GenericParameters) {
					if (IsTypeSpecGeneric (gen))
						return false;
				}
				return true;
			}
			return false;
		}

		public bool IsTypeSpecAssociatedType (NamedTypeSpec named)
		{
			var proto = ThisOrParentProtocol (this);
			if (proto == null)
				return false;
			if (named.ContainsGenericParameters) {
				foreach (var gen in named.GenericParameters) {
					if (gen is NamedTypeSpec namedGen && IsTypeSpecAssociatedType (namedGen))
						return true;
				}
			}
			return proto.AssociatedTypeNamed (named.NameWithoutModule) != null;
		}

		public AssociatedTypeDeclaration AssociatedTypeDeclarationFromNamedTypeSpec (NamedTypeSpec named)
		{
			var proto = ThisOrParentProtocol (this);
			return proto.AssociatedTypeNamed (named.NameWithoutModule);
		}

		public ProtocolDeclaration AsProtocolOrParentAsProtocol ()
		{
			return ThisOrParentProtocol (this);
		}

		static ProtocolDeclaration ThisOrParentProtocol (BaseDeclaration self)
		{
			if (self == null)
				return null;

			do {
				if (self is ProtocolDeclaration decl)
					return decl;
				self = self.Parent;
			} while (self != null);
			return null;
		}

		public bool IsTypeSpecGeneric (TypeSpec sp)
		{
			if (sp.ContainsGenericParameters) {
				foreach (var gen in sp.GenericParameters) {
					if (IsTypeSpecGeneric (gen))
						return true;
				}
			}

			if (sp is NamedTypeSpec named) {
				return IsTypeSpecGeneric (named.Name);
			} else if (sp is ClosureTypeSpec closure) {
				return IsTypeSpecGeneric (closure.Arguments) || IsTypeSpecGeneric (closure.ReturnType);
			} else if (sp is TupleTypeSpec tuple) {
				foreach (var tupSpec in tuple.Elements) {
					if (IsTypeSpecGeneric (tupSpec))
						return true;
				}
				return false;
			} else if (sp is ProtocolListTypeSpec) {
				// protocol list type specs can't be generic.
				return false;
			} else {
				throw new NotImplementedException ($"Unknown TypeSpec type {sp.GetType ().Name}");
			}
		}

		public bool IsTypeSpecGenericReference (TypeSpec sp)
		{
			if (sp.ContainsGenericParameters)
				return false;
			var ns = sp as NamedTypeSpec;
			return ns != null && IsTypeSpecGeneric (ns.Name);
		}

		public bool IsTypeSpecGeneric (string typeSpecName)
		{
			foreach (GenericDeclaration gendecl in Generics) {
				if (typeSpecName == gendecl.Name)
					return true;
			}
			if (Parent != null) {
				return Parent.IsTypeSpecGeneric (typeSpecName);
			} else {
				return false;
			}
		}

		public int GetTotalDepth ()
		{
			int depth = 0;
			BaseDeclaration bd = this;
			while (bd.Parent != null) {
				if (Parent.ContainsGenericParameters)
					depth++;
				bd = bd.Parent;
			}
			return depth;
		}

		public Tuple<int, int> GetGenericDepthAndIndex (string name)
		{
			return GetGenericDepthAndIndex (name, GetTotalDepth ());
		}

		public Tuple<int, int> GetGenericDepthAndIndex (TypeSpec spec)
		{
			var ns = spec as NamedTypeSpec;
			if (ns == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerBase + 5, $"Can't get generic depth from a {spec.GetType ().Name}.");
			return GetGenericDepthAndIndex (ns.Name);
		}

		Tuple<int, int> GetGenericDepthAndIndex (string name, int depth)
		{
			for (int i = 0; i < Generics.Count; i++) {
				if (Generics [i].Name == name)
					return new Tuple<int, int> (depth, i);
			}
			if (Parent != null) {
				return Parent.GetGenericDepthAndIndex (name, depth - 1);
			}
			return new Tuple<int, int> (-1, -1);
		}

		public GenericDeclaration GetGeneric (int depth, int index)
		{
			var parentsToWalk = GetMaxDepth () - depth;
			BaseDeclaration decl = this;
			do {
				// skip runs of no generics
				while (!decl.ContainsGenericParameters) {
					decl = decl.Parent;
				}
				if (parentsToWalk > 0) {
					parentsToWalk--;
					decl = decl.Parent;
				}
			} while (parentsToWalk > 0);
			return decl.Generics [index];
		}


		int GetMaxDepth (int depth)
		{
			depth += (ContainsGenericParameters ? 1 : 0);
			if (Parent == null)
				return depth;
			return Parent.GetMaxDepth (depth);
		}

		public int GetMaxDepth()
		{
			return GetMaxDepth (-1);
		}

		public bool IsPublicOrOpen {
			get {
				return Access == Accessibility.Public || Access == Accessibility.Open;
			}
		}


		public static BaseDeclaration FromXElement (XElement elem, ModuleDeclaration module, BaseDeclaration parent)
		{
			var generics = GenericDeclaration.FromXElement (elem.Element ("genericparameters"));
			BaseDeclaration decl = null;
			switch (elem.Name.ToString ()) {
			case "func":
				decl = FunctionDeclaration.FuncFromXElement (elem, module, parent);
				break;
			case "typedeclaration":
				decl = TypeDeclaration.TypeFromXElement (elem, module, parent);
				break;
			case "property":
				decl = PropertyDeclaration.PropFromXElement (elem, module, parent);
				break;
			default:
				decl = new BaseDeclaration {
					Name = (string)elem.Attribute ("name"),
					Access = TypeDeclaration.AccessibilityFromString ((string)elem.Attribute ("accessibility"))
				};
				break;
			}
			decl.Generics.AddRange (generics);
			return decl;
		}

		public virtual string ToFullyQualifiedName (bool includeModule = true)
		{
			var sb = new StringBuilder ();
			BaseDeclaration decl = this;
			// recursion? We don't need to stinking recursion!
			while (decl != null) {
				TypeDeclaration typeDecl = decl as TypeDeclaration;
				// unrooted types have no parent, but do have a fully qualified name
				if (typeDecl != null && typeDecl.IsUnrooted) {
					sb.Insert (0, typeDecl.ToFullyQualifiedName (false));
					break;
				} else {
					sb.Insert (0, decl.Name);
					decl = decl.Parent;
					if (decl != null)
						sb.Insert (0, '.');
				}
			}
			if (includeModule) {
				sb.Insert (0, '.').Insert (0, Module.Name);
			}
			return sb.ToString ();
		}

		public virtual string ToFullyQualifiedNameWithGenerics ()
		{
			var sb = new StringBuilder (ToFullyQualifiedName ());
			if (ContainsGenericParameters) {
				sb.Append ("<");
				for (int i = 0; i < Generics.Count; i++) {
					if (i > 0)
						sb.Append (", ");
					sb.Append (Generics [i].Name);
				}
				sb.Append (">");
			}
			return sb.ToString ();
		}
	}

}

