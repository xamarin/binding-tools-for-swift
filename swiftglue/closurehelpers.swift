import Swift;

// actions

public func netActionToSwiftClosure<T1>(a1: @escaping (UnsafeMutablePointer<T1>)->()) -> (T1) -> ()
{
	return { b1 in
		let args = UnsafeMutablePointer<T1>.allocate(capacity:1)
		args.initialize(to:b1)
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2>(a1: @escaping (UnsafeMutablePointer<(T1, T2)>)->()) ->
	(T1, T2) -> ()
{
	return { (b1, b2) in
		let args = UnsafeMutablePointer<(T1, T2)>.allocate(capacity:1)
		args.initialize(to:(b1, b2))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3)>)->()) ->
	(T1, T2, T3) -> ()
{
	return { (b1, b2, b3) in
		let args = UnsafeMutablePointer<(T1, T2, T3)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4)>)->()) ->
	(T1, T2, T3, T4) -> ()
{
	return { (b1, b2, b3, b4) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()) ->
	(T1, T2, T3, T4, T5) -> ()
{
	return { (b1, b2, b3, b4, b5) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()) ->
	(T1, T2, T3, T4, T5, T6) -> ()
{
	return { (b1, b2, b3, b4, b5, b6) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}


public func netActionToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(a1: @escaping (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) -> ()
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16))
		a1(args)
		args.deinitialize(count:1)
		args.deallocate()
	}
}



// funcs

