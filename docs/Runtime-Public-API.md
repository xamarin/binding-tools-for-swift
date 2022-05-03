# Runtime Public API

# C# Code

# SwiftMarshal Namespace

## BlindClosureMapper

This class is used to hold P/Invokes into swift code for turning arbitrary closures into something that can be called from .NET. The complication of marshaling makes it really hard to map closures, but it can be done by changing the type of a closure of type `(args)→returnValue` into a closure of type `(UnsafeMutablePointer<(returnValue, args)>)→()`. This works because in .NET we can pack and unpack tuples at runtime.
I created 32 functions in swift that turn a swift closure into a callable closure with the previous mapping. For example:

    public func netActionToSwiftClosure<T1, T2>(a1: @escaping (UnsafeMutablePointer<(T1, T2)>)->()) ->
            (T1, T2) -> ()
    {
            return { (b1, b2) in
                    let args = UnsafeMutablePointer<(T1, T2)>.allocate(capacity:1)
                    args.initialize(to:(b1, b2))
                    a1(args)
                    args.deinitialize(count:1)
                    args.deallocate(capacity:1)
            }
    }

There are 16 functions for `Action` type closures (returns an empty tuple) and another 16 for `Func` type closures (has an actual return value).

## DynamibLib

No public APIs

## Extensions

No public APIs

## Memory

No public APIs

## NominalSizeStride

No public APIs

## StringMemory

No public APIs

## StructMarshal

- `IntPtr RetainSwiftObject(ISwiftObject obj)` - calls swift_retain on obj’s handle and returns the handle. Used in code that passes handles to Swift functions. Returns IntPtr.Zero if the handle is 0. 
- `IntPtr ReleaseSwiftObject(ISwiftObject obj)` - calls swift_release on obj’s handle if and only if the retain count is greater than 0 and returns the handle. Used in code virtual methods called with a handle to release that handle on return. Returns IntPtr.Zero if the handle is 0. 
- `nint RetainCount(ISwiftObject obj)` - returns the strong retain count of obj. 
- `nint WeakRetainCount(ISwiftObject obj)` - returns the weak retain count of obj. 
- `IntPtr RetainNSObject (NSObject obj)` - calls objc_retain on obj’s handle. Returns the handle if the object is non-null. Returns `IntPtr.Zero` if the object is null.
- `void ReleaseNSObject (NSObject obj)` - calls objc_release on the object’s handle.
- `bool IsSwiftRepresentable(Type t)` - returns true if the t can be represented in swift, false otherwise. 
- `SwiftMetatype Metatypeof(Type t)` - returns the equivalent `SwiftMetatype` for the given type. 
- `SwiftMetatype Metatypeof(Type t, Type[] interfaceConstraints)` - returns the `SwiftMetatype` of the type t with respect to the supplied interface constraints. At present, only 1 interface constraint is supported. 
- `IntPtr ProtocolWitnessof(Type t, Type withRespectTo)` - if `withRespectTo` is null, locates the protocol witness table of interface type t using the `SwiftProtocolTypeAttribute` attached to the type declaration. If `withRespectTo` is non-null, gets the protocol witness table using an “external” protocol definition by looking at the `SwiftExternalProtocolDefinitionAttribute` attached to the type declaration. 
- `int Sizeof(Type t)` - returns the size in bytes of the swift representation of the type, t. 
- `int Sizeof(Type[] t)` - returns the size in bytes of the first non-interface type in `types`. If all the types are interfaces, then this is a protocol and returns the size of the an existential container. 
- `int Strideof(Type t)` - returns the padded memory size for type t. 
- `int Strideof(Type t, Type[] constraints)` - returns the padded memory size for type t with the supplies interface constraints.
- `T DefaultNominal<T> () where T : ISwiftNominalType` - returns an *uninitialized* version of the type that implements `ISwiftNominalType`. This happens by calling a constructor that takes an argument of type `SwiftNominalCtorArg`. If there is no such constructor, this method will fail with a `SwiftRuntimeException`. Note that an uninitialized value type should only be used as a placeholder for a return value passed by reference. Never pass an uninitialized value type to a swift function as this can cause a hard crash.
- `byte[] PrepareNominal(ISwiftNominalType e)` - if the `SwiftData` member of nominal type `e` has not been allocated, this returns a new byte of the stride of the nominals type. 
- `IntPtr ToSwift(object o, IntPtr p)` - writes the swift equivalent of object `o` into the memory `p` and returns `p`'s value. 
- `T ToNet(IntPtr p)` - reads memory at `p` converting it to an (expected) value of type `T`. 
- `object ToNet(IntPtr p, Type t)` - reads memory at `p` converting it to an (expected) value of type `t`. 
- `Delegate MakeDelegateFromBlindClosure (BlindSwiftClosureRepresentation blindClosure, Type [] argTypes, Type returnType)` - returns a new .NET delegate from a `BlindSwiftClosureRepresentation`. It assumes that `blindClosure` has arguments that match the given `argTypes` and a return type that matches `returnType`.
- `BlindSwiftClosureRepresentation GetBlindSwiftClosureRepresentation (Type t, Delegate del)` - given a type of a delegate and a delegate, return a new `BlindSwiftClosureRepresentation` that can invoke the delegate.
- `IntPtr MarshalTupleToSwift(Type t, object o, IntPtr p)` - marshals an instance of tuple type `t` into memory `p`. If `o` is null, this does nothing. Returns p. 
- `IntPtr MarshalTupleToSwift(Type t, SwiftTupleMap map, object o, IntPtr p)` - marshals an instance of tuple type `t` into memory `p` using the given tuple memory map. 
- `object MarshalTupleToNet(IntPtr p, Type t)` - marshals a swift tuple into a tuple of type `t`. 
- `object MarshalTupleToNet(IntPtr p, SwiftTupleMap map)` - marshals a swift tuple into a .NET Tuple using the given tuple memory map.  
- `T ExistentialPayload<T> (ISwiftExistentialContainer container)` - given an `ISwiftExistentialContainer`, returns the payload in the container, assuming it is type `T`.
- `unsafe object ExistentialPayload(Type t, ISwiftExistentialContainer container)` - returns the payload in the container assuming it is type `t`.
- `bool ExceptionReturnContainsSwiftError (IntPtr p, Type t)` - given a Medusa tuple containing the return from a swift function that could throw, returns whether or not there was an error thrown.
- `T GetErrorReturnValue(IntPtr p)` - given a pointer to a Medusa tuple containing the return from a swift function that could throw, extracts the return value. Throws a `SwiftRuntimeException` if the Medusa tuple contains a SwiftError. 
- `object GetReturnValue(IntPtr p)` - given a pointer to a Medusa tuple containing the return from a swift function that could throw, extracts the return value. Throws a `SwiftRuntimeException` if the Medusa tuple contains a SwiftError. 
- `SwiftError GetErrorThrown(IntPtr p, Type t)` - given a pointer to memory and a .NET Tuple type `t`, extracts the thrown error from the Medusa tuple at `p`. If there was no error thrown, throws a `SwiftRuntimeException`. 
- `void SetErrorThrown(IntPtr p, SwiftError error, Type t)` - writes a swift Medusa tuple at memory `p` of type .NET Tuple type `t` with the given `error` and a marker that an error was thrown. 
- `void SetErrorNotThrown(IntPtr p, Type t)` - marks a swift Medusa tuple at memory p of type .NET tuple type `t` that no error was thrown. 
- `SwiftException GetExceptionThrown(IntPtr p, Type t)` - extracts a `SwiftError` from swift Medusa tuple at memory `p` of type `t` and wraps it in a `SwiftException`. 
- `IntPtr MarshalNominalToSwift(Type t, object o, IntPtr p)` - marshals a value type (enum or struct) of type `t` in `o` to the swift equivalent in memory `p`. 
- `void ReleaseNominalData(Type t, byte *p)` - calls the nominal destructor on payload pointed to by `p`. 
- `void ReleaseNominalData(Type t, IntPtr p)` - calls the nominal destructor on payload pointed to by `p`. 
- `IntPtr RetainNominalData(Type t byte *p, int size)` - calls the nominal init_with_copy function on the supplied payload into stack allocated memory. This has a side effect of causing a retain on the data. 
- `IntPtr RetainNominalData(Type t IntPtr p, int size)` - calls the nominal init_with_copy function on the supplied payload into stack allocated memory. This has a side effect of causing a retain on the data. 
- `object MarshalNominalToNet(IntPtr p, Type t)` - creates a new .NET representation of the nominal type `t` and sets the payload from memory at `p`.

