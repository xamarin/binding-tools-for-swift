import Foundation

// Steve sez:
// This is a really handy routine to keep around in case you need to see what's in a string.
//public func dumpStringAsHex (s:UnsafeMutablePointer<String>) {
//	let size = MemoryLayout<String>.size  / MemoryLayout<UInt>.size
//    s.withMemoryRebound(to: UInt.self, capacity: size) { blah in
//    	for i in 0 ..< size {
//	        print(String(format: "%016x", blah[i]))
//	    }
//    }
//}

public func fromUnmanagedUTF16Raw(start: UnsafePointer<UInt16>, numberOfCodePoints: Int, result: UnsafeMutablePointer<String>) {
	let (s, _) = String.decodeCString(start, as: UTF16.self, repairingInvalidCodeUnits: true)!
	result.initialize(to: s)
}

public func fromUTF8(data: UnsafePointer<CChar>, result: UnsafeMutablePointer<String>) {
	result.initialize(to: String(utf8String: data)!)
}


public func createCharacter(str: UnsafeMutablePointer<String>, result: UnsafeMutablePointer<Character>) {
    result.initialize (to:str.pointee.first!);
}

public func characterValue(v:UnsafeMutablePointer<Character>, result: UnsafeMutablePointer<String>) {
    result.initialize (to:String (v.pointee));
}

public func UTF8StringSize(s: UnsafeMutablePointer<String>) -> Int {
    return s.pointee.utf8.count
}

public func copyStringToUTF8Buffer(data: UnsafeMutablePointer<UInt8>, s: UnsafeMutablePointer<String>)
{
    var i = 0
    s.pointee.utf8.forEach { char in
        data[i] = char
        i = i + 1
    }
}
