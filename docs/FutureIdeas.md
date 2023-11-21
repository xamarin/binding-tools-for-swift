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

Marshaling the parameters to a simple function call is 270 lines of code that doesn't
read well.

What we should have is an engine type that defines a series of functions for handling each individual
marshal case in complete isolation. These would include being able to set up a fixed block,
flag the need for unsafe code, write arbitrary pre-marshal code, write an expression for the
argument, write arbitrary post martial code, define extra arguments for generic type metadata
and protocol witness tables, and leave all of this in a context object.

The calling code should just be able to loop through all the arguments, select an engine,
call it to generate code, and then aggregate all the code for all the arguments.

## Exposing C# to Swift

There are many use cases where we might want to expose C# to Swift. For example, if Apple
has UI design tools that loop over objects to look for properties to expose in the tooling
we would certainly want to be able to define those properties in C# and have the Apple tools
pick them up.
There are two ways that might happen: through inheritance from a base class (think UIView) or
through implementation of a protocol.

Today, when a class that can be inherited gets implemented in C# there are two things that
happen under the hood. The first is that a class will be defined in Swift that vectors all
the calls through a simulated vtable.

Here's a simple example from existing unit tests:
```swift
open class MontyWSMBool {
    public init() {}
    open func val() -> Bool { return true; }
}
```
This class gets overriden in swift like so:
```swift
public final class xam_sub_MontyWSMBool : MontyWSMBool
{
    private var _xamarinClassIsInitialized: Bool = false;

    private static var _vtable: MontyWSMBool_xam_vtable = MontyWSMBool_xam_vtable();
    fileprivate static func setMontyWSMBool_xam_vtable(vtable: MontyWSMBool_xam_vtable)
    {
        _vtable = vtable;
    }
    public override init()
    {
        super.init();

        _xamarinClassIsInitialized = true;
    }
    fileprivate final func xam_super_val() -> Bool
    {
        return super.val();
    }
    public override func val() -> Bool
    {
        if _xamarinClassIsInitialized && xam_sub_MontyWSMBool._vtable.func0 != nil
        {
            return xam_sub_MontyWSMBool._vtable.func0!(toIntPtr(value: self));
        }
        else
        {
            return super.val();
        }
    }
    fileprivate struct MontyWSMBool_xam_vtable
    {
        fileprivate var func0: (@convention(c)(UnsafeRawPointer) -> Bool)?;
    }
}
public func setMontyWSMBool_xam_vtable(uvt: UnsafeRawPointer)
{

     let vt: UnsafePointer<xam_sub_MontyWSMBool.MontyWSMBool_xam_vtable> = fromIntPtr(ptr: uvt);
     xam_sub_MontyWSMBool.setMontyWSMBool_xam_vtable(vtable: vt.pointee);
}
```
The class implements the constructor and overrides the virtual method, sending it to a function
pointer which ends up in C#.
BTfS generates a C# class that looks like this (leaving out most implementations for brevity):
```csharp
    [SwiftNativeObjectTag()][SwiftTypeName("OverrideTests.MontyWSMBool")]
    public class MontyWSMBool : SwiftNativeObject
    {
        static MontyWSMBool_xam_vtable xamVtableMontyWSMBool;
        static MontyWSMBool()
        {
            XamSetVTable();
        }
        public static SwiftMetatype GetSwiftMetatype()
        {
            return NativeMethodsForXam_sub_MontyWSMBool.PIMetadataAccessor_Xam_sub_MontyWSMBool(SwiftMetadataRequest.Complete);
        }
        static IntPtr _XamMontyWSMBoolCtorImpl()
        {
        	// factory to construct the swift implementation and get back a handle
        }
        public  MontyWSMBool(): base(MontyWSMBool._XamMontyWSMBoolCtorImpl(), GetSwiftMetatype(), SwiftObjectRegistry.Registry)
        {
        }
        protected  MontyWSMBool(IntPtr handle, SwiftMetatype mt, SwiftObjectRegistry registry): base(handle, mt, registry)
        {
        }
        ~MontyWSMBool()
        {
            Dispose(false);
        }
        bool BaseVal()
        {
            IntPtr thisIntPtr = StructMarshal.RetainSwiftObject(this);
            bool retval = default(bool);
            // call into swift to call the base implementation of Val ()
            retval = NativeMethodsForXam_sub_MontyWSMBool.PImethod_Xam_sub_MontyWSMBoolXamarin_xam_sub_MontyWSMBoolDxam_super_val(thisIntPtr);
            SwiftCore.Release(thisIntPtr);
            return retval;
        }
        public virtual bool Val()
        {
            return BaseVal(); // default implementation, aka, the base.Val
        }
#if __IOS__ || __MACOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(MontyWSMBool_xam_vtable.Delfunc0))]
#endif
		// gets called by swift to implement Val
        static bool xamVtable_recv_Val(IntPtr self)
        {
            return SwiftObjectRegistry.Registry.CSObjectForSwiftObject <MontyWSMBool> (self).Val();
        }
        static void XamSetVTable()
        {
        }
        struct MontyWSMBool_xam_vtable
        {
            public delegate bool Delfunc0(IntPtr self);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public Delfunc0 func0;
        }
    }
```
Now you can, in C# override this type and do change the behavior of `Val ()` and if you pass
an instance of this C# type into Swift, it will pass the handle of the Swift override that
vectors through your instance.

There is one problem though - when you add new functionality to the the type, Swift will
know nothing about it. If you have a dozen new C# subclasses of the original Swift class,
you will not see the new functionality in Swift - they will all look like the singular
Swift override.

We can fix that - here's one way of doing that.
Suppose we have an attribute named `[ExposeToSwift]` that can be applied to the type. That way if
I write this in C#:
```csharp
[ExposeToSwift]
public class Bar : MontyWSMBool {
}
```
Then we make a Roslyn code generator that looks for this and writes the following Swift code:
```swift
public class Bar : xam_sub_MontyWSMBool {
    public override init () { super.init () }
}
```
And call this constructor instead of the `xam_sub_MontyWSMBool` constructor, then Swift will
now see a class called `Bar` instead of a class called `xam_sub_MontyWSMBool`.

This approach should also work for implementing protocols, including protocols with
associated types to the degree that the C# type conformance matches any Swift requirements.

In addition, we could allow `[ExposeToSwift]` to be applied to properties and methods as well.
These would need to vector into C# to pass data back and forth. This is not hard, it's just
detailed. It would require marshaling in order to ensure that the values make it back and
forth and a certain amount of static analysis in order to ensure that the C# type makes
sense in Swift. In BTfS, there is a type database that lets you look up a C# type from the
corresponding Swift type and vice versa and to flag errors where it doesn't make sense.
With appropriate options in the C# attribute, we can write the Swift to pass through Swift
attributes - we might have `[ExposeToSwift (swiftAttributes = "@available (iOS 10.0, macOS 10.12)\n@testable")]`
It would probably also behoove us to provide a way to have a possibly different name for a type, method, or
property in Swift than in C# in the event that we create a name collision that needs to be resolved.

So we might have an implementation like this:
```csharp
public class ExposeToSwiftAttribute : Attribute {
    public string SwiftAttributes { get; private set; }
    public string SwiftName { get; private set; }
    public ExposeToSwiftAttribte (string swiftAttributes = "", swiftName = "")
    {
        SwiftAttributes = swiftAttributes;
        SwiftName = swiftName;
    }
}