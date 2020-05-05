// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo;
using Dynamo.SwiftLang;
using SwiftReflector.TypeMapping;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;

namespace SwiftReflector {
	public class MarshalEngineSwiftToCSharp {
		SLImportModules imports;
		List<string> identifiersUsed;
		TypeMapper typeMapper;
		List<ICodeElement> preMarshalCode = new List<ICodeElement> ();
		List<ICodeElement> postMarshalCode = new List<ICodeElement> ();

		public MarshalEngineSwiftToCSharp (SLImportModules imports, List<string> identifiersUsed, TypeMapper typeMapper)
		{
			this.imports = imports;
			preMarshalCode = new List<ICodeElement> ();
			postMarshalCode = new List<ICodeElement> ();
			this.typeMapper = typeMapper;
			this.identifiersUsed = Ex.ThrowOnNull (identifiersUsed, "identifiersUsed");
		}


		public IEnumerable<ICodeElement> MarshalFunctionCall (FunctionDeclaration func, string vtableName, string vtableElementName)
		{
			preMarshalCode.Clear ();
			postMarshalCode.Clear ();
			ICodeElement returnLine = null;
			var instanceEntity = typeMapper.GetEntityForTypeSpec (func.ParameterLists [0] [0].TypeSpec);
			bool instanceIsProtocol = instanceEntity != null && instanceEntity.EntityType == EntityType.Protocol;
			SLIdentifier returnIdent = null;

			if (func.ParameterLists.Count != 2) {
				throw new ArgumentException (String.Format ("Method {0} is has {1} parameter lists - it should really have two (normal for an instance method).",
					func.ToFullyQualifiedName (true), func.ParameterLists.Count));
			}

			var closureArgs = new List<SLBaseExpr> ();


			// marshal the regular arguments
			for (int i = 0; i < func.ParameterLists [1].Count; i++) {
				var parm = func.ParameterLists [1] [i];
				var privateName = !String.IsNullOrEmpty (parm.PrivateName) ? parm.PrivateName : TypeSpecToSLType.ConjureIdentifier (null, i).Name;
				if (func.IsTypeSpecGeneric (parm)) {
					Tuple<int, int> depthIndex = func.GetGenericDepthAndIndex (parm.TypeSpec);
					closureArgs.Add (MarshalGenericTypeSpec (func, privateName, parm.TypeSpec as NamedTypeSpec, depthIndex.Item1, depthIndex.Item2));
				} else {
					closureArgs.Add (MarshalTypeSpec (func, privateName, parm.TypeSpec));
				}
			}
			string callName = String.Format ("{0}.{1}", vtableName, vtableElementName);
			var callInvocation = new SLPostBang (new SLIdentifier (callName), false);

			if (instanceIsProtocol) {
				string protoName = MarshalEngine.Uniqueify ("selfProto", identifiersUsed);
				identifiersUsed.Add (protoName);
				var selfProtoDecl = new SLDeclaration (false,
					new SLBinding (protoName, new SLIdentifier ("self"), new SLSimpleType (func.ParameterLists [0] [0].TypeName.NameWithoutModule ())),
								      Visibility.None);
				preMarshalCode.Add (new SLLine (selfProtoDecl));
				closureArgs.Insert (0, new SLAddressOf (new SLIdentifier (protoName), false));
			} else {
				// add self as a parameter
				imports.AddIfNotPresent ("XamGlue");
				closureArgs.Insert (0, new SLFunctionCall ("toIntPtr", false, new SLArgument (new SLIdentifier ("value"), new SLIdentifier ("self"), true)));
			}

			string throwReturnName = null;
			SLIdentifier throwReturn = null;
			SLType throwReturnType = null;

			var returnTypeSpec = TypeSpec.IsNullOrEmptyTuple (func.ReturnTypeSpec) ? func.ReturnTypeSpec : func.ReturnTypeSpec.ReplaceName ("Self", SubstituteForSelf);


			throwReturnType = new SLTupleType (
				new SLNameTypePair (SLParameterKind.None, "_", returnTypeSpec == null || returnTypeSpec.IsEmptyTuple ?
				                    SLSimpleType.Void : typeMapper.TypeSpecMapper.MapType (func, imports, returnTypeSpec, true)),
				new SLNameTypePair (SLParameterKind.None, "_", new SLSimpleType ("Swift.Error")),
				new SLNameTypePair (SLParameterKind.None, "_", new SLSimpleType ("Bool")));

			if (func.HasThrows) {
				throwReturnName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
				identifiersUsed.Add (throwReturnName);
				throwReturn = new SLIdentifier (throwReturnName);
				imports.AddIfNotPresent ("XamGlue");
				// FIXME for generics
				var throwReturnDecl = new SLDeclaration (true, new SLBinding (throwReturn,
				                                                                        new SLFunctionCall (String.Format ("UnsafeMutablePointer<{0}>.allocate", throwReturnType.ToString ()),
				                                                                                            false, new SLArgument (new SLIdentifier ("capacity"), SLConstant.Val (1), true))), Visibility.None);
				closureArgs.Insert (0, new SLFunctionCall ("toIntPtr", false, new SLArgument (new SLIdentifier ("value"), throwReturn, true)));
				preMarshalCode.Add (new SLLine (throwReturnDecl));
			}


			if (func.HasThrows) {
				returnLine = new SLLine (new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)));
				string errName = MarshalEngine.Uniqueify ("err", identifiersUsed);
				identifiersUsed.Add (errName);
				var errIdent = new SLIdentifier (errName);
				var errDecl = new SLDeclaration (true, new SLBinding (errIdent, new SLFunctionCall ("getExceptionThrown", false,
				                                                                                    new SLArgument (new SLIdentifier ("retval"), throwReturn, true))), Visibility.None);
				postMarshalCode.Add (new SLLine (errDecl));
				var ifblock = new SLCodeBlock (null);
				SLCodeBlock elseblock = null;

