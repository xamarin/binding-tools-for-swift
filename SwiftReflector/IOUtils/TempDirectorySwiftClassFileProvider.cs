// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace SwiftReflector.IOUtils {
	public class TempDirectorySwiftClassFileProvider : TempDirectoryStreamProvider<SwiftClassName> {
		public TempDirectorySwiftClassFileProvider (string directoryName, bool prependGuid)
			: base (directoryName, true)
		{
		}

		#region implemented abstract members of TempDirectoryStreamProvider

		protected override string FromThing (SwiftClassName thing)
		{
			return String.Format ("{0}.swift", thing.ToFullyQualifiedName (true).Replace ('.', '-'));
		}

		#endregion
	}
}

