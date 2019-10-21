#!/bin/bash -ex

cd "$(dirname "${BASH_SOURCE[0]}")/.."
WORKSPACE=$(pwd)

# Use Pack-Man to produce zip package
# and custom directory of Binding Tools for Swift build.
cd "$WORKSPACE/Pack-Man"
# Remove any existing zips
rm -f -- *.zip
./build.sh

FILENAME=binding-tools-for-swift-v$(make -C .. print-variable VARIABLE=SOM_PACKAGE_VERSION)
mv "binding-tools-for-swift.zip" "$FILENAME.zip"
echo "Created Pack-Man/$FILENAME.zip"

PACKAGE_DIR=$WORKSPACE/../package
rm -Rf "$PACKAGE_DIR"
mkdir -p "$PACKAGE_DIR"
cp -- *.zip "$PACKAGE_DIR"