## SwiftClassObject

SwiftClassObject represents the data type that corresponds to a Swift ISA pointer. 

- `SwiftClassObject FromSwiftObject(ISwiftObject obj)` - retrieves a `SwiftClassObject` from the given swift object. If the object is not a swift object (ObjC), throws a `NotSupportedException`. 
- `IsSwiftClassObject(ISwiftObject obj)` - returns true if `obj` is a swift class object, false otherwise. 
- `SwiftClassObject ClassClass { get }` - returns the class of the class object, `null` if there is none. 
- `SwiftClassObject SuperClass { get }` - returns the super class of the class if there is one, `null` otherwise. 
- `bool IsSwift1Class { get } -` returns true if the class is a swift 1.0 class, false otherwise. 
- `bool UsesSwift1RefCounting` `{ get }` - returns true if the class uses swift 1.0 reference counting. 
- `unit InstanceAddressPoint { get }` - returns the offset from a pointer to an instance of the class to the data of that instance. 
- `unit` `InstanceSize { get }` - returns the size of the instance data of the class. 
- `ushort InstanceAlignMask { get } -` returns the data alignment mask for an instance of the class. 
- `uint ClassSize { get }` - returns the class size. 
- `bool IsNominalTypeDescriptorValid { get }` - returns true if the `SwiftNominalTypeDescriptor` associated with this class is valid, false otherwise. 
- `SwiftNominalTypeDescriptor { get }` - returns a `SwiftNominalTypeDescriptor` for the class. 
- `IntPtr Table { get }` - returns a pointer to the class’ vtable. 
- `int VtableSize { get }` - returns the size of the vtable.

## SwiftEnumBackingTypeAttribute

When attached to a simple enum (non discriminated union), indicates the data type that represents the enumerated values (such as byte, short, nint, etc).

## SwiftEnumTypeAttribute

Inherits from SwiftNominalTypeAttribute. When attached to a nominal type indicates that the type is a swift Enum and provides information about the underlying type.

## SwiftNominalTypeDescriptor

Represents the data associated with a nominal type (struct, enum). 

- `bool IsValid { get }` - returns true if the handle is non-zero 
- `NominalTypeDescriptorKind GetKind()` - returns the kind of the type descriptor, one of Class, Struct, or Enum. 
- `bool IsGeneric()` - returns true if the type is generic, false otherwise. 
- `int` `GetGenericParamCount()` - returns the number of generic parameters of the type. 
- `int GetPrimaryGenericParamCount()` - returns the number of primary generic parameters. 
- `string GetMangledName()` - returns the mangled name of the type. 
- `string[] GetCaseNames()` - if the type is a Enum, returns an array of all the case names. Throws otherwise. 
- `string[] GetFieldNames()` - if the type is a Class of Struct, returns an array of all the field names. Throws otherwise.

## SwiftExternalProtocolDefinitionAttribute

When attached to a C# interface modeling a swift protocol, indicates a type that implements the protocol that C# can’t attached the interface to (for example, nint and SwiftEquatable).

- `Type AdoptingType { get }`- the type that adopts the protocol
- `string LibraryName { get }` - the name of the library that contains the protocol implementation
- `string ProtocolWitnessName { get }` - the name of the protocol witness table in the library

## SwiftNativeObjectAttribute

When attached to a binding, indicates that the binding directly represents a swift object as opposed to an object that inherits from that.

- `bool IsSwiftNativeObject (object o)` - returns true if o is a native swift object, false otherwise.

## SwiftNominalTypeAttribute

Represents a base class information about swift structs or swift enums. This does not get used directly ever. Instead use `SwiftStructTypeAttribute` or `SwiftEnumTypeAttribute`.

- `string LibraryName { get }` - the name of the library that defines the type
- `string NominalTypeDescriptor { get }`- the symbol name of the nominal type descriptor for the type
- `string Metadata { get }` - the symbol name of the type metadata for the type
- `string WitnessTable { get }`- the symbol name of the witness table for the type.

## SwiftProtocolConstraintAttribute

This attribute gets applied to a generic class for which a generic parameter is constrained by one of more protocols. 

- `Type EquivalentInterface { get }` - returns the C# type that represents the protocol constraint. 
- `string LibraryName { get }` - returns the library name that contains the protocol definition. 
- `string ProtocolWitnessName { get }` - returns the name of the protocol witness table for the protocol definition.

## SwiftProtocolTypeAttribute

This attribute gets applied to a C# interface that represents a swift protocol. 

