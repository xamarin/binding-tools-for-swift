TOP=../../..

# Define values specific to this test / repository
REPO=https://github.com/SwiftKickMobile/SwiftMessages
NAME:=$(shell basename $(REPO))
# master requires Xcode 10 / Swift 4.2
HASH=6b348edcc4b1a167f2961f3f921d274792272c77

# The following variables can be used to customize the build:
#
# The path to the Xcode project (unless the default 'repository/$NAME.xcodeproj' isn't right)
# XCODEPROJECT=/path/to/xcodeproject
#
# The Xcode project's scheme to build (unless the default '$NAME' isn't right).
# You can execute 'make list-schemes' to list all schemes in any xcode projects any subdirectory.
# XCODESCHEME=scheme
#
# The name of the resulting framework (unless it doesn't match the Xcode scheme).
# FRAMEWORKNAME=name
#
# If the project fails to build/bind, set IGNORED=1
# IGNORED=1
# Changing IGNORED? Consider updating the README.txt file and any Git issues
# referenced therein.

# These variables must be set before including Makefile.inc
#

include ../Makefile.inc

# Standard targets: build, run
# If the project does not need customization, we can forward to the default implementation of these targets.
build-local: default-build
run-local: default-run
