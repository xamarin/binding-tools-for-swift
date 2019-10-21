// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Foreach;


public class CSForeach {
	public static void Main(string[] Args) {
		Looper looper = new Looper();
		looper.Foreach(x => Console.WriteLine($"Here's an element: {x}"));
		looper.Foreachi((i, x) => Console.WriteLine($"Element at {i} is {x}"));
	}
}
