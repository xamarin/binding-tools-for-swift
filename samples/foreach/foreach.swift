// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public class Looper {
	private var arr:[Int] = [1, 1, 2, 3, 5, 8, 13, 21, 34, 45]
	public init() { }

	public func foreach(f:(Int)->()) {
		for i in arr {
			f(i)
		}
	}
	public func foreachi(f:(Int, Int)->()) {
		for (index, value) in arr.enumerated() {
			f(index, value)
		}
	}
}
