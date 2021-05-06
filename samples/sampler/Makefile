SWIFT_BIN = /usr/bin
XCODE_DEV = /Applications/Xcode.app/Contents/Developer
SWIFT_LIB = $(XCODE_DEV)/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0

SWIFTC = $(SWIFT_BIN)/swiftc
SWIFTARGS = -emit-library -enable-library-evolution -emit-module -emit-module-interface \
	-sdk $(XCODE_DEV)/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk \
	-L $(SWIFT_LIB)/macosx \
	-framework XamGlue

BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=Sampler

libSampler.dylib: *.swift 
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64
	@cp ../../lib/binding-tools-for-swift/SwiftRuntimeLibrary.dll .
	@mkdir -p XamGlue.framework
	@cp ../../lib/SwiftInterop/mac/XamGlue.framework/XamGlue XamGlue.framework
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) -F . *.swift
	$(BINDINGTOOLSFORSWIFT) --retain-swift-wrappers --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) --retain-swift-wrappers -o . -C . -module-name $(OUTPUT_MODULE) 
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary -lib:. *.cs -unsafe+ -out:$(OUTPUT_MODULE).exe

runit:
	@DYLD_LIBRARY_PATH=/usr/lib/swift:./XamGlue.framework:. mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.swiftinterface *.swiftsourceinfo *.dll *.exe XamGlue AFinalClassSampler.cs NumberSampler.cs XamGlue
	@rm -rf XamWrappingSource x86_64 bindings XamGlue.framework
