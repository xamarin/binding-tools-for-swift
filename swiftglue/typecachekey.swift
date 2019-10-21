public struct TypeCacheKey : Hashable {
	internal var _types : [ObjectIdentifier]
	public init(types: ObjectIdentifier...) {
		_types = types;
	}
	
	public func hash(into hasher:inout Hasher) {
		hasher.combine(_types.reduce(0, { $0 &+ $1.hashValue }))
	}
}
public func ==(lhs: TypeCacheKey, rhs: TypeCacheKey) -> Bool {
	return lhs._types == rhs._types
}