- `Type ProxyType { get }` - returns the C# type that represents a proxy type for the protocol. 
- `bool IsAssociatedTypeProtocol { get }` - returns true if the protocol contains associated types.
- `bool IsAssocuateTypeProxy (Type type)` - returns true if `type` is a proxy for a protocol containing associated types.
- `Type ProxyTypeForInterfaceType (Type interfaceType)` - returns the type of the proxy to use for the interface type.
- `BaseProxy MakeProxy (Type interfaceType, object interfaceImpl, EveryProtocol protocol)` - given the proxy type, `interfaceType` create a proxy that delegates its methods to `interfaceImpl` and associate it with `protocol`.
- `BaseProxy MakeProxy (Type interfaceTYpe, ISwiftExistentialContainer container)` - make a proxy type that delegates its methods to the given existential container.

## SwiftStructAttribute

This attribute gets applied to a C# class that represents a swift struct.

## SwiftTupleMap

This class is used to map a set of types in a C# tuple to offsets in memory for a swift tuple. This type is used for marshaling C# `Tuple` objects to the swift equivalent. 

- `Type[] Types { get }` - returns the types in the tuple map. 
- `int[] Offsets { get }` - returns the offsets in memory for each type in the tuple. 
- `int Size { get }` - returns the size in bytes of the swift tuple. 
- `int Stride { get }` - returns the padded stride in bytes of the swift tuple. 
- `int Alignment { get }` - returns the memory alignment of the tuple.

# SwiftRuntimeLibrary Namespace

## AnonymousSwiftObject

This object is a place holder for any swift objects presented to C# for which there exists no equivalent C# type.

## MetatypeKind

This enum serves to identify the type of the `SwiftMetatype` object.

- None - there is no type (shouldn’t happen)
- Struct - the type is a struct
- Enum - the type is an enum
- Optional - the type is an optional
- ForeignClass - the type is a foreign class
- Opaque - the type is hidden
- Tuple - the type is a tuple
- Function - the type is a function/closure
- Protocol - the type is a protocol
- Metatype - the type is a type
- ObjCClassWrapper - the type is a wrapper around and ObjC class
- ExistentialMetatype - the type is an existential meta type
- HeapLocalVariable - the type is a heap local variable
- HeapGenericLocalVariable - the type is a generic local variable
- ErrorObject - the type is an error
- Class - the type is a class

## SwiftCallingConvention

This enum identifies the calling convention of a function/closure type.

- Swift - uses swift calling conventions
- Block - ???
- Thin - ???
- CFunctionPointer - uses C calling conventions

## NominalTypeDescriptorKind

This enum identifies the kind of a nominal type descriptor.

- None - there is no type (shouldn’t happen)
- Module - the type is a module
- Extension - the type is an extension
- Anonymous - the type is anonymous
- Protocol - the type is a protocol
- Class - the type is a class
- Struct - the type is a struct
- Enum - the type is an enum

## DynamicCastFlags

These flags are used to control how a dynamic cast is done in swift.

- None - no flags
- Unconditional - cast no matter what
- TakeOnSuccess - if the cast is successful, perform a take operation (which increases reference counts)
- DestroyOnFailure - if the case is not successful, call the destructor for the type (which decreases reference counts)

## SwiftObjectFlags

These flags are used to identify to .NET the state of an ARC object.

## SwiftParameterFlags

These flags are used to identify parameter types.

## SwiftParameterOwnership

Describes the ownership of a parameter

## SwiftExclusivityFlags

Describes exclusivity for retaining an object.

## SwiftMetadataRequest

The mechanism for making a type metadata request.

## EveryProtocol

This is an object that is used to implement protocols in swift via extensions. It is used by the C# proxies when interoperating with swift.

- `EveryProtocol ()` - constructs a default `EveryProtocol` object.
- `EveryProtocol XamarinFactory (IntPtr p)` - constructs an `EveryProtocol` object using the given handle.
- `void Dispose ()` - disposes the object
- `IntPtr SwiftObject { get }` - returns the swift handle associated with this object

## ISwiftExistentialContainer

This interface represents a model of the swift existential container object, which is used to represent protocols or protocol list types.

- `IntPtr Data0 { get; set; }` - gets or sets the 0th payload chunk
- `IntPtr Data1 { get; set; }` - gets or sets the 1st payload chunk
- `IntPtr Data2 { get; set; }` - gets or sets the 2nd payload chunk
- `SwiftMetatype ObjectMetadata { get; set; }` - gets or sets the object metadata for the payload of the container
- `IntPtr this [int index] { get; set; }` - gets or sets the protocol witness table pointer at the given index
- `int Count { get; }` - returns the number of protocol witness table entries
- `int SizeOf { get; }` - returns the size of the existential container in bytes
- `unsafe IntPtr CopyTo (IntPtr memory)` - copies the existential container to the provided pointer

## SwiftExistentialContainer0 - SwiftExistentialContainer8

Each of these structs are existential containers with witness table tables with 0 entries up to 8 entries, depending on the struct.

- `IntPtr SwiftExistentialContainer0.CopyTo (ISwiftExistentialContainer from, IntPtr memory)` - a utility routine to copy the given existential container to memory.
- `int SwiftExistentialContainer0.MaximumContainerSize` - constant that returns the largest supported existential container size
- `SwiftExistentialContainer1 (EveryProtocol everyProtocol, IntPtr protocolWitnessTable)` - constructs an existential container from the given EveryProtocol object and the given protocol witness table.
- `SwiftExistentialContainer1 (Type interfaceType, EveryProtocol everyProtocol)` - constructs an existential container from the given interface type and everyProtocol object. Will throw a `NotSupportException` if the type is not an interface type. Will throw a `SwiftRuntimeException` if the interface type is not a swift protocol.
- `SwiftExistentialContainer1 (IntPtr memory)` - constructs a an existential container from the given memory.

## ICustomStringConvertible

This interface represents the swift protocol `CustomStringConvertible`.

- `SwiftString Description { get; }` - returns a description of the instance

## CustomStringConvertibleXamProxy

This class is a proxy class for the `ICustomStringConvertible` protocol.

## ISwiftComparable

This interface represents the `Comparable` protocol.

- `bool OpLess (ISwiftComparable other)` - returns true if the instance is less than the given other

## ISwiftEquatable

This interface represents the `Equatable` protocol.

- `bool OpEquals (ISwiftEquatable other)` - returns true if the instance is equal to the given other

## ISwiftError

This interface is used for types that implement the Swift.Error protocol.

## ISwiftHashable

