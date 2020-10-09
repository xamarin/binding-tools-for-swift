TOP=..
include $(TOP)/common.mk


all: generate-swift-bindings build-all

clean:
	rm -rf bin/*
	rm -rf bindingmetadata.*.swift

	

MODULENAME=XamGlue
BINDIR=bin

# this creates a bindings file for type metadata
bindingmetadata.iphone.swift:
	$(Q) $(shell $(TYPE_O_MATIC) --swift-lib-path=$(SYSTEM_SWIFTLIB) --generate=swift --platform=iphone $(addprefix --namespace=, $(TYPE_O_MATIC_IOS_NAMESPACES)) > $@.tmp)
	$(Q) echo "" >> $@.tmp
	$(Q) mv $@.tmp $@
bindingmetadata.macos.swift:
	$(Q) $(shell $(TYPE_O_MATIC) --swift-lib-path=$(SYSTEM_SWIFTLIB) --generate=swift --platform=mac $(addprefix --namespace=, $(TYPE_O_MATIC_MAC_NAMESPACES)) > $@.tmp)
	$(Q) echo "" >> $@.tmp
	$(Q) mv $@.tmp $@

generate-swift-bindings: bindingmetadata.iphone.swift bindingmetadata.macos.swift

CFILES:=$(wildcard *.c)

#
# Define a template that can compile the swift glue code for each architecture
#
# 1: platform
# 2: sdk name
# 3: target triple
# 4: extra args
define SwiftLibraryTemplate
$(3)_ARCH:=$(firstword $(subst -, ,$(3)))
$(3)_SDK:=$$(shell xcrun --sdk $(2) --show-sdk-path)

## debug
$(3)_DEBUG_OUTPUTDIR:=$(BINDIR)/Debug/$(1)/$$($(3)_ARCH)
$(3)_DEBUG_COUTPUTDIR:=$(BINDIR)/Debug/$(1)/$$($(3)_ARCH)/ofiles
$(3)_DEBUG_OBJFILES:=$$(foreach cfile,$(CFILES),$$($(3)_DEBUG_COUTPUTDIR)/$$(cfile:.c=.o))

$$($(3)_DEBUG_COUTPUTDIR)/%.o : %.c
	$$(Q) mkdir -p $$($(3)_DEBUG_COUTPUTDIR)
	$$(call Q_2,CLANG [$(1)/$$($(3)_ARCH)/Debug/$$(@F)]) clang -x c -arch $$($(3)_ARCH) -std=gnu11 -O0 -fasm-blocks $(4) -c $$< -o $$@

$$($(3)_DEBUG_OUTPUTDIR)/$(MODULENAME): Makefile $(wildcard *.swift) $$($(3)_DEBUG_OBJFILES)
	$(Q) mkdir -p $$(dir $$@)
	$$(call Q_2,SWIFTC [$(1)/$$($(3)_ARCH)/Debug])   $(SYSTEM_SWIFTC) -g -sdk $$($(3)_SDK) -target $(3) -emit-module-interface -enable-library-evolution -emit-module -emit-library -Xlinker -rpath -Xlinker /usr/lib/swift -Xlinker -rpath -Xlinker @executable_path/Frameworks -Xlinker -rpath -Xlinker @loader_path/Frameworks -Xlinker -rpath -Xlinker @executable_path -Xlinker -final_output -Xlinker $(MODULENAME) -Xlinker -install_name -Xlinker @rpath/$(MODULENAME).framework/$(MODULENAME) -I . -module-name $(MODULENAME) -o $$@.tmp *.swift $$($(3)_DEBUG_OBJFILES)
#	$$(Q) install_name_tool -delete_rpath $(realpath $(UNPACKAGED_SWIFTLIB)/$(2)) $$@.tmp
	$$(Q) mv $$@.tmp $$@

## release
$(3)_RELEASE_OUTPUTDIR=$(BINDIR)/Release/$(1)/$$($(3)_ARCH)
$(3)_RELEASE_COUTPUTDIR:=$(BINDIR)/Release/$(1)/$$($(3)_ARCH)/ofiles
$(3)_RELEASE_OBJFILES:=$$(foreach cfile,$(CFILES),$$($(3)_DEBUG_COUTPUTDIR)/$$(cfile:.c=.o))
$$($(3)_RELEASE_COUTPUTDIR)/%.o : %.c
	$$(Q) mkdir -p $$($(3)_RELEASE_COUTPUTDIR)
	$$(call Q_2,CLANG [$(1)/$$($(3)_ARCH)/Release/$$(@F)])clang -x c -arch $$($(3)_ARCH) -DCPU_ARCH=$$($(3)_ARCH) -std=gnu11 -Os -fasm-blocks ($4) -c $$< -o $$@

$$($(3)_RELEASE_OUTPUTDIR)/$(MODULENAME): Makefile $(wildcard *.swift) $$($(3)_RELEASE_OBJFILES)
	$(Q) mkdir -p $$(dir $$@)
	$$(call Q_2,SWIFTC [$(1)/$$($(3)_ARCH)/Release]) $(SYSTEM_SWIFTC) -O -sdk $$($(3)_SDK) -target $(3) -emit-module-interface -enable-library-evolution -emit-module -emit-library -Xlinker -rpath -Xlinker /usr/lib/swift -Xlinker -rpath -Xlinker @executable_path/Frameworks -Xlinker -rpath -Xlinker @loader_path/Frameworks -Xlinker -rpath -Xlinker @executable_path -Xlinker -final_output -Xlinker $(MODULENAME) -Xlinker -install_name -Xlinker @rpath/$(MODULENAME).framework/$(MODULENAME) -I . -module-name $(MODULENAME) -o $$@.tmp *.swift $$($(3)_RELEASE_OBJFILES)
#	$$(Q) install_name_tool -delete_rpath $(realpath $(UNPACKAGED_SWIFTLIB)/$(2)) $$@.tmp
	$$(Q) mv $$@.tmp $$@
endef

$(eval $(call SwiftLibraryTemplate,mac,macosx,x86_64-apple-macosx10.9,))
$(eval $(call SwiftLibraryTemplate,iphone,iphoneos,armv7-apple-ios10.3,-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk))
$(eval $(call SwiftLibraryTemplate,iphone,iphoneos,armv7s-apple-ios10.3,-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk))
$(eval $(call SwiftLibraryTemplate,iphone,iphoneos,arm64-apple-ios10.3,-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk))
$(eval $(call SwiftLibraryTemplate,iphone,iphonesimulator,i386-apple-ios10.3-simulator,-mios-simulator-version-min=10.3 -isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneSimulator.platform/Developer/SDKs/iPhoneSimulator.sdk))
$(eval $(call SwiftLibraryTemplate,iphone,iphonesimulator,x86_64-apple-ios10.3-simulator,-mios-simulator-version-min=10.3 -isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneSimulator.platform/Developer/SDKs/iPhoneSimulator.sdk))
$(eval $(call SwiftLibraryTemplate,watch,watchos,armv7k-apple-watchos3.2,-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/WatchOS.platform/Developer/SDKs/WatchOS.sdk))
$(eval $(call SwiftLibraryTemplate,watch,watchsimulator,i386-apple-watchos3.2-simulator,-mwatchos-simulator-version-min=3.2 -isysroot /Applications/Xcode.app/Contents/Developer/Platforms/WatchSimulator.platform/Developer/SDKs/WatchSimulator.sdk))
$(eval $(call SwiftLibraryTemplate,appletv,appletvos,arm64-apple-tvos10.2,-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/AppleTVOS.platform/Developer/SDKs/AppleTVOS.sdk))
$(eval $(call SwiftLibraryTemplate,appletv,appletvsimulator,x86_64-apple-tvos10.2-simulator,-isysroot /Applications/Xcode.app/Contents/Developer/Platforms/AppleTVSimulator.platform/Developer/SDKs/AppleTVSimulator.sdk))

# Define a template that can create a framework for each platform
#
# 1: platform
# 2: configuration
# 3: architectures
define SwiftFrameworkTemplate
$(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/$(MODULENAME): $$(foreach arch,$(3),$(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME))
	$$(Q) mkdir -p $$(dir $$@)/Modules/$(MODULENAME).swiftmodule
	$$(Q) $$(foreach arch,$(3),$(CP) $(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME).swiftdoc    $$(dir $$@)/Modules/$(MODULENAME).swiftmodule/$$(arch).swiftdoc &&) true
	$$(Q) $$(foreach arch,$(3),$(CP) $(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME).swiftinterface    $$(dir $$@)/Modules/$(MODULENAME).swiftmodule/$$(arch).swiftinterface &&) true
	$$(Q) $$(foreach arch,$(3),$(CP) $(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME).swiftsourceinfo    $$(dir $$@)/Modules/$(MODULENAME).swiftmodule/$$(arch).swiftsourceinfo &&) true
	$$(Q) $$(foreach arch,$(3),$(CP) $(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME).swiftmodule $$(dir $$@)/Modules/$(MODULENAME).swiftmodule/$$(arch).swiftmodule &&) true
	$$(call Q_2,LIPO [$(1)/$(2)]) lipo $$^ -create -output $$@.tmp
	$$(Q) mv $$@.tmp $$@

$(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME): $(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/$(MODULENAME)
	$$(Q) $(CP) $$< $$@.tmp
	$$(Q) install_name_tool -id XamGlue $$@.tmp
	$$(Q) mv $$@.tmp $$@

$(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/Info.plist: $(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/$(MODULENAME)
	$$(call Q_2,PLIST [$(1)/$(2)]) $(PLIST_SWIFTY) --lib $$< --output $$@.tmp
	$$(Q) mv $$@.tmp $$@

$(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework.stamp: $(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/Info.plist $(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/$(MODULENAME) $(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME)
	$$(call Q_2,GEN [$(1)/$(2)]) touch $$@

build-$(1):: $(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework.stamp
build-all:: build-$(1)
endef

$(eval $(call SwiftFrameworkTemplate,mac,Debug,x86_64))
$(eval $(call SwiftFrameworkTemplate,mac,Release,x86_64))
$(eval $(call SwiftFrameworkTemplate,iphone,Debug,i386 x86_64 arm64 armv7 armv7s))
$(eval $(call SwiftFrameworkTemplate,iphone,Release,i386 x86_64 arm64 armv7 armv7s))
$(eval $(call SwiftFrameworkTemplate,appletv,Debug,x86_64 arm64))
$(eval $(call SwiftFrameworkTemplate,appletv,Release,x86_64 arm64))
$(eval $(call SwiftFrameworkTemplate,watch,Debug,i386 armv7k))
$(eval $(call SwiftFrameworkTemplate,watch,Release,i386 armv7k))
