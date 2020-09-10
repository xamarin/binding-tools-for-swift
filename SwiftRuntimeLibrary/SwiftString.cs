// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;


using System.Text;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {

	[SwiftTypeName ("Swift.String")]
	[SwiftStruct (SwiftCoreConstants.LibSwiftCore, SwiftCoreConstants.SwiftString_NominalTypeDescriptor, SwiftCoreConstants.SwiftString_Metadata, "")]
	public sealed class SwiftString : SwiftNativeValueType, ISwiftStruct {
		internal SwiftString (SwiftValueTypeCtorArgument unused)
			: base ()
		{
		}

		public unsafe SwiftString (string s) : this (SwiftValueTypeCtorArgument.None)
		{
			fixed (byte* result = SwiftData) {
				FromUTF16Pointer (s, s.Length, result);
			}
		}

		~SwiftString ()
		{
			Dispose (false);
		}

		// As of the first release of Swift 5.0, using FromUTF16Pointer leaks memory for every string
		// converted that is longer than ~13 bytes. It's quite possible that it's an issue in the ownership of
		// the memory and that it's kept around because swift doesn't claim the ownership. This is not clear from
		// the documentation of that particular call. Until I have final word on either a fix or how to use it or that
		// there won't be a fix, I'm leaving it around as a pinvoke and in swift glue.

		[DllImport (SwiftCore.kXamGlue,
			EntryPoint = XamGlueConstants.SwiftString_FromUTF16Pointer)]
		static extern unsafe void FromUTF16Pointer ([MarshalAs (UnmanagedType.LPWStr)]string s, nint size, byte* result);

		[DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.SwiftString_FromUTF8Pointer)]
		static extern unsafe void FromUTF8Pointer ([MarshalAs (UnmanagedType.LPTStr)] string source, byte* result);

		public static SwiftString FromString (string s)
		{
			return new SwiftString (s);
		}

		[DllImport (SwiftCore.kXamGlue,
			EntryPoint = XamGlueConstants.SwiftString_UTF8StringSize)]
		static extern unsafe int UTF8StringSize (byte* swiftString);


		[DllImport (SwiftCore.kXamGlue,
			EntryPoint = XamGlueConstants.SwiftString_CopyStringToUTF8Buffer)]
		static extern unsafe void CopyStringToUTF8Buffer (byte* buffer, byte* swiftString);


		public unsafe override string ToString ()
		{
			fixed (byte* swiftData = SwiftData) {
				int utf8size = UTF8StringSize (swiftData);
				byte[] block = new byte [utf8size];
				fixed (byte* buffer = block) {
					CopyStringToUTF8Buffer (buffer, swiftData);
					return Encoding.UTF8.GetString (block);
				}
			}
		}

		public static explicit operator SwiftString (string s)
		{
			return SwiftString.FromString (s);
		}
	}
}

