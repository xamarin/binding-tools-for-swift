
internal struct Comparable_xam_vtable
{
		internal var func0: (@convention(c)(_: UnsafeRawPointer, _: UnsafeRawPointer) -> Bool)?;
		internal var func1: (@convention(c)(_: UnsafeRawPointer, _: UnsafeRawPointer) -> Bool)?;
}

private var _vtable: Comparable_xam_vtable = Comparable_xam_vtable()

public func setComparable_xam_vtable(uvt: UnsafeRawPointer)
{
	let vt: UnsafePointer<Comparable_xam_vtable> = fromIntPtr(ptr: uvt);
	_vtable = vt.pointee
}


extension EveryProtocol : Comparable
{
	public static func < (lhs:EveryProtocol, rhs:EveryProtocol) -> Bool
	{
		return _vtable.func1!(toIntPtr(value: lhs), toIntPtr(value: rhs));
	}
}

public func xam_proxy_ComparableOInEqualsEquals(lhs: EveryProtocol,
				rhs: EveryProtocol) -> Bool
{
	return lhs == rhs
}

public func xam_proxy_ComparableOInLess(lhs: EveryProtocol,
				rhs: EveryProtocol) -> Bool
{
	return lhs < rhs
}

