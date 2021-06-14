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

		string CpuToString ()
		{
			switch (Cpu) {
			case TargetCpu.Arm64: return "arm64";
			case TargetCpu.Armv7: return "armv7";
			case TargetCpu.Armv7s: return "armv7s";
			case TargetCpu.I386: return "i386";
			case TargetCpu.X86_64: return "x86_64";
			default:
				throw new ArgumentOutOfRangeException (nameof (Cpu));
			}
		}

		string ManufacturerToString ()
		{
			switch (Manufacturer) {
			case TargetManufacturer.Apple: return "apple";
			default: throw new ArgumentOutOfRangeException (nameof (Manufacturer));
			}
		}

		string OperatingSystemToString ()
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
	}
}
