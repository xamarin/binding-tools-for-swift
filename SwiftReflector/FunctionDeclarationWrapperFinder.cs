using System;
using System.Collections.Generic;
using System.Linq;
using Dynamo;
using ObjCRuntime;
using SwiftReflector.Demangling;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;

namespace SwiftReflector {
	public class FunctionDeclarationWrapperFinder {
		TypeMapper typeMapper;
		WrappingResult wrappingResult;

		public FunctionDeclarationWrapperFinder (TypeMapper mapper, WrappingResult wrappingResult)
		{
			typeMapper = Exceptions.ThrowOnNull (mapper, nameof (mapper));
			this.wrappingResult = wrappingResult;
		}

		public FunctionDeclaration FindWrapper (FunctionDeclaration original)
		{
			var name = original.IsProperty ? original.PropertyName : original.Name;

			var parentOrModuleFull = original.Parent != null ?
				original.Parent.ToFullyQualifiedName (true) : original.Module.Name;
			var parentOrModuleBrief = original.Parent != null ?
				original.Parent.ToFullyQualifiedName (false) : original.Module.Name;
			var wrapperName = original.IsOperator ?
				MethodWrapping.WrapperOperatorName (typeMapper, parentOrModuleFull, name, original.OperatorType) :
				MethodWrapping.WrapperName (parentOrModuleBrief, name, original.IsExtension, original.IsStatic);

			return FindWrapperForMethod (original, wrapperName);
		}

		public FunctionDeclaration FindWrapperForMethod (BaseDeclaration parent, FunctionDeclaration funcToWrap, PropertyType propType)
		{
			if (!funcToWrap.IsProperty)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 34, $"Expected a property for method signature, but got {funcToWrap}");
			var wrapperName = MethodWrapping.WrapperName (parent.ToFullyQualifiedName (), funcToWrap.PropertyName, propType, funcToWrap.IsSubscript, funcToWrap.IsExtension, funcToWrap.IsStatic);
			return FindWrapperForMethod (funcToWrap, wrapperName);
		}

		public FunctionDeclaration FindWrapperForTopLevelFunction (FunctionDeclaration original)
		{
			var name = original.IsProperty ? original.PropertyName : original.Name;

			var parentOrModuleBrief = original.Parent != null ?
				original.Parent.ToFullyQualifiedName (false) : original.Module.Name;

			var wrapperName = original.IsOperator ?
				MethodWrapping.WrapperOperatorName (typeMapper, original.Module.Name, name, original.OperatorType) :
				MethodWrapping.WrapperFuncName (original.Module.Name, name);

			return FindWrapperForMethod (original, wrapperName);
		}

		public FunctionDeclaration FindWrapperForExtension (FunctionDeclaration funcDeclToWrap, BaseDeclaration extensionOn)
		{
			string wrapperName = null;
			if (funcDeclToWrap.IsProperty) {
				wrapperName = MethodWrapping.WrapperName (extensionOn.ToFullyQualifiedName (true), funcDeclToWrap.PropertyName,
									  (funcDeclToWrap.IsGetter ? PropertyType.Getter :
									   (funcDeclToWrap.IsSetter ? PropertyType.Setter : PropertyType.Materializer)), false, funcDeclToWrap.IsExtension,
									  funcDeclToWrap.IsStatic);
			} else {
				wrapperName = MethodWrapping.WrapperName (extensionOn.ToFullyQualifiedName (false), funcDeclToWrap.Name, true, funcDeclToWrap.IsStatic);
			}
			return FindWrapperForMethod (funcDeclToWrap, wrapperName);

		}

