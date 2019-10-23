// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public func toAny<T> (result: UnsafeMutablePointer<Any>, val: T) {
	result.initialize(to: val)
}

public func fromAny<T> (result: UnsafeMutablePointer<T>, any:Any) {
	result.initialize(to: any as! T)
}
