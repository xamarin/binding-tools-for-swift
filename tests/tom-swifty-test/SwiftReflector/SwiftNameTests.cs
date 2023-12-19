// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using tomwiftytest;
using NUnit.Framework.Legacy;

namespace SwiftReflector {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	[RunWithLeaks]
	public class SwiftNameTests {

		[Test]
		public void SwiftNameEquals ()
		{
			SwiftName sn = new SwiftName ("Bob", false);
			SwiftName sn1 = new SwiftName ("Bob", false);
			ClassicAssert.AreEqual (sn, sn1);
		}

		[Test]
		public void SwiftNameNotEquals ()
		{
			SwiftName sn = new SwiftName ("Bob", false);
			SwiftName sn1 = new SwiftName ("Bob1", false);
			ClassicAssert.AreNotEqual (sn, sn1);
		}

		[Test]
		public void SwiftNamePunyEquals ()
		{
			SwiftName sn = new SwiftName ("GrIh", true);
			SwiftName sn1 = new SwiftName ("GrIh", true);
			ClassicAssert.AreEqual (sn, sn1);
		}

		[Test]
		public void SwiftNamePunyNotEquals ()
		{
			SwiftName sn = new SwiftName ("GrIh", true);
			SwiftName sn1 = new SwiftName ("Bob1", false);
			ClassicAssert.AreNotEqual (sn, sn1);
		}
	}
}

