TOP=..
include ./libraries.mk

all: compile create-dlls

clean-modules:
	@echo "Cleaning xmls in Modules directory"
	@rm -rf ./Modules/*.xml

clean: clean-modules
	
create-dlls:
	@mono Program.exe $(addprefix --library=, $(MANUAL_BINDER_FINDER_LIBRARIES))

compile:
	@csc *.cs -r:Mono.Options.dll -r:SwiftRuntimeLibrary.dll -r:SwiftReflector.dll -r:Dynamo.dll
