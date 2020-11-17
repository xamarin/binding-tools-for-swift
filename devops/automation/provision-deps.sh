#!/bin/bash -ex

cd "$(dirname "${BASH_SOURCE[0]}")/.."
#WORKSPACE=$(pwd)

# make the login keychain the default
security list-keychain -d user -s login.keychain
security default-keychain -d user -s login.keychain
if test -n "${LOGIN_KEYCHAIN_PASSWORD:-}"; then
	security unlock-keychain -p "$LOGIN_KEYCHAIN_PASSWORD" login.keychain
fi
security set-keychain-settings -lut 21600 login.keychain

# Verify dependencies and install if necessary
./devops/automation/system-dependencies.sh --provision-all

xcode-select -p
ls -la /Applications
