// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SwiftReflector.IOUtils;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using SwiftReflector.SwiftXmlReflection;
using System.Collections.Generic;
using SwiftReflector.ExceptionTools;
using System.Linq;
using Dynamo;
using System.Text.RegularExpressions;
using ObjCRuntime;
using SwiftReflector.TypeMapping;
using SwiftReflector.SwiftInterfaceReflector;

namespace SwiftReflector {

	public class SwiftCompilerOptions
	{
		public string ModuleName { get; }
		public string [] IncludeDirectories { get; } // can be null
		public string [] LibraryDirectories { get; } // can be null
		public string [] InputModules { get; } // can be null

		public SwiftCompilerOptions (string moduleName, string [] includeDirectories, string [] libraryDirectories, string [] inputModules)
		{
			ModuleName = moduleName;
			IncludeDirectories = includeDirectories;
			LibraryDirectories = libraryDirectories;
			InputModules = inputModules;
		}
	}

	public class FileSystemModuleLoader : IModuleLoader {
		IEnumerable<string> locations;
		public FileSystemModuleLoader (IEnumerable<string> locations)
		{
			this.locations = Exceptions.ThrowOnNull (locations, nameof (locations));
		}

		public bool Load (string moduleName, TypeDatabase into)
		{
			if (into.ModuleNames.Contains (moduleName))
				return true;
			foreach (var location in locations) {
				var file = Path.Combine (location, moduleName);
				if (TryLoadFile (file, into, "", ".xml", ".XML"))
					return true;
			}
			return false;
		}

		bool TryLoadFile (string file, TypeDatabase into, params string [] extensions)
		{
			foreach (var ext in extensions) {
				var candidate = file + ext;
				if (!File.Exists (candidate))
					continue;
				Errors = into.Read (file);
				return true;
			}
			return false;
		}

		public ErrorHandling Errors { get; private set; }
	}

	public class CustomSwiftCompiler : IDisposable {
		readonly SwiftTargetCompilerInfo CompilerInfo;

		DisposableTempDirectory tempDirectory;
		bool disposeTempDirectory;

		public CustomSwiftCompiler (SwiftTargetCompilerInfo compilerInfo,
		                            DisposableTempDirectory fileProvider, // can be null
		                            bool disposeSuppliedDirectory)
		{
			CompilerInfo = compilerInfo;
			tempDirectory = fileProvider ?? new DisposableTempDirectory (null, true);
			disposeTempDirectory = fileProvider != null ? disposeSuppliedDirectory : true;
			PrimaryReflectionStrategy = ReflectionStrategy.Parser;
			SecondaryReflectionStrategy = ReflectionStrategy.Compiler;
		}

		public void CompileString (SwiftCompilerOptions compilerOptions, string codeString)
		{
			if (String.IsNullOrEmpty (codeString)) {
				throw new ArgumentNullException (nameof(codeString));
			}
			using (MemoryStream stream = new MemoryStream ()) {
				StreamWriter writer = new StreamWriter (stream);
				writer.Write (codeString);
				writer.Flush ();
				stream.Position = 0;
				Compile (compilerOptions, false, stream);
			}
		}

		public void Compile (SwiftCompilerOptions compilerOptions, bool outputIsFramework, Stream codeStream)
		{
			string pathName = tempDirectory.UniquePath (null, null, "swift");
			using (FileStream stm = new FileStream (pathName, FileMode.Create)) {
				codeStream.CopyTo (stm);
			}
			Compile (compilerOptions, outputIsFramework, pathName);
		}

		public void Compile (SwiftCompilerOptions compilerOptions, bool outputIsFramework, params string [] files)
		{
			string args = BuildCompileArgs (compilerOptions, outputIsFramework, files);
			if (Verbose)
				Console.WriteLine ("Compiling swift files: " + files.InterleaveCommas ());

			Launch (CompilerInfo.CustomSwiftc, args);
			var outputLib = Path.Combine (DirectoryPath, $"lib{compilerOptions.ModuleName}.dylib");
			if (outputIsFramework) {
				string srcLib = outputLib;
				File.Copy (srcLib,
					  Path.Combine (DirectoryPath, compilerOptions.ModuleName), true);
				File.Delete (srcLib);
			}
		}


