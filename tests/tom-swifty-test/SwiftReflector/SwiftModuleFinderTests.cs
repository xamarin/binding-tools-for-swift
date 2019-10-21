using System;
using System.IO;
using SwiftReflector.IOUtils;
using NUnit;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using tomwiftytest;

namespace SwiftReflector
{
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class SwiftModuleFinderTests
	{
		void CreateFileAtLocation (params string [] paths)
		{
			string location = Path.Combine (paths);
			string path = Path.GetDirectoryName (location);
			Directory.CreateDirectory (path);
			File.Create (location);
		}
		
		[Test]
		public void GetAppleModuleName_OnlyWorksWithValidStructure ()
		{
			using (var tmp = new DisposableTempDirectory ()) {
				string framework = Path.Combine (tmp.DirectoryPath, "Foo.framework");
				Directory.CreateDirectory (Path.Combine (framework, "Modules"));
				Assert.IsNull (SwiftModuleFinder.GetAppleModuleName (framework));

				File.Create (Path.Combine (framework, "Modules", "Foo.swiftmodule"));
				Assert.AreEqual ("Foo", SwiftModuleFinder.GetAppleModuleName (framework));
			}
		}

		[Test]
		public void GetAppleModuleName_RequiresValidName ()
		{
			using (var tmp = new DisposableTempDirectory ()) {
				CreateFileAtLocation (tmp.DirectoryPath, "Foo", "Modules", "Foo.swiftmodule");
				Assert.IsNull (SwiftModuleFinder.GetAppleModuleName (Path.Combine (tmp.DirectoryPath, "Foo")));

				CreateFileAtLocation (tmp.DirectoryPath, ".framework", "Modules", ".swiftmodule");
				Assert.IsNull (SwiftModuleFinder.GetAppleModuleName (Path.Combine (tmp.DirectoryPath, ".framework")));
			}
		}

		[Test]
		public void GetXamarinModuleName_OnlyWorksWithValidStructure ()
		{
			const string Arch = "x86_64";

			using (var tmp = new DisposableTempDirectory ()) {
				string folder = Path.Combine (tmp.DirectoryPath, "Foo");
				Directory.CreateDirectory (Path.Combine (folder, Arch));
				Assert.IsNull (SwiftModuleFinder.GetXamarinModuleName (folder, Arch));

				CreateFileAtLocation (folder, "x86_64", "Foo.swiftmodule");
				Assert.AreEqual ("Foo", SwiftModuleFinder.GetXamarinModuleName (folder, Arch));
			}
		}

		[Test]
		public void GetDirectLayoutModuleName ()
		{	
			using (var tmp = new DisposableTempDirectory ()) {
				CreateFileAtLocation (tmp.DirectoryPath, "Foo");
				Assert.IsNull (SwiftModuleFinder.GetDirectLayoutModuleName (Path.Combine (tmp.DirectoryPath, "Foo")));

				CreateFileAtLocation (tmp.DirectoryPath, "Bar.swiftmodule");
				Assert.AreEqual ("Bar", SwiftModuleFinder.GetDirectLayoutModuleName (Path.Combine (tmp.DirectoryPath, "Bar.swiftmodule")));
			}
		}
	}
}
