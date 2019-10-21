SWIFT_BIN = ../../bin/swift/bin
SWIFT_LIB = ../../bin/swift/lib/swift/macosx/


SWIFTC = $(SWIFT_BIN)/swiftc
SDK = `xcrun --sdk macosx --show-sdk-path`
SWIFTARGS = -v -sdk $(SDK) -L $(SWIFT_LIB) -emit-module -emit-library

BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=Props

libPropertyBag.dylib: *.swift 
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64
	@cp ../../lib/SwiftInterop/mac/XamGlue.framework/XamGlue .
	@cp ../../lib/binding-tools-for-swift/SwiftRuntimeLibrary.dll .
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) *.swift
	$(BINDINGTOOLSFORSWIFT) --retain-swift-wrappers -o . -C . $(OUTPUT_MODULE) 
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary -lib:. *.cs -unsafe+ -out:$(OUTPUT_MODULE).exe

runit:
	@DYLD_LIBRARY_PATH=$(SWIFT_LIB):$(SWIFT_GLUE):. mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.dll *.exe XamGlue PropertyBagProps.cs
	@rm -rf XamWrappingSource armv7 armv7s arm64 x86_64