This is interface corresponds to the swift Hashable protocol.

- `nint HashValue { get }` - returns the hash value for this type
- `bool OpEquals(ISwiftHashable other)` - returns true if this type is the same as the other.

## ISwiftNominalType

This interface is used to represent swift named value types.

- `byte[] SwiftData { get; set }` - gets an array of opaque data that represents the swift value type. The size of the array can be found by calling `StructMarshal.Marshal.Strideof()`.

## ISwiftEnum

This interface inherits from ISwiftNominalType. It adds no members.

## ISwiftStruct

This interface inherits from ISwiftNominalType. It adds no members.

## ISwiftObject

This interface represents an instance of a swift class object.

- `IntPtr SwiftObject { get }` - gets the handle to the swift object instance.

## SwiftProtocolDescriptorAttribute

This attribute is applied to existential containers that represent swift protocols. An existential container is a swift type that houses a payload with a size of 3 machine pointers followed by a swift metadata pointer to identify the type of the payload, followed by 1 or more pointers to protocol witness tables for this type.

- `int WitnessTableSize { get }` - returns the number of protocol witness table entries.

## SwiftAnyObject

SwiftAnyObject is a placeholder that gets used for the `SwiftAnyObject` type.

- `T CastAs<T> () where T : class, ISwiftObject` - attempts to cast the the instance to the given type

## SwiftArray

This is a generic class that maps onto a swift array type. It implements ISwiftStruct and IList.

- `SwiftArray(uint capacity)` - creates a new array with the `capacity` reserved entries. 
- `SwiftArray()` - creates a new array with no reserved entries. 
- `SwiftArray (IList<T> list)` - creates a new array with the contents of the given list.
- `SwiftArray(IEnumerable<T> collection)` - creates a new array and adds in all elements in the collection. 
- `SwiftArray(params T[] items)` - creates a new array with the given items.
- `SwiftArray(SwiftNominalCtoArgument unused)` - initialized an array type with an empty data payload. 
- `T this[int index] { get; set; }` - gets or sets an element at the specified index in the array. 
- `int Count { get }` - returns the number of elements in the array. 
- `IEnumerator GetEnumerator()` - returns an enumerator for elements in the array. 
- `IEnumerator IEnumerable.GetEnumerator()` - returns an enumerator for elements in the array. 
- `void AddRange(IEnumerable collection)` - adds the elements of the collection onto the end of the array. 
- `void Clear()` - removes all elements in the array. 
- `bool Contains(T item)` - returns true if `Equals(elem, item)` returns true for each elem in the array, false otherwise. 
- `void CopyTo(T [] array, int arrayIndex)` - copies all elements of the `SwiftArray` into array starting from `arrayIndex` in array. 
- `bool Remove(T item)` - removes the first instance of item in the array and returns true if it removed it, false otherwise. 
- `int IndexOf(T item)` - returns the 0-based index of item in the array, or -1 if it isn’t in the array. 
- `void RemoveAt(int index)` - removes the item at the index from the array. 
- `bool IsReadOnly { get }` - returns false.

## SwiftClosureRepresentation and BlindSwiftClosureRepresentation

There are two representations of swift closures that are used in tom-swifty. The first is a `BlindSwiftClosureRepresentation`. It is blind because it contains no actual type information of a callable function. It contains two fields, `Function` and `Data`, both are `IntPtr`. It is used because it can be marshaled to and from unmanaged code with no side effects.

- `SwiftClosureRepresentation` is a typed representation of the latter in that the `Function` field is a `Delegate`. *NB: This code is still being hashed out.*

## SwiftComparableProxy

A proxy type for the `ISwiftComparable` interface.

## SwiftCore

Swift core is a collection of P/Invokes into libSwiftCore.dylib (the Apple swift runtime library) and “safe” wrappers around them.

- `IntPtr Retain(IntPtr p)` - retains a swift object increasing the strong reference count by 1. Returns the pointer passed in. If the pointer passed in is `IntPtr.Zero`, throws an `ArgumentOutOfRangeException`. 
- `IntPtr Release(IntPtr p)` - releases a swift object decreasing the strong reference count by 1. Returns the pointer passed in. If the pointer passed in is `IntPtr.Zero`, throws an `ArgumentOutOfRangeException`. 
- `IntPtr RetainObjC(IntPtr p)` - retains an objective C object. If the pointer passed in is `IntPtr.Zero`, throws an `ArgumentOutOfRangeException`. 
- `IntPtr ReleaseObjC(IntPtr p)` - releases an objective C object. If the pointer passed in is `IntPtr.Zero`, throws an `ArgumentOutOfRangeException`. 
- `void RetainWeak(IntPtr p)` - performs a retain on the swift object increasing the `weak` reference count by 1. 
- `void ReleaseWeak(IntPtr p)` - releases a swift object decreasing the `weak` reference count by 1. 
- `int RetainCount(IntPtr p)` - returns the strong retain count of a swift object. If the pointer passed in is `IntPtr.Zero`, throws an `ArgumentOutOfRangeException`. 
- `int WeakRetainCount(IntPtr p)` - returns the `weak` retain count of a swift object. If the pointer passed in is `IntPtr.Zero`, throws an `ArgumentOutOfRangeException`. 
- `bool DynamicCast<T> (ref T dest, object src, Type srcType, DynamicCastFlags flags = DynamicFlags.None)` - attempts to cast the object, src, to an object of type T.
- `IntPtr ProtocolWitnessTableFromFile (string dylibFile, string conformanceIdentifier, SwiftMetatype metadata)` - attempts to get the protocol witness table for the given type using the conformance symbol
- `SwiftMetatype TupleMetatype(SwiftMetatype[] tupleMetatypes)` - given the array of swift type metadata objects, returns the type metadata for a tuple of that type. 
- `bool DynamicCast(ref T dst, object src, Type srcType, DynamicCastFlags)` - performs a swift dynamic cast of one object type to another. Upon return, if `DynamicCast` returns true, then dst will be the result of the cast.

## SwiftDate

This class represents the swift `Date` struct.

- `SwiftDate (double timeInterval, SwiftDate since)` - constructs a date object using the time interval and the given date
- `SwiftDate ()` - constructs a date object with the current date and time
- SwiftDate (NSDate nsDate) - constructs a date object from the given `NSDate`
- `SwiftDate (double timeIntervalSinceNow)` - constructs a date object adding the time interval
- `void Dispose ()` - disposes the object
- `NSDate ToNSDate ()` - returns an NSDate object representing the date

## SwiftDictionary

