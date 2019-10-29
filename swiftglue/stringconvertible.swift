// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Swift

internal struct CustomStringConvertible_xam_vtable {
	internal var func0: (@convention(c)(_: UnsafeRawPointer, _: UnsafeRawPointer) -> ())?;
}

private var _vtable: CustomStringConvertible_xam_vtable = CustomStringConvertible_xam_vtable();

public func setConvertible_xam_vtable(uvt: UnsafeRawPointer)
{ 
	let vt: UnsafePointer<CustomStringConvertible_xam_vtable> = fromIntPtr(ptr: uvt);
	_vtable = vt.pointee
}

extension EveryProtocol : CustomStringConvertible
{
	public var description : String {
		get {
			let retval = UnsafeMutablePointer<String>.allocate(capacity: 1)
			_vtable.func0!(retval, toIntPtr(value: self))
			let actualRetval = retval.move()
			retval.deallocate ()
			return actualRetval
		}
	}
}

public func xamarin_NoneDConvertibleGdescription(retval: UnsafeMutablePointer<String>, this: inout CustomStringConvertible)
{
    retval.initialize(to: this.description);
}
