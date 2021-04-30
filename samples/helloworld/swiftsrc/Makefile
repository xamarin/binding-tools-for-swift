SWIFT_BIN = /usr/bin
SWIFT_LIB = /Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0

SWIFTC = $(SWIFT_BIN)/swiftc
SDK = `xcrun --show-sdk-path`
OUTPUT_MODULE = HelloWorld

all: libHelloWorld.dylib

libHelloWorld.dylib: *.swift
	$(SWIFTC) -sdk $(SDK) -emit-module -emit-library -enable-library-evolution -emit-module-interface -module-name $(OUTPUT_MODULE) *.swift
clean:
	rm -f HelloWorld.swiftdoc
	rm -f HelloWorld.swiftmodule
	rm -f libHelloWorld.dylib
