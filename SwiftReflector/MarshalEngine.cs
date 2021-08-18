// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Dynamo.CSLang;
using System.Text;
using System.Linq;
using SwiftRuntimeLibrary;
using SwiftReflector.TypeMapping;
using SwiftRuntimeLibrary.SwiftMarshal;
using Dynamo;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.ExceptionTools;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector {
	public class MarshalEngine {
		CSUsingPackages use;
		List<string> identifiersUsed;
		TypeMapper typeMapper;
		bool skipThisParameterPremarshal = false;
		List<CSFixedCodeBlock> fixedChain = new List<CSFixedCodeBlock> ();
		Version swiftLangVersion;
		Func<int, int, string> genericReferenceNamer = null;

		public MarshalEngine (CSUsingPackages use, List<string> identifiersUsed, TypeMapper typeMapper, Version swiftLangVersion)
		{
			this.use = use;
			this.identifiersUsed = identifiersUsed;
			absolutelyMustBeFirst = new List<CSLine> ();
			preMarshalCode = new List<CSLine> ();
			postMarshalCode = new List<CSLine> ();

			this.typeMapper = typeMapper;
			this.swiftLangVersion = swiftLangVersion;
		}


		public IEnumerable<ICodeElement> MarshalConstructor (TypeDeclaration classDecl, CSClass cl, FunctionDeclaration originalFunc, FunctionDeclaration wrapperFunc,
					string pinvokeCall, CSParameterList pl, TypeSpec swiftObjectType, CSType objectType,
    					string cctorName, CSIdentifier backingField, WrappingResult wrapper, bool isHomonym)
		{
			RequiredUnsafeCode = false;
			preMarshalCode.Clear ();
			postMarshalCode.Clear ();
			fixedChain.Clear ();
			returnLine = null;
			functionCall = null;
			var entity = typeMapper.GetEntityForTypeSpec (swiftObjectType);

			var parms = new CSParameterList (pl); // work with local copy
			CSIdentifier thisIntPtr = null;
			CSIdentifier thisID = null;

			bool skipThisParam = false;
			if (entity.EntityType == EntityType.Struct) {
				if (isHomonym) {
					var name = Uniqueify ("this0", identifiersUsed);
					identifiersUsed.Add (name);
					thisID = new CSIdentifier (name);
					absolutelyMustBeFirst.Add (CreateNominalCall (new CSSimpleType (cl.Name.Name), thisID, objectType, entity));
					postMarshalCode.Add (CSReturn.ReturnLine (thisID));
				} else {
					thisID = CSIdentifier.This;
				}
				if (entity.EntityType != EntityType.Scalar) {
					RequiredUnsafeCode = true;
					var thisDataPtr = new CSIdentifier (Uniqueify ("thisDataPtr", identifiersUsed));
					identifiersUsed.Add (thisDataPtr.Name);
					var returnFixed = new CSFixedCodeBlock (CSSimpleType.ByteStar, thisDataPtr,
										new CSFunctionCall ("StructMarshal.Marshaler.PrepareValueType", false, thisID), null);
					fixedChain.Add (returnFixed);
					var thisPtr = new CSIdentifier (Uniqueify ("thisPtr", identifiersUsed));
					identifiersUsed.Add (thisPtr.Name);
					preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, thisPtr,
											new CSFunctionCall ("IntPtr", true, thisDataPtr)));
					parms.Insert (0, new CSParameter (CSSimpleType.IntPtr, thisPtr, CSParameterKind.None));


				}
			} else {
				// if this is class, make an IntPtr 
				thisIntPtr = new CSIdentifier (Uniqueify ("retvalIntPtr", identifiersUsed));
				preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, thisIntPtr));
			}

			var filteredTypes = FilterParams (parms, wrapperFunc, originalFunc.HasThrows);


			var callParameters = new List<CSBaseExpression> (parms.Count);
			for (int i = 0; i < parms.Count; i++) {
				var originalParamType = i == 0 ? (TypeSpec)TupleTypeSpec.Empty :
					originalFunc.ParameterLists.Last () [i - 1].TypeSpec;
				skipThisParameterPremarshal = skipThisParam && i == 0;
				callParameters.Add (Marshal (classDecl, wrapperFunc, parms [i], filteredTypes [i], false, false, originalParamType));
			}

			// class objects need a class constructor parameter, not so structs.
			if (entity.EntityType != EntityType.Struct) {
				callParameters.Add (new CSFunctionCall ($"{cl.ToCSType ().ToString ()}.GetSwiftMetatype", false));
			}

			if (wrapperFunc.ContainsGenericParameters) {
				AddExtraGenericParameters (wrapperFunc, callParameters);
			}



			var call = new CSFunctionCall (pinvokeCall, false, callParameters.ToArray ());

			if (entity.EntityType == EntityType.Class) {
				functionCall = CSAssignment.Assign (thisIntPtr, CSAssignmentOperator.Assign, call);
				if (classDecl.IsObjC) {
					postMarshalCode.Add (CSReturn.ReturnLine (thisIntPtr));
				} else {
					if (isHomonym) {
						use.AddIfNotPresent (typeof (SwiftObjectRegistry));
						postMarshalCode.Add (CSReturn.ReturnLine (new CSFunctionCall (cl.Name.Name, true, thisIntPtr,
							new CSFunctionCall ($"{cl.Name.Name}.GetSwiftMetatype", false), new CSIdentifier ("SwiftObjectRegistry.Registry"))));
					} else {
						postMarshalCode.Add (CSReturn.ReturnLine (thisIntPtr));
					}
				}
			} else {
				functionCall = new CSLine (call);
			}

			if (RequiredUnsafeCode) {
				CSUnsafeCodeBlock block = new CSUnsafeCodeBlock (null);
				CodeElementCollection<ICodeElement> outside = null, inside = null;
				CollapseFixedChain (block, out outside, out inside);
				inside.AddRange (preMarshalCode);
				inside.Add (functionCall);
				inside.AddRange (postMarshalCode);
				if (returnLine != null)
					inside.Add (returnLine);
				outside.InsertRange (0, absolutelyMustBeFirst);
				yield return outside;
			} else {
				foreach (CSLine l in absolutelyMustBeFirst)
					yield return l;
				foreach (CSLine l in preMarshalCode)
					yield return l;
				yield return functionCall;
				foreach (CSLine l in postMarshalCode)
					yield return l;
				if (returnLine != null)
					yield return returnLine;
			}
		}


		CSLine CreateNominalCall (CSType returnType, CSIdentifier id, CSType objectType, Entity entity)
		{
			if (entity.IsObjCEnum) {
				return CSVariableDeclaration.VarLine (returnType, id, objectType.Default ());
			} else if (entity.IsObjCStruct) {
				return CSVariableDeclaration.VarLine (returnType, id, objectType.Ctor ());
			} else {
				use.AddIfNotPresent (typeof (StructMarshal));
				var marshalCreate = CSFunctionCall.Function ($"StructMarshal.DefaultValueType<{objectType}>");

				return CSVariableDeclaration.VarLine (returnType, id, marshalCreate);
			}
		}

		public IEnumerable<ICodeElement> MarshalFunctionCall (FunctionDeclaration wrapperFuncDecl, bool isExtension, string pinvokeCall,
								      CSParameterList pl,
								      BaseDeclaration typeContext,
								      TypeSpec swiftReturnType,
								      CSType returnType,
								      TypeSpec swiftInstanceType,
								      CSType instanceType,
								      bool includeCastToReturnType,
								      FunctionDeclaration originalFunction,
								      bool includeIntermediateCastToLong = false,
								      int passedInIndexOfReturn = -1,
								      bool originalThrows = false,
								      bool restoreDynamicSelf = false)
		{
			RequiredUnsafeCode = false;
			preMarshalCode.Clear ();
			postMarshalCode.Clear ();
			fixedChain.Clear ();
			returnLine = null;
			functionCall = null;


			if (restoreDynamicSelf) {
				wrapperFuncDecl = wrapperFuncDecl.MacroReplaceType (typeContext.AsTypeDeclaration ().ToFullyQualifiedName (), "Self", true);
				swiftReturnType = swiftReturnType != null ? swiftReturnType.ReplaceName (typeContext.AsTypeDeclaration ().ToFullyQualifiedName (), "Self") : null;
			}

			var parms = new CSParameterList (pl); // work with local copy
			CSIdentifier returnIdent = null, returnIntPtr = null, returnProtocol = null;
			var indexOfReturn = passedInIndexOfReturn;

			var originalReturn = swiftReturnType;
			if (originalThrows) {
				// becomes UnsafeMutablePointer<(swiftReturnType, Error, Bool)>
				swiftReturnType = MethodWrapping.ReturnTypeToExceptionType (swiftReturnType);
				indexOfReturn = 0;
			}

			int indexOfInstance = (swiftReturnType != null && (typeMapper.MustForcePassByReference (typeContext, swiftReturnType)) && !swiftReturnType.IsDynamicSelf) || originalThrows ?
				1 : 0;
			var instanceIsSwiftProtocol = false;
			var instanceIsObjC = false;

			if (swiftInstanceType != null) {
				var entity = typeMapper.GetEntityForTypeSpec (swiftInstanceType);
				instanceIsSwiftProtocol = entity.EntityType == EntityType.Protocol;
				instanceIsObjC = entity.Type.IsObjC;
				var thisIdentifier = isExtension ? new CSIdentifier ("self") : CSIdentifier.This;
				parms.Insert (0, new CSParameter (instanceType, thisIdentifier, wrapperFuncDecl.ParameterLists.Last () [indexOfInstance].IsInOut ?
				    CSParameterKind.Ref : CSParameterKind.None));
			}

			var hasReturn = returnType != null && returnType != CSSimpleType.Void;

			if (hasReturn)
				returnType = ReworkTypeWithNamer (returnType);

			var returnIsScalar = returnType != null && TypeMapper.IsScalar (swiftReturnType);
			var returnEntity = hasReturn && !typeContext.IsTypeSpecGenericReference (swiftReturnType) ? typeMapper.GetEntityForTypeSpec (swiftReturnType) : null;
			var returnIsTrivialEnum = hasReturn && returnEntity != null && returnEntity.EntityType == EntityType.TrivialEnum;
			var returnIsGenericClass = hasReturn && returnEntity != null && returnEntity.EntityType == EntityType.Class && swiftReturnType.ContainsGenericParameters;
			var returnIsClass = hasReturn && returnEntity != null && returnEntity.EntityType == EntityType.Class;
			var returnIsNonTrivialTuple = hasReturn && swiftReturnType is TupleTypeSpec && ((TupleTypeSpec)swiftReturnType).Elements.Count > 1;
			var returnIsClosure = hasReturn && swiftReturnType is ClosureTypeSpec;
			var returnIsGeneric = hasReturn && typeContext.IsTypeSpecGeneric (swiftReturnType) && !returnIsClosure;
			var returnIsAssocPath = hasReturn && typeContext.IsProtocolWithAssociatedTypesFullPath (swiftReturnType as NamedTypeSpec, typeMapper);
			var returnIsNonScalarStruct = hasReturn && !returnIsScalar && returnEntity != null &&
				(returnEntity.EntityType == EntityType.Struct || returnEntity.EntityType == EntityType.Enum);
			var returnIsSelf = hasReturn && swiftReturnType.IsDynamicSelf;

			var retSimple = returnType as CSSimpleType;
			var returnIsInterfaceFromProtocol =
				hasReturn && returnEntity != null && returnEntity.EntityType == EntityType.Protocol && retSimple != null;
			var returnIsProtocolList = hasReturn && swiftReturnType is ProtocolListTypeSpec;
			var returnIsObjCProtocol = hasReturn && returnEntity != null && returnEntity.IsObjCProtocol;
			var returnNeedsPostProcessing = (hasReturn && (returnIsClass || returnIsProtocolList || returnIsInterfaceFromProtocol || returnIsNonTrivialTuple ||
				returnIsGeneric || returnIsNonScalarStruct || returnIsAssocPath || (returnIsSelf && !restoreDynamicSelf))) || originalThrows
				|| returnIsTrivialEnum;

			includeCastToReturnType = includeCastToReturnType || returnIsTrivialEnum;
			includeIntermediateCastToLong = includeIntermediateCastToLong || returnIsTrivialEnum;
			CSType exceptionTupleType = null;
			CSType exceptionContainerType = null;

			if (returnNeedsPostProcessing) {
				if (originalThrows) {
					// Type retType = typeof(Tuple<retype, SwiftError, bool>);
					// -- or -- typeof(Tuple<SwiftError, bool>)
					// -- or -- typeof(Tuple<SwiftExistentialContainerN, SwiftError, bool>)
					// byte *retbuffer = stackalloc byte[StructMarshal.Marshaler.Sizeof(retType)]
					// IntPtr retValIntPtr = new IntPtr(retbuffer)
					// SomePInvokeCall(retValIntPtr);
					// ...
					// post marshal code
					// ...
					// if (StructMarshal.Marshaler.SwiftErrorReturned(retValIntPtr, retType))
					//     throw StructMarshal.Marshaler.ExceptionFromSwiftError(retValIntPtr, retType);
					// else {
					// 	   return (CSRetType)StructMarshal.Marshaler.SwiftReturnValue(retValIntPtr, retType);
					// }

					use.AddIfNotPresent (typeof (StructMarshal));
					exceptionTupleType = ExceptionReturnType (originalReturn, returnType, ref exceptionContainerType);

					var returnPtr = new CSIdentifier (Uniqueify ("retvalPtr", identifiersUsed));
					RequiredUnsafeCode = true;

					preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, returnPtr,
											CSArray1D.New (CSSimpleType.Byte, true,
													  new CSFunctionCall ("StructMarshal.Marshaler.Sizeof", false,
															   exceptionTupleType.Typeof ()))));

					returnIntPtr = new CSIdentifier (Uniqueify ("retvalIntPtr", identifiersUsed));
					preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, returnIntPtr,
						new CSFunctionCall ("IntPtr", true, returnPtr)));
					indexOfReturn = 0;
					parms.Insert (0, new CSParameter (CSSimpleType.IntPtr, returnIntPtr, CSParameterKind.None));
				} else {
					bool returnsIntPtrForReal = returnEntity != null && returnEntity.IsStructClassOrEnum &&
						(returnType is CSSimpleType && ((CSSimpleType)returnType).Name == "IntPtr");

					if (!returnsIntPtrForReal) {
						returnIdent = new CSIdentifier (Uniqueify ("retval", identifiersUsed));
						identifiersUsed.Add (returnIdent.Name);
					}

					if (returnEntity != null && returnEntity.EntityType == EntityType.Enum) {
						// SomeEnumType someEnum = new SomeEnum();
						// fixed (byte *retvalPtr = StructMarshal.Marshaler.PrepareValueType(someEnum)) {
						//    ...
						//    IntPtr retvalIntPtr = new IntPtr(retvalPtr);
						//    SomePInvokeCall(retValIntPtr);
						//    return someEnum;
						// }
						absolutelyMustBeFirst.Add (CSVariableDeclaration.VarLine (returnType, returnIdent,
							new CSFunctionCall (((CSSimpleType)returnType).Name, true)));

						RequiredUnsafeCode = true;
						indexOfReturn = 0;
						parms.Insert (0, new CSParameter (returnType, returnIdent, CSParameterKind.None));
					} else if (returnEntity != null && returnEntity.EntityType == EntityType.Struct) {
						// if the struct is non blitable, we need to do something like this:
						// premarshal
						// returnType retval = new returnType();
						// byte *retbuffer = stackalloc byte[StructMarshal.Strideof(typeof(returnType))];
						// postmarshal
						// StructMarshal.ToNet(retbuffer, typeof(returnType), ref retval);
						RequiredUnsafeCode = true;

						absolutelyMustBeFirst.Add (CreateNominalCall (returnType, returnIdent, returnType, returnEntity));

						indexOfReturn = 0;
						parms.Insert (0, new CSParameter (returnType, returnIdent, CSParameterKind.None));
					} else if (returnEntity != null && (returnEntity.EntityType == EntityType.Class || returnEntity.IsObjCProtocol)) {
						returnIntPtr = new CSIdentifier (Uniqueify ("retvalIntPtr", identifiersUsed));
						identifiersUsed.Add (returnIntPtr.Name);
						if (!returnsIntPtrForReal)
							preMarshalCode.Add (CSVariableDeclaration.VarLine (returnType, returnIdent, CSConstant.Null));

						preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, returnIntPtr, new CSIdentifier ("IntPtr.Zero")));
						use.AddIfNotPresent (typeof (SwiftObjectRegistry));

						if (!returnsIntPtrForReal) {
							var registerCall = NewClassCompiler.SafeMarshalClassFromIntPtr (returnIntPtr, returnType, use, returnEntity.Type.ToFullyQualifiedName (true), typeMapper, returnEntity.IsObjCProtocol);
							postMarshalCode.Add (CSAssignment.Assign (returnIdent, registerCall));
						} else {
							returnIdent = returnIntPtr;
						}
					} else if ((returnEntity != null && returnEntity.EntityType == EntityType.Protocol) || returnIsSelf) {
						CSBaseExpression initialValue;

						if (!(swiftReturnType is NamedTypeSpec ns && ns.Name == "Swift.Any")) {
							initialValue = returnIsSelf ? returnType.Default () : (CSBaseExpression)CSConstant.Null;
						} else {
							initialValue = new CSFunctionCall ("SwiftExistentialContainer0", true);
						}
						preMarshalCode.Insert (0, CSVariableDeclaration.VarLine (returnType, returnIdent, initialValue));
						indexOfReturn = 0;
						parms.Insert (0, new CSParameter (returnType, returnIdent, CSParameterKind.None));
					} else if (swiftReturnType is TupleTypeSpec) {
						RequiredUnsafeCode = true;
						use.AddIfNotPresent (typeof (StructMarshal));
						var tupeType = returnType as CSSimpleType;
						if (tupeType == null)
							throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 38, $"Expected the type of a tuple to be a CSSimpleType but it was {tupeType.Name}");
						var ctorParms = BuildDefaultTupleParams (tupeType.GenericTypes);
						var ctorCall = new CSFunctionCall (tupeType.ToString (), true, ctorParms.ToArray ());

						preMarshalCode.Add (CSVariableDeclaration.VarLine (returnType, returnIdent, ctorCall));
						indexOfReturn = 0;
						parms.Insert (0, new CSParameter (returnType, returnIdent, CSParameterKind.None));
					} else if (returnIsGeneric || returnIsProtocolList || returnIsAssocPath) {
						use.AddIfNotPresent (typeof (StructMarshal));
						preMarshalCode.Add (CSVariableDeclaration.VarLine (returnType, returnIdent, returnType.Default ()));
						indexOfReturn = 0;
						parms.Insert (0, new CSParameter (returnType, returnIdent, CSParameterKind.None));
					} else if (returnIsTrivialEnum) {
						absolutelyMustBeFirst.Add (CSVariableDeclaration.VarLine (returnType, returnIdent, CSFunctionCall.Default ((CSSimpleType)returnType)));
						indexOfReturn = 0;
						parms.Insert (0, new CSParameter (returnType, returnIdent, CSParameterKind.Out));
					}
				}
			}


			var filteredTypeSpec = FilterParams (parms, wrapperFuncDecl, originalThrows);

			var callParameters = new List<CSBaseExpression> (parms.Count);

			for (int i = 0; i < parms.Count; i++) {
				var offsetToOriginalArgs = (hasReturn && indexOfReturn >= 0 ? 1 : 0) + (swiftInstanceType != null ? 1 : 0);
				var p = parms [i];
				// if it's the instance, pass that
				// if it's the return, pass that
				// otherwise take it from the original functions primary parameter list
				TypeSpec originalParm = null;

				if (i == indexOfInstance && swiftInstanceType != null) {
					originalParm = swiftInstanceType;
				} else if (hasReturn && i == indexOfReturn) {
					originalParm = swiftReturnType;
				} else {
					originalParm = originalFunction.ParameterLists.Last () [i - offsetToOriginalArgs].TypeSpec;
				}
				callParameters.Add (Marshal (typeContext, wrapperFuncDecl, p, filteredTypeSpec [i], instanceIsSwiftProtocol && i == indexOfInstance,
							 indexOfReturn >= 0 && i == indexOfReturn, originalParm));
			}

			if (wrapperFuncDecl.ContainsGenericParameters) {
				AddExtraGenericParameters (wrapperFuncDecl, callParameters);
			}


			var call = new CSFunctionCall (pinvokeCall, false, callParameters.ToArray ());

			if (returnIsClosure) {
				call = BuildWrappedClosureCall (call, returnType as CSSimpleType);
			}

			if (originalThrows) {
				use.AddIfNotPresent (typeof (SwiftError));
				// post marshal code
				// ...
				// if (StructMarshal.Marshaler.ExceptionReturnContainsSwiftError(retValIntPtr, retType))
				//     throw StructMarshal.Marshaler.GetExceptionThrown(retValIntPtr, retType);
				// else {
				// 	   return (CSRetType)StructMarshal.Marshaler.SwiftReturnValue(retValIntPtr, retType);
				// }
				// -- OR -- if protocol list type
				// else {
				//        var container = (CSContainerType)StructMarshal.Marshaler.GetErrorReturnValue<CSContainerType>(retalIntPtr);
				//        return StructMarshal.Marshaler.ExistentialPayload<CSRetType>(container);
				// }
				var checkExcept = new CSFunctionCall ("StructMarshal.Marshaler.ExceptionReturnContainsSwiftError",
													false, returnIntPtr, exceptionTupleType.Typeof ());
				var ifBlock = new CSCodeBlock ();
				ifBlock.Add (new CSLine (new CSThrow (new CSFunctionCall ("StructMarshal.Marshaler.GetExceptionThrown",
																	  false, returnIntPtr, exceptionTupleType.Typeof ()))));
				CSIfElse ifElse = null;

				if (originalReturn == null || originalReturn.IsEmptyTuple) {
					ifElse = new CSIfElse (checkExcept, ifBlock);
				} else {
					var elseBlock = new CSCodeBlock ();

					if (exceptionContainerType != null) {
						var containerID = new CSIdentifier (Uniqueify ("container", identifiersUsed));
						identifiersUsed.Add (containerID.Name);
						elseBlock.Add (CSVariableDeclaration.VarLine (CSSimpleType.Var, containerID,
							new CSFunctionCall ($"StructMarshal.Marshaler.GetErrorReturnValue<{exceptionContainerType.ToString ()}>", false, returnIntPtr)));
						elseBlock.Add (CSReturn.ReturnLine (new CSFunctionCall ($"StructMarshal.Marshaler.ExistentialPayload<{returnType.ToString ()}>", false, containerID)));

					} else {
						elseBlock.Add (CSReturn.ReturnLine (new CSFunctionCall ($"StructMarshal.Marshaler.GetErrorReturnValue<{returnType.ToString ()}>",
							false, returnIntPtr)));
					}
					ifElse = new CSIfElse (checkExcept, ifBlock, elseBlock);
				}

				postMarshalCode.Add (new CSLine (ifElse, false));
				this.functionCall = new CSLine (call);
			} else {
				// Post marshal code demands an intermediate return value
				if (postMarshalCode.Count > 0 && ((object)returnIdent) == null && (returnType != null && returnType != CSSimpleType.Void)) {
					returnIdent = new CSIdentifier (Uniqueify ("retval", identifiersUsed));
					identifiersUsed.Add (returnIdent.Name);
					preMarshalCode.Add (CSVariableDeclaration.VarLine (returnType, returnIdent, returnType.Default ()));
				}


				if (((object)returnIdent) != null) {
					// if returnIntPtr is non-null, then the function returns a pointer to a class
					// If this is the case, we have post marshal code which will assign it to
					// retval.

					if (typeMapper.MustForcePassByReference (typeContext, swiftReturnType) || returnIsNonTrivialTuple || returnIsProtocolList) {
						this.functionCall = new CSLine (call);
					} else {
						CSBaseExpression callExpr = call;
						if (includeCastToReturnType && returnType != null && returnType != CSSimpleType.Void) {
							if (includeIntermediateCastToLong) {
								callExpr = new CSCastExpression (CSSimpleType.Long, callExpr);
							}
							callExpr = new CSCastExpression (returnType, callExpr);
						}
						this.functionCall = CSAssignment.Assign ((returnIntPtr ?? returnProtocol) ?? returnIdent, callExpr);
					}
					this.returnLine = CSReturn.ReturnLine (returnIdent);
				} else {
					if (returnType != null && returnType != CSSimpleType.Void) {
						if (includeCastToReturnType) {
							CSBaseExpression expr = call;
							if (includeIntermediateCastToLong) {
								expr = new CSCastExpression (CSSimpleType.Long, expr);
							}
							expr = new CSCastExpression (returnType, expr);
							this.functionCall = CSReturn.ReturnLine (expr);
						} else {
							this.functionCall = CSReturn.ReturnLine ((ICSExpression)call);
						}
					} else
						this.functionCall = new CSLine (call);
				}
			}

			if (RequiredUnsafeCode) {
				var block = new CSUnsafeCodeBlock (null);
				CodeElementCollection<ICodeElement> outside = null, inside = null;
				CollapseFixedChain (block, out outside, out inside);
				inside.AddRange (preMarshalCode);
				inside.Add (functionCall);
				inside.AddRange (postMarshalCode);
				if (returnLine != null)
					inside.Add (returnLine);
				outside.InsertRange (0, absolutelyMustBeFirst);
				yield return outside;
			} else {
				foreach (var l in absolutelyMustBeFirst)
					yield return l;
				foreach (var l in preMarshalCode)
					yield return l;
				yield return functionCall;
				foreach (var l in postMarshalCode)
					yield return l;
				if (returnLine != null)
					yield return returnLine;
			}

		}

		static CSType ExceptionReturnType (TypeSpec swiftReturnType, CSType csReturnType, ref CSType containerType)
		{
			if (csReturnType != null && csReturnType != CSSimpleType.Void) {
				if (swiftReturnType is ProtocolListTypeSpec pl) {
					csReturnType = containerType = new CSSimpleType ($"SwiftExistentialContainer{pl.Protocols.Count}");
				}
				return new CSSimpleType ("Tuple", false, csReturnType, new CSSimpleType (typeof (SwiftError)), new CSSimpleType (typeof (bool)));
			} else {
				return new CSSimpleType ("Tuple", false, new CSSimpleType (typeof (SwiftError)), new CSSimpleType (typeof (bool)));
			}
		}


		public static CSFunctionCall BuildWrappedClosureCall (CSBaseExpression call, CSSimpleType expectedType)
		{
			if (expectedType == null)
				throw new ArgumentNullException (nameof (expectedType));
			if ((object)call == null)
				throw new ArgumentNullException (nameof (call));
			var isFunc = (expectedType.GenericTypeName ?? expectedType.Name) == "Func";

			var funcName = new StringBuilder ();
			funcName.Append (isFunc ? "SwiftObjectRegistry.Registry.FuncForSwiftClosure" :
			                 "SwiftObjectRegistry.Registry.ActionForSwiftClosure");


			if (expectedType.IsGeneric) {
				foreach (var s in expectedType.GenericTypes.Select (gt => gt.ToString ()).BracketInterleave ("<", ">", ","))
					funcName.Append (s);
			}
			var wrapCall = new CSFunctionCall (funcName.ToString (), false, call);
			return wrapCall;
		}

		public static CSFunctionCall BuildBlindClosureCall (CSBaseExpression call, CSSimpleType expectedType, CSUsingPackages use)
		{
			if (expectedType == null)
				throw new ArgumentNullException (nameof (expectedType));
			if ((object)call == null)
				throw new ArgumentNullException (nameof (call));

			use.AddIfNotPresent (typeof (StructMarshal));

			var wrapCall = new CSFunctionCall ("StructMarshal.Marshaler.GetBlindSwiftClosureRepresentation", false,
				expectedType.Typeof (), call);
			return wrapCall;
		}
		

		void CollapseFixedChain (CodeElementCollection<ICodeElement> block, out CodeElementCollection<ICodeElement> outside, out CodeElementCollection<ICodeElement> inside)
		{
			if (fixedChain.Count == 0) {
				outside = inside = block;
			} else {
				outside = block;
				block.Add (fixedChain [0]);
				for (int i = 1; i < fixedChain.Count; i++) {
					fixedChain [i - 1].Add (fixedChain [i]);
				}
				inside = fixedChain.Last ();
				fixedChain.Clear ();
			}
		}




		void AddExtraGenericParameters (FunctionDeclaration wrapperFunc, List<CSBaseExpression> callParameters)
		{
			foreach (GenericDeclaration genDecl in wrapperFunc.Generics) {
				if (TopLevelFunctionCompiler.GenericDeclarationIsReferencedByGenericClassInParameterList (wrapperFunc, genDecl, typeMapper))
					continue;
				use.AddIfNotPresent (typeof (StructMarshal));
				var csGenType = ReworkGenericTypeFromWrapperFunc (genDecl, wrapperFunc);
				if (csGenType.InterfaceConstraints.Count <= 0) {
					callParameters.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false,
									    csGenType.Typeof ()));
				} else {
					CSType argType = csGenType;
					if (genDecl.Constraints.Count == 1 && genDecl.Constraints [0] is InheritanceConstraint inh) {
						var protoEntity = typeMapper.GetEntityForTypeSpec (inh.InheritsTypeSpec);
						if (protoEntity != null && protoEntity.Type is ProtocolDeclaration proto && proto.HasAssociatedTypes) {
							var argNtb = typeMapper.MapType (wrapperFunc, inh.InheritsTypeSpec, false);
							var ifaceType = argNtb.ToCSType (use) as CSSimpleType;
							argType = new CSSimpleType (OverrideBuilder.AssociatedTypeProxyClassName (proto), false, ifaceType.GenericTypes);
						}

					}
					
					var ifaceConstrs = new CSArray1DInitialized (CSSimpleType.Type,
										   csGenType.InterfaceConstraints.Select (constr =>
															  (CSBaseExpression)constr.Typeof ()).ToArray ());
					callParameters.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false,
									    argType.Typeof (), ifaceConstrs));
				}
			}
			foreach (GenericDeclaration genDecl in wrapperFunc.Generics) {
				if (TopLevelFunctionCompiler.GenericDeclarationIsReferencedByGenericClassInParameterList (wrapperFunc, genDecl, typeMapper))
					continue;
				var csGenType = ReworkGenericTypeFromWrapperFunc (genDecl, wrapperFunc);
				foreach (CSType csType in csGenType.InterfaceConstraints) {
					callParameters.Add (new CSFunctionCall ("StructMarshal.Marshaler.ProtocolWitnessof", false,
									    csType.Typeof (), csGenType.Typeof ()));
				}
			}
		}


		CSGenericReferenceType ReworkGenericTypeFromWrapperFunc (GenericDeclaration genDecl, FunctionDeclaration func)
		{
			if (func.IsEqualityConstrainedByAssociatedType (genDecl, typeMapper)) {
				var protoRef = func.RefProtoFromConstrainedGeneric (genDecl, typeMapper);
				var assocType = func.AssociatedTypeDeclarationFromConstrainedGeneric (genDecl, typeMapper);
				var index = protoRef.Protocol.AssociatedTypes.IndexOf (assocType);
				var genRef = new CSGenericReferenceType (0, index);
				genRef.ReferenceNamer = NewClassCompiler.MakeAssociatedTypeNamer (protoRef.Protocol);
				return genRef;
			} else {
				// guh
				if (!Char.IsUpper (genDecl.Name [0]))
					throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 39, "Expected an uppercase letter in generic type declaration, but got " + genDecl.Name [0]);
				// FIXME
				// what happens after Z?
				int depth = genDecl.Name [0] - 'T';
				int index = Int32.Parse (genDecl.Name.Substring (1));
				var decl = func.GetGeneric (depth, index);
				var genRef = new CSGenericReferenceType (depth, index);
				genRef.ReferenceNamer = GenericReferenceNamer ?? genRef.ReferenceNamer;
				var constraints = decl.
						      Constraints.
						      OfType<InheritanceConstraint> ().
						      Where (cnstr => {
							      Entity en = typeMapper.GetEntityForTypeSpec (cnstr.InheritsTypeSpec);
							      return en.EntityType == EntityType.Protocol;
						      }).OrderBy ((arg) => ((NamedTypeSpec)arg.InheritsTypeSpec).Name).
				Select (inh => {
					var selfDepthIndex = IsProtocolWithSelfInArguments (inh.InheritsTypeSpec) ? new Tuple<int, int> (depth, index) : null;
					NetTypeBundle ntb = typeMapper.MapType (func, inh.InheritsTypeSpec, false, selfDepthIndex: selfDepthIndex);
					return ntb.ToCSType (use);
				}).ToList ();
				genRef.InterfaceConstraints.AddRange (constraints);
				return genRef;
			}
		}

		CSParameter ReworkParameterWithNamer (CSParameter p)
		{
			if (GenericReferenceNamer == null)
				return p;
			var pClone = ReworkTypeWithNamer (p.CSType);
			return new CSParameter (pClone, p.Name, p.ParameterKind, p.DefaultValue);
		}

		CSType ReworkTypeWithNamer (CSType ty)
		{
			if (ty is CSGenericReferenceType genRef) {
				var newGen = new CSGenericReferenceType (genRef.Depth, genRef.Index);
				newGen.ReferenceNamer = GenericReferenceNamer;
				return newGen;
			} else if (ty is CSSimpleType simple) {
				if (simple.GenericTypes == null)
					return simple;
				var genSubTypes = new CSType [simple.GenericTypes.Length];
				for (int i = 0; i < genSubTypes.Length; i++) {
					genSubTypes [i] = ReworkTypeWithNamer (simple.GenericTypes [i]);
				}
				var simpleClone = new CSSimpleType (simple.GenericTypeName, simple.IsArray, genSubTypes);
				return simpleClone;
			} else {
				throw new NotImplementedException ($"Unable to rework type {ty.GetType ().Name} {ty.ToString ()} as generic reference");
			}
		}

		CSBaseExpression Marshal (BaseDeclaration typeContext, FunctionDeclaration funcDecl, CSParameter p, TypeSpec swiftType,
			bool marshalProtocolAsValueType, bool isReturnVariable, TypeSpec originalType)
		{
			p = ReworkParameterWithNamer (p);

			if (swiftType.HasDynamicSelf)
				return MarshalDynamicSelf (typeContext, funcDecl, p, swiftType, marshalProtocolAsValueType, isReturnVariable);
			if (typeContext.IsTypeSpecGenericReference (swiftType) || typeContext.IsProtocolWithAssociatedTypesFullPath (swiftType as NamedTypeSpec, typeMapper)) {
				return MarshalGenericReference (typeContext, funcDecl, p, swiftType as NamedTypeSpec);
			}
			if (funcDecl.IsTypeSpecGenericReference (swiftType) || funcDecl.IsProtocolWithAssociatedTypesFullPath (swiftType as NamedTypeSpec, typeMapper)) {
				return MarshalGenericReference (funcDecl, funcDecl, p, swiftType as NamedTypeSpec);
			}
			if (swiftType is NamedTypeSpec && typeContext.IsTypeSpecBoundGeneric (swiftType)) {
				return MarshalBoundGeneric (typeContext, p, funcDecl, swiftType as NamedTypeSpec, marshalProtocolAsValueType, isReturnVariable);
			}
			if (swiftType is NamedTypeSpec && funcDecl.IsTypeSpecBoundGeneric (swiftType)) {
				return MarshalBoundGeneric (funcDecl, p, funcDecl, swiftType as NamedTypeSpec, marshalProtocolAsValueType, isReturnVariable);
			}
			var entityType = typeMapper.GetEntityTypeForTypeSpec (swiftType);
			switch (entityType) {
			case EntityType.Scalar:
				return MarshalScalar (p);
			case EntityType.Class:
				return MarshalClass (p, swiftType as NamedTypeSpec);
			case EntityType.Struct:
				return MarshalStruct (p, swiftType as NamedTypeSpec);
			case EntityType.Enum:
				return MarshalNominal (p);
			case EntityType.Protocol:
				return MarshalProtocol (p, swiftType as NamedTypeSpec, marshalProtocolAsValueType);
			case EntityType.TrivialEnum:
				return MarshalTrivialEnumAsPointer (p, isReturnVariable);
			case EntityType.Closure:
				return MarshalClosure (p, swiftType as ClosureTypeSpec, originalType as ClosureTypeSpec);
			case EntityType.ProtocolList:
				return MarshalProtocolList (typeContext, p, swiftType as ProtocolListTypeSpec, marshalProtocolAsValueType);
			case EntityType.Tuple:
			case EntityType.None:
				break;
			}
			throw new NotImplementedException ($"Uh-oh - not ready for {swiftType.ToString ()}, a {entityType}.");
		}

		CSBaseExpression MarshalScalar (CSParameter p)
		{
			return ParmName (p);
		}

		CSBaseExpression MarshalBoundGeneric (BaseDeclaration typeContext, CSParameter p, FunctionDeclaration funcDecl, NamedTypeSpec swiftType, bool marshalProtocolAsValueType, bool isReturnValue)
		{
			var typeName = p.CSType.ToString ();
			if (typeName == "System.IntPtr" || typeName == "IntPtr")
				return ParmName (p);
			if (TypeMapper.IsSwiftPointerType (swiftType.Name)) {
				return MarshalAsPointer (typeContext, funcDecl, p, swiftType.GenericParameters [0], marshalProtocolAsValueType, isReturnValue);
			}
			var enType = typeMapper.GetEntityTypeForTypeSpec (swiftType);
			if (enType == EntityType.Class)
				return MarshalClass (p, swiftType);
			return MarshalAsPointer (typeContext, funcDecl, p, swiftType, marshalProtocolAsValueType, isReturnValue);
		}

		CSBaseExpression MarshalClosure (CSParameter p, ClosureTypeSpec closure, ClosureTypeSpec originalClosure)
		{
			var csSimp = p.CSType as CSSimpleType;
			if (csSimp == null) {
				throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 41, $"Expected parameter type to be a simple type, but was {p.CSType.GetType ().Name}");
			}
			use.AddIfNotPresent (typeof (SwiftObjectRegistry));
			use.AddIfNotPresent (typeof (SwiftClosureRepresentation));
			// action
	    		if (csSimp.GenericTypeName == "Action") {
				var typeArr = new CSArray1DInitialized ("Type",
					csSimp.GenericTypes.Select (ct => ct.Typeof ()));
				string actionCallName = csSimp.GenericTypes.Count () == 0 ?
							      "SwiftClosureRepresentation.ActionCallbackVoidVoid" :
							      "SwiftClosureRepresentation.ActionCallback";
				return new CSFunctionCall ("SwiftObjectRegistry.Registry.SwiftClosureForDelegate",
							 false,  p.Name, new CSIdentifier (actionCallName), typeArr);
			} else {  // func
				var typeArr = new CSArray1DInitialized ("Type",
									csSimp.GenericTypes.Take (csSimp.GenericTypes.Length - 1).Select (ct => ct.Typeof ()));
				string funcCallName = CallbackNameForFuncClosure (csSimp.GenericTypes.Count (), originalClosure);
				var retType = TypeOfFuncClosureReturnType (csSimp.GenericTypes.Last (), originalClosure);
				return new CSFunctionCall ("SwiftObjectRegistry.Registry.SwiftClosureForDelegate",
							false, p.Name, new CSIdentifier (funcCallName), typeArr, retType);
			}
		}

		static CSBaseExpression TypeOfFuncClosureReturnType (CSType returnType, ClosureTypeSpec closure)
		{
			if (closure.Throws && !closure.IsAsync) {
				var newType = new CSSimpleType ("Tuple", false, new CSType [] { returnType, new CSSimpleType ("SwiftError"), CSSimpleType.Bool });
				return newType.Typeof ();
			} else if (closure.IsAsync) {
				throw new NotImplementedException ("Async closures not supported yet.");
			} else {
				return returnType.Typeof ();
			}
		}

		static string CallbackNameForFuncClosure (int argumentCount, ClosureTypeSpec closure)
		{
			if (closure.Throws && !closure.IsAsync) {
				return argumentCount == 1 ? "SwiftClosureRepresentation.FuncCallbackVoidMaybeThrows" :
					"SwiftClosureRepresentation.FuncCallbackMaybeThrows";
			} else if (closure.IsAsync) {
				throw new NotImplementedException ("Need to implement async closures");
			} else {
				return argumentCount == 1 ? "SwiftClosureRepresentation.FuncCallbackVoid" :
							      "SwiftClosureRepresentation.FuncCallback";
			}
		}

		CSBaseExpression MarshalAsPointer (BaseDeclaration typeContext, FunctionDeclaration funcDecl, CSParameter p, TypeSpec pointsTo, bool marshalProtocolAsValueType, bool isReturnVariable)
		{
			if (typeContext.IsTypeSpecGenericReference (pointsTo))
				return MarshalGenericReferenceAsPointer (typeContext, funcDecl, p, pointsTo as NamedTypeSpec, isReturnVariable);
			if (funcDecl.IsTypeSpecGenericReference (pointsTo))
				return MarshalGenericReferenceAsPointer (funcDecl, funcDecl, p, pointsTo as NamedTypeSpec, isReturnVariable);
			if (typeContext.IsProtocolWithAssociatedTypesFullPath (pointsTo as NamedTypeSpec, typeMapper))
				return MarshalAssociatedTypePathAsPointer (typeContext, funcDecl, p, pointsTo as NamedTypeSpec, isReturnVariable);
			if (funcDecl.IsProtocolWithAssociatedTypesFullPath (pointsTo as NamedTypeSpec, typeMapper))
				return MarshalAssociatedTypePathAsPointer (funcDecl, funcDecl, p, pointsTo as NamedTypeSpec, isReturnVariable);

			var isBoundGenericWithRespectToTypeContext = typeContext.IsTypeSpecBoundGeneric (pointsTo);
			var isBoundGenericWithRespectToFunc = funcDecl.IsTypeSpecBoundGeneric (pointsTo);

			if (pointsTo is NamedTypeSpec && isBoundGenericWithRespectToFunc && !isBoundGenericWithRespectToTypeContext)
				return MarshalBoundGenericAsPointer (funcDecl, p, pointsTo as NamedTypeSpec, marshalProtocolAsValueType, isReturnVariable);
			if (pointsTo is NamedTypeSpec && !isBoundGenericWithRespectToFunc && isBoundGenericWithRespectToTypeContext)
				return MarshalBoundGenericAsPointer (funcDecl, p, pointsTo as NamedTypeSpec, marshalProtocolAsValueType, isReturnVariable);

			var entityType = typeMapper.GetEntityTypeForTypeSpec (pointsTo);
			// can be null here for types with no named (closures, tuples)
			var entity = typeMapper.GetEntityForTypeSpec (pointsTo);
			switch (entityType) {
			case EntityType.Scalar:
				return MarshalScalarAsPointer (p);
			case EntityType.TrivialEnum:
				return MarshalTrivialEnumAsPointer (p, isReturnVariable);
			case EntityType.Class:
				return MarshalClassAsPointer (p, funcDecl, pointsTo as NamedTypeSpec, marshalProtocolAsValueType, isReturnVariable);
			case EntityType.Enum:
			case EntityType.Struct:
				// entity can't be null if we get here
				if (entity.IsObjCStruct)
					return MarshalObjStructAsPointer (p);
				if (entity.IsObjCEnum)
					return MarshalObjCEnumAsPointer (p);
				return MarshalNominalAsPointer (p, isReturnVariable);
			case EntityType.Protocol:
				return MarshalProtocolAsPointer (p, pointsTo as NamedTypeSpec, isReturnVariable);
			case EntityType.Tuple:
				return MarshalTupleAsPointer (typeContext, funcDecl, p, pointsTo as TupleTypeSpec, marshalProtocolAsValueType, isReturnVariable);
			case EntityType.ProtocolList:
				return MarshalProtocolListAsPointer (typeContext, p, pointsTo as ProtocolListTypeSpec, isReturnVariable);
			default:
				throw new NotImplementedException ();
			}
		}

		CSBaseExpression MarshalBoundGenericAsPointer (FunctionDeclaration funcDecl, CSParameter p, NamedTypeSpec swiftType, bool marshalProtocolAsValueType, bool isReturnVariable)
		{
			var entityType = typeMapper.GetEntityTypeForTypeSpec (swiftType);
			if (entityType == EntityType.Struct || entityType == EntityType.Enum) {
				return MarshalNominalAsPointer (p, isReturnVariable);
			} else if (entityType == EntityType.Struct) {
				return MarshalClassAsPointer (p, funcDecl, swiftType, marshalProtocolAsValueType, isReturnVariable);
			}
			throw new NotImplementedException ("unknown type in MarshalBoundGenericAsPointer");
		}

		CSBaseExpression MarshalScalarAsPointer (CSParameter p)
		{
			return new CSIdentifier ($"ref {p.Name}");
		}

		CSBaseExpression MarshalTrivialEnumAsPointer (CSParameter p, bool isReturnVariable)
		{
			// var pVal = (nint)(long)p;
			// var pPtr = &pVal;
			// ... new IntPtr (pPtr);
			// p = (PType)pVal;
			var pValName = Uniqueify (p.Name.Name + "Val", identifiersUsed);
			identifiersUsed.Add (pValName);
			var pValID = new CSIdentifier (pValName);
			var pCast = new CSCastExpression (new CSSimpleType ("nint"), new CSCastExpression (CSSimpleType.Long, p.Name));
			var pValDecl = CSVariableDeclaration.VarLine (pValID, pCast);
			preMarshalCode.Add (pValDecl);
			var pPtrName = Uniqueify (p.Name.Name + "Ptr", identifiersUsed);
			identifiersUsed.Add (pPtrName);
			var pPtrID = new CSIdentifier (pPtrName);
			var pPtrDecl = CSVariableDeclaration.VarLine (pPtrID, new CSUnaryExpression (CSUnaryOperator.AddressOf, pValID));
			preMarshalCode.Add (pPtrDecl);
			if (p.ParameterKind == CSParameterKind.Out || p.ParameterKind == CSParameterKind.Ref) {
				var reAssign = CSAssignment.Assign (p.Name, new CSCastExpression (p.CSType, new CSCastExpression (CSSimpleType.Long, pValID)));
				postMarshalCode.Add (reAssign);
			}
			RequiredUnsafeCode = true;
			return new CSFunctionCall ("IntPtr", true, pPtrID);
		}

		CSBaseExpression MarshalClassAsPointer (CSParameter p, FunctionDeclaration funcDecl, NamedTypeSpec swiftType, bool marshalProtocolAsValueType, bool isReturnVariable)
		{
			throw new NotImplementedException ();
		}

		CSBaseExpression MarshalNominalAsPointer (CSParameter p, bool isReturnValue)
		{
			// fixed (byte * pSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType(p)) {
			//   SomePiCall(... newIntPtr(pSwiftDataPtr));
			// }
			string enumDataPtrName = Uniqueify (String.Format ("{0}SwiftDataPtr", p.Name.Name), identifiersUsed);
			identifiersUsed.Add (enumDataPtrName);
			CSIdentifier enumDataPtr = new CSIdentifier (enumDataPtrName);

			RequiredUnsafeCode = true;
			use.AddIfNotPresent (typeof (StructMarshal));
			CSFixedCodeBlock fixedBlock = new CSFixedCodeBlock (CSSimpleType.ByteStar, enumDataPtr,
			    new CSFunctionCall ("StructMarshal.Marshaler.PrepareValueType", false, p.Name), null);
			fixedChain.Add (fixedBlock);
			return new CSCastExpression ("IntPtr", enumDataPtr);
		}

		CSBaseExpression MarshalObjStructAsPointer (CSParameter p)
		{
			var structReference = new CSIdentifier ($"ref {p.Name}");
			return structReference;
		}

		CSBaseExpression MarshalObjCEnumAsPointer (CSParameter p)
		{
			RequiredUnsafeCode = true;
			return new CSIdentifier ($"ref {p.Name}");
		}

		CSBaseExpression MarshalProtocolAsPointer (CSParameter p, NamedTypeSpec cl, bool isReturnValue)
		{
			// normal argument
			// var paramProxy = SwiftObjectRegistry.Registry.ProxyForInterface<ifaceType>(param);
			// var paramContainer = new SwiftExistentialContainer1 (((BaseProxy)paramProxy).SwiftExistentialContainer);
			// var paramContainerPtr = &paramContainer;
			// SomePiCall (... new IntPtr(paramContainerPtr));

			// return value
			// var paramContainer = new SwiftExistentialContainer1 ();
			// var paramContainerPtr = &paramContainer;
			// SomePiCall (new IntPtr (paramContainterPtr));
			// p = SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<IFaceType> (paramContainer);

			// return value of Swift.Any
			// var paramContainer = new SwiftExistentialContainer0 ();
			// var paramContainerPtr = &paramContainer;
			// SomePiCall (new IntPtr (paramContainerPtr)
			// paramContainer.CopyTo (p)

			RequiredUnsafeCode = true;

			var containerIdent = new CSIdentifier (Uniqueify (p.Name + "Container", identifiersUsed));
			identifiersUsed.Add (containerIdent.Name);
			var containerPtrIdent = new CSIdentifier (Uniqueify (p.Name + "ContainerPtr", identifiersUsed));
			identifiersUsed.Add (containerPtrIdent.Name);
			var proxyIdent = new CSIdentifier (Uniqueify (p.Name + "Proxy", identifiersUsed));
			identifiersUsed.Add (containerIdent.Name);

			var isAny = cl.Name == "Swift.Any";

			CSFunctionCall newContainer = null;
			use.AddIfNotPresent (typeof (SwiftExistentialContainer1));
			if (!isReturnValue) {
				var proxyDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, proxyIdent, new CSFunctionCall ($"SwiftObjectRegistry.Registry.ProxyForInterface<{p.CSType.ToString()}>", false, p.Name));
				preMarshalCode.Add (proxyDecl);
				var castoRama = new CSParenthesisExpression (new CSCastExpression (new CSSimpleType (typeof (BaseProxy)), proxyIdent));
				newContainer = new CSFunctionCall ("SwiftExistentialContainer1", true, castoRama.Dot (new CSIdentifier ("ProxyExistentialContainer")));
			} else {
				newContainer = new CSFunctionCall (isAny ? "SwiftExistentialContainer0" : "SwiftExistentialContainer1", true);
			}
			var containerDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, containerIdent, newContainer);
			preMarshalCode.Add (containerDecl);

			var containerPtrDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, containerPtrIdent,
				new CSUnaryExpression (CSUnaryOperator.AddressOf, containerIdent));

			preMarshalCode.Add (containerPtrDecl);

			if (p.Name.Name != CSIdentifier.This.Name) {
				if (isAny) {
					var copyIt = CSFunctionCall.FunctionCallLine ($"{containerIdent}.CopyTo", false, new CSUnaryExpression (CSUnaryOperator.Ref, p.Name));
					postMarshalCode.Add (copyIt);
				} else {
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));
					var rebuildIt = CSAssignment.Assign (p.Name, new CSFunctionCall ($"SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<{p.CSType.ToString ()}>",
						false, containerIdent));
					postMarshalCode.Add (rebuildIt);
				}
			}

			return new CSFunctionCall ("IntPtr", true, containerPtrIdent);
		}

		CSBaseExpression MarshalProtocolListAsPointer (BaseDeclaration typeContext, CSParameter p, ProtocolListTypeSpec proto, bool isReturnValue)
		{
			// normal argument
			// SwiftExistentialContainerN paramContainer;
			// SomePICall (ref protocolListReturn);
			// arg = StructMarshal.ExistentialPayload<pType>(protocolListReturn);
			// var container = (SwiftExistentialContainerN)SwiftObjectRegistry.Registry.ExistentialContainerForProtocols (p, types);
			// callSite (ref container);

			// return value
			// SwiftExistentialContainern paramContainer = default(SwiftExistentialContainern);
			// callSite (ref container);
			// parm = StructMarshal.Marshaler.ExistentialPayload<ParmType>(container);

			use.AddIfNotPresent (typeof (SwiftExistentialContainer0));
			var containerType = new CSSimpleType ($"SwiftExistentialContainer{proto.Protocols.Count}");
			var localProto = new CSIdentifier (Uniqueify (p.Name + "Protocol", identifiersUsed));

			if (isReturnValue) {
				var protoDecl = CSVariableDeclaration.VarLine (containerType, localProto, containerType.Default ());
				preMarshalCode.Add (protoDecl);
				postMarshalCode.Add (CSAssignment.Assign (p.Name, new CSFunctionCall ($"StructMarshal.Marshaler.ExistentialPayload<{p.CSType.ToString ()}>", false, localProto)));
				return ParmName (localProto.Name, CSParameterKind.Ref);
			} else {
				var csProtoTypes = proto.Protocols.Keys.Select (ns => (CSBaseExpression)typeMapper.MapType (typeContext, ns, false).ToCSType (use).Typeof ());
				var typeExpr = new CSArray1DInitialized (CSSimpleType.Type.ToString (), csProtoTypes);
				var protoDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, localProto, new CSCastExpression (containerType,
					new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocols", false, p.Name, typeExpr)));
				preMarshalCode.Add (protoDecl);
				CSParameterKind kind = p.ParameterKind == CSParameterKind.None ? CSParameterKind.Ref : p.ParameterKind;
				return ParmName (localProto.Name, kind);
			}
		}

		CSBaseExpression MarshalAssociatedTypePathAsPointer (BaseDeclaration typeContext, FunctionDeclaration funcDecl, CSParameter p, NamedTypeSpec pointsTo, bool isReturnValue)
		{
			// in swift, we have a type which is T.Assoc
			// in C#, it's going to be an unadorned generic, ATAssoc
			// byte *pBufferPtr = stackalloc byte[StructMarshal.Strideof (typeof(ATAssoc))];
			// IntPtr pBufferIntPtr = new IntPtr (pBuffer);
			// if !isReturnValue:
			//      StructMarshal.Marshaler.ToSwift (typeof(ATAssoc), pBufferIntPtr);
			// SomePInvoke (pBufferIntPtr);
			// p = StructMarshal.Marshaler.ToNet<ATAssoc>(pBufferIntPtr);
			RequiredUnsafeCode = true;

			use.AddIfNotPresent (typeof (StructMarshal));

			var bufferIdent = new CSIdentifier (Uniqueify (p.Name.Name + "Buffer", identifiersUsed));
			identifiersUsed.Add (bufferIdent.Name);
			var retvalbufferDecl = CSVariableDeclaration.VarLine (
				CSSimpleType.ByteStar, bufferIdent,
				CSArray1D.New (CSSimpleType.Byte, true, new CSFunctionCall ("StructMarshal.Marshaler.Strideof", false, p.CSType.Typeof ())));
			preMarshalCode.Add (retvalbufferDecl);

			var bufferPtrIdent = new CSIdentifier (Uniqueify (p.Name.Name + "BufferPtr", identifiersUsed));
			identifiersUsed.Add (bufferPtrIdent.Name);
			var retvalBufferPtrDecl = CSVariableDeclaration.VarLine (
				CSSimpleType.IntPtr, bufferPtrIdent,
				new CSFunctionCall ("IntPtr", true, bufferIdent));
			preMarshalCode.Add (retvalBufferPtrDecl);

			if (!isReturnValue) {
				preMarshalCode.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
					p.CSType.Typeof (), p.Name, bufferPtrIdent));
			}
			var toNetCall = new CSFunctionCall (String.Format ("StructMarshal.Marshaler.ToNet<{0}>", p.CSType.ToString ()),
				  false, bufferPtrIdent, CSConstant.Val (true));
			postMarshalCode.Add (CSAssignment.Assign (p.Name, toNetCall));
			return bufferPtrIdent;
		}

		CSBaseExpression MarshalGenericReferenceAsPointer (BaseDeclaration typeContext, FunctionDeclaration funcDecl, CSParameter p, NamedTypeSpec pointsTo, bool isReturnValue)
		{
			if (IsProtocolConstrained (funcDecl, pointsTo)) {
				return null;
 			} else {
				//
				// unsafe {
				//    byte *pBuffer = stackalloc byte[StructMarshal.Marshaler.Strideof(typeof(T))];
				//    IntPtr retvalPtr = new IntPtr(retval);
				//    bool typeIsISwiftObject = p is ISwiftObject;
				// !isReturnValue
				//    if (typeIsISwiftObject) {
				//        Marshal.WriteIntPtr(retvalPtr, ((ISwiftObject)p).SwiftObject);
				//    }
				//    else {
				//       StructMarshal.Marshaler.ToSwift(typeof(T), retvalPtr);
				//    }
				//    SomePInvoke(retvalPtr);
				//    p = typeIsISwiftObject ? SwiftObjectRegistry.CSObjectForSwiftObject<T>(Marshal.ReadIntPtr(retvalPtr)) :
				//          StructMarshal.Marshaler.ToNet<T>(retvalPtr);

				RequiredUnsafeCode = true;

				use.AddIfNotPresent (typeof (StructMarshal));

				var bufferIdent = new CSIdentifier (Uniqueify (p.Name.Name + "Buffer", identifiersUsed));
				identifiersUsed.Add (bufferIdent.Name);
				var retvalbufferDecl = CSVariableDeclaration.VarLine (
					CSSimpleType.ByteStar, bufferIdent,
					CSArray1D.New (CSSimpleType.Byte, true, new CSFunctionCall ("StructMarshal.Marshaler.Strideof", false, p.CSType.Typeof ())));
				preMarshalCode.Add (retvalbufferDecl);

				var bufferPtrIdent = new CSIdentifier (Uniqueify (p.Name.Name + "BufferPtr", identifiersUsed));
				identifiersUsed.Add (bufferPtrIdent.Name);
				var retvalBufferPtrDecl = CSVariableDeclaration.VarLine (
					CSSimpleType.IntPtr, bufferPtrIdent,
					new CSFunctionCall ("IntPtr", true, bufferIdent));
				preMarshalCode.Add (retvalBufferPtrDecl);

				var typeIsSwiftObjectIdent = new CSIdentifier (Uniqueify (p.Name.Name + "IsISwiftObject", identifiersUsed));
				identifiersUsed.Add (typeIsSwiftObjectIdent.Name);

				var typeIsSwiftObjectDecl = CSVariableDeclaration.VarLine (
					CSSimpleType.Bool, typeIsSwiftObjectIdent, new CSFunctionCall (
						"(typeof(ISwiftObject)).IsAssignableFrom", false, p.CSType.Typeof ()));

				preMarshalCode.Add (typeIsSwiftObjectDecl);

				if (!isReturnValue) {
					use.AddIfNotPresent (typeof (System.Runtime.InteropServices.Marshal));
					CSCodeBlock ifClause = new CSCodeBlock ();
					CSCodeBlock elseClause = new CSCodeBlock ();
					ifClause.Add (CSFunctionCall.FunctionCallLine ("Marshal.WriteIntPtr",
										     false,
										     bufferPtrIdent,
										     new CSParenthesisExpression (new CSCastExpression (new CSSimpleType (typeof (ISwiftObject)), p.Name)).Dot (new CSIdentifier ("SwiftObject"))));
					elseClause.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift",
										       false,
										       p.CSType.Typeof (), p.Name, bufferPtrIdent));
					CSIfElse ifelse = new CSIfElse (typeIsSwiftObjectIdent, ifClause, elseClause);
					preMarshalCode.Add (new CSLine (ifelse, false));
				}

				use.AddIfNotPresent (typeof (SwiftObjectRegistry));
				var toNetCall = new CSFunctionCall (String.Format ("StructMarshal.Marshaler.ToNet<{0}>", p.CSType.ToString ()),
								  false, bufferPtrIdent, CSConstant.Val (true));

				postMarshalCode.Add (CSAssignment.Assign (p.Name, toNetCall));

				return bufferPtrIdent;
			}
		}

		CSBaseExpression MarshalTupleAsPointer (BaseDeclaration typeContext, FunctionDeclaration funcDecl, CSParameter p, TupleTypeSpec pointsTo, bool marshalProtocolAsValueType, bool isReturnValue)
		{
			if (pointsTo.Elements.Count == 1) {
				return MarshalAsPointer (typeContext, funcDecl, p, pointsTo.Elements [0], marshalProtocolAsValueType, isReturnValue);
			} else {
				//
				// unsafe {
				// SwiftTupleMap map = StructMarshal.Marshaler.BuildTupleMap(new Type[] { t1, t2, t3, t4 });
				// byte *tuplePtr = stackalloc byte[map.Stride];
				// IntPtr tupleIntPtr = new IntPtr(tuplePtr)
				// StructMarshal.Marshaler.ToSwiftTuple(typeof(pType), pName, tupleIntPtr, map)
				// ...
				// SomePInvoke(... tupleIntPtr ...)
				// StructMarshal.Marshaler.NominalDestroy (typeof (pType), tupleIntPtr);
				// only injected if the argument is by reference
				// p.Name = StructMarshal.Marshaler.ToNetTuple<t1, t2, t3, t4>(tupleIntPtr, map);
				// }

				var tupleContents = pointsTo.Elements.Select (ts => typeMapper.MapType (funcDecl, ts, false)).ToList ();
				var tupleContentTypeNames = tupleContents.Select (ntb => {
					use.AddIfNotPresent (ntb.NameSpace);
					return (CSBaseExpression)ntb.ToCSType (use).Typeof ();
				}).ToArray ();

				var declaredTupleType = NetTypeBundle.ToCSTuple (tupleContents, use);

				use.AddIfNotPresent (typeof (StructMarshal));
				RequiredUnsafeCode = true;

				var typeList = new CSArray1DInitialized ("Type", tupleContentTypeNames);

				var tupleMap = new CSIdentifier (Uniqueify (p.Name + "Map", identifiersUsed));
				identifiersUsed.Add (tupleMap.Name);

				use.AddIfNotPresent (typeof (SwiftTupleMap));
				use.AddIfNotPresent (typeof (StructMarshal));
				var tupleMapDecl = CSVariableDeclaration.VarLine (new CSSimpleType (typeof (SwiftTupleMap)), tupleMap,
							new CSFunctionCall ("SwiftTupleMap.FromTypes", false, typeList));
				preMarshalCode.Add (tupleMapDecl);


				var pPtr = new CSIdentifier (Uniqueify (p.Name.Name + "TuplePtr", identifiersUsed));
				identifiersUsed.Add (pPtr.Name);
				var pPtrDecl = CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, pPtr,
									    CSArray1D.New (CSSimpleType.Byte, true, tupleMap.Dot (new CSIdentifier ("Stride"))));
				preMarshalCode.Add (pPtrDecl);

				if (!MarshalingConstructor) {
					var releaseNominal = CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.NominalDestroy", false, p.CSType.Typeof (), pPtr);
					postMarshalCode.Add (releaseNominal);
				}

				if (p.ParameterKind == CSParameterKind.Out || p.ParameterKind == CSParameterKind.Ref || isReturnValue) {
					var reMarhsal = CSAssignment.Assign (p.Name, new CSCastExpression (declaredTupleType,
												 new CSFunctionCall ("StructMarshal.Marshaler.MarshalTupleToNet", false,
														   new CSFunctionCall ("IntPtr", true, pPtr),
														   tupleMap, CSConstant.Val (true))));
					postMarshalCode.Add (reMarhsal);
				}

				if (isReturnValue) {
					return new CSFunctionCall ("IntPtr", true, pPtr);
				} else {
					return new CSFunctionCall ("StructMarshal.Marshaler.MarshalTupleToSwift", false,
					    declaredTupleType.Typeof (), tupleMap, p.Name,
						new CSFunctionCall ("IntPtr", true, pPtr));
				}
			}
		}

		CSBaseExpression MarshalDynamicSelf (BaseDeclaration typeContext, FunctionDeclaration funcDecl, CSParameter p,
			TypeSpec swiftType, bool marshalProtocolAsValueType, bool isReturnVariable)
		{
			// FIXME - see issue 363 https://github.com/xamarin/binding-tools-for-swift/issues/363
			return CSConstant.IntPtrZero;
		}


		bool IsClassConstrained (FunctionDeclaration funcDecl, NamedTypeSpec swiftType)
		{
			var depthIndex = funcDecl.GetGenericDepthAndIndex (swiftType);
			if (depthIndex.Item1 != 0)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 24, $"Depth of generic reference in wrapper function should always be 0, but is {depthIndex.Item1}");
			var arg = funcDecl.Generics [depthIndex.Item2];
			return arg.IsClassConstrained (typeMapper);
		}


		bool IsProtocolConstrained (FunctionDeclaration funcDecl, NamedTypeSpec swiftType)
		{
			var depthIndex = funcDecl.GetGenericDepthAndIndex (swiftType);
			if (depthIndex.Item1 != 0)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 25, $"Depth of generic reference in wrapper function should always be 0, but is {depthIndex.Item1}");
			var arg = funcDecl.Generics [depthIndex.Item2];
			return arg.IsProtocolConstrained (typeMapper);
		}

		bool IsAssociatedTypeProtocolConstrained (FunctionDeclaration funcDecl, NamedTypeSpec swiftType)
		{
			if (swiftType.Name.IndexOf ('.') > 0)
				return false;
			var depthIndex = funcDecl.GetGenericDepthAndIndex (swiftType);
			if (depthIndex.Item1 != 0)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 25, $"Depth of generic reference in wrapper function should always be 0, but is {depthIndex.Item1}");
			var arg = funcDecl.Generics [depthIndex.Item2];
			return arg.IsAssociatedTypeProtocolConstrained (typeMapper);
		}

		CSBaseExpression MarshalProtocolConstrained (BaseDeclaration typeContext, FunctionDeclaration wrapperFunc, CSParameter p, NamedTypeSpec swiftType)
		{
			if (IsAssociatedTypeProtocolConstrained (wrapperFunc, swiftType))
				return MarshalAssociatedTypeProtocolConstrained (typeContext, wrapperFunc, p, swiftType);

			var isAssocTypePath = typeContext.IsProtocolWithAssociatedTypesFullPath (swiftType, typeMapper);
			// bool isSwiftable = StructMarshal.Marshaler.IsSwiftRepresentable(typeof(p.type));
			// ISwiftObject pProxyObj = null;
			// IntPtr pNameIntPtr;
			// if (isSwiftable) {
			// 		byte *pNamePtr = stackalloc byte[StructMarshal.Marshaler.Strideof(typeof(p.type))];
			// 		pNameIntPtr = new IntPtr(pNamePtr)
			// 		StructMarshal.Marshaler.ToSwift(pName, pNamePtr);
			// }
			// else {
			//      if (SwiftProtocolTypeAttribute.IsAssociatedType (typeof (ProtConstr)) {
			//          var everyProtocol = SwiftObjectRegistry.Registry.EveryProtocolForInterface ((ProtConstr)p);
			//          byte *pPtr = stack alloc [IntPtr.Size];
			//          pNameIntPtr = new IntPtr (pPtr);
			//          StructMarshal.Marshaler.ToSwift (pProxyObj, pNameIntPtr);
			//      }
			//      else {
			//          Type[] protocolConstraints = new Type[] { new Type[] { typeof(ProtConstr1)... } }
			//  	    SwiftProtocol p = SwiftObjectRegistry.Registry.ProtocolForObject((object)pName, protocolConstraints);
			//          byte *pProtocol = stackalloc byte[StructMarshal.Marshaler.Strideof(typeof(p.type), protocolConstraints);
			//          pNameIntPtr = new IntPtr(pProtocol);
			//  	    StructMarhsal.Marshaler.ToSwift(p, pProtocol);
			//      }
			// }
			// ..
			// if (isSwiftable && !((typeof(p.type) is ISwiftObject)) {
			//    	StructMarshal.Marshaler.ToNet(pNamePtr, ref pName);
			// }
			// ...
			// if (isSwiftable) {
			//	StructMarshal.Marshaler.ReleaseSwiftPtr(typeof (p.type), pNamePtr);
			// } else {
			//	if (pProxyObj != null) {
			//		StructMarshal.Marshaler.ReleaseSwiftObject (pProxyObj);
			//	} else { // not sure what to do here yet. Maybe nothing?
			//	}
			// }
			RequiredUnsafeCode = true;
			use.AddIfNotPresent (typeof (StructMarshal));
			var pNameIntPtr = new CSIdentifier (Uniqueify (p.Name.Name + "IntPtr", identifiersUsed));
			identifiersUsed.Add (pNameIntPtr.Name);
			var pNameAssocProxy = new CSIdentifier (Uniqueify (p.Name.Name + "Proxy", identifiersUsed));
			identifiersUsed.Add (pNameAssocProxy.Name);
			var pNameIsSwiftable = new CSIdentifier (Uniqueify (p.Name.Name + "IsSwiftable", identifiersUsed));
			identifiersUsed.Add (pNameIsSwiftable.Name);
			preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, pNameIntPtr));
			preMarshalCode.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("ISwiftObject"), pNameAssocProxy, CSConstant.Null));
			preMarshalCode.Add (CSVariableDeclaration.VarLine (
			    CSSimpleType.Bool, pNameIsSwiftable, new CSFunctionCall (
				"StructMarshal.Marshaler.IsSwiftRepresentable", false, p.CSType.Typeof ())));


			var ifClause = new CSCodeBlock ();
			var pNamePtr = new CSIdentifier (Uniqueify (p.Name.Name + "Ptr", identifiersUsed));
			identifiersUsed.Add (pNamePtr.Name);
			ifClause.Add (CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, pNamePtr,
				CSArray1D.New (CSSimpleType.Byte, true, new CSFunctionCall ("StructMarshal.Marshaler.Strideof", false,
											p.CSType.Typeof ()))));
			ifClause.Add (CSAssignment.Assign (pNameIntPtr, new CSFunctionCall ("IntPtr", true, pNamePtr)));
			ifClause.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false, p.Name, pNameIntPtr));



			var protoElseClause = new CSCodeBlock ();
			// else {
			//      var container = SwiftObjectRegistry.Registry.ExistentialContainerForProtocol ((object)pName, protocolConstraint);
			//      byte *pProtocol = stackalloc byte[p.SizeOf];
			//      pNameIntPtr = new IntPtr(pProtocol);
			//      container.CopyTo (pNameIntPtr);
			// }
			// else {
			//      Type[] protocolConstraints = new Type[] { new Type[] { typeof(ProtConstr1)... } }
			//  	SwiftProtocol p = SwiftObjectRegistry.Registry.SwiftProtocolForInterfaces((object)pName, protocolConstraints);
			//      byte *pProtocol = stackalloc byte[p.SizeOf];
			//      pNameIntPtr = new IntPtr(pProtocol);
			//  	StructMarhsal.Marshaler.ToSwift(p, pProtocol);
			// }
			var protoConstraints = new CSIdentifier (Uniqueify (p.Name.Name + "ProtocolConstraints", identifiersUsed));
			identifiersUsed.Add (protoConstraints.Name);

			var constraints = new List<CSBaseExpression> ();
			var depthIndex = wrapperFunc.GetGenericDepthAndIndex (swiftType);
			if (!isAssocTypePath) {
				constraints.AddRange (wrapperFunc.Generics [depthIndex.Item2].Constraints.OfType<InheritanceConstraint> ().Select (inheritanceConstraint => {
					Tuple<int, int> selfDepthIndex = null;
					if (IsProtocolWithSelfInArguments (inheritanceConstraint.InheritsTypeSpec)) {
						selfDepthIndex = depthIndex;
					}
					var constraintEntity = typeMapper.GetEntityForTypeSpec (inheritanceConstraint.InheritsTypeSpec);

					var ntb = typeMapper.MapType (wrapperFunc, inheritanceConstraint.InheritsTypeSpec, false,
						selfDepthIndex: selfDepthIndex);
					if (ntb == null)
						throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 33, $"Unable to find C# type for protocol constraint type {inheritanceConstraint.Inherits}");
					use.AddIfNotPresent (ntb.NameSpace);
					return (CSBaseExpression)ntb.ToCSType (use).Typeof ();
				}));
			}

			var protoInit = new CSArray1DInitialized ("Type", constraints);
			protoElseClause.Add (CSVariableDeclaration.VarLine (new CSSimpleType ("Type", true), protoConstraints, protoInit));
			var protoName = new CSIdentifier (Uniqueify (p.Name.Name + "Container", identifiersUsed));
			identifiersUsed.Add (protoName.Name);
			protoElseClause.Add (CSVariableDeclaration.VarLine (CSSimpleType.Var, protoName,
									    new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocols",
												false, p.Name, protoConstraints)));
			var protoStar = new CSIdentifier (Uniqueify (p.Name.Name + "ProtoPtr", identifiersUsed));
			identifiersUsed.Add (protoStar.Name);
			protoElseClause.Add (CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, protoStar,
									    CSArray1D.New (CSSimpleType.Byte, true, protoName.Dot (new CSIdentifier ("SizeOf")))));
			protoElseClause.Add (CSAssignment.Assign (pNameIntPtr, new CSFunctionCall ("IntPtr", true, protoStar)));
			protoElseClause.Add (CSFunctionCall.FunctionCallLine ($"{protoName.Name}.CopyTo", false,  pNameIntPtr));

			CSCodeBlock assocElseClause = null;
			CSCodeBlock assocPostElseClause = null;
			if (!isAssocTypePath && (wrapperFunc.Generics [depthIndex.Item2].Constraints.Count == 1
				&& wrapperFunc.Generics [depthIndex.Item2].Constraints [0] is InheritanceConstraint constraint)) {

				var selfDepthIndex = IsProtocolWithSelfInArguments (constraint.InheritsTypeSpec) ? depthIndex : null;

				var constraintNTB = typeMapper.MapType (wrapperFunc, constraint.InheritsTypeSpec as NamedTypeSpec, false, selfDepthIndex: selfDepthIndex);
				use.AddIfNotPresent (constraintNTB.NameSpace);
				var constraintType = constraintNTB.ToCSType (use);
				var assocIfClause = new CSCodeBlock ();

				var makeProxyDecl = CSAssignment.Assign (pNameAssocProxy, new CSFunctionCall ("SwiftObjectRegistry.Registry.EveryProtocolForInterface", false, new CSCastExpression (constraintType, p.Name)));
				assocIfClause.Add (makeProxyDecl);

				var argPtrName = new CSIdentifier (Uniqueify (p.Name.Name + "Ptr", identifiersUsed));
				identifiersUsed.Add (argPtrName.Name);
				var makeStackMem = CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, argPtrName,
										  CSArray1D.New (CSSimpleType.Byte, true, new CSIdentifier ("IntPtr.Size")));
				assocIfClause.Add (makeStackMem);
				assocIfClause.Add (CSAssignment.Assign (pNameIntPtr, new CSFunctionCall ("IntPtr", true, argPtrName)));
				use.AddIfNotPresent (typeof (System.Runtime.InteropServices.Marshal));
				assocIfClause.Add (CSFunctionCall.FunctionCallLine ("Marshal.WriteIntPtr", false, pNameIntPtr, new CSFunctionCall ("SwiftCore.Retain", false, pNameAssocProxy.Dot (NewClassCompiler.kSwiftObjectGetter))));

				var assocIfElse = new CSIfElse (new CSFunctionCall ("SwiftProtocolTypeAttribute.IsAssociatedTypeProxy", false, constraintType.Typeof ()), assocIfClause, protoElseClause);
				assocElseClause = CSCodeBlock.Create (assocIfElse);

				var assocPostIfClause = CSCodeBlock.Create (CSFunctionCall.FunctionCallLine ("StructMarshal.ReleaseSwiftObject", false, pNameAssocProxy));
				var assocPostIfElse = new CSIfElse (pNameAssocProxy != CSConstant.Null, assocPostIfClause);

				assocPostElseClause = CSCodeBlock.Create (assocPostIfElse);
			}

			var postIfClause = CSCodeBlock.Create (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ReleaseSwiftPointer", false, p.CSType.Typeof (), pNameIntPtr));
			var postMarshalCheck = new CSLine (new CSIfElse (pNameIsSwiftable, postIfClause, assocPostElseClause), false);

			preMarshalCode.Add (new CSLine (new CSIfElse (pNameIsSwiftable, ifClause, assocElseClause ?? protoElseClause), false));

			if (!MarshalingConstructor)
				postMarshalCode.Add (postMarshalCheck);
			return pNameIntPtr;

		}

		bool IsProtocolWithSelfInArguments (TypeSpec type)
		{
			if (type == null)
				return false;
			var entity = typeMapper.GetEntityForTypeSpec (type);
			if (entity == null || entity.EntityType != EntityType.Protocol)
				return false;
			var protocol = (ProtocolDeclaration)entity.Type;
			return protocol.HasDynamicSelfInArguments;
		}

		CSBaseExpression MarshalAssociatedTypeProtocolConstrained (BaseDeclaration typeContext, FunctionDeclaration wrapperFunc, CSParameter p, NamedTypeSpec swiftType)
		{
			// var proxy = p is ProxyType ? (InterfaceType)p : SwiftProtocolTypeAttribute.MakeProxy (typeof (proxyType), (InterfaceType)p);
			// var proxyRefCount = StructMarshal.RetainCount ((ISwiftObject)proxy);
			// var proxyPtr = stackalloc byte [IntPtr.Size];
			// var proxyIntPtr = new IntPtr (proxyPtr);
			// StructMarshal.Marshaler.ToSwift (proxy, proxyIntPtr);
			// ...
			// if (p is ProxyType && proxyRefCount >= StructMarshal.RetainCount ((ISwiftObject)proxy)) {
			//    ((ISwiftObject)proxy).Dispose ();
			// }

			var protocolConstraint = ProtocolConstraintFromFunctionDeclaration (swiftType, wrapperFunc);
			Tuple<int, int> selfDepthIndex = null;
			if (protocolConstraint.HasDynamicSelfInArguments) {
				selfDepthIndex = wrapperFunc.GetGenericDepthAndIndex (swiftType);
				if (selfDepthIndex.Item1 < 0 || selfDepthIndex.Item2 < 0)
					selfDepthIndex = null;
			}
			var csInterfaceTypeNTB = typeMapper.MapType (wrapperFunc, new NamedTypeSpec (protocolConstraint.ToFullyQualifiedName ()), false,
				selfDepthIndex: selfDepthIndex);
			var csInterfaceType = csInterfaceTypeNTB.ToCSType (use) as CSSimpleType;
			var csProxyType = BuildGenericInterfaceFromAssociatedTypes (csInterfaceType, protocolConstraint);

			RequiredUnsafeCode = true;
			var proxyID = new CSIdentifier (Uniqueify ("proxy", identifiersUsed));
			identifiersUsed.Add (proxyID.Name);
			var proxyRefCountID = new CSIdentifier (Uniqueify ("proxyRefCount", identifiersUsed));
			identifiersUsed.Add (proxyRefCountID.Name);
			var proxyPtrID = new CSIdentifier (Uniqueify ("proxyPtr", identifiersUsed));
			identifiersUsed.Add (proxyPtrID.Name);
			var proxyIntPtrID = new CSIdentifier (Uniqueify ("proxyIntPtr", identifiersUsed));
			identifiersUsed.Add (proxyIntPtrID.Name);

			// proxy declaration
			var isProxyTypeTest = new CSBinaryExpression (CSBinaryOperator.Is, p.Name, new CSIdentifier (csProxyType.ToString ()));
			var makeProxy = new CSFunctionCall ("SwiftProtocolTypeAttribute.MakeProxy", false, csProxyType.Typeof (), new CSCastExpression (csInterfaceType, p.Name));
			var proxyExpr = new CSTernary (isProxyTypeTest, new CSCastExpression (csInterfaceType, p.Name), makeProxy, false);
			var proxyDecl = CSVariableDeclaration.VarLine (proxyID, proxyExpr);
			preMarshalCode.Add (proxyDecl);

			var proxyCount = new CSFunctionCall ("StructMarshal.RetainCount", false, new CSCastExpression (new CSSimpleType ("ISwiftObject"), proxyID));
			// proxyRefCount declaration
			var proxyRefCountDecl = CSVariableDeclaration.VarLine (proxyRefCountID, proxyCount);
			preMarshalCode.Add (proxyRefCountDecl);

			// proxyPtr decl
			var proxyPtrDecl = CSVariableDeclaration.VarLine (proxyPtrID, CSArray1D.New (CSSimpleType.Byte, true, new CSIdentifier ("IntPtr.Size")));
			preMarshalCode.Add (proxyPtrDecl);

			// proxyIntPtr decl
			var proxyIntPtrDecl = CSVariableDeclaration.VarLine (proxyIntPtrID, new CSFunctionCall ("IntPtr", true, proxyPtrID));
			preMarshalCode.Add (proxyIntPtrDecl);

			// proxy marshal
			var proxyMarshal = CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false, proxyID, proxyIntPtrID);
			preMarshalCode.Add (proxyMarshal);

			// post marshal cleanup

			var condition = new CSBinaryExpression (CSBinaryOperator.And, isProxyTypeTest,
				new CSBinaryExpression (CSBinaryOperator.GreaterEqual, proxyRefCountID, proxyCount));
			var disposeCall = CSFunctionCall.FunctionCallLine ($"((ISwiftObject){proxyID.Name}).Dispose", false);
			var body = CSCodeBlock.Create (disposeCall);
			var ifPost = new CSLine (new CSIfElse (condition, body), false);
			postMarshalCode.Add (ifPost);


			return proxyIntPtrID;

		}

		CSSimpleType BuildGenericInterfaceFromAssociatedTypes (CSSimpleType interfaceType, ProtocolDeclaration protocol)
		{
			return new CSSimpleType (OverrideBuilder.AssociatedTypeProxyClassName (protocol), false, interfaceType.GenericTypes);
		}

		ProtocolDeclaration ProtocolConstraintFromFunctionDeclaration (NamedTypeSpec swiftType, FunctionDeclaration funcDecl)
		{
			var depthIndex = funcDecl.GetGenericDepthAndIndex (swiftType);
			var genDecl = funcDecl.GetGeneric (depthIndex.Item1, depthIndex.Item2);
			var constraint = genDecl.Constraints [0] as InheritanceConstraint;
			var entity = typeMapper.GetEntityForTypeSpec (constraint.InheritsTypeSpec);
			if (entity == null)
				throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 46, $"Unable to find entity for type {constraint.Inherits}");
			if (entity.EntityType != EntityType.Protocol)
				throw new ArgumentOutOfRangeException (nameof (funcDecl), $"Expected a protocol constraint but got a {entity.EntityType}");
			return entity.Type as ProtocolDeclaration;
		}


		CSBaseExpression MarshalGenericReference (BaseDeclaration typeContext, FunctionDeclaration wrapperFunc, CSParameter p, NamedTypeSpec swiftType)
		{
			if (typeContext.IsProtocolWithAssociatedTypesFullPath (swiftType, typeMapper) || IsProtocolConstrained (wrapperFunc, swiftType)) {
				return MarshalProtocolConstrained (typeContext, wrapperFunc, p, swiftType);
			} else if (typeContext.IsTypeSpecGenericMetatypeReference (swiftType)) {
				return p.Name;
			} else {
				// Class constraints:
				// IntPtr pNameIntPtr
				// bool pNameIsSwiftObject = typeof(p.type) is ISwiftObject;
				// if (pIsSwiftObject) {
				//     pNameIntPtr = StructMarshal.Marshaler.RetainSwiftObject((ISwiftObject)p);
				// }
				// else {
				//     byte *pNamePtr = stackalloc byte[StructMarshal.Marshaler.Strideof(typeof(p.type))]
				//     pNameIntPtr = new IntPtr(pNamePtr);
				//     StructMarshal.Marshaler.ToSwift(pName, pNamePtr);
				// }



				// Swift 4 calling conventions no class constraints
				// IntPtr pNameIntPtr
				// bool pNameIsSwiftObject = typeof(p.type) is ISwiftObject;
				// byte *pNamePtr = stackalloc byte[StructMarshal.Marshaler.Strideof(typeof(p.type))]
				// pNameIntPtr = new IntPtr(pNamePtr);
				// StructMarshal.Marshaler.ToSwift(pName, pNamePtr);
				// ...
				// if (!pIsSwiftObject) {
				//     StructMarshal.Marshaler.ToNet(pNamePtr, ref pName);
				// }

				var isClassConstrained = IsClassConstrained (wrapperFunc, swiftType);
				var pNameIntPtr = new CSIdentifier (Uniqueify (p.Name.Name + "IntPtr", identifiersUsed));
				identifiersUsed.Add (pNameIntPtr.Name);
				var pNameIsSwiftObject = new CSIdentifier (Uniqueify (p.Name.Name + "IsSwiftObject", identifiersUsed));
				identifiersUsed.Add (pNameIsSwiftObject.Name);

				if (isClassConstrained) {
					preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, pNameIntPtr));
					preMarshalCode.Add (CSVariableDeclaration.VarLine (
					    CSSimpleType.Bool, pNameIsSwiftObject, new CSFunctionCall (
							"(typeof(ISwiftObject)).IsAssignableFrom", false, p.CSType.Typeof ())));
					var ifClause = new CSCodeBlock ();
					ifClause.Add (CSAssignment.Assign (pNameIntPtr,
									 new CSFunctionCall ("StructMarshal.RetainSwiftObject", false,
											   new CSCastExpression (new CSSimpleType (typeof (ISwiftObject)), p.Name))));


					var elseClause = new CSCodeBlock ();
					var pNamePtr = new CSIdentifier (Uniqueify (p.Name.Name + "Ptr", identifiersUsed));
					identifiersUsed.Add (pNamePtr.Name);
					elseClause.Add (CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, pNamePtr,
										     CSArray1D.New (CSSimpleType.Byte, true,
												    new CSFunctionCall ("StructMarshal.Marshaler.Strideof", false,
														      p.CSType.Typeof ()))));
					elseClause.Add (CSAssignment.Assign (pNameIntPtr, new CSFunctionCall ("IntPtr", true, pNamePtr)));
					if (!skipThisParameterPremarshal) {
						CSLine toSwift = CSAssignment.Assign (pNameIntPtr,
										  new CSFunctionCall ("StructMarshal.Marshaler.ToSwift", false,
												    p.Name, pNameIntPtr));
						elseClause.Add (toSwift);
					}

					var intPtrIf = new CSIfElse (pNameIsSwiftObject, ifClause, elseClause);
					preMarshalCode.Add (new CSLine (intPtrIf, false));


					var postIfClause = new CSCodeBlock ();
					postIfClause.Add (CSFunctionCall.FunctionCallLine ("SwiftCore.Release", false, pNameIntPtr));

					var postElseClause = new CSCodeBlock ();
					postElseClause.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ReleaseSwiftPointer", false, p.CSType.Typeof (), pNameIntPtr));

					var postIf = new CSIfElse (pNameIsSwiftObject, postIfClause, postElseClause);
					if (!MarshalingConstructor)
						postMarshalCode.Add (new CSLine (postIf, false));
				} else {
					preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, pNameIntPtr));
					preMarshalCode.Add (CSVariableDeclaration.VarLine (
					    CSSimpleType.Bool, pNameIsSwiftObject, new CSFunctionCall (
							"(typeof(ISwiftObject)).IsAssignableFrom", false, p.CSType.Typeof ())));

					var pNamePtr = new CSIdentifier (Uniqueify (p.Name.Name + "Ptr", identifiersUsed));
					identifiersUsed.Add (pNamePtr.Name);
					preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.ByteStar, pNamePtr,
										     CSArray1D.New (CSSimpleType.Byte, true,
												    new CSFunctionCall ("StructMarshal.Marshaler.Strideof", false,
														      p.CSType.Typeof ()))));
					preMarshalCode.Add (CSAssignment.Assign (pNameIntPtr, new CSFunctionCall ("IntPtr", true, pNamePtr)));
					if (!skipThisParameterPremarshal) {
						CSLine toSwift = CSAssignment.Assign (pNameIntPtr,
										  new CSFunctionCall ("StructMarshal.Marshaler.ToSwift", false,
												    p.Name, pNameIntPtr));
						preMarshalCode.Add (toSwift);
					}
		    			if (!MarshalingConstructor)
						postMarshalCode.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ReleaseSwiftPointer", false, p.CSType.Typeof (), pNameIntPtr));
				}


				var toNet = new CSIfElse (new CSUnaryExpression (CSUnaryOperator.Not, pNameIsSwiftObject), new CSCodeBlock (), null);
				toNet.IfClause.Add (CSAssignment.Assign (p.Name, new CSCastExpression (p.CSType,
											     new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false,
													       pNameIntPtr, p.CSType.Typeof (), CSConstant.Val (false)))));

				postMarshalCode.Add (new CSLine (toNet, false));

				RequiredUnsafeCode = true;
				return pNameIntPtr;
			}

		}


		CSBaseExpression MarshalTrivialEnum (CSParameter p, NamedTypeSpec st)
		{
			var entity = typeMapper.GetEntityForTypeSpec (st);
			var decl = entity.Type as EnumDeclaration;

			bool isSigned = decl.RawTypeName == "Swift.Int";
			if (decl.HasRawType) {
				if (decl.RawTypeName == "Swift.Int" || decl.RawTypeName == "Swift.UInt") {
					return new CSCastExpression (isSigned ? "nint" : "nuint", new CSCastExpression (isSigned ? "long" : "ulong",
					    p.Name));
				} else {
					return p.Name;
				}
			} else {
				return new CSCastExpression ("nint", new CSCastExpression ("long", p.Name));
			}
		}

		CSBaseExpression MarshalStruct (CSParameter p, NamedTypeSpec st)
		{
			var entity = typeMapper.GetEntityForTypeSpec (st);
			if (entity != null && entity.IsObjCStruct) {
				return new CSIdentifier ($"ref {p.Name}");
			}
			return MarshalNominal (p);
		}

		CSBaseExpression MarshalNominal (CSParameter p)
		{
			// fixed (byte *nameSwiftDataPtr = StructMarshal.Marshaler.PrepareValueType(name)) {
			// ...
			// someCallToAPinvoke(new IntPtr(nameSwiftDataPtr))
			// ...

			if (p.Name.Name != "this")
				preMarshalCode.Add (ThrowOnNull (p.Name));

			string nomDataPtrName = Uniqueify (String.Format ("{0}SwiftDataPtr", p.Name.Name), identifiersUsed);
			identifiersUsed.Add (nomDataPtrName);
			var enumDataPtr = new CSIdentifier (nomDataPtrName);

			RequiredUnsafeCode = true;
			use.AddIfNotPresent (typeof (StructMarshal));
			var fixedBlock = new CSFixedCodeBlock (CSSimpleType.ByteStar, enumDataPtr,
							       new CSFunctionCall ("StructMarshal.Marshaler.PrepareValueType", false, p.Name), null);
			fixedChain.Add (fixedBlock);
			return new CSCastExpression ("IntPtr", enumDataPtr);
		}

		CSLine ThrowOnNull (CSIdentifier parameterName)
		{
			// code to generate:
			// Exceptions.ThrowOnNull (parameterName, nameof (parameterName));
			use.AddIfNotPresent (typeof (SwiftRuntimeLibrary.Exceptions));
			var throwOnNull = CSFunctionCall.FunctionCallLine ("Exceptions.ThrowOnNull", parameterName, CSFunctionCall.Nameof (parameterName));
			return throwOnNull;
		}

		CSBaseExpression MarshalClass (CSParameter p, NamedTypeSpec cl)
		{

			// new for Swift 5 -
			// ARC works like this:
			// 1. stack alloc an access buffer
			// 2. call swift_beginAccess (objPtr, &valueBuffer, 0x20, IntPtr.Zero);
			// 3. retain (objPtr)
			// 4. class swift_endAccess (&valueBuffer)
			// 5. use class in call
			// 6. release***

			// ***Except in a Constructor (thanks swift, you're the best)

			// in here we have 2 cases - either the class is a normal argument or it is passed by reference.
			// In the first case, nothing else happens.
			// In the second case, we reassign the argument.

			// I put all the swift_beginAccess/swift_endAccess code into
			// StructMarshal.
			if (p.Name.Name != "this")
				preMarshalCode.Add (ThrowOnNull (p.Name));

			var fullClassName = cl.Name;
			var backingFieldAccessor = NewClassCompiler.SafeBackingFieldAccessor (p.Name, use, fullClassName, typeMapper);
			string intPtrName = Uniqueify (p.Name + "IntPtr", identifiersUsed);
			identifiersUsed.Add (intPtrName);
			CSIdentifier intPtrIdent = new CSIdentifier (intPtrName);
			CSLine intPtrDecl = CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, intPtrIdent, backingFieldAccessor);
			preMarshalCode.Add (intPtrDecl);

			if (!MarshalingConstructor)
				postMarshalCode.Add (NewClassCompiler.SafeReleaser (intPtrIdent, use, fullClassName, typeMapper));

			if (p.ParameterKind == CSParameterKind.None) {
				return intPtrIdent;
			} else {

				use.AddIfNotPresent (typeof (SwiftAnyObject));
				use.AddIfNotPresent (typeof (SwiftObjectRegistry));
				var call = NewClassCompiler.SafeMarshalClassFromIntPtr (intPtrIdent, p.CSType, use, fullClassName, typeMapper, false);

				CSLine assignLine = CSAssignment.Assign (p.Name, CSAssignmentOperator.Assign, call);
				postMarshalCode.Add (assignLine);

				return intPtrIdent;
			}
		}


		CSBaseExpression MarshalProtocol (CSParameter p, NamedTypeSpec proto, bool marshalProtocolAsValueType)
		{
			if (IsObjCProtocol (proto.Name)) {
				return MarshalObjCProtocol (p, proto);
			}

			var netInterfaceName = NewClassCompiler.InterfaceNameForProtocol (proto.Name, typeMapper);
			string netWrapperName = NewClassCompiler.CSProxyNameForProtocol (proto.Name, typeMapper);

			if (marshalProtocolAsValueType) {
				string protoName = Uniqueify (p.Name + "Container", identifiersUsed);
				identifiersUsed.Add (protoName);
				CSIdentifier protoIdent = new CSIdentifier (protoName);
				CSLine protoDecl = CSVariableDeclaration.VarLine (new CSSimpleType (typeof (SwiftExistentialContainer1)), protoIdent,
					new CSFunctionCall ("SwiftExistentialContainer1", true, 
				    NewClassCompiler.BackingProxyExistentialContainerAccessor (p)));
				preMarshalCode.Add (protoDecl);

				use.AddIfNotPresent (typeof (SwiftAnyObject));
				use.AddIfNotPresent (typeof (SwiftObjectRegistry));

				return new CSIdentifier (String.Format ("ref {0}", protoName));
			} else {
				var protoMaker = new CSFunctionCall ("SwiftExistentialContainer1", true,
					new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocols", false, p.Name,
					    new CSSimpleType (netInterfaceName).Typeof ()));
				var localProto = new CSIdentifier (Uniqueify (p.Name + "Protocol", identifiersUsed));
				identifiersUsed.Add (localProto.Name);
				preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.Var, localProto, protoMaker));

				CSParameterKind kind = p.ParameterKind == CSParameterKind.None ? CSParameterKind.Ref : p.ParameterKind;
				return ParmName (localProto.Name, kind);
			}

		}

		CSBaseExpression MarshalProtocolList (BaseDeclaration typeContext, CSParameter p, ProtocolListTypeSpec proto, bool marshalProtocolAsValueType)
		{

			// var container = (SwiftExistentialContainerN)SwiftObjectRegistry.Registry.ExistentialContainerForProtocols (p, types);
			// callSite (ref container);

			var containerType = new CSSimpleType ($"SwiftExistentialContainer{proto.Protocols.Count}");
			var csProtoTypes = proto.Protocols.Keys.Select (ns => (CSBaseExpression)typeMapper.MapType (typeContext, ns, false).ToCSType (use).Typeof ());
			var typeExpr = new CSArray1DInitialized (CSSimpleType.Type.ToString (), csProtoTypes);
			var localProto = new CSIdentifier (Uniqueify (p.Name + "Protocol", identifiersUsed));
			var protoDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, localProto, new CSCastExpression (containerType,
				new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocols", false, p.Name, typeExpr)));
			preMarshalCode.Add (protoDecl);
			CSParameterKind kind = p.ParameterKind == CSParameterKind.None ? CSParameterKind.Ref : p.ParameterKind;
			return ParmName (localProto.Name, kind);
		}

		CSBaseExpression MarshalObjCProtocol (CSParameter p, NamedTypeSpec proto)
		{
			var handleExpr = NewClassCompiler.SafeHandleAccessor (p.Name);
			if (proto.IsInOut || p.ParameterKind == CSParameterKind.Out || p.ParameterKind == CSParameterKind.Ref) {
				// var localHandle = handleExpr
				// call ( out localHandle )
				// p = ObjCRuntime.Runtime.GetINativeObject<p.CSType>(localHandle, false)
				var localHandle = new CSIdentifier (Uniqueify (p.Name + "Handle", identifiersUsed));
				identifiersUsed.Add (localHandle.Name);
				preMarshalCode.Add (CSVariableDeclaration.VarLine (CSSimpleType.IntPtr, localHandle, handleExpr));
				var accessCallName = $"ObjCRuntime.Runtime.GetINativeObject<{p.CSType.ToString ()}>";
				var accessCall = new CSFunctionCall (accessCallName, false, localHandle, CSConstant.Val (false));
				postMarshalCode.Add (CSAssignment.Assign (p.Name, accessCall));
				return ParmName (localHandle.Name, p.ParameterKind);
			} else {
				return handleExpr;
			}
		}

		static CSIdentifier ParmName (CSParameter parm)
		{
			return ParmName (parm.Name.Name, parm.ParameterKind);
		}

		static CSIdentifier ParmName (string ident, CSParameterKind parmKind)
		{
			string prefix = "";
			switch (parmKind) {
			case CSParameterKind.Out:
				prefix = "out ";
				break;
			case CSParameterKind.Ref:
				prefix = "ref ";
				break;
			default:
				break;
			}
			return new CSIdentifier (String.Format ("{0}{1}", prefix, ident));
		}

		public static string Uniqueify (string name, IEnumerable<string> names)
		{
			int thisTime = 0;
			var sb = new StringBuilder (name);
			while (names.Contains (sb.ToString ())) {
				sb.Clear ().Append (name).Append (thisTime++);
			}
			return sb.ToString ();
		}

		TypeSpec [] FilterParams (CSParameterList parms, FunctionDeclaration wrapperFunc, bool originalThrows)
		{
			var results = new TypeSpec [parms.Count];
			var parameterList = wrapperFunc.ParameterLists.Last ();
			for (int i=0; i < parms.Count; i++) {
				var currType = parameterList [i].TypeSpec;
				results [i] = currType;
			}
			return results;
		}

		List<CSBaseExpression> BuildDefaultTupleParams (CSType [] types)
		{
			if (types.Length < 8) {
				return types.Select (cst => (CSBaseExpression)cst.Default ()).ToList ();
			} else {
				if (types.Length > 8)
					throw new ArgumentException ("argument types to a tuple should be 8 or fewer", nameof (types));
				List<CSBaseExpression> head = types.Take (7).Select (cst => (CSBaseExpression)cst.Default ()).ToList ();
				var finalTuple = types [7] as CSSimpleType;
				if (finalTuple == null)
					throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 27, "Last argument to a large tuple needs to be a tuple");
				head.Add (new CSFunctionCall (finalTuple.ToString (), true,
							  BuildDefaultTupleParams (finalTuple.GenericTypes).ToArray ()));
				return head;
			}
		}

		bool IsObjCProtocol (string fullClassName)
		{
			if (fullClassName == null)
				return false;
			var entity = typeMapper.TryGetEntityForSwiftClassName (fullClassName);
			if (entity == null)
				return false;
			if (entity.EntityType != EntityType.Protocol)
				return false;
			return entity.IsObjCProtocol;
		}

		List<CSLine> absolutelyMustBeFirst;
		List<CSLine> preMarshalCode;
		CSLine functionCall;
		List<CSLine> postMarshalCode;
		CSLine returnLine;

		public bool MarshalProtocolsDirectly { get; set; }
		public bool RequiredUnsafeCode { get; private set; }
		public bool MarshalingConstructor { get; set; }
		public Func<int, int, string> GenericReferenceNamer { get; set; }
		public CSType ProtocolInterfaceType { get; set; }

	}
}

