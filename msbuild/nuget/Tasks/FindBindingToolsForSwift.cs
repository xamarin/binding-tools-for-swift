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
	public class FindBindingToolsForSwift : Task
	{
		[Required]
		public string ProjectDirectory { get; set; }

		[Output]
		public string FoundDirectory { get; set; }

		// This is a bit of a development hack, we want to be able
		// to use our "local" SoM bits however even local nuget msbuild get extracted to ~/.nuget
		// and we lose where the tooling should come from. Walk up for a Pack-Man/binding-tools-for-swift/binding-tools-for-swift
		// file if /p:BindingToolsForSwiftPath=PATH is unset
		public override bool Execute ()
		{
			string current = Path.GetDirectoryName (ProjectDirectory);

			while (current != null) {
				string canidate = Path.Combine (current, "Pack-Man/binding-tools-for-swift/");
				if (File.Exists (Path.Combine (canidate, "binding-tools-for-swift"))) {
 					FoundDirectory = canidate;
					break;
				}
				current = Path.GetDirectoryName (current);
			}

			if (FoundDirectory == null)
				Log.LogError ($"Unable to locate local binding-tools-for-swift relative to project and was not set via /p:BindingToolsForSwiftPath=PATH");

			return !Log.HasLoggedErrors;
		}
	}
}
