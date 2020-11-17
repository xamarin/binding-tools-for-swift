#!/bin/bash -e

cd "$(dirname "${BASH_SOURCE[0]}")/.."
#WORKSPACE=$(pwd)

make -C tests/tom-swifty-test
make -C tests/tom-swifty-test run-runtime-library-tests
# Offline until swift 5 smoke clears
#make -C tests/3rd-party

unset SOM_PATH
