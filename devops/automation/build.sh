#!/bin/bash -ex

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
WORKSPACE=$(pwd)

nuget restore tom-swifty.sln
cd "$WORKSPACE/plist-swifty"
msbuild
cd "$WORKSPACE/type-o-matic"
msbuild
cd "$WORKSPACE/swiftglue"
make generate-swift-bindings
make all -j8

cd "$WORKSPACE"
msbuild tom-swifty.sln
