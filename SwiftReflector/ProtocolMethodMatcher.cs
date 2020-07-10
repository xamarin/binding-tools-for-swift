using System;
using System.Linq;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;

namespace SwiftReflector {
	public class ProtocolMethodMatcher {
		ProtocolDeclaration protocol;
		List<FunctionDeclaration> originalFuncs;
		WrappingResult wrapper;
		ClassDeclaration wrapperClass;

		public ProtocolMethodMatcher (ProtocolDeclaration protocol, List<FunctionDeclaration> originalFuncs, WrappingResult wrapper)
		{
			this.protocol = Ex.ThrowOnNull (protocol, nameof (protocol));
			this.originalFuncs = Ex.ThrowOnNull (originalFuncs, nameof (originalFuncs));
			this.wrapper = Ex.ThrowOnNull (wrapper, nameof (wrapper));
		}

		public void MatchFunctions (List<FunctionDeclaration> result)
		{
			wrapperClass = FindWrapperClass ();
			foreach (var func in originalFuncs) {
				var matchFunc = MatchFunction (func);
				if (matchFunc == null)
					throw new NotSupportedException ("This should never fail - unable to find matching function");
				result.Add (matchFunc);
			}
		}

		public ClassDeclaration WrapperClass => wrapperClass;

		FunctionDeclaration MatchFunction (FunctionDeclaration decl)
		{
			var classFuncs = wrapperClass.AllMethodsNoCDTor ();
			var argCount = decl.ParameterLists.Last ().Count;
			var nameAndArgCountMatch = classFuncs.Where (fn => fn.Name == decl.Name && fn.ParameterLists.Last ().Count == argCount).ToList ();
			if (nameAndArgCountMatch.Count == 0)
				return null;
			else if (nameAndArgCountMatch.Count == 1)
				return nameAndArgCountMatch [0];
			return DeepMatch (decl, nameAndArgCountMatch);
		}

		FunctionDeclaration DeepMatch (FunctionDeclaration decl, List<FunctionDeclaration> candidates)
		{
			foreach (var candidate in candidates) {
				var argsMatch = ArgsMatch (decl, decl.ParameterLists.Last (), candidate, candidate.ParameterLists.Last ());
				var typesMatch = TypesMatch (decl, decl.ReturnTypeSpec, candidate, candidate.ReturnTypeSpec);
				if (argsMatch && typesMatch)
					return candidate;
			}
			return null;
		}

		bool ArgsMatch (FunctionDeclaration protoFunc, List<ParameterItem> protoList, FunctionDeclaration classFunc, List<ParameterItem> classList)
		{
			for (int i = 0; i < protoList.Count; i++) {
				if (protoList [i].IsInOut != classList [i].IsInOut)
					return false;
				if (!TypesMatch (protoFunc, protoList [i].TypeSpec, classFunc, classList [i].TypeSpec))
					return false;
			}
			return true;
		}

		bool TypesMatch (FunctionDeclaration protoFunc, TypeSpec protoType, FunctionDeclaration classFunc, TypeSpec classType)
		{
			if (protoType.Kind != classType.Kind)
				return false;
			if (protoType.Equals (classType))
				return true;

			switch (protoType.Kind) {
			case TypeSpecKind.Closure:
				return ClosureTypesMatch (protoFunc, protoType as ClosureTypeSpec, classFunc, classType as ClosureTypeSpec);
			case TypeSpecKind.Named:
				return NamedTypesMatch (protoFunc, protoType as NamedTypeSpec, classFunc, classType as NamedTypeSpec);
			case TypeSpecKind.ProtocolList:
				return ProtoListTypesMatch (protoFunc, protoType as ProtocolListTypeSpec, classFunc, classType as ProtocolListTypeSpec);
			case TypeSpecKind.Tuple:
				return TupleTypesMatch (protoFunc, protoType as TupleTypeSpec, classFunc, classType as TupleTypeSpec);
			default:
				throw new NotImplementedException ($"Unknown TypeSpec kind {protoType.Kind}");
			}
		}

		bool NamedTypesMatch (FunctionDeclaration protoFunc, NamedTypeSpec protoType, FunctionDeclaration classFunc, NamedTypeSpec classType)
		{
			if (!GenericParametersMatch (protoFunc, protoType.GenericParameters, classFunc, classType.GenericParameters))
				return false;

			var assoc = protocol.AssociatedTypeDeclarationFromNamedTypeSpec (protoType);
			if (assoc != null) {
				if (!wrapperClass.IsTypeSpecGenericReference (classType))
					return false;
				var depthIndex = wrapperClass.GetGenericDepthAndIndex (classType);
				var assocIndex = protocol.AssociatedTypes.IndexOf (assoc);
				return assocIndex == depthIndex.Item2;
			} else {
				return protoType.Name == classType.Name;
			}
		}

		bool ClosureTypesMatch (FunctionDeclaration protoFunc, ClosureTypeSpec protoType, FunctionDeclaration classFunc, ClosureTypeSpec classType)
		{
			var argsMatch = TypesMatch (protoFunc, protoType.Arguments, classFunc, classType.Arguments);
			var retsMatch = TypesMatch (protoFunc, protoType.ReturnType, classFunc, classType.ReturnType);
			return argsMatch && retsMatch;
		}

		bool GenericParametersMatch (FunctionDeclaration protoFunc, List<TypeSpec> protoGens, FunctionDeclaration classFunc, List<TypeSpec> classGens)
		{
			if (protoGens.Count != classGens.Count)
				return false;
			for (int i = 0; i < protoGens.Count; i++) {
				if (!TypesMatch (protoFunc, protoGens [i], classFunc, classGens [i]))
					return false;
			}
			return true;
		}

		bool ProtoListTypesMatch (FunctionDeclaration protoFunc, ProtocolListTypeSpec protoType, FunctionDeclaration classFunc, ProtocolListTypeSpec classType)
		{
			if (protoType.Protocols.Count != classType.Protocols.Count)
				return false;
			for (int i = 0; i < protoType.Protocols.Count; i++) {
				if (!NamedTypesMatch (protoFunc, protoType.Protocols.Keys [i], classFunc, classType.Protocols.Keys [i]))
					return false;
			}
			return true;
		}

		bool TupleTypesMatch (FunctionDeclaration protoFunc, TupleTypeSpec protoType, FunctionDeclaration classFunc, TupleTypeSpec classType)
		{
			if (protoType.Elements.Count != classType.Elements.Count)
				return false;
			for (int i = 0; i < protoType.Elements.Count; i++) {
				if (!TypesMatch (protoFunc, protoType.Elements [i], classFunc, classType.Elements [i]))
					return false;
			}
			return true;
		}

		ClassDeclaration FindWrapperClass ()
		{
			return FindWrapperClass (wrapper, protocol);
		}

		public static ClassDeclaration FindWrapperClass (WrappingResult wrapper, ProtocolDeclaration protocol)
		{
			var className = protocol.IsExistential ? OverrideBuilder.ProxyClassName (protocol) : OverrideBuilder.AssociatedTypeProxyClassName (protocol);
			var theClass = wrapper.Module.Classes.FirstOrDefault (cl => cl.Name == className);
			return wrapper.FunctionReferenceCodeMap.OriginalOrReflectedClassFor (theClass) as ClassDeclaration;
		}
	}
}
