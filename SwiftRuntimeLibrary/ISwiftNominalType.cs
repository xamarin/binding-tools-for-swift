using System;
namespace SwiftRuntimeLibrary {
	public interface ISwiftNominalType : IDisposable {
		byte [] SwiftData { get; set; }
	}

	public interface ISwiftEnum : ISwiftNominalType {
	}

	public interface ISwiftStruct : ISwiftNominalType {
	}

	public enum SwiftNominalCtorArgument {
		None = 0
	}
}
