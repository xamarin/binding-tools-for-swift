/*
 * FilesToCopy.cake
 *    List of files and directories from our custom swift compiler
 *    and others needed to be copied over the final Binding Tools for Swift bundle.
 *
 * Authors
 *    Alex Soto <alexsoto@microsoft.com>
 */
#load BuildInfo.cake

void CreateFolderStructure (BuildInfo bi, bool swiftToolchainOnly = false)
{
	EnsureDirectoryExists (bi.DestBaseDir);
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("bin"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("lib"));

	if (swiftToolchainOnly)
		return;

	EnsureDirectoryExists (bi.DestBaseDir.Combine ("bin/swift"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("bin/swift/bin"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("lib/binding-tools-for-swift"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("lib/swift-copy-libs"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("lib/SwiftInterop"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("samples"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("bindings"));
}

IEnumerable<(FilePath Src, FilePath Dest)> GetFilesToBundle (BuildInfo bi, bool swiftToolchainOnly = false)
{
	var fl = new List<(FilePath Src, FilePath Dest)> ();

	// Get custom compiler 'bin' files
	var swiftDestBin = swiftToolchainOnly ? "bin" : "bin/swift/bin";
	fl.Add (GetInfo (bi.SwiftBaseDir, "bin/swift", $"{swiftDestBin}/swift"));
	fl.Add (GetInfo (bi.SwiftBaseDir, "bin/swift-demangle", $"{swiftDestBin}/swift-demangle"));
	fl.Add (GetInfo (bi.SwiftBaseDir, "bin/swift-stdlib-tool", $"{swiftDestBin}/swift-stdlib-tool"));

	// Get custom compiler 'lib' files
	var swiftDestLib = swiftToolchainOnly ? "lib" : "bin/swift/lib";
	fl.Add (GetInfo (bi.SwiftBaseDir, "lib/libswiftDemangle.dylib", $"{swiftDestLib}/libswiftDemangle.dylib"));

	if (swiftToolchainOnly)
		return fl;

	fl.Add (GetInfo (bi.BaseDir, "binding-tools-for-swift", "binding-tools-for-swift"));

	// Get 'binding-tools-for-swift' files
	Func<string, bool> isValidExt = e => e == ".dll" || e == ".pdb" || e == ".exe";
	var btfsPattern = $"{bi.TomSwifty.FullPath}/*.*";
	var btfsFiles = GetFiles (btfsPattern).Where (f => isValidExt (f.GetExtension ()));
	foreach (var btfsFile in btfsFiles) {
		var dest = bi.DestBaseDir.CombineWithFilePath ($"lib/binding-tools-for-swift/{btfsFile.GetFilename ()}");
		fl.Add ((btfsFile, dest));
	}

	// Get 'swift-copy-libs' files
	btfsPattern = $"{bi.SwiftCopyLibs.FullPath}/*.*";
	btfsFiles = GetFiles (btfsPattern).Where (f => isValidExt (f.GetExtension ()));
	foreach (var btfsFile in btfsFiles) {
		var dest = bi.DestBaseDir.CombineWithFilePath ($"lib/swift-copy-libs/{btfsFile.GetFilename ()}");
		fl.Add ((btfsFile, dest));
	}
	fl.Add (GetInfo (bi.SwiftCopyLibs, "../../swift-copy-libs-packaged", $"bin/swift-copy-libs"));

	// Get iOS/macOS 'SwiftRuntimeLibrary' files.
	var iosRuntimePatern = $"{bi.SwiftRuntimeiOS.FullPath}/*.*";
	var macRuntimePatern = $"{bi.SwiftRuntimeMac.FullPath}/*.*";
	var iosRuntimeFiles = GetFiles (iosRuntimePatern).Where (f => isValidExt (f.GetExtension ()));
	var macRuntimeFiles = GetFiles (macRuntimePatern).Where (f => isValidExt (f.GetExtension ()));
	foreach (var srcFile in iosRuntimeFiles.Union (macRuntimeFiles)) {
		var dest = bi.DestBaseDir.CombineWithFilePath ($"lib/SwiftInterop/{srcFile.GetFilename ()}");
		fl.Add ((srcFile, dest));
	}

	return fl;

	(FilePath, FilePath) GetInfo (DirectoryPath srcBaseDir, string s, string d) {
		return (srcBaseDir.CombineWithFilePath (s), bi.DestBaseDir.CombineWithFilePath (d));
	}
}

IEnumerable<(DirectoryPath Src, DirectoryPath Dest)> GetDirectoriesToBundle (BuildInfo bi, bool swiftToolchainOnly = false)
{
	var dl = new List<(DirectoryPath Src, DirectoryPath Dest)> ();

	// Get custom compiler 'lib' folders to copy
	var swiftDestLib = swiftToolchainOnly ? "lib" : "bin/swift/lib";
	dl.Add (GetInfo (bi.SwiftBaseDir, "lib/sourcekitd.framework", $"{swiftDestLib}/sourcekitd.framework"));

	var swiftFolders = new [] {
		"appletvos", "appletvsimulator", "clang", "iphoneos", "iphonesimulator",
		"macosx", "migrator", "shims", "watchos", "watchsimulator",
	};
	var innerSwiftDestLib = $"{swiftDestLib}/swift";
	foreach (var swiftFolder in swiftFolders)
		 dl.Add (GetInfo (bi.SwiftBaseDir, $"lib/swift/{swiftFolder}", $"{innerSwiftDestLib}/{swiftFolder}"));

	if (swiftToolchainOnly)
		return dl;

	// Get 'XamGlue' folders to copy
	var glues = new [] { "appletv", "iphone", "mac", "watch" };
	foreach (var glue in glues)
		dl.Add (GetInfo (bi.XamGlue, $"{glue}/FinalProduct/XamGlue.framework", $"lib/SwiftInterop/{glue}/XamGlue.framework"));

	// Get 'Sample' folders to copy
	var samples = new [] { "helloswift", "foreach", "piglatin", "propertybag", "sampler" };
	foreach (var sample in samples)
		dl.Add (GetInfo (bi.BaseDir, $"samples/{sample}", $"samples/{sample}"));

	// Get 'Bindings' folder to copy.
	dl.Add (GetInfo (bi.BaseDir, "bindings", "bindings"));

	return dl;

	(DirectoryPath, DirectoryPath) GetInfo (DirectoryPath srcBaseDir, string s, string d) {
		return (srcBaseDir.Combine (s), bi.DestBaseDir.Combine (d));
	}
}

void EnsureDirectoryAndFilesToCopyExist (IEnumerable<(DirectoryPath Src, DirectoryPath Dest)> directories, IEnumerable<(FilePath Src, FilePath Dest)> files)
{
	foreach (var dir in directories) {
		if (!DirectoryExists (dir.Src))
			throw new InvalidOperationException ($"Required directory does not exist: '{dir.Src}'.");
	}

	foreach (var file in files) {
		if (!FileExists (file.Src))
			throw new InvalidOperationException ($"Required file does not exist: '{file.Src}'.");
	}
}

void CreateSwiftcSymlink (BuildInfo bi, bool swiftToolchainOnly = false)
{
	var settings = new ProcessSettings {
		WorkingDirectory = swiftToolchainOnly ?  bi.DestBaseDir.Combine ("bin") : bi.DestBaseDir.Combine ("bin/swift/bin"),
		Arguments = @"-s ""./swift"" ""swiftc"""
	};

	Information ("Creating 'swiftc' symlink...");
	using (var process = StartAndReturnProcess ("ln", settings)) {
		process.WaitForExit ();
		if (process.GetExitCode () == 0)
			Information ("'swiftc' symlink created!");
		else
			throw new Exception ($"An error ocurred creating 'swiftc' symlink: Exit code {process.GetExitCode ()}");
	}
}

void ZipEverything (BuildInfo bi, string dirName = null)
{
	var filename = dirName ?? bi.DestBaseDir.GetDirectoryName ();
	 var settings = new ProcessSettings {
		Arguments = $@"-qr {filename}.zip {filename} -x ""*.DS_Store"" -x ""__MACOSX"" --symlinks"
	};

	Information ($"Creating '{filename}.zip'...");
	using (var process = StartAndReturnProcess ("zip", settings)) {
		process.WaitForExit ();
		if (process.GetExitCode () == 0)
			Information ($"'{filename}.zip' created!");
		else
			throw new Exception ($"An error ocurred creating '{filename}.zip': Exit code {process.GetExitCode ()}");
	}
}
