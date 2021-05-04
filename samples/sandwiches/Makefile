SWIFT_BIN = /usr/bin
SWIFT_LIB = /Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0

SWIFTC = $(SWIFT_BIN)/swiftc
SDK = `xcrun --sdk macosx --show-sdk-path`
SWIFTARGS = -v -sdk $(SDK) -L $(SWIFT_LIB) -emit-module -emit-library -enable-library-evolution -emit-module-interface


BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=Sandwiches

libSandwiches.dylib: *.swift 
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64
	@cp ../../lib/SwiftInterop/SwiftRuntimeLibrary.Mac.dll .
	@cp ../../lib/SwiftInterop/mac/XamGlue.framework/XamGlue .
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) *.swift
	$(BINDINGTOOLSFORSWIFT) --retain-swift-wrappers --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) -o . -C . -module-name $(OUTPUT_MODULE) 
	@xcrun install_name_tool -change "@rpath/XamGlue.framework/XamGlue" XamGlue libXamWrapping.dylib
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary.Mac -lib:. *.cs -unsafe+ -out:$(OUTPUT_MODULE).exe

runit:
	@DYLD_LIBRARY_PATH=$(SWIFT_LIB)/macosx:. mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.swiftinterface *.swiftsourceinfo *.dll *.exe TopLevelEntities.cs IBreadSandwiches.cs IFillingSandwiches.cs HamSandwiches.cs RyeSandwiches.cs XamGlue
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64 bindings
