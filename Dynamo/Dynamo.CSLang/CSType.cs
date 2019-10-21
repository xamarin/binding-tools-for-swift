// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Dynamo;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Dynamo.CSLang {
	public abstract class CSType : DelegatedSimpleElement {
		public override string ToString () => CodeWriter.WriteToString (this);

		public abstract CSFunctionCall Typeof ();
		public abstract CSFunctionCall Default ();
		public abstract CSFunctionCall Ctor ();
	}

	public class CSSimpleType : CSType {
		public CSSimpleType (Type t)
			: this (t.Name)
		{
		}
		public CSSimpleType (string name)
			: this (name, false)
		{
		}

		public CSSimpleType (string name, bool isArray)
		{
			Name = Exceptions.ThrowOnNull (name, "name") + (isArray ? "[]" : "");
		}

		public static explicit operator CSSimpleType (string name)
		{
			return new CSSimpleType (name);
		}

		public static CSSimpleType CreateArray (string name)
		{
			return new CSSimpleType (name, true);
		}

		public CSSimpleType (string name, bool isArray, params CSType [] genericSpecialization)
		{
			IsGeneric = genericSpecialization != null && genericSpecialization.Length > 0;
			GenericTypes = genericSpecialization;
			GenericTypeName = name;
			IsArray = isArray;

			StringBuilder sb = new StringBuilder ();
			sb.Append (Exceptions.ThrowOnNull (name, "name"));
			if (genericSpecialization != null && genericSpecialization.Length > 0) {
				sb.Append ("<");
				int i = 0;
				foreach (CSType type in genericSpecialization) {
					if (i > 0)
						sb.Append (", ");
					sb.Append (type.ToString ());
					i++;
				}
				sb.Append (">");
			}
			if (isArray)
				sb.Append ("[]");
			Name = sb.ToString ();
		}

		public CSSimpleType (string name, bool isArray, params string [] genericSpecialization)
			: this (name, isArray, genericSpecialization.Select (s => new CSSimpleType (s)).ToArray ())
		{
		}

		public string Name { get; private set; }
		public bool IsGeneric { get; private set; }
		public string GenericTypeName { get; private set; }
		public CSType [] GenericTypes { get; private set; }
		public bool IsArray { get; private set; }

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			writer.Write (Name, false);
		}

		public override CSFunctionCall Typeof ()
		{
			return new CSFunctionCall ("typeof", false, new CSIdentifier (Name));
		}

		public override CSFunctionCall Default ()
		{
			return new CSFunctionCall ("default", false, new CSIdentifier (Name));
		}

		public override CSFunctionCall Ctor ()
		{
			return new CSFunctionCall (Name, true);
		}

		static CSSimpleType
			tBool = new CSSimpleType ("bool"),
			tChar = new CSSimpleType ("char"),
			tSbyte = new CSSimpleType ("sbyte"),
			tShort = new CSSimpleType ("short"),
			tInt = new CSSimpleType ("int"),
			tLong = new CSSimpleType ("long"),
			tFloat = new CSSimpleType ("float"),
			tByte = new CSSimpleType ("byte"),
			tUshort = new CSSimpleType ("ushort"),
			tUint = new CSSimpleType ("uint"),
			tUlong = new CSSimpleType ("ulong"),
			tDouble = new CSSimpleType ("double"),
			tString = new CSSimpleType ("string"),
			tObject = new CSSimpleType ("object"),
			tIntPtr = new CSSimpleType ("IntPtr"),
			tVoid = new CSSimpleType ("void"),
			tByteStar = new CSSimpleType ("byte *"),
			tType = new CSSimpleType ("Type"),
			tVar = new CSSimpleType ("var"),
			tNfloat = new CSSimpleType ("nfloat")
			;

		public CSSimpleType Star {
			get {
				if (Name.EndsWith ("[]")) {
					throw new NotImplementedException ("Blindly making an array a pointer doesn't do what you think.");
				} else {
					return new CSSimpleType (Name + " *", false);
				}
			}
		}

		public static CSSimpleType Bool { get { return tBool; } }
		public static CSSimpleType Char { get { return tChar; } }
		public static CSSimpleType SByte { get { return tSbyte; } }
		public static CSSimpleType Short { get { return tShort; } }
		public static CSSimpleType Int { get { return tInt; } }
		public static CSSimpleType Long { get { return tLong; } }
		public static CSSimpleType Float { get { return tFloat; } }
		public static CSSimpleType Byte { get { return tByte; } }
		public static CSSimpleType UShort { get { return tUshort; } }
		public static CSSimpleType UInt { get { return tUint; } }
		public static CSSimpleType ULong { get { return tUlong; } }
		public static CSSimpleType Double { get { return tDouble; } }
		public static CSSimpleType String { get { return tString; } }
		public static CSSimpleType Object { get { return tObject; } }
		public static CSSimpleType IntPtr { get { return tIntPtr; } }
		public static CSSimpleType Void { get { return tVoid; } }
		public static CSSimpleType ByteStar { get { return tByteStar; } }
		public static CSSimpleType Type { get { return tType; } }
		public static CSSimpleType Var { get { return tVar; } }
		public static CSSimpleType NFloat { get { return tNfloat; } }
	}

	public class CSGenericReferenceType : CSType {
		public CSGenericReferenceType (int depth, int index)
		{
			Depth = depth;
			Index = index;
			InterfaceConstraints = new List<CSType> ();
		}

		// this doesn't really belong here, but I'm going to need it.
		public List<CSType> InterfaceConstraints { get; private set; }

		public int Depth { get; private set; }
		public int Index { get; private set; }
		public Func<int, int, string> ReferenceNamer { get; set; }

		protected override void LLWrite (ICodeWriter writer, object o)
		{
			writer.Write (Name, true);
		}

		public string Name {
			get {
				Func<int, int, string> namer = ReferenceNamer ?? DefaultNamer;
				return namer (Depth, Index);
			}
		}

		const string kNames = "TUVWABCDEFGHIJKLMN";

		public static string DefaultNamer (int depth, int index)
		{
			if (depth < 0 || depth >= kNames.Length)
				throw new ArgumentOutOfRangeException (nameof (depth));
			if (index < 0)
				throw new ArgumentOutOfRangeException (nameof (index));
			return String.Format ("{0}{1}", kNames [depth], index);
		}

		public override CSFunctionCall Typeof ()
		{
			return new CSFunctionCall ("typeof", false, new CSIdentifier (Name));
		}

		public override CSFunctionCall Default ()
		{
			return new CSFunctionCall ("default", false, new CSIdentifier (Name));
		}

		public override CSFunctionCall Ctor ()
		{
			return new CSFunctionCall (Name, true);
		}
	}
}

