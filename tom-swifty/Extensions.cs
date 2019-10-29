// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;

namespace tomswifty {
	public static class Extensions {
		public static T [] Prepend<T> (this T [] arr, params T [] items)
		{
			return items.Concat(arr).ToArray();
		}
	}
}