		FunctionDeclaration FindWrapperForMethod (FunctionDeclaration funcDecl, string wrapperName)
		{
			var referenceCodedWrapper = LookupReferenceCodeForFunctionDeclaration (funcDecl, wrapperName, "method");
			if (referenceCodedWrapper != null)
				return referenceCodedWrapper;

			var allWrappers = wrappingResult.Module.Functions.Where (fn => fn.Name == wrapperName).ToList ();
			if (allWrappers == null || allWrappers.Count == 0)
				return null;

			// if this is not a static function, then there is an extra argument for the instance
			var instanceSkip =  funcDecl.IsStatic || funcDecl.Parent == null ? 0 : 1;

			var hasReturn = !TypeSpec.IsNullOrEmptyTuple (funcDecl.ReturnTypeSpec);
			var returnOrExceptionSkip = (hasReturn && typeMapper.MustForcePassByReference (funcDecl, funcDecl.ReturnTypeSpec)) ||
				funcDecl.HasThrows ? 1 : 0;

			var argumentsToSkip = instanceSkip + returnOrExceptionSkip;

			return allWrappers.FirstOrDefault (fn => ParametersMatchExceptSkippingFirstN (fn, funcDecl, argumentsToSkip) &&
							   ReturnTypesMatch (fn, funcDecl));
		}

		public FunctionDeclaration FindWrapperForConstructor (BaseDeclaration parent, FunctionDeclaration funcToWrap)
		{
			string wrapperName = MethodWrapping.WrapperCtorName (parent.ToFullyQualifiedName (false), parent.Name, funcToWrap.IsExtension);
			return FindWrapperForConstructor (funcToWrap, wrapperName);
		}

		FunctionDeclaration FindWrapperForConstructor (FunctionDeclaration funcDecl, string wrapperName)
		{
			var referenceCodedWrapper = LookupReferenceCodeForFunctionDeclaration (funcDecl, wrapperName, "constructor");
			if (referenceCodedWrapper != null)
				return referenceCodedWrapper;

			var allWrappers = wrappingResult.Module.Functions.Where (fn => fn.Name == wrapperName).ToList ();
			if (allWrappers == null || allWrappers.Count == 0)
				return null;

			var returnType = funcDecl.ReturnTypeSpec;
			var entity = typeMapper.GetEntityForTypeSpec (returnType);
			var isClass = entity.EntityType == EntityType.Class;
			int skipCount = isClass ? 0 : 1;

			return allWrappers.FirstOrDefault (fn => ParametersMatchExceptSkippingFirstN (fn, funcDecl, skipCount));
		}

		bool ParametersMatchExceptSkippingFirstN (FunctionDeclaration wrapper, FunctionDeclaration toWrap, int n)
		{
			var matchNames = !toWrap.IsProperty;
			var wrapArgs = wrapper.ParameterLists.Last ().Skip (n).ToList ();
			var toWrapArgs = toWrap.ParameterLists.Last ();

			return ParametersMatch (wrapper, wrapArgs, toWrap, toWrapArgs, matchNames);
		}

		bool ParametersMatch (FunctionDeclaration wrapper, List<ParameterItem> wrapArgs,
			FunctionDeclaration toWrap, List<ParameterItem> toWrapArgs, bool matchNames)
		{
			if (wrapArgs.Count != toWrapArgs.Count)
				return false;
			for (int i = 0; i < wrapArgs.Count; i++) {
				var wrapArg = wrapArgs [i];
				var toWrapArg = toWrapArgs [i];
				if (wrapArg.TypeSpec is ClosureTypeSpec wrapClosure && toWrapArg.TypeSpec is ClosureTypeSpec toWrapClosure) {
					return WrappedClosuresMatch (wrapper, wrapArg.PublicName, wrapClosure, toWrap, toWrapClosure);
				}
				if (!MatchSimple (wrapper, wrapArg.TypeSpec, toWrap, toWrapArg.TypeSpec))
					return false;
				if (matchNames && !NamesMatch (i, toWrapArg.PublicName, wrapArg.PublicName))
					return false;
			}
			return true;
		}

