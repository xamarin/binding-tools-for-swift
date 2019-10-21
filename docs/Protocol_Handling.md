# Protocol Handling
The protocol handling in Binding Tools for Swift was redone in July/August of 2019 and now follows the following pattern.

There now exists a type in swift and bound in C# called `EveryProtocol`. Just like it says on the box, `EveryProtocol` represents every protocol that will be reflected in swift. This gets done using retroactive modeling to implement any given protocol onto `EveryProtocol`. There are a few steps:


1. Define a vtable struct in swift that represents delegates in C# that swift can call for each method/property in the protocol.
2. Define a public top-level function for setting that vtable
3. For each method/property on the protocol, write an adapter function to call that protocol.
4. In C# write an interface (`IFace`) that matches the protocol which will include an `SwiftProtocolType` attribute that includes the type of the proxy
5. In C# write a proxy class that inherits from `BaseProxy`  and implements that interface meeting the following requirements:
    1. There will be a constructor with 2 arguments:
        1. an `IFace` and an `EveryProtocol`. This stores the `IFace` instance and hands `typeof (IFace)` and the `EveryProtocol` instance to the base, it also sets up an existential container for marshaling
        2. There will be a constructor with 1 argument of type `ISwiftExistentialContainer`. It hands the base class `typeof(IFace)` and null for `EveryProtocol`. It copies the existential container for marshaling.
    2. There will be a vtable struct parallel to the swift one
    3. There will be a static constructor that calls the swift function to set the vtable
    4. There will be static methods for each vtable entry
    5. There will be a static property named `ProtocolWitnessTable` which returns the protocol witness table for the swift protocol with respect to `EveryProtocol`.

This allows the proxy to represent either a C# type with no swift roots in a way that swift can consume. It also allows a swift object with no C# roots to be be represented in C# in a way that C# can consume.

This is best illustrated with an example, `CustomStringConvertible`, which in swift is defined like this:


    public protocol CustomStringConvertible {
        var description : String { get }
    }

In C#, the parallel interface will look like this:


    public interface ICustomStringConvertible {
        String Description { get; }
    }

The swift wrappers will look like this:

    // vtable
    internal struct CustomStringConvertible_xam_vtable {
        // the first arg is a pointer to the return value
        // the second arg is a pointer to an existential container for the type
        internal var func0: (@convention(c)(_: UnsafeRawPointer, _: UnsafeRawPointer) -> ())?;
    }
    
    private var _vtable: CustomStringConvertible_xam_vtable = CustomStringConvertible_xam_vtable();
    
    // install a C# vtable
    public func setConvertible_xam_vtable(uvt: UnsafeRawPointer)
    { 
        let vt: UnsafePointer<CustomStringConvertible_xam_vtable> = fromIntPtr(ptr: uvt);
        _vtable = vt.pointee
    }
    
    // implement the protocol for EveryProtocol
    extension EveryProtocol : CustomStringConvertible
    {
        public var description : String {
            get {
                // allocate space for the return value
                let retval = UnsafeMutablePointer<String>.allocate(capacity: 1)
                // call C#
                _vtable.func0!(retval, toIntPtr(value: self))
                // grab the return value
                let actualRetval = retval.move()
                // free memory
                retval.deallocate ()
                return actualRetval
            }
        }
    }
    
    // wrapper around the description property
    public func xamarin_NoneDConvertibleGdescription(retval: UnsafeMutablePointer<String>, this: inout CustomStringConvertible)
    {
        retval.initialize(to: this.description);
    }

