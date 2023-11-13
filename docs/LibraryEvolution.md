# Library Evolution

As part of Binding Tools for Swift, it is necessary to consume swift libraries that
have been compiled with `enable-library-evolution`. There are a few of reasons that
precipitated that decision.

In the past we hacked to swift compiler to generate reflection information for a module.
This worked to a point but was fragile. It was susceptible to internal changes in the compiler
and it only worked on code that was compiled with exactly the same compiler version as what the
module was compiled with. This worked up until a point that Apple released a version of their
SDK that included a compiler that had a different version than the release of the compiler
on github, so it was no longer compatible.

At this point we decided to derive reflection information from `.swiftinterface` files generated
by the compiler and these files are only generated with `enable-library-evolution`. Fortunately
all of modules from Apple are compiled with this setting.

Because the current iteration of BTfS uses wrapper methods for most everything, no changes
were needed to support library evolution because the approach to wrapping uses pointers
aggressively instead of passing by value. This results in a very neutral interface not
affected by Swift's ABI which is incompatible with the standard platform ABI used by .NET.

With changes to the .NET runtime, the wrapping may no longer be necessary.

There are 3 general cases that we need to be concerned with:

- heap allocated types
- value types marked as `@frozen`
- value types

In these cases, there several subconditions that we need to take into account:

- return values
- typical arguments
- inout arguments
- instance values for methods
- instance values for mutating methods

I'm going to break these down per type:

## heap allocated types
- return values are passed by returning a pointer to the instance
- typical arguments are passed using a pointer to the instance
- inout arguments are passed using a pointer to a pointer to the instance
- instance values are passed in the "self" register (R13)
- n/a

## value types marked as `@frozen`
- return values are passed directly if they fit in 4 registers, otherwise the caller allocates
  space on the stack for the return
- typical arguments are passed directly if they fit in 4 registers, otherwise they a copy
  gets passed by reference
- inout arguments are passed by reference
- instance values are passed using standard registers if they fit in 4 registers otherwise a copy
  is passed in the self register
- instance values are passed by reference in the self register

## value types not marked as frozen
- return values are allocated by the caller and a pointer gets passed in to that space (note
  that the size can and must be determined at runtime by the caller)
- typical arguments are copied and passed by reference
- inout arguments are passed by reference
- instance values are passed by reference as a copy passed in the self register
- instance values are passed by reference in the self register (no copy)