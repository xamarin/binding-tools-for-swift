#!/bin/bash -eu

# This script takes two frameworks, one built for the simulator and one built
# for device, and creates a single fat framework as output.
#
# It will include swift-specific files
# (foo.framework/Modules/f.swiftmodule/*.swift*) from both the input
# frameworks into the output framework.
#
# For files that can't be merged (Info.plist), the device version will be used.
#

OUTPUT=
INPUT_SIM=
INPUT_DEV=

function show_usage ()
{
	echo "Usage: $0 --output=<output-fat-framework> --input-device=<input-framework-built-for-device> --input-simulator=<input-framework-built-for-simulator>"
	if test -n "$@"; then
		echo "$@"
		return 1
	fi
	return 0
}

while ! test -z "${1:-}"; do
	case "$1" in
		--output=*)
			OUTPUT="${1:9}"
			shift
			;;
		--output)
			OUTPUT="$2"
			shift 2
			;;
		--input-device=*)
			INPUT_DEV="${1:15}"
			shift
			;;
		--input-device)
			INPUT_DEV="$2"
			shift 2
			;;
		--input-simulator=*)
			INPUT_SIM="${1:18}"
			shift
			;;
		--input-simulator)
			INPUT_SIM="$2"
			shift 2
			;;
		*)
			show_usage "Error: Unknown argument: $1"
			exit 1
			;;
    esac
done

if test -z "$OUTPUT"; then
	show_usage "Error: The output (--output=<output-fat-framework>) is required."
	exit 1
elif test -d "$OUTPUT"; then
	show_usage "Error: The output location ($OUTPUT) already exists."
	exit 1
fi

if test -z "$INPUT_SIM"; then
	show_usage "Error: The simulator version of the framework (--input-simulator=<input-framework-built-for-simulator>) is required."
	exit 1
elif ! test -d "$INPUT_SIM"; then
	show_usage "Error: The simulator version of the framework ($INPUT_SIM) does not exist."
	exit 1
fi

if test -z "$INPUT_DEV"; then
	show_usage "Error: The device version of the framework (--input-device=<input-framework-built-for-device>) is required."
	exit 1
elif ! test -d "$INPUT_SIM"; then
	show_usage "Error: The device version of the framework ($INPUT_DEV) does not exist."
	exit 1
fi

# Remove any trailing slashes
OUTPUT=${OUTPUT%/}
INPUT_DEV=${INPUT_DEV%/}
INPUT_SIM=${INPUT_SIM%/}

FW_NAME=$(basename -- "$INPUT_DEV")
FW_NAME=${FW_NAME%.framework}

cp -r "$INPUT_DEV" "$OUTPUT"
cp "$INPUT_SIM"/Modules/"$FW_NAME".swiftmodule/*.swift* "$OUTPUT/Modules/$FW_NAME.swiftmodule/"
rm -f "$OUTPUT/$FW_NAME"
lipo "$INPUT_DEV/$FW_NAME" "$INPUT_SIM/$FW_NAME" -create -output "$OUTPUT/$FW_NAME"
