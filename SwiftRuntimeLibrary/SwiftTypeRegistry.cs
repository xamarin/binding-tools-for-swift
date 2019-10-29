// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public sealed class SwiftTypeRegistry {
		static SwiftTypeRegistry registry = new SwiftTypeRegistry ();
		object registryLock = new object ();
		Dictionary<SwiftMetatype, Type> primaryCache = new Dictionary<SwiftMetatype, Type> ();
		Dictionary<string, Type> secondaryCache = new Dictionary<string, Type> ();
		HashSet<Assembly> cachedAssemblies = new HashSet<Assembly> ();

		SwiftTypeRegistry ()
		{
			CachePrimitives ();
		}

		public bool TryGetValue (SwiftMetatype swiftType, out Type csType)
		{
			if (swiftType.Handle == EveryProtocol.GetSwiftMetatype ().Handle) {
				csType = null;
				return false;
			}
			lock (registryLock) {
				if (primaryCache.TryGetValue (swiftType, out csType))
					return true;
				if (IsNamedType (swiftType) && TrySecondaryCache (swiftType, out csType))
					return true;
				else if (swiftType.Kind == MetatypeKind.Tuple) {
					csType = GetTupleType (swiftType);
					if (csType != null)
						primaryCache.Add (swiftType, csType);
				} else if (swiftType.Kind == MetatypeKind.Optional) {
					csType = GetOptionalType (swiftType);
					if (csType != null)
						primaryCache.Add (swiftType, csType);
				} else if (swiftType.Kind == MetatypeKind.Function) {
					csType = GetFunctionType (swiftType);
					if (csType != null)
						primaryCache.Add (swiftType, csType);
				}
				if (csType != null)
					return true;
				return false;
			}
		}

		void CachePrimitives ()
		{
			lock (registryLock) {
				foreach (var swiftCsPair in StructMarshal.PrimitiveTypeMap ())
					primaryCache.Add (swiftCsPair.Item1, swiftCsPair.Item2);
			}
		}

		Type GetFunctionType (SwiftMetatype swiftType)
		{
			if (swiftType.FunctionHasParameterFlags ())
				throw new NotSupportedException ("functions with parameter flags not supported yet");
			var types = GetFunctionParameterTypes (swiftType);
			if (types == null)
				return null;
			if (swiftType.FunctionHasReturn ()) {
				Type returnType;

				if (!TryGetValue (swiftType.GetFunctionReturnType (), out returnType))
					return null;
				return FunctionTypeFromTypes (types, returnType);
			} else {
				return ActionTypeFromTypes (types);
			}
		}

		Type ActionTypeFromTypes (Type[] types)
		{
			var actionType = ActionTypeOfLength (types.Length);
			return types.Length == 0 ? actionType :
				actionType.MakeGenericType (types);
		}

		Type ActionTypeOfLength (int length)
		{
			switch (length) {
			case 0: return typeof (Action);
			case 1: return typeof (Action<>);
			case 2: return typeof (Action<,>);
			case 3: return typeof (Action<,,>);
			case 4: return typeof (Action<,,,>);
			case 5: return typeof (Action<,,,,>);
			case 6: return typeof (Action<,,,,,>);
			case 7: return typeof (Action<,,,,,,>);
			case 8: return typeof (Action<,,,,,,,>);
			case 9: return typeof (Action<,,,,,,,,>);
			case 10: return typeof (Action<,,,,,,,,,,>);
			case 11: return typeof (Action<,,,,,,,,,,,>);
			case 12: return typeof (Action<,,,,,,,,,,,,>);
			case 13: return typeof (Action<,,,,,,,,,,,,,>);
			case 14: return typeof (Action<,,,,,,,,,,,,,,>);
			case 15: return typeof (Action<,,,,,,,,,,,,,,,>);
			default:
				return null;
			}
		}

		Type FunctionTypeFromTypes (Type[] types, Type returnType)
		{
			var funcType = GetFunctionTypeOfLength (types.Length);
			if (funcType == null)
				return null;
			var finalTypes = new Type [types.Length + 1];
			Array.Copy (types, finalTypes, types.Length);
			finalTypes [types.Length] = returnType;
			return funcType.MakeGenericType (finalTypes);
		}

		Type GetFunctionTypeOfLength (int argCount)
		{
			switch (argCount) {
			case 0: return typeof (Func<>);
			case 1: return typeof (Func<,>);
			case 2: return typeof (Func<,,>);
			case 3: return typeof (Func<,,,>);
			case 4: return typeof (Func<,,,,>);
			case 5: return typeof (Func<,,,,,>);
			case 6: return typeof (Func<,,,,,,>);
			case 7: return typeof (Func<,,,,,,,>);
			case 8: return typeof (Func<,,,,,,,,>);
			case 9: return typeof (Func<,,,,,,,,,>);
			case 10: return typeof (Func<,,,,,,,,,,>);
			case 11: return typeof (Func<,,,,,,,,,,,>);
			case 12: return typeof (Func<,,,,,,,,,,,,>);
			case 13: return typeof (Func<,,,,,,,,,,,,,>);
			case 14: return typeof (Func<,,,,,,,,,,,,,,>);
			case 15: return typeof (Func<,,,,,,,,,,,,,,,>);
			case 16: return typeof (Func<,,,,,,,,,,,,,,,,>);
			default:
				return null;
			}
		}

		Type[] GetFunctionParameterTypes (SwiftMetatype swiftType)
		{
			var count = swiftType.GetFunctionParameterCount ();
			var types = new Type [count];
			for (int i=0; i < count; i++) {
				Type type;
				if (!TryGetValue (swiftType.GetFunctionParameter (i), out type))
					return null;
				types [i] = type;
			}
			return types;
		}

		Type GetOptionalType (SwiftMetatype swiftType)
		{
			var swiftOpt = swiftType.GetOptionalBoundGeneric ();
			var csOptType = new Type [1];
			Type boundType;
			if (!TryGetValue (swiftOpt, out boundType))
				return null;
			csOptType [0] = boundType;
			return typeof (SwiftOptional<>).MakeGenericType (csOptType);
		}

		Type GetTupleType (SwiftMetatype swiftType)
		{
			var swiftTupleTypes = swiftType.GetTupleMetatypes ();
			var csTupleTypes = new Type [swiftTupleTypes.Length];
			for (int i=0; i < swiftTupleTypes.Length; i++) {
				Type csType;
				if (!TryGetValue (swiftTupleTypes [i], out csType))
					return null;
				csTupleTypes [i] = csType;
			}
			return StructMarshal.Marshaler.MakeTupleType (csTupleTypes);
		}

		bool TrySecondaryCache (SwiftMetatype swiftType, out Type csType)
		{
			var nominalType = swiftType.GetNominalTypeDescriptor ();
			var swiftTypeName = nominalType.GetFullName ();

			if (!secondaryCache.ContainsKey (swiftTypeName)) {
				LoadSecondaryCacheFromAssemblies ();
			}

			if (!secondaryCache.TryGetValue (swiftTypeName, out csType))
				return false;

			if (csType.IsGenericTypeDefinition) {
				if (!nominalType.IsGeneric ())
					throw new SwiftRuntimeException ("Swift type is not generic where C# type is.");
				csType = MakeGenericType (swiftType, csType);
			}
			primaryCache.Add (swiftType, csType);

			return true;
		}

		Type MakeGenericType (SwiftMetatype swiftType, Type genericType)
		{
			var count = swiftType.GenericArgumentCount;
			var csCount = genericType.GetTypeInfo ().GetGenericArguments ().Length;
			if (count != csCount)
				throw new SwiftRuntimeException ($"Expected type {genericType.Name} to have {count} generic arguments, but it has {csCount}");
			var typeArguments = new Type [count];
			for (int i=0; i < count; i++) {
				var genericSwiftType = swiftType.GetGenericMetatype (i);
				Type theType;
				if (!TryGetValue (genericSwiftType, out theType))
					return null;
				typeArguments [i] = theType;
			}
			return genericType.MakeGenericType (typeArguments);
		}

		static bool IsNamedType (SwiftMetatype swiftType)
		{
			switch (swiftType.Kind) {
			case MetatypeKind.Class:
			case MetatypeKind.Enum:
			case MetatypeKind.Struct:
				return true;
			default:
				return false;
			}
		}

		void LoadSecondaryCacheFromAssemblies ()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
				if (cachedAssemblies.Contains (assembly))
					continue;
				cachedAssemblies.Add (assembly);
				if (NoSwiftRuntimeReferences (assembly))
					continue;
				foreach (var t in assembly.GetTypes ()) {
					string swiftTypeName;
					if (SwiftTypeNameAttribute.TryGetSwiftName (t, out swiftTypeName)) {
						secondaryCache.Add (swiftTypeName, t);
					}
				}
			}
		}

#if TOM_SWIFTY
		static string runtimeAssemblyName = "SwiftRuntimeLibrary";
#elif __IOS__
		static string runtimeAssemblyName = "SwiftRuntimeLibrary.iOS";
#elif __WATCHOS__
		static string runtimeAssemblyName = "SwiftRuntimeLibrary.watchOS";
#elif __TVS__
		static string runtimeAssemblyName = "SwiftRuntimeLibrary.tvOS";
#else
		static string runtimeAssemblyName = "SwiftRuntimeLibrary.Mac";
#endif


		static bool NoSwiftRuntimeReferences (Assembly assembly)
		{
			foreach (var reference in assembly.GetReferencedAssemblies ()) {
				if (reference.Name == runtimeAssemblyName)
					return false;
			}
			return true;
		}

		public static SwiftTypeRegistry Registry => registry;
	}
}
