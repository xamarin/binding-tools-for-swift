SWIFT_DIR = "../../../apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/"
SWIFT_BIN = $(SWIFT_DIR)/bin
SWIFT_LIB = $(SWIFT_DIR)/lib/swift/macosx/

TOM_SWIFTY = ../../tom-swifty/bin/Debug/tom-swifty.exe
SWIFT_GLUE = ../../swiftglue/bin/Debug/mac/FinalProduct/XamGlue.framework
TOM_SWIFTY_LIB = ../../SwiftRuntimeLibrary.Mac/bin/Debug/

all: swift tomswifty csharp

swift:
	$(MAKE) -C swiftsrc

tomswifty:
	mono $(TOM_SWIFTY) --retain-swift-wrappers --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) -o . -C swiftsrc -C $(SWIFT_GLUE) HelloWorld

csharp: *.cs
	mcs -nowarn:CS0169 -lib:$(TOM_SWIFTY_LIB) -r:SwiftRuntimeLibrary.Mac -lib:swiftsrc *.cs -out:HelloWorld.exe
clean:
	$(MAKE) -C swiftsrc clean
	rm -f TopLevelEntitiesHelloWorld.cs
	rm -f HelloWorld.exe

run:
	LD_LIBRARY_PATH=.:swiftsrc:$(TOM_SWIFTY_LIB):$(SWIFT_GLUE) \
	mono HelloWorld.exe
