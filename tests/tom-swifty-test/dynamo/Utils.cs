// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Dynamo;
using Dynamo.CSLang;
using tomwiftytest;

namespace dynamotests {
	public class Utils {
		public Utils ()
		{
		}

		public delegate CSClass ClassMutator (CSClass cl);
		public delegate CSUsingPackages UsingMutator (CSUsingPackages pkg);

		public static Stream BasicClass (string nameSpace, string className, CSMethod m, ClassMutator mutator, UsingMutator useMutator = null)
		{
			CSUsingPackages use = new CSUsingPackages ("System");
			if (useMutator != null)
				use = useMutator (use);

			CSClass cl = new CSClass (CSVisibility.Public, className, m != null ? new CSMethod [] { m } : null);
			if (mutator != null)
				cl = mutator (cl);

			CSNamespace ns = new CSNamespace (nameSpace);
			ns.Block.Add (cl);

			CSFile file = CSFile.Create (use, ns);
			return CodeWriter.WriteToStream (file);
		}

		public static void CompileAStream (Stream stm)
		{
			using (DisposableTempFile tf = new DisposableTempFile (null, null, "cs", true)) {
				stm.CopyTo (tf.Stream);
				tf.Stream.Flush ();
				Compiler.CompileUsing (null, XCodeCompiler.CSharp, tf.Filename, "");
			}
		}
	}
}

