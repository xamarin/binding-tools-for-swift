#!/bin/bash

# Builds the simulator version
xcodebuild -quiet -project EulerPhone/EulerPhone.xcodeproj -configuration Debug -target EulerPhone -sdk iphonesimulator ONLY_ACTIVE_ARCH=NO clean build

# Builds the iphone version
xcodebuild -quiet -project EulerPhone/EulerPhone.xcodeproj -configuration Debug -target EulerPhone -sdk iphoneos ONLY_ACTIVE_ARCH=NO build

UNIVERSAL=EulerPhone/build/Debug-universal
rm -rf $UNIVERSAL
mkdir -p $UNIVERSAL

# Use iphone build as an output template
cp -R $UNIVERSAL/../Debug-iphoneos/EulerPhone.framework $UNIVERSAL

# Merge the binaries
lipo -create -output $UNIVERSAL/EulerPhone.framework/EulerPhone $UNIVERSAL/../Debug-iphonesimulator/EulerPhone.framework/EulerPhone $UNIVERSAL/../Debug-iphoneos/EulerPhone.framework/EulerPhone

# Copy the simulator .swiftmodule files to the output
cp $UNIVERSAL/../Debug-iphonesimulator/EulerPhone.framework/Modules/EulerPhone.swiftmodule/* $UNIVERSAL/EulerPhone.framework/Modules/EulerPhone.swiftmodule

# stage the output framework for running Binding Tools for Swift
cp -r $UNIVERSAL/EulerPhone.framework .
