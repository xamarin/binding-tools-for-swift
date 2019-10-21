TOP=../../..

# Define values specific to this test / repository
REPO=https://github.com/Ramotion/Preview-Transition
NAME:=$(shell basename $(REPO))
# Modify the hash if the latest won't compile (for instance if it requires Swift 4.2 / Xcode 10)
HASH=74f93c36d54067dd44f6f419fea855945ace05b0

# The following variables can be used to customize the build:
#
# The path to the Xcode project (unless the default 'repository/$NAME.xcodeproj' isn't right)
XCODEPROJECT=repository/PreviewTransitionDemo/PreviewTransitionDemo.xcodeproj
#
# The Xcode project's scheme to build (unless the default '$NAME' isn't right).
# You can execute 'make list-schemes' to list all schemes in any xcode projects any subdirectory.
XCODESCHEME=PreviewTransition
#
# The name of the resulting framework (unless it doesn't match the Xcode scheme).
# FRAMEWORKNAME=name
#
# If the project fails to build/bind, set IGNORED=1
# IGNORED=1
#

# These variables must be set before including Makefile.inc
#

include ../Makefile.inc

# Standard targets: build, run
# If the project does not need customization, we can forward to the default implementation of these targets.
build-local: default-build
run-local: default-run
