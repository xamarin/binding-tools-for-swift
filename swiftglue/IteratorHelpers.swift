// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Swift

fileprivate var _vtable: [TypeCacheKey : SwiftIteratorProtocol_xam_vtable] 
    = [TypeCacheKey : SwiftIteratorProtocol_xam_vtable]();

fileprivate struct SwiftIteratorProtocol_xam_vtable
{
    
    fileprivate var 
    func0: (@convention(c)(UnsafeRawPointer, 
        UnsafeRawPointer) -> ())?;
}

public final class SwiftIteratorProtocolProtocol<T0> : IteratorProtocol
{
	private var _xamarinClassIsInitialized: Bool = false;
	
    public init()
    {
    }
    
	public func next() -> Optional<T0>
	{    
		let vt: SwiftIteratorProtocol_xam_vtable = getSwiftIteratorProtocol_xam_vtable(T0.self)!;
        
		let retval = UnsafeMutablePointer<Swift.Optional<T0>>.allocate(capacity: 1);
		vt.func0!(retval, toIntPtr(value: self));
        
		let actualRetval = retval.move();
		retval.deallocate();
		return actualRetval;
	}
}

public func setSwiftIteratorProtocol_xam_vtable(_ uvt: UnsafeRawPointer, _ t0: Any.Type)
{    
	let vt: UnsafePointer<SwiftIteratorProtocol_xam_vtable> = fromIntPtr(ptr: uvt);
    
    _vtable[TypeCacheKey(types: ObjectIdentifier(t0))] = vt.pointee;
}

fileprivate func getSwiftIteratorProtocol_xam_vtable(_ t0: Any.Type) -> SwiftIteratorProtocol_xam_vtable?
{
    return _vtable[TypeCacheKey(types: ObjectIdentifier(t0))];
}

public func xamarin_static_wrapper_IteratorProtocol_next<T0, T1>(this: inout T0) -> 
	Optional<T1> where T0 : IteratorProtocol, T1 == T0.Element
{
	return this.next();
}

public func xamarin_XamWrappingFxamarin_static_wrapper_ProtocolTests_IteratorProtocol_next00000000<T0, T1>
			(retval: UnsafeMutablePointer<Optional<T1>>, this: inout T0) 
				where T0 : IteratorProtocol, T1 == T0.Element
{
	retval.initialize(to: xamarin_static_wrapper_IteratorProtocol_next(this: &this));
}

public func xamarin_SwiftIteratorProtocolProtocolDnext00000001<T0>(retval: 
	UnsafeMutablePointer<Optional<T0>>, this: SwiftIteratorProtocolProtocol<T0>)
{
	retval.initialize(to: this.next());
}

public func xamarin_SwiftIteratorProtocolProtocolDSwiftIteratorProtocolProtocol00000002<T0>() 
    -> SwiftIteratorProtocolProtocol<T0>
{
    return SwiftIteratorProtocolProtocol();
}