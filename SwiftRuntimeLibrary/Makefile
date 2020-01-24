TOP=..
include $(TOP)/common.mk

configuration ?= debug

all: create-pinvokes

clean-ios:
	rm -rf $(TOP)/SwiftRuntimeLibrary.iOS/bin/*
	rm -rf $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode

clean-mac:
	rm -rf $(TOP)/SwiftRuntimeLibrary.Mac/bin/*
	rm -rf $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode

clean: clean-ios clean-mac
	
create-pinvokes-ios:
	$(shell mkdir -p $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode)
	$(shell /Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TOP)/type-o-matic/bin/Debug/type-o-matic.exe --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=iphone --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_IOS_NAMESPACES)) --xamglue-framework=$(TOP)/swiftglue/bin/$(configuration)/iphone/FinalProduct/XamGlue.framework --output=$(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode/BindingMetadata.iOS.cs)

create-pinvokes-mac:
	$(shell mkdir -p $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode)
	$(shell /Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TOP)/type-o-matic/bin/Debug/type-o-matic.exe --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=mac --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_MAC_NAMESPACES)) --xamglue-framework=$(TOP)/swiftglue/bin/$(configuration)/mac/FinalProduct/XamGlue.framework --output=$(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode/BindingMetadata.MacOS.cs)

create-pinvokes: create-pinvokes-ios create-pinvokes-mac
