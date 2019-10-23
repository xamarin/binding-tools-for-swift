// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Swift

public enum SwiftEnumError : Error { case undefined }

public struct DotNetError : Error, CustomStringConvertible {
	private var _message : String
	public init(message : String, className: String) {
		_message = message
		self.className = className
	}
	
	public private(set) var className : String
	
	public var description: String {
		get {
			return _message;
		}
	}
}

public func makeDotNetError(message: UnsafePointer<String>, className: UnsafePointer<String>) -> Error {
	return DotNetError(message:message.pointee, className: className.pointee)
}

public func getErrorDescription(message: UnsafeMutablePointer<String>, error: Error)
{
	message.initialize(to: String(describing:error))
}

public func errorMetadata () -> Any.Type
{
	return Error.self
}