Represents a binding onto the swift dictionary type. It implements the `ISwiftStruct` and `IDictionary<T, U>` interfaces. 

- `SwiftDictionary()` - allocates an empty dictionary. 
- `SwiftDictionary(IDictionary<T, U> elems)` - allocates a dictionary and populates it with the contents of the supplied dictionary. 
- `SwiftDictionary(SwiftNominalCtorArgument unused)` - allocates the payload space for a swift dictionary, but does not populate it or initialize it. 
- `int Count { get }` - returns the number of elements in the dictionary. 
- `IsReadOnly { get }` - returns false. 
- `ICollection<T> Keys { get }` - returns a collection of the keys in the dictionary. 
- `ICollection<T> Values { get }` - returns a collection of the values in the dictionary
- `void Add(KeyValuePair item)` - adds the key and value in item to the dictionary. 
- `void Add(T key, U Value)` - adds the key and value to the dictionary. 
- `bool Contains (KeyValuePair<T, U> item)` - returns true if the dictionary contains the key/value pair
- `void CopyTo(KeyValuePair [] array, int arrayIndex)` - copies all the elements in the dictionary into the array starting at arrayIndex. 
- `IEnumerator> GetEnumerator()` - returns a sequence of items in the dictionary. 
- `void Clear() -` removes all elements in the dictionary. 
- `bool Remove(KeyValuePair item)` - removes the item from the dictionary. Returns true if the dictionary contained the item, false otherwise. 
- `bool Remove(T key)` - removes the item associated with the key. Returns true if it removed it. 
- `bool TryGetValue(T key, out U value)` - if the dictionary contains the key, sets value to the value associated with the key and returns true. Otherwise, it sets value to `default(U)` and returns false. 
- `U this[T key] { get; set; }` - gets or sets a value in the dictionary.

## SwiftDotNetCapsule

This class is used to encapsulate .NET that that will be held by a closure representation in swift. This class is itself a binding onto a swift class. 

- `SwiftDotNetCapsult(IntPtr p)` - allocates a capsule holding the pointer `p`. 
- `IntPtr Data { get; set }` - gets or sets the data help by the capsule. 
- `IsEscaping { get; set }` - gets or sets a flag to indicate that the closure representation is escaping or not.

## SwiftEnumHasRawValueAttribute

Indicates that the swift enum has a bound raw value and what that type is. 

- `Type RawValueType { get }` - returns the type of the raw value.

## SwiftEquatableProxy

This class is a proxy type for the interface `ISwiftEquatable`.

## SwiftError

Swift error is a binding onto the swift Error type. 

- `IntPtr Handle { get }` - returns the swift handle for the type. 
- `SwiftMetatype` `Metatype { get }` - returns the swift metadata for the error object. 
- `string Description { get }` - returns the description of the error. 
- `SwiftError FromException(Exception e)` - given a .NET exception, returns a `SwiftError` that represents it. If `e` inherits from `SwiftException`, this will map directly onto a corresponding swift error contained in the `SwiftException.Error` property. If there is no corresponding `SwiftError` for the exception, this will return a `SwiftError` with a handle from a swift `DotNetError` object with the description set to `e.Message`.

## SwiftException

Swift exception is a .NET exception that encapsulates a corresponding `SwiftError` object. When a swift function or method returns an error, it will be thrown as this type. 

- `SwiftError Error { get }` - returns the `SwiftError` associated with this object.

## SwiftHashableProxy

SwiftHashableProxy is a .NET proxy class that represents the swift Hashable protocol. If a swift method requires a Hashable protocol, this proxy class can be used in its place.

## SwiftHasher

This class is a binding onto the swift struct `Hasher` which is a general hashing implementation.

- `unsafe void Combine<T>(T thing) where T: ISwiftHashable` - combines the hash value of the given object into the hasher
- `unsafe void Combine (UnsafeRawBufferPointer bytes)` - combines a hash of the given buffer of bytes into the hasher
- `unsafe void Combine (byte[] bytes)` - combines a hash of the given array of bytes into the hasher
- `nint FinalizeHasher ()` - returns the final hash value of the hasher

## SwiftMetatype

SwiftMetatype represents type metadata for a swift object’s type. 

- `SwiftMetatype(IntPtr handle)` - initializes the swift metadata from the supplied handle. 
- `MetatypeKind Kind { get }` - returns the kind of the metadata. 
- `bool IsValid { get }` - returns true if the metadata has a valid handle, false otherwise. 
- `IntPtr Handle { get }` - returns the handle for the metadata
- `SwiftNominalTypeDescriptor GetNominalTypeDescriptor()` - returns a `SwidtNominalTypeDescriptor` associated with the metadata if the kind is `Enum`, `Struct`, or `Class`. Throws a `NotSupportException` otherwise.

## SwiftObjectRegistry

The SwiftObjectRegistry is a repository for SwiftObjects that were instantiated in C#. The registry exists as a singleton class and its accessors are thread-safe.

- `void Add(ISwiftObject obj)` - If the object is already in the registry, this does nothing. If it is not already in the registry, it does a swift weak retain on the object and puts a weak reference of the C# object into the registry. 
- `void RemoveAndWeakRelease(ISwiftObject obj)` - removes the object from the registry and adds it to a queue of objects for releasing on the main thread. 
- `bool Contains(IntPtr p)` - returns true if the given handle is in the registry, false otherwise. 
- `Type RegisteredTypeOf(IntPtr p)` - returns the type of the object if it is registered, null otherwise. 
- `T CSObjectForSwiftObject(IntPtr p) where T : class, ISwiftObject` - returns a C# object for the given handle. If the handle is already in the registry, this will return the existing object. If not, a new object will get constructed and added to the registry. If the type of an existing object doesn’t match the generic type, this will throw a `SwiftRuntimeException`. 
- `T CSObjectForSwiftObjectRTChecked(IntPtr p)` - returns a C# object with runtime checking that does not require the class and ISwiftObject constraints.
- `ISwiftExistentialContainer ExistentialContainerForProtocol (object implementation, Type type)` - creates a existential container given an implementation and the interface type. If `type` is not an interface type or is not swift protocol, this will throw a `SwiftRuntimeException`
- ISwiftExistentialContainer ExistentialContainerForProtocols (object implementation, Type[] types) - creates an existential container for the type that implements each of the supplied protocols in `types`. If the number of types exceeds `SwiftExistentialContainer0.MaximumContainerSize`, this will throw an `ArgumentOutOfRangeException`. If any of the types are not interface types or aren’t swift protocols, this will throw a `SwiftRuntimeException`.
- `T ProxyForInterface<T> (T interfaceImpl)` - returns a proxy type for the given interface implementation.  The result is cached so allocation only happens on demand. If the given implementation is already a proxy (inherits from `BaseProxy`), this returns the original object.
- `EveryProtocol EveryProtocolForInterface<T> (T interfaceImpl)` - returns an instance of `EveryProtocol` for a given interface implementation. The result is cached.
- `T InterfaceForEveryProtocolHandle<T> (IntPtr everyProtocolHandler)` - given a handle to an `EveryProtocol` object, returns the interface implementation that it is associated with.
- `T ProxyForEveryProtocolHandle<T> (IntPtr everyProtocolHandle)` - given a handle to an `EveryProtocol` object, returns its associated proxy.
- `T InterfaceForExistentialContainer<T> (ISwiftExistentialContainer container)` - given a container, finds an associated object of that type, if it exists, or constructs a proxy to represent it.
- `SwiftClosureRepresentation SwiftClosureForDelegate(…)` - this method (and its variants) builds a `SwiftClosureRepresentation` that will adapt onto the given delegate. The variants are necessary for the four general variations of functions:

