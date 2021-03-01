// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;

namespace Dynamo.SwiftLang {
	public class SLImport : SimpleLineElement {
		public SLImport (string module, ImportKind kind = ImportKind.None)
			: base (string.Format ("import {0}{1}", ToImportKindString (kind),
			                       Exceptions.ThrowOnNull (module, nameof(module))),
			        false, false, false)
		{
			// TJ - since we do not use libswiftCore as an import module
			// let's ignore it here
			if (module == "libswiftCore" || module == "U0" || module == "T0") {
				return;
			}

			Module = module;
		}

		public string Module { get; private set; }

		static string ToImportKindString (ImportKind kind)
		{
			switch (kind) {
			case ImportKind.None:
				return "";
			case ImportKind.Class:
				return "class ";
			case ImportKind.Enum:
				return "enum ";
			case ImportKind.Func:
				return "func ";
			case ImportKind.Protocol:
				return "protocol ";
			case ImportKind.Struct:
				return "struct ";
			case ImportKind.TypeAlias:
				return "typealias ";
			case ImportKind.Var:
				return "var ";
			default:
				throw new ArgumentOutOfRangeException (nameof(kind));
			}
		}
	}

	public class SLImportModules : CodeElementCollection<SLImport> {
		public SLImportModules () : base () { }
		public SLImportModules (params SLImport [] imp)
			: this ()
		{
			AddRange (imp);
		}
		public SLImportModules (params string [] imp)
			: this ()
		{
			AddRange (imp.Select (s => new SLImport (s)));
		}

		public SLImportModules And (SLImport use)
		{
			if (OwningModule != null && use.Module == OwningModule)
				return this;
			if (use.Module == "Self")
				return this;
			Add (use);
			return this;
		}

		public SLImportModules And (string package) { return And (new SLImport (package)); }

		public void AddIfNotPresent (string package)
		{
			SLImport target = new SLImport (package);
			if (package == "Self")
				return;

			// TJ - since we do not use libswiftCore as an import module
			// let's ignore it here
			if (package == "libswiftCore" || package == "U0" || package == "T0") {
				return;
			}

			if (package != OwningModule && !this.Exists (imp => imp.Contents == target.Contents))
				Add (target);
		}

		public string OwningModule { get; set; }
	}

}

