// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace TomTest
{
	public interface ITomTest
	{
		void Run ();
		string TestName { get; }
		string ExpectedOutput { get; }
	}

	public class TomSkipAttribute : Attribute
	{
		public TomSkipAttribute (string reason)
		{
			Reason = reason;
		}
		public string Reason { get; private set; }
	}
}
