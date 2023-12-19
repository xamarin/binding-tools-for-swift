// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.IO;
using System.Diagnostics;
using Xamarin;
using System.Linq;
using System.Collections.Generic;
using SwiftRuntimeLibrary;
using SwiftRuntimeLibrary.SwiftMarshal;
using NUnit.Framework.Legacy;

namespace SwiftRuntimeLibraryTests {
	[TestFixture]
	[Parallelizable (ParallelScope.All)]
	public class SwiftObjectRegistryTests {

		// Can't do this test - it will cause a Retain() to get called on the sham IntPtr and that's super bad.
		//		[Test]
		//		public void CanRegisterClass()
		//		{
		//			IntPtr sham = new IntPtr (1);
		//			AnonymousSwiftObject anon = AnonymousSwiftObject.XamarinFactory (sham);
		//
		//			AnonymousSwiftObject registered = SwiftObjectRegistry.Registry.CSObjectForSwiftObject (sham);
		//			ClassicAssert.AreEqual (anon, registered);
		//		}

		class GoodISwiftObject : ISwiftObject {
			public GoodISwiftObject (IntPtr p)
			{
				SwiftObject = p;
				SwiftObjectRegistry.Registry.Add (this);
			}
			#region IDisposable implementation
			bool disposed = false;
			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}
			~GoodISwiftObject ()
			{
				Dispose (false);
			}
			protected virtual void Dispose (bool disposing)
			{
				if (!disposed) {
					if (disposing) {
						//						SwiftObjectRegistry.Registry.Remove (this);
					}
					disposed = true;
				}
			}
			#endregion
			#region ISwiftObject implementation
			public IntPtr SwiftObject { get; set; }
			#endregion
			public static GoodISwiftObject XamarinFactory (IntPtr p)
			{
				return new GoodISwiftObject (p);
			}
		}

		class BadISwiftObject : ISwiftObject {
			public BadISwiftObject (IntPtr p)
			{
				SwiftObject = p;
				SwiftObjectRegistry.Registry.Add (this);
			}
			#region IDisposable implementation
			bool disposed = false;
			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}
			~BadISwiftObject ()
			{
				Dispose (false);
			}
			protected virtual void Dispose (bool disposing)
			{
				if (!disposed) {
					if (disposing) {
						//						SwiftObjectRegistry.Registry.Remove (this);
					}
					disposed = true;
				}
			}
			#endregion
			#region ISwiftObject implementation
			public IntPtr SwiftObject { get; set; }
			#endregion
			public static BadISwiftObject NoXamarinFactory (IntPtr p)
			{
				return new BadISwiftObject (p);
			}
		}

		[Test]
		public void BadClassFailRegister ()
		{
			// has no class factory
			IntPtr sham = new IntPtr (42);
			ClassicAssert.Throws<SwiftRuntimeException> (() => {
				try {
					ClassicAssert.IsFalse (SwiftObjectRegistry.Registry.Contains (sham));
					using (SwiftObjectRegistry.Registry.CSObjectForSwiftObject<BadISwiftObject> (sham)) {

					}
				} finally {
					ClassicAssert.IsFalse (SwiftObjectRegistry.Registry.Contains (sham));
				}
			});
		}

	}
}

