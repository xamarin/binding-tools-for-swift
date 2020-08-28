// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using SwiftReflector.TypeMapping;
using Dynamo;
using Dynamo.CSLang;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.ExceptionTools;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;
using ObjCRuntime;

namespace SwiftReflector {
	public class MarshalEngineCSafeSwiftToCSharp {
		CSUsingPackages use;
		List<string> identifiersUsed;
		TypeMapper typeMapper;

		public Func<int, int, string> GenericRenamer { get; set; }

		public MarshalEngineCSafeSwiftToCSharp (CSUsingPackages use, List<string> identifiersUsed, TypeMapper typeMapper)
		{
			this.use = use;
			this.typeMapper = typeMapper;
			this.identifiersUsed = Ex.ThrowOnNull (identifiersUsed, "identifiersUsed");
		}

		public List<ICodeElement> MarshalFromLambdaReceiverToCSFunc (CSType thisType, string csProxyName, CSParameterList delegateParams,
			FunctionDeclaration funcDecl, CSType methodType, CSParameterList methodParams, string methodName, bool isObjC, bool hasAssociatedTypes)
		{
			if (hasAssociatedTypes)
				funcDecl = funcDecl.MacroReplaceType (funcDecl.Parent.ToFullyQualifiedNameWithGenerics (), "Self", true);

			bool thisIsInterface = csProxyName != null;
			bool isIndexer = funcDecl.IsSubscript;
			bool needsReturn = methodType != null && methodType != CSSimpleType.Void;
			bool isSetter = funcDecl.IsSubscriptSetter;
			bool returnIsGeneric = funcDecl.IsTypeSpecGeneric (funcDecl.ReturnTypeSpec);
			var returnIsSelf = !TypeSpec.IsNullOrEmptyTuple (funcDecl.ReturnTypeSpec) && funcDecl.ReturnTypeSpec.IsDynamicSelf;

			var entity = !returnIsGeneric ? typeMapper.GetEntityForTypeSpec (funcDecl.ReturnTypeSpec) : null;
			var returnEntity = entity;
			var entityType = !returnIsGeneric ? typeMapper.GetEntityTypeForTypeSpec (funcDecl.ReturnTypeSpec) : EntityType.None;

			bool returnIsStruct = needsReturn && entity != null && entity.IsStructOrEnum;
			bool returnIsClass = needsReturn && entity != null && entity.EntityType == EntityType.Class;
			bool returnIsProtocol = needsReturn && ((entity != null && entity.EntityType == EntityType.Protocol) || entityType == EntityType.ProtocolList);
			bool returnIsTuple = needsReturn && entityType == EntityType.Tuple;
			bool returnIsClosure = needsReturn && entityType == EntityType.Closure;

			var callParams = new List<CSBaseExpression> ();
			var preMarshalCode = new List<CSLine> ();
			var postMarshalCode = new List<CSLine> ();
			CSBaseExpression valueExpr = null;
			bool marshalingThrows = false;


			if (isSetter) {
				var valueID = delegateParams [1].Name;
				valueExpr = valueID;
				var swiftNewValue = funcDecl.ParameterLists [1] [0];
				bool newValueIsGeneric = funcDecl.IsTypeSpecGeneric (funcDecl.PropertyType);
				entity = !newValueIsGeneric ? typeMapper.GetEntityForTypeSpec (swiftNewValue.TypeSpec) : null;
				entityType = !newValueIsGeneric ? typeMapper.GetEntityTypeForTypeSpec (swiftNewValue.TypeSpec) : EntityType.None;
				var isUnusualNewValue = IsUnusualParameter (entity, delegateParams [1]);

				if (entityType == EntityType.Class || entity != null && entity.IsObjCProtocol) {
					var csParmType = new CSSimpleType (entity.SharpNamespace + "." + entity.SharpTypeName);
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 26, "Inconceivable! The class type for a subscript was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));

					var fullClassName = entity.Type.ToFullyQualifiedName (true);
					valueExpr = NewClassCompiler.SafeMarshalClassFromIntPtr (valueID, csParmType, use, fullClassName, typeMapper, entity.IsObjCProtocol);
				} else if (entityType == EntityType.Protocol) {
					var csParmType = new CSSimpleType (entity.SharpNamespace + "." + entity.SharpTypeName);
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 27, "Inconceivable! The protocol type for a subscript was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (StructMarshal));
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));
					valueExpr = new CSFunctionCall ($"SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<{csParmType.ToString ()}>", false, valueID);
				} else if ((entityType == EntityType.Struct || entityType == EntityType.Enum) && !isUnusualNewValue) {
					var csParmType = new CSSimpleType (entity.SharpNamespace + "." + entity.SharpTypeName);
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 28, $"Inconceivable! The {entityType} type for a subscript was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify ("val", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					CSLine valDecl = CSVariableDeclaration.VarLine (csParmType, valMarshalId,
					                                            new CSCastExpression (csParmType, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false, valueID,
					                                                                                        csParmType.Typeof ())));
					preMarshalCode.Add (valDecl);
					valueExpr = valMarshalId;
				} else if (entityType == EntityType.Tuple) {
					var csParmType = new CSSimpleType (entity.SharpNamespace + "." + entity.SharpTypeName);
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 29, "Inconceivable! The tuple type for a subscript was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify ("val", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					var valDecl = CSVariableDeclaration.VarLine (csParmType, valMarshalId,
					                                            new CSCastExpression (csParmType, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false, valueID,
					                                                                                        csParmType.Typeof ())));
					preMarshalCode.Add (valDecl);
					valueExpr = valMarshalId;
				} else if (newValueIsGeneric) {
					var depthIndex = funcDecl.GetGenericDepthAndIndex (swiftNewValue.TypeSpec);
					var genRef = new CSGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
					genRef.ReferenceNamer = GenericRenamer;
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify ("valTemp", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					var valDecl = CSVariableDeclaration.VarLine (genRef, valMarshalId,
					                                           new CSCastExpression (genRef, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false,
					                                                                                    valueID, genRef.Typeof ())));
					preMarshalCode.Add (valDecl);
					valueExpr = valMarshalId;
				}
			}

			int j = 0;
			int k = isSetter ? 1 : 0;
			for (int i = (funcDecl.HasThrows || returnIsStruct || returnIsProtocol || isSetter || returnIsGeneric || returnIsSelf) ? 2 : 1; i < delegateParams.Count; i++, j++, k++) {
				var swiftParm = funcDecl.ParameterLists [1] [k];
				bool parmIsGeneric = funcDecl.IsTypeSpecGeneric (swiftParm);
				entity = !parmIsGeneric ? typeMapper.GetEntityForTypeSpec (swiftParm.TypeSpec) : null;
				entityType = !parmIsGeneric ? typeMapper.GetEntityTypeForTypeSpec (swiftParm.TypeSpec) : EntityType.None;
				var isUnusualParameter = IsUnusualParameter (entity, delegateParams [i]);
				var csParm = methodParams [j];

				if (entityType == EntityType.DynamicSelf) {
					var retrieveCallSite = isObjC ? $"Runtime.GetNSObject<{csProxyName}> " : $"SwiftObjectRegistry.Registry.CSObjectForSwiftObject<{csProxyName}> ";
					callParams.Add (new CSCastExpression(csParm.CSType, new CSFunctionCall (retrieveCallSite, false, csParm.Name).Dot (NewClassCompiler.kInterfaceImpl)));

				} else if (entityType == EntityType.Class || (entity != null && entity.IsObjCProtocol)) {
					var csParmType = csParm.CSType as CSSimpleType;
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 31, "Inconceivable! The class type for a method was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));

					var fullClassName = entity.Type.ToFullyQualifiedName (true);
					var retrievecall = NewClassCompiler.SafeMarshalClassFromIntPtr (delegateParams [0].Name, csParmType, use, fullClassName, typeMapper, entity.IsObjCProtocol);
					if (csParm.ParameterKind == CSParameterKind.Out || csParm.ParameterKind == CSParameterKind.Ref) {
						string id = MarshalEngine.Uniqueify (delegateParams [i].Name.Name, identifiersUsed);
						identifiersUsed.Add (id);
						preMarshalCode.Add (CSFieldDeclaration.FieldLine (csParmType, id, retrievecall));
						callParams.Add (new CSIdentifier (String.Format ("{0} {1}",
											       csParm.ParameterKind == CSParameterKind.Out ? "out" : "ref", id)));
						postMarshalCode.Add (CSAssignment.Assign (delegateParams [i].Name,
											  NewClassCompiler.SafeBackingFieldAccessor (new CSIdentifier (id), use, entity.Type.ToFullyQualifiedName (true), typeMapper)));
					} else {
						callParams.Add (retrievecall);
					}
				} else if (entityType == EntityType.Protocol) {
					var thePtr = new CSIdentifier (MarshalEngine.Uniqueify ("p", identifiersUsed));
					var csParmType = csParm.CSType as CSSimpleType;
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 32, "Inconceivable! The protocol type for a method was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));
					string csParmProxyType = NewClassCompiler.CSProxyNameForProtocol (entity.Type.ToFullyQualifiedName (true), typeMapper);
					if (csParmProxyType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 33, $"Unable to find C# interface type for protocol {entity.Type.ToFullyQualifiedName ()}");

					var retrievecall = new CSFunctionCall ($"SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<{csParmType.ToString ()}>", false, delegateParams [i].Name);
					if (csParm.ParameterKind == CSParameterKind.Out || csParm.ParameterKind == CSParameterKind.Ref) {
						CSIdentifier id = new CSIdentifier (MarshalEngine.Uniqueify (delegateParams [i].Name.Name, identifiersUsed));
						identifiersUsed.Add (id.Name);
						preMarshalCode.Add (CSFieldDeclaration.FieldLine (csParmType, id.Name, retrievecall));
						callParams.Add (new CSIdentifier (String.Format ("{0} {1}",
											       csParm.ParameterKind == CSParameterKind.Out ? "out" : "ref", id)));
						postMarshalCode.Add (CSAssignment.Assign (delegateParams [i].Name,
							new CSFunctionCall ("SwiftExistentialContainer1", true,
							    new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocol", false, id, csParmType.Typeof ()))));
					} else {
						callParams.Add (retrievecall);
					}
				} else if ((entityType == EntityType.Struct || entityType == EntityType.Enum) && !isUnusualParameter) {
					var csParmType = csParm.CSType as CSSimpleType;
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 34, $"Inconceivable! The {entityType} type for a method was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (StructMarshal));
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify (delegateParams [i].Name + "Temp", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					var valDecl = CSVariableDeclaration.VarLine (csParmType, valMarshalId,
										   new CSCastExpression (csParmType, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false, delegateParams [i].Name,
															       csParmType.Typeof ())));
					preMarshalCode.Add (valDecl);
					callParams.Add (valMarshalId);
				} else if (entityType == EntityType.Tuple) {
					var csParmType = csParm.CSType as CSSimpleType;
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 35, "Inconceivable! The tuple type for a parameter in a method was NOT a CSSimpleType!");
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify (delegateParams [i].Name + "Temp", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					var valDecl = CSVariableDeclaration.VarLine (csParmType, valMarshalId,
										   new CSCastExpression (csParmType, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false, delegateParams [i].Name,
															       csParmType.Typeof ())));
					preMarshalCode.Add (valDecl);
					callParams.Add (valMarshalId);
				} else if (entityType == EntityType.Closure) {
					// parm is a SwiftClosureRepresentation
					// (FuncType)StructMarshal.Marshaler.MakeDelegateFromBlindClosure (arg, argTypes, returnType);
					var argTypesId = new CSIdentifier (MarshalEngine.Uniqueify ("argTypes" + delegateParams [i], identifiersUsed));
					identifiersUsed.Add (argTypesId.Name);
					var parmType = csParm.CSType as CSSimpleType;
					if (parmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 44, "Inconceivable! The type for a closure should be a CSSimpleType");
					var hasReturn = parmType.GenericTypeName == "Func";
					var returnType = hasReturn ? (CSBaseExpression)parmType.GenericTypes [parmType.GenericTypes.Length - 1].Typeof () : CSConstant.Null;
					var argTypesLength = hasReturn ? parmType.GenericTypes.Length - 1 : parmType.GenericTypes.Length;
					var argTypes = new CSBaseExpression [argTypesLength];
					for (int idx = 0; idx < argTypesLength; idx++) {
						argTypes [idx] = parmType.GenericTypes [idx].Typeof ();
					}
					var typeArr = new CSArray1DInitialized (CSSimpleType.Type, argTypes);

					var closureExpr = new CSFunctionCall ("StructMarshal.Marshaler.MakeDelegateFromBlindClosure", false, delegateParams [i].Name,
									      typeArr, returnType);
					var castTo = new CSCastExpression (csParm.CSType, closureExpr);
					callParams.Add (castTo);
				} else if (entityType == EntityType.ProtocolList) {
					preMarshalCode.Add (CSFunctionCall.FunctionCallLine ("throw new NotImplementedException", false, CSConstant.Val ($"Argument {csParm.Name} is a protocol list type and can't be marshaled from a virtual method.")));
					callParams.Add (CSConstant.Null);
					marshalingThrows = true;
				} else if (parmIsGeneric) {
					// parm is an IntPtr to some T
					// to get T, we ask funcDecl for the depthIndex of T
					// T someVal = (T)StructMarshal.Marshaler.ToNet(parm, typeof(T));
					// someVal gets passed in
					var genRef = csParm.CSType as CSGenericReferenceType;
					if (genRef == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 36, "Inconceivable! The generic type for a parameter in a method was NOT a CSGenericReferenceType!");
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify (delegateParams [i].Name + "Temp", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					var valDecl = CSVariableDeclaration.VarLine (genRef, valMarshalId,
					                                           new CSCastExpression (genRef, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false,
					                                                                                   delegateParams [i].Name,
					                                                                                   genRef.Typeof ())));
					preMarshalCode.Add (valDecl);
					callParams.Add (valMarshalId);
				} else {
					if (csParm.ParameterKind == CSParameterKind.Out || csParm.ParameterKind == CSParameterKind.Ref) {
						callParams.Add (new CSIdentifier (String.Format ("{0} {1}",
							csParm.ParameterKind == CSParameterKind.Out ? "out" : "ref", delegateParams [i].Name.Name)));
					} else {
						callParams.Add (delegateParams [i].Name);
					}
				}
			}

			var body = new CSCodeBlock ();
			if (isObjC) {
				use.AddIfNotPresent ("ObjCRuntime");
			} else {
				use.AddIfNotPresent (typeof (SwiftObjectRegistry));
			}


			var thisTypeName = hasAssociatedTypes ? csProxyName : thisType.ToString ();
			string registryCall;

			if (thisIsInterface && !hasAssociatedTypes) {
				registryCall = $"SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<{thisTypeName}> (self)";
			} else {
				registryCall = isObjC ? $"Runtime.GetNSObject<{thisTypeName}> (self)" : $"SwiftObjectRegistry.Registry.CSObjectForSwiftObject <{thisTypeName}> (self)";
			}


			var invoker = isIndexer ? (CSBaseExpression)new CSIndexExpression (registryCall, false, callParams.ToArray ()) :
				new CSFunctionCall ($"{registryCall}.{methodName}", false, callParams.ToArray ());

			var tryBlock = funcDecl.HasThrows ? new CSCodeBlock () : null;
			var catchBlock = funcDecl.HasThrows ? new CSCodeBlock () : null;
			var altBody = tryBlock ?? body;
			string catchName = MarshalEngine.Uniqueify ("e", identifiersUsed);
			var catchID = new CSIdentifier (catchName);


			altBody.AddRange (preMarshalCode);
			if (marshalingThrows)
				return altBody;

			if (funcDecl.HasThrows || needsReturn) { // function that returns or getter
				if (funcDecl.HasThrows) {
					use.AddIfNotPresent (typeof (SwiftError));
					use.AddIfNotPresent (typeof (Tuple));
					use.AddIfNotPresent (typeof (StructMarshal));
					CSType returnTuple = null;
					if (needsReturn) {
						returnTuple = new CSSimpleType ("Tuple", false,
						                                methodType,
						                                new CSSimpleType (typeof (SwiftError)),
						                                CSSimpleType.Bool);
					} else {
						returnTuple = new CSSimpleType ("Tuple", false,
						                                new CSSimpleType (typeof (SwiftError)),
						                                CSSimpleType.Bool);
					}


					if (needsReturn) {
						string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
						var retvalId = new CSIdentifier (retvalName);
						altBody.Add (CSFieldDeclaration.VarLine (methodType, retvalId, invoker));
						postMarshalCode.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
						                                                    methodType.Typeof (),
						                                                    retvalId,
						                                                    delegateParams [0].Name));
						altBody.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.SetErrorNotThrown", false,
						                                            delegateParams [0].Name, returnTuple.Typeof ()));
					} else {
						if (isSetter) {
							altBody.Add (CSAssignment.Assign (invoker, CSAssignmentOperator.Assign, valueExpr));
						} else {
							altBody.Add (new CSLine (invoker));
						}
						altBody.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.SetErrorNotThrown", false,
						                                            delegateParams [0].Name, returnTuple.Typeof ()));

					}
					string swiftError = MarshalEngine.Uniqueify ("err", identifiersUsed);
					var swiftErrorIdentifier = new CSIdentifier (swiftError);
					catchBlock.Add (CSFieldDeclaration.VarLine (new CSSimpleType (typeof (SwiftError)), swiftErrorIdentifier,
										new CSFunctionCall ("SwiftError.FromException", false, catchID)));
					catchBlock.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.SetErrorThrown", false,
					                                               delegateParams [0].Name,
					                                               swiftErrorIdentifier,
					                                               returnTuple.Typeof ()));
				} else {
					if (returnIsClass) {
						string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
						CSIdentifier retvalId = new CSIdentifier (retvalName);
						altBody.Add (CSFieldDeclaration.VarLine (methodType, retvalId, invoker));
						postMarshalCode.Add (CSReturn.ReturnLine (
							NewClassCompiler.SafeBackingFieldAccessor (retvalId, use, returnEntity.Type.ToFullyQualifiedName (), typeMapper)));
					} else if (returnIsProtocol || returnIsSelf) {
						string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
						identifiersUsed.Add (retvalName);
						var retvalId = new CSIdentifier (retvalName);
						altBody.Add (CSFieldDeclaration.VarLine (methodType, retvalId, invoker));
						var returnContainer = MarshalEngine.Uniqueify ("returnContainer", identifiersUsed);
						identifiersUsed.Add (returnContainer);
						var returnContainerId = new CSIdentifier (returnContainer);
						var protoGetter = new CSFunctionCall ($"SwiftObjectRegistry.Registry.ExistentialContainerForProtocols", false, retvalId, methodType.Typeof ());
						var protoDecl = CSVariableDeclaration.VarLine (CSSimpleType.Var, returnContainerId, protoGetter);
						var marshalBack = CSFunctionCall.FunctionCallLine ($"{returnContainer}.CopyTo", delegateParams [0].Name);
						postMarshalCode.Add (protoDecl);
						postMarshalCode.Add (marshalBack);
					} else if (returnIsStruct) {
						// non-blitable means that the parameter is an IntPtr and we can call the
						// marshaler to copy into it
						use.AddIfNotPresent (typeof (StructMarshal));
						var marshalCall = CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
												 invoker, delegateParams [0].Name);
						altBody.Add (marshalCall);
					} else if (returnIsTuple) {
						// non-blitable means that the parameter is an IntPtr and we can call the
						// marshaler to copy into it
						use.AddIfNotPresent (typeof (StructMarshal));
						var marshalCall = CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
												 invoker, delegateParams [0].Name);
						altBody.Add (marshalCall);
					} else if (returnIsGeneric) {
						// T retval = invoker();
						// if (retval is ISwiftObject) {
						//     Marshal.WriteIntPtr(delegateParams [0].Name, ((ISwiftObject)retval).SwiftObject);
						// }
						// else {
						//    StructMarshal.Marshaler.ToSwift(typeof(T), retval, delegateParams[0].Name);
						// }
						string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
						var retvalId = new CSIdentifier (retvalName);
						altBody.Add (CSFieldDeclaration.VarLine (methodType, retvalId, invoker));
						var ifClause = new CSCodeBlock ();
						ifClause.Add (CSFunctionCall.FunctionCallLine ("Marshal.WriteIntPtr", false,
											     delegateParams [0].Name,
											     new CSParenthesisExpression (new CSCastExpression ("ISwiftObject", retvalId)).Dot (NewClassCompiler.kSwiftObjectGetter)));
						var elseClause = new CSCodeBlock ();
						elseClause.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
											       methodType.Typeof (),
											       retvalId,
											       delegateParams [0].Name));
						CSBaseExpression ifExpr = new CSSimpleType ("ISwiftObject").Typeof ().Dot (new CSFunctionCall ("IsAssignableFrom", false,
																														  methodType.Typeof ()));

						var retTest = new CSIfElse (ifExpr, ifClause, elseClause);
						altBody.Add (retTest);
					} else {
						if (returnIsClosure) {
							invoker = MarshalEngine.BuildBlindClosureCall (invoker, methodType as CSSimpleType, use);
						}
						if (postMarshalCode.Count > 0) {
							string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
							CSIdentifier retvalId = new CSIdentifier (retvalName);
							altBody.Add (CSFieldDeclaration.VarLine (methodType, retvalId, invoker));
							postMarshalCode.Add (CSReturn.ReturnLine (retvalId));
						} else {
							altBody.Add (CSReturn.ReturnLine (invoker));
						}
					}
				}
			} else { // no return or setter
				if (isSetter) {
					altBody.Add (CSAssignment.Assign (invoker, CSAssignmentOperator.Assign, valueExpr));
				} else {
					altBody.Add (new CSLine (invoker));
				}
			}
			altBody.AddRange (postMarshalCode);

			if (funcDecl.HasThrows) {
				body.Add (new CSTryCatch (tryBlock, new CSCatch (typeof (Exception), catchName, catchBlock)));
			}
			return body;
		}


		public List<ICodeElement> MarshalFromLambdaReceiverToCSProp (CSProperty prop, CSType thisType, string csProxyName, CSParameterList delegateParams,
			FunctionDeclaration funcDecl, CSType methodType, bool isObjC, bool hasAssociatedTypes)
		{
			bool forProtocol = csProxyName != null;
			bool needsReturn = funcDecl.IsGetter;
			bool returnIsGeneric = funcDecl.IsTypeSpecGeneric (funcDecl.ReturnTypeSpec);

			var entity = !returnIsGeneric ? typeMapper.GetEntityForTypeSpec (funcDecl.ReturnTypeSpec) : null;
			var entityType = !returnIsGeneric ? typeMapper.GetEntityTypeForTypeSpec (funcDecl.ReturnTypeSpec) : EntityType.None;
			bool returnIsStructOrEnum = needsReturn && entity != null && entity.IsStructOrEnum;
			bool returnIsClass = needsReturn && entity != null && entity.EntityType == EntityType.Class;
			bool returnIsProtocol = needsReturn && entity != null && entity.EntityType == EntityType.Protocol;
			bool returnIsProtocolList = needsReturn && entityType == EntityType.ProtocolList;
			bool returnIsTuple = needsReturn && entityType == EntityType.Tuple;
			bool returnIsClosure = needsReturn && entityType == EntityType.Closure;
			bool returnIsDynamicSelf = needsReturn && forProtocol && methodType.ToString () == NewClassCompiler.kGenericSelfName;

			string returnCsProxyName = returnIsProtocol ?
				NewClassCompiler.CSProxyNameForProtocol (entity.Type.ToFullyQualifiedName (true), typeMapper) : null;
			if (returnIsProtocol && returnCsProxyName == null)
				throw ErrorHelper.CreateError (ReflectorError.kCompilerReferenceBase + 22, $"Unable to find C# interface for protocol {entity.Type.ToFullyQualifiedName ()}");


			var body = new List<ICodeElement> ();
			if (isObjC) {
				use.AddIfNotPresent ("ObjCRuntime");
			} else {
				use.AddIfNotPresent (typeof (SwiftObjectRegistry));
			}


			CSIdentifier csharpCall = null;
			var thisTypeName = hasAssociatedTypes ? csProxyName : thisType.ToString ();
			if (forProtocol && !hasAssociatedTypes) {
				csharpCall = new CSIdentifier ($"SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<{thisTypeName}> (self).{prop.Name.Name}");
			} else {
				var call = isObjC ?
					$"ObjCRuntime.Runtime.GetNSObject<{thisTypeName}> (self).{prop.Name.Name}" :
					$"SwiftObjectRegistry.Registry.CSObjectForSwiftObject<{thisTypeName}> (self).{prop.Name.Name}";

				csharpCall = new CSIdentifier (call);
			}

			if (funcDecl.IsGetter) {
				if (returnIsClass) {
					if (isObjC) {
						body.Add (CSReturn.ReturnLine (csharpCall.Dot (new CSIdentifier ("Handle"))));
					} else {
						if (returnIsDynamicSelf) {
							var objName = MarshalEngine.Uniqueify ("obj", identifiersUsed);
							identifiersUsed.Add (objName);
							var objId = new CSIdentifier (objName);
							body.Add (CSVariableDeclaration.VarLine (objId, csharpCall));
							var proxyName = MarshalEngine.Uniqueify ("proxy", identifiersUsed);
							identifiersUsed.Add (proxyName);
							var proxyId = new CSIdentifier (proxyName);
							var makeProxy = new CSFunctionCall ("SwiftProtocolTypeAttribute.MakeProxy", false,
								new CSSimpleType (csProxyName).Typeof (), new CSCastExpression (thisType, objId));
							body.Add (CSVariableDeclaration.VarLine (proxyId, new CSBinaryExpression (CSBinaryOperator.As, makeProxy, new CSIdentifier ("ISwiftObject"))));
							body.Add (CSReturn.ReturnLine (proxyId.Dot (NewClassCompiler.kSwiftObjectGetter)));
						} else {
							body.Add (CSReturn.ReturnLine (csharpCall.Dot (NewClassCompiler.kSwiftObjectGetter)));
						}
					}
				} else if (returnIsStructOrEnum || returnIsTuple || returnIsGeneric) {
					use.AddIfNotPresent (typeof (StructMarshal));
					string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
					var retvalId = new CSIdentifier (retvalName);
					body.Add (CSFieldDeclaration.VarLine (methodType, retvalId, csharpCall));
					if (returnIsGeneric) {
						body.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
											   methodType.Typeof (), retvalId, delegateParams [0].Name));
					} else {
						body.Add (CSFunctionCall.FunctionCallLine ("StructMarshal.Marshaler.ToSwift", false,
											   retvalId, delegateParams [0].Name));
					}
				} else if (returnIsProtocol) {
					string retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
					identifiersUsed.Add (retvalName);
					var retvalId = new CSIdentifier (retvalName);
					body.Add (CSFieldDeclaration.VarLine (methodType, retvalId, csharpCall));
					var protocolMaker = new CSFunctionCall ("SwiftExistentialContainer1", true,
						new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocols", false, retvalId, methodType.Typeof ()));
					body.Add (CSReturn.ReturnLine (protocolMaker));
				} else if (returnIsProtocolList) {
					var protoTypeOf = new List<CSBaseExpression> ();
					var swiftProtoList = funcDecl.ReturnTypeSpec as ProtocolListTypeSpec;
					foreach (var swiftProto in swiftProtoList.Protocols.Keys) {
						protoTypeOf.Add (typeMapper.MapType (funcDecl, swiftProto, false).ToCSType (use).Typeof ());
					}
					var callExprs = new List<CSBaseExpression> ();
					callExprs.Add (csharpCall);
					callExprs.AddRange (protoTypeOf);
					var retvalName = MarshalEngine.Uniqueify ("retval", identifiersUsed);
					identifiersUsed.Add (retvalName);
					var retvalId = new CSIdentifier (retvalName);
					body.Add (CSVariableDeclaration.VarLine (methodType, retvalId, new CSFunctionCall ("StructMarshal.ThrowIfNotImplementsAll", false, callExprs.ToArray ())));
					var containerExprs = new List<CSBaseExpression> ();
					containerExprs.Add (retvalId);
					containerExprs.AddRange (protoTypeOf);

					var returnContainerName = MarshalEngine.Uniqueify ("returnContainer", identifiersUsed);
					identifiersUsed.Add (returnContainerName);
					var returnContainerId = new CSIdentifier (returnContainerName);
					body.Add (CSVariableDeclaration.VarLine (CSSimpleType.Var, returnContainerId, new CSFunctionCall ("SwiftObjectRegistry.Registry.ExistentialContainerForProtocols", false, containerExprs.ToArray ())));
					body.Add (CSFunctionCall.FunctionCallLine ($"{returnContainerName}.CopyTo", false, new CSUnaryExpression (CSUnaryOperator.Ref, delegateParams [0].Name)));
					
				} else {
					if (returnIsClosure) {
						body.Add (CSReturn.ReturnLine (MarshalEngine.BuildBlindClosureCall (csharpCall, methodType as CSSimpleType, use)));
					} else {
						body.Add (CSReturn.ReturnLine (csharpCall));
					}
				}
			} else {
				CSBaseExpression valueExpr = null;
				bool valueIsGeneric = funcDecl.IsTypeSpecGeneric (funcDecl.ParameterLists [1] [0].TypeSpec) ;
				entity = !valueIsGeneric ? typeMapper.GetEntityForTypeSpec (funcDecl.ParameterLists [1] [0].TypeSpec) : null;
				entityType = !valueIsGeneric ? typeMapper.GetEntityTypeForTypeSpec (funcDecl.ParameterLists [1] [0].TypeSpec) : EntityType.None;
				var isUnusualNewValue = IsUnusualParameter (entity, delegateParams [1]);

				if (entityType == EntityType.Class || (entity != null && entity.IsObjCProtocol)) {
					var csParmType = delegateParams [1].CSType as CSSimpleType;
					if (csParmType == null)
						throw ErrorHelper.CreateError (ReflectorError.kTypeMapBase + 42, "Inconceivable! The class type for a method was a CSSimpleType!");
					var valueIsDynamicSelf = forProtocol && methodType.ToString () == NewClassCompiler.kGenericSelfName;
					if (valueIsDynamicSelf) {
						// code generated:
						// var obj = SwiftObjectRegistry.Registry.CSObjectForSwiftObject<thisTypeName> (delegateParams [1].Name);
						// callSite = (TSelf)(obj.xamarinImpl ?? obj)
						var proxObjName = MarshalEngine.Uniqueify ("proxyObj", identifiersUsed);
						identifiersUsed.Add (proxObjName);
						var proxyObjId = new CSIdentifier (proxObjName);
						var proxyObjDecl = CSVariableDeclaration.VarLine (proxyObjId, new CSIdentifier ($"SwiftObjectRegistry.Registry.CSObjectForSwiftObject<{thisTypeName}> ({delegateParams [1].Name})"));
						body.Add (proxyObjDecl);
						valueExpr = new CSCastExpression (methodType, new CSParenthesisExpression (new CSBinaryExpression (CSBinaryOperator.NullCoalesce, proxyObjId.Dot (NewClassCompiler.kInterfaceImpl), proxyObjId)));
					} else {
						use.AddIfNotPresent (typeof (SwiftObjectRegistry));
						var fullClassName = entity.Type.ToFullyQualifiedName (true);
						var retrievecall = NewClassCompiler.SafeMarshalClassFromIntPtr (delegateParams [1].Name, csParmType, use, fullClassName, typeMapper, entity.IsObjCProtocol);
						valueExpr = retrievecall;
					}
				} else if (entityType == EntityType.Protocol) {
					use.AddIfNotPresent (typeof (SwiftObjectRegistry));
					var retrievecall = new CSFunctionCall ($"SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<{thisType.ToString ()}> (self).{prop.Name.Name}", false);
					valueExpr = retrievecall;
				} else if (entityType == EntityType.Tuple || (entity != null && entity.IsStructOrEnum && !isUnusualNewValue)) {
					var ntb = typeMapper.MapType (funcDecl, funcDecl.ParameterLists [1] [0].TypeSpec, false);
					var valType = ntb.ToCSType (use);
					if (entityType == EntityType.TrivialEnum) {
						valueExpr = new CSCastExpression (valType, new CSCastExpression (CSSimpleType.Long, delegateParams [1].Name));
					} else {
						var marshalCall = new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false, delegateParams [1].Name, valType.Typeof ());
						valueExpr = new CSCastExpression (valType, marshalCall);
					}
				} else if (valueIsGeneric) {
					// T someVal = (T)StructMarshal.Marshaler.ToNet(parm, typeof(T));
					// someVal gets passed in
					var depthIndex = funcDecl.GetGenericDepthAndIndex (funcDecl.ParameterLists [1] [0].TypeSpec);
					var genRef = new CSGenericReferenceType (depthIndex.Item1, depthIndex.Item2);
					genRef.ReferenceNamer = GenericRenamer;
					use.AddIfNotPresent (typeof (StructMarshal));
					string valMarshalName = MarshalEngine.Uniqueify (delegateParams [1].Name + "Temp", identifiersUsed);
					var valMarshalId = new CSIdentifier (valMarshalName);

					var valDecl = CSVariableDeclaration.VarLine (genRef, valMarshalId,
					                                           new CSCastExpression (genRef, new CSFunctionCall ("StructMarshal.Marshaler.ToNet", false,
					                                                                                   delegateParams [1].Name,
					                                                                                   genRef.Typeof ())));
					body.Add (valDecl);
					valueExpr = valMarshalId;
				} else {
					if (entityType == EntityType.Closure) {
						valueExpr = MarshalEngine.BuildWrappedClosureCall (delegateParams [1].Name, methodType as CSSimpleType);
					} else {
						valueExpr = delegateParams [1].Name;
					}
				}
				body.Add (CSAssignment.Assign (csharpCall, valueExpr));
			}
			return body;
		}

		static bool IsUnusualParameter (Entity entity, CSParameter parameter)
		{
			// check to see if an entity is either an objc struct or enum 
			return entity != null && (entity.IsObjCStruct || entity.IsObjCEnum) && parameter.ParameterKind == CSParameterKind.Ref;
		}
	}
}

