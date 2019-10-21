// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Xamarin.MacDev.Tasks;

namespace BindingToolsForSwift.Tasks
{
	public class PostProcessSwiftTask : Task
	{
		[Required]
		public string OutputDirectory { get; set; }

		[Required]
		public string ToolPath { get; set; }
		
		[Required]
		public string WrappingName { get; set; }
		
		static void MoveAndReplace (string file, string finalLocation)
		{
			File.Delete (finalLocation);
			File.Move (file, finalLocation);
		}

		// XamWrapping comes out as a raw dylib but other
		// parts of SoM expect it to be a framework, so let's play pretend
		// https://github.com/xamarin/maccore/issues/1229
		string CreateWrappingFramework (string libraryName)
		{
			string originalLibrary = Path.Combine (OutputDirectory, libraryName);
			if (!File.Exists (originalLibrary))
				throw new InvalidOperationException ($"{libraryName} not found in expected location ({originalLibrary}).");

			Log.LogMessage (MessageImportance.Normal, $"Processing {originalLibrary}");  

			string frameworkPath = Path.Combine (OutputDirectory, libraryName + ".framework");
			Directory.CreateDirectory (frameworkPath);

			string finalLibraryPath = Path.Combine (frameworkPath, libraryName);
			MoveAndReplace (originalLibrary, finalLibraryPath);
			return finalLibraryPath;
		}

		static ProcessStartInfo GetProcessStartInfo (string tool, string args)
		{
			var startInfo = new ProcessStartInfo (tool, args);
			startInfo.WorkingDirectory = Environment.CurrentDirectory;
			startInfo.CreateNoWindow = true;
			return startInfo;
		}

		int Run (string program, string args)
		{
			using (var stderr = new StringWriter ()) {
				using (var process = ProcessUtils.StartProcess (GetProcessStartInfo (program, args), null, null)) {
					process.Wait ();
					int exitCode = process.Result;
					if (exitCode != 0)
						Log.LogError ($"{program} returned exit code {exitCode} unexpectingly. See error for more details:\n{stderr}");
					return exitCode;
				}	
			}
		}

		public override bool Execute ()
		{
			string libraryPath = CreateWrappingFramework (WrappingName);
			if (libraryPath == null)
				return false;

			return !Log.HasLoggedErrors;
		}
	}
}
