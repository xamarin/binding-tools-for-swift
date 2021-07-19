using System;
namespace SwiftReflector {
	public class CompilationTarget {
		public CompilationTarget (PlatformName os, TargetCpu cpu, TargetEnvironment environment,
			Version minimumOSVersion, TargetManufacturer manufacturer = TargetManufacturer.Apple)
		{
			if (os == PlatformName.None)
				throw new ArgumentOutOfRangeException (nameof (os));
			OperatingSystem = os;
			Cpu = cpu;
			Environment = environment;
			if (minimumOSVersion == null)
				throw new ArgumentNullException (nameof (minimumOSVersion));
			MinimumOSVersion = minimumOSVersion;
			Manufacturer = manufacturer;
		}

		public CompilationTarget (string target)
		{
			var pieces = target.Split ('-');
			if (pieces.Length < 3 || pieces.Length > 4)
				throw new ArgumentOutOfRangeException (nameof (target));
			var cpuString = pieces [0];
			var manufacturerString = pieces [1];
			var osString = pieces [2];
			var environmentString = pieces.Length == 4 ? pieces [3] : null;
			TargetCpu cpu;
			if (!TryGetTargetCpu (cpuString, out cpu))
				throw new ArgumentOutOfRangeException (nameof (target), $"Unknown cpu {cpuString}");
			TargetManufacturer manufacturer;
			if (!TryGetManufacturer (manufacturerString, out manufacturer))
				throw new ArgumentOutOfRangeException (nameof (target), $"Unknown manufacturer {manufacturerString}");
			Version minOSVersion;
			PlatformName os;
			if (!TryGetOSVersion (osString, out os, out minOSVersion))
				throw new ArgumentOutOfRangeException (nameof (target), $"Error in os/version {os}");
			TargetEnvironment environment;
			if (!TryGetTargetEnvironment (environmentString, out environment))
				throw new ArgumentOutOfRangeException (nameof (target), $"Unknown target environment {environmentString}");
			OperatingSystem = os;
			Cpu = cpu;
			Environment = environment;
			MinimumOSVersion = minOSVersion;
			Manufacturer = manufacturer;
		}

		public PlatformName OperatingSystem {
			get; private set;
		}

		public TargetCpu Cpu {
			get; private set;
		}

		public TargetEnvironment Environment {
			get; private set;
		}

		public TargetManufacturer Manufacturer {
			get; private set;
		}

		public Version MinimumOSVersion {
			get; private set;
		}

		public bool SameIgnoreOSVersion (CompilationTarget other)
		{
			return other.OperatingSystem == OperatingSystem && other.Cpu == Cpu &&
				other.Environment == Environment && other.Manufacturer == Manufacturer;
		}

		public CompilationTarget WithMinimumOSVersion (Version version)
		{
			if (version == MinimumOSVersion)
				return this;
			return new CompilationTarget (this.OperatingSystem, this.Cpu,
				this.Environment, version, this.Manufacturer);
		}

		public override string ToString ()
		{
			var environment = Environment == TargetEnvironment.Device ? "" : "-simulator";
			return $"{CpuToString ()}-{ManufacturerToString ()}-{OperatingSystemToString ()}{MinimumOSVersion}{environment}";
		}

		public bool TryGetTargetEnvironment (string str, out TargetEnvironment environment)
		{
			if (str == null) {
				environment = TargetEnvironment.Device;
				return true;
			} else if (str == "simulator") {
				environment = TargetEnvironment.Simulator;
				return true;
			} else {
				environment = TargetEnvironment.Device; // doesn't matter
				return false;
			}
		}

		public string EnvironmentToString ()
		{
			// do NOT call this in ToString - this is for displaying exceptions
			return Environment == TargetEnvironment.Device ? "device" : "simulator";
		}

		public string CpuToString ()
		{
			switch (Cpu) {
			case TargetCpu.Arm64: return "arm64";
			case TargetCpu.Arm64_32: return "arm64_32";
			case TargetCpu.Armv7: return "armv7";
			case TargetCpu.Arm7vk: return "armv7k";
			case TargetCpu.Armv7s: return "armv7s";
			case TargetCpu.I386: return "i386";
			case TargetCpu.X86_64: return "x86_64";
			default:
				throw new ArgumentOutOfRangeException (nameof (Cpu));
			}
		}

		static bool TryGetTargetCpu (string str, out TargetCpu cpu)
		{
			switch (str.ToLowerInvariant ()) {
			case "arm64": cpu = TargetCpu.Arm64; break;
			case "arm64_32": cpu = TargetCpu.Arm64_32; break;
			case "armv7": cpu = TargetCpu.Armv7; break;
			case "armv7k": cpu = TargetCpu.Arm7vk; break;
			case "armv7s": cpu = TargetCpu.Armv7s; break;
			case "i386": cpu = TargetCpu.I386; break;
			case "x86_64": cpu = TargetCpu.X86_64; break;
			default:
				cpu = TargetCpu.Arm64; // doesn't matter
				return false;
			}
			return true;
		}

		static bool TryGetManufacturer (string str, out TargetManufacturer manufacturer)
		{
			manufacturer = TargetManufacturer.Apple; // doesn't matter
			return str.ToLowerInvariant () == "apple";
		}	

		public string ManufacturerToString ()
		{
			switch (Manufacturer) {
			case TargetManufacturer.Apple: return "apple";
			default: throw new ArgumentOutOfRangeException (nameof (Manufacturer));
			}
		}

		static char [] digits = new char [] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		static bool TryGetOSVersion (string str, out PlatformName platform, out Version minOSVersion)
		{
			minOSVersion = null;
			platform = PlatformName.None;

			var firstNumber = str.IndexOfAny (digits);
			if (firstNumber < 0)
				return false;

			var osStr = str.Substring (0, firstNumber);
			var versionStr = str.Substring (firstNumber);

			switch (osStr.ToLowerInvariant ()) {
			case "ios": platform = PlatformName.iOS; break;
			case "macosx": platform = PlatformName.macOS; break;
			case "tvos": platform = PlatformName.tvOS; break;
			case "watchos": platform = PlatformName.watchOS; break;
			default: platform = PlatformName.None; break;
			}
			minOSVersion = new Version (versionStr);

			return platform != PlatformName.None;
		}

		public string OperatingSystemToString ()
		{
			switch (OperatingSystem) {
			case PlatformName.iOS: return "ios";
			case PlatformName.macOS: return "macosx";
			case PlatformName.tvOS: return "tvos";
			case PlatformName.watchOS: return "watchos";
			default:
				throw new ArgumentOutOfRangeException (nameof (OperatingSystem));
			}
		}

		public override int GetHashCode ()
		{
			// lazy, expensive, but terse.
			return ToString ().GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			if (obj is CompilationTarget other) {
				return other.Cpu == Cpu && other.Manufacturer == Manufacturer &&
					other.OperatingSystem == OperatingSystem && other.MinimumOSVersion == MinimumOSVersion &&
					other.Environment == Environment;
			} else {
				return false;
			}
		}
	}
}
