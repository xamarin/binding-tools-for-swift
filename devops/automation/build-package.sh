#!/bin/bash -ex

# env var should have been defined by the CI
if test -z "$BTFS_TOP"; then
    echo "Variable BTFS_TOP is missing."
    exit 1
fi

cd $BTFS_TOP
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
