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
				var isSimulator = IsSimulator (file.Architecture, osmin);
				targets.Add ($"{arch}-apple-{TripleOSName(osmin, MinOSSdk (osmin), isSimulator)}");
			}
			return targets;
		}

		public static List<CompilationTarget> CompilationTargetsFromDylib (Stream stm)
		{
			var targets = new List<CompilationTarget> ();
			foreach (var file in MachO.Read (stm)) {
				try {
					var osmin = file.MinOS;
					if (osmin == null)
						throw new NotSupportedException ("dylib files without a minimum supported operating system load command are not supported.");
					var isSimulator = IsSimulator (file.Architecture, osmin);
					var cpu = ToCpu (file.Architecture);
					var platform = ToPlatform (osmin.Platform);
					var environment = IsSimulator (file.Architecture, osmin) ? TargetEnvironment.Simulator : TargetEnvironment.Device;
					targets.Add (new CompilationTarget (platform, cpu, environment, osmin.Version));
				} catch {
					continue;
				}
			}
			return targets;
		}

		static PlatformName ToPlatform (MachO.Platform platform)
		{
			switch (platform) {
			case MachO.Platform.IOS:
			case MachO.Platform.IOSSimulator:
				return PlatformName.iOS;
			case MachO.Platform.MacOS:
				return PlatformName.macOS;
			case MachO.Platform.TvOS:
			case MachO.Platform.TvOSSimulator:
				return PlatformName.tvOS;
			case MachO.Platform.WatchOS:
			case MachO.Platform.WatchOSSimulator:
				return PlatformName.watchOS;
			default:
				throw new ArgumentOutOfRangeException (nameof (platform));
			}
		}

		static TargetCpu ToCpu (MachO.Architectures arch)
		{
			switch (arch) {
			case MachO.Architectures.ARM64: return TargetCpu.Arm64;
			case MachO.Architectures.ARMv7: return TargetCpu.Armv7;
			case MachO.Architectures.ARMv7s: return TargetCpu.Armv7s;
			case MachO.Architectures.i386: return TargetCpu.I386;
			case MachO.Architectures.x86_64: return TargetCpu.X86_64;
			case MachO.Architectures.ARM64_32: return TargetCpu.Arm64_32;
			case MachO.Architectures.ARM64e: return TargetCpu.Arm64e;
			case MachO.Architectures.ARMv6:
			default:
				throw new ArgumentOutOfRangeException (nameof (arch));
			}
		}

		static string TripleOSName (MachOFile.MinOSVersion minOS, string version, bool isSimulator)
		{
			var simulatorSuffix = isSimulator ? "-simulator" : "";
			switch (minOS.Platform) {
			case MachO.Platform.IOS:
				return $"ios{version}{simulatorSuffix}";
			case MachO.Platform.IOSSimulator:
				return $"ios{version}-simulator";
			case MachO.Platform.MacOS:
				return $"macosx{version}";
			case MachO.Platform.TvOS:
				return $"tvos{version}{simulatorSuffix}";
			case MachO.Platform.TvOSSimulator:
				return $"tvos{version}-simulator";
			case MachO.Platform.WatchOS:
				return $"watchos{version}{simulatorSuffix}";
			case MachO.Platform.WatchOSSimulator:
				return $"watchos{version}-simulator";
			default:
				throw new ArgumentOutOfRangeException (nameof (minOS));
			}
		}

		static bool IsSimulator (MachO.Architectures arch, MachOFile.MinOSVersion minOS)
		{
			// rules, rules, rules.
			// OK - if this is IOS, TvOS, or WatchOS there are two cases:
			// either it is the "old style" which means you figure out if
			// it's a simulator by the CPU architecture of the file. If it's i386 or
			// x86_64, then it's a simulator.
			// If it's a newer file, then the Platform carries the info as to
			// whether or not it's a simulator.
			// Why?
			// Because there exist arm64 iphones and there exist arm64 simulators
			// so looking at the processor architecture no longer works reliably.

			switch (minOS.Platform) {
			case MachO.Platform.MacOS:
				return false;
			case MachO.Platform.IOS:
			case MachO.Platform.TvOS:
			case MachO.Platform.WatchOS:
				return arch == MachO.Architectures.i386 ||
					arch == MachO.Architectures.x86_64;
			case MachO.Platform.IOSSimulator:
			case MachO.Platform.TvOSSimulator:
			case MachO.Platform.WatchOSSimulator:
				return true;
			default:
				throw new ArgumentOutOfRangeException (nameof (minOS));
			}
		}

		static string MinOSSdk (MachOFile.MinOSVersion minOS)
		{
			return minOS.Version.ToString ();
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