		string BuildCompileArgs (SwiftCompilerOptions compilerOptions, bool outputIsFramework, string [] files)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("-emit-library ");
			sb.Append ("-enable-library-evolution ");
			sb.Append ("-emit-module-interface ");

			if (CompilerInfo.HasTarget)
				sb.Append ("-target ").Append (CompilerInfo.Target).Append (" ");

			sb.Append ("-sdk ").Append (GetSDKPath ()).Append (" ");

			if (compilerOptions.IncludeDirectories != null) {
				foreach (string includeDirectory in compilerOptions.IncludeDirectories) {
					if (!String.IsNullOrEmpty (includeDirectory)) {
						sb.Append ("-I ").Append (QuoteIfNeeded (includeDirectory)).Append (' ');
					}
				}
			}

			AppendLibraryAndFrameworks (sb, compilerOptions.LibraryDirectories ?? new string [] { }, compilerOptions.InputModules ?? new string [] { }, true);

			//if (Verbose)
			//sb.Append(" -v ");

			string moduleName = compilerOptions.ModuleName;
			if (!String.IsNullOrEmpty (moduleName)) {
				sb.Append ("-emit-module -module-name ").Append (QuoteIfNeeded (moduleName));
			}

			if (outputIsFramework) {
				sb.Append (" -Xlinker -rpath -Xlinker @executable_path/Frameworks -Xlinker -rpath -Xlinker @loader_path/Frameworks");
				sb.Append (" -Xlinker -rpath -Xlinker @executable_path -Xlinker -rpath -Xlinker @rpath");
				sb.Append (" -Xlinker -final_output -Xlinker ").Append (QuoteIfNeeded (moduleName));
				sb.Append (" -Xlinker -install_name -Xlinker ").Append (QuoteIfNeeded ($"@rpath/{moduleName}.framework/{moduleName}"));
			} else {
				sb.Append (" -Xlinker -rpath -Xlinker @executable_path/Frameworks -Xlinker -rpath -Xlinker @loader_path/Frameworks");
				sb.Append (" -Xlinker -rpath -Xlinker @executable_path -Xlinker -rpath -Xlinker @rpath");
				sb.Append (" -Xlinker -rpath -Xlinker @loader_path");
				sb.Append (" -Xlinker -install_name -Xlinker ").Append (QuoteIfNeeded ($"@rpath/lib{moduleName}.dylib"));
			}

			foreach (string file in files) {
				sb.Append (" ").Append (file);
			}
			return sb.ToString ();
		}

		// \d+(?:\.\d+){1,3}
		// matches major.minor[.rev[.build]]
		const string kVersionStringMatch = "\\d+(?:\\.\\d+){1,3}";
		public Version GetCompilerVersion ()
		{
			try {
				var output = Launch (CompilerInfo.CustomSwiftc, "--version");
				var matcher = new Regex (kVersionStringMatch, RegexOptions.None);
				var theMatch = matcher.Match (output);
				return theMatch.Success ? new Version (theMatch.Value) : null;

			} catch {
				return null;
			}
		}

		public Stream ReflectToStream (IEnumerable<string> includeDirectories, IEnumerable<string> libraryDirectories,
		                               string extraArgs, params string [] moduleNames)
		{
			string pathName = tempDirectory.UniquePath (null, null, "xml");

			var output = Reflect (includeDirectories, libraryDirectories, pathName, extraArgs, moduleNames);
			ThrowOnCompilerVersionMismatch (output, moduleNames);
			MemoryStream stm = new MemoryStream ();
			using (FileStream xml = new FileStream (pathName, FileMode.Open)) {
				xml.CopyTo (stm);
				stm.Seek (0, SeekOrigin.Begin);
			}
			return stm;
		}