				ifblock.Add (SLFunctionCall.FunctionCallLine ($"{throwReturnName}.deinitialize", new SLArgument (new SLIdentifier ("count"), SLConstant.Val (1), true)));
				ifblock.Add (SLFunctionCall.FunctionCallLine ($"{throwReturnName}.deallocate"));
				ifblock.Add (new SLLine (new SLThrow (new SLPostBang (errIdent, false))));


				if (returnTypeSpec != null && !returnTypeSpec.IsEmptyTuple) {
					elseblock = new SLCodeBlock (null);
					string retvalvalName = MarshalEngine.Uniqueify ("retvalval", identifiersUsed);
					identifiersUsed.Add (retvalvalName);
					SLIdentifier retvalval = new SLIdentifier (retvalvalName);
					string tuplecracker = "getExceptionNotThrown";
					var retvalvaldecl =
						new SLDeclaration (true, new SLBinding (retvalval,
						                                        new SLFunctionCall (tuplecracker, false,
						                                                            new SLArgument (new SLIdentifier ("retval"), throwReturn, true))), Visibility.None);
					elseblock.Add (new SLLine (retvalvaldecl));
					elseblock.Add (SLFunctionCall.FunctionCallLine ($"{throwReturnName}.deallocate"));
					ISLExpr returnExpr = new SLPostBang (retvalval, false);
					elseblock.Add (SLReturn.ReturnLine (returnExpr));
				}
				postMarshalCode.Add (new SLIfElse (new SLBinaryExpr (BinaryOp.NotEqual, errIdent, SLConstant.Nil),
				                                   ifblock, elseblock));
			} else {
				if (returnTypeSpec == null || returnTypeSpec.IsEmptyTuple) {
					// On no return value
					// _vtable.entry!(args)
					//
					returnLine = new SLLine (new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)));
				} else {
					if (TypeSpec.IsBuiltInValueType (returnTypeSpec)) {
						// on simple return types (Int, UInt, Bool, etc)
						// return _vtable.entry!(args)
						//
						var closureCall = new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs));
						if (postMarshalCode.Count == 0) {
							returnLine = SLReturn.ReturnLine (closureCall);
						} else {
							returnIdent = new SLIdentifier (MarshalEngine.Uniqueify ("retval", identifiersUsed));
							identifiersUsed.Add (returnIdent.Name);
							var retvalDecl = new SLDeclaration (true, returnIdent, null, closureCall, Visibility.None);
							returnLine = new SLLine (retvalDecl);
							postMarshalCode.Add (SLReturn.ReturnLine (returnIdent));
						}
					} else {
						if (func.IsTypeSpecGeneric (returnTypeSpec)) {
							imports.AddIfNotPresent ("XamGlue");
							// dealing with a generic here.
							// UnsafeMutablePointer<T> retval = UnsafeMutablePointer<T>.alloc(1)
							// someCall(toIntPtr(retval), ...)
							// T actualRetval = retval.move()
							// retval.dealloc(1)
							// return actualRetval
							returnIdent = new SLIdentifier (MarshalEngine.Uniqueify ("retval", identifiersUsed));
							identifiersUsed.Add (returnIdent.Name);
							Tuple<int, int> depthIndex = func.GetGenericDepthAndIndex (returnTypeSpec);
							var retvalDecl = new SLDeclaration (true, returnIdent, null,
							                                              new SLFunctionCall (String.Format ("UnsafeMutablePointer<{0}>.allocate", SLGenericReferenceType.DefaultNamer (depthIndex.Item1, depthIndex.Item2)),
							                                                                  false, new SLArgument (new SLIdentifier ("capacity"), SLConstant.Val (1), true)),
							                                              Visibility.None);
							preMarshalCode.Add (new SLLine (retvalDecl));
							closureArgs.Insert (0, new SLFunctionCall ("toIntPtr", false, new SLArgument (new SLIdentifier ("value"), returnIdent, true)));
							SLIdentifier actualReturnIdent = new SLIdentifier (MarshalEngine.Uniqueify ("actualRetval", identifiersUsed));
							identifiersUsed.Add (actualReturnIdent.Name);

							returnLine = new SLLine (new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)));

							var actualRetvalDecl = new SLDeclaration (true, actualReturnIdent, null,
							                                                    new SLFunctionCall (String.Format ("{0}.move", returnIdent.Name), false),
							                                                    Visibility.None);
							postMarshalCode.Add (new SLLine (actualRetvalDecl));
							postMarshalCode.Add (SLFunctionCall.FunctionCallLine (String.Format ("{0}.deallocate", returnIdent.Name)));
							postMarshalCode.Add (SLReturn.ReturnLine (actualReturnIdent));

						} else if (NamedSpecIsClass (returnTypeSpec as NamedTypeSpec)) {
							// class (not struct or enum) return type is a pointer
							// if we have no post marshal code:
							// return fromIntPtr(_vtable.entry!(args))
							// if we have post marshal code:
							// let retval:returnType = fromIntPtr(_vtable.entry!(args))
							// ... post marshal code
							// return retval;
							imports.AddIfNotPresent ("XamGlue");
							SLBaseExpr callExpr = new SLFunctionCall ("fromIntPtr", false,
								new SLArgument (new SLIdentifier ("ptr"), new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)), true));
							if (postMarshalCode.Count > 0) {
								string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
								var retDecl = new SLDeclaration (true, retvalName,
															typeMapper.TypeSpecMapper.MapType (func, imports, returnTypeSpec, true), callExpr);
								returnLine = new SLLine (retDecl);
								postMarshalCode.Add (SLReturn.ReturnLine (new SLIdentifier (retvalName)));
							} else {
								returnLine = SLReturn.ReturnLine (callExpr);
							}
						} else {
							var entity = typeMapper.GetEntityForTypeSpec (returnTypeSpec);
							if (returnTypeSpec is NamedTypeSpec && entity == null && !func.ReturnTypeSpec.IsDynamicSelf)
								throw new NotImplementedException ($"Function {func.ToFullyQualifiedName (true)} has an unknown return type {returnTypeSpec.ToString ()}");
							if (entity?.EntityType == EntityType.TrivialEnum) {
								imports.AddIfNotPresent (entity.Type.Module.Name);
								var slSelf = new SLIdentifier ($"{entity.Type.Name}.self");
								SLBaseExpr callExpr = new SLFunctionCall ("unsafeBitCast", false,
									new SLArgument (new SLIdentifier ("_"), new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)), false),
									new SLArgument (new SLIdentifier ("to"), slSelf, true));
								if (postMarshalCode.Count == 0) {
									returnLine = SLReturn.ReturnLine (callExpr);
								} else {
									returnIdent = new SLIdentifier (MarshalEngine.Uniqueify ("retval", identifiersUsed));
									identifiersUsed.Add (returnIdent.Name);
									var retvalDecl = new SLDeclaration (true, returnIdent, null, callExpr, Visibility.None);
									returnLine = new SLLine (retvalDecl);
									postMarshalCode.Add (SLReturn.ReturnLine (returnIdent));
								}
							} else {
								switch (returnTypeSpec.Kind) {
								case TypeSpecKind.Closure:

									// let retval:CT = allocSwiftClosureToFunc_ARGS ()
									// _vtable.entry!(retval, args)
									// let actualReturn = netFuncToSwiftClosure (retval.move())
									// retval.deallocate()
									// return actualReturn

									var ct = returnTypeSpec as ClosureTypeSpec;
									var slct = new SLBoundGenericType ("UnsafeMutablePointer", ToMarshaledClosureType (func, ct));
									var ptrName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
									identifiersUsed.Add (ptrName);

									var ptrAllocCallSite = ct.HasReturn () ? $"allocSwiftClosureToFunc_{ct.ArgumentCount()}" : $"allocSwiftClosureToAction_{ct.ArgumentCount ()}";
									var ptrAllocCall = new SLFunctionCall (ptrAllocCallSite, false);
									var ptrDecl = new SLDeclaration (true, new SLIdentifier (ptrName), slct, ptrAllocCall, Visibility.None);
									preMarshalCode.Add (new SLLine (ptrDecl));
									closureArgs.Insert (0, new SLIdentifier (ptrName));

									returnLine = new SLLine (new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)));

									var actualReturnName = MarshalEngine.Uniqueify ("actualReturn", identifiersUsed);
									identifiersUsed.Add (actualReturnName);
									var convertCallSite = ct.HasReturn () ? "netFuncToSwiftClosure" : "netActionToSwiftClosure";
									var isEmptyClosure = !ct.HasReturn () && !ct.HasArguments ();
									var pointerMove = new SLFunctionCall ($"{ptrName}.move", false);
									var convertCall = isEmptyClosure ? pointerMove : new SLFunctionCall (convertCallSite, false, new SLArgument (new SLIdentifier ("a1"), pointerMove, true));
									var actualDecl = new SLDeclaration (true, actualReturnName, value: convertCall, vis: Visibility.None);
									postMarshalCode.Add (new SLLine (actualDecl));
									postMarshalCode.Add (new SLReturn (new SLIdentifier (actualReturnName)));
									break;
								case TypeSpecKind.ProtocolList:
								case TypeSpecKind.Tuple:
								case TypeSpecKind.Named:
									var namedReturn = returnTypeSpec as NamedTypeSpec;
									// enums and structs can't get returned directly
									// instead they will be inserted at the head of the argument list
									// let retval = UnsafeMutablePointer<StructOrEnumType>.allocate(capacity: 1)
									// _vtable.entry!(retval, args)
									// T actualRetval = retval.move()
									// retval.deallocate()
									// return actualRetval
									string allocCallSite = String.Format ("UnsafeMutablePointer<{0}>.allocate", namedReturn.NameWithoutModule);
									if (namedReturn != null && !namedReturn.IsDynamicSelf)
										imports.AddIfNotPresent (namedReturn.Module);
									string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
									identifiersUsed.Add (retvalName);
									var retDecl = new SLDeclaration (true, retvalName,
													 null, new SLFunctionCall (allocCallSite, false, new SLArgument (new SLIdentifier ("capacity"),
																					 SLConstant.Val (1), true)), Visibility.None);
									preMarshalCode.Add (new SLLine (retDecl));
									closureArgs.Insert (0, new SLIdentifier (retvalName));
									returnLine = new SLLine (new SLNamedClosureCall (callInvocation, new CommaListElementCollection<SLBaseExpr> (closureArgs)));

									SLIdentifier actualReturnIdent = new SLIdentifier (MarshalEngine.Uniqueify ("actualRetval", identifiersUsed));
									identifiersUsed.Add (actualReturnIdent.Name);
									var actualRetvalDecl = new SLDeclaration (true, actualReturnIdent, null,
															    new SLFunctionCall (String.Format ("{0}.move", retvalName), false),
															    Visibility.None);
									postMarshalCode.Add (new SLLine (actualRetvalDecl));
									postMarshalCode.Add (SLFunctionCall.FunctionCallLine (
										String.Format ("{0}.deallocate", retvalName)));
									postMarshalCode.Add (SLReturn.ReturnLine (actualReturnIdent));

									break;
								}
							}
						}
					}
				}
			}

			foreach (ICodeElement line in preMarshalCode)
				yield return line;
			yield return returnLine;
			foreach (ICodeElement line in postMarshalCode)
				yield return line;
		}

		SLBaseExpr MarshalTypeSpec (BaseDeclaration declContext, string name, TypeSpec typeSpec)
		{
			switch (typeSpec.Kind) {
			case TypeSpecKind.Named:
				return MarshalNamedTypeSpec (declContext, name, typeSpec as NamedTypeSpec);
			case TypeSpecKind.Tuple:
				return MarshalTupleTypeSpec (declContext, name, typeSpec as TupleTypeSpec);
			case TypeSpecKind.Closure:
				return MarshalClosureTypeSpec (declContext, name, typeSpec as ClosureTypeSpec);
			case TypeSpecKind.ProtocolList:
				return MarshalProtocolListTypeSpec (declContext, name, typeSpec as ProtocolListTypeSpec);
			default:
				throw new NotImplementedException ();
			}
		}

		SLBaseExpr MarshalGenericTypeSpec (BaseDeclaration declContext, string name, NamedTypeSpec spec, int depth, int index)
		{
			// given Foo(T x)
			// the vtable entry should be something like
			// foo : ((@convention(c)(UnsafeRawPointer)->())
			//
			// UnsafeMutablePointer<T> xPtr = UnsafeMutablePointer<T>.alloc(1);
			// pointerToX.initialize(x)
			// vtable.foo(toIntPtr(pointerToX))
			// pointerToX.deinitialize(1)
			// pointerToX.deallocate()
			imports.AddIfNotPresent ("XamGlue");
			var xPtr = new SLIdentifier (MarshalEngine.Uniqueify (name + "Ptr", identifiersUsed));
			identifiersUsed.Add (xPtr.Name);
			var xPtrDecl = new SLDeclaration (true, xPtr, null,
								   new SLFunctionCall (String.Format ("UnsafeMutablePointer<{0}>.allocate", SLGenericReferenceType.DefaultNamer (depth, index)),
																		  false, new SLArgument (new SLIdentifier ("capacity"), SLConstant.Val (1), true)),
													   Visibility.None);
			var xPtrBinding = new SLLine (xPtrDecl);
			preMarshalCode.Add (xPtrBinding);
			var xPtrInit = SLFunctionCall.FunctionCallLine (xPtr.Name + ".initialize",
			                                                new SLArgument (new SLIdentifier ("to"), new SLIdentifier (name), true));
			preMarshalCode.Add (xPtrInit);

			var xPtrDeinit = SLFunctionCall.FunctionCallLine (xPtr.Name + ".deinitialize",
			                                                  new SLArgument (new SLIdentifier ("count"), SLConstant.Val (1), true));

			var xPtrDalloc = SLFunctionCall.FunctionCallLine (xPtr.Name + ".deallocate");
			postMarshalCode.Add (xPtrDeinit);
			postMarshalCode.Add (xPtrDalloc);

			return new SLFunctionCall ("toIntPtr", false, new SLArgument (new SLIdentifier ("value"), xPtr, true));
		}

		SLBaseExpr MarshalNamedTypeSpec (BaseDeclaration declContext, string name, NamedTypeSpec spec)
		{
			if (typeMapper.GetEntityForTypeSpec (spec) == null)
				throw new NotImplementedException ($"Unknown type {name}:{spec.ToString ()} in context {declContext.ToFullyQualifiedName (true)}");
			bool isClass = NamedSpecIsClass (spec);
			if (isClass || spec.IsInOut) {
				imports.AddIfNotPresent ("XamGlue");
				return new SLFunctionCall ("toIntPtr", false,
							   new SLArgument (new SLIdentifier ("value"), new SLIdentifier (name), true));
			}

			if (TypeSpec.IsBuiltInValueType (spec)) {
				return new SLIdentifier (name);
			}

			// at this point, the value is either an enum or a struct, not passed by reference which we need to copy
			// into a local which we can then pass by reference.

			var bindingName = new SLIdentifier (MarshalEngine.Uniqueify (name, identifiersUsed));
			identifiersUsed.Add (bindingName.Name);
			var decl = new SLDeclaration (false, bindingName, typeMapper.TypeSpecMapper.MapType (declContext, imports, spec, false),
			                              new SLIdentifier (name), Visibility.None, false);
			var varBinding = new SLLine (decl);
			preMarshalCode.Add (varBinding);
			return new SLAddressOf (bindingName, false);
		}

		SLBaseExpr MarshalTupleTypeSpec (BaseDeclaration declContext, string name, TupleTypeSpec tuple)
		{
			var bindingName = new SLIdentifier (MarshalEngine.Uniqueify (name, identifiersUsed));
			var argType = typeMapper.TypeSpecMapper.MapType (declContext, imports, tuple, false);
			var ptrType = new SLBoundGenericType ("UnsafeMutablePointer", argType);
			identifiersUsed.Add (bindingName.Name);
			var decl = new SLDeclaration (true, bindingName, ptrType,
			                              new SLFunctionCall (ptrType + ".allocate", false, new SLArgument (new SLIdentifier ("capacity"), SLConstant.Val (1), true)),
			                              Visibility.None, false);
			var varBinding = new SLLine (decl);
			preMarshalCode.Add (varBinding);
			var initCall = SLFunctionCall.FunctionCallLine (bindingName.Name + ".initialize",
						  new SLArgument (new SLIdentifier ("to"), new SLIdentifier (name), true));
			preMarshalCode.Add (initCall);
			imports.AddIfNotPresent ("XamGlue");
			if (tuple.IsInOut) {
				postMarshalCode.Add (new SLLine (new SLBinding (name, bindingName.Dot (new SLIdentifier ("pointee")))));
			}
			return new SLFunctionCall ("toIntPtr", false, new SLArgument (new SLIdentifier ("value"), bindingName, true));
		}

		SLBaseExpr MarshalClosureTypeSpec (BaseDeclaration declContext, string name, ClosureTypeSpec closure)
		{
			// retuns a value
			// let p = swiftClosureToFunc(name)
			// -- or --
			// no return value
			// let p = swiftClosureToAction<at0, at1, ...>(name)
			// expr will be toIntPtr(p)
			// ...
			// p.deallocate()

			Ex.ThrowOnNull (closure, "closure");
			imports.AddIfNotPresent ("XamGlue");
			var funcToCall = closure.HasReturn () ? "swiftClosureToFunc" : "swiftClosureToAction";
			var localPtr = new SLIdentifier (MarshalEngine.Uniqueify ("ptr", identifiersUsed));
			var ptrAlloc = new SLFunctionCall (funcToCall, false, true,
			                                   new SLArgument (new SLIdentifier ("a1"), new SLIdentifier (name), true));
			var decl = new SLLine(new SLDeclaration (true, localPtr.Name, null, ptrAlloc, Visibility.None));
			preMarshalCode.Add (decl);

			var ptrDealloc = SLFunctionCall.FunctionCallLine ($"{localPtr.Name}.deallocate");
			postMarshalCode.Add (ptrDealloc);
			return new SLFunctionCall ("toIntPtr", false, true,
						   new SLArgument (new SLIdentifier ("value"), localPtr, true));
		}

		SLBaseExpr MarshalProtocolListTypeSpec (BaseDeclaration declContext, string name, ProtocolListTypeSpec protocols)
		{
			// let p = UnsafeMutablePointer<protoType>.allocate (argName)
			// p.initialize(to: argName)
			// exp is toIntPtr(value: p)
			// ...
			// if isInOut:
			// argName = p.pointee
			// always:
			// p.deinitialize ()
			// p.deallocate ()
			var bindingName = new SLIdentifier (MarshalEngine.Uniqueify (name, identifiersUsed));
			var argType = typeMapper.TypeSpecMapper.MapType (declContext, imports, protocols, false);
			var ptrType = new SLBoundGenericType ("UnsafeMutablePointer", argType);
			identifiersUsed.Add (bindingName.Name);
			var decl = new SLDeclaration (true, bindingName, ptrType,
				new SLFunctionCall (ptrType + ".allocate", false, new SLArgument (new SLIdentifier ("capacity"), SLConstant.Val (1), true)),
				Visibility.None, false);
			var varBinding = new SLLine (decl);
			preMarshalCode.Add (varBinding);
			var initCall = SLFunctionCall.FunctionCallLine (bindingName.Name + ".initialize",
				new SLArgument (new SLIdentifier ("to"), new SLIdentifier (name), true));
			preMarshalCode.Add (initCall);
			imports.AddIfNotPresent ("XamGlue");
			if (protocols.IsInOut) {
				postMarshalCode.Add (new SLLine (new SLBinding (name, bindingName.Dot (new SLIdentifier ("pointee")))));
			}
			postMarshalCode.Add (SLFunctionCall.FunctionCallLine ($"{bindingName.Name}.deinitialize", new SLArgument (new SLIdentifier ("count"), SLConstant.Val (1), true)));
			postMarshalCode.Add (SLFunctionCall.FunctionCallLine ($"{bindingName.Name}.deallocate"));
			return new SLFunctionCall ("toIntPtr", false, new SLArgument (new SLIdentifier ("value"), bindingName, true));
		}

		bool NamedSpecIsClass (NamedTypeSpec spec)
		{
			return spec != null && !spec.IsDynamicSelf && typeMapper.GetEntityTypeForSwiftClassName (spec.Name) == EntityType.Class;
		}

		SLFuncType ToMarshaledClosureType (BaseDeclaration declContext, ClosureTypeSpec closure)
		{
			var newFunc = new SLFuncType (new SLTupleType (), new List<SLUnnamedParameter> ());
			if (!closure.ReturnType.IsEmptyTuple)
				newFunc.Parameters.Add (new SLUnnamedParameter (new SLBoundGenericType ("UnsafeMutablePointer", typeMapper.TypeSpecMapper.MapType (declContext, imports, closure.ReturnType, false))));
			if (!closure.Arguments.IsEmptyTuple)
				newFunc.Parameters.Add (new SLUnnamedParameter (new SLBoundGenericType ("UnsafeMutablePointer", typeMapper.TypeSpecMapper.MapType (declContext, imports, closure.Arguments, false))));
			return newFunc;
		}

		bool IsAction (ClosureTypeSpec closure)
		{
			return closure.ReturnType.IsEmptyTuple;
		}

		public string SubstituteForSelf { get; set; }
	}
}

