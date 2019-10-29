# Default target is build, so it needs to come first.
build:

TOP=../../..

include $(TOP)/common.mk

BINDINGTOOLSFORSWIFT=mono --debug $(TOMSWIFTY)

# Detect if the system Xcode is not the expected one.
ACTUAL_XCODE_VERSION:=$(shell /usr/libexec/PlistBuddy -c "Print :CFBundleShortVersionString" "$(shell xcode-select -p)/../Info.plist")
EXPECTED_XCODE_VERSION:=$(shell grep ^XCODE_VERSION= $(TOP)/Make.config | sed 's/.*=//')
ifneq ($(ACTUAL_XCODE_VERSION),$(EXPECTED_XCODE_VERSION))
ifeq ($(IGNORE_XCODE_VERSION),)
$(error Invalid Xcode version, expected $(EXPECTED_XCODE_VERSION) found $(ACTUAL_XCODE_VERSION))
endif
endif

# Show a warning if the current test is disabled.
ifneq ($(IGNORED),)
ifeq ($(HIDE_IGNORED_WARNING),)
$(warning This test ($(NAME)) will build/run, but the result is ignored because it's a known failure)
endif
endif

XCODEBUILD_VERBOSITY=$(if $(V),,-quiet)
BINDINGTOOLSFORSWIFT_VERBOSITY=$(if $(V),-vvvv)

# Standard targets: build, run
build:
	@echo "Building $(NAME)"
	$(Q) $(if $(IGNORED),-)$(MAKE) build-local HIDE_IGNORED_WARNING=1

run:
	@echo "Running $(NAME)"
	$(Q) $(if $(IGNORED),-)$(MAKE) run-local HIDE_IGNORED_WARNING=1

## Helpful targets

XCODESCHEME?=$(NAME)
FRAMEWORKNAME?=$(XCODESCHEME)
XCODEPROJECT?=repository/$(NAME).xcodeproj

# If anything needs to be done before the GitHub project is cloned, add another
# target, and make this target depend on that other target:
#
# clone-dependencies: preclone
# preclone:
# 	echo "Running tasks before cloning the GitHub project"
#

clone-dependencies:

# Clone target: will clone the repository in the 'repository' subdirectory, and checkout the expected hash.
clone: clone-$(HASH).stamp
clone-$(HASH).stamp: Makefile clone-dependencies
	$(Q) ../clone-repository.sh "$(REPO)" "$(HASH)"
	$(Q) touch $@

# If anything needs to be done before the xcode project is built, add another
# target, and make this target depend on that other target:
#
# build-dependencies: prebuild
# prebuild:
# 	echo "Running tasks before building the Xcode project"
#

build-dependencies:

# This target will build the xcode project using default options.
# The pattern % is the sdk to build the project for.
# The project path can be customized by setting the XCODEPROJECT variable
# The scheme can be customized by setting the XCODEXCHEME variable
build-sdk-%.stamp: Makefile ../Makefile.inc clone-$(HASH).stamp build-dependencies
	$(Q) rm -Rf ./bin/$*
	$(Q_XCODEBUILD) xcodebuild $(XCODEBUILD_VERBOSITY) -project "$(XCODEPROJECT)" -configuration Debug -scheme "$(XCODESCHEME)" -sdk $* -derivedDataPath=$(abspath ./bin/derived-data/$*) ONLY_ACTIVE_ARCH=NO clean build CONFIGURATION_BUILD_DIR=$(abspath ./bin/$*) IPHONEOS_DEPLOYMENT_TARGET=10.3 SYMROOT=$(abspath ./bin/symroot/$*) OBJROOT=$(abspath ./bin/intermediates/$*)
	$(Q) touch $@

# This target will create a fat iOS framework from the device and simulator frameworks.
build-ios-framework.stamp: build-sdk-iphoneos.stamp build-sdk-iphonesimulator.stamp
	$(Q) rm -Rf "bin/$(FRAMEWORKNAME).framework"
	$(Q_FRAMEWORK) ../make-fat-framework.sh --output="$(abspath ./bin/$(FRAMEWORKNAME).framework)" --input-device="$(abspath ./bin/iphoneos/$(FRAMEWORKNAME).framework)" --input-simulator="$(abspath ./bin/iphonesimulator/$(FRAMEWORKNAME).framework)"
	$(Q) touch $@

run-binding-tools-for-swift: build-ios-framework.stamp
	$(Q_SOM) $(BINDINGTOOLSFORSWIFT $(BINDINGTOOLSFORSWIFT_VERBOSITY) --swift-bin-path $(SWIFTBIN) --swift-lib-path $(SWIFTLIB) --retain-xml-reflection --retain-swift-wrappers --type-database-path=$(abspath $(SWIFTBINDINGS)) -o $(abspath bin/tsout) -C $(abspath "bin/$(FRAMEWORKNAME).framework") -C $(SWIFTGLUEPREFIX)/iphone/$(SWIFTGLUESUFFIX) -module-name "$(FRAMEWORKNAME)"

build-swift: build-ios-framework.stamp

default-build: run-binding-tools-for-swift

default-run:
	@echo "Not implemented yet"

build-save-failed-log:
	sed -i '' 's/^# IGNORED=1$$/IGNORED=1/' Makefile
	$(MAKE) -C .. $(shell basename $(CURDIR)) build-$(shell basename $(CURDIR)) -j8 BUILD_LOG_NAME=failed-build.log
	git add failed-build.log Makefile

# This target will list all schemes in any xcode project found in any subdirectory of the current directory.
list-schemes:
	@for i in $$(find . -name '*.xcodeproj'); do echo "Listing schemes in $$i"; xcodebuild -list -project "$$i"; done
