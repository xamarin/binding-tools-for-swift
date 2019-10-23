// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Options;

using Xamarin;

class SwiftLibraryCopier {
	enum Action {
		None,
		Print,
		Copy,
	}
	// This is tool very similar to Apple's swift-stdlib-tool, which scans an .app and copies the required swift libraries to the app.
	// Unfortunately Apple's tool has a shortcoming: it scans only executables, not shared libraries / frameworks.
	// So this tool fixes that shortcoming: it scans all both executables and shared libraries / frameworks.
	// As opposed to swift-stdlib-tool, code signing is not supported, since we don't need it (for now at least).
	// swift-stdlib-tool also doesn't update the timestamp of the files it copies, which makes dependency tracking annoying in the MSBuild target files.
	public static int Main (string [] args)
	{
		const string TOOL = "swift-copy-libs";
		OptionSet os = null;
		int? exit_code = null;
		var action = Action.None;
		var scan_executables = new List<string> ();
		var scan_folders = new List<string> ();
		var platform = string.Empty;
		var source_libraries = string.Empty;
		var destination = string.Empty;
		var resource_libraries = new List<string> ();
		var verbosity = 0;
		var failed = false;

		os = new OptionSet {
			{ "h|help|?", "Show help", (v) =>
				{
					Console.WriteLine (TOOL + " [OPTIONS]");
					os.WriteOptionDescriptions (Console.Out);
					exit_code = 0;
				}
			},
			{ "copy", "Copy required swift libraries to the target location", (v) => action = Action.Copy },
			{ "print", "Print required swift libraries", (v) => action = Action.Print },
			{ "scan-executable=", "Scan the specified executable", (v) => scan_executables.Add (v) },
			{ "scan-folder=", "Scan the specified folder", (v) => scan_folders.Add (v) },
			{ "platform=", "The platform to use", (v) => platform = v },
			{ "source-libraries=", "The path where to find the swift libraries", (v) => source_libraries = v },
			{ "destination=", "The destination path for the swift libraries", (v) => destination = v },
			{ "resource-library=", "Additional swift libraries.", (v) => resource_libraries.Add (v) },
			{ "v|verbose", "Show verbose output", (v) => { verbosity++; } },
			{ "q|quiet", "Show less verbose output", (v) => { verbosity--; } },
			{ "strip-bitcode", "Strip bitcode", (v) => {} },
		};

		var left = os.Parse (args);

		if (exit_code.HasValue)
			return exit_code.Value;

		if (left.Count > 0) {
			Console.Error.WriteLine ($"{TOOL}: unexpected arguments: {left.First ()}");
			return 1;
		}

		switch (action) {
		case Action.Copy:
		case Action.Print:
			break;
		default:
			Console.Error.WriteLine ($"{TOOL}: no action specified: pass either --copy or --print.");
			return 1;
		}

		var files = new HashSet<string> ();
		files.UnionWith (scan_executables);
		foreach (var folder in scan_folders) {
			if (!Directory.Exists (folder)) {
				Console.WriteLine ($"Could not find the folder {folder}.");
				continue;
			}
			files.UnionWith (Directory.EnumerateFileSystemEntries (folder, "*", SearchOption.AllDirectories));
		}

		// Remove directories from the list.
		files.RemoveWhere (Directory.Exists);

		var process_queue = new Queue<string> (files);
		var processed = new HashSet<string> ();
		var dependencies = new HashSet<string> ();
		while (process_queue.Count > 0) {
			var file = process_queue.Dequeue ();
			if (processed.Contains (file))
				continue;
			processed.Add (file);
			if (!File.Exists (file)) {
				Console.WriteLine ($"Could not find the file {file}.");
				failed = true;
				continue;
			}
			try {
				var macho_file = MachO.Read (file, ReadingMode.Deferred);
				foreach (var slice in macho_file) {
					using (slice) {
						Console.WriteLine ($"{file} [{slice.Architecture}]:");
						foreach (var lc in slice.load_commands) {
							var cmd = (MachO.LoadCommands)lc.cmd;
							if (cmd != MachO.LoadCommands.LoadDylib)
								continue;
							var ldcmd = (DylibLoadCommand)lc;
							if (!ldcmd.name.StartsWith ("@rpath/libswift", StringComparison.Ordinal))
								continue;
							var dependency = ldcmd.name.Substring ("@rpath/".Length);
							dependency = Path.Combine (source_libraries, platform, dependency);

							if (verbosity > 0)
								Console.WriteLine ($"    {Path.GetFileName (dependency)}");
							if (!File.Exists (dependency)) {
								if (verbosity > 0)
									Console.WriteLine ($"        Could not find the file {dependency}");
							} else {
								dependencies.Add (dependency);
								process_queue.Enqueue (dependency);
							}
						}
					}
				}
			} catch (Exception e) {
				if (verbosity > 0)
					Console.WriteLine ($"{file}: not a Mach-O file ({e.Message})");
			}
		}

		var allDependencies = dependencies.Union (resource_libraries.Select ((v) => Path.Combine (source_libraries, platform, v))).ToList ();
		if (verbosity > 0) {
			Console.WriteLine ($"Found {allDependencies.Count} dependencies:");
			foreach (var dependency in allDependencies) {
				Console.WriteLine ($"    {dependency}");
			}
		}

		if (action == Action.Copy) {
			foreach (var dependency in allDependencies) {
				var src = dependency;
				var tgt = Path.Combine (destination, Path.GetFileName (dependency));
				var srcInfo = new FileInfo (src);
				var tgtInfo = new FileInfo (tgt);
				if (!tgtInfo.Exists || srcInfo.Length != tgtInfo.Length || srcInfo.LastWriteTimeUtc > tgtInfo.LastWriteTimeUtc) {
					File.Copy (src, tgt, true);
				} else {
					if (verbosity > 0)
						Console.WriteLine ($"Did not copy {Path.GetFileName (src)} because it's up-to-date.");
				}

				new FileInfo (tgt).LastWriteTimeUtc = DateTime.Now;
			}
			if (verbosity >= 0)
				Console.WriteLine ($"Copied all dependencies ({string.Join (", ", allDependencies.Select (Path.GetFileName))}) to {destination}.");
		}

		return failed ? 1 : 0;
	}
}
