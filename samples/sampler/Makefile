SWIFT_BIN = ../../bin/swift/bin
SWIFT_LIB = ../../bin/swift/lib/swift/macosx/

SWIFTC = $(SWIFT_BIN)/swiftc
SDK = `xcrun --sdk macosx --show-sdk-path`
SWIFTARGS = -v -sdk $(SDK) -L $(SWIFT_LIB) -emit-module -emit-library

BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

OUTPUT_MODULE=Sampler

libSampler.dylib: *.swift 
	@cp ../../lib/binding-tools-for-swift/SwiftRuntimeLibrary.dll .
	@cp ../../lib/SwiftInterop/mac/XamGlue.framework/XamGlue .
	$(SWIFTC) $(SWIFTARGS) -module-name $(OUTPUT_MODULE) *.swift
	$(BINDINGTOOLSFORSWIFT) --retain-swift-wrappers -o . -C . $(OUTPUT_MODULE) 
	mcs -nowarn:CS0169 -lib:../lib -r:SwiftRuntimeLibrary -lib:. *.cs -unsafe+ -out:$(OUTPUT_MODULE).exe

runit:
	@LD_LIBRARY_PATH=$(SWIFT_LIB) mono --arch=64 $(OUTPUT_MODULE).exe

clean:
	@rm -f *.dylib *.swiftmodule *.swiftdoc *.dll *.exe XamGlue AFinalClassSampler.cs NumberSampler.cs
	@rm -rf XamWrappingSource x86_64
