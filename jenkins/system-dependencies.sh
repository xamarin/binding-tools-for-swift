#!/bin/bash -e

# SC2034=unused variable (triggered for PROVISION_MONO, PROVISION_XI, PROVISION_XM and the IGNRORE_* variants of the same, because these are accessed indirectly)
# shellcheck disable=SC2034

set -o pipefail

cd "$(dirname "$0")/.."

FAIL=
PROVISION_DOWNLOAD_DIR=/tmp/x-provisioning
SUDO=sudo

# parse command-line arguments
while ! test -z "$1"; do
	case $1 in
		--no-sudo)
			SUDO=
			shift
			;;
		--provision-xcode)
			PROVISION_XCODE=1
			unset IGNORE_XCODE
			shift
			;;
		--provision)
			# historical reasons :(
			PROVISION_XCODE=1
			unset IGNORE_XCODE
			shift
			;;
		--provision-mono)
			PROVISION_MONO=1
			unset IGNORE_MONO
			shift
			;;
		--provision-xamarin-ios | --provision-xi)
			PROVISION_XI=1
			unset IGNORE_XI
			shift
			;;
		--provision-xamarin-mac | --provision-xm)
			PROVISION_XM=1
			unset IGNORE_XM
			shift
			;;
		--provision-all)
			PROVISION_MONO=1
			unset IGNORE_MONO
			PROVISION_XCODE=1
			unset IGNORE_XCODE
			PROVISION_XI=1
			unset IGNORE_XI
			PROVISION_XM=1
			unset IGNORE_XM
			shift
			;;
		--ignore-all)
			IGNORE_MACOS=1
			IGNORE_MONO=1
			IGNORE_XCODE=1
			IGNORE_XI=1
			IGNORE_XM=1
			shift
			;;
		--ignore-macos)
			IGNORE_MACOS=1
			shift
			;;
		--ignore-xcode)
			IGNORE_XCODE=1
			shift
			;;
		--ignore-mono)
			IGNORE_MONO=1
			shift
			;;
		--ignore-xamarin-ios | --ignore-xi)
			IGNORE_XI=1
			shift
			;;
		--ignore-xamarin-mac | --ignore-xm)
			IGNORE_XM=1
			shift
			;;
		-v | --verbose)
			set -x
			shift
			;;
		*)
			echo "Unknown argument: $1"
			exit 1
			;;
	esac
done

# reporting functions
COLOR_RED=$(tput setaf 1 2>/dev/null || true)
COLOR_ORANGE=$(tput setaf 3 2>/dev/null || true)
COLOR_MAGENTA=$(tput setaf 5 2>/dev/null || true)
COLOR_BLUE=$(tput setaf 6 2>/dev/null || true)
COLOR_CLEAR=$(tput sgr0 2>/dev/null || true)
COLOR_RESET=uniquesearchablestring
function fail () {
	echo "    ${COLOR_RED}${1//${COLOR_RESET}/${COLOR_RED}}${COLOR_CLEAR}"
	FAIL=1
}

function warn () {
	echo "    ${COLOR_ORANGE}${1//${COLOR_RESET}/${COLOR_ORANGE}}${COLOR_CLEAR}"
}

function ok () {
	echo "    ${1//${COLOR_RESET}/${COLOR_CLEAR}}"
}

function log () {
	echo "        ${1//${COLOR_RESET}/${COLOR_CLEAR}}"
}

