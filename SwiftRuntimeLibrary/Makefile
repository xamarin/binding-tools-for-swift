TOP=..
include $(TOP)/common.mk

configuration ?= debug
BINDING_METADATA_IOS = SwiftRuntimeLibrary.iOS/GeneratedCode/BindingMetadata.iOS.cs
BINDING_METADATA_MAC = SwiftRuntimeLibrary.Mac/GeneratedCode/BindingMetadata.MacOS.cs

all: create-pinvokes

clean-ios:
	@echo "Cleaning binary files and generated code for iOS"
	@rm -rf $(TOP)/SwiftRuntimeLibrary.iOS/bin/*
	@rm -rf $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode

clean-mac:
	@echo "Cleaning binary files and generated code for Mac"
	@rm -rf $(TOP)/SwiftRuntimeLibrary.Mac/bin/*
	@rm -rf $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode

clean: clean-ios clean-mac
	
create-pinvokes-ios:
	@echo "Generating code for iOS..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TOP)/type-o-matic/bin/Debug/type-o-matic.exe --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=iphone --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_IOS_NAMESPACES)) --xamglue-framework=$(TOP)/swiftglue/bin/$(configuration)/iphone/FinalProduct/XamGlue.framework --output=$(TOP)/$(BINDING_METADATA_IOS)
	@echo "Generated C# binding metadata files for iOS at $(TOP)/$(BINDING_METADATA_IOS)"

create-pinvokes-mac:
	@echo "Generating code for MacOS..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TOP)/type-o-matic/bin/Debug/type-o-matic.exe --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=mac --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_MAC_NAMESPACES)) --xamglue-framework=$(TOP)/swiftglue/bin/$(configuration)/mac/FinalProduct/XamGlue.framework --output=$(TOP)/$(BINDING_METADATA_MAC)
	@echo "Generated C# binding metadata files for MacOS at $(TOP)/$(BINDING_METADATA_MAC)"

create-pinvokes: create-pinvokes-ios create-pinvokes-mac
