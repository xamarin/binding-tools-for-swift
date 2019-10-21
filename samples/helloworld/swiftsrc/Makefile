SWIFT_DIR = "../../../../apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/"
SWIFT_BIN = $(SWIFT_DIR)/bin
SWIFT_LIB = $(SWIFT_DIR)/lib/swift/macosx/
SWIFTC = $(SWIFT_BIN)/swiftc
SDK = `xcrun --show-sdk-path`
OUTPUT_MODULE = HelloWorld

all: libHelloWorld.dylib

libHelloWorld.dylib: *.swift
	$(SWIFTC) -sdk $(SDK) -emit-module -emit-library -module-name $(OUTPUT_MODULE) *.swift
clean:
	rm -f HelloWorld.swiftdoc
	rm -f HelloWorld.swiftmodule
	rm -f libHelloWorld.dylib
