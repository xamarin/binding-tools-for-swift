TOP=../..
include $(TOP)/common.mk

all: build run-tests
	@# This needs to run in a subprocess, because the existing make process
	@# has already calculated required variables that contain output from
	@# the 'run-tests' target, which hadn't executed when the variables were
	@# evaluated.
	#$(MAKE) -j8 build-device-tests

check: run-tests


# in order to run a single test, do:
# dotnet test --filter Name=HasBuiltInTypes
# If you want to do a fully qualified name, do:
# dotnet test --filter FullyQualifiedName~SwiftReflector.SwiftTypeRegistryTests.HasBuiltInTypes
run-tests: bin/Debug/net7.0/tom-swifty-test.dll
	rm -f .failed-stamp
	rm -Rf bin/devicetests # remove any existing device test output
	DYLD_LIBRARY_PATH=$(DYLD_LIBRARY_PATH):/usr/lib/swift:$(SWIFTGLUEPREFIX)mac$(SWIFTGLUESUFFIX) \
	dotnet test tom-swifty-test.csproj
	@# Create an html file and tell MonkeyWrench to upload it (if we're running there)
	@[[ -z "$$BUILD_REPOSITORY" ]] || \
		( xsltproc ../HtmlTransform.xslt TestResult.xml  > index.html && \
		echo "@MonkeyWrench: AddFile: $$PWD/index.html")
	@[[ ! -e .failed-stamp ]] 

build: bin/Debug/tom-swifty-test.dll

run-runtime-library-tests: bin/Debug/net7.0/tom-swifty-test.dll
	rm -f .$@-failed-stamp
	LEAKTEST_DYLD_LIBRARY_PATH=$(DYLD_LIBRARY_PATH):/usr/lib/swift:$(SWIFTGLUEPREFIX)mac$(SWIFTGLUESUFFIX) \
	$(TOP)/leaktest/bin/Debug/net7.0/leaktest mono --debug --runtime=v4.0 $(TOP)/packages/NUnit.ConsoleRunner.3.8.0/tools/nunit3-console.exe bin/Debug/net7.0/tom-swifty-test.dll --workers=1 --framework=mono-4.0 --nocolor --labels=All --shadowcopy --where=namespace=SwiftRuntimeLibraryTests --inprocess || touch .failed-stamp

bin/Debug/net7.0/tom-swifty-test.dll: $(shell git ls-files . | grep '[.]cs$$') tom-swifty-test.csproj SwiftRuntimeLibrary.Mac.dll.config
	dotnet build

SwiftRuntimeLibrary.Mac.dll.config: SwiftRuntimeLibrary.Mac.dll.config.in Makefile
	$(Q) sed -e 's|@SWIFTLIB@|$(SWIFTLIB)|' -e 's|@SWIFTGLUEPREFIX@|$(SWIFTGLUEPREFIX)|' -e 's|@SWIFTGLUESUFFIX@|$(SWIFTGLUESUFFIX)|' $< > $@.tmp
	$(Q) mv $@.tmp $@

dependencies: build

clean:
	@rm -rf bin obj TestResult.xml devicetests/*

