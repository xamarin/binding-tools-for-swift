// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using SwiftReflector.Importing;
using SwiftReflector.TypeMapping;
using tomwiftytest;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class BindingImportTests {
		[TestCase (PlatformName.macOS, 900)]
		[TestCase (PlatformName.iOS, 1000)]
		[TestCase (PlatformName.watchOS, 400)]
		[TestCase (PlatformName.tvOS, 675)]
		public void LoadsDatabaseSmokeTest (PlatformName platform, int expectedLowerLimit)
		{
			var errors = new ErrorHandling ();
			var importer = new BindingImporter (platform, errors);
			var database = importer.Import ();
			Assert.IsNotNull (database, $"null database for {platform}");
			Assert.Less (expectedLowerLimit, database.Count, $"Expected at least {expectedLowerLimit} db entries, but got {database.Count} entries.");
			Assert.IsTrue (!errors.AnyErrors, $"{errors.ErrorCount} errors importing database.");
		}

		[TestCase (PlatformName.iOS)]
		[TestCase (PlatformName.macOS)]
		[TestCase (PlatformName.tvOS)]
		[TestCase (PlatformName.watchOS)]
		public void HasAtLeastNSObject (PlatformName platform)
		{
			var errors = new ErrorHandling ();
			var importer = new BindingImporter (platform, errors);
			var database = importer.Import ();
			var entity = database.EntityForDotNetName (new DotNetName ("Foundation", "NSObject"));
			Assert.IsNotNull (entity, $"Didn't get an NSObject from database on {platform}");
			Assert.IsTrue (entity.IsObjCClass, $"NSObject is not an ObjC class on {platform}. Seriously?");
			Assert.IsNotNull (entity.Type, $"No type in NSObject on {platform}");
			Assert.IsTrue (!errors.AnyErrors, $"{errors.ErrorCount} errors importing database.");
		}

		[Test]
		public void ExcludesEverything ()
		{
			var errors = new ErrorHandling ();
			var importer = new BindingImporter (PlatformName.macOS, errors);
			importer.Excludes.Add (new PatternMatch (".*"));
			var database = importer.Import ();
			Assert.AreEqual (0, database.Count, $"This was supposed to exclude everything, but we got {database.Count} entries.");
			Assert.IsTrue (!errors.AnyErrors, $"{errors.ErrorCount} errors importing database.");
		}


		[Test]
		public void ReincludesNSObject()
		{
			var errors = new ErrorHandling ();
			var importer = new BindingImporter (PlatformName.macOS, errors);
			importer.Excludes.Add (new PatternMatch (".*"));
			importer.Includes.Add (new PatternMatch ("Foundation\\.NSObject"));
			var database = importer.Import ();
			Assert.AreEqual (1, database.Count, $"This was supposed to exclude everything, but we got {database.Count} entries.");
			Assert.IsTrue (!errors.AnyErrors, $"{errors.ErrorCount} errors importing database.");
		}



		[Test]
		public void ReincludesFoundation ()
		{
			var errors = new ErrorHandling ();
			var importer = new BindingImporter (PlatformName.macOS, errors);
			importer.Excludes.Add (new PatternMatch (".*"));
			importer.Includes.Add (new PatternMatch ("Foundation\\..*"));
			var database = importer.Import ();
			Assert.Less (175, database.Count, $"This was supposed to exclude everything, but we got {database.Count} entries.");
			Assert.IsTrue (!errors.AnyErrors, $"{errors.ErrorCount} errors importing database.");
		}


		[Ignore ("Need to check this failure.")]
		[Test]
		[TestCase (PlatformName.iOS)]
		public void LoadsIUIViewControllerTransitionCoordinator (PlatformName platform)
		{
			var errors = new ErrorHandling ();
			var importer = new BindingImporter (platform, errors);
			var database = importer.Import ();
			var entity = database.EntityForDotNetName (new DotNetName ("UIKit", "IUIViewControllerTransitionCoordinator"));
			Assert.IsNotNull (entity, $"Didn't get an IUIViewControllerTransitionCoordinator from database on {platform}");
			Assert.IsTrue (entity.IsObjCProtocol, $"NSObject is not an ObjC protocol on {platform}. Seriously?");
			Assert.IsNotNull (entity.Type, $"No type in IUIViewControllerTransitionCoordinator on {platform}");
			Assert.IsTrue (!errors.AnyErrors, $"{errors.ErrorCount} errors importing database.");

		}
	}
}
