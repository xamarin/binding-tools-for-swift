SWIFT_BIN = /usr/bin
XCODE_DEV = /Applications/Xcode.app/Contents/Developer
SWIFT_LIB = $(XCODE_DEV)/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift-5.0
BINDINGTOOLSFORSWIFT=../../binding-tools-for-swift

all: swift_pig_latin

swift_pig_latin: swiftPigLatinLibrary/SwiftPigLatinLibrary/SwiftIgPay.swift
	# compile the swift library using Xcode
	xcodebuild clean build \
		-project SwiftPigLatinLibrary/SwiftPigLatinLibrary.xcodeproj \
		-scheme SwiftPigLatinLibrary \
		-configuration Debug \
		-sdk iphonesimulator \
		-derivedDataPath derived_data \
		BUILD_LIBRARY_FOR_DISTRIBUTION=YES
	mkdir -p frameworks

	# copy the swift framework locally
	cp -r derived_data/Build/Products/Debug-iphonesimulator/SwiftPigLatinLibrary.framework frameworks
	rm -rf derived_data
	cp -r ../../lib/SwiftInterop/iphone/XamGlue.xcframework/ios-i386_x86_64-simulator/XamGlue.framework frameworks

	# build the C# binding
	mkdir -p bindingtoolsoutput
	$(BINDINGTOOLSFORSWIFT) --type-database-path=../../bindings --retain-swift-wrappers --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) -o bindingtoolsoutput -C . -C frameworks/SwiftPigLatinLibrary.framework -C frameworks/XamGlue.framework -C frameworks -module-name SwiftPigLatinLibrary
	
	# copy all the needed frameworks
	cp -r bindingtoolsoutput/XamWrapping.framework frameworks
	cp -r frameworks PigLatin/PigLatin

	# copy the C# binding
	cp bindingtoolsoutput/*.cs PigLatin/PigLatin

clean:
	rm -rf bindingtoolsoutput
	rm -rf frameworks
	rm -rf derived_data
	rm -rf PigLatin/PigLatin/frameworks
	
