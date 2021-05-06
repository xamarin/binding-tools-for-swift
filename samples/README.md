# Building and Running the Samples

Each sample lives in its own directory. Except for the `piglatin` sample, all samples are
shell applications. To build and run them do the following:
```
cd sample-name
make
make runit
```

# What Each Sample Does

- helloswift - Defines a top-level function that prints "Hello, world." and calls it from C#
- propertybag - Defines a generic class named PropertyBag that maps a Swift string onto a boolean and exercises it from C#
- sampler - Defines a variety of swift types including a final class, an inner struct, a static function and property, a get only propery, a subscript, and an enum and exercises them from C#
- sandwiches - Defines protocols for creating sandwiches and concrete implementations of them. The C# code shows to implement these with interfaces and transparently call a swift function that uses the protocols.
- piglatin - an iphone app that implements a translator from normal words to pig latin.

# What's In the Makefiles and Why

The rule in the Makefile that copies a flavor of SwiftRuntimeLibrary.dll. There are several flavors of this library for the platform:
- SwiftRuntimeLibrary.dll - this is for running apps from the shell.
- SwiftRuntimeLibrary.Mac.dll - this is for running MacOS apps
- SwiftRuntimeLibrary.iOS.dll - this is fro running iOS apps

There are two lines that make a directory XamGlue.framework and copies XamGlue into it. XamGlue is a set of runtime utilities that get used for handling marshaling and type handling as well as some of the basic types such as Dictionary, Array, Set etc.

There is a line that compiles all swift files. It is important to pass the following flags to the compiler:
	- `-emit-module` - the creates a swift module as the output
	- `-emit-library` - builds the code as a library
	- `-enable-library-evolution` - builds the code in a way that uses a stable ABI
	- `-emit-module-interface` - builds the code that generates a `.swiftinterface` file that is used for reflection
	- `-sdk /path/to/platform/sdk` - tells the compiler which SDK to compile against
	- `-L /path/to/platform/sdk/libraries` - tells teh compiler where to look for the libraries
	- `-F .` - tells the compiler where to look for frameworks
	- `-framework XamGlue` - this is not strictly necessary for building the library, but shows how to reference another framework

There is a line that then runs Binding Tools for Swift (BTfS) on the built library. The arguments that get passed in are
	- `--retain-swift-wrappers` - this keeps a copy of the source generated for swift wrappers. This is not imperative, but it useful if you want to see what BTfS generates
	- `-o .` tells BTfS where to put output files
	- `-C .` tells BTfS where to look for `.dylib` and `.swiftinterface` files
	- `-module-name name` tells BTfS what module to create bindings for

The final line in the rule is used to compile the C# sources into an executable.

Finally the `runit` rule invokes mono on the executable. It is necessary to tell mono where to look for dynamic libraries. In this case, it needs a path to the Apple Swift runtime libraries, the current working directory (to find the wrapping library as well as the original Swift library), and the XamGlue.framework.
