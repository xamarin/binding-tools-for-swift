import Swift


public final class DotNetCapsule {
	public var data: OpaquePointer
	public init(p:OpaquePointer) {
		data = p;
// Steve says:
// This code is for debugging only. It prints out the data that comes in and also does some
// truly horrible things to print out the pointer value of "self". This is not recommended, but
// if you uncomment this code, you're already desperate and desperate times call for desperate measures.
//		let n = unsafeBitCast(p, to: UInt.self)
//		let hex = String(n, radix:16, uppercase: false)
//		
//		print("DotNetCapsule init. Data: \(hex)")
//		
//		let n1 = unsafeBitCast(self, to: UInt.self)
//		let hex1 = String(n1, radix:16, uppercase: false)
//		
//		print("DotNetCapsule address: \(hex1)")
	}
	
	deinit {
		if (DotNetCapsule.onDeinitFunc != nil) {
			DotNetCapsule.onDeinitFunc!(toIntPtr(value: self))
		}
	}
	public static var onDeinitFunc: (@convention(c) (UnsafeRawPointer) -> ())?
}

public func setCapsuleDeinitFunc(p: @escaping (@convention(c) (UnsafeRawPointer) -> ())) {
	DotNetCapsule.onDeinitFunc = p
}

public func makeDotNetCapsule(p:OpaquePointer) -> DotNetCapsule {
	return DotNetCapsule(p:p)
}

public func getCapsuleData(dnc: DotNetCapsule) -> OpaquePointer {
	return dnc.data;
}

public func setCapsuleData(dnc: DotNetCapsule, p:OpaquePointer) {
	dnc.data = p
}