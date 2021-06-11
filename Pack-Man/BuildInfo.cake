/*
 * BuildInfo.cake
 *    Holds the required directories information
 *    to package the final Binding Tools for Swift bundle.
 *
 * Authors
 *    Alex Soto <alexsoto@microsoft.com>
 */

public class BuildInfo {
	public BuildInfo (ICakeContext ctx, string configuration, string outputDir, string target)
	{
		DestBaseDir = ctx.MakeAbsolute (ctx.Directory (outputDir));

		BaseDir = ctx.MakeAbsolute (ctx.Directory ("../"));
		SwiftRuntimeiOS = BaseDir.Combine ($"SwiftRuntimeLibrary.iOS/bin/{configuration}");
		SwiftRuntimeMac = BaseDir.Combine ($"SwiftRuntimeLibrary.Mac/bin/{configuration}");
		TomSwifty = BaseDir.Combine ($"tom-swifty/bin/{configuration}");
		SwiftCopyLibs = BaseDir.Combine ($"swift-copy-libs/bin/{configuration}");
		XamGlue = BaseDir.Combine ($"swiftglue/bin/{configuration}");
		Bindings = BaseDir.Combine ("bindings");
		PlistSwifty = BaseDir.Combine ($"plist-swifty/bin/{configuration}");
		MakeFramework = BaseDir.Combine ("tools");
	}
	public DirectoryPath DestBaseDir { get; private set; }
	public DirectoryPath BaseDir { get; private set; }
	public DirectoryPath TomSwifty { get; private set; }
	public DirectoryPath SwiftCopyLibs { get; private set; }
	public DirectoryPath SwiftRuntimeiOS { get; private set; }
	public DirectoryPath SwiftRuntimeMac { get; private set; }
	public DirectoryPath XamGlue { get; private set; }
	public DirectoryPath Bindings { get; private set; }
	public DirectoryPath PlistSwifty { get; private set; }
	public DirectoryPath MakeFramework { get; private set; }

	public void EnsurePathsExistance (ICakeContext ctx)//, bool swiftToolchainOnly = false)
	{
		if (!ctx.DirectoryExists (TomSwifty))
			Die (TomSwifty);
		if (!ctx.DirectoryExists (SwiftCopyLibs))
			Die (SwiftCopyLibs);
		if (!ctx.DirectoryExists (XamGlue))
			Die (XamGlue);
		if (!ctx.DirectoryExists (SwiftRuntimeiOS))
			Die (SwiftRuntimeiOS);
		if (!ctx.DirectoryExists (SwiftRuntimeMac))
			Die (SwiftRuntimeMac);
		if (!ctx.DirectoryExists (Bindings))
			Die (Bindings);
		if (!ctx.DirectoryExists (PlistSwifty))
			Die (PlistSwifty);
		if (!ctx.DirectoryExists (MakeFramework))
			Die (MakeFramework);
		
		void Die (DirectoryPath path) {
			throw new InvalidOperationException ($"Required directory does not exist: '{path}'.");
		}
	}
}