The proxy will look like this:


    [SwiftProtocolType (typeof (CustomStringConvertibleXamProxy), SwiftCore.kXamGlue, SwiftCoreConstants.ICustomStringConvertible_MetadataAccessor)]
        // here's the interface
    public interface ICustomStringConvertible {
        SwiftString Description { get; }
    }
    
    // This is a proxy that gets used when there is either a C# type that needs to look
    // like a swift type of a swift type that needs to look like a C# type
    public class CustomStringConvertibleXamProxy : BaseProxy, ICustomStringConvertible {
        ICustomStringConvertible actualImpl;
        SwiftExistentialContainer1 container;
    
        static CustomStringConvertible_xam_vtable xamVtableICustomStringConvertible;
        static CustomStringConvertibleXamProxy ()
        {
            // install the vtable
            XamSetVTable ();
        }
    
        // required ctor #1
        public CustomStringConvertibleXamProxy (ICustomStringConvertible actualImplementation, EveryProtocol everyProtocol)
            : base (typeof (ICustomStringConvertible), everyProtocol)
        {
            container = new SwiftExistentialContainer1 (everyProtocol, ProtocolWitnessTable);
        }
    
        // required ctor #2
        public CustomStringConvertibleXamProxy (ISwiftExistentialContainer container)
            : base (typeof (ICustomStringConvertible), null)
        {
            this.container = new SwiftExistentialContainer1 (container);
        }
    
    #if __IOS__
        [MonoPInvokeCallback (typeof (CustomStringConvertible_xam_vtable.Delfunc0))]
    #endif
        static void xamVtable_recv_get_Description (IntPtr xam_retval, IntPtr self)
        {
            // this gets called from swift. xam_retval is a pointer to the return value
            // self is a pointer to a existential container with 1 witness table
            // make an existential contained from the self arg
            var container = new SwiftExistentialContainer1 (self);
            // get an interface for it
            var proxy = SwiftObjectRegistry.Registry.InterfaceForExistentialContainer<ICustomStringConvertible> (container);
            // get the description
            var retval = proxy.Description;
            // copy it into swift
            StructMarshal.Marshaler.ToSwift (retval, xam_retval);
        }
    
        static void XamSetVTable ()
       {
           xamVtableICustomStringConvertible.func0 = xamVtable_recv_get_Description;
           unsafe {
               byte* vtData = stackalloc byte [Marshal.SizeOf (xamVtableICustomStringConvertible)];
               IntPtr vtPtr = new IntPtr (vtData);
               Marshal.WriteIntPtr (vtPtr, Marshal.GetFunctionPointerForDelegate (xamVtableICustomStringConvertible.func0));
               NativeMethodsForICustomStringConvertible.SwiftXamSetVtable (vtPtr);
           }
       }
    
        public SwiftString Description {
            get {
                // this is the tricky part - we have one of two cases here:
                // either there is a C# implementation or there is a swift implementation
                // if actualImpl is non-null, there is a C# implementation that we call.
                // if actualImpl is null, then the existential container is used and that
                // requires pinvoking into swift.
                if (actualImpl != null)
                    return actualImpl.Description;
                unsafe {
                    SwiftString retval = new SwiftString (SwiftNominalCtorArgument.None);
                    fixed (byte* retvalSwiftDataPtr = StructMarshal.Marshaler.PrepareNominal (retval)) {
                        NativeMethodsForICustomStringConvertible.PIpropg_IConvertiblexamarin_NoneDConvertibleGdescription (new IntPtr (retvalSwiftDataPtr), ref container);
                        return retval;
                    }
                }
            }
        }
    
        // vtable definition
        struct CustomStringConvertible_xam_vtable {
            public delegate void Delfunc0 (IntPtr xam_retval, IntPtr self);
                [MarshalAs (UnmanagedType.FunctionPtr)]
                public Delfunc0 func0;
            }
    
            static IntPtr protocolWitnessTable;
            // protocol witness table with cache
            public static IntPtr ProtocolWitnessTable {
                get {
                    if (protocolWitnessTable == IntPtr.Zero)
                        protocolWitnessTable = SwiftCore.ProtocolWitnessTableFromFile (SwiftCore.kXamGlue, XamGlueConstants.ICustomStringConvertible_ConformanceIdentifier,
                                                    StructMarshal.Marshaler.Metatypeof (typeof (CustomStringConvertibleXamProxy)));
                        return protocolWitnessTable;
                }
            }
        }
    
        internal class NativeMethodsForICustomStringConvertible {
            [DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.ICustomStringConvertible_NoneDConvertibleGdescription)]
            internal static extern void PIpropg_IConvertiblexamarin_NoneDConvertibleGdescription (IntPtr retval, ref SwiftExistentialContainer1 container);
            [DllImport (SwiftCore.kXamGlue, EntryPoint = XamGlueConstants.ICustomStringConvertible_SwiftXamSetVtable)]
            internal static extern void SwiftXamSetVtable (IntPtr vt);
        }
    }


## Making Proxies

In the bad old days, when calling the registry to make a proxy, the caller had to include a factory lambda expression to new up the proxy class. This was awkward and I hated it. This can go away now. Here’s why: `EveryProtocol` has a no-argument constructor so it can be made with no issue. In addition, the proxy classes have standard constructors: 1 for proxying a disconnected C# object and one for proxying a swift protocol.

The main public interfaces in the registry are  `T ProxyForInterface<T> (T interfaceImpl)` where `T` is an interface type from swift and `T InterfaceForExistentialContainer<T>(IExistentialContainer container)` which returns a proxy of type T for the existential container provided. In the first case, the proxy for the object will get cached. This cache is possible a one-to-many cache where each entry in the cache for a given object may be a proxy for a different interface type but all of them share the same `EveryProtocol`. As an example, if I have:

    public class Foo : ISwiftProto1, ISwiftProto2, ISwiftProto3 { }

The cache for an instance of `Foo` could eventually be a `List<BaseProxy>` containing a proxy for `ISwiftProto1`, `ISwiftProto2` and `ISwiftProto3` (each of which contains the same version of `EveryProtocol`.

On the other side, when a request is made to get a proxy for an existential container, the code checks to see if the existential container contains an instance of `EveryProtocol`. This is cheap because the swift metadata is for any type is a singleton in a given instance of an app so it ends up being a pointer compare. If it is an `EveryProtocol`, we rifle through the cache to find the original object. This could be improved by having a more sophisticated data structure, but I don’t think that this will (1) get called much and (2) should be able to run in microseconds in typical use. (PRs welcome)


## Working With Protocol List Types

We can now handle protocol list types in swift. Given the following func:

    public func foo (a: proto1 & proto2 & proto3) { }

This can be modeled in C# with a method like this:

    public void Foo<PL> (PL a) where PL : IProto1, IProto2, IProto3 { }

Previously, since there was a 1:1 mapping between protocols and proxies, we couldn’t handle this.
Now since there is a single `EveryProtocol` object, we can build the `ExistentialContainer3` object that represents this in swift.

To make the existential container, the registry provides the following method `public ISwiftExistentialContainer ExistentialContainerForProtocols (object implementation, Type[] types)` which takes a C# object that implements each of the interface types in `types` and precaches proxies for each interface and then builds a swift Existential Container of the right size and layout for swift.



