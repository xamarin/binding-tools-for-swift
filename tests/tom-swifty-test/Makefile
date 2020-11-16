TOP=../..
include $(TOP)/common.mk

all: build run-tests
	@# This needs to run in a subprocess, because the existing make process
	@# has already calculated required variables that contain output from
	@# the 'run-tests' target, which hadn't executed when the variables were
	@# evaluated.
	$(MAKE) -j8 build-device-tests

check: run-tests


# new (incompatible but much more powerful) runner syntax means using FIXTURES has to look like
# FIXTURES="--where=test=SwiftReflector.LinkageTests.TestMissingNSObject" make
# https://github.com/nunit/docs/wiki/Test-Selection-Language
run-tests: bin/Debug/tom-swifty-test.dll
	rm -f .failed-stamp
	rm -Rf bin/devicetests # remove any existing device test output
	DYLD_LIBRARY_PATH=$(DYLD_LIBRARY_PATH):$(SWIFTLIB)/macosx:$(SWIFTGLUEPREFIX)mac$(SWIFTGLUESUFFIX) \
	mono --debug --runtime=v4.0 $(TOP)/packages/NUnit.ConsoleRunner.3.8.0/tools/nunit3-console.exe bin/Debug/tom-swifty-test.dll --workers=4 --framework=mono-4.0 --nocolor --labels=All --shadowcopy $(FIXTURES) || touch .failed-stamp
	@# Create an html file and tell MonkeyWrench to upload it (if we're running there)
	@[[ -z "$$BUILD_REPOSITORY" ]] || \
		( xsltproc ../HtmlTransform.xslt TestResult.xml  > index.html && \
		echo "@MonkeyWrench: AddFile: $$PWD/index.html")
	@[[ ! -e .failed-stamp ]] 

build: bin/Debug/tom-swifty-test.dll

run-runtime-library-tests: bin/Debug/tom-swifty-test.dll
	rm -f .$@-failed-stamp
	LEAKTEST_DYLD_LIBRARY_PATH=$(DYLD_LIBRARY_PATH):$(SWIFTLIB)/macosx:$(SWIFTGLUEPREFIX)mac$(SWIFTGLUESUFFIX) \
	$(TOP)/leaktest/bin/Debug/leaktest mono --debug --runtime=v4.0 $(TOP)/packages/NUnit.ConsoleRunner.3.8.0/tools/nunit3-console.exe bin/Debug/tom-swifty-test.dll --workers=1 --framework=mono-4.0 --nocolor --labels=All --shadowcopy --where=namespace=SwiftRuntimeLibraryTests --inprocess || touch .failed-stamp

bin/Debug/tom-swifty-test.dll: $(shell git ls-files . | grep '[.]cs$$') tom-swifty-test.csproj SwiftRuntimeLibrary.Mac.dll.config
	nuget restore ../..
	msbuild

SwiftRuntimeLibrary.Mac.dll.config: SwiftRuntimeLibrary.Mac.dll.config.in Makefile
	$(Q) sed -e 's|@SWIFTLIB@|$(SWIFTLIB)|' -e 's|@SWIFTGLUEPREFIX@|$(SWIFTGLUEPREFIX)|' -e 's|@SWIFTGLUESUFFIX@|$(SWIFTGLUESUFFIX)|' $< > $@.tmp
	$(Q) mv $@.tmp $@

dependencies: build