		public XDocument ReflectToXDocument (IEnumerable<string> includeDirectories,
								 IEnumerable<string> libraryDirectories, string extraArgs,
		                                                 params string [] moduleNames)
		{
			var moduleNameAggregate = moduleNames.Aggregate ((s1, s2) => s1 + s2);
			var pathName = tempDirectory.UniquePath (moduleNameAggregate, null, "xml");

			includeDirectories = includeDirectories ?? new string [] { tempDirectory.DirectoryPath };
			libraryDirectories = libraryDirectories ?? new string [] { tempDirectory.DirectoryPath };

			var modulesInLibraries = SwiftModuleFinder.FindModuleNames (libraryDirectories, CompilerInfo.Target);

			List<ISwiftModuleLocation> locations = SwiftModuleFinder.GatherAllReferencedModules (modulesInLibraries,
													     includeDirectories, CompilerInfo.Target);
			string output = "";
			try {
				output = Reflect (locations.Select (loc => loc.DirectoryPath), libraryDirectories, pathName, extraArgs, moduleNames);
			} finally {
				locations.DisposeAll ();
			}
			ThrowOnCompilerVersionMismatch (output, moduleNames);
			using (var stm = new FileStream (pathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				return XDocument.Load (stm);
			}
		}


		public List<ModuleDeclaration> ReflectToModules (IEnumerable<string> includeDirectories,
		                                                 IEnumerable<string> libraryDirectories, string extraArgs,
		                                                 params string [] moduleNames)
		{
			var moduleNameAggregate = moduleNames.Aggregate ((s1, s2) => s1 + s2);
			var pathName = tempDirectory.UniquePath (moduleNameAggregate, null, "xml");

			includeDirectories = includeDirectories ?? new string [] { tempDirectory.DirectoryPath };
			libraryDirectories = libraryDirectories ?? new string [] { tempDirectory.DirectoryPath };

			var modulesInLibraries = SwiftModuleFinder.FindModuleNames (libraryDirectories, CompilerInfo.Target);

			List<ISwiftModuleLocation> locations = SwiftModuleFinder.GatherAllReferencedModules (modulesInLibraries,
			                                                                                     includeDirectories, CompilerInfo.Target);

			ReflectWithStrategies (locations.Select (loc => loc.DirectoryPath), libraryDirectories, pathName, extraArgs, moduleNames);
			return Reflector.FromXmlFile (pathName);
		}

		public void ReflectWithStrategies (IEnumerable<string> includeDirectories, IEnumerable<string> libraryDirectories,
				     string outputFile, string extraArgs, params string [] moduleNames)
		{
			// both set to none, you really want nothing?
			if (PrimaryReflectionStrategy == ReflectionStrategy.None && SecondaryReflectionStrategy == ReflectionStrategy.None) {
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 71, "Both reflection strategies are set to 'None'.");
			}
			// primary set to none, just take the secondary
			var primary = PrimaryReflectionStrategy == ReflectionStrategy.None ? SecondaryReflectionStrategy : PrimaryReflectionStrategy;
			// if we did the previous change, set the secondary to none because it looks like you only want one.
			var secondary = PrimaryReflectionStrategy == ReflectionStrategy.None ? ReflectionStrategy.None : SecondaryReflectionStrategy;
			// if they're the same, don't both with a secondary
			if (secondary == primary)
				secondary = ReflectionStrategy.None;
			try {
				ReflectWithStrategy (includeDirectories, libraryDirectories, outputFile, extraArgs, primary, moduleNames);
			} catch (Exception err) {
				if (secondary == ReflectionStrategy.None)
					throw err;
				ReflectWithStrategy (includeDirectories, libraryDirectories, outputFile, extraArgs, secondary, moduleNames);
			}
		}

