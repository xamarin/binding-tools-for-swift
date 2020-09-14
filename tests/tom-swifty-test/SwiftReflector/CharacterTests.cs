// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using tomwiftytest;
using SwiftReflector.Inventory;
using SwiftReflector.IOUtils;
using Dynamo.CSLang;
using System.Text;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class CharacterTests {
		string [] TestCases = { "a", "啊", "㐀", "𪛖", "᠀", "ༀ", "ꀀ", "ە", "ᥐ", "ᄓ", "🎉" };

		[Test]
		public void TestCharacterConstructors ()
		{
			var swiftCode = @"public func Echo (c: Character) -> Character { return c; }";

			var callingCode = new CodeElementCollection<ICodeElement> ();
			StringBuilder expected = new StringBuilder ();

			foreach (string c in TestCases)
			{
				CSIdentifier testIdentifier = (CSIdentifier)$@"""{c}""";

				var ctorCall = CSFunctionCall.Ctor ("SwiftCharacter", testIdentifier);
				var fromCall = CSFunctionCall.Function ("SwiftCharacter.FromCharacter", testIdentifier);
				var implicitCall = new CSCastExpression ((CSSimpleType)"SwiftCharacter", testIdentifier);

				foreach (var call in new CSBaseExpression [] { ctorCall, fromCall, implicitCall })
				{
					CSLine print = CSFunctionCall.FunctionLine ("Console.Write", CSFunctionCall.Function ("TopLevelEntities.Echo", call));
					callingCode.Add (print);
					expected.Append (c);
				}
			}

			TestRunning.TestAndExecute (swiftCode, callingCode, expected.ToString ());
		}

		[Test]
		public void TestCharacterReturns ()
		{
			var swiftCode = @"
public class CharacterEcho
{
    var C : Character;
    public init (c : Character) { C = c }
    public func GetValue () -> Character { return C }
    public var Value : Character { get { return C; } }
}";
			var callingCode = new CodeElementCollection<ICodeElement> ();
			StringBuilder expected = new StringBuilder ();

			foreach (string c in TestCases) 
			{
				CSIdentifier testIdentifier = (CSIdentifier)$@"""{c}""";

				CSCodeBlock block = new CSCodeBlock ();
				var ctorParam = new CSCastExpression ((CSSimpleType)"SwiftCharacter", testIdentifier);
				CSLine instance = CSVariableDeclaration.VarLine ((CSSimpleType)"CharacterEcho", (CSIdentifier)"Foo", CSFunctionCall.Ctor ("CharacterEcho", ctorParam));

				// First the properties
				var explicitCastProp = (CSInject)"(string)Foo.Value";
				var toStringProp = (CSIdentifier)"Foo.Value.ToString ()";

				// Then the function returns as well
				var explicitCastFun = (CSInject)"(string)Foo.GetValue ()";
				var toStringFun = (CSInject)"Foo.GetValue ().ToString ()";

				block.Add (instance);
				foreach (var call in new CSBaseExpression [] { explicitCastProp, toStringProp, explicitCastFun, toStringFun }) 
				{
					CSLine print = CSFunctionCall.FunctionLine ("Console.Write", call);
					block.Add (print);
					expected.Append (c);
				}

				callingCode.Add (block);
			}

			TestRunning.TestAndExecute (swiftCode, callingCode, expected.ToString ());
		}

		[Test]
		public void TestCharacterCreation ()
		{
			var swiftCode = @"
public class CharacterHolder
{
    public var C: Character;
    public init (c: Character) {C = c }
}";
			var callingCode = new CodeElementCollection<ICodeElement> ();
			StringBuilder expected = new StringBuilder ();

			foreach (string c in TestCases)
			{
				CSIdentifier testIdentifier = (CSIdentifier)$@"""{c}""";

				Action<ICSExpression> testCharacter = creation => {
					CSCodeBlock block = new CSCodeBlock ();
					block.Add (CSVariableDeclaration.VarLine ((CSSimpleType)"SwiftCharacter", (CSIdentifier)"Char", creation));

					block.Add (CSVariableDeclaration.VarLine ((CSSimpleType)"CharacterHolder", (CSIdentifier)"Foo", CSFunctionCall.Ctor ("CharacterHolder", (CSIdentifier)"Char")));
					block.Add (CSFunctionCall.FunctionLine ("Console.Write", (CSIdentifier)"(string)Foo.C"));

					expected.Append (c);

					callingCode.Add (block);
				};

				testCharacter (CSFunctionCall.Function ("SwiftCharacter.FromCharacter", (testIdentifier)));
				testCharacter (new CSCastExpression ((CSSimpleType)"SwiftCharacter", testIdentifier));	
			}

			TestRunning.TestAndExecute (swiftCode, callingCode, expected.ToString ());
		}
	}
}
