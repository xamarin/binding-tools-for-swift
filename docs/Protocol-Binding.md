This document was used to inform the current design and current documents. Look at those instead, OK?

# Protocol Binding (no associated types)
One of the language features in swift is protocols, which are more or less akin to C# interfaces.
At present we do a 1:1 mapping for each protocol. We implement a swift object that implements the protocol and vectors each of the methods through a vtable that has function pointers from C# that receive the call and marshal it into a C# object.

This works, but it has a limitation - it only works for one protocol at a time. To understand this limitation, I’m going to dive into the concrete implementation of protocols in swift.

Any type can implement a protocol and, unlike C#, Swift has the ability to adopt the protocol on any type via retroactive modeling. Because of this, the implementation for the protocol can’t be part of the object itself. Swift manages this will a structure called a Protocol Witness Table. A Protocol Witness Table is essentially a vtable that lives outside of the type. Any protocol witness table is described with two types, the implementing type and the protocol. For example, “protocol witness table for Swift.Int : Comparable”.  Witness tables can be compiled into the code statically, or in Swift 5 and later, they’re gotten through an accessor function that generates one as needed.

When representing a type that is a protocol type, swift uses a type of box called an existential container. An existential container consists of the following:


| Field                  | Offset (in pointers) |
| ---------------------- | -------------------- |
| Payload 0              | 0                    |
| Payload 1              | 1                    |
| Payload 2              | 2                    |
| Type Metadata          | 3                    |
| Protocol Witness Table | 4+                   |

The first 3 words are the data payload for the box. If the type is a value type and fits in the first 3 pointers, then it gets blitted into place. If it doesn’t fit then a heap-allocated blob gets allocated and the value type gets blotted into that space.

After the payload is a pointer to the type metadata for the type contained in the payload. The type metadata is used for information on how to manipulate the payload (how big is it? How do I move it from point a to point b and abide by ARC, etc).

Finally is a pointer to the protocol witness table. But this is a half-truth that I believed in my heart of hearts, but it turned out to be wrong, which is purpose of this document.

Based on my incorrect belief, I modeled the existential container as a block of 5 pointers always. This turns out not to be the case. There are two special cases and 1 general case that break this.

The first special case is the type `Swift.Any`, which is used in place of any type in swift. It is an existential container as above except that it has no protocol witness table. `Swift.Any` is the empty protocol and it does nothing. There is no protocol witness table, so it contains just 4 pointers. And make no mistake, Swift is fairly strict about treating it as a protocol.

The second special case is `Swift.AnyObject`, which is used in place of any class instance. Like `Swift.Any` it is also an empty protocol, but it is a special case because it isn’t represented by an existential container. This is because `Swift.AnyObject` is a class instance, so the type metadata is attached to the ISA pointer and we always know the ARC rules for it.

The general case in swift that breaks this is the protocol list type. I can declare a function like this:

    public func foo (a: Proto1 & Proto2 & Proto3) {
    }

and the type of a some type that implements all three of those protocols. In C#, this would be represented like this:

    public void Foo<PR> (PR a) where PR: IProto1, IProto2, IProto3 {
    }

The representation under the hood for this in Swift is an existential container with 3 protocol witness table slots, one for each protocol implemented.

Previously, to implement a protocol, SoM would write a class in swift that implements the protocol and defers each method into a vtable entry.

In C# there was a class that would map onto the swift class and receive all of it’s vtable calls, diverting them to the C# interface implementation. It works and works pretty well. Except for protocol list types that it can’t handle at all. The problem is that there is a 1:1 mapping of swift adapter onto swift protocol and it doesn’t work for more than one protocol at a time. This class also served double duty in that it was both a representation of a swift protocol as well as a proxy for types that don’t belong in the swift world, but still implement the equivalent C# interface.

To fix this, there will be a universal adapter which will live in swift glue, which will probably look something like this:


    public class EveryProtocol {
        public init(handle: OpaquePointer) { // don't know if the handle is needed.
            self.handle = handle
        }
        public var handle: OpaquePointer
    }

Then when given a protocol like this:

    public protocol Uppy {
       func addOne()
    }

We’ll write the following:

    internal struct Uppy_vtable {
        public var func0: @convention(c)((UnsafeRawPointer)->()?) = nil
    }
    internal vt: Uppy_vtable
    public func setUppy_vtable(ptr: UnsafeRawPointer) {
        let p:UnsafeMutablePointer<Uppy_vtable> = fromIntPtr(ptr)
        vt = p.pointee
    }
    public extension EveryProtocol : Uppy {
        public func addOne() {
            vt.func0!(toIntPtr(self))
        }
    }
    
    public func xam_FooDUppy(this: UnsafeMutablePointer<Uppy>) {
        this.pointee.Uppy()
    }

So now when we want to represent a C# type as an `Uppy` in swift, we just need to make an instance of `EveryProtocol` and use that as a proxy. And since `EveryProtocol` can act as a proxy for every protocol, we can now use it in protocol list types.

The only thing about this that will break the overall model is that `type(of:)` will violate the principal of least astonishment in that when used from within swift from a type exposed in C#, it will always return `XamGlue.EveryProtocol` instead of something representing the actual type. There’s not much we can do about this.