		bool WrappedClosuresMatch (FunctionDeclaration wrapper, string funcName, ClosureTypeSpec wrapperFunc,
			FunctionDeclaration toWrap, ClosureTypeSpec toWrapFunc)
		{
			if (wrapperFunc.ArgumentCount () < 1 || wrapperFunc.ArgumentCount () > 3)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 46, $"Unexpected parameter count in wrapper function {funcName}");
			var opaquePointerType = wrapperFunc.GetArgument ((wrapperFunc.ArgumentCount () - 1)) as NamedTypeSpec;
			if (opaquePointerType == null || opaquePointerType.Name != "Swift.OpaquePointer")
				return false;

			if (toWrapFunc.ArgumentCount () > 0) {
				var wrapperArg = wrapperFunc.GetArgument (wrapperFunc.ArgumentCount () - 2); // second from last
				var namedType = wrapperArg as NamedTypeSpec;
				if (!IsUnsafeMutablePointer (namedType)) // returns false on null
					return false;
				var actualParms = UndoATuple (namedType.GenericParameters);
				var toWrapParms = toWrapFunc.ArgumentsAsTuple.Elements;
				bool argsMatch = TypeListMatches (wrapper, actualParms, toWrap, toWrapParms);
				if (!argsMatch)
					return false;
			}
			if (toWrapFunc.Throws && !toWrapFunc.IsAsync) {
				var emptyReturn = TypeSpec.IsNullOrEmptyTuple (toWrapFunc.ReturnType);
				if (emptyReturn) {
					throw new NotSupportedException ("action closures are not supported yet");
				}

				var wrapperReturn = wrapperFunc.GetArgument (0);
				var returnNamedTypeSpec = wrapperReturn as NamedTypeSpec;
				if (!IsUnsafeMutablePointer (returnNamedTypeSpec))
					return false;
				if (!(returnNamedTypeSpec.GenericParameters [0] is TupleTypeSpec))
					return false;
				// return type will be:
				// UnsafeMutablePointer<(returnType, Error, Bool)>
				var returnTupleParts = UndoATuple (returnNamedTypeSpec.GenericParameters);
				if (returnTupleParts.Count != 3)
					return false;
				if (!returnTupleParts [1].Equals (new NamedTypeSpec ("Swift.Error")))
					return false;
				if (!returnTupleParts [2].Equals (new NamedTypeSpec ("Swift.Bool")))
					return false;
				var actualReturn = returnTupleParts [0];
				var toWrapReturn = toWrapFunc.ReturnType;
				return TypeListMatches (wrapper, new List<TypeSpec> () { actualReturn }, toWrap, new List<TypeSpec> () { toWrapReturn });
			} else if (toWrapFunc.IsAsync) {
				throw new NotSupportedException ("async closures not supported (yet)");
			} else {
				var emptyWrapReturn = toWrapFunc.ArgumentCount () > 0 ? wrapperFunc.ArgumentCount () == 2 : wrapperFunc.ArgumentCount () == 1;
				var emptyReturn = TypeSpec.IsNullOrEmptyTuple (toWrapFunc.ReturnType);
				if (emptyReturn && emptyWrapReturn)
					return true;
				if (emptyReturn && !emptyWrapReturn)
					return false;
				var wrapperReturn = wrapperFunc.GetArgument (0);
				var returnNamedTypeSpec = wrapperReturn as NamedTypeSpec;
				if (!IsUnsafeMutablePointer (returnNamedTypeSpec))
					return false;
				var actualReturn = returnNamedTypeSpec.GenericParameters [0];
				var toWrapReturn = toWrapFunc.ReturnType;
				return TypeListMatches (wrapper, new List<TypeSpec> () { actualReturn }, toWrap, new List<TypeSpec> () { toWrapReturn });
			}
		}

		static List<TypeSpec> UndoATuple (List<TypeSpec> spec)
		{
			if (spec.Count == 1 && spec [0] is TupleTypeSpec tuple) {
				return tuple.Elements;
			} else {
				return spec;
			}
		}

		static bool IsUnsafeMutablePointer (NamedTypeSpec nt)
		{
			if (nt == null) return false;
			return nt.Name == "Swift.UnsafeMutablePointer";
		}

		bool TypeListMatches (FunctionDeclaration wrapper, List<TypeSpec> wrapTypes, FunctionDeclaration toWrap, List<TypeSpec> toWrapTypes)
		{
			if (wrapTypes.Count != toWrapTypes.Count)
				return false;
			for (int i = 0; i < wrapTypes.Count; i++) {
				var wrapType = wrapTypes [i];
				var toWrapType = toWrapTypes [i];
				if (!MatchSimple (wrapper, wrapType, toWrap, toWrapType))
					return false;
			}
			return true;
		}

		static bool NamesMatch (int index, string originalName, string wrappedName)
		{
			if (String.IsNullOrEmpty (originalName) || String.IsNullOrEmpty (wrappedName))
				return true; // match on either or both names optional
			return originalName == wrappedName;
		}

		bool MatchSimple (FunctionDeclaration wrapper, TypeSpec a, FunctionDeclaration toWrap, TypeSpec b)
		{
			bool aIsGeneric = wrapper.IsTypeSpecGenericReference (a);
			bool bIsGeneric = toWrap.IsTypeSpecGenericReference (b);
			if ((aIsGeneric && !bIsGeneric) || (!aIsGeneric && bIsGeneric))
				return false;
			if (!aIsGeneric && typeMapper.MustForcePassByReference (wrapper, a)) {
				if (!a.EqualsReferenceInvaraint (b))
					return false;
			} else {
				if (!a.Equals (b))
					return false;
			}
			return true;
		}

		FunctionDeclaration LookupReferenceCodeForFunctionDeclaration (FunctionDeclaration funcDecl, string wrapperName, string functionKind)
		{
			var referenceCode = wrappingResult.FunctionReferenceCodeMap.ReferenceCodeFor (funcDecl);
			if (referenceCode != null) {
				var referenceCodeName = MethodWrapping.FuncNameWithReferenceCode (wrapperName, referenceCode.Value);
				var wrappers = wrappingResult.Module.Functions.Where (fn => fn.Name == referenceCodeName).ToList ();
				if (wrappers == null)
					return null;
				if (wrappers.Count != 1)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 37, $"The {functionKind} {funcDecl.ToFullyQualifiedName (true)} has {wrappers.Count} reference codes and should have exactly 1");
				return wrappers [0];
			}
			return null;
		}

		bool ReturnTypesMatch (FunctionDeclaration wrapper, FunctionDeclaration toWrap)
		{
			var toWrapReturn = toWrap.ReturnTypeSpec ?? TupleTypeSpec.Empty;
			TypeSpec wrapReturn = null;
			if (toWrap.ReturnTypeSpec != null && typeMapper.MustForcePassByReference (toWrap, toWrap.ReturnTypeSpec) || toWrap.HasThrows) {
				var returnParam = wrapper.ParameterLists.Last () [0].TypeSpec;
				var named = returnParam as NamedTypeSpec;
				if (named == null)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 38, $"failed to match return type passed by reference. Expected a SwiftBoundGenericType but got {returnParam.GetType ().Name}");
				if (named.Name != "Swift.UnsafeMutablePointer")
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 40, $"failed to match return type passed by reference. Expected an UnsafeMutablePointer, but got {named.Name}");
				if (toWrap.HasThrows) {
					var medusaTuple = named.GenericParameters [0] as TupleTypeSpec;
					if (medusaTuple == null)
						throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 41, $"failed to match return type passed by reference. Expected an UnsafeMutablePointer<TupleTypeSpec>, but got {named.GenericParameters [0]}");
					wrapReturn = medusaTuple.Elements [0];
				} else {
					wrapReturn = named.GenericParameters [0];
				}
			} else {
				wrapReturn = wrapper.ReturnTypeSpec ?? TupleTypeSpec.Empty;
			}

			bool aIsGeneric = toWrap.IsTypeSpecGenericReference (toWrapReturn);
			bool bIsGeneric = wrapper.IsTypeSpecGenericReference (wrapReturn);
			if ((aIsGeneric && !bIsGeneric) || (!aIsGeneric && bIsGeneric))
				return false;

			if (toWrapReturn is ClosureTypeSpec toWrapClosure && wrapReturn is ClosureTypeSpec wrapClosure) {
				return ClosureTypesMatch (wrapper, wrapClosure, toWrap, toWrapClosure);
			} else {
				return toWrapReturn.Equals (wrapReturn);
			}
		}

		bool ClosureTypesMatch (FunctionDeclaration wrapper, ClosureTypeSpec closureWrap, FunctionDeclaration toWrap, ClosureTypeSpec closureToWrap)
		{
			// to wrap will be: ()->()
			// wrap will be: ()->()
			// or
			// to wrap will be: (args)->return
			// wrap will be: (UnsafeMutablePointer<return>, UnsafeMutablePointer<(args)>)->()
			// or
			// to wrap will be (args)->()
			// wrap will be (UnsafeMutablePointer<(args)>)-> ()

			if (!TypeSpec.IsNullOrEmptyTuple (closureWrap.ReturnType))
				return false;


			if (IsVoidOnVoid (closureToWrap) && IsVoidOnVoid (closureWrap))
				return true;

			TypeSpec toMatchReturn = null;
			int toMatchArgsIndex = 0;
			if (closureToWrap.ReturnType != null && !closureToWrap.ReturnType.IsEmptyTuple) {
				if (closureWrap.ArgumentCount () < 1 || closureWrap.ArgumentCount () > 2)
					return false;
				toMatchArgsIndex = closureToWrap.ArgumentCount () == 0 ? -1 : 1;
				toMatchReturn = GetUnsafeMutablePointerBoundType (closureWrap.GetArgument (0));
				if (toMatchReturn == null)
					return false;
				if (closureToWrap.Throws && !closureToWrap.IsAsync) {
					var toMatchReturnList = toMatchReturn as TupleTypeSpec;
					if (toMatchReturnList == null)
						return false;
					toMatchReturn = toMatchReturnList.Elements [0];
				} else if (closureToWrap.IsAsync) {
					throw new NotImplementedException ("Not matching async closures (yet).");
				}
			}

			if (toMatchReturn != null) {
				if (!toMatchReturn.Equals (closureToWrap.ReturnType))
					return false;
			} else {
				if (closureToWrap.Throws || closureToWrap.IsAsync) {
					throw new NotImplementedException ("not matching async action closures (yet).");
				}
			}

			if (toMatchArgsIndex >= 0) {
				if (closureWrap.ArgumentCount () != toMatchArgsIndex + 1)
					return false;
				var opaqueArg = GetUnsafeMutablePointerBoundType (closureWrap.GetArgument (toMatchArgsIndex));
				var toMatchArgs = opaqueArg as TupleTypeSpec;
				if (toMatchArgs == null)
					toMatchArgs = new TupleTypeSpec (opaqueArg);

				var toWrapArgs = closureToWrap.Arguments as TupleTypeSpec;
				if (toWrapArgs == null)
					toWrapArgs = new TupleTypeSpec (closureToWrap.Arguments);

				if (!TypeListMatches (wrapper, toMatchArgs.Elements, toWrap, toWrapArgs.Elements))
					return false;
			}
			return true;
		}

		static bool IsVoidOnVoid (ClosureTypeSpec clos)
		{
			return TypeSpec.IsNullOrEmptyTuple (clos.Arguments) &&
				TypeSpec.IsNullOrEmptyTuple (clos.ReturnType);
		}

		TypeSpec GetUnsafeMutablePointerBoundType (TypeSpec t)
		{
			var named = t as NamedTypeSpec;
			if (named == null)
				return null;
			if (named.Name != "Swift.UnsafeMutablePointer")
				return null;
			return named.GenericParameters [0];
		}
	}
}
