VERSION := $(shell xmllint --xpath "/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']/text()" nuget/Binding.Tools.For.Swift.nuspec )
NUGET = binding-tools-for-swift.$(VERSION).nupkg

TEST_NUGET = tests/nuget/$(NUGET)

SAMPLE_MAC_BINDING = tests/csharp/mac/lib/sample-xm.csproj
SAMPLE_MAC_APP = tests/csharp/mac/app/sample-app-xm.csproj

SAMPLE_IOS_BINDING = tests/csharp/ios/lib/sample-ios.csproj
SAMPLE_IOS_APP = tests/csharp/ios/app/sample-app-ios.csproj

SOM_PATH := $(abspath $(CURDIR)/../Pack-Man/binding-tools-for-swift)

tests/csharp/ios/lib/bin/final/dev/sample-ios.dll:
	xcodebuild -sdk iphoneos11.2 -project tests/swift/testLib.xcodeproj/ build -target testLib -target testHigherLib -configuration Release
	nuget restore $(SAMPLE_IOS_BINDING) 
	msbuild $(SAMPLE_IOS_BINDING) /p:DeviceBuild=true
	mkdir -p tests/csharp/ios/lib/bin/final/dev
	mv tests/csharp/ios/lib/bin/Debug/sample-ios.dll tests/csharp/ios/lib/bin/final/dev/sample-ios.dll

tests/csharp/ios/lib/bin/final/sim/sample-ios.dll:
	xcodebuild -sdk iphonesimulator11.2 -project tests/swift/testLib.xcodeproj/ build -target testLib -target testHigherLib -configuration Release
	nuget restore $(SAMPLE_IOS_BINDING) 
	msbuild $(SAMPLE_IOS_BINDING)
	mkdir -p tests/csharp/ios/lib/bin/final/sim
	mv tests/csharp/ios/lib/bin/Debug/sample-ios.dll tests/csharp/ios/lib/bin/final/sim/sample-ios.dll

test-ios-dev:: $(TEST_NUGET) tests/csharp/ios/lib/bin/final/dev/sample-ios.dll
	nuget restore $(SAMPLE_IOS_APP) 
	msbuild $(SAMPLE_IOS_APP) /p:Platform=iPhone /t:Clean,Build

test-ios-sim:: $(TEST_NUGET) tests/csharp/ios/lib/bin/final/sim/sample-ios.dll
	nuget restore $(SAMPLE_IOS_APP) 
	msbuild $(SAMPLE_IOS_APP) /t:Clean,Build

test-mac:: $(TEST_NUGET)
	xcodebuild -sdk macosx10.13 -project tests/swift/testLib.xcodeproj/ build -target testLib -target testHigherLib -configuration Release
	nuget restore $(SAMPLE_MAC_BINDING) 
	msbuild $(SAMPLE_MAC_BINDING)
	nuget restore $(SAMPLE_MAC_APP) 
	msbuild $(SAMPLE_MAC_APP)
	install_name_tool -add_rpath @executable_path/../MonoBundle tests/csharp/mac/app/bin/Debug/sample-app-xm.app/Contents/MacOS/sample-app-xm
	./tests/csharp/mac/app/bin/Debug/sample-app-xm.app/Contents/MacOS/sample-app-xm

clean::
	msbuild $(TASK_CSPROJ) /t:clean
	msbuild $(SAMPLE_MAC_BINDING) /t:clean
	msbuild $(SAMPLE_MAC_APP) /t:clean
	msbuild $(SAMPLE_IOS_BINDING) /t:clean
	msbuild $(SAMPLE_IOS_APP) /t:clean
	xcodebuild -sdk macosx10.13 -project tests/swift/testLib.xcodeproj/ clean
	rm -r tests/nuget/

$(TEST_NUGET): nuget/bin/$(NUGET)
	mkdir -p tests/nuget/
	cp nuget/bin/$(NUGET) $(TEST_NUGET)

TASK_CSPROJ = nuget/Tasks/Binding.Tools.For.Swift.Tasks.csproj
TASK_DLL = nuget/Tasks/bin/Debug/Binding.Tools.For.Swift.Tasks.dll
NUSPEC = nuget/Binding.Tools.For.Swift.nuspec

$(TASK_DLL): Makefile $(TASK_CSPROJ) $(wildcard nuget/Tasks/*.cs) $(wildcard nuget/build/*)
	dotnet nuget locals all --clear
	nuget restore $(TASK_CSPROJ)
	msbuild $(TASK_CSPROJ)

nuget/bin/$(NUGET): $(NUSPEC) $(wildcard nuget/build/*) $(TASK_DLL)
	nuget pack -OutputDirectory nuget/bin $(NUSPEC)

nuget:
	$(MAKE) nuget/bin/$(NUGET)
	@rm -Rf local-feed
	@mkdir -p local-feed
	nuget add nuget/bin/$(NUGET) -source local-feed
	@printf "\n*** Local NuGet feed available at $(CURDIR)/local-feed - you can add this to Visual Studio for Mac's Nuget sources (Preferences -> NuGet/Sources) to test packages. ***\n\n"

.PHONY: nuget