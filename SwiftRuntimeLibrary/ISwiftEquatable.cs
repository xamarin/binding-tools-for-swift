using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftProtocolType (typeof (SwiftEquatableProxy), true)]
	[SwiftExternalProtocolDefinition (typeof (SwiftString), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_SwiftString)]
	[SwiftExternalProtocolDefinition (typeof (IntPtr), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_IntPtr)]
	[SwiftExternalProtocolDefinition (typeof (bool), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_bool)]

	[SwiftExternalProtocolDefinition (typeof (double), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_double)]
	[SwiftExternalProtocolDefinition (typeof (float), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_float)]
	[SwiftExternalProtocolDefinition (typeof (nint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_nint)]
	[SwiftExternalProtocolDefinition (typeof (nuint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_nuint)]
	[SwiftExternalProtocolDefinition (typeof (sbyte), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_sbyte)]
	[SwiftExternalProtocolDefinition (typeof (short), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_short)]
	[SwiftExternalProtocolDefinition (typeof (int), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_int)]
	[SwiftExternalProtocolDefinition (typeof (long), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_long)]
	[SwiftExternalProtocolDefinition (typeof (byte), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_byte)]
	[SwiftExternalProtocolDefinition (typeof (ushort), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_ushort)]
	[SwiftExternalProtocolDefinition (typeof (uint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_uint)]
	[SwiftExternalProtocolDefinition (typeof (ulong), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftEquatable_ulong)]
	public interface ISwiftEquatable {
		bool OpEquals (ISwiftEquatable other);
	}
}
