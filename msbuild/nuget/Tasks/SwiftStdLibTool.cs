// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BindingToolsForSwift.Tasks
{
	public class SwiftStdLibTool : ToolTask
	{
		[Required]
		public string Platform { get; set; }

		[Required]
		public string BundlePath { get; set; }

		[Required]
		public string AppName { get; set; }
		
		protected override string ToolName => "swift-stdlib-tool";
		protected override string GenerateFullPathToTool () => "/usr/bin/xcrun";

		protected override string GenerateCommandLineCommands ()
		{
			string resourcePath = Platform == "macosx" ? $"{BundlePath}/Contents/MonoBundle/" : $"{BundlePath}/Frameworks";
			return $"swift-stdlib-tool --copy --platform {Platform} --scan-folder {BundlePath} --destination {resourcePath} --verbose";
		}

		public override bool Execute ()
		{
			if (!base.Execute ())
				return false;
			return !Log.HasLoggedErrors;
		}

		protected override void LogEventsFromTextOutput (string singleLine, MessageImportance messageImportance)
		{
			try { // We first try to use the base logic, which shows up nicely in XS.
				base.LogEventsFromTextOutput (singleLine, messageImportance);
			}
			catch { // But when that fails, just output the message to the command line and XS will output it raw
				Log.LogMessage (messageImportance, "{0}", singleLine);
			}
		}
	}
}
