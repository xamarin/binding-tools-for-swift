SWIFT_BIN = /usr/bin
SWIFT_LIB = /Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0

SWIFTC = $(SWIFT_BIN)/swiftc
SWIFTARGS = -sdk `xcrun --show-sdk-path` -emit-module -emit-library -enable-library-evolution -emit-module-interface

BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=HelloMod

libHello.dylib: *.swift *.cs
	@cp ../../lib/SwiftInterop/SwiftRuntimeLibrary.Mac.dll .
	@cp ../../lib/SwiftInterop/mac/XamGlue.framework/XamGlue .
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) *.swift
	$(BINDINGTOOLSFORSWIFT) --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) --retain-swift-wrappers -o . -C . -module-name $(OUTPUT_MODULE) 
	@xcrun install_name_tool -change "@rpath/XamGlue.framework/XamGlue" XamGlue libXamWrapping.dylib
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary.Mac -lib:. *.cs -out:$(OUTPUT_MODULE).exe

runit:
	@LD_LIBRARY_PATH=.:$(SWIFT_GLUE):$(SWIFT_LIB)/macosx mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.dll *.exe XamGlue HelloHelloMod.cs
	@rm -rf XamWrappingSource x86_64 XmlReflection
