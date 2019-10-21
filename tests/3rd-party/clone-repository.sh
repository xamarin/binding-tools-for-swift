#!/bin/bash -eu

# This is a helper script to the 'clone' target in Makefile.inc, since this
# becomes horribly complicated to express in Make syntax.
#
# It takes the url of a GitHub repository and a hash, and clones it into a
# subdirectory called 'repository', checking out the specified hash at the
# same time.
#
# It works fine if the repository is already checked out, and tries to be
# smart about not hitting the network as long as the required hash is already
# present in the cloned repository.
#

URL="${1:-}"
HASH="${2:-}"

function show_usage ()
{
	echo "Usage: $0 github-repository-url hash"
	if test -n "$@"; then
		echo "$@"
		return 1
	fi
	return 0
}

if test -z "$URL"; then
	show_usage "Error: The url of the repository must be the first argument."
	exit 1
fi
if test -z "$HASH"; then
	show_usage "Error: The hash to check out must be the second argument."
	exit 1
fi
if test -n "${3:-}"; then
	shift 2
	show_usage "Error: Unexpected argument(s): $*"
	exit 1
fi

# Clone if not already cloned
if ! test -d repository/.git; then
	rm -Rf repository
	echo "Cloning $URL into ./repository..."
	git clone -q --recurse-submodules -q "$URL" repository
fi

cd repository
if ! git log -1 --pretty=%H "$HASH" > /dev/null 2>&1; then
	# We don't have the hash we need. Fetch more sources
	echo "Fetching $URL..."
	git fetch
fi

CURRENT_HASH=$(git log -1 --pretty="%H")
if [[ "$CURRENT_HASH" != "$HASH" ]]; then
	# We're at the wrong hash.
	echo "Checking out $HASH for $URL..."
	git reset --hard "$HASH"
	echo "Updating submodules for $URL..."
	git submodule update --recursive --init
fi

echo "Successfully checked out $HASH for $URL in ./repository."
