# Tests for 3rd-party libraries

This directory contains tests for 3rd-party libraries.

## How to find a library project.

Look in [PROJECTS](PROJECTS.md) for a repository without a comment. Notify in
\#swift-o-matic which repository you're working on (and also check if someone
else is already working on the same repository first).

If there are no projects left, go hunt for more!

## How to create a new test

Get the https link for the GitHub repository, and pass it to create-test.sh:

```shell
$ ./create-test.sh https://github.com/owner/test
```

This will:

* Create the subdirectory for the test.
* Copy the [template Makefile](Makefile.template) and fill in the blanks
  according to the repository.
* Try to build the new test.

Usually something goes wrong, these are the most common reasons:

1. There are more than one Xcode project in the repository.

    `create-test.sh` will guess a location for the Xcode project, and if it's
    not there, it will look for a single Xcode project anywhere and use that
    instead. However, if there are multiple Xcode projects, and none in the
    default location, you'll have to edit the generated Makefile and point it
    to the right location.

    Example for the `RazzleDazzle` test:

	```make
	XCODEPROJECT=repository/Example/RazzleDazzle.xcodeproj
	```

2. The Xcode scheme to build isn't the default one (the name of the repository).

   This is simple to solve, go the test directory and execute `make list-schemes`.

   Example for the `Euler` test:

   ```shell
   $ cd Euler
   $ make list-schemes
   Listing schemes in ./repository/Euler.xcodeproj
   Information about project "Euler":
       Targets:
           Euler
           EulerPackageDescription
           EulerPackageTests
           EulerTests

       Build Configurations:
           Debug
           Release

       If no build configuration is specified and -scheme is not passed then "Release" is used.

       Schemes:
           Euler-Package
    ```

    The schemes are listed at the end: in this case the scheme to use is `Euler-Package`.

    Finally edit the Makefile accordingly:

    ```make
    XCODESCHEME=Euler-Package
    ```

3. The framework name isn't the default one (which is the same as the scheme).

   This is simple to solve, go to the test directory, try to build, check the
   name of the framework Xcode built:

   ```shell
   $ cd Macaw
   $ make
   [...]
   [GEN]  Macaw.framework
   The simulator version of the framework (/work/swift-o-matic/maccore2/tools/tom-swifty/tests/3rd-party/Macaw/bin/iphonesimulator/Macaw /work/swift-o-matic/maccore2/tools/tom-swifty/tests/3rd-party/Macaw/iOS.framework) does not exist.
   The device version of the framework (/work/swift-o-matic/maccore2/tools/tom-swifty/tests/3rd-party/Macaw/bin/iphoneos/Macaw /work/swift-o-matic/maccore2/tools/tom-swifty/tests/3rd-party/Macaw/iOS.framework) does not exist.
   cp: /work/swift-o-matic/maccore2/tools/tom-swifty/tests/3rd-party/Macaw/bin/iphoneos/Macaw /work/swift-o-matic/maccore2/tools/tom-swifty/tests/3rd-party/Macaw/iOS.framework: No such file or directory
   $ ls -la bin/iphoneos
   total 0
   drwxr-xr-x  3 rolf  wheel   96 Dec 13 10:10 .
   drwxr-xr-x  6 rolf  wheel  192 Dec 13 10:10 ..
   drwxr-xr-x  6 rolf  wheel  192 Dec 13 10:10 Macaw.framework
   ```

   Finally modify the Makefile accordingly:

   ```make
   FRAMEWORKNAME=Macaw
   ```

4. The Xcode project doesn't build because the Swift code has been upgraded to Swift 4.2 / Xcode 10.

   Look in the history for the commit previous to the Swift 4.2 upgrade (I use
   `gitk` and just look for the last commit before this summer/autumn), and
   set the HASH variable to the corresponding hash:

   Example for the `Euler` test:

   ```make
   # Modify the hash if the latest won't compile (for instance if it requires Swift 4.2 / Xcode 10)
   HASH=64cebc34c856638296878804308dbf6a457f89f6
   ```

If the test still fails:

1. Uncomment `IGNORED=1` in the Makefile.
2. Create a failed build log and commit that as well.

   From the top directory:

   ```shell
   $ cd test
   $ make build-save-failed-log
   ```

## General structure

The top-level Makefile has a three main targets:

1. `build`: Builds all tests.
2. `run`: Runs all tests.
3. `all`: Builds and runs all tests parallelized.

   This is the default target if just executing `make`.

   Both the `build` and `run` targets can be parallelized by passing `-jX` to
   make, but they're not parallelized by default).

Each test subdirectory has a Makefile, which must define two targets:

1. `build-local`: Build the test.
2. `run-local`: Run the test.

The only other requirement for each test's Makefile is that if it doesn't
work, it must set `IGNORED=1` somewhere (in a line by itself), so that the
top-level Makefile can detect tests that don't work.

The top-level directory has a `Makefile.inc` file, with reusable build and run
logic. If this file is included from a test's Makefile, the build and run
logic can usually be forwarded to a default build and run implementation if a
few variables are set to configure each test's particularities. There are
plenty of examples (see [Euler/Makefile](Euler/Makefile) for instance).

Test repositories are cloned into a 'repository' subdirectory.

Make targets must be parallel-safe.

