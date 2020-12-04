TOP=..
include ./libraries.mk

all: compile execute

clean-modules:
	@echo "Cleaning xmls in Modules directory"
	@rm -rf ./Modules/*.xml

clean: clean-modules

platform: compile
	@echo "What platform?: "; \
	read Platform; \
	mono bin/Debug/BindingNemo.exe --platform="$$Platform" --architecture=$(arch)

execute:
	@mono bin/Debug/BindingNemo.exe --platform=$(plat) --architecture=$(arch)

compile:
	msbuild BindingNemo.csproj

help:
	@mono bin/Debug/BindingNemo.exe --help
