
public func arrayNew<T>(capacity: Int) -> [T] {
	var a = [T]()
	a.reserveCapacity(capacity)
	return a
}

public func arrayCount<T>(a:UnsafeMutablePointer<[T]>) -> Int {
	return a.pointee.count
}

public func arrayGet<T>(retval: UnsafeMutablePointer<T>, a:UnsafeMutablePointer<[T]>, index: Int) {
	retval.initialize(to: a.pointee[index])
}

public func arraySet<T>(a:UnsafeMutablePointer<[T]>, value: T, index: Int) {
	a.pointee[index] = value
}

public func arrayInsert<T>(a:UnsafeMutablePointer<[T]>, value: T, index: Int) {
	a.pointee.insert(value, at: index)
}

public func arrayClear<T>(a:UnsafeMutablePointer<[T]>) {
	a.pointee.removeAll(keepingCapacity: true)
}

public func arrayRemoveAt<T>(a:UnsafeMutablePointer<[T]>, index: Int) {
	a.pointee.remove(at: index)
}

public func arrayAdd<T>(a:UnsafeMutablePointer<[T]>, thing: T) {
	a.pointee.append(thing)
}

public func arrayCapacity<T> (a:UnsafeMutablePointer<[T]>) -> Int {
	return a.pointee.capacity
}

public func arrayReserveCapacity<T> (a:UnsafeMutablePointer<[T]>, capacity: Int) {
	a.pointee.reserveCapacity(capacity)
}
