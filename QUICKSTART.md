# Quickstart

Clone the repository on a macOS machine.

    cd binding-tools-for-swift
    make
    cd tests/tom-swifty-test
    make

In order to run the binding tools, you will need a compiled Swift library which includes a `.swiftmodule`. Here are the options for the binding generator:

    steveh$ mono tom-swifty/bin/Debug/tom-swifty.exe --help
    Usage:
            tom-swifty.exe [options] -o=output-directory -module-name=ModuleName
            tom-swifty.exe --demangle symbol [symbol...]
    Options:
          --demangle             Demangles the given swift symbols, printing human
                                   readable trees of each symbol.
          --swift-lib-path=VALUE swift library directory path.
          --swift-bin-path=VALUE uses 'path' as the directory to search for the
                                   swift compiler
          --retain-xml-reflection
                                 keeps the xml reflection files generated from the
                                   swift module.
          --retain-swift-wrappers
                                 keeps the swift wrapper source code in the output
                                   directory.
          --pinvoke-class-prefix=VALUE
                                 use 'name' as a prefix for classes to hold
                                   PInvokes. Default is 'NativeMethods'
          --print-stack-trace    prints a stack trace with each error.
          --wrapping-module-name=VALUE
                                 sets the swift wrapper module name to 'wrap-name'.
          --module-name=VALUE    sets the name of the module that will be processed.
          --global-class-name=VALUE
                                 use 'name' as the name of a class to hold global
                                   functions and properties.
          --arch=VALUE           set the architecture to target. Default is 64.
      -L, --library-directory=VALUE
                                 searches in directory for dylib files; can be used
                                   multiple times.
      -M=VALUE                   [module-directory] searches in directory for
                                   swiftmodule files; can be used multiple times
      -C=VALUE                   [combined-directory] searches in directory for
                                   both dylib and swiftmodule files; can be used
                                   multiple times
          --type-database-path=VALUE
                                 searches in directory for type database files; can
                                   be used multiple times
      -o=VALUE                   [directory] write all output files to directory
          --unicode-mapping=VALUE
                                 XML file describing mapping from swift unicode
                                   identifiers to C# identifiers
      -v, --verbose              prints information about work in process.
          --version              print version information.
      -h, -?, --help             prints this message

If you run this on its own, you will likely run into the error `Unable to find the custom swift compiler. Try using --swift-bin-path.` This is because the tool `tom-swifty` needs a separate tool, the reflector, which can reflect on the types in a Swift module. This is the custom Swift compiler. It also needs access to the associated Swift libraries.

When you make binding-tools-for-swift, the build will bring down a copy of the built Swift reflector in the directory `/path/to/binding-tools-for-swift/SwiftToolchain-v3-GITHASH` where `GITHASH` is a git hash. The bin and lib paths are:

    /path/to/binding-tools-for-swift/SwiftToolchain-v3/GITHASH/build/Ninja-ReleaseAssert/swift-macosx-x86_64/bin
    /path/to/binding-tools-for-swift/SwiftToolchain-v3/GITHASH/build/Ninja-ReleaseAssert/swift-macosx-x86_64/lib

Since the binding tools need to compile Swift wrappers that use some runtime glue, it needs a reference 

A typical set of commands to generate bindings is:

    mono /path/to/tom-swifty.exe --swift-bin-path SWIFT_BIN_PATH --swift-lib-path SWIFT_LIB_PATH -o /path/to/output_directory -C /path/to/YOURLIBRARY.framework -C /path/to/binding-tools-for-swift/swiftglue/bin/Debug/PLATFORM/XamGlue.framework -module-name YOURLIBRARY

In this example `SWIFT_BIN_PATH` and `SWIFT_LIB_PATH` are paths to the Swift reflector build (see above). `YOURLIBRARY` is the name of the library you’re trying to bind. `PLATFORM` is one of `appletv`, `iphone`, `mac`, or `watch`.

If you add the argument `--retain-swift-wrappers`, the binding tools will leave a directory that contains the source to the Swift wrappers written during the compilation. If you add the argument `--retain-xml-reflection`, the binding tools will leaves a directory that contains output of the reflector in XML format.

The binding tools need the following to operate:

- the location of the custom compiler and its libraries
- the location of the Swift runtime glue framework for the platform you’re targeting
- the location of your framework
- the name of your framework

In addition, the tools can operator on separate `.swiftmodule` and `.dylib` files. This can be handled by using a `-M` argument for the `.swiftmodule` and `-L` for the library. It’s easier to use the `.framework` directory and a single `-C` argument.

## Building and Running Samples
Samples source code is in the `samples` directory, but are will not build and run there.
Instead you need to make a packaged build, from the root of binding-tools-for-swift execute the command:
	`make package`
this will leave a directory inside of the directory `Pack-Man` named `binding-tools-for-swift` which contains buildable samples.
Most samples can be built by executing `make` in their directory. To try out a sample, do `make runit` for most of the samples.

