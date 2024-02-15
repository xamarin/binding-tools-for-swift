// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
#if !TOM_SWIFTY
using Foundation;
using ObjCRuntime;
#endif

namespace SwiftRuntimeLibrary.SwiftMarshal {
	public class StructMarshal {
		const int kMaxTupleSize = 8;
		const int kLastTupleElem = kMaxTupleSize - 1;
		object cacheLock = new object ();
		Dictionary<Type, NominalSizeStride> nominalCache = new Dictionary<Type, NominalSizeStride> ();


		public static unsafe IntPtr RetainSwiftObject (ISwiftObject obj)
		{
			if (obj == null)
				return IntPtr.Zero;
			if (obj.SwiftObject == IntPtr.Zero)
				throw new SwiftRuntimeException ("SwiftObject handle is IntPtr.Zero, likely because it was disposed.");
			byte* valueBuffer = stackalloc byte [3 * IntPtr.Size];
			SwiftCore.swift_beginAccess (obj.SwiftObject, valueBuffer, (uint)SwiftExclusivityFlags.Track, IntPtr.Zero);
			var result = SwiftCore.Retain (obj.SwiftObject);
			SwiftCore.swift_endAccess (valueBuffer);
			return result;
		}

		public static IntPtr ReleaseSwiftObject (ISwiftObject obj)
		{
			if (obj == null)
				return IntPtr.Zero;
			var result = SwiftCore.Release (obj.SwiftObject);
			return result;
		}

		public static nint RetainCount (ISwiftObject obj)
		{
			if (obj == null) return 0;
			return SwiftCore.RetainCount (obj.SwiftObject);
		}

		public static nint WeakRetainCount (ISwiftObject obj)
		{
			if (obj == null) return 0;
			return SwiftCore.RetainCount (obj.SwiftObject);
		}

#if !TOM_SWIFTY
		public unsafe static IntPtr RetainNSObject (NSObject obj)
		{
			if (obj == null)
				return IntPtr.Zero;

			byte* valueBuffer = stackalloc byte [3 * IntPtr.Size];
			SwiftCore.swift_beginAccess (obj.Handle, valueBuffer, (uint)SwiftExclusivityFlags.Track, IntPtr.Zero);
			var result = SwiftCore.RetainObjC (obj.Handle);
			SwiftCore.swift_endAccess (valueBuffer);
			return result;
		}

		public static IntPtr ReleaseNSObject (NSObject obj)
		{
			if (obj == null)
				return IntPtr.Zero;
			return SwiftCore.ReleaseObjC (obj.Handle);
		}
#endif

		Type FirstNonInterface (Type [] types)
		{
			return types.FirstOrDefault (ty => !ty.IsInterface);
		}

		public bool IsSwiftRepresentable (Type t)
		{
			if (IsSwiftPrimitive (t))
				return true;
			if (t == typeof (nint) || t == typeof (nuint))
				return true;
#if !TOM_SWIFTY
	    		if (t == typeof (nfloat))
	    			return true;
#endif
			if (t.IsTuple ())
				return TupleTypes (t).All (tup => IsSwiftRepresentable (tup));
			if (IsSwiftObject (t))
				return true;
			if (IsSwiftEnum (t))
				return true;
			if (IsSwiftStruct (t))
				return true;
			if (IsSwiftTrivialEnum (t))
				return true;
			if (IsSwiftProtocol (t))
				return true;
			if (IsSwiftError (t))
				return true;
			return false;
		}

		public SwiftMetatype Metatypeof (Type t)
		{
			var mt = new SwiftMetatype ();
			if (!MetatypeofPriv (t, ref mt))
				throw new NotSupportedException ("Unable to retrieve swift metatype for type " + t.Name);
			return mt;
		}

		public SwiftMetatype Metatypeof (Type t, Type [] interfaceConstraints)
		{
			var mt = new SwiftMetatype ();
			if (MetatypeofPriv (t, ref mt))
				return mt;
			if (interfaceConstraints.Length > 1) {
				throw new NotSupportedException ($"Can't get metatype of non-swift type with multiple interface constraints. Consider making {t.Name} inherit from a swift-backed object such as XamTrivialSwiftObject.");
			}
			if (MetatypeofPriv (interfaceConstraints [0], ref mt))
				return mt;
			throw new NotSupportedException ($"Unable to get metatype of type {t.Name} with interface constraint {interfaceConstraints [0].Name}.");
		}

		bool MetatypeofPriv (Type t, ref SwiftMetatype mt)
		{
			if (t.IsPrimitive) {
				mt = PrimitiveMetatype (t);
				return true;
			}
			if (t == typeof (nint)) {
				mt = SwiftStandardMetatypes.Int;
				return true;
			}
			if (t == typeof (nuint)) {
				mt = SwiftStandardMetatypes.UInt;
				return true;
			}
			if (t == typeof (UnsafeRawPointer)) {
				mt = SwiftStandardMetatypes.UnsafeRawPointer;
				return true;
			}
			if (t == typeof (UnsafeMutableRawPointer)) {
				mt = SwiftStandardMetatypes.UnsafeMutableRawPointer;
				return true;
			}
			if (t == typeof (OpaquePointer)) {
				mt = SwiftStandardMetatypes.OpaquePointer;
				return true;
			}
#if !TOM_SWIFTY
	    		if (t == typeof (nfloat)) {
				mt = SwiftStandardMetatypes.CGFloat;
				return true;
			}
#endif
			if (t.IsEnum) {
				if (TryGetValueTypeMetadata (t, out mt))
					return true;
			}

			if (t.IsDelegate ()) {
				mt = DelegateMetatype (t);
			}

			if (IsAction (t)) {
				mt = ActionMetatype (t);
				return true;
			}

			if (IsFunc (t)) {
				mt = FunctionMetatype (t);
				return true;
			}


			if (t.IsTuple ()) {
				SwiftMetatype [] tupleTypes = TupleTypes (t).Select (Metatypeof).ToArray ();
				mt = SwiftCore.TupleMetatype (tupleTypes);
				return true;
			}
			if (IsSwiftObject (t)) {
				mt = SwiftObjectMetatype (t);
				return true;
			}
			if (IsSwiftProtocol (t)) {
				mt = SwiftProtocolMetatype (t);
				return true;
			}
			if (IsSwiftNominal (t)) {
				mt = GetNominalMetatype (t);
				return true;
			}

			if (IsSwiftError (t)) {
				mt = SwiftCore.SwiftErrorMetatype ();
				return true;
			}

			var maybeMetatype = ImportedTypeCache.SwiftMetatypeForType (t);
			if (maybeMetatype.HasValue) {
				mt = maybeMetatype.Value;
				return true;
			}
			return false;
		}

		public IntPtr ProtocolWitnessof (Type t, Type withRespectTo = null)
		{
			if (!t.IsInterface)
				throw new NotSupportedException ($"Type {t.Name} must be an interface.");
#if DEBUG
			//string name = withRespectTo == null ? "nothing" : withRespectTo.Name;
			//Console.WriteLine($"Looking for protocol witness for {t.Name} with respect to {name}.");
#endif
			if (withRespectTo != null) {
				var extProto = t.GetCustomAttributes ().OfType<SwiftExternalProtocolDefinitionAttribute> ()
				                .Where (pa => pa.AdoptingType == withRespectTo).FirstOrDefault ();

				if (extProto != null) {
#if DEBUG
					//Console.WriteLine ($"Loading protocol witness table {extProto.ProtocolWitnessName} from {extProto.LibraryName}");
#endif
					var witTable = SwiftValueWitnessTable.ProtocolWitnessTableFromDylibFile (extProto.LibraryName,
					                                                                         DLOpenMode.Now,
					                                                                         extProto.ProtocolWitnessName);
#if DEBUG
					//Console.WriteLine ($"Loaded protocol witness table {witTable.ToString("X8")}");
#endif
					if (witTable == IntPtr.Zero) {
						throw new SwiftRuntimeException ($"Unable to find protocol witness table named {extProto.ProtocolWitnessName} in library {extProto.LibraryName}.");
					}
					return witTable;
				} else {
					var protoConst = withRespectTo.GetCustomAttributes ().OfType<SwiftProtocolConstraintAttribute> ()
					                              .Where (pa => pa.EquivalentInterface == t).FirstOrDefault ();
					if (protoConst != null) {
#if DEBUG
											//Console.WriteLine("Got protocol constraint attribute.");
#endif
						var witTable = SwiftValueWitnessTable.ProtocolWitnessTableFromDylibFile (protoConst.LibraryName,
														DLOpenMode.Now, protoConst.ProtocolWitnessName);
						if (witTable == IntPtr.Zero) {
							throw new SwiftRuntimeException ($"Unable to find protocol witness table named {protoConst.ProtocolWitnessName} in library {protoConst.LibraryName}.");
						}
						return witTable;
					} else {
#if DEBUG
						//Console.WriteLine("Failed to get protocol witness table.");
#endif
					}
				}
			}
			var attr = t.GetCustomAttributes ().OfType<SwiftProtocolTypeAttribute> ().FirstOrDefault ();
			if (attr == null) {
				throw new SwiftRuntimeException ($"Interface type {t.Name} is missing the {typeof (SwiftProtocolTypeAttribute).Name} attribute.");
			}
			var proxyType = attr.ProxyType;
			if (proxyType.ContainsGenericParameters) {
				var generics = t.GenericTypeArguments;
				proxyType = proxyType.MakeGenericType (generics);
			}
			var pi = proxyType.GetProperty ("ProtocolWitnessTable", BindingFlags.Static | BindingFlags.Public);
			if (pi == null)
				throw new SwiftRuntimeException ($"Unable to find ProtocolWitnessTable property in proxy {attr.ProxyType.Name}");
			return (IntPtr)pi.GetValue (null);
		}

