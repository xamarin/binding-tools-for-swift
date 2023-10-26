// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using static SwiftInterfaceParser;
using System.Collections.Generic;

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

		internal static string TypeText (ICharStream input, ParserRuleContext ty)
		{
			if (ty is null)
				return null;
			var start = ty.Start.StartIndex;
			var end = ty.Stop.StopIndex;
			var interval = new Interval (start, end);
			return input.GetText (interval);
		}

		string TypeText (ParserRuleContext ty)
		{
			return TypeText (charStream, ty);
		}

		public override void ExitOptional_type ([NotNull] Optional_typeContext context)
		{
			var innerType = TypeText (context.type ());
			var replacementType = $"Swift.Optional<{innerType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}

		public override void ExitUnwrapped_optional_type ([NotNull] Unwrapped_optional_typeContext context)
		{
			var innerType = TypeText (context.type ());
			var replacementType = $"Swift.Optional<{innerType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}

		public override void ExitArray_type ([NotNull] Array_typeContext context)
		{
			var innerType = TypeText (context.type ());
			var replacementType = $"Swift.Array<{innerType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}

		public override void ExitDictionary_type ([NotNull] Dictionary_typeContext context)
		{
			var keyType = TypeText (context.children [1] as ParserRuleContext);
			var valueType = TypeText (context.children [3] as ParserRuleContext);
			var replacementType = $"Swift.Dictionary<{keyType},{valueType}>";
			var startToken = context.Start;
			var endToken = context.Stop;
			rewriter.Replace (startToken, endToken, replacementType);
		}

		static Dictionary<string, string> typeChanges = new Dictionary<string, string> () {
			{ "Swift.Void", "()" },
			{ "CoreFoundation.CGAffineTransform", "CoreGraphics.CGAffineTransform" },
			{ "CoreFoundation.CGColorSapceModel", "CoreGraphics.CGColorSapceModel" },
			{ "CoreFoundation.CGPoint", "CoreGraphics.CGPoint" },
			{ "CoreFoundation.CGRect", "CoreGraphics.CGRect" },
			{ "CoreFoundation.CGSize", "CoreGraphics.CGSize" },
			{ "CoreFoundation.CGVector", "CoreGraphics.CGVector" },
		};

		public override void ExitIdentifier_type ([NotNull] Identifier_typeContext context)
		{
			if (typeChanges.TryGetValue (context.GetText (), out var substitution)) {
				var startToken = context.Start;
				var endToken = context.Stop;
				rewriter.Replace (startToken, endToken, substitution);
			}
		}
	}
}

