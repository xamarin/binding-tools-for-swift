using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.CSLang;
using SwiftReflector.Demangling;
using SwiftReflector.Inventory;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace SwiftReflector {
	public class Homonyms {
		public static bool IsHomonym(TLFunction func, OverloadInventory inventory)
		{
			foreach (TLFunction f in inventory.Functions) {
				if (IsHomonym (func, f))
					return true;
			}
			return false;
		}

		public static bool IsHomonym (FunctionDeclaration func, IEnumerable<FunctionDeclaration> funcs)
		{
			foreach (var f in funcs) {
				if (IsHomonym (func, f))
					return true;
			}
			return false;
		}

		static bool IsHomonym(TLFunction func1, TLFunction func2)
		{
			// Two functions are homonyms if and only if
			// 1. They are not the same function
			// 2. The same function name
			// 3. Matching argument types
			// 4. Either different argument names or different argument types

			// 1. not the same function
			if (func1.MangledName == func2.MangledName)
				return false;
			// 2. same name
			if (!func1.Signature.Name.Equals (func2.Signature.Name))
				return false;
			// 3. same argument types
			if (!ArgumentTypesMatch (func1, func2))
				return false;

			return !ArgumentNamesMatch (func1, func2) || !ReturnTypesMatch (func1, func2);
		}

		static bool IsHomonym(FunctionDeclaration func1, FunctionDeclaration func2)
		{
			if (func1 == func2)
				return false;
			if (func1.Name != func2.Name)
				return false;
			if (!ArgumentTypesMatch (func1, func2))
				return false;
			return !ArgumentNamesMatch (func1, func2) || !ReturnTypesMatch (func1, func2);
		}


		static bool ArgumentTypesMatch(TLFunction func1, TLFunction func2)
		{
			if (func1.Signature.ParameterCount != func2.Signature.ParameterCount)
				return false;
			for (int i = 0; i < func1.Signature.ParameterCount; i++) {
				var funcarg1 = func1.Signature.GetParameter (i);
				var funcarg2 = func2.Signature.GetParameter (i);
				if (!funcarg1.Equals (funcarg2))
					return false;
			}
			return true;
		}

		static bool ArgumentTypesMatch(FunctionDeclaration func1, FunctionDeclaration func2)
		{
			// there will be no more that 2 parameter lists.
			// the last is the actual parameters
			// the 0th if there are 2 is the instance (i.e. self)
			var list1 = func1.ParameterLists.Last ();
			var list2 = func2.ParameterLists.Last ();
			if (list1.Count != list2.Count)
				return false;
			for (int i = 0; i < list1.Count; i++) {
				if (!list1 [i].TypeSpec.Equals (list2 [i].TypeSpec))
					return false;
			}
			return true;
		}

		static bool ArgumentNamesMatch(TLFunction func1, TLFunction func2)
		{
			if (func1.Signature.ParameterCount != func2.Signature.ParameterCount)
				return false;
			for (int i = 0; i < func1.Signature.ParameterCount; i++) {
				var funcarg1 = func1.Signature.GetParameter (i);
				var funcarg2 = func2.Signature.GetParameter (i);
				if (!funcarg1.Name.Equals (funcarg2.Name))
					return false;
			}
			return true;
		}

		static bool ArgumentNamesMatch(FunctionDeclaration func1, FunctionDeclaration func2)
		{
			// there will be no more that 2 parameter lists.
			// the last is the actual parameters
			// the 0th if there are 2 is the instance (i.e. self)
			var list1 = func1.ParameterLists.Last ();
			var list2 = func2.ParameterLists.Last ();
			if (list1.Count != list2.Count)
				return false;
			for (int i = 0; i < list1.Count; i++) {
				if (list1 [i].PublicName != list2 [i].PublicName)
					return false;

			}
			return true;
		}

		static bool ReturnTypesMatch(TLFunction func1, TLFunction func2)
		{
			return func1.Signature.ReturnType.Equals (func2.Signature.ReturnType);
		}

		static bool ReturnTypesMatch(FunctionDeclaration func1, FunctionDeclaration func2)
		{
			var ret1 = func1.ReturnTypeSpec ?? TupleTypeSpec.Empty;
			var ret2 = func2.ReturnTypeSpec ?? TupleTypeSpec.Empty;
			return ret1.Equals (ret2);
		}

		static List<List<string>> HomonymPartsFor(TLFunction func, OverloadInventory inventory, TypeMapper mapper)
		{
			return inventory.Functions.Where (f => IsHomonym (func, f)).Select (f => HomonymPartsFor (f, mapper)).ToList ();
		}

		static List<List<string>> HomonymPartsFor(FunctionDeclaration func, IEnumerable<FunctionDeclaration> funcs, TypeMapper mapper)
		{
			var result = new List<List<string>> ();
			foreach (var f in funcs) {
				if (IsHomonym(func, f)) {
					List<string> parts = HomonymPartsFor (f, mapper);
					result.Add (parts);
				}
					
			}
			return result;
		}

		static List<string> HomonymPartsFor(TLFunction func, TypeMapper mapper)
		{
			var parts = func.Signature.EachParameter.Select (arg => arg.Name.Name).ToList ();

			if (func.Signature.ReturnType == null || func.Signature.ReturnType.IsEmptyTuple) {
				parts.Add ("void");
			} else {
				var use = new CSUsingPackages ();
				var ntb = mapper.MapType (func.Signature.ReturnType, false);
				var type = NetTypeBundle.ToCSSimpleType (ntb, use);
				var returnTypeName = type.ToString ().Replace (".", "").Replace ("<", "").Replace (">", "");
				parts.Add (returnTypeName);
			}
			return parts;
		}

		static List<string> HomonymPartsFor(FunctionDeclaration func, TypeMapper mapper)
		{
			var parts = func.ParameterLists.Last ().Select (arg => arg.NameIsRequired ? arg.PublicName : "_").ToList ();
			if (func.ReturnTypeSpec == null || func.ReturnTypeSpec.IsEmptyTuple) {
				parts.Add ("void");
			} else {
				var use = new CSUsingPackages ();
				if (func.ReturnTypeSpec is ProtocolListTypeSpec pl) {
					var sb = new StringBuilder ();
					foreach (var nt in pl.Protocols.Keys) {
						sb.Append (MangledReturnType (func, mapper, use, nt));
					}
					parts.Add (sb.ToString ());
				} else {
					parts.Add (MangledReturnType (func, mapper, use, func.ReturnTypeSpec));
				}
			}
			return parts;
		}

		static string MangledReturnType (FunctionDeclaration func, TypeMapper mapper, CSUsingPackages use, TypeSpec ts)
		{
			var ntb = mapper.MapType (func, ts, false, true);
			var type = NetTypeBundle.ToCSSimpleType (ntb, use);
			var returnTypeName = type.ToString ().Replace (".", "").Replace ("<", "").Replace (">", "");
			return returnTypeName;
		}

		public static string HomonymSuffix(TLFunction func, OverloadInventory inventory, TypeMapper mapper)
		{
			return HomonymSuffix (HomonymPartsFor (func, mapper), HomonymPartsFor (func, inventory, mapper));
		}

		public static string HomonymSuffix(FunctionDeclaration func, IEnumerable<FunctionDeclaration> funcs, TypeMapper mapper)
		{
			return HomonymSuffix (HomonymPartsFor (func, mapper), HomonymPartsFor (func, funcs, mapper));
		}

		static bool ReturnValueUniform(List<string> master, List<List<string>> homonyms)
		{
			var last = master.Count - 1;
			foreach (var homonym in homonyms) {
				if (master [last] != homonym [last])
					return false;
			}
			return true;
		}

		static string HomonymSuffix(List<String> master, List<List<String>> homonyms)
		{
			// right now, we use all the argument names and possibly the return type.
			// If need be at some point in future this can be modified to be the minimum
			// set of pieces to guarantee that master is uniquely named, but this is
			// probably going to be less astonshing to our customers.

			if (homonyms.Count == 0)
				return "";
			int endIndex = ReturnValueUniform (master, homonyms) ? master.Count - 1 : master.Count;
			var builder = new StringBuilder ();
			for (int i = 0; i < endIndex; i++) {
				builder.Append ("_").Append (master [i]);
			}
			return builder.ToString ();
		}

	}
}
