# Ideas for the Future

## Avoid secondary compilation
When we write wrappers for various types and functions or to provide default implementations
for protocols (extensions on EveryProtocol) or to provide wrappers for virtual classes, we
presently run a swift compilation cycle. This is necessary because we let the swift compiler
generate the mangled names which we then need to consume. There are a number of problems
with this: we have to generate an identical swiftmodule as the original, which includes compiling
for the final architecture. This is a task that is better left to being done by a shell script
or a Makefile, not from within C#. It also slows down the overall process.

To avoid this, we can take advantage of the `@_silgen_name` attribute. This lets us put a
specific symbol name onto a function which will decouple the swift demangler. The some issues
to be aware of: you can't set a symbol name for initializers and we have to create our own
mangler/namer. Initializers can be handled with a function like this:

```swift
public func SomeTypeInit (/* args */) -> SomeType {
    return SomeType (/* args */)
}
```

The nice thing about this compared to other wrappers that we currently write is that the
registers will be aligned perfectly since the constructor wants the type metadata in the
`self` register and the rest of the args follow as per normal.

One downside is that the output of BTfS ends up being a set of swift files and there leaves
an additional step to compile these into a module. Our C# will reference entry points through
p/invoke calls which need to know, a priori, the name of the swift module that will contain
the entrypoints. We can suggest what the wrapper module should be named, but we need to
guarantee what the wrapper module will be called or p/invokes can't be resolved and we'll
have grumpy customers.

Another downside is that swift attributes that start with an underscore are fragile. They
may end up in the language in the future. Or they may not.

## Replacing NewClassCompiler and MarshalEngines
These are by far the worst sections of code BTfS, mostly because of how they grew and
were affected by special cases.

Instead, what I think we need is a different architecture that is informed by all the
special cases that we encountered.
To that end, the general process of writing a method in C# is to do something like this:

```
foreach arg in the C# function
    figure out its type
    maybe write C# to initialize a local that will be used as the expression in the pinvoke
    write one or more C# expressions that will be passed to the pinvoke (more will be needed for
        flattened tuples)
    maybe inject a try catch and do exception translation
    pass expression to pinvoke
    maybe write C# to deinitialize the local or copy it back from the pinvoke
handle return marshaling
```
It gets ugly especially if unsafe and fixed blocks are needed.

Marshaling the parameters to a simple function call is 270 lines of code that don't read.

What we should have is an engine type that defines a series of functions for handling each individual
marshal case in complete isolation. These would include being able to set up a fixed block,
flag the need for unsafe code, write arbitrary pre-marshal code, write an expression for the
argument, write arbitrary post martial code, define extra arguments for generic type metadata
and protocol witness tables, and leave all of this in a context object.

The calling code should just be able to loop through all the arguments, select and engine,
call it to generate code, and then aggregate all the code for all the arguments.