public func netFuncToSwiftClosure<TR>(a1: @escaping (UnsafeMutablePointer<TR>)->()) -> () -> TR
{
	return { 
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr)
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->()) -> (T1) -> TR
{
	return { b1 in
		let args = UnsafeMutablePointer<T1>.allocate(capacity:1)
		args.initialize(to:b1)
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->()) ->
	(T1, T2) -> TR
{
	return { (b1, b2) in
		let args = UnsafeMutablePointer<(T1, T2)>.allocate(capacity:1)
		args.initialize(to:(b1, b2))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->()) ->
	(T1, T2, T3) -> TR
{
	return { (b1, b2, b3) in
		let args = UnsafeMutablePointer<(T1, T2, T3)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->()) ->
	(T1, T2, T3, T4) -> TR
{
	return { (b1, b2, b3, b4) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()) ->
	(T1, T2, T3, T4, T5) -> TR
{
	return { (b1, b2, b3, b4, b5) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()) ->
	(T1, T2, T3, T4, T5, T6) -> TR
{
	return { (b1, b2, b3, b4, b5, b6) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


public func netFuncToSwiftClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>(a1: @escaping (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()) ->
	(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) -> TR
{
	return { (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16) in
		let args = UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>.allocate(capacity:1)
		args.initialize(to:(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16))
		let resultptr = UnsafeMutablePointer<TR>.allocate(capacity:1)
		a1(resultptr, args)
		args.deinitialize(count:1)
		args.deallocate()
		let retval = resultptr.move()
		resultptr.deallocate()
		return retval
	}
}


// swift -> .NET callable

public func allocSwiftClosureToAction_0() ->
	UnsafeMutablePointer<()->()>
{
	return UnsafeMutablePointer<()->()>.allocate(capacity:1)
}

public func swiftClosureToAction(a1: @escaping ()->()) ->
	UnsafeMutablePointer<()->()>
{
	let p = allocSwiftClosureToAction_0 ();
	p.initialize(to: a1)
	return p
}

public func allocSwiftClosureToAction_1<T1>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<T1>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<T1>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1>(a1: @escaping (T1)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<T1>)->()>
{
	let f:(UnsafeMutablePointer<T1>)->() = { b1 in
		a1(b1.pointee)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<T1>)->()> = allocSwiftClosureToAction_1()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_2<T1, T2>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2>(a1: @escaping (T1, T2)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2)>)->() = { p in
		let (b1, b2) = p.pointee
		a1(b1, b2)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2)>)->()> = allocSwiftClosureToAction_2 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_3<T1, T2, T3>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3>(a1: @escaping (T1, T2, T3)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3)>)->() = { p in
		let (b1, b2, b3) = p.pointee
		a1(b1, b2, b3)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3)>)->()> = allocSwiftClosureToAction_3 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_4<T1, T2, T3, T4>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4>(a1: @escaping (T1, T2, T3, T4)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4)>)->() = { p in
		let (b1, b2, b3, b4) = p.pointee
		a1(b1, b2, b3, b4)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4)>)->()> = allocSwiftClosureToAction_4 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_5<T1, T2, T3, T4, T5>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5>(a1: @escaping (T1, T2, T3, T4, T5)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->() = { p in
		let (b1, b2, b3, b4, b5) = p.pointee
		a1(b1, b2, b3, b4, b5)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()> = allocSwiftClosureToAction_5()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_6<T1, T2, T3, T4, T5, T6>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6>(a1: @escaping (T1, T2, T3, T4, T5, T6)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6) = p.pointee
		a1(b1, b2, b3, b4, b5, b6)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()> = allocSwiftClosureToAction_6 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_7<T1, T2, T3, T4, T5, T6, T7>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()> = allocSwiftClosureToAction_7()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_8<T1, T2, T3, T4, T5, T6, T7, T8>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()> = allocSwiftClosureToAction_8 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_9<T1, T2, T3, T4, T5, T6, T7, T8, T9>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()> = allocSwiftClosureToAction_9 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_10<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()> = allocSwiftClosureToAction_10 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_11<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()> = allocSwiftClosureToAction_11 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_12<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()> = allocSwiftClosureToAction_12 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_13<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()> = allocSwiftClosureToAction_13 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_14<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()> = allocSwiftClosureToAction_14 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_15<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()> = allocSwiftClosureToAction_15 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToAction_16<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()>.allocate(capacity:1)
}

public func swiftClosureToAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)->()) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()>
{
	let f:(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->() = { p in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16) = p.pointee
		a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16)
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()> = allocSwiftClosureToAction_16 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_0<TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<TR>(a1: @escaping ()->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>)->()>
{
	let f:(UnsafeMutablePointer<TR>)->() = { rv in
		rv.initialize(to: a1())
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>)->()> = allocSwiftClosureToFunc_0()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_1<T1, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, TR>(a1: @escaping (T1)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->() = { (rv, p) in
		rv.initialize(to: a1(p.pointee))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->()> = allocSwiftClosureToFunc_1 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_2<T1, T2, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, TR>(a1: @escaping (T1, T2)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->() = { (rv, p) in
		let (b1, b2) = p.pointee
		rv.initialize(to: a1(b1, b2))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->()> = allocSwiftClosureToFunc_2 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_3<T1, T2, T3, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, TR>(a1: @escaping (T1, T2, T3)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->() = { (rv, p) in
		let (b1, b2, b3) = p.pointee
		rv.initialize(to: a1(b1, b2, b3))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->()> = allocSwiftClosureToFunc_3 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_4<T1, T2, T3, T4, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, TR>(a1: @escaping (T1, T2, T3, T4)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->() = { (rv, p) in
		let (b1, b2, b3, b4) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->()> = allocSwiftClosureToFunc_4 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_5<T1, T2, T3, T4, T5, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, TR>(a1: @escaping (T1, T2, T3, T4, T5)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->()> = allocSwiftClosureToFunc_5 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_6<T1, T2, T3, T4, T5, T6, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->()> = allocSwiftClosureToFunc_6 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_7<T1, T2, T3, T4, T5, T6, T7, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->()> = allocSwiftClosureToFunc_7 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_8<T1, T2, T3, T4, T5, T6, T7, T8, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->()> = allocSwiftClosureToFunc_8 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_9<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->()> = allocSwiftClosureToFunc_9 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_10<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->()> = allocSwiftClosureToFunc_10 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_11<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->()> = allocSwiftClosureToFunc_11 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_12<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->()> = allocSwiftClosureToFunc_12 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_13<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->()> = allocSwiftClosureToFunc_13 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_14<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->()> = allocSwiftClosureToFunc_14 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_15<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->()> = allocSwiftClosureToFunc_15 ()
	p.initialize(to:f)
	return p
}

public func allocSwiftClosureToFunc_16<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>() ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()>
{
	return UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()>.allocate(capacity:1)
}

public func swiftClosureToFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>(a1: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)->TR) ->
	UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()>
{
	let f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->() = { (rv, p) in
		let (b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16) = p.pointee
		rv.initialize(to: a1(b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16))
	}
	let p:UnsafeMutablePointer<(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->()> = allocSwiftClosureToFunc_16 ()
	p.initialize(to:f)
	return p
}



// wrap actions for return values

public func swiftActionWrapper<T1>(f1: @escaping (T1) -> ()) ->
		(UnsafeMutablePointer<(T1)>)->() {
	let fprime: (UnsafeMutablePointer<(T1)>)->() = {
		(targ) in
		f1(targ.pointee)
	}
	return fprime;
}

public func swiftActionWrapper<T1, T2>(f2: @escaping (T1, T2) -> ()) ->
		(UnsafeMutablePointer<(T1, T2)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2)>)->() = {
		(targ) in
		let (a1, a2) = targ.pointee
		f2(a1, a2)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3>(f3: @escaping (T1, T2, T3) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3)>)->() = {
		(targ) in
		let (a1, a2, a3) = targ.pointee
		f3(a1, a2, a3)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4>(f4: @escaping (T1, T2, T3, T4) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4)>)->() = {
		(targ) in
		let (a1, a2, a3, a4) = targ.pointee
		f4(a1, a2, a3, a4)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5>(f5: @escaping (T1, T2, T3, T4, T5) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5) = targ.pointee
		f5(a1, a2, a3, a4, a5)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6>(f6: @escaping (T1, T2, T3, T4, T5, T6) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6) = targ.pointee
		f6(a1, a2, a3, a4, a5, a6)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7>(f7: @escaping (T1, T2, T3, T4, T5, T6, T7) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7) = targ.pointee
		f7(a1, a2, a3, a4, a5, a6, a7)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8>(f8: @escaping (T1, T2, T3, T4, T5, T6, T7, T8) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8) = targ.pointee
		f8(a1, a2, a3, a4, a5, a6, a7, a8)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9>(f9: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9) = targ.pointee
		f9(a1, a2, a3, a4, a5, a6, a7, a8, a9)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(f10: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) = targ.pointee
		f10(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(f11: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) = targ.pointee
		f11(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(f12: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) = targ.pointee
		f12(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(f13: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) = targ.pointee
		f13(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(f14: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) = targ.pointee
		f14(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(f15: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) = targ.pointee
		f15(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15)
	}
	return fprime
}

public func swiftActionWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(f16: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) -> ()) ->
		(UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->() {
	let fprime: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->() = {
		(targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) = targ.pointee
		f16(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16)
	}
	return fprime
}



public func swiftFuncWrapper<TR>(f0: @escaping () -> TR) ->
		(UnsafeMutablePointer<TR>)->() {
	let fprime: (UnsafeMutablePointer<TR>)->() = {
		(trp) in
		trp.initialize(to: f0())
	}
	return fprime
} 

public func swiftFuncWrapper<T1, TR>(f1: @escaping (T1) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1)>)->() = {
		(trp, targ) in
		trp.initialize(to: f1(targ.pointee))
	}
	return fprime
}

public func swiftFuncWrapper<T1, T2, TR>(f2: @escaping (T1, T2) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->() = {
		(trp, targ) in
		let (a1, a2) = targ.pointee
		trp.initialize(to: f2(a1, a2))
	}
	return fprime
}

public func swiftFuncWrapper<T1, T2, T3, TR>(f3: @escaping (T1, T2, T3) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->() = {
		(trp, targ) in
		let (a1, a2, a3) = targ.pointee
		trp.initialize(to: f3(a1, a2, a3))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, TR>(f4: @escaping (T1, T2, T3, T4) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4) = targ.pointee
		trp.initialize(to: f4(a1, a2, a3, a4))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, TR>(f5: @escaping (T1, T2, T3, T4, T5) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5) = targ.pointee
		trp.initialize(to: f5(a1, a2, a3, a4, a5))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, TR>(f6: @escaping (T1, T2, T3, T4, T5, T6) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6) = targ.pointee
		trp.initialize(to: f6(a1, a2, a3, a4, a5, a6))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, TR>(f7: @escaping (T1, T2, T3, T4, T5, T6, T7) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7) = targ.pointee
		trp.initialize(to: f7(a1, a2, a3, a4, a5, a6, a7))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, TR>(f8: @escaping (T1, T2, T3, T4, T5, T6, T7, T8) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8) = targ.pointee
		trp.initialize(to: f8(a1, a2, a3, a4, a5, a6, a7, a8))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(f9: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9) = targ.pointee
		trp.initialize(to: f9(a1, a2, a3, a4, a5, a6, a7, a8, a9))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(f10: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) = targ.pointee
		trp.initialize(to: f10(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>(f11: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) = targ.pointee
		trp.initialize(to: f11(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>(f12: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) = targ.pointee
		trp.initialize(to: f12(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>(f13: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) = targ.pointee
		trp.initialize(to: f13(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>(f14: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) = targ.pointee
		trp.initialize(to: f14(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>(f15: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) = targ.pointee
		trp.initialize(to: f15(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15))
	}
	return fprime
} 

public func swiftFuncWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>(f16: @escaping (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) -> TR) ->
		(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->() {
	let fprime: (UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->() = {
		(trp, targ) in
		let (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) = targ.pointee
		trp.initialize(to: f16(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16))
	}
	return fprime
} 


public func invokePlainAction (f:()->()) {
	f()
}

public func invokeAction<T1> (f: (UnsafeMutablePointer<T1>)->(), a:UnsafeMutablePointer<T1>) {
	f(a)
}

public func invokeAction<T1, T2> (f: (UnsafeMutablePointer<(T1, T2)>)->(), a:UnsafeMutablePointer<(T1, T2)>) {
	f(a)
}

public func invokeAction<T1, T2, T3> (f: (UnsafeMutablePointer<(T1, T2, T3)>)->(), a:UnsafeMutablePointer<(T1, T2, T3)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4> (f: (UnsafeMutablePointer<(T1, T2, T3, T4)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>) {
	f(a)
}

public func invokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> (f: (UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->(), a:UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>) {
	f(a)
}

public func invokeFunction<TR>(f:(UnsafeMutablePointer<TR>)->(), retval: UnsafeMutablePointer<TR>) {
	f(retval)
}

public func invokeFunction<T1, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<T1>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<T1>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>) {
	f(retval, a)
}

public func invokeFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>(f:(UnsafeMutablePointer<TR>, UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>)->(), retval: UnsafeMutablePointer<TR>, a: UnsafeMutablePointer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>) {
	f(retval, a)
}
