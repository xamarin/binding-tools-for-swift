#!/bin/bash -eu

#
# This script will run the device tests (iphone/bin/iPhone/Debug/tomswiftydevicetests.app) on device.
#
# It takes no arguments.
#

if test -n "${1:-}"; then
	echo "Usage: $0"
	echo "Error: unexpected arguments: $*"
	exit 1
fi

cd "$(dirname "${BASH_SOURCE[0]}")"

MLAUNCH_STDIN_PIPE=mlaunch-input
MLAUNCH_DONE=mlaunch-done
MLAUNCH_OUTPUT=mlaunch-output
function onexit ()
{
	rm -f "$MLAUNCH_STDIN_PIPE" "$MLAUNCH_DONE" "$MLAUNCH_OUTPUT"
}
trap onexit EXIT

MLAUNCH=/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch
APP=iphone/bin/iPhone/Debug/tomswiftydevicetests.app

if ! test -d "$APP"; then
	echo "The device test app ($APP) doesn't exist."
	exit 1
fi

echo "Running iOS device tests"

# Find the UDID of a device we can execute on.
# Don't search for devices again if we already have a list of them (devices.xml), so that reruns are faster.
DEVICE=
if test -f devices.xml; then
	DEVICE="$(./find-device.csharp iPad iPhone)"
fi
if test -z "$DEVICE"; then
	$MLAUNCH --listdev=devices.xml --output-format=xml
	echo "Devices:"
	sed 's/^/    /' devices.xml
	DEVICE="$(./find-device.csharp iPad iPhone)"
fi

if test -z "$DEVICE"; then
	echo "Could not find any applicable iOS devices."
	exit 1
fi

# Install the app on device
echo "Installing $APP on $DEVICE..."
$MLAUNCH --installdev "$APP" --devname "$DEVICE"
echo "App installed successfully."

echo "Running $(basename $APP) on $DEVICE"
# Run the app on device using mlaunch
# There's a 10 minute timeout (600 seconds), upon which we'll kill the app (by writing a newline to mlaunch's stdin)
# We also redirect mlaunch's stdout+stderr, so that we can grep for the string that means the tests passed.
# This requires some creative use of pipes...
rm -f "$MLAUNCH_STDIN_PIPE"
mkfifo "$MLAUNCH_STDIN_PIPE"
rm -f "$MLAUNCH_DONE"
(
	$MLAUNCH --launchdev "$APP" --devname "$DEVICE" --wait-for-exit < <(tail -f "$MLAUNCH_STDIN_PIPE") | tee 2>&1 "$MLAUNCH_OUTPUT"
	touch "$MLAUNCH_DONE"
)&
# Check every second if mlaunch is done
SECONDS_LEFT=600
while [ $SECONDS_LEFT -gt 0 ]; do
	if test -f "$MLAUNCH_DONE"; then break; fi
	sleep 1
	let SECONDS_LEFT-- || true
	echo "$SECONDS_LEFT seconds left"
done
# If not done, we timed out, so kill the app (by writing a newline to mlaunch's stdin)
if ! test -f "$MLAUNCH_DONE"; then
	echo "iOS tests timed out, killing the app"
	# write newlines to stdin to kill the app
	printf "\\n\\n\\n" > "$MLAUNCH_STDIN_PIPE"
fi
# Wait for mlaunch to actually finish executing
wait

echo "Done running iOS device tests"

RESULT=$(grep " Executed.*passed.*failed.*skipped" "$MLAUNCH_OUTPUT" || true)
if test -z "$RESULT"; then
	echo "Test run didn't complete successfully"
	EC=1
elif [[ "$RESULT" == *" 0 failed, 0 skipped"* ]]; then
	echo "Test run completed successfully."
	EC=0
else
	echo "Test run completed, but with failures: ${RESULT/* Executed/Executed}"
	EC=1
fi

exit $EC