		public SwiftProtocolConformanceDescriptor ProtocolConformanceof (Type interfaceType, Type withRespectTo)
		{
			return ProtocolConformanceof (interfaceType, Metatypeof (withRespectTo));
		}

		public SwiftProtocolConformanceDescriptor ProtocolConformanceof (Type interfaceType, SwiftMetatype withRespectTo)
		{
			if (!interfaceType.IsInterface)
				throw new NotSupportedException ($"Type {interfaceType.Name} must be an interface.");
			var nominalDescriptor = SwiftProtocolTypeAttribute.DescriptorForType (interfaceType);
			var witnessTable = SwiftCore.ConformsToSwiftProtocol (withRespectTo, nominalDescriptor);
			return witnessTable.Conformance;
		}

		bool IsAction (Type t)
		{
			return t.FullName.StartsWith ("System.Action`", StringComparison.Ordinal) || t.FullName == "System.Action";
		}

		bool IsFunc (Type t)
		{
			return t.FullName.StartsWith ("System.Func`", StringComparison.Ordinal);
		}

		SwiftMetatype ActionMetatype (Type t)
		{
			if (t.IsGenericType) {
				return GeneralFuncActionMetatype (t.GenericTypeArguments, t.GenericTypeArguments.Length, SwiftStandardMetatypes.Void);
			} else {
				return SwiftCore.GetFunctionTypeMetadata (new SwiftMetatype [0], new int [0], SwiftStandardMetatypes.Void, false);
			}
		}

		SwiftMetatype FunctionMetatype (Type t)
		{
			var args = t.GenericTypeArguments;
			var returnType = Metatypeof (args [args.Length - 1]);
			return GeneralFuncActionMetatype (args, args.Length - 1, returnType);
		}

		SwiftMetatype GeneralFuncActionMetatype (Type [] args, int length, SwiftMetatype returnType)
		{
			var swiftArgs = new SwiftMetatype [length];
			var inOutFlags = new int [length];
			for (int i=0; i < length; i++) {
				swiftArgs [i] = Metatypeof (args [i]);
			}
			return SwiftCore.GetFunctionTypeMetadata (swiftArgs, inOutFlags, returnType, false);
		}

		unsafe SwiftMetatype DelegateMetatype (Type t)
		{
			var mi = t.GetMethod ("Invoke");
			var pis = mi.GetParameters ();
			var parmTypes = pis.Select (pi => Metatypeof (pi.ParameterType)).ToArray ();

			var inOutFlags = pis.Select (pi => ToSwiftFlags (pi)).ToArray ();

			var returnType = mi.ReturnType != typeof (void) ? Metatypeof (mi.ReturnType) : SwiftStandardMetatypes.Void;
			return SwiftCore.GetFunctionTypeMetadata (parmTypes, inOutFlags, returnType, false);
		}

		static int ToSwiftFlags (ParameterInfo pi)
		{
			var flags = pi.GetCustomAttribute (typeof (ParamArrayAttribute)) != null ? SwiftParameterFlags.Variadic : SwiftParameterFlags.None;
			var ownership = pi.IsOut ? SwiftParameterOwnership.InOut : SwiftParameterOwnership.Default;
			return (int)ownership | (int)flags;
		}


		internal static SwiftMetatype SwiftObjectMetatype (Type t)
		{
			var workingType = t;
			MethodInfo mi = null;
			while (mi == null) {
				mi = workingType.GetMethod ("GetSwiftMetatype", BindingFlags.Static | BindingFlags.Public);
				if (mi != null)
					break;
				workingType = workingType.BaseType;
				if (workingType == null)
					throw new SwiftRuntimeException ($"Unable to find static method GetSwiftMetatype in type {t.Name}.");
			}
			return (SwiftMetatype)mi.Invoke (null, null);
		}

		bool IsSwiftProtocol (Type t)
		{
			return t.IsInterface && t.GetCustomAttributes ().OfType<SwiftProtocolTypeAttribute> ().FirstOrDefault () != null;
		}

		static bool IsExistentialContainer (Type t)
		{
			return typeof (ISwiftExistentialContainer).IsAssignableFrom (t);
		}

		SwiftMetatype SwiftProtocolMetatype (Type t)
		{
			return EveryProtocol.GetSwiftMetatype ();
		}

		SwiftMetatype GetNominalMetatype (Type t)
		{
			var attr = t.GetCustomAttributes ().OfType<SwiftValueTypeAttribute> ().FirstOrDefault ();
			if (attr == null) {
				throw new SwiftRuntimeException (String.Format ("Can't retrieve metatype for type {0}, there is no {1} value attached to it.",
				    t.Name, typeof (SwiftValueTypeAttribute).Name));
			}
			if (String.IsNullOrEmpty (attr.Metadata)) {
				return SwiftObjectMetatype (t);
			} else {
				var meta = SwiftMetatype.FromDylib (attr.LibraryName, DLOpenMode.Now, attr.Metadata);
				if (meta == null) {
					throw new SwiftRuntimeException (String.Format ("Unable to find metatype symbol {0} in library {1} for type {2}.",
					    attr.Metadata, attr.LibraryName, t.Name));
				}
				return meta.Value;
			}
		}

		bool TryGetValueTypeMetadata (Type t, out SwiftMetatype mt)
		{
			try {
				mt = GetNominalMetatype (t);
				return true;
			} catch {
				mt = default (SwiftMetatype);
				return false;
			}
		}

		SwiftMetatype SwiftStructMetatype (Type t)
		{
			var attr = t.GetCustomAttributes ().OfType<SwiftStructAttribute> ().FirstOrDefault ();
			if (String.IsNullOrEmpty (attr.Metadata)) {
				return SwiftObjectMetatype (t);
			} else {
				var meta = SwiftMetatype.FromDylib (attr.LibraryName, DLOpenMode.Now, attr.Metadata);
				if (meta == null) {
					throw new SwiftRuntimeException (String.Format ("Unable to find metatype symbol {0} in library {1} for type {2}.",
					    attr.Metadata, attr.LibraryName, t.Name));
				}
				return meta.Value;
			}
		}

		internal static bool IsSwiftPrimitive (Type t)
		{
			if (!t.IsPrimitive)
				return false;
			switch (Type.GetTypeCode (t)) {
			case TypeCode.Boolean:
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
				return true;
			default:
				if (t == typeof (IntPtr) || t == typeof (UIntPtr)) {
					return true;
				}
				return false;
			case TypeCode.Char:
				return false;
			}
		}

		SwiftMetatype PrimitiveMetatype (Type t)
		{
			// see https://msdn.microsoft.com/en-us/library/system.type.isprimitive.aspx
			switch (Type.GetTypeCode (t)) {
			case TypeCode.Boolean:
				return SwiftStandardMetatypes.Bool;
			case TypeCode.Byte:
				return SwiftStandardMetatypes.UInt8;
			case TypeCode.SByte:
				return SwiftStandardMetatypes.Int8;
			case TypeCode.Int16:
				return SwiftStandardMetatypes.Int16;
			case TypeCode.UInt16:
				return SwiftStandardMetatypes.UInt16;
			case TypeCode.Int32:
				return SwiftStandardMetatypes.Int32;
			case TypeCode.UInt32:
				return SwiftStandardMetatypes.UInt32;
			case TypeCode.Int64:
				return SwiftStandardMetatypes.Int64;
			case TypeCode.UInt64:
				return SwiftStandardMetatypes.UInt64;
			case TypeCode.Char:
				throw new NotSupportedException (".NET char type not supported in swift.");
			case TypeCode.Single:
				return SwiftStandardMetatypes.Float;
			case TypeCode.Double:
				return SwiftStandardMetatypes.Double;
			default:
				if (t == typeof (IntPtr)) {
					return SwiftStandardMetatypes.Int;
				} else if (t == typeof (UIntPtr)) {
					return SwiftStandardMetatypes.UInt;
				}
				throw new SwiftRuntimeException ($"Illegal type code for type {t.Name}:  {Type.GetTypeCode (t)}");
			}
		}

		static Type[] primitiveTypes = new Type [] {
				typeof (bool), typeof (byte), typeof (sbyte), typeof (short), typeof (ushort),
				typeof (int), typeof (uint), typeof (long), typeof (ulong), typeof (float), typeof (double),
				typeof (nint), typeof (nuint)
			};

		internal static IEnumerable<Tuple<SwiftMetatype, Type>> PrimitiveTypeMap ()
		{
			foreach (var type in primitiveTypes) {
				yield return new Tuple<SwiftMetatype, Type> (Marshaler.Metatypeof (type), type);
			}
		}

