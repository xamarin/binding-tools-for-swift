// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


public class PropertyBag<T> {
	private var bag: [String : T]
	
	public init() {
		bag = [String : T]()
	}
	
	public func add(key:String, val: T) {
		bag[key] = val
	}
		
	public func contains(key:String) -> Bool {
		return bag[key] != nil
	}
	
	public func contents() -> [(String, T)] {
		return Array(bag.lazy)
	}
}
