TOP=..
include $(TOP)/common.mk
MAKE_FRAMEWORK=$(TOP)/tools/make-framework
ALL_FILES=--swift-files *.swift --c-files *.c


all: generate-swift-bindings build-all

clean:
	rm -rf bin/*
	rm -rf bindingmetadata.*.swift

build-all-mac-debug: *.c *.swift
	$(Q) rm -rf bin/Debug/mac/
	$(call Q_2,MAKE-FRAMEWORK [macOSX/Debug]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os macosx \
		--device-archs x86_64 --output-path bin/Debug/mac/FinalProduct \
		--module-name XamGlue --minimum-os-version 10.9 \
		--extra-swift-args -g --extra-c-args -g
build-all-mac-release: *.c *.swift
	$(Q) rm -rf bin/Release/mac/
	$(call Q_2,MAKE-FRAMEWORK [macOSX/Release]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os macosx \
		--device-archs x86_64 --output-path bin/Release/mac/FinalProduct \
		--module-name XamGlue --minimum-os-version 10.9 \
		--extra-swift-args -O --extra-c-args -O

build-all-iphone-debug: *.c *.swift
	$(Q) rm -rf bin/Debug/iphone/
	$(call Q_2,MAKE-FRAMEWORK [iOS/Debug]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os ios \
		--output-path bin/Debug/iphone/FinalProduct \
		--device-archs arm64 armv7 armv7s --simulator-archs i386 x86_64 \
		--module-name XamGlue --minimum-os-version 10.2 \
		--extra-swift-args -g --extra-c-args -g \
		--make-xcframework
build-all-iphone-release: *.c *.swift
	$(Q) rm -rf bin/Release/iphone/
	$(call Q_2,MAKE-FRAMEWORK [iOS/Release]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os ios \
		--output-path bin/Release/iphone/FinalProduct \
		--device-archs arm64 armv7 armv7s --simulator-archs i386 x86_64 \
		--module-name XamGlue --minimum-os-version 10.2 \
		--extra-swift-args -O --extra-c-args -O \
		--make-xcframework

build-all-watch-debug: *.c *.swift
	$(Q) rm -rf bin/Debug/watch/
	$(call Q_2,MAKE-FRAMEWORK [WatchOS/Debug]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os watchos \
		--output-path bin/Debug/watch/FinalProduct \
		--device-archs armv7k --simulator-archs i386 \
		--module-name XamGlue --minimum-os-version 3.2 \
		--extra-swift-args -g --extra-c-args -g \
		--make-xcframework
build-all-watch-release: *.c *.swift
	$(Q) rm -rf bin/Release/watch/
	$(call Q_2,MAKE-FRAMEWORK [WatchOS/Release]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os watchos \
		--output-path bin/Release/watch/FinalProduct \
		--device-archs armv7k --simulator-archs i386 \
		--module-name XamGlue --minimum-os-version 3.2 \
		--extra-swift-args -O --extra-c-args -O \
		--make-xcframework

build-all-appletv-debug: *.c *.swift
	$(Q) rm -rf bin/Debug/appletv/
	$(call Q_2,MAKE-FRAMEWORK [AppleTV/Debug]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os tvos \
		--output-path bin/Debug/appletv/FinalProduct \
		--device-archs arm64 --simulator-archs x86_64 \
		--module-name XamGlue --minimum-os-version 10.2 \
		--extra-swift-args -g --extra-c-args -g \
		--make-xcframework
build-all-appletv-release: *.c *.swift
	$(Q) rm -rf bin/Release/appletv/
	$(call Q_2,MAKE-FRAMEWORK [AppleTV/Release]) $(MAKE_FRAMEWORK) $(ALL_FILES) \
		--target-os tvos \
		--output-path bin/Release/appletv/FinalProduct \
		--device-archs arm64 --simulator-archs x86_64 \
		--module-name XamGlue --minimum-os-version 10.2 \
		--extra-swift-args -O --extra-c-args -O \
		--make-xcframework


build-all: build-all-mac-debug build-all-mac-release build-all-iphone-debug \
	build-all-iphone-release build-all-watch-debug build-all-watch-release \
	build-all-appletv-debug build-all-appletv-release

	

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
