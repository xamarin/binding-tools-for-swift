// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace SwiftReflector.IOUtils {
	public class ExecAndCollect {
		public static string Run (string path, string args, string workingDirectory = null, bool verbose = false)
		{
			var output = new StringBuilder ();
			var exitCode = RunCommand (path, args, output: output, verbose: verbose, workingDirectory: workingDirectory);

			// TJ - Read the file as one string.
			//if (args.Contains ("libswiftCore-Swift-AnyKeyPath")) {
			//	string currentLocation = Directory.GetCurrentDirectory ();
			//	//string text = System.IO.File.ReadAllText ("libswiftCore-Swift-AnyKeyPath.swift");
			//	var shellOutput = RunBash ("ls");
			//	string text = System.IO.File.ReadAllText ("/var/folders/nj/446lm_hs4zz72gfhvwglxzvr0000gn/T/libswiftCore-Swift-AnyKeyPath-c6c644.o");
			//	Console.WriteLine ("Contents of WriteText.txt = {0}", text);
			//}

			//DirectoryCopy (".", "../../TJTemp", true);

			var pathToFile = workingDirectory + "/libswiftCore-Swift-ManagedBuffer.swift";
			var outp = output.ToString ();

			// TJ - TODO Exploring what happens if we skip over this catch
			if (exitCode != 0)
				throw new Exception ($"Failed to execute (exit code {exitCode}): {path} {string.Join (" ", args)}\n{output.ToString ()}");
			return output.ToString ();
		}

		public static string [] TJSeparateRun2 (string path, string args, string workingDirectory = null, bool verbose = false)
		{
			var output = new StringBuilder ();
			var exitCode = RunCommand (path, args, output: output, verbose: verbose, workingDirectory: workingDirectory);

			var pathToFile = workingDirectory + "/libswiftCore-Swift-ManagedBuffer.swift";
			var outp = output.ToString ();
			string [] outputs = new string [] { outp, pathToFile };
			return outputs;
		}

		public static string TJSeparateRun (string path, string args, string [] files, string workingDirectory = null, bool verbose = false)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (var f in files) {
				var output = new StringBuilder ();
				var exitCode = RunCommand (path, args + " " + f + " -v", output: output, verbose: verbose, workingDirectory: workingDirectory);

				var pathToFile = workingDirectory + "/libswiftCore-Swift-ManagedBuffer.swift";
				var outp = output.ToString ();
				sb.AppendLine (outp);
			}
			//var output = new StringBuilder ();
			//var exitCode = RunCommand (path, args, output: output, verbose: verbose, workingDirectory: workingDirectory);

			//var pathToFile = workingDirectory + "/libswiftCore-Swift-ManagedBuffer.swift";
			//var outp = output.ToString ();

			// TJ - TODO Exploring what happens if we skip over this catch
			//if (exitCode != 0)
			//	throw new Exception ($"Failed to execute (exit code {exitCode}): {path} {string.Join (" ", args)}\n{output.ToString ()}");
			//return output.ToString ();
			var totalOutput = sb.ToString ();
			return totalOutput;
		}

		public static string RunBash (string script)
		{
			var process = new Process () {
				StartInfo = new ProcessStartInfo {
					FileName = "/bin/bash",
					Arguments = $"-c \"{script}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};
			process.Start ();
			string result = process.StandardOutput.ReadToEnd ();
			process.WaitForExit ();
			return result;
		}

		//// TJ adding to try to view temp files
		//static void DirectoryCopy (string sourceDirName, string destDirName, bool copySubDirs)
		//{
		//	// Get the subdirectories for the specified directory.
		//	DirectoryInfo dir = new DirectoryInfo (sourceDirName);

		//	if (!dir.Exists) {
		//		throw new DirectoryNotFoundException (
		//		    "Source directory does not exist or could not be found: "
		//		    + sourceDirName);
		//	}

		//	DirectoryInfo [] dirs = dir.GetDirectories ();

		//	// If the destination directory doesn't exist, create it.       
		//	Directory.CreateDirectory (destDirName);

		//	// Get the files in the directory and copy them to the new location.
		//	FileInfo [] files = dir.GetFiles ();
		//	foreach (FileInfo file in files) {
		//		string tempPath = Path.Combine (destDirName, file.Name);
		//		file.CopyTo (tempPath, false);
		//	}

		//	// If copying subdirectories, copy them and their contents to new location.
		//	if (copySubDirs) {
		//		foreach (DirectoryInfo subdir in dirs) {
		//			string tempPath = Path.Combine (destDirName, subdir.Name);
		//			DirectoryCopy (subdir.FullName, tempPath, copySubDirs);
		//		}
		//	}
		//}

		static void ReadStream (Stream stream, StringBuilder sb, ManualResetEvent completed)
		{
			var encoding = Encoding.UTF8;
			var decoder = encoding.GetDecoder ();
			var buffer = new byte [1024];
			var characters = new char [encoding.GetMaxCharCount (buffer.Length)];

			AsyncCallback callback = null;
			callback = new AsyncCallback ((IAsyncResult ar) => {
				var read = stream.EndRead (ar);

				var chars = decoder.GetChars (buffer, 0, read, characters, 0);
				lock (sb)
					sb.Append (characters, 0, chars);

				if (read > 0) {
					stream.BeginRead (buffer, 0, buffer.Length, callback, null);
				} else {
					completed.Set ();
				}
			});
			stream.BeginRead (buffer, 0, buffer.Length, callback, null);
		}

		public static int RunCommand (string path, string args, IDictionary<string, string> env = null, StringBuilder output = null, bool verbose = false, string workingDirectory = null)
		{
			var info = new ProcessStartInfo (path, args);
			info.UseShellExecute = false;
			info.RedirectStandardInput = false;
			info.RedirectStandardOutput = true;
			info.RedirectStandardError = true;
			if (workingDirectory != null)
				info.WorkingDirectory = workingDirectory;

			if (output == null)
				output = new StringBuilder ();

			if (env != null) {
				foreach (var kvp in env) {
					if (kvp.Value == null) {
						if (info.EnvironmentVariables.ContainsKey (kvp.Key))
							info.EnvironmentVariables.Remove (kvp.Key);
					} else {
						info.EnvironmentVariables [kvp.Key] = kvp.Value;
					}
				}
			}

			if (info.EnvironmentVariables.ContainsKey ("XCODE_DEVELOPER_DIR_PATH")) {
				// VSfM adds this key, which confuses Xcode mightily if it doesn't match the value of xcode-select.
				// So just remove it, we don't need it for anything.
				info.EnvironmentVariables.Remove ("XCODE_DEVELOPER_DIR_PATH");
			}

			if (verbose)
				Console.WriteLine ("{0} {1}", path, args);

			using (var p = Process.Start (info)) {
				var stdout_completed = new ManualResetEvent (false);
				var stderr_completed = new ManualResetEvent (false);

				ReadStream (p.StandardOutput.BaseStream, output, stdout_completed);
				ReadStream (p.StandardError.BaseStream, output, stderr_completed);

				p.WaitForExit ();

				stderr_completed.WaitOne (TimeSpan.FromMinutes (1));
				stdout_completed.WaitOne (TimeSpan.FromMinutes (1));

				if (verbose) {
					if (output.Length > 0)
						Console.WriteLine (output);
					if (p.ExitCode != 0)
						Console.Error.WriteLine ($"Process exited with code {p.ExitCode}");
				}
				return p.ExitCode;
			}
		}
	}
}
