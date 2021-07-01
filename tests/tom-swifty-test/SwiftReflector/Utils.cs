// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using tomwiftytest;
using Xamarin;
using Dynamo;
using Dynamo.CSLang;
using SwiftReflector.TypeMapping;
using System.Text;
using NUnit.Framework;
using SwiftReflector.Demangling;
using SwiftReflector.IOUtils;

using Xamarin.Utils;

namespace SwiftReflector {
	public static class Utils {

		static Utils ()
		{
			// Caching ModuleDefinitions can speed up test runs significantly.
			SwiftReflector.Importing.BindingImporter.CacheModules = true;
		}

		public static CustomSwiftCompiler DefaultSwiftCompiler (DisposableTempDirectory provider = null, string target = "x86_64-apple-macosx10.9")
		{
			var targetInfo = Compiler.SystemCompilerLocation.GetTargetInfo (target);
			return new CustomSwiftCompiler (targetInfo, provider, false);
		}

		public static CustomSwiftCompiler CompileSwift (string swiftCode, DisposableTempDirectory provider = null, string moduleName = "Xython", string target = "x86_64-apple-macosx10.9")
		{
			CustomSwiftCompiler compiler = DefaultSwiftCompiler (provider, target : target);
			string [] includeDirectories = null;
			List<string> libraryDirectories = new List<string> { compiler.DirectoryPath, Compiler.kSwiftRuntimeGlueDirectory };
			if (provider != null)
			{
				includeDirectories = new string [] { provider.DirectoryPath };
				libraryDirectories.Add (provider.DirectoryPath);
				File.Copy (Path.Combine (Compiler.kXamGlueSourceDirectory, "module.map"), Path.Combine (provider.DirectoryPath, "module.map"));
				File.Copy (Path.Combine (Compiler.kXamGlueSourceDirectory, "registeraccess.h"), Path.Combine (provider.DirectoryPath, "registeraccess.h"));
			}

			SwiftCompilerOptions options = new SwiftCompilerOptions (moduleName, includeDirectories, libraryDirectories.ToArray (), new string [] { "XamGlue" });
			compiler.CompileString (options, swiftCode);
			return compiler;
		}

		public static CustomSwiftCompiler DefaultSystemCompiler (DisposableTempDirectory provider = null, string target = "x86_64-apple-macosx10.9")
		{
			var targetInfo = Compiler.SystemCompilerLocation.GetTargetInfo (target);
			return new CustomSwiftCompiler (targetInfo, provider, false);
		}

		public static CustomSwiftCompiler SystemCompileSwift (string swiftCode, DisposableTempDirectory provider = null, string moduleName = "Xython", string target = "x86_64-apple-macosx10.9")
		{
			CustomSwiftCompiler compiler = DefaultSystemCompiler (provider, target: target);
			string [] includeDirectories = null;
			List<string> libraryDirectories = new List<string> { compiler.DirectoryPath, Compiler.kSwiftRuntimeGlueDirectory };
			if (provider != null) {
				includeDirectories = new string [] { provider.DirectoryPath };
				libraryDirectories.Add (provider.DirectoryPath);
				File.Copy (Path.Combine (Compiler.kXamGlueSourceDirectory, "module.map"), Path.Combine (provider.DirectoryPath, "module.map"));
				File.Copy (Path.Combine (Compiler.kXamGlueSourceDirectory, "registeraccess.h"), Path.Combine (provider.DirectoryPath, "registeraccess.h"));
			}

			SwiftCompilerOptions options = new SwiftCompilerOptions (moduleName, includeDirectories, libraryDirectories.ToArray (), new string [] { "XamGlue" });
			compiler.CompileString (options, swiftCode);
			return compiler;
		}


		public static NewClassCompiler DefaultCSharpCompiler (UniformTargetRepresentation inputTarget, UnicodeMapper unicodeMapper = null)
		{
			Exceptions.ThrowOnNull (inputTarget, nameof (inputTarget));
			ClassCompilerOptions compilerOptions = new ClassCompilerOptions (targetPlatformIs64Bit : true, verbose : false, retainReflectedXmlOutput : true, retainSwiftWrappers : true, inputTarget);
			return new NewClassCompiler (Compiler.SystemCompilerLocation, compilerOptions, unicodeMapper ?? UnicodeMapper.Default);
		}

