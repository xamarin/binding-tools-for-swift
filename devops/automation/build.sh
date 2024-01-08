#!/bin/bash -ex

if test -z "$BTFS_TOP"; then
    echo "Variable BTFS_TOP is missing."
    exit 1
fi

cd $BTFS_TOP
WORKSPACE=$(pwd)

dotnet restore tom-swifty.sln --packages packages
cd "$WORKSPACE/plist-swifty"
dotnet build
cd "$WORKSPACE/type-o-matic"
dotnet build
cd "$WORKSPACE/swiftglue"
make generate-swift-bindings
make all -j8

cd "$WORKSPACE"
dotnet build tom-swifty.sln
