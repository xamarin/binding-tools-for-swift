SWIFT_BIN = /usr/bin
SWIFT_LIB = /Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift

TOM_SWIFTY = ../../tom-swifty/bin/Debug/tom-swifty.exe
SWIFT_GLUE = ../../swiftglue/bin/Debug/mac/FinalProduct/XamGlue.framework
TOM_SWIFTY_LIB = ../../SwiftRuntimeLibrary.Mac/bin/Debug/

all: swift tomswifty csharp

swift:
	$(MAKE) -C swiftsrc

tomswifty:
	mono $(TOM_SWIFTY) --retain-swift-wrappers --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) -o . -C swiftsrc -C $(SWIFT_GLUE) -module-name HelloWorld

csharp: *.cs
	mcs -nowarn:CS0169 -lib:$(TOM_SWIFTY_LIB) -r:SwiftRuntimeLibrary.Mac -lib:swiftsrc *.cs -out:HelloWorld.exe
clean:
	$(MAKE) -C swiftsrc clean
	rm -f TopLevelEntitiesHelloWorld.cs
	rm -f HelloWorld.exe
	rm -rf bindings

runit:
	LD_LIBRARY_PATH=.:swiftsrc:$(TOM_SWIFTY_LIB):$(SWIFT_GLUE) \
	mono HelloWorld.exe
