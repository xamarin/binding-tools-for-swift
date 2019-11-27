// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

internal var iteratorprotocol_xam_helperVtableCache = [TypeCacheKey : iteratorprotocol_xam_vtable ] ()

internal struct iteratorprotocol_xam_vtable
{
	internal var func0: (@convention(c)(UnsafeRawPointer, UnsafeRawPointer)->())?
}

internal func iteratorprotocol_get_xam_vtable(_ t0: Any.Type) -> iteratorprotocol_xam_vtable?
{
    return iteratorprotocol_xam_helperVtableCache[TypeCacheKey(types: ObjectIdentifier(t0))];
}

public class iteratorprotocol_xam_helper<Elem> : IteratorProtocol {
	private var _xamarinClassIsInitialized: Bool = false;
	
	public init ()
	{
		_xamarinClassIsInitialized = true;
	}
	
	public func next() -> Elem? {
		let vt = iteratorprotocol_get_xam_vtable(Elem.self)!
		let retval = UnsafeMutablePointer<Elem?>.allocate(capacity: 1)
		vt.func0!(toIntPtr(value: retval), toIntPtr(value: self))
		let actualRetval = retval.move()
		retval.deallocate();
		return actualRetval
	}
}

public func iteratorprotocol_set_xam_vtable (_ vt0: UnsafeRawPointer, _ t0: Any.Type)
{
	let vt: UnsafePointer<iteratorprotocol_xam_vtable> = fromIntPtr(ptr: vt0)
	
	iteratorprotocol_xam_helperVtableCache [ TypeCacheKey(types: ObjectIdentifier(t0)) ] = vt.pointee
}

public func newIteratorProtocol<T>() -> iteratorprotocol_xam_helper<T>
{
	return iteratorprotocol_xam_helper<T> ()
}

public func anyIteratorProtocolNext<T>(retval: UnsafeMutablePointer<T.Element?>, this: UnsafeMutablePointer<T> ) where T:IteratorProtocol
{
	retval.initialize(to: this.pointee.next())
}

