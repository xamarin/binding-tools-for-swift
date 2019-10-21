// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public func optionalNewNone<T>(retval: UnsafeMutablePointer<T?>)
{
	retval.pointee = nil
}

public func optionalNewSome<T>(retval: UnsafeMutablePointer<T?>, val: UnsafePointer<T>)
{
	retval.pointee = val.pointee
}

public func optionalHasValue<T>(val: UnsafeMutablePointer<T?>) -> Bool
{
	return val.pointee != nil
}

public func optionalCase<T>(val: UnsafeMutablePointer<T?>) -> Int
{
	return val.pointee == nil ? 0 : 1
}

public func optionalValue<T>(optval: UnsafeMutablePointer<T?>, val: UnsafeMutablePointer<T>)
{
	if optval.pointee != nil {
		val.pointee = optval.pointee!
	}
}
