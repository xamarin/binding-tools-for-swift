TOP=..
include $(TOP)/common.mk


all: generate-swift-bindings build-all

clean:
	rm -rf bin/*
	rm -rf bindingmetadata.*.swift

MODULENAME=XamGlue
BINDIR=bin
MAC_NAMESPACES= \
	Foundation \

IOS_NAMESPACES= \
	Accelerate \
	Accounts \
	AddressBook \
	AddressBookUI \
	AdSupport \
	ARKit \
	AssetsLibrary \
	AudioToolbox \
	AudioUnit \
	AuthenticationServices \
	AVFoundation \
	AVKit \
	BusinessChat \
	CallKit \
	CarPlay \
	ClassKit \
	CloudKit \
	Compression \
	Contacts \
	ContactsUI \
	CoreGraphics \
	DeviceCheck \
	EventKit \
	EventKitUI \
	ExternalAccessory \
	FileProvider \
	FileProviderUI \
	Foundation \
	GameController \
	GameKit \
	GameplayKit \
	GLKit \
	HealthKit \
	HealthKitUI \
	HomeKit \
	iAd \
	IdentityLookup \
	IdentityLookupUI \
	ImageIO \
	Intents \
	IntentsUI \
	IOSurface \
	JavaScriptCore \
	LocalAuthentication \
	MapKit \
	MediaAccessibility \
	MediaPlayer \
	MediaToolbox \
	Messages \
	MessageUI \
	Metal \
	MetalKit \
	MetalPerformanceShaders \
	MobileCoreServices \
	ModelIO \
	MultipeerConnectivity \
	NaturalLanguage \
	Network \
	NetworkExtension \
	NewsstandKit \
	NotificationCenter \
	PassKit \
	PdfKit \
	PushKit \
	Photos \
	PhotosUI \
	QuickLook \
	ReplayKit \
	SafariServices \
	SceneKit \
	Security \
	Social \
	Speech \
	SpriteKit \
	StoreKit \
	SystemConfiguration \
	Twitter \
	UIKit \
	UserNotifications \
	UserNotificationsUI \
	VideoToolbox \
	Vision \
	WatchConnectivity \
	WatchKit \
	WebKit \

# this creates a bindings file for type metadata
bindingmetadata.iphone.swift:
	$(Q) $(shell $(TYPE_O_MATIC) --swift-lib-path=$(UNPACKAGED_SWIFTLIB) --platform=iphone $(addprefix --namespace=, $(IOS_NAMESPACES)) > $@.tmp)
	$(Q) echo "" >> $@.tmp
	$(Q) mv $@.tmp $@
bindingmetadata.macos.swift:
	$(Q) $(shell $(TYPE_O_MATIC) --swift-lib-path=$(UNPACKAGED_SWIFTLIB) --platform=mac $(addprefix --namespace=, $(MAC_NAMESPACES)) > $@.tmp)
	$(Q) echo "" >> $@.tmp
	$(Q) mv $@.tmp $@

generate-swift-bindings: bindingmetadata.iphone.swift bindingmetadata.macos.swift

#
# Define a template that can compile the swift glue code for each architecture
#
# 1: platform
# 2: sdk name
# 3: target triple
define SwiftLibraryTemplate
$(3)_ARCH:=$(firstword $(subst -, ,$(3)))
$(3)_SDK:=$$(shell xcrun --sdk $(2) --show-sdk-path)

## debug
$(3)_DEBUG_OUTPUTDIR:=$(BINDIR)/Debug/$(1)/$$($(3)_ARCH)
$$($(3)_DEBUG_OUTPUTDIR)/$(MODULENAME): Makefile $(wildcard *.swift)
	$(Q) mkdir -p $$(dir $$@)
	$$(call Q_2,SWIFTC [$(1)/$$($(3)_ARCH)/Debug])   $(UNPACKAGED_SWIFTC) -g -sdk $$($(3)_SDK) -target $(3) -emit-module -emit-library -Xlinker -rpath -Xlinker /usr/lib/swift -Xlinker -rpath -Xlinker @executable_path/Frameworks -Xlinker -rpath -Xlinker @loader_path/Frameworks -Xlinker -rpath -Xlinker @executable_path -Xlinker -final_output -Xlinker $(MODULENAME) -Xlinker -install_name -Xlinker @rpath/$(MODULENAME).framework/$(MODULENAME) -module-name $(MODULENAME) -o $$@.tmp *.swift
	$$(Q) install_name_tool -delete_rpath $(realpath $(UNPACKAGED_SWIFTLIB)/$(2)) $$@.tmp
	$$(Q) mv $$@.tmp $$@

## release
$(3)_RELEASE_OUTPUTDIR=$(BINDIR)/Release/$(1)/$$($(3)_ARCH)
$$($(3)_RELEASE_OUTPUTDIR)/$(MODULENAME): Makefile $(wildcard *.swift)
	$(Q) mkdir -p $$(dir $$@)
	$$(call Q_2,SWIFTC [$(1)/$$($(3)_ARCH)/Release]) $(UNPACKAGED_SWIFTC) -O -sdk $$($(3)_SDK) -target $(3) -emit-module -emit-library -Xlinker -rpath -Xlinker /usr/lib/swift -Xlinker -rpath -Xlinker @executable_path/Frameworks -Xlinker -rpath -Xlinker @loader_path/Frameworks -Xlinker -rpath -Xlinker @executable_path -Xlinker -final_output -Xlinker $(MODULENAME) -Xlinker -install_name -Xlinker @rpath/$(MODULENAME).framework/$(MODULENAME) -module-name $(MODULENAME) -o $$@.tmp *.swift
	$$(Q) install_name_tool -delete_rpath $(realpath $(UNPACKAGED_SWIFTLIB)/$(2)) $$@.tmp
	$$(Q) mv $$@.tmp $$@
endef

$(eval $(call SwiftLibraryTemplate,mac,macosx,x86_64-apple-macosx10.9))
$(eval $(call SwiftLibraryTemplate,iphone,iphoneos,armv7-apple-ios10.3))
$(eval $(call SwiftLibraryTemplate,iphone,iphoneos,armv7s-apple-ios10.3))
$(eval $(call SwiftLibraryTemplate,iphone,iphoneos,arm64-apple-ios10.3))
$(eval $(call SwiftLibraryTemplate,iphone,iphonesimulator,i386-apple-ios10.3))
$(eval $(call SwiftLibraryTemplate,iphone,iphonesimulator,x86_64-apple-ios10.3))
$(eval $(call SwiftLibraryTemplate,watch,watchos,armv7k-apple-watchos3.2))
$(eval $(call SwiftLibraryTemplate,watch,watchsimulator,i386-apple-watchos3.2))
$(eval $(call SwiftLibraryTemplate,appletv,appletvos,arm64-apple-tvos10.2))
$(eval $(call SwiftLibraryTemplate,appletv,appletvsimulator,x86_64-apple-tvos10.2))

# Define a template that can create a framework for each platform
#
# 1: platform
# 2: configuration
# 3: architectures
define SwiftFrameworkTemplate
$(BINDIR)/$(2)/$(1)/FinalProduct/$(MODULENAME).framework/$(MODULENAME): $$(foreach arch,$(3),$(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME))
	$$(Q) mkdir -p $$(dir $$@)/Modules/$(MODULENAME).swiftmodule
	$$(Q) $$(foreach arch,$(3),$(CP) $(BINDIR)/$(2)/$(1)/$$(arch)/$(MODULENAME).swiftdoc    $$(dir $$@)/Modules/$(MODULENAME).swiftmodule/$$(arch).swiftdoc &&) true
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
