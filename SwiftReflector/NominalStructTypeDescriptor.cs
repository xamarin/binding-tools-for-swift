// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SwiftReflector.ExceptionTools;
using SwiftReflector.Demangling;
using ObjCRuntime;

namespace SwiftReflector {
	public class NominalStructTypeDescriptor {
		public string MangledName { get; private set; }
		public uint FieldCount { get; private set; }
		public uint FieldOffsetVectorOffset { get; private set; }
		public List<string> FieldNames { get; private set; }
		public List<ulong> FieldOffsets { get; private set; }

		NominalStructTypeDescriptor (string mangledName, uint fieldCount,
			uint fieldOffsetVectorOffset, List<string> fieldNames, List<ulong> fieldOffsets)
		{
			MangledName = mangledName;
			FieldCount = fieldCount;
			FieldOffsetVectorOffset = fieldOffsetVectorOffset;
			FieldNames = fieldNames;
			FieldOffsets = fieldOffsets;
		}

		public static NominalStructTypeDescriptor FromStream (Stream stm, TLNominalTypeDescriptor tlNom,
		                                                      TLDirectMetadata tlMet, int sizeofMachinePointer)
		{
			if (sizeofMachinePointer != 4 && sizeofMachinePointer != 8) {
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 12, $"was expecting a machine pointer size of either 4 or 8, but got {sizeofMachinePointer}");
			}
			var reader = new BinaryReader (stm);
			reader.BaseStream.Seek ((long)tlNom.Offset, SeekOrigin.Begin);
			string mangledName = ReadRelativeString (reader);
			uint fieldCount = reader.ReadUInt32 ();
			uint fieldOffsetVectorOffset = reader.ReadUInt32 ();
			var fieldNames = ReadRelativeStringArray (reader, fieldCount);
			var fieldOffsets = ReadFieldOffsets (reader, tlMet, sizeofMachinePointer, fieldOffsetVectorOffset, fieldCount);
			return new NominalStructTypeDescriptor (mangledName, fieldCount, fieldOffsetVectorOffset, fieldNames, fieldOffsets);
		}

		static List<ulong> ReadFieldOffsets (BinaryReader reader, TLDirectMetadata tlMet, int sizeofMachinePointer,
			uint fieldOffsetVectorOffset, uint fieldCount)
		{
			reader.BaseStream.Seek ((long)tlMet.Offset + (fieldOffsetVectorOffset * sizeofMachinePointer), SeekOrigin.Begin);
			return ReadMachinePointers (reader, sizeofMachinePointer, fieldCount);
		}

		static List<ulong> ReadMachinePointers (BinaryReader reader, int sizeofMachinePointer, uint fieldCount)
		{
			var pointers = new List<ulong> ();
			for (int i = 0; i < fieldCount; i++) {
				pointers.Add (sizeofMachinePointer == 4 ? (ulong)reader.ReadUInt32 () : reader.ReadUInt64 ());
			}
			return pointers;
		}

		static string ReadRelativeString (BinaryReader reader)
		{
			long targetPosition = reader.BaseStream.Position;
			int offset = reader.ReadInt32 ();
			long savePos = reader.BaseStream.Position;
			reader.BaseStream.Seek (targetPosition + offset, SeekOrigin.Begin);
			string result = ReadNullTerminatedString (reader);
			reader.BaseStream.Seek (savePos, SeekOrigin.Begin);
			return result;
		}

		static List<string> ReadRelativeStringArray (BinaryReader reader, uint count)
		{
			long targetPosition = reader.BaseStream.Position;
			int offset = reader.ReadInt32 ();
			long savePos = reader.BaseStream.Position;
			reader.BaseStream.Seek (targetPosition + offset, SeekOrigin.Begin);

			var result = new List<string> ();
			for (int i = 0; i < count; i++) {
				result.Add (ReadNullTerminatedString (reader));
			}
			reader.BaseStream.Seek (savePos, SeekOrigin.Begin);
			return result;
		}

		static string ReadNullTerminatedString (BinaryReader reader)
		{
			var data = new List<byte> ();
			byte c;
			while ((c = reader.ReadByte ()) != 0) {
				data.Add (c);
			}
			return Encoding.ASCII.GetString (data.ToArray ());
		}
	}
}

