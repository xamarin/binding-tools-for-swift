using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.Comparable")]
	[SwiftProtocolType (typeof (SwiftComparableProxy), true)]
	[SwiftExternalProtocolDefinition (typeof (SwiftString), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_SwiftString)]
	[SwiftExternalProtocolDefinition (typeof (IntPtr), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_IntPtr)]

	[SwiftExternalProtocolDefinition (typeof (double), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_double)]
	[SwiftExternalProtocolDefinition (typeof (float), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_float)]
	[SwiftExternalProtocolDefinition (typeof (nint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_nint)]
	[SwiftExternalProtocolDefinition (typeof (nuint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_nuint)]
	[SwiftExternalProtocolDefinition (typeof (sbyte), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_sbyte)]
	[SwiftExternalProtocolDefinition (typeof (short), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_short)]
	[SwiftExternalProtocolDefinition (typeof (int), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_int)]
	[SwiftExternalProtocolDefinition (typeof (long), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_long)]
	[SwiftExternalProtocolDefinition (typeof (byte), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_byte)]
	[SwiftExternalProtocolDefinition (typeof (ushort), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_ushort)]
	[SwiftExternalProtocolDefinition (typeof (uint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_uint)]
	[SwiftExternalProtocolDefinition (typeof (ulong), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftComparable_ulong)]
	public interface ISwiftComparable : ISwiftEquatable {
		bool OpLess (ISwiftComparable other);
	}
}
