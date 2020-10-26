#!/bin/bash -e

cd "$(dirname "${BASH_SOURCE[0]}")/.."
WORKSPACE=$(pwd)

# SC1091: Not following: SwiftVersion.mk was not specified as input (see shellcheck -x).
# shellcheck disable=SC1091
source SwiftVersion.mk

STAMPDIR="$WORKSPACE/apple"
STAMPFILE="$STAMPDIR/swift-$SWIFT_HASH.stamp"
PACKAGED_STAMPFILE="$STAMPDIR/packaged-$SWIFT_HASH.stamp"

if [[ "x$1" == "x--publish" ]]; then
	PUBLISH=1
fi

# Check if we're using a packaged version of swift
if test -f "$PACKAGED_STAMPFILE$FORCE_SWIFT_BUILD"; then
	echo "✅ No need to build swift, we're already using a packaged version"
	exit 0
fi

# Check if we already have a swift package uploaded
# Bump 'v#' whenever a new package must be uploaded with the same swift hash (it can be bumped back to 1 every time we bump the swift hash if we wish)
# (for instance if changes to Pack-Man were required to adjust which files are packaged)
SWIFT_TOOLCHAIN_NAME="SwiftToolchain-v1-$SWIFT_HASH"
if test -z "$FORCE_SWIFT_BUILD"; then
	toolchain_url="https://bosstoragemirror.blob.core.windows.net/wrench/binding-tools-for-swift/toolchain/$SWIFT_TOOLCHAIN_NAME.zip"
	echo "Checking if we already have a toolchain built for $SWIFT_HASH ($toolchain_url)"

	# Extract only if not already extracted
	if ! test -d "$SWIFT_TOOLCHAIN_NAME"; then
		# Download only if not already downloaded, and use a temporary file to avoid problems with incomplete downloads
		if ! test -f "$SWIFT_TOOLCHAIN_NAME.zip"; then
			CACHED_PATH=~/Library/Caches/binding-tools-for-swift/$SWIFT_TOOLCHAIN_NAME.zip
			if test -f "$CACHED_PATH"; then
				echo "Found a cached version of $toolchain_url in $CACHED_PATH."
				cp -c "$CACHED_PATH" "$SWIFT_TOOLCHAIN_NAME.zip"
			elif curl -vf -L "$toolchain_url" --output "$SWIFT_TOOLCHAIN_NAME.zip.tmp"; then
				# Yay, downloaded a package!
				mv "$SWIFT_TOOLCHAIN_NAME.zip.tmp" "$SWIFT_TOOLCHAIN_NAME.zip"
			else
				echo "Failed to download the toolchain, will now build swift from source."
			fi
		fi
		if test -f "$SWIFT_TOOLCHAIN_NAME.zip"; then
			# Unzip & use it
			rm -Rf "$SWIFT_TOOLCHAIN_NAME" "$SWIFT_TOOLCHAIN_NAME.tmp"
			unzip -o "$SWIFT_TOOLCHAIN_NAME.zip" -d "$SWIFT_TOOLCHAIN_NAME.tmp"
			mv "$SWIFT_TOOLCHAIN_NAME.tmp/$SWIFT_TOOLCHAIN_NAME" .
			rm -rf "$SWIFT_TOOLCHAIN_NAME.tmp"
		fi
	fi

	if test -d "$SWIFT_TOOLCHAIN_NAME"; then
		rm -f "$WORKSPACE/apple"
		ln -s "$WORKSPACE/$SWIFT_TOOLCHAIN_NAME" "$WORKSPACE/apple"
		echo "✅ Swift build completed using packaged toolchain"
		find "$WORKSPACE/apple"
		touch "$PACKAGED_STAMPFILE"
		exit 0
	fi
fi

# Get & build swift
cd "$WORKSPACE/.."
mkdir -p swift
cd swift

function complete_swift_build ()
{
	echo "Creating swift symlink..."
	rm -Rf "$WORKSPACE/apple"
	ln -s "$WORKSPACE/../swift" "$WORKSPACE/apple"

	if test -n "$PUBLISH"; then
		echo "Packaging toolchain..."
		cd "$WORKSPACE/Pack-Man" && "./build.sh" --target=SwiftToolchain --output-directory="$SWIFT_TOOLCHAIN_NAME"
	fi
}

# Check if we've already built the right hash, in which case just bail out.
if test -d swift; then
	cd swift
	HASH=$(git log -1 --pretty=%H)
	if [[ "x$HASH" == "x$SWIFT_HASH" && -f "$STAMPFILE" ]]; then
		complete_swift_build

		echo "✅ No need to build swift!"
		exit 0
	fi

	# We don't have an existing build of the right hash.
	# Cleanup everything and start from scratch.
	echo "⚠️  Cleaning & rebuilding the swift dependency, since it's not up-to-date.  ⚠️"
	echo "⚠️  This is a very destructive operation. ⚠️ "
	echo "⚠️  You have 15 seconds to cancel (Ctrl-C) if you wish. ⚠️ "
	sleep 15
	echo "Cleaning swift..."

	cd ../..
	rm -Rf swift
	mkdir swift
	cd swift
fi

echo "Checking out swift..."
git clone https://github.com/xamarin/binding-tools-for-swift-reflector swift
cd swift
ls -la
git checkout --force "$SWIFT_BRANCH"
git reset --hard "$SWIFT_HASH"

echo "Updating swift dependencies..."
./utils/update-checkout --clone --skip-repository swift -j 1 --scheme "$SWIFT_SCHEME"

echo "Reinstalling six"
pip uninstall six
pip install six

echo "Building swift (not so swiftly, some patience is required)..."
./utils/build-script --clean -R --ios --tvos --watchos --extra-cmake-options=-DSWIFT_DARWIN_ENABLE_STABLE_ABI_BIT:BOOL=TRUE

complete_swift_build

touch "$STAMPFILE"

echo "✅ Swift build completed"
