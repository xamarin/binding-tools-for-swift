using System;
using System.Reflection;
using System.Runtime.InteropServices;
#if __IOS__
using ObjCRuntime;
#endif

namespace SwiftRuntimeLibrary.SwiftMarshal {
	[AttributeUsage (AttributeTargets.Interface, AllowMultiple = false)]
	public sealed class SwiftProtocolTypeAttribute : Attribute {
		public SwiftProtocolTypeAttribute (Type proxyType, bool isAssociatedTypeProtocol = false)
		{
			if (proxyType == null)
				throw new ArgumentNullException (nameof (proxyType));
			ProxyType = proxyType;
			IsAssociatedTypeProtocol = isAssociatedTypeProtocol;
		}
		public Type ProxyType { get; private set; }
		public bool IsAssociatedTypeProtocol { get; private set; }

#if __IOS__
		[MonoNativeFunctionWrapper]
#endif
		delegate IntPtr MetatypeFunctionType ();

		public bool IsAssociatedTypeProxy ()
		{
			return IsAssociatedTypeProtocol;
		}

		public static bool IsAssociatedTypeProxy (Type type)
		{
			var proxyAttr = type.GetCustomAttribute<SwiftProtocolTypeAttribute> ();
			return proxyAttr != null ? proxyAttr.IsAssociatedTypeProxy () : false;
		}

		public static Type ProxyTypeForInterfaceType (Type interfaceType)
		{
			var proxyAttr = interfaceType.GetCustomAttribute<SwiftProtocolTypeAttribute> ();
			if (proxyAttr == null)
				throw new SwiftRuntimeException ($"Type {interfaceType.Name} does not have a SwiftProtocol attribute.");
			return proxyAttr.ProxyType;
		}

		public static BaseProxy MakeProxy (Type interfaceType, object interfaceImpl, EveryProtocol protocol)
		{
			var proxyType = ProxyTypeForInterfaceType (interfaceType);

			var ci = proxyType.GetConstructor (new Type [] { interfaceType, typeof (EveryProtocol) });
			if (ci == null)
				throw new SwiftRuntimeException ($"Type {proxyType.Name} does not have a constructor that takes {interfaceType.Name} and {typeof (EveryProtocol).Name}");

			return (BaseProxy)ci.Invoke (new object [] { interfaceImpl, protocol });
		}

		public static BaseProxy MakeProxy (Type interfaceType, ISwiftExistentialContainer container)
		{
			var proxyType = ProxyTypeForInterfaceType (interfaceType);

			var ci = proxyType.GetConstructor (new Type [] { typeof (ISwiftExistentialContainer) });
			if (ci == null)
				throw new SwiftRuntimeException ($"Type {proxyType.Name} does not have a constructor that an ISwiftExistentialContainer");

			return (BaseProxy)ci.Invoke (new object [] { container });
		}
	}
}
