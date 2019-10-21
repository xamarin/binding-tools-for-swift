using System;
using System.Collections.Generic;
using System.IO;
using ObjCRuntime;
using SwiftReflector.Exceptions;
using SwiftReflector.IOUtils;

namespace SwiftReflector 
{
	public class SwiftCompilerLocation 
	{
		public string SwiftCompilerBin { get; }
		public string SwiftCompilerLib { get; }
		Dictionary<string, SwiftTargetCompilerInfo> TargetInfo = new Dictionary<string, SwiftTargetCompilerInfo> ();

		public SwiftCompilerLocation (string swiftCompilerBin, string swiftCompilerLib)
		{
			SwiftCompilerBin = Ex.ThrowOnNull (swiftCompilerBin, nameof (swiftCompilerBin));
			SwiftCompilerLib = Ex.ThrowOnNull (swiftCompilerLib, nameof (swiftCompilerLib));
			VerifyCompilerLocations ();
		}

		void VerifyCompilerLocations ()
		{
			if (!Directory.Exists (SwiftCompilerBin))
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 19, $"Can't find the swift compiler binary directory '{SwiftCompilerBin}'.");
			if (!Directory.Exists (SwiftCompilerLib))
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 20, $"Can't find the swift compiler library directory '{SwiftCompilerLib}'.");
		}

		public SwiftTargetCompilerInfo GetTargetInfo (string target)
		{
			target = target ?? ""; // Null target valid but used as a key into dictionary

			if (TargetInfo.TryGetValue (target, out SwiftTargetCompilerInfo value))
				return value;

			var info = SwiftTargetCompilerInfo.Create (this, target);
			TargetInfo.Add (target, info);
			return info;
		}
	}

	public class SwiftTargetCompilerInfo
	{
		public string Target { get; }
		public string BinDirectory { get; }
		public string LibDirectory { get; }
		public string CustomSwiftc { get; }
		public string CustomSwift { get; }
		public string SDKPath { get; }

		public bool HasTarget => !string.IsNullOrEmpty (Target);

		internal SwiftTargetCompilerInfo (string target, string binDirectory, string libDirectory, string customSwiftc, string customSwift, string sdkPath)
		{
			Target = target;
			BinDirectory = binDirectory;
			LibDirectory = libDirectory;
			CustomSwiftc = customSwiftc;
			CustomSwift = customSwift;
			SDKPath = sdkPath;
		}

		public static SwiftTargetCompilerInfo Create (SwiftCompilerLocation compilerLocation, string target)
		{
			if (compilerLocation == null)
				throw new ArgumentNullException (nameof (compilerLocation));

			string pathToCompilerLib = compilerLocation.SwiftCompilerLib;
			string sdkPath = "";
			if (target != null) {
				string parent = ReliablePath.GetParentDirectory (pathToCompilerLib);
				sdkPath = SdkForTarget (target);
				pathToCompilerLib = Path.Combine (parent, sdkPath);
			}

			if (!Directory.Exists (pathToCompilerLib)) {
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 18, $"Unable to find path to compiler library directory '{pathToCompilerLib}'.");
			}

			string swiftc = Path.Combine (compilerLocation.SwiftCompilerBin, "swiftc");
			string swift = Path.Combine (compilerLocation.SwiftCompilerBin, "swift");
			return new SwiftTargetCompilerInfo (target, compilerLocation.SwiftCompilerBin, pathToCompilerLib, swiftc, swift, sdkPath);
		}

		static string SdkForTarget (string givenTarget)
		{
			if (String.IsNullOrEmpty (givenTarget))
				return "macosx";
			string [] parts = givenTarget.Split ('-');
			if (parts.Length != 3) {
				throw new Exception ("Expected target to be in the form cpu-apple-os");
			}

			if (parts [2].StartsWith ("macosx", StringComparison.Ordinal))
				return "macosx";
			if (parts [2].StartsWith ("ios", StringComparison.Ordinal)) {
				if (IsSimulator (parts [0]))
					return "iphonesimulator";
				return "iphoneos";
			}
			if (parts [2].StartsWith ("tvos", StringComparison.Ordinal)) {
				return "appletvos";
			}
			if (parts [2].StartsWith ("watchos", StringComparison.Ordinal)) {
				if (IsSimulator (parts [0]))
					return "watchsimulator";
				return "watchos";
			}
			return "macosx";
		}

		static bool IsSimulator (string part)
		{
			return part.StartsWith ("i386", StringComparison.Ordinal) ||
				   part.StartsWith ("x86_64", StringComparison.Ordinal);
		}
	}
}
