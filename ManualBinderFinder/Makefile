TOP=..
include $(TOP)/common.mk

configuration ?= debug
BINDING_METADATA_IOS = SwiftRuntimeLibrary.iOS/GeneratedCode/BindingMetadata.iOS.cs
BINDING_METADATA_MAC = SwiftRuntimeLibrary.Mac/GeneratedCode/BindingMetadata.MacOS.cs
BINDING_SWIFT_METADATA_IOS = SwiftRuntimeLibrary.iOS/GeneratedCode/BindingMetadata.Swift-iOS.cs
BINDING_SWIFT_METADATA_MAC = SwiftRuntimeLibrary.Mac/GeneratedCode/BindingMetadata.Swift-MacOS.cs
BINDING_METADATA_Test = SwiftRuntimeLibrary.iOS/GeneratedCode/BindingMetadataTest.iOS.cs

TYPE_O_MATIC_EXE = $(TOP)/type-o-matic/bin/Debug/type-o-matic.exe
XAMGLUE_IOS = $(TOP)/swiftglue/bin/$(configuration)/iphone/FinalProduct/XamGlue.framework
XAMGLUE_MAC = $(TOP)/swiftglue/bin/$(configuration)/mac/FinalProduct/XamGlue.framework

all: create-pinvokes create-swift-pinvokes create-swift-Test-pinvokes

clean-ios:
	@echo "Cleaning binary files and generated code for iOS"
	@rm -rf $(TOP)/SwiftRuntimeLibrary.iOS/bin/*
	@rm -rf $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode

clean-mac:
	@echo "Cleaning binary files and generated code for Mac"
	@rm -rf $(TOP)/SwiftRuntimeLibrary.Mac/bin/*
	@rm -rf $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode

clean-modules:
	@echo "Cleaning xmls in Modules directory"
	@rm -f ./Modules/*.xml

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

$(TOP)/$(BINDING_SWIFT_METADATA_IOS): $(TYPE_O_MATIC_EXE) $(TOP)/common.mk $(XAMGLUE_IOS)
	@echo "Generating code for iOS..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TYPE_O_MATIC_EXE) --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=iphone --generate=swift $(addprefix --namespace=, $(TYPE_O_MATIC_IOS_NAMESPACES)) --xamglue-framework=$(XAMGLUE_IOS) --output=$@
	@echo "Generated Swift binding metadata files for iOS at $@"

$(TOP)/$(BINDING_SWIFT_METADATA_MAC): $(TYPE_O_MATIC_EXE) $(TOP)/common.mk $(XAMGLUE_MAC)
	@echo "Generating code for MacOS..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.Mac/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TYPE_O_MATIC_EXE) --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=mac --generate=swift $(addprefix --namespace=, $(TYPE_O_MATIC_MAC_NAMESPACES)) --xamglue-framework=$(XAMGLUE_MAC) --output=$@
	@echo "Generated Swift binding metadata files for MacOS at $@"

$(TOP)/$(BINDING_METADATA_Test): $(TYPE_O_MATIC_EXE) $(TOP)/common.mk $(XAMGLUE_IOS)
	@echo "Generating Test Code..."
	@mkdir -p $(TOP)/SwiftRuntimeLibrary.iOS/GeneratedCode
	@/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono $(TYPE_O_MATIC_EXE) --swift-lib-path=$(TOP)/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/ --platform=iphone --generate=csharp $(addprefix --namespace=, $(TYPE_O_MATIC_IOS_NAMESPACES)) --xamglue-framework=$(XAMGLUE_IOS) --output=$@
	@echo "Generated C# binding metadata test files for iOS at $@"

create-pinvokes: $(TOP)/$(BINDING_METADATA_MAC) $(TOP)/$(BINDING_METADATA_IOS)
create-pinvokes-ios: $(TOP)/$(BINDING_METADATA_IOS)
create-pinvokes-mac: $(TOP)/$(BINDING_METADATA_MAC)

create-swift-pinvokes: $(TOP)/$(BINDING_SWIFT_METADATA_IOS) $(TOP)/$(BINDING_SWIFT_METADATA_MAC)
create-swift-pinvokes-ios: $(TOP)/$(BINDING_SWIFT_METADATA_IOS)
create-swift-pinvokes-mac: $(TOP)/$(BINDING_SWIFT_METADATA_MAC)

create-swift-Test-pinvokes: $(TOP)/$(BINDING_METADATA_Test) 

