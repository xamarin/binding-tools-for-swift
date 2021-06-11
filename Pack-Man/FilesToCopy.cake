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
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("lib/plist-swifty"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("lib/make-framework"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("samples"));
	EnsureDirectoryExists (bi.DestBaseDir.Combine ("bindings"));
}

IEnumerable<(FilePath Src, FilePath Dest)> GetFilesToBundle (BuildInfo bi, bool swiftToolchainOnly = false)
{
	var fl = new List<(FilePath Src, FilePath Dest)> ();

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

	var sampleDocPattern = $"{bi.BaseDir}/samples/*.md";
	foreach (var sampleDoc in GetFiles (sampleDocPattern)) {
		var destDoc = bi.DestBaseDir.CombineWithFilePath ($"samples/{sampleDoc.GetFilename ()}");
		fl.Add ((sampleDoc, destDoc));
	}
	var fwkFile = GetFiles ($"{bi.MakeFramework.FullPath}/make-framework").FirstOrDefault ();
	var destFwkFile = bi.DestBaseDir.CombineWithFilePath ($"lib/make-framework/{fwkFile.GetFilename ()}");
	fl.Add ((fwkFile, destFwkFile));

	var plistPattern = $"{bi.PlistSwifty.FullPath}/*.*";
	var plistFiles = GetFiles (plistPattern).Where (f => isValidExt (f.GetExtension ()));
	foreach (var plistFile in plistFiles) {
		var dest = bi.DestBaseDir.CombineWithFilePath ($"lib/plist-swifty/{plistFile.GetFilename ()}");
		fl.Add ((plistFile, dest));
	} 

	return fl;

	(FilePath, FilePath) GetInfo (DirectoryPath srcBaseDir, string s, string d) {
		return (srcBaseDir.CombineWithFilePath (s), bi.DestBaseDir.CombineWithFilePath (d));
	}
}

IEnumerable<(DirectoryPath Src, DirectoryPath Dest)> GetDirectoriesToBundle (BuildInfo bi, bool swiftToolchainOnly = false)
{
	var dl = new List<(DirectoryPath Src, DirectoryPath Dest)> ();

	var swiftFolders = new [] {
		"appletvos", "appletvsimulator", "clang", "iphoneos", "iphonesimulator",
		"macosx", "migrator", "shims", "watchos", "watchsimulator",
	};

	if (swiftToolchainOnly)
		return dl;

	// Get 'XamGlue' folders to copy
	var glues = new [] { "appletv", "iphone", "mac", "watch" };
	foreach (var glue in glues)
		dl.Add (GetInfo (bi.XamGlue, $"{glue}/FinalProduct/XamGlue.framework", $"lib/SwiftInterop/{glue}/XamGlue.framework"));

	// Get 'Sample' folders to copy
	var samples = new [] { "helloswift", "piglatin", "propertybag", "sampler", "sandwiches" };
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
