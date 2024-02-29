using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Dynamo;
using Dynamo.CSLang;
using SwiftReflector.SwiftXmlReflection;
using SwiftReflector.TypeMapping;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftReflector
{
	public class VTableDetails
	{
		int currentVTEntry = 0;
		const string kFuncName = "Func";
		const string kReceiverName = "_Receiver";
		public const string kLocalSetVtableName = "_SetLocalVTable";

		public VTableDetails (string className, string pinvokeClassName)
		{
			PInvokes = new CSClass (CSVisibility.Internal, pinvokeClassName);
			LocalCSVTable = new CSClass (CSVisibility.Internal, $"{className}LocalCSVTable");
			UnmanagedReceivers = new CSClass (CSVisibility.Internal, $"{className}UnmanagedReceivers", members: null, isStatic: true);
			SwiftVtable = new CSStruct (CSVisibility.Internal, $"{className}SwiftVTable", isUnsafe: true);

			var gcType = new CSSimpleType (typeof (IntPtr));
			var decl = new CSFieldDeclaration (gcType, OverrideBuilder.kVtableHandleName, value: null, CSVisibility.Public);
			SwiftVtable.Fields.Insert (0, new CSLine (decl));

		}

		public IEnumerable<CSClass> AllClasses ()
		{
			yield return PInvokes;
			if (HasActualVTable) {
				yield return LocalCSVTable;
				yield return UnmanagedReceivers;
				yield return SwiftVtable;
			}
		}

		public bool HasActualVTable => currentVTEntry > 0;
		// Definitions:
		// 1. Receiver - a static C# function that will get called by swift to
		// implement the functionality of a virtual function or a protocol method.
		// 2. VTable - a table of function pointers that implements functionality
		// 3.

		//
		// Because of the way that [UnmanagedCallersOnly] works, you
		// can't have receivers live inside of a generic class, so we have to
		// manage things in a ping-pong fashion.

		// There will exist a "local" vtable which is a C# class that contains
		// delegates that point to the "old" receive methods

		// There will exist a class that will contain all the unmanaged receivers
		
		// The local CSVtable is a class that is used to hold
		// C# delegates to receivers

		// This is more or less how this will look when all is said and done.
		// Given a swift type with virtual methods OR an implementation of a protocol,
		// name SomeClass, assuming an open func DoSomething(a: Type1, b: Type2) we will
		// create:
		// internal class SomeClassLocalCSVtable {
		//      public SomeClassLocalCSVtable () { }
		//      public Action<Type1, Type2> Func0;
		// }
		//
		// internal static class SomeClassUnmanagedReceivers {
		//      public static unsafe void SetVTable (SomeClassLocalCSVtable vt)
		//          var gch = GCHandle.Alloc (vt);
		//
		//          var swiftVT = new SomeClassSwiftVtable ();
		//          swiftVT.gcHandle = GCHandle.ToIntPtr (gch);
		//          swiftVT.Func0 = &Func0Receiver;
		//          
		//          SomeClassPInvokes.SetSwiftVTable (&swiftVT); // gets copied
		//      }
		//      [UnmanagedCallersOnly]
		//      static void Func0Receiver (IntPtr gcHandle, Type1 a, Type 2 b)
		//      {
		//          var gch = GCHandle.FromIntPtr (gcHandle);
		//          var localVT = (SomeClassLocalCSVTable)gch.Target;
		//          localVT.Func0 (a, b);
		//      }
		// }
		//
		// internal unsafe struct SomeClassSwiftVTable {
		//     public IntPtr gcHandle;
		//     public delegate *unmanaged<IntPtr, Type1, Type2, void> Func0;
		// }
		//
		// public void SomeClass {
		//     static SomeClass {
		//         var vt = new SomeClassLocalCSVtable ();
		//         vt.Func0 = RealFunc0Receiver;
		//         SomeClassUnmanagedReceivers.SetVTable (vt);
		//     }
		//     static void RealRecevier (Type1 a, Type2 b)
		//     {
		//         ... do the actual marshaling etc ..
		//     }
		// }
		//

		// Where this gets spicy is when there are generics.
		// in this case, SetVtable gets new arguments of type SwiftMetatype
		// which get passed to the pinvoke.
		// The actual class will add get the metatype arguments by calling
		// StructMarshal.Marshaler.Metatypeof (typeof (T)) for each generic parameter.

		public CSClass LocalCSVTable { get; private set; }
		public CSClass UnmanagedReceivers { get; private set; }
		public CSStruct SwiftVtable { get; private set; }
		public CSClass PInvokes { get; private set; }
		public List<string> UsedPInvokeNames { get; } = new List<string> ();


		public string DefineSetVtablePinvoke (bool hasRealGenericArguments, string pinvokeName, string entryPoint,
			int metatypeCount)
		{
			var setterName = NewClassCompiler.Uniqueify ("SwiftXamSetVtable", UsedPInvokeNames);
			UsedPInvokeNames.Add (setterName);

			var swiftSetter = new CSMethod (CSVisibility.Internal, CSMethodKind.StaticExternUnsafe, CSSimpleType.Void,
							new CSIdentifier (setterName), new CSParameterList (), null);
			CSAttribute.DllImport (pinvokeName, entryPoint).AttachBefore (swiftSetter);
			PInvokes.Methods.Add (swiftSetter);

			swiftSetter.Parameters.Add (new CSParameter (new CSSimpleType (SwiftVtable.Name.Name).Star, new CSIdentifier ("vt"), CSParameterKind.None));
			if (hasRealGenericArguments) {
				for (var i = 0; i < metatypeCount; i++) {
					swiftSetter.Parameters.Add (new CSParameter (new CSSimpleType ("SwiftMetatype"), new CSIdentifier ($"t{i}")));
				}
			}
			return setterName;
		}

		public string DefineSetVtable (string pinvokeSetterName, int metatypeCount, CSUsingPackages use)
		{
			// public static unsafe void SetVTable (SomeLocalVTable vt, SwiftMetatype mt0...)
			// {
			//      var gch = GCHandle.Alloc (vt);
			//      var swiftVT = new SomeClassSwiftVtable ();
			//      swiftVT.gcHandle = GCHandle.ToIntPtr (gch);
			//      ... swift vt assignments
			//      SomeClassPInvokes.SetSwiftVTable (&swiftVt, mt0...);
			// }

			var localVTName = new CSIdentifier ("vt");
			var mts = new CSIdentifier [metatypeCount];
			for (var i = 0; i < metatypeCount; i++) {
				mts [i] = new CSIdentifier ($"mt{i}");
			}

			var parameters = new CSParameterList (new CSParameter (new CSSimpleType (LocalCSVTable.Name.Name),
				localVTName));
			use.AddIfNotPresent (typeof (SwiftMetatype));
			parameters.AddRange (mts.Select (mt => new CSParameter (new CSSimpleType (typeof (SwiftMetatype)), mt.Name)));
			var body = new CSCodeBlock ();
			use.AddIfNotPresent (typeof (GCHandle));
			var gch = new CSIdentifier ("gch");
			var gchDecl = CSVariableDeclaration.VarLine (gch, new CSFunctionCall ("GCHandle.Alloc", false, localVTName));
			body.Add (gchDecl);

			var swiftVT = new CSIdentifier ("swiftVT");
			var swiftVTDecl = CSVariableDeclaration.VarLine (swiftVT, new CSFunctionCall (SwiftVtable.Name.Name, true));
			body.Add (swiftVTDecl);
			body.Add (CSAssignment.Assign (swiftVT.Dot (new CSIdentifier (OverrideBuilder.kVtableHandleName)), new CSFunctionCall ("GCHandle.ToIntPtr", false, gch)));

			for (var i = 0; i < currentVTEntry; i++) {
				body.Add (CSAssignment.Assign (swiftVT.Dot (new CSIdentifier (FuncName (i))),
					CSUnaryExpression.AddressOf (new CSIdentifier ($"{kReceiverName}{i}"))));
			}

			var args = new List<CSBaseExpression>
			{
				CSUnaryExpression.AddressOf(swiftVT)
			};
			args.AddRange (mts);
			body.Add (CSFunctionCall.FunctionCallLine ($"{PInvokes.Name.Name}.{pinvokeSetterName}", false, args.ToArray ()));

			var method = new CSMethod (CSVisibility.Public, CSMethodKind.StaticUnsafe, CSSimpleType.Void,
				new CSIdentifier ("SetVTable"), parameters, body);

			UnmanagedReceivers.Methods.Add (method);
			return $"{UnmanagedReceivers.Name.Name}.{method.Name.Name}";
		}

		public void DefineLocalVTInitializer (CSClass owningClass, bool hasRealGenericArguments, bool hasDynamicSelf, string callSiteToSetVTable, CSUsingPackages use)
		{
			if (!HasActualVTable)
				return;
			// static void SetVTable ()
			// {
			//     var localVT = new SomeClassLocalVT ();
			//     localVT.Func0 = ReceiverFunc0;
			//     ..
			//     callSiteToSetVTable (localVT, StructMarshal.Metatypeof (typeof (T))...);
			// }

			var body = new CSCodeBlock ();
			var localVT = new CSIdentifier ("localVT");
			body.Add (CSVariableDeclaration.VarLine (localVT, new CSFunctionCall (LocalCSVTable.Name.Name, true)));

			for (var i=0; i < currentVTEntry; i++) {
				body.Add (CSAssignment.Assign (localVT.Dot (new CSIdentifier (FuncName (i))),
					new CSIdentifier ($"{kReceiverName}{i}")));
			}

			use.AddIfNotPresent (typeof (StructMarshal));
			var args = new List<CSBaseExpression> () {
				localVT
			};
			if (hasRealGenericArguments) {
				var start = hasDynamicSelf ? 1 : 0;
				for (var i = start; i < owningClass.GenericParams.Count; i++) {
					var p = owningClass.GenericParams [i];
					args.Add (new CSFunctionCall ("StructMarshal.Marshaler.Metatypeof", false, new CSSimpleType (p.Name.Name).Typeof ()));
				}
			}

			body.Add (CSFunctionCall.FunctionCallLine (callSiteToSetVTable, args.ToArray ()));

			var method = new CSMethod (CSVisibility.None, CSMethodKind.StaticUnsafe, CSSimpleType.Void,
				new CSIdentifier (kLocalSetVtableName), new CSParameterList (), body);
			owningClass.Methods.Add (method);

			owningClass.StaticConstructor.Add (CSFunctionCall.FunctionCallLine (method.Name));
		}

		public CSDelegateTypeDecl DefineDelegateAndAddToVtable (TopLevelFunctionCompiler TLFCompiler, FunctionDeclaration func, CSUsingPackages use, bool isProtocol)
		{
			var decl = TLFCompiler.CompileToDelegateDeclaration (func, use, null, $"Del{currentVTEntry}",
									     true, CSVisibility.Public, isProtocol);

			// in the swift vtable, add field for unmanaged delegate type
			var unmanagedFunctionPtrType = RecastDelegateDeclAsFunctionPtr (decl);
			var entryID = new CSIdentifier (FuncName (currentVTEntry));
			var field = new CSFieldDeclaration (unmanagedFunctionPtrType, entryID, null, CSVisibility.Public, isStatic: false, isReadOnly: false, isUnsafe: true);
			CSAttribute.MarshalAsFunctionPointer ().AttachBefore (field);
			SwiftVtable.Fields.Add (new CSLine (field));

			// in the local vtable add a field for the receiver
			var localDelegateDecl = RecastDelegateDeclWithoutGCH (decl);
			LocalCSVTable.Delegates.Add (localDelegateDecl);
			var localDelType = new CSSimpleType (localDelegateDecl.Name.Name);
			field = new CSFieldDeclaration (localDelType.Nullable, entryID, null, CSVisibility.Public, false, false, true);
			LocalCSVTable.Fields.Add (new CSLine (field));

			return decl;
		}

		CSDelegateTypeDecl RecastDelegateDeclWithoutGCH (CSDelegateTypeDecl originalDel)
		{
			var pl = new CSParameterList (originalDel.Parameters.Skip (1));
			var isUnsafe = ParameterListContainsPointers (pl) || IsPointer (originalDel.Type);
			var del = new CSDelegateTypeDecl (originalDel.Visibility, originalDel.Type,
				new CSIdentifier ($"Func{currentVTEntry}Delegate"), pl, isUnsafe);
			return del;
		}

		CSSimpleType RecastDelegateDeclAsFunctionPtr (CSDelegateTypeDecl decl)
		{
			// given a type in the form:
			// delegate returnType DelFunc(Arg1 arg1 ...)
			// Turn it into
			// delegate *unmanaged<Arg1..., returnType> ();
			// if the type is a pointer type, turn it into an IntPtr
			var types = new CSType [decl.Parameters.Count + 1];
			for (int i = 0; i < decl.Parameters.Count; i++) {
				types [i] = ToMaybeIntPtr (decl.Parameters [i].CSType);
			}
			types [types.Length - 1] = ToMaybeIntPtr (decl.Type);
			var declType = new CSSimpleType ("delegate *unmanaged", false, types);
			return declType;
		}

		static CSType ToMaybeIntPtr (CSType theType)
		{
			return theType is CSSimpleType simple && simple.IsPointer ?
				CSSimpleType.IntPtr : theType;
		}


		public CSMethod ImplementVirtualMethodStaticReceiver (CSType thisType, string csProxyName, CSDelegateTypeDecl delType, CSUsingPackages use,
					       FunctionDeclaration funcDecl, CSMethod publicMethod,
					       bool isObjC, TypeMapper typeMapper, bool hasAssociatedTypes)
		{
			// double duty:
			// 1. Write the receiver of the form
			// static [unsafe] [returnType] ReceiverFunc0 (args)
			// {
			//     // marshaling code
			// }
			// 2. Write the actual static receiver which goes in the SwiftVtable
			// [UnmanagedCallersOnly]
			// static [unsafe] [returnType Func0 (IntPtr gcPtr, args)
			// {
			//      var gch = GCHandle.FromIntPtr (gcPtr);
			//      var localVT = (LocalVTable)gch.Target;
			//      [return] localVT.Func0(args);
			// }

			var pl = delType.Parameters;
			var plWithoutGCPtr = new CSParameterList (pl.Skip (1));
			var usedIDs = new List<string> (pl.Select (p => p.Name.Name));

			var marshal = new MarshalEngineCSafeSwiftToCSharp (use, usedIDs, typeMapper);

			var bodyContents = marshal.MarshalFromLambdaReceiverToCSFunc (thisType, csProxyName, pl, funcDecl, publicMethod.Type,
										      publicMethod.Parameters, publicMethod.Name.Name, isObjC, hasAssociatedTypes);

			var body = new CSCodeBlock (bodyContents);

			var methodKind = CSMethodKind.Static;
			if (delType.IsUnsafe || ParameterListContainsPointers (pl))
				methodKind = CSMethodKind.StaticUnsafe;

			var recvr = new CSMethod (CSVisibility.None, methodKind, delType.Type,
						  new CSIdentifier (RecevierName ()), plWithoutGCPtr, body);


			// write unmanaged receiver
			ImplementUnmanagedReceiver (methodKind, delType, pl, plWithoutGCPtr, usedIDs, use);


			currentVTEntry++;
			return recvr;
		}

		void ImplementUnmanagedReceiver (CSMethodKind methodKind,CSDelegateTypeDecl delType,
			CSParameterList pl, CSParameterList plWithoutGCPtr, List<string> usedIDs, CSUsingPackages use)
		{
			var body = new CSCodeBlock ();

			var unmanagedParameterList = new CSParameterList ();
			var args = new List<CSBaseExpression> ();
			unmanagedParameterList.Add (pl [0]); // add in the gc handle

			// the compiler doesn't like delegate *unmanaged<SomeType *>
			// why? Couldn't say, so we rewrite any pointer types as
			// IntPtr and then case them as the original pointer

			for (var i = 1; i < pl.Count; i++) {
				var parameter = pl [i];
				var theType = parameter.CSType;
				var maybePtr = ToMaybeIntPtr (theType);

				if (maybePtr == theType) {
					unmanagedParameterList.Add (parameter);
					args.Add (parameter.Name);
				} else {
					unmanagedParameterList.Add (new CSParameter (maybePtr, parameter.Name, parameter.ParameterKind));
					args.Add (new CSCastExpression (theType, parameter.Name));
				}
			}

			var unmanagedReceiver = new CSMethod (CSVisibility.None, methodKind, delType.Type,
				new CSIdentifier (RecevierName ()), unmanagedParameterList, body);
			var gchName = NewClassCompiler.Uniqueify ("gch", usedIDs);
			usedIDs.Add (gchName);
			var gchID = new CSIdentifier (gchName);
			body.Add (CSVariableDeclaration.VarLine (gchID, new CSFunctionCall ("GCHandle.FromIntPtr", false, pl [0].Name)));
			var localVTName = NewClassCompiler.Uniqueify ("localVT", usedIDs);
			usedIDs.Add (localVTName);
			var localVTID = new CSIdentifier (localVTName);
			body.Add (CSVariableDeclaration.VarLine (localVTID, new CSCastExpression (LocalCSVTable.ToCSType (),
				new CSParenthesisExpression (CSUnaryExpression.PostBang (gchID.Dot (new CSIdentifier ("Target")))))));

			var invocation = new CSFunctionCall ($"{localVTName}.{FuncName ()}!", false, args.ToArray ());

			var callLine = delType.Type == CSSimpleType.Void ? new CSLine (invocation) :
				CSReturn.ReturnLine (invocation);
			body.Add (callLine);
			use.AddIfNotPresent (typeof (UnmanagedCallersOnlyAttribute));
			var attr = CSAttribute.FromAttr (typeof (UnmanagedCallersOnlyAttribute), new CSArgumentList (), true);
			attr.AttachBefore (unmanagedReceiver);
			UnmanagedReceivers.Methods.Add (unmanagedReceiver);
		}

		public CSMethod ImplementVirtualPropertyStaticReceiver (CSType thisType, string csProxyName, CSDelegateTypeDecl delType,
					       CSUsingPackages use, FunctionDeclaration funcDecl, CSProperty prop, CSMethod protoListMethod,
					       TypeMapper typeMapper, bool isObjC, bool hasAssociatedTypes, Func<int, int, string> genericRenamer = null)
		{
			var returnType = funcDecl.IsGetter ? delType.Type : CSSimpleType.Void;

			var pl = delType.Parameters;
			var plWithoutGCPtr = new CSParameterList (pl.Skip (1));
			var usedIDs = new List<string> (pl.Select (p => p.Name.Name));

			var body = new CSCodeBlock ();

			if (protoListMethod != null) {
				body.Add (CSFunctionCall.FunctionCallLine ("throw new NotImplementedException", false, CSConstant.Val ($"Property method {protoListMethod.Name.Name} protocol list type is not supported yet")));
			} else {
				var marshaler = new MarshalEngineCSafeSwiftToCSharp (use, usedIDs, typeMapper);
				marshaler.GenericRenamer = genericRenamer;

				var bodyContents = marshaler.MarshalFromLambdaReceiverToCSProp (prop, thisType, csProxyName,
												delType.Parameters,
												funcDecl, prop.PropType, isObjC, hasAssociatedTypes);
				body.AddRange (bodyContents);
			}

			var methodKind = CSMethodKind.Static;
			if (delType.IsUnsafe || ParameterListContainsPointers (pl))
				methodKind = CSMethodKind.StaticUnsafe;

			var recvr = new CSMethod (CSVisibility.None, methodKind, returnType, new CSIdentifier (RecevierName ()), plWithoutGCPtr, body);

			// write unmanaged receiver
			ImplementUnmanagedReceiver (methodKind, delType, pl, plWithoutGCPtr, usedIDs, use);

			currentVTEntry++;
			return recvr;
		}

		public CSMethod ImplementVirtualSubscriptStaticReceiver (CSType thisType, string csProxyName, CSDelegateTypeDecl delType, CSUsingPackages use,
						FunctionDeclaration funcDecl, CSProperty prop, CSMethod protoListMethod, TypeMapper typeMapper, bool isObjC,
						Func<int, int, string> genericRenamer = null, bool hasAssociatedTypes = false)
		{
			var returnType = funcDecl.IsSubscriptGetter ? delType.Type : CSSimpleType.Void;

			var pl = delType.Parameters;
			var plWithoutGCPtr = new CSParameterList (pl.Skip (1));
			var usedIDs = new List<string> (pl.Select (p => p.Name.Name));

			var body = new CSCodeBlock ();

			if (protoListMethod != null) {
				body.Add (CSFunctionCall.FunctionCallLine ("throw new NotImplementedException", false, CSConstant.Val ($"In Subscript method {protoListMethod.Name.Name} protocol list type is not supported yet")));
			} else {
				var marshaler = new MarshalEngineCSafeSwiftToCSharp (use, usedIDs, typeMapper);
				marshaler.GenericRenamer = genericRenamer;

				var bodyContents = marshaler.MarshalFromLambdaReceiverToCSFunc (thisType, csProxyName, pl, funcDecl,
												funcDecl.IsSubscriptGetter ? prop.PropType : CSSimpleType.Void, prop.IndexerParameters, null, isObjC, hasAssociatedTypes);
				body.AddRange (bodyContents);
			}

			var methodKind = CSMethodKind.Static;
			if (delType.IsUnsafe || ParameterListContainsPointers (pl))
				methodKind = CSMethodKind.StaticUnsafe;

			var recvr = new CSMethod (CSVisibility.None, methodKind, returnType,
					new CSIdentifier (RecevierName ()),
					plWithoutGCPtr, body);

			// write unmanaged receiver
			ImplementUnmanagedReceiver (methodKind, delType, pl, plWithoutGCPtr, usedIDs, use);
			currentVTEntry++;
			return recvr;
		}

		string FuncName ()
		{
			return FuncName (currentVTEntry);
		}

		string FuncName (int i)
		{
			return $"{kFuncName}{i}";
		}

		string RecevierName ()
		{
			return $"{kReceiverName}{currentVTEntry}";
		}

		bool ParameterListContainsPointers (CSParameterList pl)
		{
			return pl.Any (pi => IsPointer (pi.CSType));
		}

		bool IsPointer (CSType ct)
		{
			return ct is CSSimpleType simple && simple.IsPointer;
		}
	}
}