		public SwiftMetatype ExistentialMetatypeof (params Type [] interfaceTypes)
		{
			if (interfaceTypes.Length == 1) {
				var protocolDesc = SwiftProtocolTypeAttribute.DescriptorForType (interfaceTypes [0]);
				return SwiftCore.ExistentialContainerMetadata (protocolDesc);
			} else {
				var protoDescs = new SwiftNominalTypeDescriptor [interfaceTypes.Length];
				for (int i=0; i < interfaceTypes.Length; i++) {
					protoDescs [i] = SwiftProtocolTypeAttribute.DescriptorForType (interfaceTypes [i]);
				}
				Array.Sort (protoDescs, (x, y) => string.Compare (x.GetFullName (), y.GetFullName (), StringComparison.Ordinal));
				return SwiftCore.ExistentialContainerMetadata (protoDescs);
			}
		}

		public int Sizeof (Type [] types)
		{
			var classType = FirstNonInterface (types);
			if (classType != null) {
				return IntPtr.Size;
			}
			if (types.Length != 1)
				throw new NotSupportedException ("Protocol lists not supported. Yet.");
			return 5 * IntPtr.Size;
		}

		public int Sizeof (Type t)
		{
			var mt = new SwiftMetatype ();
			if (!MetatypeofPriv (t, ref mt))
				throw new NotSupportedException ("Unable to determine swift size for type " + t.Name);
			return (int)SwiftCore.SizeOf (mt);
		}

		public int Strideof (Type t, Type [] constraints)
		{
			var size = StrideofPriv (t);
			if (size >= 0)
				return size;
			if (constraints.Length > 1)
				throw new NotSupportedException ($"Type {t.Name} has multiple interface constraints which is supported for Strideof.");
			return Strideof (constraints [0]);
		}

		public int Strideof (Type t)
		{
			var size = StrideofPriv (t);
			if (size < 0)
				throw new ArgumentOutOfRangeException (nameof(t), String.Format ("Type {0} should be a swift struct or enum.", t.Name));
			return size;
		}

		int StrideofPriv (Type t)
		{
			var mt = new SwiftMetatype ();
			if (!MetatypeofPriv (t, ref mt))
				throw new NotSupportedException ("Unable to determine swift stride for type " + t.Name);
			return (int)SwiftCore.StrideOf (mt);
		}

		internal int Alignmentof (Type [] types)
		{
			var classType = FirstNonInterface (types);
			if (classType != null) {
				return IntPtr.Size;
			}
			if (types.Length != 1)
				throw new NotSupportedException ("Protocol lists not supported. Yet.");
			return IntPtr.Size;
		}

		internal int Alignmentof (Type t)
		{
			var mt = new SwiftMetatype ();
			if (!MetatypeofPriv (t, ref mt))
				throw new NotSupportedException ("Unable to determine swift alignment for type " + t.Name);
			return (int)SwiftCore.AlignmentOf (mt);
		}

		public static T DefaultValueType<T> () where T : ISwiftValueType
		{
			ConstructorInfo constructorInfo = GetValueTypeCtor (typeof (T));
			if (constructorInfo == null)
				throw new SwiftRuntimeException ($"Unable to find Constuctor ({typeof (SwiftValueTypeCtorArgument).Name}) for {typeof (T)}");

			return (T)constructorInfo.Invoke (new object [] { SwiftValueTypeCtorArgument.None });
		}

