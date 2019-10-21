#!/bin/sh -e

xcrun nm $1 | xcrun swift-demangle
