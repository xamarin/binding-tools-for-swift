using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Xamarin.Utils;

namespace BindingToolsForSwift.Tasks
{
	public static class StringBuilderExtensions
	{
		public static void AppendWithSpace (this StringBuilder builder, string s) => builder.Append (s).Append (' ');
	}

	public class SwiftTask : ToolTask
	{
		[Required]
		public string OutputDirectory { get; set; }

		[Required]
		public string SwiftFramework { get; set; }

		[Required]
		public string SwiftToolPath { get; set; }
		
		[Required]
		public string WrappingName { get; set; }
		
		[Required]
		public string AllSwiftFrameworks { get; set; }
		
		public bool Verbose { get; set; }
		
		public string AdditionalArguments { get; set; }

		public string UnicodeMapFile { get; set; }
		
		public bool RetainReflectionXML { get; set; }
		
		public bool RetainWrappers { get; set; }
		
		public string AdditionalTypeDatabases { get; set; }

		protected override string ToolName => SwiftToolPath;
		protected override string GenerateFullPathToTool () => SwiftToolPath;

		protected override bool ValidateParameters ()
		{
			if (!Directory.Exists (SwiftFramework)) {
				Log.LogError ($"Unable to find framework {SwiftFramework}");
				return false;
			}
					
			return true;
		}

		string GenerateModuleArguments ()
		{
			StringBuilder arg = new StringBuilder ();

			foreach (var lib in AllSwiftFrameworks.Split (new char [] { ';' }).Where (x => x != SwiftFramework))
				arg.Append (" -M " + StringUtils.Quote (lib));
				
			string typeDatabasePath = Path.Combine (OutputDirectory, "bindings");
			if (Directory.Exists (typeDatabasePath))
				arg.Append (" --type-database-path " + StringUtils.Quote (typeDatabasePath));
	
			return arg.ToString ();
		}

		protected override string GenerateCommandLineCommands ()
		{
			StringBuilder args = new StringBuilder ();

			args.AppendWithSpace ($"-C {StringUtils.Quote (SwiftFramework)}");
			args.AppendWithSpace ($"-o {StringUtils.Quote (OutputDirectory)}");
			args.AppendWithSpace (AdditionalArguments);
			
			string name = Path.GetFileNameWithoutExtension (SwiftFramework);
			args.AppendWithSpace ($"-module-name {StringUtils.Quote (name)}");
			args.AppendWithSpace ($"-wrapping-module-name {StringUtils.Quote (WrappingName)}");

			args.AppendWithSpace (GenerateModuleArguments ());

			if (Verbose)
				args.AppendWithSpace ("-v");

			if (!String.IsNullOrEmpty (UnicodeMapFile))
				args.AppendWithSpace ($"--unicode-mapping={StringUtils.Quote (UnicodeMapFile)}");

			if (RetainReflectionXML)
				args.AppendWithSpace ("--retain-xml-reflection");

			if (RetainWrappers)
				args.AppendWithSpace ("--retain-swift-wrappers");

			if (AdditionalTypeDatabases != null) {
				foreach (var db in AdditionalTypeDatabases.Split (new char [] { ';' }))
					args.AppendWithSpace ($"--type-database-path={StringUtils.Quote (db)}");
			}

			return args.ToString ();
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
