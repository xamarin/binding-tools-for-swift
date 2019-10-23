// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

internal struct Hashable_xam_vtable
{
	internal var func0: (@convention(c)(_: UnsafeRawPointer) -> Int)?;
}

private var _vtable: Hashable_xam_vtable = Hashable_xam_vtable();

public func setHashable_xam_vtable(uvt: UnsafeRawPointer)
{
	let vt: UnsafePointer<Hashable_xam_vtable> = fromIntPtr(ptr: uvt);
	_vtable = vt.pointee
}

extension EveryProtocol : Hashable
{
	public var hashValue: Int {
		get {
			return _vtable.func0!(toIntPtr(value: self))
		}
	}
	
	public func hash(into: inout Hasher) {
		into.combine(self.hashValue)
	}
}

public func newDict<T, U>(retval:UnsafeMutablePointer<[T:U]>, capacity:Int)
{
	retval.initialize(to: Dictionary<T, U>(minimumCapacity:capacity));
}

public func dictCount<T, U>(d:UnsafeMutablePointer<[T:U]>) -> Int
{
	return d.pointee.count
}

public func dictGet<T, U>(retval:UnsafeMutablePointer<(U, Bool)>, d: UnsafeMutablePointer<[T:U]>, key:T)
{
	let value = d.pointee[key]
	fromOptional(opt:value, retval:retval)
}

public func dictSet<T, U>(d: UnsafeMutablePointer<[T:U]>, key:T, val:U)
{
	d.pointee[key] = val;
}

public func dictContainsKey<T, U>(d:UnsafeMutablePointer<[T:U]>, key:T) -> Bool
{
	let value = d.pointee[key]
	return value != nil
}


public func dictKeys<T, U>(retval:UnsafeMutablePointer<[T]>, d:UnsafeMutablePointer<[T:U]>)
{
	retval.initialize(to:[T](d.pointee.keys))
}

public func dictValues<T, U>(retval:UnsafeMutablePointer<[U]>, d:UnsafeMutablePointer<[T:U]>)
{
	retval.initialize(to:[U](d.pointee.values))
}

public func dictAdd<T, U>(d:UnsafeMutablePointer<[T:U]>, key:T, value:U)
{
	d.pointee[key] = value
}

public func dictClear<T, U>(d:UnsafeMutablePointer<[T:U]>)
{
	d.pointee.removeAll(keepingCapacity: true)
}

public func dictRemove<T, U>(d:UnsafeMutablePointer<[T:U]>, key:T) -> Bool
{
	return d.pointee.removeValue(forKey:key) != nil
}

