// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftRuntimeLibrary {
	[Flags]
	public enum MetadataFlags {
		MetadataKindIsNonType = 0x400,
		MetadataKindIsNonHeap = 0x200,
		MetadataKindIsRuntimePrivate = 0x100,
	}
	public enum MetatypeKind {
		None = 0,
		Struct = 0 | MetadataFlags.MetadataKindIsNonHeap,
		Enum = 1 | MetadataFlags.MetadataKindIsNonHeap,
		Optional = 2 | MetadataFlags.MetadataKindIsNonHeap,
		ForeignClass = 3 | MetadataFlags.MetadataKindIsNonHeap,
		Opaque = 0 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		Tuple = 1 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		Function = 2 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		Protocol = 3 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		Metatype = 4 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		ObjCClassWrapper = 5 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		ExistentialMetatype = 6 | MetadataFlags.MetadataKindIsRuntimePrivate | MetadataFlags.MetadataKindIsNonHeap,
		HeapLocalVariable = 0 | MetadataFlags.MetadataKindIsNonType,
		HeapGenericLocalVariable = 0 | MetadataFlags.MetadataKindIsNonType | MetadataFlags.MetadataKindIsRuntimePrivate,
		ErrorObject = 1 | MetadataFlags.MetadataKindIsNonType | MetadataFlags.MetadataKindIsRuntimePrivate,
		Class = 0x800 // not really, but it's what we do.
			// This number looks arbitrary, but the swift source says that they're never going to
	    		// go over 0x7ff for predefined/enumerated metadata kind values
	}

	public enum SwiftCallingConvention {
		Swift = 0,
		Block = 1,
		Thin = 2,
		CFunctionPointer = 3
	}

	public enum NominalTypeDescriptorKind {
		None = 0,
		Module,
		Extension,
		Anonymous,
		Protocol,
		Class,
		Struct,
		Enum
	}

	[Flags]
	public enum DynamicCastFlags {
		None = 0,
		Unconditional = 1 << 0,
		TakeOnSuccess = 1 << 1,
		DestroyOnFailure = 1 << 2
	}

	[Flags]
	public enum SwiftObjectFlags : byte {
		None = 0,
		Disposed = 1,
		NativeRef = 2,
		IsDirectBinding = 4,
		RegisteredToggleRef = 8,
		InFinalizerQueue = 16,
		HasManagedRef = 32,
		IsSwift = 64
	}

	[Flags]
    	internal enum SwiftParameterFlags : int {
		None = 0,
		Variadic = 0x80,
		AutoClosure = 0x100,
	}

	internal enum SwiftParameterOwnership : int {
		Default = 0,
		InOut = 1,
		Shared = 2,
		Owned = 3,
	}

	[Flags]
    	internal enum SwiftExclusivityFlags {
		Read = 0,
		Modify = 1,
		Track = 0x20,
	}

	[Flags]
    	public enum SwiftMetadataRequest {
		Complete = 0,
		NonTransitiveComplete = 1,
		LayoutComplete = 0x3f,
		Abstract = 0xff,
		IsNotBlocking = 0x100,
	}

	[Flags]
	public enum SwiftProtocolMetadataFlags {
		ClassConstraint = 1 << 1,
		HasSuperClassConstraint = 1 << 0,
	}

	public enum SwiftSpecialProtocol {
		None = 0,
		Error = 1,
	}

	public enum ProtocolDispatchStrategy {
		ObjC = 0,
		Swift = 1,
	}

	public enum SwiftProtocolConformanceTypeDescriptorKind {
		DirectTypeDescriptor,
		IndirectTypeDescriptor,
		DirectObjCClassName,
		IndirectObjCClass,
	}
}

