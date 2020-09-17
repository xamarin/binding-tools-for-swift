
public protocol Filling {
    var stuff: String { get }
}

public protocol Bread {
    var name: String { get }
    var sliced: Bool { get }
}

public struct Rye : Bread {
    public init () { }
    public var name:String {
        get { return "rye" }
    }
    public var sliced:Bool {
        get { return true }
    }
}

public struct Ham : Filling {
    public init () { }
    public var stuff: String {
        get { return "ham" }
    }
}

public func printSandwich (of: Bread, with: Filling) {
    print ("\(with.stuff) on \(of.name)")
}
