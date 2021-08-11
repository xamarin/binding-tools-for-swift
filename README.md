# Xamarin Binding Tools For Swift

## Welcome!

This module is the main repository for Binding Tools for Swift.

This is a set of tools that can consume a compiled Apple Swift library and generates wrappers that allow it to be surfaced as a .NET library.

## Quickstart

Check out our [quickstart guide](QUICKSTART.md) to build and run the tool locally.

The packaging of BTFS is still evolving, and we expect to provide a binding project style interface to make this process easier in the future.

## Caution ❗

In order to contribute to Binding-Tools-For-Swift, you will need Xcode 12 or Xcode 13!

Binding Tools for Swift is currently in the process of moving to Swift 5.5.
At present, the code and tests appear to run correctly with either Swift 5.3 or Swift 5.5, however the new concurrency model (async/await/actor) is not yet supported.
Continuous integration is being done with Xcode 12.

## Current Status

### What Binds?

- Classes
- Structs
- Enums
- Protocols *without* associated types
- Top-level functions and variables
- Generic classes, structs, and enums
- Escaping closures
- Support of `@ObjC` types
- Protocol composition types in non-virtual methods
- Exceptions
- Extensions

### What Doesn’t Bind Yet?

- Protocols with associated types
- Bound generic types with closures
- Non-escaping closures
- Async function/methods/properties
- Actors

### What Else Can I Expect?

- An `open` class in Swift can be subclassed in C# and the subclass can be passed in to Swift. Overridden virtual methods in C# will be called when invoked from Swift.
- A C# type implementing an interface bound to a Swift protocol can be passed in to Swift. Methods and properties in the C# interface implementation will be called when invoked from Swift.
- At runtime, the generated code honors the Swift Automatic Reference Counting as well as .NET garbage collection.
- When writing bindings, the code generator tries hard to generate something. If an API uses a type that’s not supported yet, that API will be flagged and skipped.

## Technical Documentation

The [docs](https://github.com/xamarin/binding-tools-for-swift/tree/main/docs) directory contains a detailed walkthrough of how things work under the hood.

In particular the [functional outline](https://github.com/xamarin/binding-tools-for-swift/blob/main/docs/FunctionalOutline.md) is a great place to start exploring.

## Feedback

- Discuss development and design on [Gitter](https://gitter.im/xamarin/xamarin-macios)

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/xamarin/xamarin-macios?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## License

Copyright (c) .NET Foundation Contributors. All rights reserved.
Licensed under the [MIT](https://github.com/xamarin/binding-tools-for-swift/blob/main/LICENSE) License.
