// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Swift;

public func newSet<T>(retval: UnsafeMutablePointer<Set<T>>, capacity: Int)
{
	if capacity <= 0 {
		retval.initialize(to: Set<T>())
	}
	else {
		retval.initialize(to: Set<T>(minimumCapacity: capacity))
	}
}

public func setIsEmpty<T>(s: UnsafeMutablePointer<Set<T>>) -> Bool
{
	return s.pointee.isEmpty
}

public func setGetCount<T>(s: UnsafeMutablePointer<Set<T>>) -> Int
{
	return s.pointee.count
}

public func setGetCapacity<T>(s: UnsafeMutablePointer<Set<T>>) -> Int
{
	return s.pointee.capacity
}

public func setContains<T>(s: UnsafeMutablePointer<Set<T>>, e: UnsafePointer<T>) -> Bool
{
	return s.pointee.contains(e.pointee)
}

public func setInsert<T>(retval: UnsafeMutablePointer<(Bool, T)>, s: UnsafeMutablePointer<Set<T>>, e: UnsafePointer<T>)
{
	retval.initialize(to: s.pointee.insert(e.pointee))
}

public func setRemove<T>(retval: UnsafeMutablePointer<T?>, s: UnsafeMutablePointer<Set<T>>, e: UnsafePointer<T>)
{
	retval.initialize(to: s.pointee.remove(e.pointee))
}
