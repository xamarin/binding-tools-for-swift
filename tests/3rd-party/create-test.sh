#!/bin/bash -eu

# This script takes the url of a GitHub repository, and creates a test directory for it, using Makefile.template, filling in the required info automatically.
# Optionally it can take the hash to checkout as the second argument
#
# At the end it will also try to build the sample
#

URL="${1:-}"
HASH="${2:-}"

function show_usage ()
{
	echo "Usage: $0 github-repository-url [hash]"
	if test -n "$@"; then
		echo "$@"
		return 1
	fi
	return 0
}

if test -z "$URL"; then
	show_usage "Error: No url specified."
	exit 1
fi
if test -n "${3:-}"; then
	shift 2
	show_usage "Error: Unexpected argument(s): $*"
	exit 1
fi

NAME=${URL##*/}
# Remove any trailing .git
NAME=${NAME%.git}

OWNER=${URL%/*}
OWNER=${OWNER##*/}

if test -d "$NAME"; then
	echo "The test directory $NAME already exists."
	exit 1
fi

if test -z "$HASH"; then
	if ! HASH=$(curl -f -s -L -H 'Accept: application/vnd.github.VERSION.sha' "https://api.github.com/repos/$OWNER/$NAME/commits/master"); then
		echo "Could not get the latest hash for $URL from GitHub (this might happen if the default branch isn't 'master'). Please pass it as the second argument."
		exit 1
	fi
fi

mkdir "$NAME"
sed -e "s|%REPO%|$URL|" -e "s|%HASH%|$HASH|" Makefile.template > "$NAME/Makefile"

git add "$NAME/Makefile"

make -C "$NAME" clone

DEFAULT_XCODEPROJ="$NAME/repository/$NAME.xcodeproj"
if ! test -d "$DEFAULT_XCODEPROJ"; then
	PROJECTS=$(find "$NAME/repository" -name '*.xcodeproj' | wc -l)
	# remove leading whitespace
	PROJECTS="${PROJECTS#"${PROJECTS%%[![:space:]]*}"}"
	if [[ $PROJECTS != 1 ]]; then
		echo "Found $PROJECTS in the repository $NAME, none of them the default project path (repository/$NAME.xcodeproj):"
		cd "$NAME" && find repository -name '*.xcodeproj' | sed 's|^|    |'
		echo "Edit $NAME/Makefile and set XCODEPROJECT to the project to build."
		exit 1
	fi
	PROJECT=$(cd "$NAME" && find ./repository -name '*.xcodeproj')
	sed -i '' "s|^.*XCODEPROJECT.*$|XCODEPROJECT=$PROJECT|" "$NAME/Makefile"
fi

make -C "$NAME"