- A `Func` type that returns a value and takes no arguments
- A `Func` type that returns a value and takes arguments
- An `Action` type that takes no arguments
- An `Action` type that takes arguments

- `Tuple ClosureForCapsule(SwiftDotNetCapsule capsule)` - given a previously constructed SwiftDotNetCapsule, returns a tuple of the original delegate, its argument types and its return type. If there is no return type, it will be null. If there are no arguments, the type array will be empty.
- `void RemoveCapsule(SwiftDotNetCapsult capsule)` - removes the given capsule from the registry.
- `static SwiftObjectRegistry Registry { get }` - gets the single instance of the `SwiftObjectRegistry`.

## SwiftOptional

This class represents optional value types in swift.

## SwiftRuntimeException

This exception is thrown when an error has been detected by code in the SwiftRuntimeLibrary assembly. Typical causes are errors in marshaling, type mismatches in the SwiftObjectRegistry, or errors location symbols generated by the swift compiler.

## SwiftSet<T>

- `SwiftSet` is a binding to the swift set type.
- `SwiftSet ()` - constructs an empty set.
- `SwiftSet (nint capacity)` - allocates a set with the given capacity.
- `nint Count { get }` - returns the number of elements in the set
- `nint Capacity { get }` - returns the current capacity of the set
- `bool Contains (T key)` - returns true if the set contains the given key, false otherwise
- `Tuple<bool, T> Insert (T key)` - inserts the key in the set if not already there. If the element wasn’t there, this returns `(true, key)`. If an element already equal to `key` is present, this returns `(false, oldValue)` and does not change the set.
- `SwiftOptional<T> Remove (T key)` - removes the key from the set. Returns an optional value that contains the key from the set if it was present or an empty optional otherwise.

## SwiftString

SwiftString is a C# binding to the swift String type. Unlike C#, swift strings are value types and as such are implemented in C# as an ISwiftStruct with an opaque payload.

- `SwiftString (SwiftNominalCtorArgument unused)` - constructor used by marshaling. Allocated the SwiftData member to the correct size but does not initialize it.
- `unsafe SwiftString (string s)` - constructs a swift string containing the C# string contents.
- `static SwiftString FromString (string s)` - converts a .NET string into a swift String equivalent.
- `string ToString()` - converts a swift String to the .NET string equivalent.
- `static explicit operator SwiftString (string s)` - cast operator for `string` to `SwiftString`.

## UnsafeMutablePointer<T>

This class is a binding to the swift `UnsafeMutablePointer` type.

- `UnsafeMutablePointer (UnsafePointer<T> ptr)` - constructs a new `UnsafeMutablePointer` from the given `UnsafePointer`
- `UnsafeMutablePointer (OpaquePointer ptr)` - constructs a new `UnsafeMutablePointer` from the given `OpaquePointer`
- `static UnsafeMutablePointer<T> Allocate (nint capacity)` - creates a block of memory of the given capacity to the type `T`
- `void Initialize (T to)` - initializes the memory of the pointer to the given value
- `void Initialize (T repeating, nint count)` - initializes the memory of the pointer to `count` copies of `repeating`
- `void Deinitialize (nint count)` - deinitializes `count` elements pointed to by the pointer
- `void Deallocate ()` - deallocates the memory associated with the pointer
- `T Pointee { get; set; }` - gets or sets object at the pointer
- `IntPtr ToIntPtr ()` - returns an `IntPtr` for the given pointer
- `UnsafeMutablePointer<T> Advance (nint by)` - advances the pointer by the given number of elements
- `UnsafeMutablePointer<T> AdvanceNative (nint by)` - advances the pointer using native C# calls
- `UnsafeMutablePointer<T> Predecessor ()` - returns a pointer to the object preceding the current
- `UnsafeMutablePointer<T> Successor ()` - returns a pointer to the object subsequent to the current

## UnsafeMutableRawBufferPointer

This class represents a pointer to raw untyped data with a dimension.

- `UnsafeMutableRawBufferPointer (IntPtr start, nint count)` - returns a pointer to the specified block of memory
- `SwiftString DebugDescription { get }` - returns a debugger description of the buffer
- `unsafe nint Count { get }` - returns the number of bytes in the buffer
- `unsafe byte this [int index] { get; set; }` - get or set a value in the buffer at the given index

## UnsafePointer<T>

This class represents a typed read-only pointer to values.

- `UnsafePointer (IntPtr p)` - constructs a pointer using the given C# pointer
- `UnsafePointer (UnsafeMutablePointer<T> ptr)` - constructs a pointer using the given `UnsafeMutablePointer`
- `UnsafePointer (UnsafePointer<T>)` - copy constructor
- `UnsafePointer (OpaquePointer ptr)` - constructs a pointer using the given `OpaquePointer`
- `T Pointee { get; set; }` - gets or sets object at the pointer
- `IntPtr ToIntPtr ()` - returns an `IntPtr` for the given pointer
- `UnsafeMutablePointer<T> Advance (nint by)` - advances the pointer by the given number of elements
- `UnsafePointer<T> AdvanceNative (nint by)` - advances the pointer using native C# calls
- `UnsafePointer<T> Predecessor ()` - returns a pointer to the object preceding the current
- `UnsafePointer<T> Successor ()` - returns a pointer to the object subsequent to the current

## UnsafeRawBufferPointer

This class represents a dimensioned untyped pointer to read-only memory.

