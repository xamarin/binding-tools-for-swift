// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin;

namespace SwiftReflector {
	public class MachOHelpers {
		public MachOHelpers ()
		{
		}

		public static List<string> TargetsFromDylib (Stream stm)
		{
			List<string> targets = new List<string> ();

			foreach (MachOFile file in MachO.Read (stm)) {
				string arch = ToArchString (file.Architecture);
				var osmin = file.MinOS;
				if (osmin == null)
					throw new NotSupportedException ("dylib files without a minimum supported operating system load command are not supported.");
				targets.Add ($"{arch}-apple-{osmin.OSName}{osmin.Version.ToString ()}");
			}
			return targets;
		}

		public static List<string> CommonTargets (List<string> lt, List<string> commonTo)
		{
			List<string> isec = new List<string> ();
			foreach (string s in lt) {
				if (commonTo.Contains (s))
					isec.Add (s);
			}
			return isec;
		}

		static string ToArchString (MachO.Architectures arch)
		{
			switch (arch) {
			case MachO.Architectures.ARM64:
				return "arm64";
			case MachO.Architectures.ARMv6:
				return "armv6";
			case MachO.Architectures.ARMv7:
				return "armv7";
			case MachO.Architectures.ARMv7s:
				return "armv7s";
			case MachO.Architectures.i386:
				return "i386";
			case MachO.Architectures.x86_64:
				return "x86_64";
			case MachO.Architectures.ARM64e:
				return "arm64e";
			default:
				throw new ArgumentOutOfRangeException (nameof (arch));
			}
		}
	}
}
