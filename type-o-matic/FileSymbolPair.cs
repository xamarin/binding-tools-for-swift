// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
namespace typeomatic {
	public class FileSymbolPair {
		public FileSymbolPair () { }
		public FileSymbolPair (string file, string symbol)
		{
			File = file;
			Symbol = symbol;
		}
		public string File { get; set; }
		public string Symbol { get; set; }
	}
}
