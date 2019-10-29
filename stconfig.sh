#!/bin/bash -e

# stconfig.sh
# Author:
#   Alex Soto <alexsoto@microsoft.com>
#
# Swift Toolchain Config vars
# If SOM_PATH is set it means we will use a custom build
# of the swift toolchain like the one produced by Pack-Man.

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

if [[ ! -z "${SOM_PATH}" ]] ; then
    SWIFTBIN="${SOM_PATH}/bin/swift/bin"
    SWIFTLIB="${SOM_PATH}/bin/swift/lib/swift"
    SWIFTC="${SOM_PATH}/bin/swift/bin/swiftc"
    SWIFTGLUEPREFIX="${SOM_PATH}/lib/SwiftInterop/"
    SWIFTGLUESUFFIX="/XamGlue.framework"
    SWIFTBINDINGS="${SOM_PATH}/bindings"
    TOMSWIFTY="${SOM_PATH}/lib/binding-tools-for-swift/tom-swifty.exe"
else
    SWIFTBIN="$SCRIPT_DIR/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin"
    SWIFTLIB="$SCRIPT_DIR/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift"
    SWIFTC="$SCRIPT_DIR/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin/swiftc"
    SWIFTGLUE="${PLATFORM}"
    SWIFTGLUEPREFIX="$SCRIPT_DIR/swiftglue/bin/Debug/"
    SWIFTGLUESUFFIX="/FinalProduct/XamGlue.framework"
    SWIFTBINDINGS="$SCRIPT_DIR/bindings"
    TOMSWIFTY="$SCRIPT_DIR/tom-swifty/bin/Debug/tom-swifty.exe"
fi

# Returns the absolute path of the input path.
# The returned path may contain symlinks.
abspath ()
{
    if test -z "$1"; then
        echo "abspath: Usage: abspath <path>"
        exit 1
    elif test -n "${2:-}"; then
        echo "abspath: Only one argument, got at least two: $*"
        exit 1
    fi

    if [ "$1" == "/" ]; then
        echo "/"
        exit 0
    fi

    local input
    input="$1"

    if [ "${input:0:1}" != "/" ]; then
        # relative path, make it absolute
        input=$(pwd)/$input
    fi

    local trailingslash
    if [ "${input:-1}" == "/" ]; then
        trailingslash="/"
    fi

    # split the input path into its components
    IFS='/' read -r -a elements <<< "$input"

    local rebuild=1
    while [ "$rebuild" == "1" ]; do
        rebuild=
        # remove any empty entries (can happen if there are multiple path separators simultaneously, such as /foo//bar)
        # this will also remove trailing slashes.
        local elements2
        for i in "${!elements[@]}"; do
            elements2+=( "${elements[i]}" )
        done
        elements=("${elements2[@]}")
        unset elements2

        arraylength=${#elements[@]}
        for (( i=1; i<arraylength+1; i++ )); do
            # resolve . (remove current path component) and .. (remove current and previous (if it exists) path components).
            if [ "${elements[$i]:-}" == "." ]; then
                unset "elements[$i]"
            elif [ "${elements[$i]:-}" == ".." ]; then
                unset "elements[$i]"
                if [ "$i" -gt "1" ]; then
                    # remove previous component as well
                    unset "elements[$((i-1))]"
                fi
                rebuild=1
                break
            fi
        done
    done

    # create the return value
    local path=""
    for i in "${elements[@]}"; do
        local element=$i
        if test -n "$element"; then
            path="$path/$element"
        fi
    done
    path+="$trailingslash"
    echo "$path"
    #echo "ABSPATH ($1) => $path" >&2
    return 0
}

# Make any paths absolute paths.
SWIFTBIN=$(abspath "$SWIFTBIN")
SWIFTLIB=$(abspath "$SWIFTLIB")
SWIFTC=$(abspath "$SWIFTC")
SWIFTGLUEPREFIX=$(abspath "$SWIFTGLUEPREFIX")/
SWIFTBINDINGS=$(abspath "$SWIFTBINDINGS")
TOMSWIFTY=$(abspath "$TOMSWIFTY")

# Write the output to a makefile fragment
FRAGMENT="$SCRIPT_DIR/stconfig.inc"
{
    echo "# This file is generated from stconfig.sh"
    echo "SWIFTBIN=$SWIFTBIN"
    echo "SWIFTLIB=$SWIFTLIB"
    echo "SWIFTC=$SWIFTC"
    echo "SWIFTGLUE=$SWIFTGLUE"
    echo "SWIFTGLUEPREFIX=$SWIFTGLUEPREFIX"
    echo "SWIFTGLUESUFFIX=$SWIFTGLUESUFFIX"
    echo "SWIFTBINDINGS=$SWIFTBINDINGS"
    echo "TOMSWIFTY=$TOMSWIFTY"
} > "$FRAGMENT"