		static ConstructorInfo GetValueTypeCtor (Type type)
		{
			var ctors = type.GetConstructors (BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
			for (int i = 0; i < ctors.Length; ++i) {
				var param = ctors [i].GetParameters ();
				if (param.Length == 1 && param [0].ParameterType == typeof (SwiftValueTypeCtorArgument))
					return ctors [i];
			}
			return null;
		}

		public byte [] PrepareValueType (ISwiftValueType nominal)
		{
			var size = Strideof (nominal.GetType ());
			if (nominal.SwiftData == null || nominal.SwiftData.Length != size) {
				nominal.SwiftData = new byte [size];
			}
			return nominal.SwiftData;
		}

		public IntPtr ToSwift (object o, IntPtr swiftDestinationMemory)
		{
			if (o == null)
				throw new ArgumentNullException ();
			return ToSwift (o.GetType (), o, swiftDestinationMemory);
		}

		internal unsafe IntPtr ToSwift (Type t, object o, byte* swiftDestinationMemory)
		{
			return ToSwift (t, o, (IntPtr)swiftDestinationMemory);
		}

		public IntPtr ToSwift (Type t, object o, IntPtr swiftDestinationMemory)
		{
			if (t.IsPrimitive) {
				return MarshalScalarToSwift (t, o, swiftDestinationMemory);
			}

			if (t == typeof (nint)) {
				Write ((nint)o, swiftDestinationMemory);
				return swiftDestinationMemory;
			}

			if (t == typeof (nuint)) {
				Write ((nuint)o, swiftDestinationMemory);
				return swiftDestinationMemory;
			}
#if !TOM_SWIFTY
			if (t == typeof (nfloat)) {
				Write ((nfloat)o, swiftDestinationMemory);
				return swiftDestinationMemory;
			}
#endif

			if (IsSwiftTrivialEnum (t)) {
				Write ((nuint)(long)o, swiftDestinationMemory);
				return swiftDestinationMemory;
			}

			if (IsSwiftNominal (t)) {
				return MarshalNominalToSwift (t, o, swiftDestinationMemory);
			}

			if (IsSwiftObject (t)) {
				if (o != null)
					Write (RetainSwiftObject ((ISwiftObject)o), swiftDestinationMemory);
				else
					Write (IntPtr.Zero, swiftDestinationMemory);
				return swiftDestinationMemory;
			}

#if !TOM_SWIFTY
			if (IsObjCProtocol (t)) {
				// protocols don't get a retain before a call
				var nsobject = (INativeObject)o;
				Write (nsobject.Handle, swiftDestinationMemory);
				return swiftDestinationMemory;
			}

			if (IsNSObject (t)) {
				if (o != null)
					Write (RetainNSObject ((NSObject)o), swiftDestinationMemory);
				else
					Write (IntPtr.Zero, swiftDestinationMemory);
				return swiftDestinationMemory;
			}
#endif
			if (IsSwiftProtocol (t)) {
				return MarshalProtocolToSwift (t, o, swiftDestinationMemory);
			}

			if (IsSwiftError (t)) {
				return MarshalSwiftErrorToSwift ((SwiftError)o, swiftDestinationMemory);
			}

			if (t.IsDelegate ()) {
				return MarshalDelegateToSwift (t, (Delegate)o, swiftDestinationMemory);
			}


			if (t.IsTuple ()) {
				return MarshalTupleToSwift (t, o, swiftDestinationMemory);
			}

			if (IsExistentialContainer (t)) {
				return MarshalExistentialContainerToSwift (t, o, swiftDestinationMemory);
			}

			throw new SwiftRuntimeException (String.Format ("Unable to marshal type {0} to swift.", o.GetType ().Name));
		}

		unsafe internal T ToNet<T> (byte* swiftSourceMemory, bool owns)
		{
			return ToNet<T> ((IntPtr)swiftSourceMemory, owns);
		}

		// FIXME: call the 'owns' overload from our code, and remove this overload
		public T ToNet<T> (IntPtr swiftSourceMemory)
		{
			return ToNet<T> (swiftSourceMemory, false);
		}

		public T ToNet<T> (IntPtr p, bool owns)
		{
			return (T)ToNet (p, typeof (T), owns);
		}

		// FIXME: call the 'owns' overload from our code, and remove this overload
		public object ToNet (IntPtr swiftSourceMemory, Type t)
		{
			return ToNet (swiftSourceMemory, t, false);
		}

		public object ToNet (IntPtr swiftSourceMemory, Type t, bool owns)
		{
			if (t.IsPrimitive) {
				return MarshalScalarToNet (swiftSourceMemory, t);
			}

			if (t == typeof (nint)) {
				return ReadNint (swiftSourceMemory);
			}

			if (t == typeof (nuint)) {
				return ReadNuint (swiftSourceMemory);
			}

#if !TOM_SWIFTY
			if (t == typeof (nfloat)) {
				return ReadNfloat (swiftSourceMemory);
			}
#endif

			if (t.IsTuple ()) {
				return MarshalTupleToNet (swiftSourceMemory, t, owns);
			}

			if (t.IsDelegate ()) {
				return MarshalDelegateToNet (swiftSourceMemory, t, owns);
			}

			if (IsSwiftTrivialEnum (t)) {
				//return Enum.ToObject(t, Convert.ChangeType((long)ReadNint(p), Enum.GetUnderlyingType(t)));
				return Enum.ToObject (t, (long)ReadNint (swiftSourceMemory));
			}

			if (IsSwiftNominal (t)) {
				return MarshalNominalToNet (swiftSourceMemory, t, owns);
			}

			if (IsSwiftError (t)) {
				return new SwiftError (Marshal.ReadIntPtr (swiftSourceMemory));
			}

			if (IsSwiftObject (t)) {
				return SwiftObjectRegistry.Registry.CSObjectForSwiftObject (Marshal.ReadIntPtr (swiftSourceMemory), t, owns);
			}
#if !TOM_SWIFTY
			if (IsNSObject (t)) {
				var rv = ObjCRuntime.Runtime.GetNSObject (Marshal.ReadIntPtr (swiftSourceMemory))!;
				if (owns)
					rv.DangerousRelease ();
				return rv;
			}

			if (IsObjCProtocol (t)) {
				return GetINativeObject (Marshal.ReadIntPtr (swiftSourceMemory), t, owns);

			}
#endif
			throw new SwiftRuntimeException (String.Format ("Unable to marshal type {0}.", t.Name));
		}

		IntPtr MarshalSwiftErrorToSwift (SwiftError err, IntPtr swiftDestinartionMemory)
		{
			Write (err.Handle, swiftDestinartionMemory);
			return swiftDestinartionMemory + IntPtr.Size;
		}

		IntPtr MarshalExistentialContainerToSwift (Type t, object o, IntPtr swiftDestinationMemory)
		{
			var container = (ISwiftExistentialContainer)o;
			SwiftExistentialContainer0.CopyTo (container, swiftDestinationMemory);
			swiftDestinationMemory += container.SizeOf;
			return swiftDestinationMemory;
		}

		Type [] DelegateParameterTypes (MethodInfo mi)
		{
			return mi.GetParameters ().Select (pi => {
				if (pi.IsOut) {
					throw new NotSupportedException ("reference parameter types are not supported in closures in run-time marshaling.");
				}
				return pi.ParameterType;
			}).ToArray ();
		}

		object MarshalDelegateToNet (IntPtr swiftSourceMemory, Type t, bool owns)
		{
			var mi = t.GetMethod ("Invoke");
			var argTypes = DelegateParameterTypes (mi);
			var returnType = mi.ReturnType;
			var blindClosure = (BlindSwiftClosureRepresentation)Marshal.PtrToStructure (swiftSourceMemory, typeof (BlindSwiftClosureRepresentation));
			if (SwiftObjectRegistry.Registry.Contains (blindClosure.Data)) {
				var capsule = SwiftObjectRegistry.Registry.CSObjectForSwiftObject<SwiftDotNetCapsule> (blindClosure.Data);
				var delTuple = SwiftObjectRegistry.Registry.ClosureForCapsule (capsule);
				if (delTuple != null)
					return delTuple.Item1;
				// didn't find it - weird - but we can still handle that case. Let it fall through
			}

			throw new NotImplementedException ();
		}

		IntPtr MarshalDelegateToSwift (Type t, Delegate del, IntPtr swiftDestinationMemory)
		{
			var mi = t.GetMethod ("Invoke");
			var argTypes = DelegateParameterTypes (mi);
			var returnType = mi.ReturnType;

			var rep = BuildClosureRepresentation (del, argTypes, returnType);
			var blindRep = BuildBlindClosure (rep, argTypes, returnType, swiftDestinationMemory);
			Marshal.StructureToPtr (blindRep, swiftDestinationMemory, false);
			return swiftDestinationMemory;
		}

		public unsafe BlindSwiftClosureRepresentation GetBlindSwiftClosureRepresentation (Type t, Delegate del)
		{
			byte* p = stackalloc byte [IntPtr.Size * 2];
			var mi = t.GetMethod ("Invoke");
			var argTypes = DelegateParameterTypes (mi);
			var returnType = mi.ReturnType;

			var rep = BuildClosureRepresentation (del, argTypes, returnType);
			return BuildBlindClosure (rep, argTypes, returnType, new IntPtr (p));
		}

		public Delegate MakeDelegateFromBlindClosure (BlindSwiftClosureRepresentation blindClosure, Type [] argTypes, Type returnType, ClosureFlags flags = ClosureFlags.None)
		{
			if (argTypes.Length == 0 && flags == ClosureFlags.None)
				return SwiftObjectRegistry.Registry.ActionForSwiftClosure (blindClosure);

			if (returnType != null) {
				Array.Resize (ref argTypes, argTypes.Length + 1);
				argTypes [argTypes.Length - 1] = returnType;
			}

			var methodName = "";
			if (flags == ClosureFlags.Throws) {
				if (returnType == null)
					throw new SwiftRuntimeException ("Need to handle Action closures that throw.");
				methodName = returnType == null ? "ActionForSwiftClosureThrows" : "FuncForSwiftClosureThrows";
			} else if ((flags & ClosureFlags.Async) == ClosureFlags.Async) {
				throw new SwiftRuntimeException ("Marshaling async closures isn't supported (yet).");
			} else {
				methodName = returnType == null ? "ActionForSwiftClosure" : "FuncForSwiftClosure";
			}
			var mi = typeof (SwiftObjectRegistry).GetMethod (methodName);
			if (mi == null)
				throw new SwiftRuntimeException ($"Making a closure, unable to find method {methodName}");
			var genCall = mi.MakeGenericMethod (argTypes);
			return (Delegate)genCall.Invoke (SwiftObjectRegistry.Registry, new object [] { blindClosure });
		}

		BlindSwiftClosureRepresentation BuildBlindClosure (SwiftClosureRepresentation rep, Type [] argTypes, Type returnType, IntPtr p)
		{
			var argMetatypes = argTypes.Select (at => Metatypeof (at)).ToArray ();
			if (returnType == null) {// action
				switch (argMetatypes.Length) {
				case 0:
					Marshal.StructureToPtr (rep, p, false);
					return new BlindSwiftClosureRepresentation (Marshal.ReadIntPtr (p), rep.Data);
				case 1:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0]);
				case 2:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1]);
				case 3:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2]);
				case 4:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3]);
				case 5:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4]);
				case 6:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5]);
				case 7:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6]);
				case 8:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7]);
				case 9:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8]);
				case 10:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9]);
				case 11:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9], argMetatypes [10]);
				case 12:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9], argMetatypes [10], argMetatypes [11]);
				case 13:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9], argMetatypes [10], argMetatypes [11],
									      argMetatypes [12]);
				case 14:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9], argMetatypes [10], argMetatypes [11],
									      argMetatypes [12], argMetatypes [13]);
				case 15:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9], argMetatypes [10], argMetatypes [11],
									      argMetatypes [12], argMetatypes [13], argMetatypes [14]);
				case 16:
					return SwiftCore.ActionToSwiftClosure (rep, argMetatypes [0], argMetatypes [1], argMetatypes [2],
									      argMetatypes [3], argMetatypes [4], argMetatypes [5],
									      argMetatypes [6], argMetatypes [7], argMetatypes [8],
									      argMetatypes [9], argMetatypes [10], argMetatypes [11],
									      argMetatypes [12], argMetatypes [13], argMetatypes [14],
									      argMetatypes [15]);
				default:
					throw new NotImplementedException ("more than 16 arguments not supported in closures.");
				}
			} else { // func
				var retMeta = Metatypeof (returnType);
				switch (argMetatypes.Length) {
				case 0:
					return SwiftCore.FuncToSwiftClosure (rep,
									    retMeta);
				case 1:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0],

									    retMeta);
				case 2:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1],

									    retMeta);
				case 3:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],

									    retMeta);
				case 4:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3],

									    retMeta);
				case 5:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4],

									    retMeta);
				case 6:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],

									    retMeta);
				case 7:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6],

									    retMeta);
				case 8:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7],

									    retMeta);
				case 9:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],

									    retMeta);
				case 10:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9],

									    retMeta);
				case 11:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9], argMetatypes [10],

									    retMeta);
				case 12:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9], argMetatypes [10], argMetatypes [11],

									    retMeta);
				case 13:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9], argMetatypes [10], argMetatypes [11],
									    argMetatypes [12],

									    retMeta);
				case 14:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9], argMetatypes [10], argMetatypes [11],
									    argMetatypes [12], argMetatypes [13],

									    retMeta);
				case 15:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9], argMetatypes [10], argMetatypes [11],
									    argMetatypes [12], argMetatypes [13], argMetatypes [14],

									    retMeta);
				case 16:
					return SwiftCore.FuncToSwiftClosure (rep,
									    argMetatypes [0], argMetatypes [1], argMetatypes [2],
									    argMetatypes [3], argMetatypes [4], argMetatypes [5],
									    argMetatypes [6], argMetatypes [7], argMetatypes [8],
									    argMetatypes [9], argMetatypes [10], argMetatypes [11],
									    argMetatypes [12], argMetatypes [13], argMetatypes [14],
									    argMetatypes [15],

									    retMeta);
				default:
					throw new NotImplementedException ("more than 16 arguments not supported in closures.");
				}
			}
		}

		unsafe SwiftClosureRepresentation BuildClosureRepresentation (Delegate del, Type [] argTypes, Type returnType)
		{
			if (returnType == null) { // Action
				if (argTypes.Length == 0) {
					return SwiftObjectRegistry.Registry.SwiftClosureForDelegate (del, &SwiftClosureRepresentation.ActionCallbackVoidVoid,
												    argTypes);
				} else {
					return SwiftObjectRegistry.Registry.SwiftClosureForDelegate (del, &SwiftClosureRepresentation.ActionCallback,
												    argTypes);
				}
			} else {
				if (argTypes.Length == 0) {
					return SwiftObjectRegistry.Registry.SwiftClosureForDelegate (del, &SwiftClosureRepresentation.FuncCallbackVoid,
												    argTypes, returnType);
				} else {
					return SwiftObjectRegistry.Registry.SwiftClosureForDelegate (del, &SwiftClosureRepresentation.FuncCallback,
												    argTypes, returnType);
				}
			}
		}

		public IntPtr MarshalProtocolToSwift (Type t, object implementsProtocol, IntPtr swiftDestinationMemory)
		{
			var container = SwiftObjectRegistry.Registry.ExistentialContainerForProtocols (implementsProtocol, t);
			return container.CopyTo (swiftDestinationMemory);
		}


		public IntPtr MarshalTupleToSwift (Type t, object tuple, IntPtr swiftDestinationMemory)
		{
			// null tuple? Don't do anything.
			if (tuple == null)
				return swiftDestinationMemory;
			var allTypes = TupleTypes (t).ToArray ();
			var map = SwiftTupleMap.FromTypes (allTypes);
			return MarshalTupleToSwift (t, map, tuple, swiftDestinationMemory);
		}

		public IntPtr MarshalTupleToSwift (Type t, SwiftTupleMap map, object tuple, IntPtr swiftDestinationMemory)
		{
			if (tuple == null)
				return swiftDestinationMemory;

			var tupleTypesValues = TupleItems (t, tuple);
			int i = 0;
			foreach (Tuple<Type, object> typeVal in tupleTypesValues) {
				if (typeVal.Item1 != map.Types [i])
					throw new SwiftRuntimeException ("Error decomposing tuple for marshaling to swift.");
				ToSwift (typeVal.Item1, typeVal.Item2, swiftDestinationMemory + map.Offsets [i]);
				i++;
			}
			return swiftDestinationMemory;
		}

		public IntPtr MarshalObjectsAsTuple (object [] objects, SwiftTupleMap map, IntPtr swiftDestinationMemory)
		{
			if (objects == null)
				throw new ArgumentNullException (nameof (objects));
			if (map.Types.Length != objects.Length)
				throw new ArgumentOutOfRangeException (nameof (objects), "Size mismatch in object array length and tuple map size.");
			for (int i = 0; i < objects.Length; i++) {
				ToSwift (map.Types [i], objects [i], swiftDestinationMemory + map.Offsets [i]);
			}
			return swiftDestinationMemory;
		}


		public object MarshalTupleToNet (IntPtr swiftSourceMemory, Type t, bool owns)
		{
			var allTypes = TupleTypes (t).ToArray ();
			var map = SwiftTupleMap.FromTypes (allTypes);
			return MarshalTupleToNet (swiftSourceMemory, map, owns);
		}


		object InvokeTupleConstructor (Type [] types, object [] args)
		{
			var tupleType = MakeTupleType (types);

			if (types.Length < kMaxTupleSize) {
				var ci = tupleType.GetConstructor (types);
				if (ci == null)
					throw new SwiftRuntimeException ("Unable to find tuple constructor.");
				return ci.Invoke (args);
			} else {
				var argsHead = args.Slice (0, kLastTupleElem, true);
				var tail = InvokeTupleConstructor (
				    types.Slice (kLastTupleElem, types.Length - kLastTupleElem),
				    args.Slice (kLastTupleElem, types.Length - kLastTupleElem));
				argsHead [kLastTupleElem] = tail;
				var ci = tupleType.GetConstructor (tupleType.GetGenericArguments ());
				if (ci == null)
					throw new SwiftRuntimeException ("Unable to find large tuple constructor.");
				return ci.Invoke (argsHead);
			}
		}

		void PrintArrOfTypes (Type [] types)
		{
			foreach (Type t in types) {
				Console.Write (" " + t.Name);
				if (t.IsGenericType) {
					Type [] gen = t.GetGenericArguments ();
					Console.Write ("<");
					PrintArrOfTypes (gen);
					Console.Write (">");
				}
			}
		}



		public object MarshalTupleToNet (IntPtr swiftSourceMemory, SwiftTupleMap map, bool owns)
		{
			var args = map.Types.Select ((tt, i) => ToNet (swiftSourceMemory + map.Offsets [i], tt, owns)).ToArray ();

			return InvokeTupleConstructor (map.Types, args);
		}

		internal object [] MarshalSwiftTupleMemoryToNet (IntPtr p, Type [] tupleTypes)
		{
			var map = SwiftTupleMap.FromTypes (tupleTypes);
			var values = map.Types.Select ((tt, i) => ToNet (p + map.Offsets [i], tt)).ToArray ();
			return values;
		}

		IOrderedEnumerable<PropertyInfo> TupleProperties (Type t)
		{
			return t.GetProperties ().OrderBy (pi => pi.Name);
		}

		ConstructorInfo GetTupleConstructorMethod (Type t)
		{
			var propTypes = TupleProperties (t).Select (pi => pi.PropertyType).ToArray ();
			return t.GetConstructor (propTypes);
		}

		IEnumerable<Tuple<Type, object>> SimpleTupleItems (object o, List<PropertyInfo> props, int count)
		{
			return props.Take (count).Select (pi => Tuple.Create (pi.PropertyType, pi.GetValue (o)));
		}

		IEnumerable<Tuple<Type, object>> TupleItems (Type t, object o)
		{
			if (!t.IsTuple ())
				throw new ArgumentException ("Type is not a tuple.", nameof (t));
			var props = TupleProperties (t).ToList ();
			if (props.Count < kMaxTupleSize) {
				return SimpleTupleItems (o, props, props.Count);
			} else {
				var lastProp = props.Last ();
				return Enumerable.Concat (SimpleTupleItems (o, props, kMaxTupleSize - 1), TupleItems (lastProp.PropertyType,
				    lastProp.GetValue (o)));
			}
		}

		IEnumerable<Type> SimpleTupleTypes (List<PropertyInfo> props, int count)
		{
			return props.Take (count).Select (pi => pi.PropertyType);
		}

		IEnumerable<Type> TupleTypes (Type t)
		{
			var props = TupleProperties (t).ToList ();
			if (props.Count < kMaxTupleSize) {
				return SimpleTupleTypes (props, props.Count);
			} else {
				var lastProp = props.Last ();
				return Enumerable.Concat (SimpleTupleTypes (props, kMaxTupleSize - 1), TupleTypes (lastProp.PropertyType));
			}
		}

		Func<object [], object> GetTupleCtor (Type t)
		{
			var ci = GetTupleConstructorMethod (t);
			if (ci == null)
				throw new SwiftRuntimeException ("Unable to find constructor for tuple of type " + t);
			return args => ci.Invoke (args);
		}

		Func<object [], object> GetTupleConstructor (Type t)
		{
			var boundTypes = t.GetGenericArguments ();
			var maker1 = GetTupleCtor (t);
			// two cases -
			// either exactly kMaxTupleSize or less than that.
			if (boundTypes.Length < kMaxTupleSize)
				return maker1;
			else {
				// the last type will encode the rest of the tuple
				Type tyBenc = boundTypes [kMaxTupleSize - 1];
				var maker2 = GetTupleConstructor (tyBenc);
				return args => {
					object encVal = maker2 (args.Slice (7, args.Length - 7));
					object [] newargs = args.Slice (0, 7, true);
					newargs [7] = encVal;
					return maker1 (newargs);
				};
			}
		}

		object MakeTuple (Type t, object [] args)
		{
			return GetTupleConstructor (t) (args);
		}

		internal Type MakeTupleType (Type [] types)
		{
			if (types.Length == 0)
				throw new SwiftRuntimeException ("Empty tuples not supported. Yet.");
			switch (types.Length) {
			case 1:
				return typeof (Tuple<>).MakeGenericType (types);
			case 2:
				return typeof (Tuple<,>).MakeGenericType (types);
			case 3:
				return typeof (Tuple<,,>).MakeGenericType (types);
			case 4:
				return typeof (Tuple<,,,>).MakeGenericType (types);
			case 5:
				return typeof (Tuple<,,,,>).MakeGenericType (types);
			case 6:
				return typeof (Tuple<,,,,,>).MakeGenericType (types);
			case 7:
				return typeof (Tuple<,,,,,,>).MakeGenericType (types);
			default:
				Type [] first = types.Slice (0, 7, true); // take first 7
				Type [] second = types.Slice (7, types.Length - 7); // take rest
				first [7] = MakeTupleType (second);
				return typeof (Tuple<,,,,,,,>).MakeGenericType (first);
			}
		}

		public T ExistentialPayload<T> (ISwiftExistentialContainer container)
		{

			return (T)ExistentialPayload (typeof (T), container);
		}

		public unsafe object ExistentialPayload (Type t, ISwiftExistentialContainer container)
		{
			var metadataOfContainer = container.ObjectMetadata;

			// Special case - if this is an EveryProtocol object then there *must* exist
			// a C# instance for it already.
			// Otherwise the user would get an instance of EveryProtocol which is not useful.
			if (metadataOfContainer.Handle == EveryProtocol.GetSwiftMetatype ().Handle) {
				return SwiftObjectRegistry.Registry.InstanceFromEveryProtocolHandle (container.Data0);
			} else {
				Type actualType;
				if (!SwiftTypeRegistry.Registry.TryGetValue (metadataOfContainer, out actualType)) {
					var name = $"kind {metadataOfContainer.Kind}";
					try {
						var nominalDesc = metadataOfContainer.GetNominalTypeDescriptor ();
						name = nominalDesc.GetFullName ();
					} catch { }

					throw new SwiftRuntimeException ($"Unknown C# type for swift type {name}");
				}
				if (!t.IsAssignableFrom (actualType))
					throw new SwiftRuntimeException ($"Expected type {t.Name} is not assignable from actual type {actualType.Name}");
				var payload = stackalloc byte [container.SizeOf];
				var payloadMemory = new IntPtr (payload);
				container.CopyTo (payloadMemory);
				return ToNet (payloadMemory, actualType);
			}
		}

		SwiftTupleMap TupleMapForException (Type t)
		{
			if (!t.IsTuple ())
				throw new ArgumentException ($"Expected a tuple but got {t.Name}");
			var types = TupleTypes (t).ToArray ();
			return SwiftTupleMap.FromTypes (types);
		}

		// a Medusa tuple is a swift tuple of the form: (T, error, Bool)
		// where the Bool indicates whether or not the value of T is valid or the error is valid
		// if the bool is true, the error is valid (throw condition).
		// if the bool is false, T contains a valid value.
		// Effectively the bool is a choice between two doors and the Medusa is behind one of them.
		// If you look behind the wrong door, you will turn to stone.

		public bool ExceptionReturnContainsSwiftError (IntPtr swiftMedusaTuple, Type t)
		{
			return ExceptionReturnContainsSwiftError (swiftMedusaTuple, TupleMapForException (t));
		}

		bool ExceptionReturnContainsSwiftError (IntPtr swiftMeduaTuple, SwiftTupleMap tMap)
		{
			//#if DEBUG
			//			Console.WriteLine("Given pointer " + swiftMeduaTuple.ToString("X8"));
			//			Console.WriteLine($"And a tuple map with ${tMap.Offsets.Length} offsets");
			//			foreach (int i in tMap.Offsets)
			//			{
			//				Console.Write($"{i}, ");
			//			}
			//			Console.WriteLine();
			//			Memory.Dump(swiftMeduaTuple, 40);
			//#endif
			var boolPtr = swiftMeduaTuple + tMap.Offsets [tMap.Offsets.Length - 1];
			//#if DEBUG
			//			string bps = boolPtr.ToString("X8");
			//			Console.WriteLine("Checking error at " + bps);
			//			Memory.Dump(boolPtr, 8);
			//			byte b = ReadByte(boolPtr);
			//			Console.WriteLine($"Read {b} at {bps}");
			//#endif
			return ReadByte (boolPtr) != 0;
		}

		public T GetErrorReturnValue<T> (IntPtr swiftMedusaTuple)
		{
			return (T)GetErrorReturnValue (swiftMedusaTuple, typeof (Tuple<T, IntPtr, bool>));
		}

		public object GetErrorReturnValue (IntPtr swiftMedusaTuple, Type t)
		{
			var tMap = TupleMapForException (t);
			if (ExceptionReturnContainsSwiftError (swiftMedusaTuple, tMap))
				throw new SwiftRuntimeException ("Can't retrieve return value from tuple containing a SwiftError.");
			if (IsSwiftObject (tMap.Types [0])) {
				return SwiftObjectRegistry.Registry.CSObjectForSwiftObject (swiftMedusaTuple + tMap.Offsets [0], tMap.Types [0]);
			} else {
				var retval = ToNet (swiftMedusaTuple + tMap.Offsets [0], tMap.Types [0]);
				return retval;
			}
		}

		public SwiftError GetErrorThrown (IntPtr swiftMedusaTuple, Type t)
		{
			var tMap = TupleMapForException (t);
			if (!ExceptionReturnContainsSwiftError (swiftMedusaTuple, tMap))
				throw new SwiftRuntimeException ("Can't retrieve SwiftError from tuple containing a value.");
			var handlePtr = Marshal.ReadIntPtr (swiftMedusaTuple + tMap.Offsets [tMap.Offsets.Length - 2]);
			return new SwiftError (handlePtr);
		}

		public void SetErrorThrown (IntPtr swiftMedusaTuple, SwiftError error, Type t)
		{
			var tMap = TupleMapForException (t);
			var boolPtr = swiftMedusaTuple + tMap.Offsets [tMap.Offsets.Length - 1];
			Write ((byte)1, boolPtr);
			var handlePtr = swiftMedusaTuple + tMap.Offsets [tMap.Offsets.Length - 2];
			Write (error.Handle, handlePtr);
		}

		public void SetErrorNotThrown (IntPtr swiftMedusaTuple, Type t)
		{
			var tMap = TupleMapForException (t);
			var boolPtr = swiftMedusaTuple + tMap.Offsets [tMap.Offsets.Length - 1];
			Write ((byte)0, boolPtr);
		}

		public void SetErrorNotThrownWithValue (IntPtr swiftMedusaTuple, Type t, object value)
		{
			var tMap = TupleMapForException (t);
			var boolPtr = swiftMedusaTuple + tMap.Offsets [tMap.Offsets.Length - 1];
			Write ((byte)0, boolPtr);
			ToSwift (tMap.Types [0], value, swiftMedusaTuple);
		}


		public SwiftException GetExceptionThrown (IntPtr swiftMeduaTuple, Type t)
		{
			var error = GetErrorThrown (swiftMeduaTuple, t);
			string message = $"Swift exception thrown: {error.Description}";
			return new SwiftException (message, error);
		}


		static object GetProperty (string propName, Type t, object o)
		{
			var pi = t.GetProperty (propName);
			if (pi == null)
				throw new SwiftRuntimeException (String.Format ("Object of type {0} is missing expected property {1}.",
				    t.Name, propName));
			return pi.GetValue (o);
		}



		static IntPtr MarshalBlitableStructToSwift (object o, IntPtr swiftDestinationMemory)
		{
			Marshal.StructureToPtr (o, swiftDestinationMemory, false);
			return swiftDestinationMemory;
		}

		static object MarshalBlitableStructToNet (IntPtr swiftSourceMemory, Type t)
		{
			var o = Marshal.PtrToStructure (swiftSourceMemory, t);
			return o;
		}

		public unsafe IntPtr MarshalNominalToSwift (Type t, object valueTYple, IntPtr swiftDestinationMemory)
		{
			if (valueTYple == null)
				throw new ArgumentNullException (nameof (valueTYple));
			var mt = Metatypeof (t);
			byte [] payload = PrepareValueType ((ISwiftValueType)valueTYple);
			var initWithCopy = GetNominalInitializeWithCopy (t);
			fixed (byte* src = payload) {
				initWithCopy (swiftDestinationMemory, new IntPtr (src), mt);
			}
			return swiftDestinationMemory;
		}

		public unsafe void NominalDestroy (Type t, byte* memoryContainingSwiftValueType)
		{
			NominalDestroy (t, new IntPtr (memoryContainingSwiftValueType));
		}

		public unsafe void NominalDestroy (ISwiftValueType obj)
		{
			var data = obj.SwiftData;
			if (data != null) {
				fixed (byte* p = data)
					NominalDestroy (obj.GetType (), p);
				obj.SwiftData = null;
			}
		}

		public void NominalDestroy (Type t, IntPtr memoryContainingSwiftValueTyple)
		{
			var mt = Metatypeof (t);
			var destroy = GetNominalDestroy (t);
			destroy (memoryContainingSwiftValueTyple, mt);
		}

		public unsafe void NominalDestroy (SwiftMetatype mt, byte [] memoryContainingSwiftValueType)
		{
			var destroy = GetNominalDestroy (mt);
			fixed (byte* pptr = memoryContainingSwiftValueType) {
				destroy (new IntPtr (pptr), mt);
			}
		}

		// value points to the return value from calling ToSwift
		public void ReleaseSwiftPointer (Type type, IntPtr value)
		{
			if (value == IntPtr.Zero)
				return;

			if (type.IsPrimitive)
				return; // nothing to release

			if (type == typeof (nint))
				return; // nothing to release

			if (type == typeof (nuint))
				return; // nothing to release
#if !TOM_SWIFTY
			if (type == typeof (nfloat))
				return; // nothing to release
#endif

			if (IsSwiftTrivialEnum (type))
				return; // nothing to release

			if (IsSwiftNominal (type) || type.IsTuple ()) {
				NominalDestroy (type, value);
				return;
			}

			if (IsSwiftObject (type)) {
				SwiftCore.Release (Marshal.ReadIntPtr (value));
				return;
			}

#if !TOM_SWIFTY
			if (IsObjCProtocol (type) || IsNSObject (type)) {
				SwiftCore.ReleaseObjC (value);
				return;
			}
#endif
			if (IsSwiftProtocol (type)) {
				return;
			}
			throw new SwiftRuntimeException ($"Don't know how to release a swift pointer to {type}.");
		}

