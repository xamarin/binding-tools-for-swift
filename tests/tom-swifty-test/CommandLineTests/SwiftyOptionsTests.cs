// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SwiftReflector.IOUtils;
using tomswifty;

namespace CommandLineTests {

	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class SwiftyOptionsTests {


		/// <summary>
		/// IEnumerable that returns all different test cases
		/// for the command line. This classs allows to create
		/// more complicated test data.
		/// </summary>
		class CommandLineCases : IEnumerable
		{
			public IEnumerator GetEnumerator ()
			{
				yield return new object [] {
					new string [] {
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"Default options.",
				};
				yield return new object [] {
					new string [] {
						"-h"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PrintHelp = true,
						PInvokeClassPrefix  = null,
					},
					"Print help options (-h).",
				};
				yield return new object [] {
					new string [] {
						"-help"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						PrintHelp = true,
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"Print help options (-help).",
				};
				yield return new object [] {
					new string [] {
						"-demangle"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = true,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"demangle options.",
				};
				yield return new object [] {
					new string [] {
						"-swift-lib-path=foo",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = Path.GetFullPath("foo"),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'swift-lib-path=foo' options.",
				};
				yield return new object [] {
					new string [] {
						"-swift-bin-path=foo",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = Path.GetFullPath("foo"),
						PInvokeClassPrefix  = null,
					},
					"'swift-bin-path=foo' options.",
				};
				yield return new object [] {
					new string [] {
						"-retain-xml-reflection",
					},
					new SwiftyOptions  {
						RetainXmlReflection = true,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"retain-xml-reflection options.",
				};
				yield return new object [] {
					new string [] {
						"-retain-swift-wrappers",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = true,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"retain-swift-wrappers options.",
				};
				yield return new object [] {
					new string [] {
						"-pinvoke-class-prefix=foo",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = "foo",
					},
					"'pinvoke-class-prefix=foo' options.",
				};
				yield return new object [] {
					new string [] {
						"-print-stack-trace",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = true,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'print-stack-trace' options.",
				};
				yield return new object [] {
					new string [] {
						"-module-name=foo",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						ModuleName = "foo",
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'module-name=foo' options.",
				};
				yield return new object [] {
					new string [] {
						"-module-name",
						"foo"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						ModuleName = "foo",
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'module-name foo' options.",
				};
				yield return new object [] {
					new string [] {
						"-global-class-name=foo",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "foo",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'global-class-name=foo' options.",
				};
				yield return new object [] {
					new string [] {
						"-arch=64",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'arch=64' options.",
				};
				yield return new object [] {
					new string [] {
						"-arch=32",
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = false,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
					},
					"'arch=32' options.",
				};
				var singleLibPathOptions = new SwiftyOptions {
					RetainXmlReflection = false,
					RetainSwiftWrappingCode = false,
					PrintStackTrace = false,
					WrappingModuleName = null,
					GlobalClassName = "TopLevelEntities",
					TargetPlatformIs64Bit = true,
					SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
					Demangle = false,
					SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
					SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
					PInvokeClassPrefix = null,
				};
				singleLibPathOptions.DylibPaths.Add (Path.GetFullPath("first"));
				yield return new object [] {
					new string [] {
						"-Lfirst",
					},
					singleLibPathOptions,
					"'-Lfirst' options.",
				};
				yield return new object [] {
					new string [] {
						"-L=first",
					},
					singleLibPathOptions,
					"'-L=first' options.",
				};
				yield return new object [] {
					new string [] {
						"-library-directory=first",
					},
					singleLibPathOptions,
					"'-library-directory first' options.",
				};
				yield return new object [] {
					new string [] {
						"-library-directory=first",
					},
					singleLibPathOptions,
					"'-library-directory=first' options.",
				};
				var multiLibPathOptions = new SwiftyOptions {
					RetainXmlReflection = false,
					RetainSwiftWrappingCode = false,
					PrintStackTrace = false,
					WrappingModuleName = null,
					GlobalClassName = "TopLevelEntities",
					TargetPlatformIs64Bit = true,
					SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
					Demangle = false,
					SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
					SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
					PInvokeClassPrefix = null,
				};
				multiLibPathOptions.DylibPaths.Add (Path.GetFullPath("first"));
				multiLibPathOptions.DylibPaths.Add (Path.GetFullPath("second"));
				yield return new object [] {
					new string [] {
						"-Lfirst",
						"-Lsecond"
					},
					multiLibPathOptions,
					"'-Lfirst -Lsecond' options.",
				};
				yield return new object [] {
					new string [] {
						"-L=first",
						"-L=second"
					},
					multiLibPathOptions,
					"'-L=first -L=second' options.",
				};
				yield return new object [] {
					new string [] {
						"-library-directory=first",
						"-Lsecond"
					},
					multiLibPathOptions,
					"'-library-directory first -Lsecond' options.",
				};
				yield return new object [] {
					new string [] {
						"-library-directory=first",
						"-L=second"
					},
					multiLibPathOptions,
					"'-library-directory=first -L=second' options.",
				};
				var singleModulePathOptions = new SwiftyOptions {
					RetainXmlReflection = false,
					RetainSwiftWrappingCode = false,
					PrintStackTrace = false,
					WrappingModuleName = null,
					GlobalClassName = "TopLevelEntities",
					TargetPlatformIs64Bit = true,
					SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
					Demangle = false,
					SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
					SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
					PInvokeClassPrefix = null,
				};
				singleModulePathOptions.ModulePaths.Add (Path.GetFullPath ("first"));
				yield return new object [] {
					new string [] {
						"-Mfirst",
					},
					singleModulePathOptions,
					"'-Mfirst' options.",
				};
				yield return new object [] {
					new string [] {
						"-M=first",
					},
					singleModulePathOptions,
					"'-M=first' options.",
				};
				var multiModulePathOptions = new SwiftyOptions {
					RetainXmlReflection = false,
					RetainSwiftWrappingCode = false,
					PrintStackTrace = false,
					WrappingModuleName = null,
					GlobalClassName = "TopLevelEntities",
					TargetPlatformIs64Bit = true,
					SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
					Demangle = false,
					SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
					SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
					PInvokeClassPrefix = null,
				};
				multiModulePathOptions.ModulePaths.Add (Path.GetFullPath ("first"));
				multiModulePathOptions.ModulePaths.Add (Path.GetFullPath ("second"));
				yield return new object [] {
					new string [] {
						"-Mfirst",
						"-Msecond"
					},
					multiModulePathOptions,
					"'-Mfirst -Msecond' options.",
				};
				yield return new object [] {
					new string [] {
						"-M=first",
						"-M=second"
					},
					multiModulePathOptions,
					"'-M=first -M=second' options.",
				};
				var singleCombinedPathOptions = new SwiftyOptions {
					RetainXmlReflection = false,
					RetainSwiftWrappingCode = false,
					PrintStackTrace = false,
					WrappingModuleName = null,
					GlobalClassName = "TopLevelEntities",
					TargetPlatformIs64Bit = true,
					SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
					Demangle = false,
					SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
					SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
					PInvokeClassPrefix = null,
				};
				singleCombinedPathOptions.DylibPaths.Add (Path.GetFullPath("first"));
				singleCombinedPathOptions.ModulePaths.Add (Path.GetFullPath("first"));
				yield return new object [] {
					new string [] {
						"-Cfirst",
					},
					singleCombinedPathOptions,
					"'-Cfirst' options.",
				};
				yield return new object [] {
					new string [] {
						"-C=first",
					},
					singleCombinedPathOptions,
					"'-C=first' options.",
				};
				var multiCombinedPathOptions = new SwiftyOptions {
					RetainXmlReflection = false,
					RetainSwiftWrappingCode = false,
					PrintStackTrace = false,
					WrappingModuleName = null,
					GlobalClassName = "TopLevelEntities",
					TargetPlatformIs64Bit = true,
					SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
					Demangle = false,
					SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
					SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
					PInvokeClassPrefix = null,
				};
				multiCombinedPathOptions.DylibPaths.Add (Path.GetFullPath("first"));
				multiCombinedPathOptions.DylibPaths.Add (Path.GetFullPath("second"));
				multiCombinedPathOptions.ModulePaths.Add (Path.GetFullPath("first"));
				multiCombinedPathOptions.ModulePaths.Add (Path.GetFullPath("second"));
				yield return new object [] {
					new string [] {
						"-Cfirst",
						"-Csecond"
					},
					multiCombinedPathOptions,
					"'-Cfirst -Csecond' options.",
				};
				yield return new object [] {
					new string [] {
						"-C=first",
						"-C=second"
					},
					multiCombinedPathOptions,
					"'-C=first -C=second' options.",
				};
				yield return new object [] {
					new string [] {
						"-ofirst"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						OutputDirectory = Path.GetFullPath ("first"),
					},
					"'-ofirst' options.",
				};
				yield return new object [] {
					new string [] {
						"-o=first"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						OutputDirectory = Path.GetFullPath ("first"),
					},
					"'-o=first' options.",
				};
				yield return new object [] {
					new string [] {
						"-verbose"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						Verbosity = 1,
					},
					"'-verbose' options.",
				};
				yield return new object [] {
					new string [] {
						"-v"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						Verbosity = 1,
					},
					"'-v' options.",
				};
				yield return new object [] {
					new string [] {
						"-version"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						PrintVersion = true,
					},
					"'-version' options.",
				};
				yield return new object [] {
					new string [] {
						"-h"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						PrintHelp = true,
					},
					"'-h' options.",
				};
				yield return new object [] {
					new string [] {
						"-?"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						PrintHelp = true,
					},
					"'-?' options.",
				};
				yield return new object [] {
					new string [] {
						"-help"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = null,
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						PrintHelp = true,
					},
					"'-help' options.",
				};

				yield return new object [] {
					new string [] {
						"-wrapping-module-name=foo"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = "foo",
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						PrintHelp = false,
					},
					"'-wrapping-module-name=foo' options.",
				};


				yield return new object [] {
					new string [] {
						"-wrapping-module-name",
						"foo"
					},
					new SwiftyOptions  {
						RetainXmlReflection = false,
						RetainSwiftWrappingCode = false,
						PrintStackTrace = false,
						WrappingModuleName = "foo",
						GlobalClassName = "TopLevelEntities",
						TargetPlatformIs64Bit = true,
						SwiftGluePath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftGluePath ()),
						Demangle = false,
						SwiftLibPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftLibPath ()),
						SwiftBinPath = PosixHelpers.RealPath (SwiftyOptions.FindSwiftBinPath ()),
						PInvokeClassPrefix  = null,
						PrintHelp = false,
					},
					"'-wrapping-module-name foo' options.",
				};
				
			}
		}

		[TestCaseSource(typeof(CommandLineCases))]
		public void TestCommandLineParsing (string[] args, SwiftyOptions expectedOptions, string testMessage)
		{
			var options = new SwiftyOptions ();
			var extra = options.ParseCommandLine (args);
			Assert.AreEqual (0, extra.Count, "Extra parameters");

			var d = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "first");
			// compare the values
			Assert.AreEqual (expectedOptions.RetainXmlReflection, options.RetainXmlReflection, $"{testMessage} - {nameof(SwiftyOptions.RetainXmlReflection)}");
			Assert.AreEqual (expectedOptions.RetainSwiftWrappingCode, options.RetainSwiftWrappingCode, $"{testMessage} - {nameof(SwiftyOptions.RetainSwiftWrappingCode)}");
			Assert.AreEqual (expectedOptions.PrintStackTrace, options.PrintStackTrace, $"{testMessage} - {nameof(SwiftyOptions.PrintStackTrace)}");
			Assert.AreEqual (expectedOptions.ModuleName, options.ModuleName, $"{testMessage} - {nameof(SwiftyOptions.ModuleName)}");
			Assert.AreEqual (expectedOptions.WrappingModuleName, options.WrappingModuleName, $"{testMessage} - {nameof(SwiftyOptions.WrappingModuleName)}");
			Assert.AreEqual (expectedOptions.GlobalClassName, options.GlobalClassName, $"{testMessage} - {nameof(SwiftyOptions.GlobalClassName)}");
			Assert.AreEqual (expectedOptions.TargetPlatformIs64Bit, options.TargetPlatformIs64Bit, $"{testMessage} - {nameof(SwiftyOptions.TargetPlatformIs64Bit)}");
			Assert.AreEqual (expectedOptions.PrintHelp, options.PrintHelp, $"{testMessage} - {nameof(SwiftyOptions.PrintHelp)}");
			Assert.AreEqual (expectedOptions.Demangle, options.Demangle, $"{testMessage} - {nameof(SwiftyOptions.Demangle)}");
			Assert.AreEqual (expectedOptions.PInvokeClassPrefix, options.PInvokeClassPrefix, $"{testMessage} - {nameof(SwiftyOptions.PInvokeClassPrefix)}");
			Assert.AreEqual (expectedOptions.PrintStackTrace, options.PrintStackTrace, $"{testMessage} - {nameof(SwiftyOptions.PrintStackTrace)}");
			Assert.AreEqual (expectedOptions.Verbose, options.Verbose, $"{testMessage} - {nameof(SwiftyOptions.Verbose)}");
			Assert.AreEqual (expectedOptions.PrintVersion, options.PrintVersion, $"{testMessage} - {nameof(SwiftyOptions.PrintVersion)}");
			
			Assert.AreEqual (expectedOptions.PrintHelp, options.PrintHelp, $"{testMessage} - {nameof(SwiftyOptions.PrintHelp)}");

			if (expectedOptions.SwiftGluePath != null)
				Assert.AreEqual (expectedOptions.SwiftGluePath, options.SwiftGluePath,
					$"{testMessage} - {nameof (SwiftyOptions.SwiftGluePath)}");
			else
				Assert.Null (options.SwiftGluePath, $"{testMessage} - {nameof (SwiftyOptions.SwiftGluePath)}");



			if (expectedOptions.SwiftLibPath != null)
				Assert.AreEqual (expectedOptions.SwiftLibPath, options.SwiftLibPath,
					$"{testMessage} - {nameof (SwiftyOptions.SwiftLibPath)}");
			else
				Assert.Null (options.SwiftLibPath, $"{testMessage} - {nameof (SwiftyOptions.SwiftLibPath)}");
				
			if (expectedOptions.SwiftBinPath != null)
				Assert.AreEqual (expectedOptions.SwiftBinPath, options.SwiftBinPath,
					$"{testMessage} - {nameof (SwiftyOptions.SwiftBinPath)}");
			else
				Assert.Null (options.SwiftBinPath, $"{testMessage} - {nameof (SwiftyOptions.SwiftBinPath)}");
			
			if (expectedOptions.OutputDirectory != null)
				Assert.AreEqual (expectedOptions.OutputDirectory, options.OutputDirectory,
					$"{testMessage} - {nameof (SwiftyOptions.OutputDirectory)}");
			else
				Assert.Null (options.OutputDirectory, $"{testMessage} - {nameof (SwiftyOptions.OutputDirectory)}");
				
			// path collections
			
			Assert.AreEqual (expectedOptions.DylibPaths.Count, options.DylibPaths.Count, $"{testMessage} - {nameof(SwiftyOptions.DylibPaths)}.Count");
			for (var i = 0; i < expectedOptions.DylibPaths.Count; i++)
			{
				Assert.AreEqual (expectedOptions.DylibPaths[i], options.DylibPaths[i],
					$"{testMessage} - {nameof (SwiftyOptions.DylibPaths)}[{i}]");
			}
			
			Assert.AreEqual (expectedOptions.ModulePaths.Count, options.ModulePaths.Count, $"{testMessage} - {nameof(SwiftyOptions.ModulePaths)}.Count");
			for (var i = 0; i < expectedOptions.ModulePaths.Count; i++)
			{
				Assert.AreEqual (expectedOptions.ModulePaths[i], options.ModulePaths[i],
					$"{testMessage} - {nameof (SwiftyOptions.ModulePaths)}[{i}]");
			}
			
			
				
		}
	}
}
