TOP=.
GIT_DIR=$(TOP)/.git

all: check-system
	./devops/automation/build-swift.sh
	./devops/automation/build.sh

include $(TOP)/Make.config

provision:
	./devops/automation/provision-deps.sh

swift:
	./devops/automation/build-swift.sh

som binding-tools-for-swift:
	./devops/automation/build.sh

Constants.cs: Constants.cs.in $(GIT_DIR)/index Makefile
	sed \
		-e "s/@VERSION@/$(SOM_PACKAGE_VERSION)/g" \
		-e "s/@BRANCH@/$(SOM_BRANCH)/g" \
		-e "s/@HASH@/$(shell git log -1 --pretty=%h)/g" \
		$< > $@

package:
	./devops/automation/build-package.sh

print-variable:
	@echo $($(VARIABLE))

check-system:
	@if (( $(SOM_COMMIT_DISTANCE) > 999 )); then \
		echo "$(COLOR_RED)*** The commit distance for Binding Tools For Swift ($(SOM_COMMIT_DISTANCE)) is > 999.$(COLOR_CLEAR)"; \
		echo "$(COLOR_RED)*** To fix this problem, bump the revision ($(COLOR_GRAY)SOM_PACKAGE_VERSION_REV$(COLOR_RED)) in Make.config.$(COLOR_CLEAR)"; \
		echo "$(COLOR_RED)*** Once fixed, you need to commit the changes for them to pass this check.$(COLOR_CLEAR)"; \
		exit 1; \
	fi
	@./devops/automation/system-dependencies.sh
	@echo "Building Binding Tools For Swift $(SOM_PACKAGE_VERSION)"
