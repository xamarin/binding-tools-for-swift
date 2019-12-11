// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SwiftRuntimeLibrary.SwiftMarshal {
	public struct SwiftNominalTypeDescriptor {
		// Memory layout
		// index Size    Meaning
		//         All descriptors
		// 0     32 bits flags
		// 1     32 bits relative pointer parent
		// Type Context
		// 2     32 bits relative pointer name
		// 3     32 bits relative pointer access function
		// 4     32 bits relative pointer field descriptors

		// Class Descriptor
		// 5     32 bits relative pointer super class type
		// 6     32 bits union metadata negative size in words / relative pointer metadata bounds
		// 7     32 bits union metadata positive size in words / extra class flags
		// 8     32 bits num immediate members
		// 9     32 bits num fields
		// 10    32 bits field offset vector offset
		// if it's resilient:
		// 11    32 bits relative pointer

		// Struct and Enum Descriptor
		// 5     32 bits num fields
		// 6     32 bits field offset vector offset
		// 


		// Generic trailer - generic trailer follows the above
		// Index Size    Meaning
		// 0     32 bits relative pointer instantiation cache
		// 1     32 bits relative pointer default instantiation pattern
		// Generic Context Descriptor Header
		// 2     16 bits num parameters
		// 2.5   16 bits num requirements
		// 3     16 bits num key arguments
		// 3.5   16 bits num extra arguments
		// Note: total arguments = num key arguments + num extra arguments


		// Protocol Descriptor
		// 3	32 bits NumRequirementsInSignature (number of generic requirements in signature)
		// -- start of base descriptor
		// 4	32 bits NumRequirements
		// 5	relative pointer to associated type names
		// -- n generic requirements (NumRequirementsInSignature)
		// first one is offset 6
		//	0	32 bits flags
		//	1	32 bits relative pointer to mangled name of constraint
		//	2	32 bits union
		// -- n associated type descriptors
		//	0	32 bits flags
		//	1	32 bits relative pointer


		[Flags]
		enum TypeContextFlags {
			// There are flags/bit field positions that are in the upper 16 bits of the
			// first 32 bits of the descriptor
			MetadataInitialization = 1 << 0,
			HasImportInfo = 1 << 2,
			ClassResilientSuperclassReferenceKind = 1 << 9,
			ClassAreImmediateMembersNegative = 1 << 12,
			ClassHasResilientSuperClass = 1 << 13,
			ClassHasOverrideTable = 1 << 14,
			ClassHasVtable = 1 << 15,
		}

		// all types
		const int kFlagsOffset = 0;
		const int kParentOffset = 1 * sizeof (int);
		const int kNameOffset = 2 * sizeof (int);
		const int kAccessOffset = 3 * sizeof (int);
		const int kFieldDescOffset = 4 * sizeof (int);

		// class elements
		const int kClassSuperOffset = 5 * sizeof (int);
		const int kNegativeSizeOffset = 6 * sizeof (int);
		const int kMetadataBoundsOffset = 6 * sizeof (int);
		const int kPositiveSizeOFfset = 7 * sizeof (int);
		const int kExtraClassFlagsOffset = 7 * sizeof (int);
		const int kNumImmediateMembersOffset = 8 * sizeof (int);
		const int kNumClassFields = 9 * sizeof (int);
		const int kClassFieldOffsetVectorOffset = 10 * sizeof (int);
		const int kResiliantSuperOffset = 11 * sizeof (int);

		const int kSmallerTypicalClassSize = 12 * sizeof (int);

		// struct and enum
		const int kNumValueFieldsOffset = 5 * sizeof (int);
		const int kValueFieldOffsetVectorOffset = 6 * sizeof (int);

		const int kValueTypeSize = 7 * sizeof (int);

		// protocol elements
		const int kNumRequirementsInSignatureOffset = 3 * sizeof (int);
		const int kRequirementsBaseDescriptorOffset = 4 * sizeof (int);
		const int kGenericRequirementsSize = 3 * sizeof (int);
		const int kAssociatedTypeDescriptorSize = 2 * sizeof (int);
		const int kGenericRequirementsOffset = 6 * sizeof (int);


		[StructLayout(LayoutKind.Sequential)]
		struct GenericHeader {
			public int InstantiationCache;
			public int InstantiationPattern;
			public ushort ParameterCount;
			public ushort RequirementCount;
			public ushort KeyArgumentCount;
			public ushort ExtraArgumentCount;
			public int TotalArgumentCount {
				get {
					return KeyArgumentCount + ExtraArgumentCount;
				}
			}
		}

		IntPtr handle;
		public SwiftNominalTypeDescriptor (IntPtr handle)
		{
			this.handle = handle;
		}

		internal IntPtr Handle { get { return handle; } }

		static bool KindIsValid (NominalTypeDescriptorKind kind)
		{
			switch (kind) {
			case NominalTypeDescriptorKind.Class:
			case NominalTypeDescriptorKind.Enum:
			case NominalTypeDescriptorKind.Struct:
				return true;
			default:
				return false;
			}
		}

		public bool IsValid {
			get {
				return handle != IntPtr.Zero;
			}
		}

		public NominalTypeDescriptorKind GetKind ()
		{
			// these are from MetadataValues.h in ContextDescriptorFlags
			ThrowOnInvalid ();
			switch (Marshal.ReadInt32 (handle) & 0x1f) {
			case 0:
				return NominalTypeDescriptorKind.Module;
			case 1:
				return NominalTypeDescriptorKind.Extension;
			case 2:
				return NominalTypeDescriptorKind.Anonymous;
			case 3:
				return NominalTypeDescriptorKind.Protocol;
			case 16:
				return NominalTypeDescriptorKind.Class;
			case 17:
				return NominalTypeDescriptorKind.Struct;
			case 18:
				return NominalTypeDescriptorKind.Enum;
			default:
				throw new NotSupportedException ();
			}
		}

		void ThrowOnNotClass ()
		{
			if (GetKind () != NominalTypeDescriptorKind.Class)
				throw new NotSupportedException ("HasResilientSuperClass applies only to class types.");
		}

		TypeContextFlags TypeContextFlagsBits {
			get {
				return (TypeContextFlags)(Marshal.ReadInt32 (handle) >> 16);
			}
		}

		bool ImmediateMembersAreNegative ()
		{
			ThrowOnNotClass ();
			return (TypeContextFlagsBits & TypeContextFlags.ClassAreImmediateMembersNegative) != 0;
		}

		bool HasResilientSuperClass ()
		{
			ThrowOnNotClass ();
			return (TypeContextFlagsBits & TypeContextFlags.ClassHasResilientSuperClass) != 0;
		}

		internal int GetGenericOffset ()
		{
			// returns the offset to generic arguments in the SwiftMetatype object
			switch (GetKind ()) {
			case NominalTypeDescriptorKind.Class:
				return GetClassGenericOffsetInWords () * IntPtr.Size;
			case NominalTypeDescriptorKind.Enum:
			case NominalTypeDescriptorKind.Struct:
				return SwiftMetatype.ValueBaseSize;
			default:
				throw new NotSupportedException ($"Type descriptor of type {GetKind ()} doesn't support generic arguments");
			}
		}

		GenericHeader GetGenericHeader ()
		{
			if (!IsGeneric ())
				throw new NotSupportedException ($"Type descriptor of type {GetKind ()} isn't generic");
			var mainSize = 0;
			// this gets the offset of the generic header within the
			// nominal type descriptor
			switch (GetKind ()) {
			case NominalTypeDescriptorKind.Class:
				mainSize = kSmallerTypicalClassSize + (HasResilientSuperClass () ? sizeof (int) : 0);
				break;
			case NominalTypeDescriptorKind.Enum:
			case NominalTypeDescriptorKind.Struct:
				mainSize = kValueTypeSize;
				break;
			default:
				throw new NotSupportedException ($"Type descriptor of type {GetKind ()} doesn't support generic arguments");
			}
			var headerPtr = handle + mainSize;
			return (GenericHeader)Marshal.PtrToStructure (headerPtr, typeof (GenericHeader));
		}

		public int GetParameterCount ()
		{
			if (!IsGeneric ())
				return 0;
			var header = GetGenericHeader ();
			return header.ParameterCount;
		}

		public int GetTotalGenericArgumentCount ()
		{
			if (!IsGeneric ())
				return 0;
			var header = GetGenericHeader ();
			return header.TotalArgumentCount;
		}

		public int GetGenericKeyArgumentCount ()
		{
			if (!IsGeneric ())
				return 0;
			var header = GetGenericHeader ();
			return header.KeyArgumentCount;
		}

		public int GetGenericExtraArgumentCount ()
		{
			if (!IsGeneric ())
				return 0;
			var header = GetGenericHeader ();
			return header.ExtraArgumentCount;
		}

		int GetClassGenericOffsetInWords () // returns in machine words
		{
			if (!HasResilientSuperClass ())
				return NonResilientImmediateMembersOffsetInWords ();
			return ResilientImmediateMembersOffsetInWords ();
		}

		int MetadataNegativeSizeInWords {
			get {
				ThrowOnNotClass ();
				return Marshal.ReadInt32 (handle + kNegativeSizeOffset);
			}
		}

		int MetadataPositiveSizeInWords {
			get {
				ThrowOnNotClass ();
				return Marshal.ReadInt32 (handle + kPositiveSizeOFfset);
			}
		}

		IntPtr ResilientMetadataBounds {
			get {
				ThrowOnNotClass ();
				var currentPtr = handle + kMetadataBoundsOffset;
				return currentPtr + Marshal.ReadInt32 (currentPtr);
			}
		}

		int NumImmediateMembers {
			get {
				ThrowOnNotClass ();
				return Marshal.ReadInt32 (handle + kNumImmediateMembersOffset);
			}
		}

		int NonResilientImmediateMembersOffsetInWords ()
		{
			return ImmediateMembersAreNegative () ?
				-MetadataNegativeSizeInWords :
				MetadataPositiveSizeInWords - NumImmediateMembers;
		}

		IntPtr ResilientSuperclass {
			get {
				ThrowOnNotClass ();
				var currentPtr = handle + kResiliantSuperOffset;
				var offset = Marshal.ReadInt32 (currentPtr);
				return offset == 0 ? IntPtr.Zero : currentPtr + offset;
			}
		}

		static int ComputeMetadataBoundsFromSuperClass (SwiftNominalTypeDescriptor descHandle)
		{
			// the computed size of a class metadata is the size of its members + the size of its super class.
			// If there is no super class, the size of the basic class metadata
			int bounds = 0;
			var superPtr = descHandle.ResilientSuperclass;
			if (superPtr != IntPtr.Zero) {
				bounds = ComputeMetadataBoundsFromSuperClass (new SwiftNominalTypeDescriptor (superPtr));
			} else {
				bounds = SwiftMetatype.ClassSizeWithoutMembers;
			}
			if (descHandle.ImmediateMembersAreNegative ()) {
				bounds -= descHandle.MetadataNegativeSizeInWords;
			} else {
				bounds += descHandle.MetadataPositiveSizeInWords;
			}
			return bounds;
		}

		int ResilientImmediateMembersOffsetInWords ()
		{
			var storedBoundsPtr = ResilientMetadataBounds;
			var bounds = (long)Marshal.ReadIntPtr (storedBoundsPtr);
			if (bounds != 0)
				return (int)(bounds / IntPtr.Size);
			return ComputeMetadataBoundsFromSuperClass (this) / IntPtr.Size;
		}

		public bool IsGeneric ()
		{
			// this is from MetadataValues.h in ContextDescriptorFlags
			ThrowOnInvalid ();
			return (Marshal.ReadInt32 (handle) & 0x80) != 0;
		}

		public byte GetVersion ()
		{
			ThrowOnInvalid ();
			return (byte)(Marshal.ReadInt32 (handle) >> 8);
		}

		public bool IsUnique ()
		{
			// this is from MetadataValues.h in ContextDescriptorFlags
			ThrowOnInvalid ();
			return (Marshal.ReadInt32 (handle) & 0x40) != 0;
		}

		void ThrowOnInvalid ()
		{
			if (!IsValid)
				throw new InvalidOperationException ();
		}

		void ThrowOnInvalidOrNoFields ()
		{
			ThrowOnInvalid ();
			if (!(GetKind () == NominalTypeDescriptorKind.Class || GetKind () == NominalTypeDescriptorKind.Struct))
				throw new InvalidOperationException ();
		}

		void ThrowOnInvalidOrNotEnum ()
		{
			ThrowOnInvalid ();
			if (GetKind () != NominalTypeDescriptorKind.Enum)
				throw new InvalidOperationException ();
		}

		public string GetName ()
		{
			ThrowOnInvalid ();
			return ReadRelativeString (handle + 2 * sizeof (int));
		}

		public string GetFullName ()
		{
			ThrowOnInvalid ();
			var buffer = new StringBuilder (GetName ());
			var parent = handle;
			while (true) {
				parent = GetParent (parent);
				if (parent == IntPtr.Zero)
					break;
				buffer.Insert (0, '.');
				buffer.Insert (0, ReadRelativeString (parent + 2 * sizeof (int)));
			}
			return buffer.ToString ();
		}

		static IntPtr GetParent (IntPtr handle)
		{
			var parentPtr = handle + sizeof (int);
			var parentOffset = Marshal.ReadInt32 (parentPtr);
			if (parentOffset == 0)
				return IntPtr.Zero;
			parentPtr += parentOffset;
			return parentPtr;
		}

		static string ReadRelativeString (IntPtr memory)
		{
			var targetPosition = memory;
			int offset = Marshal.ReadInt32 (memory);
			targetPosition += offset;
			var len = 0;
			while (Marshal.ReadByte (targetPosition, len) != 0)
				++len;
			var buffer = new byte [len];
			Marshal.Copy (targetPosition, buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString (buffer);
		}

		internal IntPtr GetProtocolRequirementsBaseDescriptor ()
		{
			if (GetKind () != NominalTypeDescriptorKind.Protocol)
				throw new InvalidOperationException ();
			return handle + kRequirementsBaseDescriptorOffset;
		}

		internal SwiftAssociatedTypeDescriptor GetAssociatedTypeDescriptor (int index)
		{
			if (GetKind () != NominalTypeDescriptorKind.Protocol)
				throw new InvalidOperationException ();
			if (index < 0 || index >= GetAssociatedTypesCount ()) {
				throw new ArgumentOutOfRangeException (nameof (index));
			}
			var numGenericsPtr = handle + kNumRequirementsInSignatureOffset;
			var numGenerics = Marshal.ReadInt32 (numGenericsPtr);
			return new SwiftAssociatedTypeDescriptor (handle + kGenericRequirementsOffset + (numGenerics * kGenericRequirementsSize));
		}

		internal int GetAssociatedTypesCount ()
		{
			if (GetKind () != NominalTypeDescriptorKind.Protocol)
				throw new InvalidOperationException ();
			var baseDesciptor = handle + kRequirementsBaseDescriptorOffset;
			return Marshal.ReadInt32 (baseDesciptor);
		}

		internal int GetFieldCount ()
		{
			ThrowOnInvalidOrNoFields ();
			var targetPosition = handle + sizeof (int);
			return Marshal.ReadInt32 (targetPosition);
		}

		internal static SwiftNominalTypeDescriptor? FromDylib (DynamicLib dylib, string nomDescSymbolName)
		{
			var nom = dylib.FindSymbolAddress (nomDescSymbolName);
			if (nom == IntPtr.Zero)
				return null;
			return new SwiftNominalTypeDescriptor (nom);
		}

		internal static SwiftNominalTypeDescriptor? FromDylibFile (string pathName, DLOpenMode openMode, string nomDescSymbolName)
		{
			using (DynamicLib dylib = new DynamicLib (pathName, openMode)) {
				return FromDylib (dylib, nomDescSymbolName);
			}
		}


	}

}

