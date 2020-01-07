// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public func toIntPtr<T>(value:T) -> UnsafeRawPointer
{
	return unsafeBitCast(value, to: UnsafeRawPointer.self);
}

public func fromIntPtr<T>(ptr: UnsafeRawPointer) -> T
{
	return unsafeBitCast(ptr, to: T.self);
}


private func roundUpToAlignment(value:Int, align:Int) -> Int
{
	return (value + align - 1) / align * align;
}

public func fromOptional<T>(opt: T?, retval: UnsafeMutablePointer<(T, Bool)>)
{
	// this is where we get a little sleazy to work around ARC.
	// In the case of an optional with a value - that's easy - just
	// stuff the value into a tuple.
	//
	// In the case of a nil optional, we have a problem. We need to be
	// able to initialize the tuple, but we can't allocate one.
	// If we allocate one, we cause problems because we can't assign it.
	// So instead, we find the offset to where the boolean sits in the tuple
	// and bang a false into that spot. The space allocated before that,
	// well, we don't need it, so we never touch it and then Swift never
	// things that something changed and it needs to change the reference
	// count.
	if opt != nil {
		retval.initialize(to:(opt!, true));
	}
	else {
		let alignment = MemoryLayout<T>.alignment;
		let offset = roundUpToAlignment(value: 0, align: alignment);
		let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self, capacity: offset + 1) + offset;
		rawPointer.withMemoryRebound(to: Bool.self, capacity:1) {
			$0.initialize(to:false);
		}
	}
}

private func setExceptionBool<T>(b: Bool, retval: UnsafeMutablePointer<(T, Error, Bool)>)
{
	let alignment = MemoryLayout<(T, Error)>.alignment;
	let boolOffset = roundUpToAlignment(value: MemoryLayout<(T, Error)>.size, align:alignment);
	let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self, capacity: boolOffset + 1)
		+ boolOffset;
	rawPointer.withMemoryRebound(to: Bool.self, capacity: 1) {
		$0.initialize(to:b);
	}
}

private func getExceptionBool<T>(retval: UnsafeMutablePointer<(T, Error, Bool)>) -> Bool
{
	let alignment = MemoryLayout<(T, Error)>.alignment;
	let boolOffset = roundUpToAlignment(value: MemoryLayout<(T, Error)>.size, align:alignment);
	let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self, capacity: boolOffset + 1)
		+ boolOffset;
	var result = false;
	rawPointer.withMemoryRebound(to: Bool.self, capacity: 1) {
		result = $0.pointee;
	}
	return result;
}

public func setExceptionThrown<T>(err: Error, retval: UnsafeMutablePointer<(T, Error, Bool)>)
{
	let alignment = MemoryLayout<T>.alignment;
	let errOffset = roundUpToAlignment(value: MemoryLayout<T>.size, align:alignment);
	let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self,
		capacity: errOffset + MemoryLayout<Error>.stride) + errOffset;
	rawPointer.withMemoryRebound(to: Error.self, capacity: 1) {
		$0.initialize(to:err);
	}
	setExceptionBool(b: true, retval:retval)
}

public func setExceptionNotThrown<T>(value: T, retval: UnsafeMutablePointer<(T, Error, Bool)>)
{
	let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self,
		capacity: MemoryLayout<T>.stride);
	rawPointer.withMemoryRebound(to: T.self, capacity: 1) {
		$0.initialize(to:value);
	}
	setExceptionBool(b: false, retval:retval)
}

public func isExceptionThrown<T>(retval: UnsafeMutablePointer<(T, Error, Bool)>) -> Bool
{
	return getExceptionBool(retval:retval)
}

public func getExceptionThrown<T>(retval: UnsafeMutablePointer<(T, Error, Bool)>) -> Error?
{
	var result:Error? = nil;
	if getExceptionBool(retval:retval) {
		let alignment = MemoryLayout<T>.alignment;
		let errOffset = roundUpToAlignment(value: MemoryLayout<T>.size, align:alignment);
		let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self,
			capacity: errOffset + MemoryLayout<Error>.stride) + errOffset;
		rawPointer.withMemoryRebound(to: Error.self, capacity: 1) {
			result = $0.pointee;
		}
	}
	return result
}

public func getExceptionNotThrown<T>(retval: UnsafeMutablePointer<(T, Error, Bool)>) -> T?
{
	var result: T? = nil;
	if !getExceptionBool(retval:retval) {
		let rawPointer = UnsafeMutableRawPointer(retval).bindMemory(to: Int8.self,
			capacity: MemoryLayout<T>.stride);
		rawPointer.withMemoryRebound(to: T.self, capacity: 1) {
			result = $0.pointee;
		}
	}
	return result;
}


public func allocateUnsafeMutablePointer<T>(retval: UnsafeMutablePointer<UnsafeMutablePointer<T>>, capacity: Int)
{
	retval.pointee = UnsafeMutablePointer<T>.allocate(capacity:capacity)
}

public func getPointeeUnsafeMutablePointer<T>(retval: UnsafeMutablePointer<T>, p: UnsafeMutablePointer<T>)
{
	retval.initialize(to: p.pointee)
}

public func setPointeeUnsafeMutablePointer<T>(p: UnsafeMutablePointer<T>, val: UnsafeMutablePointer<T>)
{
	p.pointee = val.pointee
}

public func advanceUnsafeMutablePointer<T>(retval: UnsafeMutablePointer<UnsafeMutablePointer<T>>, p: UnsafeMutablePointer<T>, by: Int)
{
	retval.initialize(to: p.advanced(by: by))
}

public func castAs<T>(retval: UnsafeMutablePointer<T?>, value: AnyObject)
{
	retval.initialize(to: value as? T)
}

public func castAs<T>(retval: UnsafeMutablePointer<T?>, value: Any)
{
	retval.initialize(to: value as? T)
}


public func sizeof<T>(_ ignored: T) -> Int
{
	return MemoryLayout<T>.size
}

public func strideof<T> (_ ignored: T) -> Int
{
	return MemoryLayout<T>.stride
}

public func alignmentof<T> (_ ignored: T) -> Int
{
	return MemoryLayout<T>.alignment
}


//public func hexString(x:UInt64) -> String {
//let s = String(x, radix:16)
//switch s.count {
//case 1: return "0000000" + s;
//case 2: return "000000" + s;
//case 3: return "00000" + s;
//case 4: return "0000" + s;
//case 5: return "000" + s;
//case 6: return "00" + s;
//case 7: return "0" + s;
//default: return s;
//}
//}
//
//public func printHexInt(ptr: UnsafeRawPointer)
//{
//let p = ptr.assumingMemoryBound(to:UInt64.self)
//print("Data:") 
//print("0x", hexString(x: p[0]))
//}
//
//public func printHexInt<T>(ptr: UnsafeMutablePointer<T>)
//{
//printHexInt(ptr: UnsafeRawPointer(ptr))
//}

//public func printProtocol(ptr: UnsafeRawPointer)
//{
//	let p = ptr.assumingMemoryBound(to:UInt64.self)
//print("Data:") 
//print("0x", hexString(x: p[0]), " 0x", hexString(x: p[1]),  " 0x", hexString(x: p[2]), separator:"");
//print("Impl:");
//print("Metadata 0x", hexString(x: p[3]), " Protocol Witness Table 0x", hexString(x: p[4]), separator:"");
//}
