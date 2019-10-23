// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin;

namespace SwiftReflector.IOUtils {
	public class InfoPList {
		public static void MakeInfoPList (string pathToLibrary, string pathToPlistFile)
		{
			using (FileStream stm = new FileStream (pathToLibrary, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				// oh dog, this code is so fragile.
				PLDict dict = MakeDefaultDict (pathToLibrary);

				string os = null;
				MachOFile.MinOSVersion lc = null;
				using (var files = MachO.Read (pathToLibrary, ReadingMode.Deferred)) {
					foreach (var file in files) {
						var osmin = file.MinOS;
						if (osmin == null)
							throw new NotSupportedException ("dylib files without a minimum supported operating system load command are not supported.");
						switch (osmin.Platform) {
						case MachO.Platform.IOS:
						case MachO.Platform.IOSSimulator:
							os = "iphoneos";
							break;
						case MachO.Platform.TvOS:
						case MachO.Platform.TvOSSimulator:
							os = "appletvos";
							break;
						case MachO.Platform.WatchOS:
						case MachO.Platform.WatchOSSimulator:
							os = "watchos";
							break;
						default:
							break;
						}
						if (os != null) {
							lc = osmin;
							break;
						}
					}
				}

				if (os != null && lc != null) {
					AddInOSSpecifics (os, lc, dict);
				}

				if (File.Exists (pathToPlistFile)) {
					File.Delete (pathToPlistFile);
				}
				using (FileStream pliststm = new FileStream (pathToPlistFile, FileMode.Create)) {
					dict.ToXml (pliststm);
				}
			}
		}

		static void AddInOSSpecifics (string os, MachOFile.MinOSVersion osmin, PLDict dict)
		{
			Dictionary<string, string> versioninfo = SdkVersion (os);
			string val = null;
			dict ["DTPlatformName"] = new PLString ("DTPlatformName");
			if (versioninfo.TryGetValue ("ProductBuildVersion", out val)) {
				dict ["DTPlatformBuild"] = new PLString (val);
				dict ["DTSDKBuild"] = new PLString (val);
				val = null;
			}

			if (versioninfo.TryGetValue ("PlatformVersion", out val)) {
				dict ["DTPlatformVersion"] = new PLString (val);
				dict ["DTSDKName"] = new PLString (os + val);
			}

			dict ["MinimumOSVersion"] = new PLString (osmin.Version.ToString ());

			PLArray supportedPlatforms = new PLArray ();
			dict ["CFBundleSupportedPlatforms"] = supportedPlatforms;
			switch (os) {
			case "iphoneos": // iPhoneOS
				supportedPlatforms.Add (new PLString ("iPhoneOS"));
				PLArray devfamily = new PLArray ();
				devfamily.Add (new PLInteger (1));
				devfamily.Add (new PLInteger (2));
				dict ["UIDeviceFamily"] = devfamily;
				break;
			case "appletvos": // AppleTVOS
				supportedPlatforms.Add (new PLString ("AppleTVOS"));
				break;
			case "watchos": // WatchOS
				supportedPlatforms.Add (new PLString ("WatchOS"));
				break;
			default:
				break;
			}
		}

		static PLDict MakeDefaultDict (string pathToLibrary)
		{
			PLDict dict = new PLDict ();
			dict [CFKey.CFBundleDevelopmentRegion.ToString ()] = new PLString ("en");
			dict [CFKey.CFBundleIdentifier.ToString ()] = new PLString ("xamarin.tomswifty." + Path.GetFileName (pathToLibrary));
			dict [CFKey.CFBundleInfoDictionaryVersion.ToString ()] = new PLString ("6.0");
			dict [CFKey.CFBundleName.ToString ()] = new PLString (Path.GetFileName (pathToLibrary));
			dict [CFKey.CFBundlePackageType.ToString ()] = new PLString ("FMWK");
			dict [CFKey.CFBundleVersion.ToString ()] = new PLString ("1");
			dict [CFKey.CFBundleShortVersionString.ToString ()] = new PLString ("1.0");
			dict [CFKey.CFBundleSignature.ToString ()] = new PLString ("????");
			dict ["NSPrincipalClass"] = new PLString ("");
			dict [CFKey.CFBundleExecutable.ToString ()] = new PLString (Path.GetFileName (pathToLibrary));
			dict ["DTCompiler"] = new PLString ("com.apple.compilers.llvm.clang.1_0");

			try {
				string buildVersion = ExecAndCollect.Run ("/usr/bin/sw_vers", "-buildVersion");
				dict ["BuildMachineOSBuild"] = new PLString (buildVersion.TrimEnd ('\r', ' ', '\n'));
			} catch { }
			try {
				string buildversions = ExecAndCollect.Run ("/usr/bin/xcodebuild", "-version");
				string [] textLines = buildversions.Split (new [] { Environment.NewLine }, StringSplitOptions.None);
				foreach (string line in textLines) {
					if (line.Contains ("Xcode")) {
						int lastSpace = line.LastIndexOf (' ');
						if (lastSpace >= 0) {
							string [] versionparts = line.Substring (lastSpace + 1).Split ('.');
							if (versionparts.Length == 3) {
								string xcodeversion = String.Format ("{0}{1}{2}{3}",
																	versionparts [0].Length <= 1 ? "0" : "",
																	versionparts [0],
																	versionparts [1],
																   versionparts [2]);
								dict ["DTXcode"] = new PLString (xcodeversion);

							}
						}
					} else if (line.Contains ("Build")) {
						int lastSpace = line.LastIndexOf (' ');
						if (lastSpace >= 0) {
							string xcodebuildversion = line.Substring (lastSpace + 1).TrimEnd ('\r', '\n', ' ');
							dict ["DTXcodeBuild"] = new PLString (xcodebuildversion);
						}
					}
				}
			} catch { }
			return dict;
		}



		static Dictionary<string, string> SdkVersion (string os)
		{
			Dictionary<string, string> versionstuff = new Dictionary<string, string> ();
			string versiontext = ExecAndCollect.Run ("/usr/bin/xcodebuild", $"-version -sdk {os}");
			string [] textLines = versiontext.Split (new [] { Environment.NewLine }, StringSplitOptions.None);

			// returns a set of lines in the form "key: value";

			foreach (string line in textLines) {
				if (line.Contains (':')) {
					string [] parts = line.Split (':');
					if (parts.Length != 2) continue;
					string key = parts [0].Trim ();
					string val = parts [1].Trim ();
					versionstuff [key] = val;
				}
			}
			return versionstuff;
		}
	}
}
