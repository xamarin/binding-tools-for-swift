# Binding the Euler Project


Here are the steps that I took to bind Euler with Binding Tools for Swift.

First, I have a branch of the Euler project on GitHub that has a few minor changes to it:
https://github.com/stephen-hawley/Euler/tree/adjustments

*NOTE* - the files in this folder are named EulerPhone and EulerPhoneApp (as opposed to Euler and Euler app.
This was due to order of operations - I didn't branch the Euler project until after I written the sample.

*VERY IMPORTANT*
**USE Xcode 9.2 ONLY**

The changes are 3-fold:

1. I embedded the code into a cocoa touch framework
2. I updated the syntax to be Swift 4 friendly
3. I commented out a few of the more esoteric operators that I couldn’t figure out how to get to be Swift 4 friendly

Next - I built a universal framework from the project. This is less obvious than it should be, but so be it. I used a shell script as follows:

    #!/bin/bash
    
    # build the simulator version to a known location
    xcodebuild -quiet -project Euler/Euler.xcodeproj -configuration Debug -target Euler -sdk iphonesimulator ONLY_ACTIVE_ARCH=NO clean build
    
    # build the iphone version to a known location
    xcodebuild -quiet -project Euler/Euler.xcodeproj -configuration Debug -target Euler -sdk iphoneos ONLY_ACTIVE_ARCH=NO build
    
    UNIVERSAL=Euler/build/Debug-universal
    # clean previous (if any)
    rm -rf $UNIVERSAL
    mkdir -p $UNIVERSAL
    
    # build the structure using the iphone build as the starting point
    cp -R $UNIVERSAL/../Debug-iphoneos/Euler.framework $UNIVERSAL
    
    # merge the two output libraries into the output
    lipo -create -output $UNIVERSAL/Euler.framework/Euler $UNIVERSAL/../Debug-iphonesimulator/Euler.framework/Euler $UNIVERSAL/../Debug-iphoneos/Euler.framework/Euler
    
    #ensure that Euler looks for libraries in the executable folder
    install_name_tool -add_rpath @executable_path $UNIVERSAL/Euler.framework/Euler
    
    # copy the simulator swiftmore into place
    cp $UNIVERSAL/../Debug-iphonesimulator/Euler.framework/Modules/Euler.swiftmodule/* $UNIVERSAL/Euler.framework/Modules/Euler.swiftmodule
    
    # stage the output
    cp -r $UNIVERSAL/Euler.framework .

Next step was to run binding-tools-for-swift on the universal framework. I did this from a makefile:


    $(BINDINGTOOLSFORSWIFT) --verbose --swift-bin-path $(SWIFT_BIN) --swift-lib-path $(SWIFT_LIB) --retain-swift-wrappers --type-database-path=../../bindings/ -o $(BINDINGTOOLSFORSWIFTOUTPUT) -C Euler.framework -C ../../swiftglue/bin/Debug/iphone/FinalProduct/XamGlue.framework -module-name Euler

With a properly installed binding-tools-for-swift, this will happen with far fewer arguments (all the `--…path` arguments get found automatically as well as XamGlue).

This generates `XamWrapping.framework` as well as `EulerTopLevelEntities.cs`

Next I built an iPhone app in VS Mac and added a reference to `SwiftRuntimeLibrary.iOS.dll`. I added native references to `Euler.framework`, `XamWrapping.framework` and `XamGlue.framework`.
**Important:** check “allow ‘unsafe’ code” in the project options→General. binding-tools-for-swift generates all manner of ‘unsafe’ code for marshaling.

Finally, I added links to the libraries needed by `Euler.framework` to the `Resources` of the project. There used to be a clever hack done by Israel Soto that could be added to the .csproj to make this automatic, but it has apparently gone stale since I last tried it, and there is likely a better approach than this (and there should be, because this is not a reasonable thing to ask the customer to do).

By running `otool -l Euler.framework/Euler` you get a list of the Apple libraries that Euler depends upon.  All the ones that start with an `@rpath` are ones we care about. We do this by going to `/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/iphoneos/` or `/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/iphonesimulator/`
And adding in the libraries to the Resource folder of the VS project.

I put in a little UI to display e, pi, and tau and calculate square and cube roots:


    using System;
    using EulerPhone;
    using Foundation;
    using UIKit;
    
    namespace EulerPhoneApp {
            public partial class ViewController : UIViewController {
                    protected ViewController (IntPtr handle) : base (handle)
                    {
                            // Note: this .ctor should not contain any initialization logic.
                    }
    
                    public override void ViewDidLoad ()
                    {
                            NSNotificationCenter.DefaultCenter.AddObserver (UITextField.TextFieldTextDidChangeNotification, (notification) =>
                            {
                                    if (notification.Object == numberField) {
                                            var val = 0.0;
                                            if (Double.TryParse(this.numberField.Text, out val) && val >= 0) {
                                                    this.sqrtValue.Text = TopLevelEntities.PrefixOperatorSquareRoot (val).ToString ();
                                                    this.cubeRootValue.Text = TopLevelEntities.PrefixOperatorCubeRoot (val).ToString ();
                                            }
                                    }
                            });
                            base.ViewDidLoad ();
                            eValue.Text = TopLevelEntities.LittleEpsilon.ToString ();
                            piValue.Text = TopLevelEntities.Π.ToString ();
                            tauValue.Text = TopLevelEntities.Tau.ToString ();
                    }
    
                    public override void DidReceiveMemoryWarning ()
                    {
                            base.DidReceiveMemoryWarning ();
                            // Release any cached data, images, etc that aren't in use.
                    }
            }
    }



## Things That Can Go Wrong (and what to do)

I ran into a bunch of problems in getting here, most of which is covered in the process, but it’s still worth discussing what I learned and how I determined what the problem was.

**The app fails quickly on simulator**. I ran the app, watched it deploy, got a white screen, the debugger gave up and about 4 seconds later, the app was brought down on the simulator.
For this, I looked into `~/Library/Logs/DiagnosticReports/` for the app which will have the crash dump. I saw two classes of failure:

1. Unable to find a library (typically libswiftCore.dylib)
2. Incompatible library version (also typically libswiftCore.dylib)

The first was because the library wasn’t present - putting it in the resource folder will end up putting it into the executable directory, which is good enough for our purposes. In the BuildUniversal script, there is a line that adds the executable path as a place to look for libraries.

The second was because I was of the way that frameworks were being build by binding-tools-for-swift. We were adding in a library link directory argument for the swift compiler (`-L pathToOurCustomBuildSwiftCompilerLibary` ) and the swift compiler embedded the entire absolute path into the output, which is precisely the wrong thing to do. SoM was updated to remove this path from the output. This issue should no longer be a problem.

**The app won’t build.**
Remember to include a reference to `SwiftRuntimeLibrary.iOS.dll` I forgot this.
Remember to allow ‘unsafe’ code.
Remember to include the required native references.

When binding-tools-for-swift wraps a library, a consuming application is required to have:
SwiftRuntimeLibrary.*platform*.dll
XamGlue.framework
original native swift framework
wrapping library

Eventually, I would expect that all of this will be automatic, as well as the command line, but for now, this is what we need.

**The Project File Hack**

There is a gist of the change that you can try here: https://gist.github.com/dalexsoto/e1878713a15b4091215dc50d720afa49

it uses `swift-std-lib-tool`

In my last try at using this, the tool was giving me impenetrable errors.


