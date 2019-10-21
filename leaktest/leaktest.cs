using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

class LeakTester
{
	[DllImport ("libc", EntryPoint="isatty")]
	extern static int _isatty (int fd);
	static bool HasControllingTerminal ()
	{
		return _isatty (0) != 0 && _isatty (1) != 0 && _isatty (2) != 0;
	}

	static int Main (string [] args)
	{
		int rv = 0;

		if (args.Length < 1) {
			Console.WriteLine ("A command to execute must be specified");
			return 1;
		}

		// Find libLeakCheckAtExit.dylib next to the leaktest.exe assembly.
		var libLeakCheckAtExit = Path.GetFullPath (Path.Combine (Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location), "libLeakCheckAtExit.dylib"));
		if (!File.Exists (libLeakCheckAtExit)) {
			Console.WriteLine ("Could not find libLeakCheckAtExit.dylib");
			return 1;
		}

		var pid = Process.GetCurrentProcess ().Id;
		var ready_file = Path.GetFullPath ($".stamp-ready-{pid}"); // this file is removed when the test app is ready for leak check
		var done_file = Path.GetFullPath ($".stamp-done-{pid}"); // this file is removed when the leak check is complete, this means the test app can exit

		File.WriteAllText (ready_file, string.Empty);
		File.WriteAllText (done_file, string.Empty);

		Environment.SetEnvironmentVariable ("LEAK_READY_FILE", ready_file);
		Environment.SetEnvironmentVariable ("LEAK_DONE_FILE", done_file);

		Console.OutputEncoding = new UTF8Encoding (false, false);
		using (var p = new Process ()) {
			p.StartInfo.FileName = args [0];
			var sb = new StringBuilder ();
			for (int i = 1; i < args.Length; i++)
				sb.Append (" \"").Append (args [i]).Append ("\"");
			p.StartInfo.Arguments = sb.ToString ();
			p.StartInfo.EnvironmentVariables ["MallocStackLogging"] = "malloc"; // logging everything (MallocStackLogging=1) may cause deadlocks with the GC :(
			p.StartInfo.EnvironmentVariables ["MallocScribble"] = "1";
			p.StartInfo.EnvironmentVariables ["DYLD_INSERT_LIBRARIES"] = libLeakCheckAtExit;

			// If any existing environment variables start with LEAKTEST_, strip that prefix from the name and set the unstripped variable to the same value.
			// Some environment variables like the DYLD_* ones can be automatically removed by macOS, so they need to be wrapped.
			foreach (System.Collections.DictionaryEntry kvp in Environment.GetEnvironmentVariables ()) {
				var key = kvp.Key.ToString ();
				if (!key.StartsWith ("LEAKTEST_", StringComparison.Ordinal))
					continue;
				key = key.Substring ("LEAKTEST_".Length);
				var value = kvp.Value.ToString ();
				p.StartInfo.EnvironmentVariables [key] = value;
			}
			p.StartInfo.UseShellExecute = false;
			Console.WriteLine ("Executing: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
			p.Start ();

			while (File.Exists (ready_file)) {
				Thread.Sleep (100);
				if (p.HasExited) {
					Console.WriteLine ("App crashed/exited, no leak check can be performed.");
					return 1;
				}
			}

			Console.WriteLine ("Performing leak test...");
			using (var leaks = new Process ()) {
				var sudo = !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("JENKINS_HOME")) && !HasControllingTerminal ();
				leaks.StartInfo.FileName = sudo ? "sudo" : "xcrun";
				sb.Clear ();
				if (sudo)
					sb.Append ("--non-interactive xcrun ");
				sb.Append ($"leaks {p.Id}");
				sb.Append (" -quiet");
				// I've filed https://github.com/mono/mono/issues/12404 the mono leaks. Hopefully we'll be able to remove these excludes one day.
				sb.Append (" -exclude create_internal_thread_object");
				sb.Append (" -exclude mono_thread_attach_internal");
				sb.Append (" -exclude mono_thread_set_name_internal");
				sb.Append (" -exclude load_aot_module");
				sb.Append (" -exclude mono_mb_add_local");
				sb.Append (" -exclude get_shared_inst");
				sb.Append (" -exclude mono_marshal_get_runtime_invoke_full");
				sb.Append (" -exclude mono_tramp_info_register_internal");
				sb.Append (" -exclude mono_w32process_init");
				sb.Append (" -exclude mono_arch_exceptions_init");
				sb.Append (" -exclude mono_thread_info_attach");
				sb.Append (" -exclude mini_method_compile");
				sb.Append (" -exclude mono_marshal_get_native_func_wrapper");
				sb.Append (" -exclude mini_get_shared_gparam");
				sb.Append (" -exclude mono_image_load_metadata");
				sb.Append (" -exclude mono_ppdb_load_file");
				sb.Append (" -exclude mono_custom_attrs_construct_by_type");

				sb.Append (" -exclude xamarin_install_nsautoreleasepool_hooks"); // https://github.com/xamarin/xamarin-macios/pull/5495
				sb.Append (" -exclude mono_set_config_dir"); // https://github.com/mono/mono/pull/12647
				sb.Append (" -exclude mono_set_dirs"); // https://github.com/mono/mono/pull/12648

				sb.Append (" -exclude mono_os_event_wait_multiple"); // This is required when using Xcode 9.2's leaks tool. It's not when using Xcode 10.1's leaks tool. Go figure.

				sb.Append (" -exclude emit_managed_wrapper_ilgen"); // This shows up with XM 5.0, but not XM 5.7.
				sb.Append (" -exclude 'app_initialize(xamarin_initialize_data*)'"); // This shows up with XM 5.0, but not XM 5.7.
				sb.Append (" -exclude install_nsautoreleasepool_hooks"); // This shows up with XM 5.0, but not XM 5.7.

				leaks.StartInfo.Arguments = sb.ToString ();
				leaks.StartInfo.UseShellExecute = false;

				Console.WriteLine ("{0} {1}", leaks.StartInfo.FileName, leaks.StartInfo.Arguments);
				leaks.Start ();
				leaks.WaitForExit ();

				Console.WriteLine ("Done performing leak test, result: {0}", leaks.ExitCode);
				if (leaks.ExitCode != 0)
					rv = 2;
			}

			File.Delete (done_file);

			Console.WriteLine ("Waiting for app to terminate...");

			p.WaitForExit ();

			Console.WriteLine ($"Done with exit code {p.ExitCode}");
			if (rv == 0 && p.ExitCode != 0)
				rv = p.ExitCode;
		}
		return rv;
	}
}
