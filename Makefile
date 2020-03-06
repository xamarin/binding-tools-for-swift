TOP=.
GIT_DIR=$(TOP)/.git

all: check-system
	./jenkins/build-swift.sh
	./jenkins/build.sh

include $(TOP)/Make.config

provision:
	./jenkins/provision-deps.sh

swift:
	./jenkins/build-swift.sh

som binding-tools-for-swift:
	./jenkins/build.sh

Constants.cs: Constants.cs.in $(GIT_DIR)/index Makefile
	sed \
		-e "s/@VERSION@/$(SOM_PACKAGE_VERSION)/g" \
		-e "s/@BRANCH@/$(SOM_BRANCH)/g" \
		-e "s/@HASH@/$(shell git log -1 --pretty=%h)/g" \
		$< > $@

package:
	./jenkins/build-package.sh

print-variable:
	@echo $($(VARIABLE))

check-system:
	@if (( $(SOM_COMMIT_DISTANCE) > 999 )); then \
		echo "$(COLOR_RED)*** The commit distance for Binding Tools For Swift ($(SOM_COMMIT_DISTANCE)) is > 999.$(COLOR_CLEAR)"; \
		echo "$(COLOR_RED)*** To fix this problem, bump the revision ($(COLOR_GRAY)SOM_PACKAGE_VERSION_REV$(COLOR_RED)) in Make.config.$(COLOR_CLEAR)"; \
		echo "$(COLOR_RED)*** Once fixed, you need to commit the changes for them to pass this check.$(COLOR_CLEAR)"; \
		exit 1; \
	fi
	@./jenkins/system-dependencies.sh
	@echo "Building Binding Tools For Swift $(SOM_PACKAGE_VERSION)"
