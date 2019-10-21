# Object Modeling
In Binding Tools for Swift, objects are modeled as C# objects implementing the `ISwiftObject` interface, which in turn implements `IDisposable`. The `ISwiftObject` includes one property, `SwiftObject` which returns an `IntPtr` which is a handle to the underlying swift object that is being modeled.

In addition to this, given the following minimal class in swift:

    public final class Monty {
        public init () { }
    }

Binding Tools for Swift will generate the following code (method bodies left blank):

    
    using System;
    using System.Runtime.InteropServices;
    using SwiftRuntimeLibrary;
    using SwiftRuntimeLibrary.SwiftMarshal;
    using ClassWrapTests;
    namespace ClassWrapTests
    {
        [SwiftNativeObject()]
        public class Monty : ISwiftObject
        {
            protected IntPtr handle;
            protected SwiftMetatype class_handle;
            protected SwiftObjectFlags object_flags = SwiftObjectFlags.IsSwift;
            public static SwiftMetatype GetSwiftMetatype()
            {
            }
    
            static IntPtr _XamMontyCtorImpl()
            {
            }
    
            public  Monty()
                : this(_XamMontyCtorImpl(), GetSwiftMetatype(), SwiftObjectRegistry.Registry)
            {
            }
    
            protected  Monty(IntPtr handle, SwiftMetatype classHandle, SwiftObjectRegistry registry)
            {
            }
            Monty(IntPtr handle, SwiftObjectRegistry registry)
                : this(handle, GetSwiftMetatype(), registry)
            {
            }
            public static Monty XamarinFactory(IntPtr p)
            {
            }
            public void Dispose()
            {
            }
            protected virtual void Dispose(bool disposing)
            {
            }
            protected virtual void DisposeManagedResources()
            {
            }
            protected virtual void DisposeUnmanagedResources()
            {
            }
            ~Monty()
            {
            }
            public IntPtr SwiftObject
            {
                get
                {
                    return handle;
                }
                private set
                {
                    handle = value;
                }
            }
        }
    }

In this, there are several pieces that are important to note:
`XamarinFactory` is a static factory method that is used to construct an instance of the C# binding using just a handle from swift. This is typically used after marshaling from swift to C#. For example, in tearing apart a swift tuple, there may be a handle to a swift object that needs to be represented in C#.

There is a static method `_XamMontyCtorImpl` which is used when a swift handle needs to be constructed from C#, typically as a result of calling a C# constructor.

There is a private constructor which constructs the C# object given a swift handle and stores it in the registry.

`GetSwiftMetatype` is an accessor that returns the swift type metadata object that is associated with the underlying swift type. If a class is generic, this method will take `Type`  arguments that represent the generic specialization. This static method is not optional.

Xamarin interfaces with swift’s Automatic Reference Counting (ARC), by taking a weak reference to the given object and a strong reference. Dispose manages the appropriate releasing of the of these references.  When a C# object gets allocated, a `GCHandle` will get stored automatically in the registry along with the swift handle.

Binding Tools for Swift marshaling code automatically handles interfacing with ARC before passing swift handles to a swift API.

With an `open` class, each class will have associated with it a vtable of delegates that get passed into swift. The delegates, when invoked from swift, calls a static *receiver* in C# which then calls the virtual C# method for the implementation. The default implementation calls back into swift to call the swift implementation of the method. In this way, C# can override the behavior of swift methods/properties. Be aware that the ordering of the vtable is not documented and should not be depended on.



