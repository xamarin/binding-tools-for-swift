// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.Hashable")]
	[SwiftProtocolType (typeof (SwiftHashableProxy), true)]
	[SwiftExternalProtocolDefinition (typeof (SwiftString), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_SwiftString)]
	[SwiftExternalProtocolDefinition (typeof (IntPtr), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_IntPtr)]
	[SwiftExternalProtocolDefinition (typeof (bool), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_bool)]

	[SwiftExternalProtocolDefinition (typeof (double), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_double)]
	[SwiftExternalProtocolDefinition (typeof (float), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_float)]
	[SwiftExternalProtocolDefinition (typeof (nint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_nint)]
	[SwiftExternalProtocolDefinition (typeof (nuint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_nuint)]
	[SwiftExternalProtocolDefinition (typeof (sbyte), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_sbyte)]
	[SwiftExternalProtocolDefinition (typeof (short), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_short)]
	[SwiftExternalProtocolDefinition (typeof (int), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_int)]
	[SwiftExternalProtocolDefinition (typeof (long), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_long)]
	[SwiftExternalProtocolDefinition (typeof (byte), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_byte)]
	[SwiftExternalProtocolDefinition (typeof (ushort), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_ushort)]
	[SwiftExternalProtocolDefinition (typeof (uint), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_uint)]
	[SwiftExternalProtocolDefinition (typeof (ulong), SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.ISwiftHashable_ulong)]
	public interface ISwiftHashable : ISwiftEquatable {
		nint HashValue { get; }
	}
}
