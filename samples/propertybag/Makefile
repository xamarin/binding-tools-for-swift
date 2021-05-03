SWIFT_BIN = /usr/bin
SWIFT_LIB = /Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0

SWIFTC = $(SWIFT_BIN)/swiftc
SDK = `xcrun --sdk macosx --show-sdk-path`
SWIFTARGS = -v -sdk $(SDK) -L $(SWIFT_LIB) -emit-module -emit-library -enable-library-evolution -emit-module-interface


BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=Props

libPropertyBag.dylib: *.swift 
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64
	@cp ../../lib/SwiftInterop/mac/XamGlue.framework/XamGlue .
	@cp ../../lib/binding-tools-for-swift/SwiftRuntimeLibrary.dll .
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) *.swift
	$(BINDINGTOOLSFORSWIFT) --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) --retain-swift-wrappers -o . -C . -module-name $(OUTPUT_MODULE) 
	@xcrun install_name_tool -change "@rpath/XamGlue.framework/XamGlue" XamGlue libXamWrapping.dylib
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary -lib:. *.cs -unsafe+ -out:$(OUTPUT_MODULE).exe

runit:
	@DYLD_LIBRARY_PATH=$(SWIFT_LIB)/macosx:$(SWIFT_GLUE):. mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.dll *.exe XamGlue PropertyBagProps.cs
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64 bindings
