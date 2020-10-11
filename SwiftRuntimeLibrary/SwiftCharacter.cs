// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;


using System.Text;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
	[SwiftTypeName ("Swift.Character")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.SwiftCharacter_NominalTypeDescriptor, SwiftCoreConstants.SwiftCharacter_Metadata, "")]
	public sealed class SwiftCharacter : SwiftNativeValueType, ISwiftStruct {
		public unsafe SwiftCharacter (string character) : this (SwiftValueTypeCtorArgument.None)
		{
			SwiftString swiftString = SwiftString.FromString (character);
			fixed (byte* src = swiftString.SwiftData)
				fixed (byte* dst = SwiftData)
					CreateCharacter (src, dst);
		}

		public SwiftCharacter (char character) : this (character.ToString ())
		{
		}

		internal SwiftCharacter (SwiftValueTypeCtorArgument unused)
			: base ()
		{
		}

		~SwiftCharacter ()
		{
			Dispose (false);
		}

		[DllImport (SwiftCore.kXamGlue,
			    EntryPoint = XamGlueConstants.SwiftCharacter_GetCharacterValue)]
		static unsafe extern void GetCharacterValue (byte* character, byte* result);


		[DllImport (SwiftCore.kXamGlue,
			    EntryPoint = XamGlueConstants.SwiftCharacter_CreateCharacter)]
		static unsafe extern void CreateCharacter (byte* str, byte* result);

		public static SwiftCharacter FromCharacter (string s) => new SwiftCharacter (s);
		public static SwiftCharacter FromCharacter (char c) => new SwiftCharacter (c);

		public unsafe override string ToString ()
		{
			SwiftString swiftString = new SwiftString (SwiftValueTypeCtorArgument.None);
			fixed (byte* source = SwiftData) {
				fixed (byte* dest = swiftString.SwiftData) {
					GetCharacterValue (source, dest);
					return swiftString.ToString ();
				}
			}
		}

		public static explicit operator string (SwiftCharacter character) => character.ToString ();
		public static explicit operator SwiftCharacter (string character) => new SwiftCharacter (character);
		public static explicit operator SwiftCharacter (char character) => new SwiftCharacter (character);

		// Unlike the 3 above, this is unsafe as a SwiftCaracter may contain more than a char
		//public static explicit operator char (SwiftCharacter character) => throw new NotImplementedException ();
	}
}

