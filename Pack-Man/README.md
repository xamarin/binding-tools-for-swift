```
 ____                _            __  __
|  _ \   __ _   ___ | | __       |  \/  |  __ _  _ __
| |_) | / _` | / __|| |/ / _____ | |\/| | / _` || '_ \
|  __/ | (_| || (__ |   < |_____|| |  | || (_| || | | |
|_|     \__,_| \___||_|\_\       |_|  |_| \__,_||_| |_|
```

The Binding Tools For Swift Pack-Man is a [cake](https://cakebuild.net) script that lists the files and directories needed by BTfS to be distributed and it creates a zip file that can be consumed by our NuGet shell once the zip file is uploaded to azure.

## Usage

You can simply execute `./build.sh` once you have followed the [bootstrap instructions](https://github.com/dalexsoto/maccore/blob/master/tools/tom-swifty/BUILDING.md) and built `tom-swifty.sln`.

You might want to supply `--configuration=VALUE` depending on the configuration you used to build our custom swift compiler.

|          Option          |                               Description                               |
|:------------------------:|:-----------------------------------------------------------------------:|
|                       -h | Shows help.                                                             |
|    --configuration=VALUE | Value can be `Debug` or `Release`, defaults to `Debug` if not supplied. |
| --output-directory=VALUE | Output directory name, defaults to `binding-tools-for-swift` if not supplied.     |
|           --target=Clean | Removes the packager output directory and zip file.                     |
|   --target=CreatePackage | `CreatePackage` is the default target that creates the SoM package.     |

## List of files and directories

The current list of files and directories bundled in the final package are:

### Directories
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/sourcekitd.framework`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/sourcekitd.framework`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/appletvos`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/appletvos`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/appletvsimulator`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/appletvsimulator`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/clang`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/clang`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/iphoneos`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/iphoneos`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/iphonesimulator`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/iphonesimulator`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/macosx`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/macosx`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/migrator`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/migrator`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/shims`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/shims`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/watchos`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/watchos`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/swift/watchsimulator`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/swift/watchsimulator`


---
* Source:
    * `maccore/tools/tom-swifty/swiftglue/bin/Debug/appletv/FinalProduct/XamGlue.framework`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/appletv/XamGlue.framework`


---
* Source:
    * `maccore/tools/tom-swifty/swiftglue/bin/Debug/iphone/FinalProduct/XamGlue.framework`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/iphone/XamGlue.framework`


---
* Source:
    * `maccore/tools/tom-swifty/swiftglue/bin/Debug/mac/FinalProduct/XamGlue.framework`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/mac/XamGlue.framework`


---
* Source:
    * `maccore/tools/tom-swifty/swiftglue/bin/Debug/watch/FinalProduct/XamGlue.framework`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/watch/XamGlue.framework`


---
* Source:
    * `maccore/tools/tom-swifty/samples/helloswift`
* Destination:
    * `binding-tools-for-swift/samples/helloswift`


---
* Source:
    * `maccore/tools/tom-swifty/samples/foreach`
* Destination:
    * `binding-tools-for-swift/samples/foreach`


---
* Source:
    * `maccore/tools/tom-swifty/samples/piglatin`
* Destination:
    * `binding-tools-for-swift/samples/piglatin`


---
* Source:
    * `maccore/tools/tom-swifty/samples/propertybag`
* Destination:
    * `binding-tools-for-swift/samples/propertybag`


---
* Source:
    * `maccore/tools/tom-swifty/samples/sampler`
* Destination:
    * `binding-tools-for-swift/samples/sampler`


--

###Files
* Source:
    * `maccore/tools/tom-swifty/binding-tools-for-swift`
* Destination:
    * `binding-tools-for-swift/binding-tools-for-swift`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin/swift`
* Destination:
    * `binding-tools-for-swift/bin/swift/bin/swift`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin/swift-demangle`
* Destination:
    * `binding-tools-for-swift/bin/swift/bin/swift-demangle`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin/swift-stdlib-tool`
* Destination:
    * `binding-tools-for-swift/bin/swift/bin/swift-stdlib-tool`


---
* Source:
    * `maccore/tools/apple/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib/libswiftDemangle.dylib`
* Destination:
    * `binding-tools-for-swift/bin/swift/lib/libswiftDemangle.dylib`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/Dynamo.dll`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/Dynamo.dll`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/Dynamo.pdb`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/Dynamo.pdb`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/SwiftReflector.dll`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/SwiftReflector.dll`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/SwiftReflector.pdb`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/SwiftReflector.pdb`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/SwiftRuntimeLibrary.dll`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/SwiftRuntimeLibrary.dll`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/SwiftRuntimeLibrary.pdb`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/SwiftRuntimeLibrary.pdb`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/tom-swifty.exe`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/tom-swifty.exe`


---
* Source:
    * `maccore/tools/tom-swifty/tom-swifty/bin/Debug/tom-swifty.pdb`
* Destination:
    * `binding-tools-for-swift/lib/binding-tools-for-swift/tom-swifty.pdb`


---
* Source:
    * `maccore/tools/tom-swifty/SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.iOS.dll`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/SwiftRuntimeLibrary.iOS.dll`


---
* Source:
    * `maccore/tools/tom-swifty/SwiftRuntimeLibrary.iOS/bin/Debug/SwiftRuntimeLibrary.iOS.pdb`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/SwiftRuntimeLibrary.iOS.pdb`


---
* Source:
    * `maccore/tools/tom-swifty/SwiftRuntimeLibrary.Mac/bin/Debug/SwiftRuntimeLibrary.Mac.dll`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/SwiftRuntimeLibrary.Mac.dll`


---
* Source:
    * `maccore/tools/tom-swifty/SwiftRuntimeLibrary.Mac/bin/Debug/SwiftRuntimeLibrary.Mac.pdb`
* Destination:
    * `binding-tools-for-swift/lib/SwiftInterop/SwiftRuntimeLibrary.Mac.pdb`



