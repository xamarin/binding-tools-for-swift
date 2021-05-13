# Manual Binder Finder

## What is DylibBinder

DylibBinder is a tool created to aid in the Swift Binding Process.

Binding-Tools-For-Swift does a great job of binding many Swift APIs, but
there are some that cannot yet be bound with existing tools and require
manual bindings. DylibBinder aims to make that process easier.

## What does it do

DylibBinder works off the SwiftReflector to take a dynamic library
and create an .xml file that reflects the structs, classes, enums, and protocols
contained inside the dylib.

## How to use it

### From the Command Line

1. Run 'make' inside the /xamarin-macios/DylibBinder repo
1. Run 'mono bin/Debug/DylibBinder.exe --dylibPath=<Path/To/Dylib> --outputPath=<Path/To/Desired/Xml/Output> --swiftVersion=<SwiftVersion>'
    1. [Required] --dylibPath=<Path/To/Dylib>
    1. [Required] --outputPath=<Path/To/Desired/Xml/Output>
        1. Be sure that the extension to your output path is '.xml'
    1. [Optional] --swiftVersion=<SwiftVersion>
        1. If not present, swiftVersion will default to version 5.0
    1. [Optional] --ignoreListPath=<Path/To/IgnoreList>
        1. This list will be a txt file with Types that you want to not include in the reflection xml
        1. An example is in DylibBinder/IgnoreList.txt which you can include here

### From DylibBinderReflector.cs

* You can reference DylibBinder in your project and call the method
    'DylibBinderReflector.Reflect (dylibPath, outputPath, swiftVersion)'
    * 'dylibPath' is a string with the path to the dylib
    * 'outputPath' is a string to the path where you want the reflected xml to go
        * the extension to the file should be ".xml"
    * 'ignoreListPath' is a path to a file that includes Types you do not want included in the reflection xml
        * DylibBinder/IgnoreList.txt is a nice example that you can expand and/or include here
    * 'swiftVersion' is a string with the swiftVersion
        * default value is "5.0"
