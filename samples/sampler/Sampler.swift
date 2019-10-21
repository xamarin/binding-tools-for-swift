public final class AFinalClass {
	public static func aStaticMethod() -> Double { return 3.14 }
	public static var aStaticProp: Bool = true
	
	private var _x: Float
	
	public init(x:Float) {
		_x = x;
	}
	
	public var xGetOnly:Float {
		get { return _x }
	}

	private static var _names:[String] = [ "one", "two", "three" ]
	
	public subscript(i:Int) -> String { return AFinalClass._names[i]; }
	
	
	public struct AStruct {
	
		private var _x:AFinalClass
		public init(x:AFinalClass) {
			_x = x
		}
		public func getClass() -> AFinalClass
		{
			return _x;
		}
	}
}

public enum Number {
	case Integer(Int)
	case Real(Double)
}