		public void ReflectWithStrategy (IEnumerable<string> includeDirectories, IEnumerable<string> libraryDirectories,
				     string outputFile, string extraArgs, ReflectionStrategy strategy,
				     params string [] moduleNames)
		{
			if (strategy == ReflectionStrategy.None)
				throw new ArgumentOutOfRangeException (nameof (strategy));

			if (strategy == ReflectionStrategy.Compiler) {
				var output = Reflect (includeDirectories, libraryDirectories, outputFile, extraArgs, moduleNames);
				ThrowOnCompilerVersionMismatch (output, moduleNames);
			} else if (strategy == ReflectionStrategy.Parser) {
				ReflectWithParser (includeDirectories, libraryDirectories, outputFile, moduleNames);
			}
		}

		public void ReflectWithParser (IEnumerable<string> includeDirectories, IEnumerable<string> libraryDirectory, string outputFile, string [] moduleNames)
		{
			if (moduleNames.Length != 1)
				throw new ArgumentOutOfRangeException (nameof (moduleNames), "Only one module supported for parser");

			if (ReflectionTypeDatabase == null)
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 72, "Parser reflector requires a TypeDatabase");

			var fileName = $"{moduleNames [0]}.swiftinterface";

			var paths = includeDirectories.Select (dir => Path.Combine (dir, fileName));

			var path = paths.FirstOrDefault (p => File.Exists (p));

			if (path == null) {
				var lookedIn = includeDirectories.InterleaveStrings (", ");
				throw new FileNotFoundException ($"Did not find {fileName} in {lookedIn}");
			}

