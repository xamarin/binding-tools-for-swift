// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using static SwiftInterfaceParser;

namespace SwiftReflector.SwiftInterfaceReflector {
	public class SyntaxDesugaringParser : SwiftInterfaceBaseListener {
		TokenStreamRewriter rewriter;
		SwiftInterfaceParser parser;
		ICharStream charStream;
		SwiftInterfaceLexer lexer;

		public SyntaxDesugaringParser (string inFile)
		{
			charStream = CharStreams.fromPath (inFile);
			lexer = new SwiftInterfaceLexer (charStream);
			var tokenStream = new CommonTokenStream (lexer);

			rewriter = new TokenStreamRewriter (tokenStream);
			this.parser = new SwiftInterfaceParser (tokenStream);
		}

		public string Desugar ()
		{
			var walker = new ParseTreeWalker ();
			walker.Walk (this, parser.swiftinterface ());
			return rewriter.GetText ();
		}

		public override void ExitOptional_type ([NotNull] Optional_typeContext context)
		{
			var innerType = context.type ().GetText ();
			var replacementType = $"Swift.Optional<{innerType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}

		public override void ExitArray_type ([NotNull] Array_typeContext context)
		{
			var innerType = context.type ().GetText ();
			var replacementType = $"Swift.Array<{innerType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}

		public override void ExitDictionary_type ([NotNull] Dictionary_typeContext context)
		{
			var keyType = context.children [1].GetText ();
			var valueType = context.children [3].GetText ();
			var replacementType = $"Swift.Dictionary<{keyType},{valueType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}
	}
}

