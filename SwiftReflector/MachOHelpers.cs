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
				targets.Add ($"{arch}-apple-{TripleOSName(osmin, MinOSSdk (osmin))}");
			}
			return targets;
		}

		static string TripleOSName (MachOFile.MinOSVersion minOS, string version)
		{
			switch (minOS.Platform) {
			case MachO.Platform.IOS:
				return $"ios{version}";
			case MachO.Platform.IOSSimulator:
				return $"ios{version}-simulator";
			case MachO.Platform.MacOS:
				return $"macosx{version}";
			case MachO.Platform.TvOS:
				return $"tvos{version}";
			case MachO.Platform.TvOSSimulator:
				return $"tvos{version}-simulator";
			case MachO.Platform.WatchOS:
				return $"watchos{version}";
			case MachO.Platform.WatchOSSimulator:
				return $"watchos{version}-simulator";
			default:
				throw new ArgumentOutOfRangeException (nameof (minOS));
			}
		}

		static string MinOSSdk (MachOFile.MinOSVersion minOS)
		{
			var min = minOS.Version < minOS.Sdk ? minOS.Version : minOS.Sdk;
			return min.ToString ();
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
			default:
				throw new ArgumentOutOfRangeException (nameof (arch));
			}
		}
	}
}
