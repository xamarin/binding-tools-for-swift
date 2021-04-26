#!/bin/bash -ex

# env var should have been defined by the CI
if test -z "$BTFS_TOP"; then
    echo "Variable BTFS_TOP is missing."
    exit 1
fi

cd $BTFS_TOP
WORKSPACE=$(pwd)

# make the login keychain the default
security list-keychain -d user -s login.keychain
security default-keychain -d user -s login.keychain
if test -n "${LOGIN_KEYCHAIN_PASSWORD:-}"; then
	security unlock-keychain -p "$LOGIN_KEYCHAIN_PASSWORD" login.keychain
fi
security set-keychain-settings -lut 21600 login.keychain

# Verify dependencies and install if necessary
$WORKSPACE/devops/automation/system-dependencies.sh --provision-all

xcode-select -p
ls -la /Applications
