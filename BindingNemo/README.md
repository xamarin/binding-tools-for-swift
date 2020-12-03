# Manual Binder Finder

## What is Manual Binder Finder

Manual Binder Finder is a tool created to aid in the Swift Binding Process.

Binding-Tools-For-Swift does a great job of binding many Swift APIs, but
there are some that cannot yet be bound with existing tools and require
manual bindings. Manual Binder Finder aims to make that process easier.

## What does it do

Manual Binder Finder works off the SwiftReflector to take a dynamic library
and create an .xml file containing a breakdown of the classes, structs,
enums, and protocols in that library.

## How to use it

There are two ways to use Manual Binder Finder.
- Running `mono Program.exe --library=<The name of the library you are targeting>`
- Running `make all`
    - the file libraries.mk contains a list of dylibs that make all will consume so you can add other dylibs here if you want to go this route

The newly created xml files can be found in Modules/<dylib name>.xml
and running `make clean` will clear out these xmls.