#if __IOS__
        [MonoNativeFunctionWrapper]
#endif
		delegate void DestroyDelegate (IntPtr p, SwiftMetatype mt);

		static DestroyDelegate GetNominalDestroy (Type t)
		{
			var vt = ValueWitnessof (t);
			return GetNominalDestroy (vt);
		}

		static DestroyDelegate GetNominalDestroy (SwiftMetatype mt)
		{
			var vt = ValueWitnessOf (mt);
			return GetNominalDestroy (vt);
		}

		static DestroyDelegate GetNominalDestroy (SwiftValueWitnessTable vt)
		{
			var destroy = (DestroyDelegate)Marshal.GetDelegateForFunctionPointer (vt.DestroyOffset, typeof (DestroyDelegate));
			return destroy;
		}

		public unsafe IntPtr NominalInitializeWithCopy (ISwiftValueType obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));

			if (obj.SwiftData == null)
				throw new ObjectDisposedException (obj.GetType ().ToString ());

			fixed (byte* ptr = obj.SwiftData)
				return NominalInitializeWithCopy (obj.GetType (), ptr, obj.SwiftData.Length);
		}

		public unsafe IntPtr NominalInitializeWithCopy (Type t, byte* swiftDestinationMemory, int size)
		{
			return NominalInitializeWithCopy (t, new IntPtr (swiftDestinationMemory), size);
		}

		public IntPtr NominalInitializeWithCopy (Type t, IntPtr swiftDestinationMemory, int size)
		{
			unsafe {
				byte* tbuf = stackalloc byte [size];
				var tbufPtr = new IntPtr (tbuf);
				var mt = Metatypeof (t);
				var initWithCopy = GetNominalInitializeWithCopy (t);
				initWithCopy (tbufPtr, swiftDestinationMemory, mt);
				Memory.Copy (tbuf, (byte*)swiftDestinationMemory, size);
			}
			return swiftDestinationMemory;
		}

		public unsafe void NominalInitializeWithCopy (SwiftMetatype metadata, byte [] swiftDestinationMemory)
		{
			byte* tbuf = stackalloc byte [swiftDestinationMemory.Length];
			var tbufPtr = new IntPtr (tbuf);
			var initWithCopy = GetNominalInitializeWithCopy (metadata);
			fixed (byte* pptr = swiftDestinationMemory) {
				initWithCopy (tbufPtr, new IntPtr (pptr), metadata);
				Memory.Copy (tbuf, pptr, swiftDestinationMemory.Length);
			}
		}

