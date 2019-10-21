/*
 * build.cake
 *    Entry point of our build script to create the
 *    final Binding Tools for Swift bundle.
 *
 * Authors
 *    Alex Soto <alexsoto@microsoft.com>
 */

#load BuildInfo.cake
#load FilesToCopy.cake

// Fetch Arguments
var target = Argument ("target", "CreatePackage");
var configuration = Argument ("configuration", "Debug"); // Debug or Release
var outputDir = Argument ("output-directory", "binding-tools-for-swift");
var showHelp = HasArgument ("h");
BuildInfo buildInfo;

// Executed before the first task.
Setup (ctx => {
	if (!DirectoryExists (DirectoryPath.FromString ("../apple")))
		throw new InvalidOperationException ("You must setup 'apple' directory first.");
	
	if (configuration == "Debug" || configuration == "Release") {
		buildInfo = new BuildInfo (ctx, configuration, outputDir, target);
		buildInfo.EnsurePathsExistance (ctx, swiftToolchainOnly: target == "SwiftToolchain");
	} else
		throw new InvalidOperationException ($"Invalid 'configuration' parameter supplied: '{configuration}'.");
});

// Tasks
Task ("CreatePackage")
.IsDependentOn ("Clean")
.Does(() => {
	if (showHelp) {
		ShowUsage ();
	} else {
		Information ($"Creating folder structure in {outputDir}...");
		CreateFolderStructure (buildInfo);
		var directories = GetDirectoriesToBundle (buildInfo);
		var files = GetFilesToBundle (buildInfo);
		EnsureDirectoryAndFilesToCopyExist (directories, files);

		Information ($"Copying directories to {outputDir}...");
		foreach (var directory in directories)
			CopyDirectory (directory.Src, directory.Dest);

		Information ($"Copying files to {outputDir}...");
		foreach (var file in files)
			CopyFile (file.Src, file.Dest);

		CreateSwiftcSymlink (buildInfo);

		// Using a custom zip method (instead of 'Zip ()') so we can preserve symlinks
		ZipEverything (buildInfo);
	}
});

Task ("SwiftToolchain")
.Does (() => {
	Information ($"Copying Swift Toolchain to {outputDir}...");
	CreateFolderStructure (buildInfo, swiftToolchainOnly: true);
	var directories = GetDirectoriesToBundle (buildInfo, swiftToolchainOnly: true);
	var files = GetFilesToBundle (buildInfo, swiftToolchainOnly: true);
	EnsureDirectoryAndFilesToCopyExist (directories, files);

	Information ($"Copying directories to {outputDir}...");
	foreach (var directory in directories)
		CopyDirectory (directory.Src, directory.Dest);

	Information ($"Copying files to {outputDir}...");
	foreach (var file in files)
		CopyFile (file.Src, file.Dest);

	CreateSwiftcSymlink (buildInfo, swiftToolchainOnly: true);

	// Using a custom zip method (instead of 'Zip ()') so we can preserve symlinks
	ZipEverything (buildInfo, outputDir);
});

Task ("Clean")
.Does (() => {
	var zipFile = $"./{outputDir}.zip";
	if (DirectoryExists (outputDir))
		DeleteDirectory (outputDir, new DeleteDirectorySettings { Recursive = true, Force = true });
	if (FileExists (zipFile))
		DeleteFile (zipFile);
});

void ShowUsage ()
{
	Information ("Usage:");
	Information ("======\n");
	Information ("-h \n\t Shows help.\n");
	Information ("--configuration=VALUE \n\t Value can be 'Debug' or 'Release', defaults to 'Debug' if not supplied.\n");
	Information ("--output-directory=VALUE \n\t Output directory name, defaults to 'binding-tools-for-swift' if not supplied.\n");
	Information ("--target=Clean \n\t Removes the packager output directory and zip file.\n");
	Information ("--target=CreatePackage \n\t 'CreatePackage' is the default target that creates the SoM package.\n");
	Information ("--target=SwiftToolchain \n\t Creates the Swift toolchain package.\n");
}

RunTarget (target);
