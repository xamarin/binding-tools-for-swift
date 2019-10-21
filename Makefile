TOP=.
GIT_DIR=$(TOP)/../../.git

all:
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