#if __IOS__
        [MonoNativeFunctionWrapper]
#endif
		delegate IntPtr InitDelegate (IntPtr dst, IntPtr src, SwiftMetatype mt);

		static InitDelegate GetNominalInitializeWithCopy (Type t)
		{
			var vt = ValueWitnessof (t);
			return GetNominalInitializeWithCopy (vt);
		}

		static InitDelegate GetNominalInitializeWithCopy (SwiftMetatype mt)
		{
			var vt = ValueWitnessOf (mt);
			return GetNominalInitializeWithCopy (vt);
		}

		static InitDelegate GetNominalInitializeWithCopy (SwiftValueWitnessTable vt)
		{
			var initBuffer = (InitDelegate)Marshal.GetDelegateForFunctionPointer (vt.InitializeWithCopyOffset, typeof (InitDelegate));
			return initBuffer;
		}

		internal static SwiftValueWitnessTable ValueWitnessof (Type t)
		{
			if (t == null)
				throw new ArgumentNullException (nameof (t));
			var libvt = LibValueWitnessName (t);
			if (String.IsNullOrEmpty (libvt.Item2)) {
				return SwiftValueWitnessTable.FromType (t);
			} else {
				var wit = SwiftValueWitnessTable.FromDylibFile (libvt.Item1, DLOpenMode.Now, libvt.Item2);
				return wit ?? SwiftValueWitnessTable.FromType (t);
			}
		}

		internal static SwiftValueWitnessTable ValueWitnessOf (SwiftMetatype mt)
		{
			return SwiftValueWitnessTable.FromType (mt);
		}

		static Tuple<string, string> LibValueWitnessName (Type t)
		{
			if (IsSwiftEnum (t)) {
				var attr = t.GetCustomAttributes ().OfType<SwiftEnumTypeAttribute> ().FirstOrDefault ();
				if (attr == null) {
					throw new SwiftRuntimeException ($"Expected a swift enum {t.Name} to have a {typeof (SwiftEnumTypeAttribute).Name} attribute.");
				}
				return new Tuple<string, string> (attr.LibraryName, attr.WitnessTable);
			} else if (IsSwiftStruct (t)) {
				var attr = t.GetCustomAttributes ().OfType<SwiftStructAttribute> ().FirstOrDefault ();
				if (attr == null) {
					throw new SwiftRuntimeException ($"Expected struct {t.Name} to have a {typeof (SwiftStructAttribute).Name} attribute.");
				}
				return new Tuple<string, string> (attr.LibraryName, attr.WitnessTable);
			} else {
				return new Tuple<string, string> (null, null);
			}
		}

		static object [] nomCtorArgs = new object [] { SwiftValueTypeCtorArgument.None };
		public ISwiftValueType MarshalNominalToNet (IntPtr swiftSourceMemory, Type t, bool owns)
		{
			var ci = GetValueTypeCtor (t);
			if (ci == null)
				throw new SwiftRuntimeException ($"Value type {t.Name} is missing a {typeof (SwiftValueTypeCtorArgument).Name} constructor.");
			var o = ci.Invoke (nomCtorArgs) as ISwiftValueType;
			if (o == null)
				throw new SwiftRuntimeException ($"Supposed value type {t.Name} does not implement {typeof (ISwiftValueType).Name}.");

			PrepareValueType (o);
			var payload = o.SwiftData; 
			Marshal.Copy (swiftSourceMemory, payload, 0, payload.Length);

			if (!owns)
				NominalInitializeWithCopy (t, swiftSourceMemory, payload.Length);

			return o;
		}

		object MarshalEnumToNet (FieldInfo fi, IntPtr swiftSourceMemory)
		{
			var swiftRawType = SwiftEnumMapper.EnumHasRawValue (fi.FieldType) ? SwiftEnumMapper.RawValueType (fi.FieldType) : typeof (int);
			switch (Type.GetTypeCode (swiftRawType)) {
			case TypeCode.Byte:
				return (object)ReadByte (swiftSourceMemory);
			case TypeCode.SByte:
				return (object)ReadSByte (swiftSourceMemory);
			case TypeCode.Int16:
				return (object)ReadShort (swiftSourceMemory);
			case TypeCode.UInt16:
				return (object)ReadUShort (swiftSourceMemory);
			case TypeCode.Int32:
				return (object)ReadInt (swiftSourceMemory);
			case TypeCode.UInt32:
				return (object)ReadUInt (swiftSourceMemory);
			case TypeCode.Int64:
				return (object)ReadLong (swiftSourceMemory);
			case TypeCode.UInt64:
				return (object)ReadULong (swiftSourceMemory);
			case TypeCode.Char:
				return (object)ReadChar (swiftSourceMemory);
			default:
				throw new SwiftRuntimeException (String.Format ("Unknown type code {0} in enum.",
					Type.GetTypeCode (swiftRawType)));
			}
		}

		void MarshalScalarFieldToNet (object o, FieldInfo fi, IntPtr swiftSourceMemory)
		{
			var value = MarshalScalarToNet (swiftSourceMemory, fi.FieldType);
			fi.SetValue (o, value);
		}

		object MarshalScalarToNet (IntPtr swiftSourceMemory, Type t)
		{
			switch (Type.GetTypeCode (t)) {
			case TypeCode.Boolean:
				return ReadByte (swiftSourceMemory) != 0;
			case TypeCode.Byte:
				return ReadByte (swiftSourceMemory);
			case TypeCode.SByte:
				return ReadSByte (swiftSourceMemory);
			case TypeCode.Int16:
				return ReadShort (swiftSourceMemory);
			case TypeCode.UInt16:
				return ReadUShort (swiftSourceMemory);
			case TypeCode.Int32:
				return ReadInt (swiftSourceMemory);
			case TypeCode.UInt32:
				return ReadUInt (swiftSourceMemory);
			case TypeCode.Int64:
				return ReadLong (swiftSourceMemory);
			case TypeCode.UInt64:
				return ReadULong (swiftSourceMemory);
			case TypeCode.Char:
				return ReadChar (swiftSourceMemory);
			case TypeCode.Single:
				return ReadFloat (swiftSourceMemory);
			case TypeCode.Double:
				return ReadDouble (swiftSourceMemory);
			default:
				if (t == typeof (IntPtr) || t == typeof (UIntPtr)) {
					return Marshal.PtrToStructure (swiftSourceMemory, t);
				}
				throw new SwiftRuntimeException ("Illegal type code " + Type.GetTypeCode (t));
			}
		}

		static unsafe byte ReadByte (IntPtr p)
		{
			unsafe { return *((byte*)p); }
		}

		static unsafe sbyte ReadSByte (IntPtr p)
		{
			unsafe { return *((sbyte*)p); }
		}

		static unsafe short ReadShort (IntPtr p)
		{
			unsafe { return *((short*)p); }
		}

		static unsafe ushort ReadUShort (IntPtr p)
		{
			unsafe { return *((ushort*)p); }
		}

		static unsafe int ReadInt (IntPtr p)
		{
			unsafe { return *((int*)p); }
		}

		static unsafe uint ReadUInt (IntPtr p)
		{
			unsafe { return *((uint*)p); }
		}

		static unsafe long ReadLong (IntPtr p)
		{
			unsafe { return *((long*)p); }
		}

		static unsafe ulong ReadULong (IntPtr p)
		{
			unsafe { return *((ulong*)p); }
		}

		static unsafe char ReadChar (IntPtr p)
		{
			unsafe { return *((char*)p); }
		}

		static unsafe float ReadFloat (IntPtr p)
		{
			unsafe { return *((float*)p); }
		}

		static unsafe double ReadDouble (IntPtr p)
		{
			unsafe { return *((double*)p); }
		}

		static unsafe nint ReadNint (IntPtr p)
		{
			unsafe {
				return *((nint*)p);
			}
		}

		static unsafe nuint ReadNuint (IntPtr p)
		{
			unsafe {
				return *((nuint*)p);
			}
		}