- `UnsafeMutableRawBufferPointer (IntPtr start, nint count)` - returns a pointer to the specified block of memory
- `SwiftString DebugDescription { get }` - returns a debugger description of the buffer
- `unsafe nint Count { get }` - returns the number of bytes in the buffer
- `unsafe byte this [int index] { get; }` - get or set a value in the buffer at the given index

## UnsafeRawPointer

This class represents a untyped read-only pointer.

- `UnsafeRawPointer (IntPtr p)` - constructs a pointer from the given `IntPtr`
- `UnsafeRawPointer (UnsafeRawPointer p)` - copy constructor
- `UnsafeRawPointer (UnsafeMutableRawPointer p)` - constructs a pointer from the given mutable pointer.
- `static explicit operator IntPtr (UnsafeRawPointer ptr)` - cast operator to `IntPtr`
- `static explicit operator UnsafeRawPointer(IntPtr ptr)` - cast operator from `IntPtr`
- `IntPtr Pointer { get }` - the pointer value

## UnsafeRawMutablePointer

This class represents a untyped mutable pointer.

- `UnsafeMutableRawPointer (IntPtr p)` - constructs a pointer from the given `IntPtr`
- `UnsafeMutableRawPointer (UnsafeRawPointer p)` - copy constructor
- `UnsafeMutableRawPointer (UnsafeMutableRawPointer p)` - constructs a pointer from the given mutable pointer.
- `static explicit operator IntPtr (UnsafeMutableRawPointer ptr)` - cast operator to `IntPtr`
- `static explicit operator UnsafeMutableRawPointer(IntPtr ptr)` - cast operator from `IntPtr`
- `IntPtr Pointer { get }` - the pointer value

## OpaquePointer

This class represents an opaque pointer.

- `OpaquePointer (IntPtr p)` - constructs a pointer from the given `IntPtr`
- `OpaquePointer (UnsafeRawPointer p)` - copy constructor
- `OpaquePointer (UnsafeMutableRawPointer p)` - constructs a pointer from the given mutable pointer.
- `static explicit operator IntPtr (OpaquePointer ptr)` - cast operator to `IntPtr`
- `static explicit operator OpaquePointer(IntPtr ptr)` - cast operator from `IntPtr`
- `IntPtr Pointer { get }` - the pointer value
- `static OpaquePointer FromUnsafeMutablePointer<T> (UnsafeMutablePointer<T> ptr)` - converts a typed mutable pointer to an `OpaquePointer`
- `static OpaquePointer FromUnsafePointer<T> (UnsafeMutablePointer<T> ptr)` - converts a typed pointer to an `OpaquePointer`

## XamProxyTypeAttribute

This attribute gets applied to a proxy type for a swift protocol.

- `Type ProxyType { get }` - returns the type of the interface for which this type is a proxy.

## XamTrivialSwiftObject

Since swift has no real root object, XamTrivialSwiftObject can be used as a parent class when it is necessary to implement a protocol for swift or to automatically make a class that can be consumed by swift. It has no real public interface or functionality beyond reference counting.

# Swift Code

# XamGlue

## arrayhelpers.swift

- `arrayNew(capacity: int) → [T]` - allocates a new array of the given type with the given capacity, but 0 elements. 
- `arrayCount(a: [T]) → Int` - returns the number of elements in the array. 
- `arrayGet(retval: UnsafeMutablePointer, a: [T], index: Int) -` retrieves the element at location `index` and writes it to the given pointer. 
- `arraySet(a: UnsafeMutablePointer<[T]>, value: T, index: Int)` - sets the element at location `index` to the given `value`. The array gets passed as a pointer since arrays are value types. 
- `arrayInsert(a: UnsafeMutablePointer<[T]>, value: T, index: Int)` - inserts the `value` at the specified index. The array gets passed as a pointer since arrays are value types. 
- `arrayClear(a: UnsafeMutablePointer<[T]>)` - removes all the elements in the array maintaining the capacity. The array gets passed as a pointer since arrays are value types. 
- `arrayRemoveAt(a: UnsafeMutablePointer<[T]>, index: Int)` - removes the element at specified `index`. The array gets passed as a pointer since arrays are value types. 
- `arrayAdd(a: UnsafeMutablePointer<[T]>, thing: T)` - appends the element `thing` onto the end of the array. The array gets passed as a pointer since arrays are value types.

## closurehelpers.swift

- `netActionToSwiftClosure<``*generics``*>(a1: @escaping: (UnsafeMutablePointer<(``*generics``*)>) → ()) → (``*generics``*)→()` - each of these functions are generic functions that can turn a closure of a pointer to a tuple (or single) returning nothing to a closure of the tuple arguments that returns nothing. This is used to build closures for swift that call into a >NET delegate that can marshal properly. In the above name, *generics* refers to one to 16 generic arguments. 
- `netFuncToSwiftClosure(a1: @escaping: (UnsafeMutablePointer<(TR,` `*generics``*)>) →()) → (``*generics``*)→TR` - each of the functions are generic functions that can turn a closure of a pointer to a tuple of the return type and from one to sixteen argument types to a closure of the argument types returning the return type. 
- `swiftClosureToAction<``*generics``*>(a1: @escaping (``*generics``*)→()) → (UnsafeMutablePointer<(``*generics``*)>) → ()` - each of the functions with this name are generic functions that can turn a swift closure of up to 16 arguments, returning nothing into a closure that takes a pointer to a tuple (or single) of the arguments returning nothing. This is used to build closures from .NET `Action` types that can can be called from swift. 
- `swiftClosureToFunc(a1: @escaping (``*generics``*)→TR) → (UnsafeMutablePointer<(TR,` `*generics``*)>)→()` - each of the functions with this name are generic functions that can turn a swift closure of up to 16 arguments returning type `TR` into a closure that takes a pointer to a tuple consisting of the return type followed by the argument types and returns nothing. This is used to build closures from .NET `Func` types that can be called from swift.

## dictionaryhelpers.swift
## xam_proxy_Hashable

This class is used for a C# proxy to provide an implementation of the Hashable protocol.

