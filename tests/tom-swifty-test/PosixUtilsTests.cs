// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;

using NUnit.Framework;
using NUnit.Framework.Legacy;
using SwiftReflector.IOUtils;


namespace tomwiftytest {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class PosixUtilsTests {
		[Test]
		public void DotWorks ()
		{
			var finalPath = PosixHelpers.RealPath ("/foo/././././bar");
			ClassicAssert.AreEqual ("/foo/bar", finalPath);
		}


		[Test]
		public void DotDotWorks ()
		{
			var finalPath = PosixHelpers.RealPath ("/foo/bar/baz/../goo");
			ClassicAssert.AreEqual ("/foo/bar/goo", finalPath);
		}

		[Test]
		public void DotDotWorks1 ()
		{
			var finalPath = PosixHelpers.RealPath ("/foo/bar/baz/bing/goo/../../doo");
			ClassicAssert.AreEqual ("/foo/bar/baz/doo", finalPath);
		}

		[Test]
		public void RelativePathsInSymlinks1 ()
		{
			using (var dir = new DisposableTempDirectory ()) {
				var sourcePath = PathWithoutLeadingSlash (Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (dir.DirectoryPath))));
				var expectedPath = Path.Combine ("/private", sourcePath);

				var link1 = Path.Combine (dir.DirectoryPath, "link1");
				symlink ("../../..", link1);
				var finalPath = PosixHelpers.RealPath (link1);

				ClassicAssert.AreEqual (expectedPath, finalPath, "1");
			}
		}

		[Test]
		public void RelativePathsInSymlinks2 ()
		{
			using (var dir = new DisposableTempDirectory ()) {
				var sourcePath = PathWithoutLeadingSlash (Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (dir.DirectoryPath))));
				var expectedPath = Path.Combine ("/private", sourcePath);

				var link2 = Path.Combine (dir.DirectoryPath, "link2");
				symlink (Path.Combine (dir.DirectoryPath, "..", "..", ".."), link2);
				var finalPath = PosixHelpers.RealPath (link2);
				ClassicAssert.AreEqual (expectedPath, finalPath, "2");
			}
		}

		// Mono.Unix can't create symlinks with relative paths, so P/Invoke instead.
		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern int symlink (string path1, string path2);

		static string PathWithoutLeadingSlash (string path)
		{
			return path.StartsWith ('/') ? path.Substring (1) : path;
		}
	}
}