#if !TOM_SWIFTY
		static unsafe nfloat ReadNfloat (IntPtr p)
		{
			unsafe {
				return *((nfloat *)p);
			}
		}
#endif


		static IntPtr MarshalScalarToSwift (Type fieldType, object value, IntPtr swiftDestinationMemory)
		{
			// see https://msdn.microsoft.com/en-us/library/system.type.isprimitive.aspx
			var typeCode = Type.GetTypeCode (fieldType);
			switch (typeCode) {
			case TypeCode.Boolean:
				Write ((bool)value ? (byte)1 : (byte)0, swiftDestinationMemory);
				break;
			case TypeCode.Byte:
				Write ((byte)value, swiftDestinationMemory);
				break;
			case TypeCode.SByte:
				Write ((sbyte)value, swiftDestinationMemory);
				break;
			case TypeCode.Int16:
				Write ((short)value, swiftDestinationMemory);
				break;
			case TypeCode.UInt16:
				Write ((ushort)value, swiftDestinationMemory);
				break;
			case TypeCode.Int32:
				Write ((int)value, swiftDestinationMemory);
				break;
			case TypeCode.UInt32:
				Write ((uint)value, swiftDestinationMemory);
				break;
			case TypeCode.Int64:
				Write ((long)value, swiftDestinationMemory);
				break;
			case TypeCode.UInt64:
				Write ((ulong)value, swiftDestinationMemory);
				break;
			case TypeCode.Char:
				Write ((char)value, swiftDestinationMemory);
				break;
			case TypeCode.Single:
				Write ((float)value, swiftDestinationMemory);
				break;
			case TypeCode.Double:
				Write ((double)value, swiftDestinationMemory);
				break;
			default:
				if (fieldType == typeof (IntPtr) || fieldType == typeof (UIntPtr)) {
					Marshal.StructureToPtr (value, swiftDestinationMemory, false);
				} else {
					throw new SwiftRuntimeException ($"Illegal type code {Enum.GetName (typeof (TypeCode), typeCode)} for {value} (value Type: {value.GetType ().Name}, fieldType {fieldType.Name})");
				}
				break;
			}
			return swiftDestinationMemory;
		}

		static unsafe void Write (nint val, IntPtr p)
		{
			if (Marshal.SizeOf (typeof (nint)) == 4) {
				Write ((int)val, p);
			} else {
				Write ((long)val, p);
			}
		}

		static unsafe void Write (nuint val, IntPtr p)
		{
			if (Marshal.SizeOf (typeof (nuint)) == 4) {
				Write ((uint)val, p);
			} else {
				Write ((ulong)val, p);
			}
		}