- `setHashable_xam_vtable(uvt: UnsafeRawPointer)` - sets the table in the Hashable proxy to C# implementations of the protocol. 
- `make_xam_proxy_Hashable(retval: UnsafeMutablePointer)` - factory function to construct a xam_proxy_Hashable object. 
- `xamarin_XythonDHashableGhashValue(this: UnsafeMutablePointer<xam_proxy_Hashable.)→Int` - adapter which calls the xam_proxy_Hashable to get the `hashValue` property. 
- `newDict(capacity: Int) → [T:U]` - allocated a new swift dictionary with the specified initial capacity. 
- `dictCount(d: [T:U]) → Int` - returns the number of elements in the dictionary. 
- `dictGet(retval: UnsafeMutablePointer<(U, Bool)>, d: [T:U], key: T)` - initializes retval to the Medusa tuple of the result of looking up the `key`: `(someValue, true)` if present or `(indeterminate, false)` if not. 
- `dictSet(d: UnsafeMutablePointer<[T:U]>, key: T, val: U)` - adds or replaces the the value `val` into the dictionary using the given `key`. The dictionary is passed by reference since it is a value type. 
- `dictContains(d: [T:U], key: T) → Bool` - returns true if the dictionary contains the `key`, false otherwise. 
- `dictKeys(d: [T:U]) → [T]` - returns an array of all the keys in the dictionary. 
- `dictValues(d: [T:U]) → [U]` - returns an array of all the values in the dictionary. 
- `dictAdd(d: [T:U], key: T, value: T)` - adds the given value into the dictionary, associating it with the key. 
- `dictClear(d: UnsafeMutablePointer<[T:U]>)` - clears the dictionary of keys retaining the current capacity. The dictionary is passed by reference since it is a value type. 
- `dictRemove(d: UnsafeMutablePointer<[T:U]>, key: T) → Bool` - removes a value in the dictionary associated with the given key. Returns true if the value existed in the dictionary, false otherwise.

## dotnetcapsule.swift
## DotNetCapsule

DotNetCapsule is a class that represents a closure data chunk in swift. It is built so that when the object gets allocated, it will call back into C# to allow the closure and any adapters to be removed from the registry.

- `setCapsuleDeinitFunc(p: @escaping(@convention(c) (UnsafeRawPointer)→()))` - sets a callback function to get called when a `DotNetCapsule` gets destroyed. It will get passed a pointer that had been stored in the capsule. 
- `makeDotNetCapsule(p: OpaquePointer) → DotNetCapsule` - factory function to construct a `DotNetCapsule` storing a pointer value within it. 
- `getCapsultData(dnc: DotNetCapsule) → OpaquePointer` - retrieve the pointer stored in a `DotNetCapsule`. 
- `setCapsultData(dnc:DotNetCapsult)` - sets the pointer stored in a `DotNetCapsule`.

## pointerhelpers.swift

- `toIntPtr(value: T) → UnsafeRawPointer` - blits a type into a pointer 
- `fromIntPtr(ptr: UnsafeRawPointer) → T` - bliss a pointer into a type 
- `toOptional(optTuple: (value: T, present: Bool)) → T?` - converts a Medusa tuple into a swift optional. 
- `fromOptional(opt: T? retval: UnsafeMutablePointer<(T, Bool)>)` - creates a Medusa tuple from a swift optional. If `opt` has no value, the initial chunk of memory in the tuple will not be initialized. 
- `setExceptionThrown(err: Error, retail: UnsafeMutablePointer<(T, Error, Bool)>)` - sets a Medusa tuple exception under the circumstances than an exception was thrown. The memory occupied by `T` in the tuple will be invalid. The memory occupied by `Error` will be set and the `Bool` will be true. 
- `setExceptionNotThrown(value: T, retail: UnsafeMutablePointer<(T, Error, Bool)>)` - sets a Medusa tuple exception under the circumstances that an exception was *not* thrown. `T` will be set, `Error` will be invalid, and `Bool` will be false. 
- `setExceptionNotThrownFromOptional(value: T?, retail: UnsafeMutablePointer<((T, Bool), Error, Bool)>)` - special case for setting an exception not thrown with an optional value which creates a nested Medusa tuple. 
- `isExceptionThrown(val: UnsafeMutablePointer<(T, Error, Bool)>) → Bool` - returns true if if pointer to the Medusa tuple represents an exception thrown, false otherwise. 
- `getExceptionThrown(val: UnsafeMutablePointer<(T, Error, Bool)>) → Error?` - returns an `Error` if an exception was thrown, nil otherwise. 
- `getExceptionNotThrown(val: UnsafeMutablePointer<(T, Error, Bool)>) → T?` - returns a `T` if the exception was not thrown, nil otherwise. 
- `getExceptionNotThrownFromOptional(val: UnsafeMutablePointer<((T, Bool), Error, Bool)>) → T?` - returns a `T?` from a nested Medusa tuple if the exception was not thrown, nil otherwise.

## stringglue.swift

- `fromUnmanagedUTF16Raw(start: UnsafeMutablePointer, numberOfCodePoints: Int, result:UnsafeMutablePointer)` - makes a new swift String from a pointer to UTF16 characters.

## swiftenumerror.swift

`SwiftEnumError` is a simple enum/Error type that is used to make the swift compiler happy in retrieving payloads from a swift enum. A typical case retriever will look like this:

    public func payloadCaseA(inout f: Foo) -> Bar {
        return try! { (b: Foo) throws -> Bar in
            if case .a(let x) = b ) { return x }
            else { throw SwiftEnumError.undefined }
        } (f)
    }

What happens in this block is that a code block gets executed which either pulls the payload or throws. This gets executed in a `try!` which will return the payload or crash if it’s the wrong type, but this function will never get called by tom-swifty unless the payload case matches. So `SwiftEnumError.undefined` is a placeholder. The compiler accepts that we checked the “not a” case and lets it go by.

- `DotNetError` is a class that is used to encompass the information in a .NET `Exception` into a swift `Error` type. It encompasses two pieces of information: the `Message` property from the exception and the exception class name. The message gets used in the `description` property. 
- `makeDotNetError(message: UnsafePointer, className: UnsafePointer) → Error` - factory function to construct a `DotNetError` object with the message and class name. 
- `getErrorDescription(message: UnsafeMutablePointer, error: Error)` - sets message to the description from the swift `Error`.

## trivialswiftobject.swift
## XamTrivialSwiftObject

This is an empty object with no functionality which is there as a tool for C# developers who want to make an object that can be passed to swift. Swift has no notion of a root object, so this can serve as a root.

## typecachekey.swift

The `TypeCacheKey` object is used as a key to use in the vtable caches that are used for generic virtual classes. In order to call back into C#, the code needs to use a vtable to find the callback, but since the vtable will be different for each specific version of the generic type. Unfortunately, type objects in swift aren’t hashable, so this class can serve in this case. It takes advantage that type object in swift are singletons and will have a unique `ObjectIdentifier` which does have a hash value.

