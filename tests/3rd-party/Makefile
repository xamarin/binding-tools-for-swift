

ALL_TESTS:=$(shell ls -1 */Makefile | sed 's_/.*__')
IGNORED_TESTS:=$(shell grep -l ^IGNORED=1$$ */Makefile | sed 's_/.*__')
WORKING_TESTS=$(shell grep -L ^IGNORED=1$$ */Makefile | sed 's_/.*__')

BUILD_LOG_NAME?=build.log
RUN_LOG_NAME?=run.log

# Main targets:
all:
	@$(MAKE) -j8 build
	@$(MAKE) -j8 run

build: $(foreach test,$(WORKING_TESTS),build-test-$(test))
build-all: $(foreach test,$(ALL_TESTS),build-test-$(test))
run:  $(foreach test,$(WORKING_TESTS),run-test-$(test))

# Helper targets

build-test-%:
	@echo "Building $*..."
	@if ! $(MAKE) -C $* > $*/$(BUILD_LOG_NAME) 2>&1; then echo "Failed to build $*:"; sed 's/^/   /' $*/$(BUILD_LOG_NAME); exit 1; fi
	@echo "Built $*"

run-test-%:
	@echo "Running $*..."
	@if ! $(MAKE) -C $* > $*/$(RUN_LOG_NAME) 2>&1; then echo "Failed to run $*:"; sed 's/^/   /' $*/$(RUN_LOG_NAME); exit 1; fi
	@echo "Built $*"

update-logs:
	git clean -xxffddq
	$(MAKE) build-all -j8 BUILD_LOG_NAME=failed-build.log

list-tests:
	@echo "All tests: $(ALL_TESTS)"
	@echo "Ignored tests: $(IGNORED_TESTS)"
	@echo "Working tests: $(WORKING_TESTS)"
