// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Dynamo;
using SwiftReflector.Inventory;
using tomwiftytest;
using SwiftReflector.IOUtils;
using Dynamo.CSLang;
using SwiftReflector.TypeMapping;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class DictionaryTests {


		public void TLDictionarySimple (string sKeyType, string sValType,
									  string csKeyType, string csValType, string output)
		{
			string variant = sKeyType + sValType;
			string swiftCode = String.Format (
		"public func makeDictTLDS{2}()  -> [{0}:{1}]\n {{\n return [{0}:{1}]() \n}}\n",
		sKeyType, sValType, variant);
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftDictionary", false,
																	 new CSSimpleType (csKeyType),
																	 new CSSimpleType (csValType)),
													new CSIdentifier ("dict"),
						    new CSFunctionCall ($"TopLevelEntities.MakeDictTLDS{variant}", false));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("dict.Count"));
			callingCode.Add (decl);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName : $"TLDictionarySimple{variant}");
		}

		[Test]
		public void TestTLDictBoolBool ()
		{
			TLDictionarySimple ("Bool", "Bool", "bool", "bool", "0");
		}


		[Test]
		public void TestTLDictBoolInt ()
		{
			TLDictionarySimple ("Bool", "Int", "bool", "nint", "0");
		}


		[Test]
		public void TestTLDictBoolFloat ()
		{
			TLDictionarySimple ("Bool", "Float", "bool", "float", "0");
		}



		[Test]
		public void TestTLDictBoolString ()
		{
			TLDictionarySimple ("Bool", "String", "bool", "SwiftString", "0");
		}



		public void TLDictionaryAdd (string sKeyType, string sValType,
							  string csKeyType, string csValType, string csKey, string csValue, string output)
		{
			string swiftCode =
			    $"public func makeDictTLDA{sKeyType}{sValType}()  -> [{sKeyType}:{sValType}]\n {{\n return [{sKeyType}:{sValType}]() \n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftDictionary", false,
																	 new CSSimpleType (csKeyType),
																	 new CSSimpleType (csValType)),
													new CSIdentifier ("dict"),
						    new CSFunctionCall ($"TopLevelEntities.MakeDictTLDA{sKeyType}{sValType}", false));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("dict.Count"));
			CSLine addcall = CSFunctionCall.FunctionCallLine ("dict.Add", false, new CSIdentifier (csKey),
														 new CSIdentifier (csValue));
			CSLine writeCall = CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val (' '));
			callingCode.Add (decl);
			callingCode.Add (call);
			callingCode.Add (addcall);
			callingCode.Add (writeCall);
			callingCode.Add (call);

			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLDictionaryAdd{sKeyType}{sValType}");
		}


		[Test]
		public void TestTLDictAddBoolBool ()
		{
			TLDictionaryAdd ("Bool", "Bool", "bool", "bool", "true", "false", "0 1");
		}

		[Test]
		public void TestTLDictAddBoolInt ()
		{
			TLDictionaryAdd ("Bool", "Int", "bool", "nint", "true", "17", "0 1");
		}

		[Test]
		public void TestTLDictAddBoolFloat ()
		{
			TLDictionaryAdd ("Bool", "Float", "bool", "float", "true", "17.1f", "0 1");
		}


		[Test]
		public void TestTLDictAddBoolString ()
		{
			TLDictionaryAdd ("Bool", "String", "bool", "SwiftString", "true", "SwiftString.FromString(\"hi mom\")", "0 1");
		}



		public void TLDictionaryAddGet (string sKeyType, string sValType,
							  string csKeyType, string csValType, string csKey, string csValue, string output)
		{
			string swiftCode =
			    $"public func makeDictTLDAG{sKeyType}{sValType}()  -> [{sKeyType}:{sValType}]\n {{\n return [{sKeyType}:{sValType}]() \n}}\n";
			CodeElementCollection<ICodeElement> callingCode = new CodeElementCollection<ICodeElement> ();
			CSLine decl = CSVariableDeclaration.VarLine (new CSSimpleType ("SwiftDictionary", false,
																	 new CSSimpleType (csKeyType),
																	 new CSSimpleType (csValType)),
													new CSIdentifier ("dict"),
						    new CSFunctionCall ($"TopLevelEntities.MakeDictTLDAG{sKeyType}{sValType}", false));
			CSLine call = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("dict.Count"));
			CSLine addcall = CSFunctionCall.FunctionCallLine ("dict.Add", false, new CSIdentifier (csKey),
														 new CSIdentifier (csValue));
			CSLine writeCall = CSFunctionCall.FunctionCallLine ("Console.Write", false, CSConstant.Val (' '));
			CSLine getLine = CSVariableDeclaration.VarLine (new CSSimpleType (csValType), "val",
													   new CSArray1D ("dict", new CSIdentifier (csKey)));
			CSLine valueWrite = CSFunctionCall.FunctionCallLine ("Console.Write", false, new CSIdentifier ("val"));
			callingCode.Add (decl);
			callingCode.Add (call);
			callingCode.Add (addcall);
			callingCode.Add (writeCall);
			callingCode.Add (call);
			callingCode.Add (writeCall);
			callingCode.Add (getLine);
			callingCode.Add (valueWrite);
			TestRunning.TestAndExecute (swiftCode, callingCode, output, testName: $"TLDictionaryAddGet{sKeyType}{sValType}");
		}

		[Test]
		public void TestTLDictGetBoolBool ()
		{
			TLDictionaryAddGet ("Bool", "Bool", "bool", "bool", "true", "false", "0 1 False");
		}

		[Test]
		public void TestTLDictGetBoolInt ()
		{
			TLDictionaryAddGet ("Bool", "Int", "bool", "nint", "true", "17", "0 1 17");
		}

		[Test]
		public void TestTLDictGetBoolFloat ()
		{
			TLDictionaryAddGet ("Bool", "Float", "bool", "float", "true", "17.1f", "0 1 17.1");
		}



		[Test]
		public void TestTLDictGetBoolString ()
		{
			TLDictionaryAddGet ("Bool", "String", "bool", "SwiftString", "true", "SwiftString.FromString(\"hi mom\")", "0 1 hi mom");
		}

	}
}
