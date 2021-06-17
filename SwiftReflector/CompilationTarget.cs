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

		public string ManufacturerToString ()
		{
			switch (Manufacturer) {
			case TargetManufacturer.Apple: return "apple";
			default: throw new ArgumentOutOfRangeException (nameof (Manufacturer));
			}
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