clean:
	@rm -rf bin obj TestResult.xml devicetests/*

# Compute the list of test modules
MODULES:=$(shell ls -d devicetests/*/swiftsrc/*[!_]?????.swift 2>/dev/null | sed -e 's_devicetests/__' -e 's_/swiftsrc__' -e 's_/.*swift__'  | uniq)

#
# Define a template that can compile the swift code for the device tests
#
# 1: platform
# 2: arch
# 3: target triple
# 4: module
# 5: sdk name
define SwiftModuleTemplate
bin/devicetests/$(1)/$(2)/$(4): $$(wildcard devicetests/$(4)/swiftsrc/*.swift)
	$$(Q) mkdir -p $$(dir $$@)
	$$(call Q_2,SWIFTC [$(1)/$(2)]) $(SWIFTC) \
		-emit-module \
		-emit-library \
		-g \
		-sdk $$(shell xcrun --sdk "$(5)" --show-sdk-path) \
		-target $(3) \
		-module-name $(4) \
		-o $$@.tmp \
		-Xlinker -final_output -Xlinker $(4) \
		-Xlinker -install_name -Xlinker @rpath/$(4).framework/$(4) \
		-Xlinker -rpath -Xlinker @executable_path/Frameworks \
		-Xlinker -rpath -Xlinker @loader_path/Frameworks \
		$$^
	$$(Q) mv $$@.tmp $$@
endef

$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,iphone,x86_64,x86_64-apple-ios10.3,$(module),iphonesimulator)))
$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,iphone,arm64,arm64-apple-ios10.3,$(module),iphoneos)))
$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,mac,x86_64,x86_64-apple-macosx10.9,$(module),macosx)))
$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,tvos,x86_64,x86_64-apple-tvos10.2,$(module),appletvsimulator)))
$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,tvos,arm64,arm64-apple-tvos10.2,$(module),appletvos)))
$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,watchos,i386,i386-apple-watchos3.2,$(module),watchsimulator)))
$(foreach module,$(MODULES),$(eval $(call SwiftModuleTemplate,watchos,armv7k,armv7k-apple-watchos3.2,$(module),watchos)))

#
# Define a template that can create a framework for each device test module.
#
# 1: platform
# 2: module
# 3: architectures
define SwiftFrameworkTemplate
bin/devicetests/$(1)/$(2).framework/$(2): $$(foreach arch,$(3),bin/devicetests/$(1)/$$(arch)/$(2))
	$$(Q) mkdir -p bin/devicetests/$(1)/$(2).framework/Modules/$(2).swiftmodule
	$$(Q) $$(foreach arch,$(3),$(CP) bin/devicetests/$(1)/$$(arch)/$(2).swiftdoc bin/devicetests/$(1)/$(2).framework/Modules/$(2).swiftmodule/$$(arch).swiftdoc &&) true
	$$(Q) $$(foreach arch,$(3),$(CP) bin/devicetests/$(1)/$$(arch)/$(2).swiftmodule bin/devicetests/$(1)/$(2).framework/Modules/$(2).swiftmodule/$$(arch).swiftmodule &&) true
	$$(call Q_2,LIPO [$(1)]) lipo $$^ -create -output $$@.tmp
	$$(Q) mv $$@.tmp $$@

bin/devicetests/$(1)/$(2).framework/Info.plist: bin/devicetests/$(1)/$(2).framework/$(2)
	$$(call Q_2,PLIST [$(1)]) $(PLIST_SWIFTY) --lib $$< --output $$@.tmp
	$$(Q) mv $$@.tmp $$@

bin/devicetests/$(1)/$(2).framework.stamp: bin/devicetests/$(1)/$(2).framework/Info.plist bin/devicetests/$(1)/$(2).framework/$(2)
	$$(Q_GEN) touch $$@

bin/devicetests/$(1)/$(2).tom-swifty.stamp: bin/devicetests/$(1)/$(2).framework/$(2)
	$$(call Q_2,SOM   [$(1)]) $$(TOM_SWIFTY) \
		--swift-bin-path="$$(SWIFTBIN)" \
		--swift-lib-path="$$(SWIFTLIB)" \
		--retain-xml-reflection \
		--type-database-path="$$(SWIFTBINDINGS)" \
		--retain-swift-wrappers \
		--wrapping-module-name=$(2)Wrapping \
		-C "$$(SWIFTGLUEPREFIX)$(1)$$(SWIFTGLUESUFFIX)" \
		-C "$$(abspath bin/devicetests/$(1)/$(2).framework)" \
		-o "$$(abspath bin/devicetests/$(1)/$(2)/tsout)" \
		--module-name="$(2)"
	$$(Q) touch $$@

build-$(1):: bin/devicetests/$(1)/$(2).tom-swifty.stamp bin/devicetests/$(1)/$(2).framework.stamp bin/devicetests/$(1)/$(2).framework/Info.plist
endef

$(foreach module,$(MODULES),$(eval $(call SwiftFrameworkTemplate,iphone,$(module),x86_64 arm64)))
$(foreach module,$(MODULES),$(eval $(call SwiftFrameworkTemplate,mac,$(module),x86_64)))
$(foreach module,$(MODULES),$(eval $(call SwiftFrameworkTemplate,tvos,$(module),x86_64 arm64)))
$(foreach module,$(MODULES),$(eval $(call SwiftFrameworkTemplate,watchos,$(module),i386 armv7k)))

tomswiftydevicetests/%/tomswiftydevicetests.csproj: tomswiftydevicetests/%/tomswiftydeviceteststemplate.csproj build-iphone
	$(Q) devicetestsbin/generatecsproj "$*" "$(abspath $(SWIFTLIB))" "$(abspath $(CURDIR)/../../swift-copy-libs/swift-copy-libs)"

csproj-iphone: tomswiftydevicetests/iphone/tomswiftydevicetests.csproj

all-ios: build-iphone csproj-iphone

build-device-tests: all-ios tomswiftydevicetests.zip

tomswiftydevicetests/iphone/bin/iPhone/Debug/tomswiftydevicetests.app: build-iphone csproj-iphone $(wildcard tomswiftydevicetests/*/*.cs)
	msbuild tomswiftydevicetests/iphone/tomswiftydevicetests.csproj /p:Platform=iPhone

run-device-tests: tomswiftydevicetests/iphone/bin/iPhone/Debug/tomswiftydevicetests.app
	./tomswiftydevicetests/runtests.sh

tomswiftydevicetests.zip: build-iphone csproj-iphone tomswiftydevicetests/iphone/bin/iPhone/Debug/tomswiftydevicetests.app
	$(Q_GEN) rm -f $@
	$(Q) cd tomswiftydevicetests && zip -9r $(abspath $@) iphone/bin/iPhone/Debug/tomswiftydevicetests.app runtests.sh find-device.csharp

print-modules:
	@echo "Modules: $(MODULES)"
