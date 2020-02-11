#!/bin/bash -ex

cd "$(dirname "${BASH_SOURCE[0]}")/.."
WORKSPACE=$(pwd)

nuget restore tom-swifty.sln
cd "$WORKSPACE/plist-swifty"
msbuild
cd "$WORKSPACE/type-o-matic"
msbuild
cd "$WORKSPACE/swiftglue"
#make generate-swift-bindings
make all -j8
nm "$WORKSPACE/swiftglue/bin/Debug/mac/FinalProduct/XamGlue.framework/XamGlue" | grep s7XamGlue12getSwiftType3str6resultSbSpySSG_SpyypXpGtF

cd "$WORKSPACE"
msbuild tom-swifty.sln
