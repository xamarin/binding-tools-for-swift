// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public func unsafeRawBufferPointerNew(retval: UnsafeMutablePointer<UnsafeRawBufferPointer>, 
    start: UnsafeRawPointer, count: Int)
{
    retval.initialize(to: UnsafeRawBufferPointer(start: start, count: count));
}

public func unsafeRawBufferPointerGetDescription(retval: UnsafeMutablePointer<String>, this: UnsafeMutablePointer<UnsafeRawBufferPointer>)
{
    retval.initialize(to: this.pointee.debugDescription);
}

public func unsafeRawBufferPointerCount(this: UnsafeMutablePointer<UnsafeRawBufferPointer>) -> Int
{
    return this.pointee.count;
}

public func unsafeRawBufferPointerGetAt(this: UnsafeMutablePointer<UnsafeRawBufferPointer>, index: Int) -> UInt8
{
	return this.pointee[index]
}

public func unsafeMutableRawBufferPointerNew(retval: UnsafeMutablePointer<UnsafeMutableRawBufferPointer>, 
    start: UnsafeMutableRawPointer, count: Int)
{
    retval.initialize(to: UnsafeMutableRawBufferPointer(start: start, count: count));
}

public func unsafeMutableRawBufferPointerGetDescription(retval: UnsafeMutablePointer<String>, this: UnsafeMutablePointer<UnsafeMutableRawBufferPointer>)
{
    retval.initialize(to: this.pointee.debugDescription);
}

public func unsafeMutableRawBufferPointerCount(this: UnsafeMutablePointer<UnsafeMutableRawBufferPointer>) -> Int
{
    return this.pointee.count;
}

public func unsafeMutableRawBufferPointerGetAt(this: UnsafeMutablePointer<UnsafeMutableRawBufferPointer>, index: Int) -> UInt8
{
	return this.pointee[index]
}

public func unsafeMutableRawBufferPinterSetAt(this: UnsafeMutablePointer<UnsafeMutableRawBufferPointer>, index: Int, value: UInt8)
{
	this.pointee[index] = value
}
