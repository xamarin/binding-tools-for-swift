SWIFT_BIN = /usr/bin
XCODE_DEV = /Applications/Xcode.app/Contents/Developer
SWIFT_LIB = $(XCODE_DEV)/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift

SWIFTC = $(SWIFT_BIN)/swiftc
SWIFTARGS = -sdk `xcrun --show-sdk-path` -emit-module -emit-library -enable-library-evolution -emit-module-interface


BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=Props

libPropertyBag.dylib: *.swift 
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64
	@cp ../../lib/binding-tools-for-swift/SwiftRuntimeLibrary.dll .
	@mkdir -p XamGlue.framework
	@cp -r ../../lib/SwiftInterop/mac/XamGlue.framework .
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) *.swift
	$(BINDINGTOOLSFORSWIFT) --retain-swift-wrappers --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) -o . -C . -module-name $(OUTPUT_MODULE) 
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary -lib:. *.cs -unsafe+ -out:$(OUTPUT_MODULE).exe

runit:
	@DYLD_LIBRARY_PATH=/usr/lib/swift:./XamGlue.framework:. mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.swiftinterface *.swiftsourceinfo *.dll *.exe XamGlue PropertyBagProps.cs
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64 bindings XamGlue.framework
