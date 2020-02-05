TOP=..
include $(TOP)/common.mk

configuration ?= debug
BINDING_METADATA_IOS = SwiftRuntimeLibrary.iOS/GeneratedCode/BindingMetadata.iOS.cs
BINDING_METADATA_MAC = SwiftRuntimeLibrary.Mac/GeneratedCode/BindingMetadata.MacOS.cs
TYPE_O_MATIC_EXE = $(TOP)/type-o-matic/bin/Debug/type-o-matic.exe
XAMGLUE_IOS = $(TOP)/swiftglue/bin/$(configuration)/iphone/FinalProduct/XamGlue.framework
XAMGLUE_MAC = $(TOP)/swiftglue/bin/$(configuration)/mac/FinalProduct/XamGlue.framework

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
	
$(TOP)/$(BINDING_METADATA_IOS): $(TYPE_O_MATIC_EXE) $(TOP)/common.mk $(XAMGLUE_IOS)
	@echo "Generating code for iOS..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TYPE_O_MATIC_EXE) --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=iphone --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_IOS_NAMESPACES)) --xamglue-framework=$(XAMGLUE_IOS) --output=$@
	@echo "Generated C# binding metadata files for iOS at $@"

$(TOP)/$(BINDING_METADATA_MAC): $(TYPE_O_MATIC_EXE) $(TOP)/common.mk $(XAMGLUE_MAC)
	@echo "Generating code for MacOS..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TYPE_O_MATIC_EXE) --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=mac --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_MAC_NAMESPACES)) --xamglue-framework=$(XAMGLUE_MAC) --output=$@
	@echo "Generated C# binding metadata files for MacOS at $@"

create-pinvokes: $(TOP)/$(BINDING_METADATA_MAC) $(TOP)/$(BINDING_METADATA_IOS)
create-pinvokes-ios: $(TOP)/$(BINDING_METADATA_IOS)
create-pinvokes-mac: $(TOP)/$(BINDING_METADATA_MAC)
