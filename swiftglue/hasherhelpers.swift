// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public func hasherNew(retval: UnsafeMutablePointer<Hasher>)
{
    retval.initialize(to: Hasher());
}

public func hasherCombine<T0>(this: UnsafeMutablePointer<Hasher>, thing: T0) where T0 : Hashable
{
    this.pointee.combine(thing);
}

public func hasherCombine(this: UnsafeMutablePointer<Hasher>, bytes: UnsafePointer<UnsafeRawBufferPointer>)
{
    this.pointee.combine(bytes: bytes.pointee);
}

public func hasherFinalize(this: UnsafeMutablePointer<Hasher>) -> Int
{
    return this.pointee.finalize();
}
