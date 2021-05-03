SWIFT_BIN = /usr/bin
SWIFT_LIB = /Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0

SWIFTC = $(SWIFT_BIN)/swiftc

BINDINGTOOLSFORSWIFT = mono ../../tom-swifty/bin/Debug/tom-swifty.exe 

BINDINGTOOLSFORSWIFTOUTPUT=tsout

all:
	./BuildUniversal.sh
	install_name_tool -add_rpath @executable_path EulerPhone.framework/EulerPhone
	@rm -rf $(BINDINGTOOLSFORSWIFTOUTPUT)
	@mkdir $(BINDINGTOOLSFORSWIFTOUTPUT)
	$(BINDINGTOOLSFORSWIFT) --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) --retain-swift-wrappers --type-database-path=../../bindings/ -o $(BINDINGTOOLSFORSWIFTOUTPUT) -C EulerPhone.framework -C ../../swiftglue/bin/Debug/iphone/FinalProduct/XamGlue.framework -module-name EulerPhone
	cd EulerPhoneApp/EulerPhoneApp && msbuild

clean:
	cd EulerPhoneApp/EulerPhoneApp && msbuild /t:clean
	rm -rf tsout
	rm -rf EulerPhone.framework
	rm -rf EulerPhone/build
	