#if !TOM_SWIFTY
		static unsafe void Write (nfloat val, IntPtr p)
		{
			if (sizeof (nfloat) == sizeof (float)) {
				Write ((float)val, p);
			} else {
				Write ((double)val, p);
			}
		}
#endif

		static unsafe void Write (byte val, IntPtr p)
		{
			*((byte*)p) = val;
		}

		static unsafe void Write (sbyte val, IntPtr p)
		{
			*((sbyte*)p) = val;
		}

		static unsafe void Write (short val, IntPtr p)
		{
			*((short*)p) = val;
		}

		static unsafe void Write (ushort val, IntPtr p)
		{
			*((ushort*)p) = val;
		}

		static unsafe void Write (int val, IntPtr p)
		{
			*((int*)p) = val;
		}

		static unsafe void Write (uint val, IntPtr p)
		{
			*((uint*)p) = val;
		}

		static unsafe void Write (long val, IntPtr p)
		{
			*((long*)p) = val;
		}

		static unsafe void Write (ulong val, IntPtr p)
		{
			*((ulong*)p) = val;
		}

		static unsafe void Write (void *val, IntPtr p)
		{
			*((void**)p) = (void*)val;
		}

		static unsafe void Write (char val, IntPtr p)
		{
			*((char*)p) = val;
		}


		static unsafe void Write (float val, IntPtr p)
		{
			*((float*)p) = val;
		}

		static unsafe void Write (double val, IntPtr p)
		{
			*((double*)p) = val;
		}

		static bool IsSwiftTrivialEnum (Type t)
		{
			return t.IsEnum && t.GetCustomAttributes ().OfType<SwiftValueTypeAttribute> () != null;
		}

		internal static bool IsSwiftNominal (Type t)
		{
			return t.IsClass && t.GetCustomAttributes ().OfType<SwiftValueTypeAttribute> ().FirstOrDefault () != null;
		}


		internal static bool IsSwiftEnum (Type t)
		{
			return t.IsClass && t.GetCustomAttributes ().OfType<SwiftEnumTypeAttribute> ().FirstOrDefault () != null;
		}

		internal static bool IsSwiftStruct (Type t)
		{
			return t.IsClass && t.GetCustomAttributes ().OfType<SwiftStructAttribute> ().FirstOrDefault () != null;
		}

		static bool IsSwiftObject (Type t)
		{
			return typeof (ISwiftObject).IsAssignableFrom (t);
		}

		static bool IsSwiftError (Type t)
		{
			return typeof (SwiftError).IsAssignableFrom (t);
		}

#if !TOM_SWIFTY
		static bool IsNSObject (Type t)
		{
			return typeof (NSObject).IsAssignableFrom (t);
		}

		static bool IsObjCProtocol (Type t)
		{
			return t.GetCustomAttributes ().OfType<ProtocolAttribute> ().FirstOrDefault () != null;
		}

		static object GetINativeObject (IntPtr p, Type t, bool owns)
		{
			// need to call static INativeObject GetINativeObject (IntPtr ptr, bool owns, Type target_type, Type implementation = null)
			// in class ObjCRuntime
			var method = typeof (ObjCRuntime.Runtime).GetMethod ("GetINativeObject", BindingFlags.NonPublic | BindingFlags.Static);
			if (method == null)
				throw new SwiftRuntimeException ("Unable to find method GetINativeObject in ObjCRuntime.Runtime");
			return method.Invoke (null, new object [] { p, owns, t, null });
		}
#endif

		public Type[] GetAssociatedTypes (SwiftMetatype implementingType, Type interfaceType, int expectedAssociatedTypeCount)
		{
			if (expectedAssociatedTypeCount <= 0)
				throw new ArgumentOutOfRangeException (nameof (expectedAssociatedTypeCount));
			Exceptions.ThrowOnNull (interfaceType, nameof (interfaceType));

			var protoDescriptor = SwiftProtocolTypeAttribute.DescriptorForType (interfaceType);
			var witness = SwiftCore.ConformsToSwiftProtocol (implementingType, protoDescriptor);
			var requirementsBase = protoDescriptor.GetProtocolRequirementsBaseDescriptor ();

			var finalTypes = new Type [expectedAssociatedTypeCount];
			for (int i = 0; i < expectedAssociatedTypeCount; i++) {
				var assocDesc = GetNthAssociatedTypeDesc (protoDescriptor, i);
				if (!assocDesc.IsValid)
					throw new SwiftRuntimeException ($"In looking for the associated type at index {i}, the associated type descriptor for protocol {protoDescriptor.GetFullName ()} was invalid.");
				var metadata = SwiftCore.AssociatedTypeMetadataRequest (implementingType, witness, requirementsBase, assocDesc);
				Type csType = null;
				if (!SwiftTypeRegistry.Registry.TryGetValue (metadata, out csType)) {
					var typeName = "";
					if (metadata.HasNominalDescriptor) {
						var nomDesc = metadata.GetNominalTypeDescriptor ();
						typeName = nomDesc.GetFullName ();
					} else {
						typeName = "with kind " + ((int)metadata.Kind).ToString ("X4");
					}
					throw new SwiftRuntimeException ($"Unable to get C# type for swift type {typeName}");
				}
				finalTypes [i] = csType;
			}
			return finalTypes;
		}

		SwiftAssociatedTypeDescriptor GetNthAssociatedTypeDesc (SwiftNominalTypeDescriptor desc, int index)
		{
			int currIndex = 0;
			for (int i = 0; i < desc.GetAssociatedTypesCount (); i++) {
				var assocDesc = desc.GetAssociatedTypeDescriptor (i);
				if (assocDesc.Kind == ProtocolRequirementsKind.AssociatedTypeAccessFunction) {
					if (currIndex == index)
						return assocDesc;
					currIndex++;
				}
			}
			return new SwiftAssociatedTypeDescriptor (IntPtr.Zero);
		}

		public static bool ImplementsAll (object o, params Type [] types)
		{
			if (o == null)
				throw new ArgumentNullException (nameof (o));
			if (types.Length == 0)
				throw new ArgumentException ("Requires one or more types");
			var oType = o.GetType ();
			foreach (var type in types) {
				if (!type.IsInterface)
					throw new ArgumentException ("Each type must be an interface type", nameof (types));
				if (!type.IsAssignableFrom (oType))
					return false;
			}
			return true;
		}

		public static object ThrowIfNotImplementsAll (object o, params Type [] types)
		{
			if (o == null)
				throw new ArgumentNullException (nameof (o));
			if (!ImplementsAll (o, types)) {
				StringBuilder sb = new StringBuilder ();
				sb.Append (types [0].Name);
				for (int i=1; i < types.Length; i++) {
					sb.Append (", ").Append (types [i].Name);
				}

				throw new SwiftRuntimeException ($"Object does not implement one or more required interfaces: {sb.ToString ()}");
			}
			return o;
		}

		internal static int RoundUpToAlignment (int value, int align)
		{
			return (value + align - 1) / align * align;
		}

		// singleton nonsense
		StructMarshal ()
		{
		}

		static StructMarshal marshal = new StructMarshal ();
		public static StructMarshal Marshaler {
			get { return marshal; }
		}
	}
}