		public static string CompileToCSharp (DisposableTempDirectory provider, string outputDirectory = null, string moduleName = "Xython", string target = "x86_64-apple-macosx10.9", IEnumerable<string> additionalTypeDatabases = null, bool separateProcess = false, UnicodeMapper unicodeMapper = null, int expectedErrorCount = -1)
		{
			List<string> typeDatabases = Compiler.kTypeDatabases;
			if (additionalTypeDatabases != null)
				typeDatabases.AddRange (additionalTypeDatabases);


			ClassCompilerLocations classCompilerLocations = new ClassCompilerLocations (new List<string> { provider.DirectoryPath, Compiler.kSwiftRuntimeGlueDirectory },
												    new List<string> { provider.DirectoryPath, Compiler.kSwiftRuntimeGlueDirectory },
												    typeDatabases);
			ClassCompilerNames compilerNames = new ClassCompilerNames (moduleName, null);
			var localErrors = new ErrorHandling ();
			var inputTarget = UniformTargetRepresentation.FromPath (moduleName, classCompilerLocations.LibraryDirectories, localErrors);
			if (inputTarget == null)
				inputTarget = UniformTargetRepresentation.FromPath (moduleName, classCompilerLocations.ModuleDirectories, localErrors);
			if (inputTarget == null) {
				CheckErrors (localErrors, 0);
				return null;
			}

			NewClassCompiler ncc = DefaultCSharpCompiler (inputTarget, unicodeMapper);

			if (separateProcess) {
				var args = new StringBuilder ();
				args.Append ($"--debug ");
				args.Append ($"{Path.Combine (Path.GetDirectoryName (ncc.GetType ().Assembly.Location), "tom-swifty.exe")} ");
				args.Append ($"--swift-bin-path={StringUtils.Quote (Compiler.SystemCompilerLocation.SwiftCompilerBin)} ");
				args.Append ($"--swift-lib-path={StringUtils.Quote (Path.GetDirectoryName (Compiler.SystemCompilerLocation.SwiftCompilerLib))} ");
				args.Append ($"--retain-xml-reflection ");
				foreach (var db in typeDatabases)
					args.Append ($"--type-database-path={StringUtils.Quote (db)} ");
				args.Append ($"--retain-swift-wrappers ");
				args.Append ($"--wrapping-module-name={StringUtils.Quote (moduleName)}Wrapping ");
				foreach (var l in classCompilerLocations.LibraryDirectories)
					args.Append ($"-L {StringUtils.Quote (l)} ");
				foreach (var m in classCompilerLocations.ModuleDirectories)
					args.Append ($"-M {StringUtils.Quote (m)} ");
				args.Append ($"-o {StringUtils.Quote (outputDirectory ?? provider.DirectoryPath)} ");
				args.Append ($"--module-name={StringUtils.Quote (moduleName)} ");
				return ExecAndCollect.Run ("mono", args.ToString ());
			} else {
				ErrorHandling errors = ncc.CompileToCSharp (classCompilerLocations, compilerNames, new List<string> { target }, outputDirectory ?? provider.DirectoryPath);
				CheckErrors (errors, expectedErrorCount);
				return null;
			}
		}

		public static void CheckErrors (ErrorHandling errors, int expectedErrorCount = -1)
		{
			if (!errors.AnyErrors && expectedErrorCount <= 0)
				return;
			if (expectedErrorCount > 0 && errors.ErrorCount == expectedErrorCount)
				return;
			var sb = new StringBuilder ();
			if (expectedErrorCount > 0 && errors.ErrorCount != expectedErrorCount)
				sb.Append ($"Expected {expectedErrorCount} errors, but got {errors.ErrorCount}\n");
			foreach (ReflectorError error in errors.Errors)
				sb.Append (error.Message).Append ('\n');
			Assert.Fail (sb.ToString ());
		}
	}
}