			var moduleLoader = new FileSystemModuleLoader (libraryDirectory);
			var reflector = new SwiftInterfaceReflector.SwiftInterfaceReflector (ReflectionTypeDatabase, moduleLoader);
			var xdoc = reflector.Reflect (path);
			xdoc.Save (outputFile);
		}

		public string Reflect (IEnumerable<string> includeDirectories, IEnumerable<string> libraryDirectories,
		                     string outputFile, string extraArgs, params string [] moduleNames)
		{
			libraryDirectories = libraryDirectories ?? new string [] { };
			includeDirectories = includeDirectories ?? new string [] { DirectoryPath };

			string args = BuildReflectArgs (includeDirectories, libraryDirectories, outputFile, extraArgs, moduleNames);

			if (Verbose)
				Console.WriteLine ("Reflecting on types from module(s): " + moduleNames.InterleaveCommas ());

			return Launch (CompilerInfo.CustomSwift, args);
		}

		string BuildReflectArgs (IEnumerable<string> includeDirectories, IEnumerable<string> libraryDirectories,
		                         string outputFile, string extraArgs, string [] moduleNames)
		{
			StringBuilder sb = new StringBuilder ();


			sb.Append ("-xamreflect ").Append ("-enable-library-evolution ");

			if (CompilerInfo.HasTarget)
				sb.Append ("-target ").Append (CompilerInfo.Target).Append (" ");

			sb.Append ("-sdk ").Append (QuoteIfNeeded (GetSDKPath ())).Append (" ");

			foreach (string includeDirectory in includeDirectories) {
				if (!String.IsNullOrEmpty (includeDirectory)) {
					sb.Append ("-I ").Append (QuoteIfNeeded (includeDirectory)).Append (' ');
				}
			}

			List<string> augmentedModuleNames = moduleNames.ToList ();
			if (!augmentedModuleNames.Contains ("XamGlue"))
				augmentedModuleNames.Add ("XamGlue");

			AppendLibraryAndFrameworks (sb, libraryDirectories, augmentedModuleNames.ToArray (), false);

			sb.Append ("-o ").Append (QuoteIfNeeded (outputFile));
			foreach (string module in moduleNames) {
				sb.Append (' ').Append (QuoteIfNeeded (module));
			}
			return sb.ToString ();
		}


		void AppendLibraryAndFrameworks (StringBuilder sb, IEnumerable<string> candidateDirectories,
		                                 string [] modNames, bool addReference)
		{
			List<string> candidates = candidateDirectories.ToList ();
			List<string> fwkDirectories = new List<string> ();
			List<string> libs = new List<string> ();
			List<string> fwks = new List<string> ();

			foreach (string moduleName in modNames) {
				string swiftModule = moduleName + ".swiftmodule";
				bool addedFwk = false;
				for (int i = 0; i < candidates.Count (); i++) {
					// if it's a framework, there will be a one-to-one mapping of module names -> framework directories
					// remove the path from the candidate and move it to fwkDirectories
					// and add the name to fwks
					if (SwiftModuleFinder.IsAppleFramework (candidates [i], swiftModule)) {
						fwks.Add (moduleName);
						fwkDirectories.Add (candidates [i]);
						candidates.RemoveAt (i);
						addedFwk = true;
						break;
					}
				}
				// if we didn't add a framework, it's probably a library
				if (!addedFwk) {
					libs.Add (moduleName);
				}
				addedFwk = false;
			}

			foreach (string libdir in candidates) {
				sb.Append ("-L ").Append (QuoteIfNeeded (libdir)).Append (' ');
			}

			foreach (string fwkdir in fwkDirectories) {
				string parentdir = ReliablePath.GetParentDirectory (fwkdir);
				sb.Append ("-F ").Append (QuoteIfNeeded (parentdir)).Append (' ');
			}

			sb.Append ("-L ").Append (QuoteIfNeeded (CompilerInfo.LibDirectory)).Append (' ');

			if (addReference) {
				foreach (string lib in libs) {
					sb.Append ("-l").Append (lib).Append (' ');
				}
				foreach (string fwk in fwks) {
					sb.Append ("-framework ").Append (fwk).Append (' ');
				}

			}
		}

		static string QuoteIfNeeded (string s)
		{
			return Xamarin.Utils.StringUtils.Quote (s);
		}


		string Launch (string executable, string args)
		{
			return ExecAndCollect.Run (executable, args, workingDirectory: DirectoryPath, verbose: Verbose);
		}


		public static void ThrowOnCompilerVersionMismatch (string output, params string[] modules)
		{
			if (String.IsNullOrEmpty (output))
				return;
			var pattern = "error: module compiled with Swift [0-9]+.[0-9]+ cannot be imported by the Swift [0-9]+.[0-9]+ compiler";
			var match = Regex.Match (output, pattern);
			if (match.Success) {
				var module = String.Join ("", modules.InterleaveCommas());
				throw ErrorHelper.CreateError (ReflectorError.kCantHappenBase + 69, $"Error reflecting on {module}. {match.Value}");
			}
		}

		string sdkPath;
		string GetSDKPath ()
		{
			if (sdkPath == null)
				sdkPath = ExecAndCollect.Run ("/usr/bin/xcrun", "--show-sdk-path --sdk " + CompilerInfo.SDKPath, workingDirectory: string.Empty).Trim ();
			return sdkPath;
		}

		public DisposableTempDirectory FilenameProvider { get { return tempDirectory; } }

		public string DirectoryPath { get { return tempDirectory.DirectoryPath; } }

		public bool Verbose { get; set; }

		public ReflectionStrategy PrimaryReflectionStrategy { get; set; }
		public ReflectionStrategy SecondaryReflectionStrategy { get; set; }
		public TypeDatabase ReflectionTypeDatabase { get; set; }

		#region IDisposable implementation
		~CustomSwiftCompiler ()
		{
			Dispose (false);
		}
		bool disposed = false;
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposed) {
				if (disposing) {
					if (disposeTempDirectory)
						tempDirectory.Dispose ();
				}
				disposed = true;
			}
		}
		#endregion
	}
}

