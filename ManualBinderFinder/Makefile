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
	dotnet build
	dotnet publish --runtime osx.10.15-x64 --self-contained true
	
