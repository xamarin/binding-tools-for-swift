# Building Binding Tools for Swift from sources

## Requirements

### Install Xcode

There are two options to installing Xcode:

1. Use automatic provisioning. This is done by executing `make provision` in `maccore/tools/tom-swifty`, but it may take a while to download Xcode unless you're in the Boston office, in which case it might be better to download Xcode manually (next option).

2. Download Xcode manually from https://download.developer.apple.com/Developer_Tools/Xcode_9.2/Xcode_9.2.xip

	1. Extract and copy Xcode_9.2.xip to `/Applications/Xcode_9.2.app` (if you already have Xcode 9.2 in `/Applications/Xcode92.app`, you can just create a symlink: `ln -s /Applications/Xcode92.app /Applications/Xcode_9.2.app`)
	2. `sudo xcode-select -s /Applications/Xcode_9.2.app/`

### Notes

* there are other requirements (e.g. `cmake`, `ninja`) that I already have, most of them are likely needed to build `xamarin-macios`. It’s possible that I have some for others reasons too. Please update this document if you find any missing requirements.


## Building

Create a directory to dedicate to Binding Tools for Swift, e.g.

1. `mkdir ~/git/binding-tools-for-swift`
2. `cd ~/git/binding-tools-for-swift`

Clone maccore

1. `git clone git@github.com:xamarin/maccore`

Build everything. This will build the swift dependency (only if needed [1]) and binding-tools-for-swift.

1. `cd tools/tom-swifty`
2. `make`

[1] The build script will first check if a prepackaged version of the swift
toolchain is available in Azure, and if so, download and use that version.
This behavior can be overriden by doing `export FORCE_SWIFT_BUILD=1`.

### Notes

The above steps are doing a **full debug** build for swift. [build-script](https://github.com/xamarin/swift/blob/swift-4.0-branch-tomswifty/utils/build-script) can also produce different builds, e.g.

* `-d | --debug`: full debug (like above)
* `-R | --release`: release
* `-r | --release-debuginfo`: release with debug info

Running `build-script` takes a **very long** time, so building extraneous local configurations is optional.

### Running Unit Tests

1. `cd maccore/tools/tom-swifty/tests/tom-swifty-test/`
2. `make`

### Notes

* The new (incompatible but much more powerful) NUnit 3 runner means using the usual `FIXTURES` variable has to use a [different syntax](https://github.com/nunit/docs/wiki/Test-Selection-Language). E.g. to run a single test case from the command line you would do

```
FIXTURES="--where=test=SwiftReflector.LinkageTests.TestMissingNSObject" make
```


## Generated Source Files
- `SwiftReflector/IOUtils/SwiftModuleList.g.cs` is generated via the `update_module_list.csharp` script. 
	- `./update_module_list.csharp SwiftToolchain-v2-8efccc1464890d6c906fb2c40f909b5324da950d`