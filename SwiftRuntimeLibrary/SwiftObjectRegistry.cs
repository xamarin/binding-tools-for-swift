// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if !TOM_SWIFTY
using Foundation;
#endif
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	public class SwiftObjectRegistry {
		public const string kXamarinFactoryMethodName = "XamarinFactory";
		static SwiftObjectRegistry registry = new SwiftObjectRegistry ();
		object registryLock = new object ();

		Dictionary<IntPtr, GCHandle> registeredObjects = new Dictionary<IntPtr, GCHandle> ();


		SwiftObjectRegistry ()
		{
		}

		public void Add (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			lock (registryLock) {
				if (registeredObjects.ContainsKey (obj.SwiftObject))
					return;
				SwiftCore.RetainWeak (obj.SwiftObject);
				// taking a weak reference will ensure that the object
				// memory sticks around after being disposed in swift
				var handle = GCHandle.Alloc (obj, GCHandleType.Weak);
				registeredObjects.Add (obj.SwiftObject, handle);
			}
		}

		void Remove (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			lock (registryLock) {
				if (registeredObjects.ContainsKey (obj.SwiftObject)) {
					registeredObjects.Remove (obj.SwiftObject);
				}
			}
		}

		public void RemoveAndWeakRelease (ISwiftObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			var p = obj.SwiftObject;
			if (p == IntPtr.Zero)
				return;
			bool release;
			lock (registryLock) {
				release = registeredObjects.Remove (p);
			}
			if (release)
				SwiftCore.ReleaseWeak (p);
		}

		public bool Contains (IntPtr p)
		{
			lock (registryLock) {
				return registeredObjects.ContainsKey (p);
			}
		}

		public Type RegisteredTypeOf (IntPtr p)
		{
			lock (registryLock) {
				GCHandle handle;
				ISwiftObject obj = null;
				if (registeredObjects.TryGetValue (p, out handle)) {
					obj = handle.Target as ISwiftObject;
					if (obj != null)
						return obj.GetType ();
				}
				return null;
			}
		}

		internal ISwiftObject ExistingCSObjectForSwiftObject (IntPtr p, Type ofType)
		{
			ISwiftObject csimpl = null;
			GCHandle handle;
			lock (registryLock) {
				if (!registeredObjects.TryGetValue (p, out handle)) {
					throw new SwiftRuntimeException ($"Failed to find an existing object of type {ofType.Name} for handle {p.ToString ("X8")}");
				} else {
					csimpl = handle.Target as ISwiftObject;
					if (csimpl == null) {
						throw new SwiftRuntimeException ($"GCHandle for swift object has gone stale");
					}
					if (!ofType.IsAssignableFrom (csimpl.GetType ())) {
						throw new SwiftRuntimeException ("Registry mismatch: expected an object of type ofType.Name but got csimpl.GetType().Name");
					}
				}
				return csimpl;
			}
		}

		internal T ExistingCSObjectForSwiftObject<T> (IntPtr p) where T : class, ISwiftObject
		{
			if (p == IntPtr.Zero)
				return null;
			object o = ExistingCSObjectForSwiftObject (p, typeof (T));
			T retval = o as T;
			if (retval == null) {
				throw new SwiftRuntimeException (String.Format ("Registry mismatch: expected an object of type {0} but got {1}",
				    typeof (T).Name, o.GetType ().Name));
			}
			return retval;
		}


		internal ISwiftObject CSObjectForSwiftObject (IntPtr p, Type ofType, bool owns = false)
		{
			ISwiftObject csimpl = null;
			GCHandle handle;
			lock (registryLock) {
				if (!registeredObjects.TryGetValue (p, out handle)) {
					csimpl = MakeFromWholeCloth (p, ofType);
					if (csimpl == null) { // should never happen
						throw new SwiftRuntimeException ($"Failed to make an object of type {ofType.Name}.");
					}
				} else {
					csimpl = handle.Target as ISwiftObject;
					if (csimpl == null) {
						throw new SwiftRuntimeException ($"GCHandle for swift object has gone stale");
					}
					if (!ofType.IsAssignableFrom (csimpl.GetType ())) {
						throw new SwiftRuntimeException ($"Registry mismatch: expected an object of type {ofType} but got {csimpl.GetType ()}");
					}
				}
			}
			if (owns)
				SwiftCore.Release (csimpl.SwiftObject);
			return csimpl;
		}

		public T CSObjectForSwiftObject<T> (IntPtr p) where T : class, ISwiftObject
		{
			if (p == IntPtr.Zero)
				return null;
			object o = CSObjectForSwiftObject (p, typeof (T));
			if (o == null) {
				throw new SwiftRuntimeException (String.Format ("Registry failed to convert a SwiftObject representation to a C# representation of type {0}.",
				    typeof (T).Name));
			}
			T retval = o as T;
			if (retval == null) {
				throw new SwiftRuntimeException (String.Format ("Registry mismatch: expected an object of type {0} but got {1}",
				    typeof (T).Name, o.GetType ().Name));
			}
			return retval;
		}

		public T CSObjectForSwiftObjectRTChecked<T> (IntPtr p)
		{
			if (p == IntPtr.Zero)
				return default (T);
			object o = CSObjectForSwiftObject (p, typeof (T));
			if (o == null) {
				throw new SwiftRuntimeException (String.Format ("Registry failed to convert a SwiftObject representation to a C# representation of type {0}.",
				    typeof (T).Name));
			}
			T retval = (T)o;
			return retval;
		}


		ISwiftObject MakeFromWholeCloth (IntPtr p, Type ofType)
		{
			// Whole cloth is "pure fabrication" or made from completely new material.
			// In this case, we have a handle to a completely new Swift object and we need to
			// make a new C# object from that.
			// https://www.merriam-webster.com/dictionary/whole%20cloth

			var mi = GetFactoryMethod (ofType, ofType);
			if (mi == null)
				throw new SwiftRuntimeException ($"Unable to find factory method kXamarinFactoryMethodName in {ofType.Name}.");
			object invokedObj = null;

			// Invoking the constructor will do an automatic Add - no need to do it here.
			if (ofType.IsGenericType) {
				invokedObj = mi.Invoke (null, new object [] { p, ofType.GetGenericArguments () });
			} else {
				invokedObj = mi.Invoke (null, new object [] { p });
			}
			if (invokedObj == null)
				throw new SwiftRuntimeException ("Registry factory method returned null.");
			if (!ofType.IsAssignableFrom (invokedObj.GetType ())) {
				throw new SwiftRuntimeException ($"Registry mismatch: expected a factory constructed object of type {ofType.Name} but got {invokedObj.GetType ().Name}.");
			}

			return invokedObj as ISwiftObject;
		}


		static MethodInfo GetFactoryMethod (Type t, Type returnType)
		{
			// looking for a method:
			// public static someType kXamarinFactoryMethodName(IntPtr p) { }
			// --or--
			// public static object kXamarinFactoryMethodName(IntPtr p, Type[] genericTypes)
			// such that returnType is assignable from someType
			if (t.IsGenericType) {
				return t.GetMethods ().FirstOrDefault (mi =>
								       mi.IsStatic && mi.IsPublic && mi.Name == kXamarinFactoryMethodName &&
								       mi.ReturnType == typeof (object) &&
								       CorrectFactoryParameters (mi.GetParameters (), true));
			} else {
				return t.GetMethods ().FirstOrDefault (mi =>
						mi.IsStatic && mi.IsPublic && mi.Name == kXamarinFactoryMethodName &&
						returnType.IsAssignableFrom (mi.ReturnType) &&
								       CorrectFactoryParameters (mi.GetParameters (), false));
			}
		}

		static bool CorrectFactoryParameters (ParameterInfo [] parms, bool isGeneric)
		{
			if (isGeneric) {
				return parms.Length == 2 && parms [0].ParameterType == typeof (IntPtr) && parms [1].ParameterType == typeof (Type []);
			} else {
				return parms.Length == 1 && parms [0].ParameterType == typeof (IntPtr);
			}
		}

		Dictionary<object, List<BaseProxy>> proxies = new Dictionary<object, List<BaseProxy>> ();

		public ISwiftExistentialContainer ExistentialContainerForProtocols (object implementation, params Type[] types)
		{
			if (types.Length > SwiftExistentialContainer0.MaximumContainerSize)
				throw new ArgumentOutOfRangeException (nameof(types), $"Exceeded the limit of {SwiftExistentialContainer0.MaximumContainerSize} types in an existential container.");
			var protocolWitnessTables = new IntPtr [types.Length];
			EveryProtocol every = null;
			for (int i=0; i < types.Length; i++) {
				if (!types[i].IsInterface) {
					throw new SwiftRuntimeException ($"Type {types [i].Name} is not an interface.");
				}
				// pre-cache the proxy
				every = every ?? ((BaseProxy)ProxyForInterface (types [i], implementation)).EveryProtocol;
				protocolWitnessTables[i] = StructMarshal.Marshaler.ProtocolWitnessof (types[i], typeof (EveryProtocol));
				if (protocolWitnessTables [i] == IntPtr.Zero)
					throw new SwiftRuntimeException ($"Unable to find protocol witness table for interface type {types[i].Name}");
			}
			return MakeExistentialContainer (every, protocolWitnessTables);
		}

		ISwiftExistentialContainer MakeExistentialContainer (EveryProtocol every, IntPtr[] protocolWitnessTables)
		{
			ISwiftExistentialContainer container = ContainerOfSize (protocolWitnessTables.Length);
			container.Data0 = every.SwiftObject;
			container.ObjectMetadata = EveryProtocol.GetSwiftMetatype ();
			for (int i=0; i < container.Count; i++) {
				container [i] = protocolWitnessTables [i];
			}
			return container;
		}

		static ISwiftExistentialContainer ContainerOfSize(int n)
		{
			switch (n) {
			case 0:
				return new SwiftExistentialContainer0 ();
			case 1:
				return new SwiftExistentialContainer1 ();
			case 2:
				return new SwiftExistentialContainer2 ();
			case 3:
				return new SwiftExistentialContainer3 ();
			case 4:
				return new SwiftExistentialContainer4 ();
			case 5:
				return new SwiftExistentialContainer5 ();
			case 6:
				return new SwiftExistentialContainer6 ();
			case 7:
				return new SwiftExistentialContainer7 ();
			case 8:
				return new SwiftExistentialContainer8 ();
			default:
				throw new ArgumentOutOfRangeException (nameof (n));
			}
		}

		public T ProxyForInterface<T> (T interfaceImpl)
		{
			if (typeof (BaseProxy).IsAssignableFrom (interfaceImpl.GetType ()))
				return interfaceImpl;
			return (T)ProxyForInterface (typeof (T), interfaceImpl);
		}

		public EveryProtocol EveryProtocolForInterface<T> (T interfaceImpl)
		{
			var baseProxy = ProxyForInterface (typeof (T), interfaceImpl) as BaseProxy;
			return baseProxy.EveryProtocol;
		}

		object ProxyForInterface(Type interfaceType, object interfaceImpl)
		{
			if (!interfaceType.IsInterface)
				throw new ArgumentException ($"implementation object type {interfaceType.Name} is not an an interface", nameof (interfaceType));
			List<BaseProxy> proxyList = null;
			lock (registryLock) {
				if (!proxies.TryGetValue (interfaceImpl, out proxyList)) {
					proxyList = new List<BaseProxy> ();
					proxies.Add (interfaceImpl, proxyList);
				}
			}
			var proxy = AddProxyIfNotPresent (interfaceType, proxyList, interfaceImpl);
			return proxy;
		}

		BaseProxy AddProxyIfNotPresent (Type interfaceType, List<BaseProxy> proxyList, object interfaceImpl)
		{
			lock (registryLock) {
				foreach (var proxy in proxyList) {
					if (proxy.InterfaceType == interfaceType)
						return proxy;
				}
				var everyProtocol = proxyList.Count != 0 ? proxyList [0].EveryProtocol : new EveryProtocol ();
				var finalProxy = SwiftProtocolTypeAttribute.MakeProxy (interfaceType, interfaceImpl, everyProtocol);
				proxyList.Add (finalProxy);
				return finalProxy;
			}
		}

		public T InterfaceForEveryProtocolHandle<T> (IntPtr everyProtocolHandle)
		{
			if (everyProtocolHandle == IntPtr.Zero)
				throw new ArgumentNullException (nameof (everyProtocolHandle));
			return (T)InterfaceForEveryProtocolHandle (typeof (T), everyProtocolHandle);
		}

		object InterfaceForEveryProtocolHandle (Type interfaceType, IntPtr everyProtocolHandle)
		{
			lock (registryLock) {
				foreach (var kvp in proxies) {
					if (kvp.Value.Count == 0)
						continue;
					if (kvp.Value [0].EveryProtocol.SwiftObject != everyProtocolHandle)
						continue;
					foreach (var proxy in kvp.Value) {
						if (proxy.InterfaceType == interfaceType)
							return kvp.Key;
					}
				}
			}
			return null;
		}

		internal object InstanceFromEveryProtocolHandle (IntPtr everyProtocolHandle)
		{
			lock (registryLock) {
				foreach (var kvp in proxies) {
					if (kvp.Value.Count == 0)
						continue;
					if (kvp.Value [0].EveryProtocol.SwiftObject == everyProtocolHandle)
						return kvp.Key;
				}
				return null;
			}
		}

		public T ProxyForEveryProtocolHandle<T> (IntPtr everyProtocolHandle)
		{
			if (everyProtocolHandle == IntPtr.Zero)
				throw new ArgumentNullException (nameof (everyProtocolHandle));
			return (T)ProxyForEveryProtocolHandle (typeof (T), everyProtocolHandle);
		}

		object ProxyForEveryProtocolHandle (Type interfaceType, IntPtr everyProtocolHandle)
		{
			lock (registryLock) {
				foreach (var proxyList in proxies.Values) {
					if (proxyList.Count == 0)
						continue;
					if (proxyList [0].EveryProtocol.SwiftObject != everyProtocolHandle)
						continue;
					foreach (var proxy in proxyList) {
						if (proxy.InterfaceType == interfaceType)
							return proxy;
					}
				}
			}
			return null;
		}

		public T InterfaceForExistentialContainer<T> (ISwiftExistentialContainer container)
		{
			Exceptions.ThrowOnNull (container, $"{nameof (container)} is a null Existential Container");
			if (container.Count != 1)
				throw new ArgumentException ($"expected a single protocol existential container", nameof (container));
			return (T)InterfaceForExistentialContainer (typeof (T), container);
		}

		object InterfaceForExistentialContainer (Type interfaceType, ISwiftExistentialContainer container)
		{
			if (!interfaceType.IsInterface)
				throw new ArgumentException ("generic object type must be an interface", nameof (interfaceType));

			var metadata = container.ObjectMetadata;
			if (container.ObjectMetadata.Handle == EveryProtocol.GetSwiftMetatype ().Handle)
				return ExistingInterfaceImplementationForContainer (interfaceType, container);
			var proxy = SwiftProtocolTypeAttribute.MakeProxy (interfaceType, container);
			return proxy;
		}

		object ExistingInterfaceImplementationForContainer (Type interfaceType, ISwiftExistentialContainer container)
		{
			var handle = container.Data0;
			lock (registryLock) {
				foreach (var kvp in proxies) {
					if (kvp.Value.Count == 0)
						continue;
					if (kvp.Value [0].EveryProtocol.SwiftObject == handle)
						return kvp.Key;
				}
			}
			throw new SwiftRuntimeException ($"Failed to find an interface implementation of type {interfaceType.Name} for an existential container.");
		}

		Dictionary<SwiftDotNetCapsule, Tuple<Delegate, Type [], Type>> registeredClosures =
				new Dictionary<SwiftDotNetCapsule, Tuple<Delegate, Type [], Type>> ();

		public SwiftClosureRepresentation SwiftClosureForDelegate (Delegate d, Action<IntPtr, IntPtr> action, Type [] argumentTypes)
		{
			lock (registryLock) {
				var capsule = new SwiftDotNetCapsule (IntPtr.Zero);
				var rep = new SwiftClosureRepresentation (action, capsule.SwiftObject);
				registeredClosures.Add (capsule, new Tuple<Delegate, Type [], Type> (d, argumentTypes, null));
				return rep;
			}
		}

		public SwiftClosureRepresentation SwiftClosureForDelegate (Delegate d, Action<IntPtr> action, Type [] argumentTypes)
		{
			lock (registryLock) {
				var capsule = new SwiftDotNetCapsule (IntPtr.Zero);
				var rep = new SwiftClosureRepresentation (action, capsule.SwiftObject);
				registeredClosures.Add (capsule, new Tuple<Delegate, Type [], Type> (d, argumentTypes, null));
				return rep;
			}
		}

		public SwiftClosureRepresentation SwiftClosureForDelegate (Delegate d, Action<IntPtr, IntPtr> action, Type [] argumentTypes, Type returnType)
		{
			lock (registryLock) {
				var capsule = new SwiftDotNetCapsule (IntPtr.Zero);
#if DEBUG_SPEW
				Console.WriteLine ("SwiftClosureForDelegate: capsule SwiftObject " + capsule.SwiftObject.ToString ("X8"));
#endif
				var rep = new SwiftClosureRepresentation (action, capsule.SwiftObject);
#if DEBUG_SPEW
				Console.WriteLine ("SwiftClosureForDelegate: closure representation: Func: " +
				               rep.Function.ToString() + " Data: " + rep.Data.ToString("X8")
				);
#endif
				registeredClosures.Add (capsule, new Tuple<Delegate, Type [], Type> (d, argumentTypes, returnType));
				return rep;
			}
		}


		public SwiftClosureRepresentation SwiftClosureForDelegate (Delegate d, Action<IntPtr, IntPtr, IntPtr> action, Type [] argumentTypes, Type returnType)
		{
			lock (registryLock) {
				var capsule = new SwiftDotNetCapsule (IntPtr.Zero);
#if DEBUG_SPEW
				Console.WriteLine("SwiftClosureForDelegate: capsule SwiftObject " + capsule.SwiftObject.ToString("X8"));
#endif
				var rep = new SwiftClosureRepresentation (action, capsule.SwiftObject);
				registeredClosures.Add (capsule, new Tuple<Delegate, Type [], Type> (d, argumentTypes, returnType));
				return rep;
			}
		}


		public Tuple<Delegate, Type [], Type> ClosureForCapsule (SwiftDotNetCapsule capsule)
		{
			if (capsule == null)
				throw new ArgumentNullException (nameof (capsule));
			lock (registryLock) {
				Tuple<Delegate, Type [], Type> d = null;
				registeredClosures.TryGetValue (capsule, out d);
				return d;
			}
		}

		public void RemoveCapsule (SwiftDotNetCapsule capsule)
		{
			if (capsule == null)
				throw new ArgumentNullException (nameof (capsule));
			lock (registryLock) {
				if (registeredClosures.ContainsKey (capsule))
					registeredClosures.Remove (capsule);
			}
		}

		Dictionary<BlindSwiftClosureRepresentation, Delegate> blindRepToDelegate = new Dictionary<BlindSwiftClosureRepresentation, Delegate> ();

		Delegate PlainActionForSwiftClosure (BlindSwiftClosureRepresentation rep)
		{
			return new Action (() => {
				BlindSwiftClosureRepresentation.InvokePlainAction (rep);
			});
		}


		T MemoizedClosure<T> (BlindSwiftClosureRepresentation rep, Func<BlindSwiftClosureRepresentation, T> factory) where T : class
		{
			lock (registryLock) {
				Delegate retval = null;
				if (!blindRepToDelegate.TryGetValue (rep, out retval)) {
					var suppliedDelegate = factory (rep);
					retval = suppliedDelegate as Delegate;
					blindRepToDelegate.Add (rep, retval);
				}
				return retval as T;
			}
		}


		public Action ActionForSwiftClosure (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure (rep, (bc) => new Action (() => BlindSwiftClosureRepresentation.InvokePlainAction (bc)));
		}

		public Action<T1> ActionForSwiftClosure<T1> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1>> (rep, (bc) => (arg1) => {
				unsafe {
					var types = new Type [] { typeof (T1) };
					var args = new object [] { arg1 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)));
				}
			});
		}

		public Action<T1, T2> ActionForSwiftClosure<T1, T2> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2>> (rep, (bc) => (arg1, arg2) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2) };
					var args = new object [] { arg1, arg2 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)));
				}
			});
		}


		public Action<T1, T2, T3> ActionForSwiftClosure<T1, T2, T3> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3>> (rep, (bc) => (arg1, arg2, arg3) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2), typeof (T3) };
					var args = new object [] { arg1, arg2, arg3 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)));
				}
			});
		}


		public Action<T1, T2, T3, T4> ActionForSwiftClosure<T1, T2, T3, T4> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4>> (rep, (bc) => (arg1, arg2, arg3, arg4) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4) };
					var args = new object [] { arg1, arg2, arg3, arg4 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5> ActionForSwiftClosure<T1, T2, T3, T4, T5> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5) };
					var args = new object [] { arg1, arg2, arg3, arg4, arg5 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6) };
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)));
				}
			});
		}


		public Action<T1, T2, T3, T4, T5, T6, T7> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7) };
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => {
				unsafe {
					var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8) };
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10), typeof (T11)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)), StructMarshal.Marshaler.Metatypeof (typeof (T11)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10), typeof (T11), typeof (T12)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)), StructMarshal.Marshaler.Metatypeof (typeof (T11)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T12)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10), typeof (T11), typeof (T12),
						typeof (T13)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)), StructMarshal.Marshaler.Metatypeof (typeof (T11)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T12)), StructMarshal.Marshaler.Metatypeof (typeof (T13)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10), typeof (T11), typeof (T12),
						typeof (T13), typeof (T14)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)), StructMarshal.Marshaler.Metatypeof (typeof (T11)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T12)), StructMarshal.Marshaler.Metatypeof (typeof (T13)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T14)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10), typeof (T11), typeof (T12),
						typeof (T13), typeof (T14), typeof (T15)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)), StructMarshal.Marshaler.Metatypeof (typeof (T11)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T12)), StructMarshal.Marshaler.Metatypeof (typeof (T13)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T14)), StructMarshal.Marshaler.Metatypeof (typeof (T15)));
				}
			});
		}

		public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> ActionForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>> (rep, (bc) => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16) => {
				unsafe {
					var types = new Type [] {
						typeof (T1), typeof (T2), typeof (T3), typeof (T4),
						typeof (T5), typeof (T6), typeof (T7), typeof (T8),
						typeof (T9), typeof (T10), typeof (T11), typeof (T12),
						typeof (T13), typeof (T14), typeof (T15), typeof (T16)
					};
					var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 };
					var tupleMap = SwiftTupleMap.FromTypes (types);
					var argMemory = stackalloc byte [tupleMap.Stride];
					var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
					BlindSwiftClosureRepresentation.InvokeAction (bc, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (T3)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T4)), StructMarshal.Marshaler.Metatypeof (typeof (T5)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (T9)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T10)), StructMarshal.Marshaler.Metatypeof (typeof (T11)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T12)), StructMarshal.Marshaler.Metatypeof (typeof (T13)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T14)), StructMarshal.Marshaler.Metatypeof (typeof (T15)),
										      StructMarshal.Marshaler.Metatypeof (typeof (T16)));
				}
			});
		}




		public Func<TR> FuncForSwiftClosure<TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<TR>> (rep, (bc) => () => {
				unsafe {
					var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
					var returnPtr = new IntPtr (returnMemory);
					BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, StructMarshal.Marshaler.Metatypeof (typeof (TR)));
					return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
				}
			});
		}

		public Func<T1, TR> FuncForSwiftClosure<T1, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, TR>> (rep, (bc) =>
							      (arg1) => {
								      unsafe {
#if DEBUG_SPEW
									      Console.WriteLine ("In memoized closure for blind closure, rep.Data: " + rep.Data.ToString ("X8"));
									      Console.WriteLine ("Arg is {0}", arg1);
#endif
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1) };
									      var args = new object [] { arg1 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, TR> FuncForSwiftClosure<T1, T2, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, TR>> (rep, (bc) =>
							      (arg1, arg2) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2) };
									      var args = new object [] { arg1, arg2 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, TR> FuncForSwiftClosure<T1, T2, T3, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3) };
									      var args = new object [] { arg1, arg2, arg3 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, TR> FuncForSwiftClosure<T1, T2, T3, T4, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4) };
									      var args = new object [] { arg1, arg2, arg3, arg4 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}


		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8), typeof (T9) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8), typeof (T9), typeof (T10) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] {
											typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8),
											typeof (T9), typeof (T10), typeof (T11)
										};
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T11)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] {
											typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8),
											typeof (T9), typeof (T10), typeof (T11), typeof (T12)
										};
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T11)), StructMarshal.Marshaler.Metatypeof (typeof (T12)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] {
											typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8),
											typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13)
										};
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T11)), StructMarshal.Marshaler.Metatypeof (typeof (T12)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T13)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] {
											typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8),
											typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13), typeof (T14)
										};
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T11)), StructMarshal.Marshaler.Metatypeof (typeof (T12)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T13)), StructMarshal.Marshaler.Metatypeof (typeof (T14)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] {
											typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8),
											typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13), typeof (T14), typeof (T15)
										};
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T11)), StructMarshal.Marshaler.Metatypeof (typeof (T12)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T13)), StructMarshal.Marshaler.Metatypeof (typeof (T14)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T15)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> FuncForSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16) => {
								      unsafe {
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (typeof (TR))];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] {
											typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6), typeof (T7), typeof (T8),
											typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13), typeof (T14), typeof (T15), typeof (T16)
										};
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunction (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)), StructMarshal.Marshaler.Metatypeof (typeof (T2)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (T4)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (T6)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T7)), StructMarshal.Marshaler.Metatypeof (typeof (T8)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (T10)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T11)), StructMarshal.Marshaler.Metatypeof (typeof (T12)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T13)), StructMarshal.Marshaler.Metatypeof (typeof (T14)),
															      StructMarshal.Marshaler.Metatypeof (typeof (T15)), StructMarshal.Marshaler.Metatypeof (typeof (T16)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));
									      return StructMarshal.Marshaler.ToNet<TR> (returnPtr);
								      }
							      });
		}

		static T HandleThrowFuncReturn<T> (IntPtr returnPtr, Type returnMedusaTupleType)
		{
			if (StructMarshal.Marshaler.ExceptionReturnContainsSwiftError (returnPtr, returnMedusaTupleType)) {
				throw StructMarshal.Marshaler.GetExceptionThrown (returnPtr, returnMedusaTupleType); ;
			}
			return StructMarshal.Marshaler.GetErrorReturnValue<T> (returnPtr);
		}

		public Func<TR> FuncForSwiftClosureThrows<TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<TR>> (rep, (bc) =>
							      () => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr,
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, TR> FuncForSwiftClosureThrows<T1, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, TR>> (rep, (bc) =>
							      (arg1) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1) };
									      var args = new object [] { arg1 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
															      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, TR> FuncForSwiftClosureThrows<T1, T2, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, TR>> (rep, (bc) =>
							      (arg1, arg2) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2) };
									      var args = new object [] { arg1, arg2 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, TR> FuncForSwiftClosureThrows<T1, T2, T3, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3) };
									      var args = new object [] { arg1, arg2, arg3 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4) };
									      var args = new object [] { arg1, arg2, arg3, arg4 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6) };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10), typeof (T11)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.SwiftObjectMetatype (typeof (T11)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10), typeof (T11), typeof (T12)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.SwiftObjectMetatype (typeof (T11)), StructMarshal.SwiftObjectMetatype (typeof (T12)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.SwiftObjectMetatype (typeof (T11)), StructMarshal.SwiftObjectMetatype (typeof (T12)), StructMarshal.SwiftObjectMetatype (typeof (T13)),
										      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13),
										      typeof (T14)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.SwiftObjectMetatype (typeof (T11)), StructMarshal.SwiftObjectMetatype (typeof (T12)), StructMarshal.SwiftObjectMetatype (typeof (T13)),
										      StructMarshal.SwiftObjectMetatype (typeof (T14)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13),
										      typeof (T14), typeof (T15)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.SwiftObjectMetatype (typeof (T11)), StructMarshal.SwiftObjectMetatype (typeof (T12)), StructMarshal.SwiftObjectMetatype (typeof (T13)),
										      StructMarshal.SwiftObjectMetatype (typeof (T14)), StructMarshal.SwiftObjectMetatype (typeof (T15)), StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> FuncForSwiftClosureThrows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> (BlindSwiftClosureRepresentation rep)
		{
			return MemoizedClosure<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>> (rep, (bc) =>
							      (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16) => {
								      unsafe {
									      var returnType = typeof (Tuple<TR, SwiftError, bool>);
									      var returnMemory = stackalloc byte [StructMarshal.Marshaler.Strideof (returnType)];
									      var returnPtr = new IntPtr (returnMemory);
									      var types = new Type [] { typeof (T1), typeof (T2), typeof (T3), typeof (T4), typeof (T5), typeof (T6),
										      typeof (T7), typeof (T8), typeof (T9), typeof (T10), typeof (T11), typeof (T12), typeof (T13),
										      typeof (T14), typeof (T15), typeof (T16)
									      };
									      var args = new object [] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 };
									      var tupleMap = SwiftTupleMap.FromTypes (types);
									      var argMemory = stackalloc byte [tupleMap.Stride];
									      var argPtr = StructMarshal.Marshaler.MarshalObjectsAsTuple (args, tupleMap, new IntPtr (argMemory));
									      BlindSwiftClosureRepresentation.InvokeFunctionThrows (bc, returnPtr, argPtr, StructMarshal.Marshaler.Metatypeof (typeof (T1)),
										      StructMarshal.SwiftObjectMetatype (typeof (T2)), StructMarshal.SwiftObjectMetatype (typeof (T3)), StructMarshal.SwiftObjectMetatype (typeof (T4)),
										      StructMarshal.SwiftObjectMetatype (typeof (T5)), StructMarshal.SwiftObjectMetatype (typeof (T6)), StructMarshal.SwiftObjectMetatype (typeof (T7)),
										      StructMarshal.SwiftObjectMetatype (typeof (T8)), StructMarshal.SwiftObjectMetatype (typeof (T9)), StructMarshal.SwiftObjectMetatype (typeof (T10)),
										      StructMarshal.SwiftObjectMetatype (typeof (T11)), StructMarshal.SwiftObjectMetatype (typeof (T12)), StructMarshal.SwiftObjectMetatype (typeof (T13)),
										      StructMarshal.SwiftObjectMetatype (typeof (T14)), StructMarshal.SwiftObjectMetatype (typeof (T15)), StructMarshal.SwiftObjectMetatype (typeof (T16)),
										      StructMarshal.Marshaler.Metatypeof (typeof (TR)));

									      return HandleThrowFuncReturn<TR> (returnPtr, returnType);
								      }
							      });
		}

		public static SwiftObjectRegistry Registry {
			get {
				return registry;
			}
		}


	}
}