# $1: the version to check
# $2: the minimum version to check against
function is_at_least_version () {
	ACT_V=$1
	MIN_V=$2

	if [[ "$ACT_V" == "$MIN_V" ]]; then
		return 0
	fi

	IFS=. read -ra V_ACT <<< "$ACT_V"
	IFS=. read -ra V_MIN <<< "$MIN_V"

	# get the minimum # of elements
	AC=${#V_ACT[@]}
	MC=${#V_MIN[@]}
	COUNT=$((AC>MC?MC:AC))

	C=0
	while (( C < COUNT )); do
		ACT=${V_ACT[$C]}
		MIN=${V_MIN[$C]}
		if (( ACT > MIN )); then
			return 0
		elif (( MIN > ACT )); then
			return 1
		fi
		(( C++ ))
	done

	if (( AC == MC )); then
		# identical?
		return 0
	fi

	if (( AC > MC )); then
		# more version fields in actual than min: OK
		return 0
	elif (( AC == MC )); then
		# entire strings aren't equal (first check in function), but each individual field is?
		return 0
	else
		# more version fields in min than actual (1.0 vs 1.0.1 for instance): not OK
		return 1
	fi
}

function check_macos_version () {
	if ! test -z $IGNORE_MACOS; then return; fi

	local MIN_MACOS_BUILD_VERSION
	MIN_MACOS_BUILD_VERSION=$(grep ^MIN_MACOS_BUILD_VERSION= Make.config | sed 's/.*=//')

	ACTUAL_MACOS_VERSION=$(sw_vers -productVersion)
	if ! is_at_least_version "$ACTUAL_MACOS_VERSION" "$MIN_MACOS_BUILD_VERSION"; then
		fail "You must have at least macOS $MIN_MACOS_BUILD_VERSION (found $ACTUAL_MACOS_VERSION)"
		return
	fi

	ok "Found macOS $ACTUAL_MACOS_VERSION (at least $MIN_MACOS_BUILD_VERSION is required)"
}

function run_xcode_first_launch ()
{
	local XCODE_VERSION="$1"
	local XCODE_DEVELOPER_ROOT="$2"

	# xcodebuild -runFirstLaunch seems to have been introduced in Xcode 9
	if ! is_at_least_version "$XCODE_VERSION" 9.0; then
		return
	fi

	unset XCODE_DEVELOPER_DIR_PATH

	# Delete any cached files by xcodebuild, because other branches'
	# system-dependencies.sh keep installing earlier versions of these
	# packages manually, which means subsequent first launch checks will
	# succeed because we've successfully run the first launch tasks once
	# (and this is cached), while someone else (we!) overwrote with
	# earlier versions (bypassing the cache).
	#
	# Removing the cache will make xcodebuild realize older packages are installed,
	# and (re-)install any newer packages.
	#
	# We'll be able to remove this logic one day, when all branches in use are
	# using 'xcodebuild -runFirstLaunch' instead of manually installing
	# packages.
	find /var/folders -name '*com.apple.dt.Xcode.InstallCheckCache*' -print -delete 2>/dev/null | sed 's/^\(.*\)$/        Deleted Xcode cache file: \1 (this is normal)/' || true
	if ! "$XCODE_DEVELOPER_ROOT/usr/bin/xcodebuild" -checkFirstLaunchStatus; then
		if ! test -z "$PROVISION_XCODE"; then
			# Remove sudo's cache as well, otherwise nothing will happen.
			$SUDO find /var/folders -name '*com.apple.dt.Xcode.InstallCheckCache*' -print -delete 2>/dev/null | sed 's/^\(.*\)$/        Deleted Xcode cache file: \1 (this is normal)/' || true
			# Run the first launch tasks
			log "Executing '$SUDO $XCODE_DEVELOPER_ROOT/usr/bin/xcodebuild -runFirstLaunch'"
			$SUDO "$XCODE_DEVELOPER_ROOT/usr/bin/xcodebuild" -runFirstLaunch
			log "Executed '$SUDO $XCODE_DEVELOPER_ROOT/usr/bin/xcodebuild -runFirstLaunch'"
		else
			fail "Xcode has pending first launch tasks. Execute '$XCODE_DEVELOPER_ROOT/usr/bin/xcodebuild -runFirstLaunch' to execute those tasks."
			return
		fi
	fi
}

function install_specific_xcode () {
	local XCODE_URL
	local XCODE_VERSION
	local XCODE_DEVELOPER_ROOT
	local XCODE_ROOT

	XCODE_URL=$(grep ^XCODE_URL= Make.config | sed 's/.*=//')
	XCODE_VERSION=$(grep ^XCODE_VERSION= Make.config | sed 's/.*=//')
	XCODE_DEVELOPER_ROOT=$(grep ^XCODE_DEVELOPER_ROOT= Make.config | sed 's/.*=//')
	XCODE_ROOT="$(dirname "$(dirname "$XCODE_DEVELOPER_ROOT")")"

	if test -z "$XCODE_URL"; then
		fail "No XCODE_URL set in Make.config, cannot provision"
		return
	fi

	mkdir -p "$PROVISION_DOWNLOAD_DIR"
	log "Downloading Xcode $XCODE_VERSION from $XCODE_URL to $PROVISION_DOWNLOAD_DIR..."
	local XCODE_NAME
	local XCODE_DMG

	XCODE_NAME=$(basename "$XCODE_URL")
	XCODE_DMG=$PROVISION_DOWNLOAD_DIR/$XCODE_NAME

	# Xcode packages can be cached locally in either ~/Downloads or ~/pkg
	if test -f "$HOME/Downloads/$XCODE_NAME"; then
		log "Found $XCODE_NAME in your ~/Downloads folder, copying that version to $XCODE_DMG instead of re-downloading it."
		cp "$HOME/Downloads/$XCODE_NAME" "$XCODE_DMG"
	elif test -f "$HOME/pkg/$XCODE_NAME"; then
		log "Found $XCODE_NAME in your ~/pkg folder, copying that version to $XCODE_DMG instead of re-downloading it."
		cp "$HOME/pkg/$XCODE_NAME" "$XCODE_DMG"
	else
		curl -L "$XCODE_URL" > "$XCODE_DMG"
	fi

	if [[ ${XCODE_DMG: -4} == ".dmg" ]]; then
		local XCODE_MOUNTPOINT=$PROVISION_DOWNLOAD_DIR/$XCODE_NAME-mount
		log "Mounting $XCODE_DMG into $XCODE_MOUNTPOINT..."
		hdiutil attach "$XCODE_DMG" -mountpoint "$XCODE_MOUNTPOINT" -quiet -nobrowse
		log "Removing previous Xcode from $XCODE_ROOT"
		rm -Rf "$XCODE_ROOT"
		log "Installing Xcode $XCODE_VERSION to $XCODE_ROOT..."
		cp -R "$XCODE_MOUNTPOINT"/*.app "$XCODE_ROOT"
		log "Unmounting $XCODE_DMG..."
		hdiutil detach "$XCODE_MOUNTPOINT" -quiet
	elif [[ ${XCODE_DMG: -4} == ".xip" ]]; then
		log "Extracting $XCODE_DMG..."
		pushd . > /dev/null
		cd $PROVISION_DOWNLOAD_DIR
		# make sure there's nothing interfering
		rm -Rf -- *.app
		rm -Rf "$XCODE_ROOT"
		# extract
		xip --expand "$XCODE_DMG"
		log "Installing Xcode $XCODE_VERSION to $XCODE_ROOT..."
		mv -- *.app "$XCODE_ROOT"
		popd > /dev/null
	else
		fail "Don't know how to install $XCODE_DMG"
	fi
	rm -f "$XCODE_DMG"

	log "Removing any com.apple.quarantine attributes from the installed Xcode"
	$SUDO xattr -s -d -r com.apple.quarantine "$XCODE_ROOT"

	if is_at_least_version "$XCODE_VERSION" 5.0; then
		log "Accepting Xcode license"
		$SUDO "$XCODE_DEVELOPER_ROOT"/usr/bin/xcodebuild -license accept
	fi

	if is_at_least_version "$XCODE_VERSION" 9.0; then
		run_xcode_first_launch "$XCODE_VERSION" "$XCODE_DEVELOPER_ROOT"
	elif is_at_least_version "$XCODE_VERSION" 8.0; then
		PKGS="MobileDevice.pkg MobileDeviceDevelopment.pkg XcodeSystemResources.pkg"
		for pkg in $PKGS; do
			if test -f "$XCODE_DEVELOPER_ROOT/../Resources/Packages/$pkg"; then
				log "Installing $pkg"
				$SUDO /usr/sbin/installer -dumplog -verbose -pkg "$XCODE_DEVELOPER_ROOT/../Resources/Packages/$pkg" -target /
				log "Installed $pkg"
			else
				log "Not installing $pkg because it doesn't exist."
			fi
		done
	fi

	log "Executing '$SUDO xcode-select -s $XCODE_DEVELOPER_ROOT'"
	$SUDO xcode-select -s "$XCODE_DEVELOPER_ROOT"
	log "Clearing xcrun cache..."
	xcrun -k

	ok "Xcode $XCODE_VERSION provisioned"
}

function install_coresimulator ()
{
	local XCODE_DEVELOPER_ROOT
	local CORESIMULATOR_PKG
	local CORESIMULATOR_PKG_DIR
	local XCODE_ROOT
	local TARGET_CORESIMULATOR_VERSION
	local CURRENT_CORESIMULATOR_VERSION

	XCODE_DEVELOPER_ROOT=$(grep XCODE_DEVELOPER_ROOT= Make.config | sed 's/.*=//')
	XCODE_ROOT=$(dirname "$(dirname "$XCODE_DEVELOPER_ROOT")")
	CORESIMULATOR_PKG=$XCODE_ROOT/Contents/Resources/Packages/XcodeSystemResources.pkg

	if ! test -f "$CORESIMULATOR_PKG"; then
		warn "Could not find XcodeSystemResources.pkg (which contains CoreSimulator.framework) in $XCODE_DEVELOPER_ROOT ($CORESIMULATOR_PKG doesn't exist)."
		return
	fi

	# Get the CoreSimulator.framework version from our Xcode
	# Extract the .pkg to get the pkg's PackageInfo file, which contains the CoreSimulator.framework version.
	CORESIMULATOR_PKG_DIR=$(mktemp -d)
	pkgutil --expand "$CORESIMULATOR_PKG" "$CORESIMULATOR_PKG_DIR/extracted"

	if ! TARGET_CORESIMULATOR_VERSION=$(xmllint --xpath 'string(/pkg-info/bundle-version/bundle[@id="com.apple.CoreSimulator"]/@CFBundleShortVersionString)' "$CORESIMULATOR_PKG_DIR/extracted/PackageInfo"); then
		rm -rf "$CORESIMULATOR_PKG_DIR"
		warn "Failed to look up the CoreSimulator version of $XCODE_DEVELOPER_ROOT"
		return
	fi
	rm -rf "$CORESIMULATOR_PKG_DIR"

	# Get the CoreSimulator.framework currently installed
	local CURRENT_CORESIMULATOR_PATH=/Library/Developer/PrivateFrameworks/CoreSimulator.framework/Versions/A/CoreSimulator
	local CURRENT_CORESIMULATOR_VERSION=0.0
	if test -f "$CURRENT_CORESIMULATOR_PATH"; then
		CURRENT_CORESIMULATOR_VERSION=$(otool -L $CURRENT_CORESIMULATOR_PATH | grep "$CURRENT_CORESIMULATOR_PATH.*current version" | sed -e 's/.*current version//' -e 's/)//' -e 's/[[:space:]]//g')
	fi

	# Either version may be composed of either 2 or 3 numbers.
	# We only care about the first two, so strip off the 3rd number if it exists.
	# shellcheck disable=SC2001
	CURRENT_CORESIMULATOR_VERSION=$(echo "$CURRENT_CORESIMULATOR_VERSION" | sed 's/\([0-9]*[.][0-9]*\).*/\1/')
	# shellcheck disable=SC2001
	TARGET_CORESIMULATOR_VERSION=$(echo "$TARGET_CORESIMULATOR_VERSION" | sed 's/\([0-9]*[.][0-9]*\).*/\1/')

	# Compare versions to see if we got what we need
	if [[ x"$TARGET_CORESIMULATOR_VERSION" == x"$CURRENT_CORESIMULATOR_VERSION" ]]; then
		log "Found CoreSimulator.framework $CURRENT_CORESIMULATOR_VERSION (exactly $TARGET_CORESIMULATOR_VERSION is recommended)"
		return
	fi

	if test -z $PROVISION_XCODE; then
		# This is not a failure for now, until this logic has been tested thoroughly
		warn "You should have exactly CoreSimulator.framework version $TARGET_CORESIMULATOR_VERSION (found $CURRENT_CORESIMULATOR_VERSION). Execute '$0 --provision-xcode' to install the expected version."
		return
	fi

	# Just installing the package won't work, because there's a version check somewhere
	# that prevents the macOS installer from downgrading, so remove the existing
	# CoreSimulator.framework manually first.
	log "Installing CoreSimulator.framework $CURRENT_CORESIMULATOR_VERSION..."
	$SUDO rm -Rf /Library/Developer/PrivateFrameworks/CoreSimulator.framework
	$SUDO installer -pkg "$CORESIMULATOR_PKG" -target / -allowUntrusted

	CURRENT_CORESIMULATOR_VERSION=$(otool -L $CURRENT_CORESIMULATOR_PATH | grep "$CURRENT_CORESIMULATOR_PATH.*current version" | sed -e 's/.*current version//' -e 's/)//' -e 's/[[:space:]]//g')
	log "Installed CoreSimulator.framework $CURRENT_CORESIMULATOR_VERSION successfully."
}

function check_specific_xcode () {
	local XCODE_DEVELOPER_ROOT
	local XCODE_VERSION
	local XCODE_ROOT

	XCODE_DEVELOPER_ROOT=$(grep ^XCODE_DEVELOPER_ROOT= Make.config | sed 's/.*=//')
	XCODE_VERSION=$(grep ^XCODE_VERSION= Make.config | sed 's/.*=//')
	XCODE_ROOT=$(dirname "$(dirname "$XCODE_DEVELOPER_ROOT")")

	if ! test -d "$XCODE_DEVELOPER_ROOT"; then
		if ! test -z "$PROVISION_XCODE"; then
			if ! test -z "$JENKINS_URL"; then
				install_specific_xcode
			else
				fail "Automatic provisioning of Xcode is only supported for provisioning internal build bots."
				fail "Please download and install Xcode $XCODE_VERSION here: https://developer.apple.com/downloads/index.action?name=Xcode"
			fi
		else
			fail "You must install Xcode ($XCODE_VERSION) in $XCODE_ROOT. You can download Xcode $XCODE_VERSION here: https://developer.apple.com/downloads/index.action?name=Xcode"
		fi
		return
	else
		if ! "$XCODE_DEVELOPER_ROOT"/usr/bin/xcodebuild -license check >/dev/null 2>&1; then
			if ! test -z "$PROVISION_XCODE"; then
				$SUDO "$XCODE_DEVELOPER_ROOT"/usr/bin/xcodebuild -license accept
			else
				fail "The license for Xcode $XCODE_VERSION has not been accepted. Execute '$SUDO $XCODE_DEVELOPER_ROOT/usr/bin/xcodebuild' to review the license and accept it."
				return
			fi
		fi

		run_xcode_first_launch "$XCODE_VERSION" "$XCODE_DEVELOPER_ROOT"
	fi

	local XCODE_ACTUAL_VERSION
	XCODE_ACTUAL_VERSION=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$XCODE_DEVELOPER_ROOT/../version.plist")
	# this is a hard match, having 4.5 when requesting 4.4 is not OK (but 4.4.1 is OK)
	if [[ ! "x$XCODE_ACTUAL_VERSION" =~ x$XCODE_VERSION ]]; then
		fail "You must install Xcode $XCODE_VERSION in $XCODE_ROOT (found $XCODE_ACTUAL_VERSION).  You can download Xcode $XCODE_VERSION here: https://developer.apple.com/downloads/index.action?name=Xcode";
		return
	fi

	local XCODE_SELECT
	XCODE_SELECT=$(xcode-select -p)
	if [[ "x$XCODE_SELECT" != "x$XCODE_DEVELOPER_ROOT" ]]; then
		if ! test -z $PROVISION_XCODE; then
			log "Executing '$SUDO xcode-select -s $XCODE_DEVELOPER_ROOT'"
			$SUDO xcode-select -s "$XCODE_DEVELOPER_ROOT"
			log "Clearing xcrun cache..."
			xcrun -k
		else
			fail "'xcode-select -p' does not point to $XCODE_DEVELOPER_ROOT, it points to $XCODE_SELECT. Execute ${COLOR_MAGENTA}$SUDO xcode-select -s $XCODE_DEVELOPER_ROOT${COLOR_RESET} or ${COLOR_MAGENTA}$0 --provision-xcode${COLOR_RESET} to fix."
		fi
	fi

	ok "Found Xcode $XCODE_ACTUAL_VERSION in $XCODE_ROOT"
}

function check_xcode () {
	if ! test -z $IGNORE_XCODE; then
		warn "Ignoring the Xcode dependency because the ${COLOR_BLUE}IGNORE_XCODE${COLOR_RESET} variable is set."
		return;
	fi

	# must have latest Xcode in /Applications/Xcode<version>.app
	check_specific_xcode
	install_coresimulator
}

function install_versioned_product () {
	local PRODUCT_URL=$1
	local INFIX=$2
	local PRODUCT_NAME=$3
	local PRODUCT_VERSION=$4

	local PRODUCT_URL_NAME
	local PRODUCT_PKG

	if test -z "$PRODUCT_URL"; then
		fail "No MIN_${INFIX}_URL set in Make.config, cannot provision $PRODUCT_NAME."
		return
	fi

	mkdir -p "$PROVISION_DOWNLOAD_DIR"
	log "Downloading $PRODUCT_NAME $PRODUCT_VERSION from $PRODUCT_URL to $PROVISION_DOWNLOAD_DIR..."
	PRODUCT_URL_NAME=$(basename "$PRODUCT_URL")
	PRODUCT_PKG=$PROVISION_DOWNLOAD_DIR/$PRODUCT_URL_NAME

	# Packages can be cached locally in either ~/Downloads or ~/pkg
	PRODUCT_SOURCE=$PRODUCT_URL
	if test -f "$HOME/Downloads/$PRODUCT_URL_NAME"; then
		log "Found $PRODUCT_URL_NAME ($PRODUCT_VERSION) in your ~/Downloads folder, copying that version to $PRODUCT_PKG instead of re-downloading it."
		PRODUCT_SOURCE="$HOME/Downloads/$PRODUCT_URL_NAME"
		cp -c "$PRODUCT_SOURCE" "$PRODUCT_PKG"
	elif test -f "$HOME/pkg/$PRODUCT_URL_NAME"; then
		log "Found $PRODUCT_URL_NAME ($PRODUCT_VERSION) in your ~/pkg folder, copying that version to $PRODUCT_PKG instead of re-downloading it."
		PRODUCT_SOURCE="$HOME/pkg/$PRODUCT_URL_NAME"
		cp -c "$PRODUCT_SOURCE" "$PRODUCT_PKG"
	else
		curl -L "$PRODUCT_SOURCE" > "$PRODUCT_PKG"
	fi

	log "Installing $PRODUCT_NAME $PRODUCT_VERSION from $PRODUCT_SOURCE..."
	$SUDO installer -pkg "$PRODUCT_PKG" -target /

	rm -f "$PRODUCT_PKG"
}

function check_versioned_product () {
	local VERSION_FILE=$1
	local INFIX=$2
	local PRODUCT_NAME=$3
	local MIN_PRODUCT_VERSION
	local MAX_PRODUCT_VERSION
	local MIN_PRODUCT_URL
	local IGNORE_PRODUCT

	local ignore_var=IGNORE_$INFIX
	IGNORE_PRODUCT=${!ignore_var}
	local provision_var=PROVISION_$INFIX
	PROVISION_PRODUCT=${!provision_var}

	if ! test -z "$IGNORE_PRODUCT"; then
		warn "Ignoring the ${PRODUCT_NAME} dependency because the ${COLOR_BLUE}IGNORE_${INFIX}${COLOR_RESET} variable is set."
		return;
	fi

	local min_var=MIN_${INFIX}_VERSION
	MIN_PRODUCT_VERSION=$(grep "^${min_var}=" Make.config | sed 's/.*=//')
	local max_var=MAX_${INFIX}_VERSION
	MAX_PRODUCT_VERSION=$(grep "^${max_var}=" Make.config | sed 's/.*=//')
	local url_var=MIN_${INFIX}_URL
	MIN_PRODUCT_URL=$(grep "^${url_var}=" Make.config | sed 's/.*=//')

	if ! test -f "$VERSION_FILE"; then
		if test -z "$PROVISION_PRODUCT"; then
			fail "You must install $PRODUCT_NAME $MIN_PRODUCT_VERSION, no version of $PRODUCT_NAME was detected."
		else
			install_versioned_product "$MIN_PRODUCT_URL" "$INFIX" "$PRODUCT_NAME" "$MIN_PRODUCT_VERSION"
		fi
		return
	fi

	ACTUAL_PRODUCT_VERSION=$(cat "$VERSION_FILE")
	if ! is_at_least_version "$ACTUAL_PRODUCT_VERSION" "$MIN_PRODUCT_VERSION"; then
		if ! test -z "$PROVISION_PRODUCT"; then
			install_versioned_product "$MIN_PRODUCT_URL" "$INFIX" "$PRODUCT_NAME" "$MIN_PRODUCT_VERSION"
			ACTUAL_PRODUCT_VERSION=$(cat "$VERSION_FILE")
		else
			fail "You must have at least $PRODUCT_NAME $MIN_PRODUCT_VERSION, found $ACTUAL_PRODUCT_VERSION. Download URL: $MIN_PRODUCT_URL"
			return
		fi
	elif [[ "$ACTUAL_PRODUCT_VERSION" == "$MAX_PRODUCT_VERSION" ]]; then
		: # this is ok
	elif is_at_least_version "$ACTUAL_PRODUCT_VERSION" "$MAX_PRODUCT_VERSION"; then
		if ! test -z "$PROVISION_PRODUCT"; then
			install_versioned_product "$MIN_PRODUCT_URL" "$INFIX" "$PRODUCT_NAME" "$MIN_PRODUCT_VERSION"
			ACTUAL_PRODUCT_VERSION=$(cat "$VERSION_FILE")
		else
			fail "Your $PRODUCT_NAME version is too new, max version is $MAX_PRODUCT_VERSION, found $ACTUAL_PRODUCT_VERSION."
			warn "You can execute ${COLOR_MAGENTA}$0 --provision-$(echo "${INFIX}" | tr '[:upper:]' '[:lower:]')${COLOR_RESET} to automatically install ${PRODUCT_NAME}."
			warn "You may also edit Make.config and change MAX_${INFIX}_VERSION to your actual version to continue the"
			warn "build (unless you're on a release branch). Once the build completes successfully, please"
			warn "commit the new MAX_${INFIX}_VERSION value."
			warn "Alternatively you can ${COLOR_MAGENTA}export IGNORE_${INFIX}=1${COLOR_RESET} to skip this check."
			return
		fi
	fi

	ok "Found $PRODUCT_NAME $ACTUAL_PRODUCT_VERSION (at least $MIN_PRODUCT_VERSION and not more than $MAX_PRODUCT_VERSION is required)"
}

function check_mono () {
	check_versioned_product /Library/Frameworks/Mono.framework/Versions/Current/VERSION MONO Mono

	if ! command -v mono > /dev/null 2>&1; then
		fail "Mono is not in PATH. You must add '/Library/Frameworks/Mono.framework/Versions/Current/Commands' to PATH. Current PATH is: $PATH".
		return
	fi
}

function check_xi () {
	check_versioned_product /Library/Frameworks/Xamarin.iOS.framework/Versions/Current/Version XI Xamarin.iOS
}

function check_xm () {
	check_versioned_product /Library/Frameworks/Xamarin.Mac.framework/Versions/Current/Version XM Xamarin.Mac
}

echo "Checking system..."

check_macos_version
check_xcode
check_mono
check_xi
check_xm

if test -z $FAIL; then
	echo "System check succeeded"
else
	echo "System check failed"
	exit 1
fi
