#!/bin/bash -e
if test -z "$BTFS_TOP"; then
    echo "Variable BTFS_TOP is missing."
    exit 1
fi

cd $BTFS_TOP
#WORKSPACE=$(pwd)

make -C tests/tom-swifty-test
make -C tests/tom-swifty-test run-runtime-library-tests
# Offline until swift 5 smoke clears
#make -C tests/3rd-party

unset SOM_PATH
