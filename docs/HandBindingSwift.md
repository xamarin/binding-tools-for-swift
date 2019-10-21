# Manually Binding Swift Types Into XamGlue
If you’re reading this, you’re trying to figure out how to manually bind a swift type.

First, can this even be done?
Of course! It’s just that Binding Tools for Swift does it far faster (and hopefully someday better). Keep in mind always that in order for Binding Tools for Swift to work on any given type or API in swift, I had to figure out how to do this on my own and I usually started by doing the work entirely manually.

If you can’t run Binding Tools for Swift on a given library, it means that there is no `.swiftmodule` file (thanks, swiftCore) or you’re getting an error that we can’t work around for some reason (I’m looking at you, associated types).

So how do I do this?
The steps are easy:

1. Create your own framework
2. Implement the type(s) you want to bind in that framework modulo some changes
3. Build the framework
4. Run Binding Tools for Swift and collect the output
5. Move the wrappers into XamGlue
6. Build XamGlue
7. Pull up XamGlue in Hopper and get the entry point names
8. Adjust the C# binding to use those names
9. Write tests

Yes, that’s a lot of steps.  I’m going to go through each of them and explain them out.


## Steps 1-5

There are several ways to do this. You can do it in Xcode if you really want. If the types have no platform dependencies (eg, UIKit), then write a unit test and use the existing Binding Tools for Swift testing infrastructure to do this steps 1 and 3 for me.  The way I do this is to write a test that looks like this:

    [Test]
    public void NotActuallyATest ()
    {
        var swiftCode = @"
    public struct SomeType { // whatever
    }
    ";
        TestRunning.TestAndExecute (swiftCode, new CSCodeBlock (), "");
    }

NB: I don’t always include all the APIs on a type that are in Swift because (1) they don’t always make sense in C# and (2) some of them tend to be chatty and don’t get you much. If you look at `SwiftString` you can see that this is a very bare-bones implementation and that’s by design. The string manipulation in swift is truly horrid and we don’t want to encourage it in C# when C# has its own more familiar idioms.

Then I set a breakpoint on in `TestRunning.TestAndExecute` on the line:

                                    var compilerWarnings = Compiler.CSCompile (tempDirectoryPath, sourceFiles, "NameNotImportant.exe", platform: platform);

and debug the test. When I hit that line, I go to the directory `tempDirectory` and in `XamWrappingSource` is all the generated swift wrappers.

I move the wrappers in XamGlue and name the file something like `mytypehelpers.swift`. I will rename the wrappers since they might have to be read by a human. The style I typically use is the pattern `nounVerb` where `noun` is the type and `Verb` is the action. So for example, the constructor wrapper my get named `XamWrapper_MyTypeDMyType` (which is a mangled version of `XamWrapper.MyType.MyType`) to `mytypeNew`.


## Step 6

In the swiftglue directory, run make.


## Step 7

I drag the Mac build onto Hopper and it pulls up the disassembly. Hopper is very nice in that the search tool searches over the demangled description of the entry points. For a value type, you will need the following for any type foo:

1. nominal type descriptor for foo
2. metadata accessor for foo
3. metadata for foo (not always present)

Each of these can be found via Hopper’s search.
You will also need the names of the main entry points into XamGlue for that type.
You can use `nm` on the library and it will dutifully provide all the symbols, but they will be mangled. You can also do `nm lib.dylib | xcrun swift-demangle` to get the demangled symbols, but you will have to correlate that back to the original symbols. It's possible and additional scripting could make it better, but Hopper is definitely easier since it searches the demangled symbols and if you click on the demangling, it shows you the actual symbol.


## Step 8

Now go back to the C# binding generated initially by Binding Tools for Swift and substitute in the entry points from step 7. These should go into string constants in `XamGlueConstants.cs` or something similar.


## Step 9

Tests are up to you. For my purposes, I try to write non-trivial tests that exercise each of the APIs. Binding Tools for Swift tests are almost all written such that we write swift that gets compiled into a framework and we write some C# calling code. Then that swift code gets wrapped/bound by Binding Tools for Swift and the C# code gets compiled into a shell application to call that code and the C# code generates text output. The test runner asserts on that text output.

Caveats: The act of running a test using the existing infrastructure also generates a test that gets run on iOS in a single application.
Therefore, all swift function names and test names need to be unique, otherwise the iOS app won’t compile.
Typically the test name is a function that derives from the unit test name. This gets done automatically through a stack crawl. However, if you use the `[TestCase(…)]` attribute, you will get a runtime error.
Output is generated by C# in almost all cases. This is because it’s way easier to collect the output on a device from C# than from swift. Avoid printing from swift for that reason. You can, but you have to jump through hoops to collect output (and there are tests that do this), but it involves writing the output to a file on device which gets picked up by the C# in the testing infrastructure and written out.


