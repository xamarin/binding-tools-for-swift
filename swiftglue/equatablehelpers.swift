// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

private struct Equatable_xam_vtable
{
	internal var func0: (@convention(c)(_: UnsafeRawPointer, _: UnsafeRawPointer) -> Bool)?
}

private var _vtable: Equatable_xam_vtable = Equatable_xam_vtable()

public func setEquatable_xam_vtable(uvt: UnsafeRawPointer)
{
	let vt: UnsafePointer<Equatable_xam_vtable> = fromIntPtr(ptr: uvt)
	_vtable = vt.pointee
}

extension EveryProtocol : Equatable
{
	public static func ==(lhs:EveryProtocol, rhs:EveryProtocol) -> Bool
	{
		return _vtable.func0!(toIntPtr(value: lhs), toIntPtr(value: rhs))
	}
}

public func xam_proxy_EquatableOInEqualsEquals (lhs: EveryProtocol, rhs: EveryProtocol) -> Bool
{
	return lhs == rhs
}

