TOP=../../..

# Define values specific to this test / repository
REPO=https://github.com/hyperoslo/Presentation
NAME:=$(shell basename $(REPO))
# Modify the hash if the latest won't compile (for instance if it requires Swift 4.2 / Xcode 10)
HASH=64ec703332fc1b85fcfd00985a6dab9fde24fc8f

# The following variables can be used to customize the build:
#
# The path to the Xcode project (unless the default 'repository/$NAME.xcodeproj' isn't right)
# XCODEPROJECT=/path/to/xcodeproject
#
# The Xcode project's scheme to build (unless the default '$NAME' isn't right).
# You can execute 'make list-schemes' to list all schemes in any xcode projects any subdirectory.
XCODESCHEME=Presentation-iOS
#
# The name of the resulting framework (unless it doesn't match the Xcode scheme).
FRAMEWORKNAME=Presentation
#
# If the project fails to build/bind, set IGNORED=1
IGNORED=1
# Changing IGNORED? Consider updating the README.txt file and any Git issues
# referenced therein.

# These variables must be set before including Makefile.inc
#

include ../Makefile.inc

# Standard targets: build, run
# If the project does not need customization, we can forward to the default implementation of these targets.
build-local: default-build
run-local: default-run

build-dependencies: prebuild.stamp
prebuild.stamp:
	test -f /usr/local/bin/carthage || brew install carthage || true
	cd repository && carthage bootstrap --platform iOS
	$(Q) touch $@
