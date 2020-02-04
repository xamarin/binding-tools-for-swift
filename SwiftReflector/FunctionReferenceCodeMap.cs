// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;

namespace SwiftReflector {
	public class FunctionReferenceCodeMap {
		Dictionary<FunctionDeclaration, int> map = new Dictionary<FunctionDeclaration, int> ();
		int currentCode = 0;
		Dictionary<string, BaseDeclaration> allClassesAdded = new Dictionary<string, BaseDeclaration> ();

		public int? ReferenceCodeFor (FunctionDeclaration declaration)
		{
			if (declaration == null)
				throw new ArgumentNullException (nameof (declaration));
			declaration = declaration.OverrideSurrogateFunction ?? declaration;

			int result = 0;
			if (map.TryGetValue (declaration, out result)) {
				return result;
			}
			return null;
		}

		public int GenerateReferenceCode(FunctionDeclaration declaration)
		{
			if (declaration == null)
				throw new ArgumentNullException (nameof (declaration));

			if (map.ContainsKey (declaration))
				throw new ArgumentOutOfRangeException (nameof (declaration), $"attempt to generate two reference codes for function {declaration.ToFullyQualifiedName (true)}");

			var result = currentCode;
			currentCode++;
			map.Add (declaration, result);
			var parentName = declaration.Parent != null ? declaration.Parent.ToFullyQualifiedName (false) : null;
			if (declaration.Parent != null && !allClassesAdded.ContainsKey (parentName))
				allClassesAdded.Add (parentName, declaration.Parent);
			return result;
		}

		public BaseDeclaration OriginalOrReflectedClassFor (BaseDeclaration baseDecl)
		{
			BaseDeclaration result = null;
			allClassesAdded.TryGetValue (baseDecl.ToFullyQualifiedName (false), out result);
			return result ?? baseDecl;
		}

	}
}
