// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using SwiftRuntimeLibrary.SwiftMarshal;

namespace SwiftRuntimeLibrary {
#if NOT_READY_FOR_SWIFT_5
	public class RuntimeDebugging {
		[Flags]
		public enum OutputOptions {
			BasicInformation = 0,
			Vtable = 1,
			TypeDescriptor = 2,
			All = Vtable | TypeDescriptor
		}

		public static string ObjectInformation (ISwiftObject obj, OutputOptions options)
		{
			string hexFormat = IntPtr.Size < 8 ? "X8" : "X16";
			var writer = new StringWriter ();

			if (obj == null) {
				writer.WriteLine ("ISwiftObject is null.");
			} else {
				if (obj.SwiftObject == IntPtr.Zero) {
					writer.WriteLine ("ISwiftObject has a 0 SwiftObject.");
				} else {
					writer.WriteLine ($"ISwiftObject with value {obj.SwiftObject.ToString (hexFormat)}.");
					writer.WriteLine ($"Retain count {StructMarshal.RetainCount (obj)}.");
					if (!SwiftClassObject.IsSwiftClassObject (obj)) {
						writer.WriteLine ("Unable to retrieve class object. Maybe an Objective C object?");
					} else {
						try {
							SwiftClassObject classObj = SwiftClassObject.FromSwiftObject (obj);
							if (classObj == null) {
								writer.WriteLine ("Unable to retrieve class object.");
							} else {
								string name = NameFromClassObject (classObj) ?? "<no name available>";
								writer.WriteLine ($"Instance of {name}.");
							}
							if (classObj.SuperClass != null) {
								string superName = NameFromClassObject (classObj.SuperClass) ?? "<no name available>";
								writer.WriteLine ($"Inherits from {superName}.");
							}
							writer.WriteLine ($"Instance size {classObj.InstanceSize} bytes.");
							writer.WriteLine ($"Is swift 1 class: {classObj.IsSwift1Class}.");
							writer.WriteLine ($"Uses swift 1 reference counting: {classObj.UsesSwift1RefCounting}.");
							if ((options & OutputOptions.Vtable) != 0)
								DumpVTable (writer, hexFormat, classObj.Vtable, classObj.VtableSize);
							if ((options & OutputOptions.TypeDescriptor) != 0) {
								if (classObj.IsNominalTypeDescriptorValid) {
									DumpNominalTypeDescriptor (writer, classObj.NominalTypeDescriptor);
								} else {
									writer.WriteLine ("No type descriptor.");
								}
							}
						} catch (Exception e) {
							writer.WriteLine ($"Error attempting to retrieve class object: {e.Message}");
						}
					}
				}
			}

			return writer.ToString ();
		}

		static string NameFromClassObject (SwiftClassObject classObj)
		{
			if (classObj.IsNominalTypeDescriptorValid) {
				SwiftNominalTypeDescriptor nom = classObj.NominalTypeDescriptor;
				return nom.GetMangledName ();
			} else {
				return null;
			}
		}

		static void DumpVTable (TextWriter writer, string hexFormat, IntPtr vtable, int nEntries)
		{
			if (vtable == IntPtr.Zero)
				return;
			writer.WriteLine ($"Vtable at address {vtable.ToString (hexFormat)}, {nEntries} entries:");
			for (int i = 0; i < nEntries; i++) {
				IntPtr ptrAddress = Marshal.ReadIntPtr (vtable);
				if (ptrAddress == IntPtr.Zero) {
					writer.WriteLine ($"{i}: Null entry");
				} else {
					string name = NameFromAddress (ptrAddress) ?? "no name";
					writer.WriteLine ($"{i}: {ptrAddress.ToString (hexFormat)} {name}");
				}
				vtable = vtable + IntPtr.Size;
			}
		}

		struct dl_info {
			public IntPtr dli_fname;
			public IntPtr dli_fbase;
			public IntPtr dli_sname;
			public IntPtr dli_saddr;
		}

		[DllImport ("libSystem.B.dylib", EntryPoint = "dladdr")]
		static extern int dladdr (IntPtr address, ref dl_info info);

		internal static string NameFromAddress (IntPtr address)
		{
			var info = new dl_info ();
			if (dladdr (address, ref info) == 0)
				return null;
			string name = AsString (info.dli_sname) ?? "unknown";
			string location = AsString (info.dli_fname) ?? "unknown";
			return $"{name} ({location})";
		}
		static string AsString (IntPtr p)
		{
			if (p == IntPtr.Zero)
				return null;
			return Marshal.PtrToStringAnsi (p);
		}

		static void DumpNominalTypeDescriptor (TextWriter writer, SwiftNominalTypeDescriptor desc)
		{
			// In this code, desc.GetKind() will always be for a class, but you never know.
			switch (desc.GetKind ()) {
			case NominalTypeDescriptorKind.Class:
				DumpClassTypeDescriptor (writer, desc);
				break;
			case NominalTypeDescriptorKind.Enum:
				DumpEnumTypeDescriptor (writer, desc);
				break;
			case NominalTypeDescriptorKind.Struct:
				DumpStructTypeDescriptor (writer, desc);
				break;
			case NominalTypeDescriptorKind.None:
				writer.WriteLine ("Unknown descriptor type.");
				break;
			}
		}

		static void DumpClassTypeDescriptor (TextWriter writer, SwiftNominalTypeDescriptor desc)
		{
			DumpLabeledList (writer, "Fields", desc.GetFieldNames ());
			DumpGenericInfo (writer, desc);
		}

		static void DumpEnumTypeDescriptor (TextWriter writer, SwiftNominalTypeDescriptor desc)
		{
			DumpLabeledList (writer, $"Cases ({desc.GetPayloadCaseCount ()} with payloads, {desc.GetEmptyCaseCount ()} empty)", desc.GetCaseNames ());
			DumpGenericInfo (writer, desc);
		}

		static void DumpStructTypeDescriptor (TextWriter writer, SwiftNominalTypeDescriptor desc)
		{
			DumpLabeledList (writer, "Fields", desc.GetFieldNames ());
			DumpGenericInfo (writer, desc);
		}

		static void DumpGenericInfo (TextWriter writer, SwiftNominalTypeDescriptor desc)
		{
			if (!desc.IsGeneric ())
				return;
			writer.WriteLine ($"Generic parameters: {desc.GetGenericParamCount ()} ({desc.GetPrimaryGenericParamCount ()} primary)");
		}

		static void DumpLabeledList (TextWriter writer, string label, string [] list)
		{
			if (list.Length == 0)
				return;
			writer.WriteLine ($"{label}:");
			for (int i = 0; i < list.Length; i++) {
				writer.WriteLine ($"{i}: {list [i]}");
			}
		}
	}
#endif
}
