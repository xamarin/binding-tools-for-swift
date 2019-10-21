# README

# Abstract

This document describes the inner workings of tom-swifty with as much ‘why’ context in addition to the ‘what’ and ‘how’.


# TL;DR - I Just Want To Dive In

The general operation of tom-swifty is as follows:

1. Read input swift module (requires either a .framework or a .dylib/.swiftmodule pair)
2. Pull out and demangle public symbols
3. Reflect the module into XML
4. Analyze the types and, if needed, write swift code to make methods callable from C#
5. Compile generated swift code wrappers
6. Pull out and demangle public symbols from wrappers
7. Reflect the wrapper module
8. Write C# that maps onto the modules

The main components that do this are:

- Dynamo - a code generator that works via C# combinators
- Decomposer - a swift function name demangler
- A hacked version of the Apple swift compiler - does reflection on modules to XML
- MethodWrapping - writes swift mappers for method uncallable from C#
- NewClassCompiler - writes C# from swift/swift wrappers
- TypeMapper - handles mapping types from one language to the other

The flow runs like this:

- Starting with a NewClassCompiler object, call CompilerToCSharp(). This method will:
  - Read input modules and generate an inventory of all the entry points associating them with inferred data types (GetModuleInventories)
    - Each symbol gets pulled from the file, if it’s a swift entry point (starts with ‘__T’), it gets demangled pushed into the module inventory. From the demangling, we infer the structural layout of the module.
  - Reflect and read all types (GetModuleDeclarations)
    - This executes a hacked up version of the apple swift compiler. I’ve had to change 1 code file and added another.
    - The .swiftmodule contains a serialized abstract syntax tree of the compiled code. Apple has a set of tools for writing visitors on this tree. I use that to generate XML representing the types and methods that we care about.
    - The output of doing this gets read in and represented in the SwiftXmlReflection hierarchy.
  - Register all types in all modules in the type mapper
  - Write swift wrappers (WrapModuleContents)
  - Write C# for each of the types (CompileFinalClass)


# What a Wreck! Why Is This The Way It Is?

There are 3 separate C# types that represent what’s in a swift module. They are:

- SwiftType (and its subclasses)
- BaseDeclaration (and its subclasses)
- TypeSpec (and its subclasses)
## SwiftType

This happened because of reasons. When I started on tom-swifty, the apple code wasn’t open source so I started reverse engineering compiler output. I did this by writing simple functions in swift, compiling them and looking at the generated names and tried to figure out what the mangling scheme was. I represented this in the with the following types:

- SwiftType
- TLDefinition (top-level definition)
- SwiftName

In Decomposer.cs there is a recursive descent tool that looks through the mangling of a Swift function name and turns it into a TLDefinition. Some TLDefinitions are data (witness table, metadata, etc), but most of these are going to be TLFunction types. Swift functions are real honest to goodness functional programming functions in that all mangled named are in the form (single-arg-type)→(return-type). If a function takes more than 1 argument, the argument list is a tuple. If a function is a method, it has 2 argument lists and the first argument list is always the type of the instance.

SwiftType and its descendants are meant to be immutable types, which explains why some of the code feels a little awkward at times, but I think it made things better.

In writing tom-swifty, I started off with only these types. The code took the output of demangling all the symbols, dropped them into a ModuleInventory, wrote wrappers and C#. This went well until I hit structs and tried to infer types and layouts of structs which is not possible by looking at only the mangled names. I kept the old code around because we still need to know what a given function is named. There are other ways to get this information, but it’s not readily available. For a future release, we could try to put the mangled names into the XML output, but at the level we look at the AST, I don’t think there’s easy access to that.
The end result is that there are 3 type hierarchies that represent swift elements. And it turns out that we need them all since the available information doesn’t overlap 100% on all of them.


## Inventories

An inventory is an object that is an Inventory<contents>. I define the following inventories:

- ModuleInventory contains a set of ModuleContents
- A ClassInventory contants a set of ClassContents (may be struct, class, enum)
- A ProtocolInventory contains a set of ProtocolContents
- A PropertyInventory contains a set of PropertyContents
- A FunctionInventory contains a set of OverloadInventory
- An OverloadInventory contains set of List<TLFunction>
- A VariableInventory contains a set of VariableContents
- A WitnessInventory is not an inventory for some reason

A ModuleContents contains:

- Protocols
- Classes
- Functions
- Variables

A ClassContents contains:

- Constructors
- ClassConstructor
- Methods
- Properties
- PrivateProperties
- Subscripts
- PrivateSubscripts
- StaticFunctions
- Destructors
- WitnessTable
- Variables
- Initializers
- FunctionsOfUnknownDestination (TLFunction that miss any of the other bins)
- DefinitionsOfUnknownDestination (TLDefinitions that miss any of the other bins)

A ProtocolContents contains

- Metaclass
- DirectMetadata
- TypeDescriptor
- WitnessTable
- FunctionsOfUnknownDestination (TLFunction that miss any of the other bins)
- DefinitionsOfUnknownDestination (TLDefinitions that miss any of the other bins)

A PropertyContents contains:

- A getter
- A setter
- A materializer (not needed by us)

A VariableContents contains:

- A TLVariable
- A list of addressors

The general process is that when you call ModuleInventory.FromFile, it will open a dylib file and extract all the swift symbols. Each symbol gets decomposed into a TLDefinition and a matching ModuleContents is found or constructed for that symbol. The TLDefinition gets send to the module’s Add method. In there, if its a TLFunction, it gets delegated off to the Add method of one of:

- Protocols
- Classes
- Functions (top-level functions)
- Variables (top-level variables)

Inner classes/structs/enums are distinguished by their full class name (module.name.name) but there is no hierarchy in the inventories. It’s not necessary.

## BaseDeclaration And Its Friends

A ModuleDeclaration is *not* a BaseDeclaration. A ModuleDeclaration is a container class that holds:

- Classes
- Structs
- Enums
- Protocols
- Functions
- Properties
- TopLevelFunctions
- TopLevelProperties

In addition to being a container, it has a bunch of convenience accessors.
A BaseDeclaration is an abstract class that has

- a Name
- Accessibility
- A ModuleDeclaration reference
- A Parent
- Generics
- A converter from XElement objects

A TypeDeclaration represents what in swift would be class a Nominal Type (any type with a name). A type declaration may be rooted or unrooted. A rooted TypeDeclaration has a valid set of Parent pointers all the way up. An unrooted TypeDeclaration has been pulled out of its tree. It still retains information about its inheritance, but it doesn’t need a reference to its parent. This is used when a type needs to be written out as XML without a reference to its parentage (maybe from a different module?)
The TypeDeclaration objects (Nominal Types) that are represented are:

- ClassDeclaration
- ProtocolDeclaration
- StructDeclaration
- EnumDeclaration

Each of these is nearly identical except for what they represent.
EnumDeclaration is the most divergent in that in addition to methods and properties, it contains a list of EnumElement objects which are the cases for the enum.

There is no real difference in the representation of functions, member functions, getters/setters, indexers, materializers, constructors and destructors. They all get represented as a FunctionDeclaration. A FunctionDeclaration is:

- a set of argument lists (Swift used to have the ability to have any number of argument lists to support partial function application, but this has gone away for now and you either have 1 argument list (function, static function) or 2 arguments lists (class function, member function)
- a return type
- generics
- stored flags:
  - can throw
  - is a property
  - is static
  - is final

There is one important TypeDeclaration descendant called ShamDeclaration. It is a placeholder type that is used when a type gets introduced by name (for example in a TypeSpec - see below) but without any notion of what it is (yet). A ShamDeclaration stands in for the real declaration until the actual declaration gets introduced. Effectively, it’s a forward declaration.

## All About TypeSpec

TypeSpec is a class that came about because of the way the visitor type works in the swift compiler. When you have an argument or return type in swift, it gets represented as something less than useful. It is a text representation of one of:

- nominal type - a name and a set of generic references (bound or unbound)
- a tuple type ([TupleElem[, TupleElem]*]) A TupleElem is [inout][name:]TypeSpec
- a closure type TypeSpec→TypeSpec

Swift gives a reasonably uniform text representation of this. TypeSpecParser is a recursive descent parser that turns a text representation of a type spec into a TypeSpec. This is a fairly typical language parsing architecture: there’s a tokenizer that generates tokens, a parser that consumes them and generates TypeSpec objects.

The C# types are:

- TypeSpec - Abstract type containing generic parameters, attributes, optional, inout.
- NamedTypeSpec - a basic nominal type. It has a name in the form a.b.c.d where a is the module name. It will either be in the form module.[intermediate names]typeName or typeName.
- TupleTypeSpec - contains a list of TypeSpec
- ClosureTypeSpec - contains a TypeSpec for arguments and a TypeSpec for the return type

TypeSpecs have a problem with generic types in that there is no way to immediately distinguish between bound and unbound generics. If a generic is represented as Something<T>, T could be the name of a type or the name of an unbound generic parameter. In order to make this determination, you have to look at the context in which the generic parameter is used. If it is an unbound generic, then it’s name will be found somewhere in the hierarchy up.


## Dynamo And Code Generation

When electrical generators were first invented, they were weak and unable to produce power at scale. One of the first improvements to the electrical generator was called a dynamo. Instead of a fixed magnet, it used a self-powered electro magnet.

When I asked around about how were wrote generated code, I was told “Console.WriteLine”. I’ve been down that road before and wanted to do better, so I knocked together a set of classes that can represent many common code patterns and wrote specific pieces for C# and for swift. The C# code came first and consequently, there are chunks that need to get renamed with a CS prefix to better disambiguate them from swift (SL).

To make a CS file, you start with a Namespace object and a UsingPackages object. These get inserted into a CSFile object. A UsingPackages contains all the references for the file. In it are a method for adding a reference: AddIfNotPresent(), which takes either a string representation of the package or a Type.
A Namespace is a container of ITopLevelDecl objects, which are CSClass, CSStruct, CSEnum, and CSInterface. The rest consists of block structured elements, “lineable" elements, and expressions. Expressions may be combined using regular C# operators. For example, if I have two expressions, expr1 and expr2 I can represent the sum of the two by doing expr1 + expr2 in C#. For BaseExpr, I define most of the operators such that they will generate a BinaryExpr of the sub expressions.

Given this C# code:

    use.AddIfNotPresent (typeof (SwiftObjectRegistry));
    
    var parms = new Parameter [] {
        new Parameter(CSSimpleType.IntPtr, new Identifier("p")),
        new Parameter(new CSSimpleType(typeof(SwiftObjectRegistry).Name), "registry")
    };
    var cons = Method.PrivateConstructor (constructorName,
        new Dynamo.CSLang.ParameterList (parms), new CodeBlock (null)
            .And (ImplementNativeObjectCheck ())
            .And (ImplementMetatypeAssignment ())
            .And (Assignment.Assign (kSwiftObjectGetterName, parms [0].Name))
            .And (FunctionCall.FunctionCallLine ("SwiftCore.Retain", false, parms [0].Name))
            .And (FunctionCall.FunctionCallLine (String.Format ("{0}.Add", parms [1].Name), false, Identifier.This))
                  );
    return cons;

This will generate the following code:

    constructorName(IntPtr p, SwiftObjectRegistry registry)
    {
        SwiftObject = p;
        registry.Add(this);
    }

The nice thing about this is that I don’t ever have to worry about formatting or semicolons etc. since that is all handled by Dynamo.

C# generation most commonly happens in NewClassCompiler and TopLevelFunctionCompiler.
Swift code generation most commonly happens in MethodWrapping and OverrideBuilder.


## About Wrapping and Why It’s Needed

In theory, we should be able to p/invoke into every entry point in a swift object. In practice, we can’t because of differences in the ABI. Most notably, swift allows up to 3 machine pointers for value type arguments and return values, has some screwy rules for tuples, uses an extra register for exceptions, and in the future will use a register for the instance pointer.

In order to manage all of this, I have code that given a FunctionDeclaration, determines if it needs to be wrapped. The rules for this are:
If the return type or argument is: generic, a tuple, a closure or a type that must be passed by reference.
A type that must be passed by reference is: an unbound generic, an optional value type, a struct, a protocol, or a non-trivial enum.

For a while, I tried to optimize the code for structs so that blittable structs that fit in 2 machine pointers could be passed by value. This caused more problems than it was worth, so it went away. Nearly all of the integral types in swift are structs. I special case these and floating point types as “scalars” since they can get passed normally.

Given that, if a function or method needs wrapping I will do the following transforms:

for each parameter, if its type ParmType needs to be passed by reference, declare the parameter instead to be UnsafePointer<ParmType> or UnsafeMutablePointer<ParmType> unless the parameter is an optional type.
If the parameter is an optional type, instead set its type to be UnsafeMutablePointer<(ParmType, Bool)>.
If the function is an instance method, prepend an argument of that type named “this” (“this” is not a reserved word in swift).
If the function has a return type that must be passed by reference, prepend an argument that is an UnsafeMutablePointer<ReturnType>.
If the function can throw an exception (and to be clear swift exceptions are not actual exceptions, they’re new return values), instead prepend an argument that is an UnsafeMutablePointer<(ReturnType, Error, Bool)>.

Given a swift method:

    public class Foo {
        public func doIt(s: String) → String { }
    }

This will get wrapped as:

    public func Foo_doIt(retval:UnsafeMutablePointer<String>, this:Foo, s:UnsafePointer<String>)
    {
        let retval0 = this.doIt(s:s.pointee)
        retval.initialize(to:retval0)
    }

This method can be called from C# because it’s all pointer sized values.

Optionals and exceptions get wrapped in tuples because that seemed like the obvious way to handle them. Optionals in swift are enums, which in turn are really discriminated unions. The swift rules for accessing the payload in a discriminated union are byzantine and appear to be fragile and likely to change. Instead, I use a special form of tuple that I call a Medusa tuple. Medusa tuples are tuples with a payload that can only be looked at if you know their state. If you look at it in the wrong state, you die.

I wrote two swift functions fromOptional<T>(opt: T?, retval:UnsafeMutablePointer<(T, Bool)>) and toOptional<T>(optTuple:(value:T, present: Bool)) → T? which convert back and forth from optionals and Medusa tuples. There are equivalent C# functions as well.


## Writing PInvokes

In the class TopLevelFunctionCompiler, there is code that writes PInvoke or delegate declarations given a FunctionDeclaration. There are flavors for FunctionDeclarations and Properties.

This code, given an unwrapped FunctionDeclaration and the wrapper mangled name, applies the wrapping rules to write a pinvoke that will call into the wrapper. The code is straight forward, with the exception of generics. If a swift function has n unbound generics, it will be followed by up to n extra arguments which are swift type metadata objects. There are exceptions to the when an generic causes an extra argument. If I have:

    f<T, U, V>(a:T, b:U, c:V)

then 3 extra args will get passed.

If I have:

    f<T, U, V>(a:T, b:U, c:V, d:T)

then 3 extra args will get passed.

If I have:

    f<T, U, V>(x:SomeClass<T>, a:T, b:U, c:V)

SURPRISE! only 2 extra args will get passed (SomeClass<T> has the metadata type of T in it so Swift optimizes that).


## Type Mapping

In addition SwiftType, TypeDeclaration, and TypeSpec, there are Dynamo code representations of types: CSType and SLType.
I created a type mapper to initially map from SwiftType to CSType, but as the project went on it became clear that I needed a wider reach.
The class TypeMapper contains a database of (possibly) unrooted entities and tools to map back and forth between the various types.


## Marshaling

There are two general circumstances of marshaling in tom-swifty: compile time and run time. If a type is known at compile time, tom-swifty will inject appropriate code to directly marshal the type. If a type is not known at compile time (generic), tom-swifty will inject code that marshals the type at runtime using the SwiftRuntimeLibrary.SwiftMarshal.StructMarshal class.

There are 3 general subtypes of compile time marshaling

1. C# to Swift `MarshalEngine.cs`
2. Swift to C# `MarshalEngineSwiftToCSharp.cs`
3. “C Safe” Swift to C# `MarshalEngineCSafeSwiftToCSharp.cs`

Each of them are named engines because they do a lot of heavy lifting that is best to be done outside of the regular code.

Most of the code tries to break the special cases down and injects the appropriate code.

Automatic Reference Counting (ARC) presents an interesting problem in marshaling because swift has some strict rules for reference counting. Generally, when a caller calls a function, the caller must increase the reference count of the object. The callee is responsible for decreasing the reference count before exit.

This is simple, but the mechanism is different depending on the type. For a reference type, it’s straight forward: call swift_retain on the handle before calling; call swift_release on the handle before returning. Value types are different because of calling semantics. Value types are passed by value. If the type doesn’t fit within 3 machine registers, then the caller allocates space on the stack and makes a copy of the local into that memory. If the value type contains a reference type, then the act of making the copy will cause a retain on that type. When the caller returns, it will call a destructor on that type, causing a release.

One of the sticky problems in managing swift types in C# came to a head for this: value types in swift have an implicit destructor that is called when they go out of scope and retain on copy semantics. C# structs have neither of these. As a result, we can’t represent a swift struct as a C# struct nor can we directly expose the values in them (unless they are blittable, but this turns out to be painful to implement). Instead, we keep a byte array of opaque data for both structs and enums to hold the object data. When a method gets called with a struct, we stackalloc an array of the size of the payload and use the copier from the swift-compiled code (in the ValueWitnessTable), which will manage ARC retain if needed.

Runtime marshaling happens in `SwiftRuntimeLibrary` in `StructMarshal.cs` The largest part of this consists of moving values represented as C# types to Swift and vice versa. The typical action is to take a pointer to raw memory and either translate a C# value to its swift equivalent into that memory or to extract a swift value from memory and translate it to C#.
Each type of translation requires the swift metadata for each type. This is done through the public interface `Metatypeof(Type t)` and looking at the C# type’s general kind and working with that.

The general types are:

- Primitives (byte, sbyte, short, etc.)
- CPU based types (nint, nuint)
- Delegates
- Tuples
- Classes
- Protocols/Existential containers
- Nominal types (structs, enums)
- Optionals
- Exceptions

The process of runtime marshaling from C# involves the calling code making a stack-allocated buffer for the memory (the size is determined by `StructMarshal.Strideof(Type t)`), then calling `StructMarshal.ToSwift(Type t, object o, IntPtr p)`. This routine goes through the following process:

| *Kind*                     | *Process*                                                                                                                                                                                              |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Primitive, CPU based types | Blitted directly into memory                                                                                                                                                                           |
| Delegates                  | TBD                                                                                                                                                                                                    |
| Nominal types              | Get the C# data buffer for the nominal and its in-built copy method from the Swift type’s value witness table that will retain any reference counted sub parts into memory                             |
| Classes                    | Call swift_retain on the handle and blit it into memory                                                                                                                                                |
| Protocols                  | Blit the data into the memory in the existential container format.                                                                                                                                     |
| Tuples                     | Get a cached type map that represents the offsets from a given pointer to where a tuple element lives, use that map to put converted tuple elements into the appropriate location in the tuple memory. |
| Optionals                  | Marshaled as a Medusa tuple.                                                                                                                                                                           |
| Exceptions                 | Assumed that the type is a SwiftError, which can be built from a .NET exception. Its handle gets blitted into the memory                                                                               |

The reverse is done in pretty much the same manner.


## Compiling Top Level Entities

Swift allows top-level entities such as global functions and variables/properties. C# does not. For any top level entities, tom-swifty will write a container class for them and create static methods/properties in it that map onto the swift entities. The default name of the class will be TopLevelEntities in the namespace of the module. The name can be overridden.

## Compiling Non-Virtual Classes, Structs, and Enums

Classes are straight forward - we write constructors, implement IDisposable, ISwiftObject, then implement all the methods, properties, and indexers. For the most part anything represented by a function is either a direct call to a pinvoke or a bunch of marshaling and eventually a invoke call (this is all handled by the marshal engine).

Structs are nearly identical to classes except that instead of instance, we pass a pointer to a stack-alloced copy of the payload.

Enums fall into two categories: trivial and non-trivial. Trivial enums are honest-to-goodness enumerations represented by an integral value. Non-trivial enums are discriminated unions that are a discriminator and (possibly) a payload. For trivial enums, we compile a C# enum. For non-trivial enums, we compile a C# enum for each of the discriminators, then write a class the holds the payload and contains accessor method that can be used to query the value’s discriminator and read its payload. Enums can also contain methods, so we make those available too.


## Compiling Virtual Classes

Virtual classes are tricky. We should be able to take consume a subclass of an object in C# and have swift consume a C# subclass transparently.

To do that, I automatically make a subclass of any virtual class (declared open in swift) in swift and create a synthetic vtable of function pointers to call for each of the methods. The overrides then delegate off the function pointer (marshaling if needed). In addition, I make available an implementation of each virtual method that calls the super class. In the C# side, we set the initial vtable to point to static methods that call the C# object virtual method. The initial virtual method calls the default implementation.

Each virtual class has precisely 1 vtable shared among all instances of the type.

In order to simplify the calling of C# from swift, the vtable definition in swift uses closure syntax with the @convention(c) attribute. This means that the closure won’t have a context pointer, but instead is just a C function pointer. It is also restricted in the types that can be passed through it directly since a C function wouldn’t necessarily follow ARC conventions. We work around this by using a helper function toIntPtr() in swift which is a heavy handed cast of a typed pointer to an untyped (and non-ARC) UnsafeRawPointer.


## Compiling Protocols

Protocols in Swift are represented in a couple ways. A type that subscribes to a protocol will have a protocol witness table which is effectively a vtable for the protocol. When a protocol gets passed as an argument, it gets passed in a special kind of data box called an existential container. An existential container is an object that is a minimum of 5 pointers. The first 3 are data payload. The next two are the type metadata for the object represented in the payload followed by a pointer to the protocol witness table for the protocol. If the existential container is meant to represent multiple protocols, the set will contain a list of protocol witness tables.

As a side note, swift has the ability to create extension protocols. Using existential containers makes that possible.

I created an interface to represent this in C#:

    public interface ISwiftProtocolImpl {
        IntPtr Data0 { get; set; }
        IntPtr Data1 { get; set; }
        IntPtr Data2 { get; set; }
        SwiftMetatype ObjectMetadata { get; set; }
        IntPtr this [int index] { get; set; }
        int Count { get; }
    }

and 8 concrete implementations to handle protocol lists of arity up to 8. Code does not currently support protocol lists. Yet.

When tom-swifty needs to implement a protocol, it writes a wrapper swift class that implements the protocol and includes a vtable for all the methods.

tom-swifty also writes a C# interface that is equivalent to the swift protocol and creates an adapter class that for any given implementation of the C# interface so that when marshaled to swift, interface gets replaced with the adapter class which is linked to an instance of the wrapper swift object through the vtable.

When swift passes a protocol (existential container) to C#, we look to see it’s an adapter that we know about already and if so grab the original object. If not, we use a C# adapter onto that.

All the protocol work is so close to the process of building an override that it’s part of OverrideBuilder.cs because of the overlap.


## Generics

Swift generics are dependent upon the swift type metadata object. Given a function like this:

    public func doSomething<T>(a:T) { /* ... */ }

Swift requires the metadata for type T in order for this function to access anything related to ‘a’. Therefore when this gets compiled, the compiler will add in an extra implicit parameter which is the type metadata for T. When presenting this in C#, tom-swifty will make any implicit metadata parameters explicit. This is done in `TopLevelFunctionCompiler.AddExtraGenericArguments`. All generic arguments are added *unless* there is a generic class (and only class) somewhere in the parameter list on the same variable. In this case, the metadata for the generic type will be available in instance of the class and will be accessed at runtime. In addition, if one or more generic type references have protocol constraints, the associated protocol witness table will be appended as additional arguments.

In other words, if I have this function:

    public func doSomething<T, U, V:Thing, W>(a:T, b:Thing<U>, c: U, d: V, e: W> where
        W: Spot, W : Dog {
    // ...
    } 

There are 5 explicit arguments, each of which is generic in some fashion, but the actual signature of the function is quite different:

    public func doSomething(a:T, b:Thing<T>, c: U, d: V, e: W, // <- explicit
      t:Metadata,
      v:Metadata,
      vpThing: ProtocolWitness // protocol witness table for V wrt Thing
      w:Metadata,
      wpDog: ProtocolWitness // protocol witness table for W wrt Dog
      wpSpot: ProtocolWitness // protocol witness table for W wrt Dog 
      ) {
      // ...
    }

Note that there are now 6 extra implicit parameters to the function and note that there is no Metadata parameter for generic type U since is referenced in Thing<U>.

In addition to the other weirdness, note that the order of the protocol constraint arguments is different. For multiple protocol constraints, the swift compiler will order them lexicographically rather than in declaration order. I have no idea what the sort order would be if the constraints contain emoji.

For the implementation of the calling code, `MarshalEngine.AddExtraGenericParameters` handles the work of adding in the references for the calling code.

Although all these extra arguments seem wasteful, they are necessary for a fully generalized implementation. The swift compiler will make specialized versions of any generic function. This is possible because when swift compiles code into a module, it includes the full abstract syntax tree of the source. It can then macro substitute in the specialized types. Nifty.

Generic reference types in swift are indexed via a pair of integral coordinates which are the depth and index.

If I have a top level generic function:

    public func topLevel<T, U>(a:T, b:U) { /* ... */ } 

The coordinates of T are (0, 0) and U is (0, 1)

If I have a function in a generic class:

    public class Foo<T, U> {
        public func method<T>(a:T, b: U, c:V) { /* ... */ }
    }

The coordinate of T is (1, 0), U is (1, 1) and V is (0, 0).

In the tom-swifty codebase, I generally use a tuple of two ints to represent the coordinates, always in the (depth, index) order.

In `BaseDeclaration` there are functions for getting the depth and index of a generic reference as well as finding out if a given type or type name is a generic reference.

Similarly, I use depth and index in Dynamo for both C# and swift generic reference types.

Virtual generic classes have all the virtual methods in a vtable. Unlike normal virtual classes, there can’t be a single vtable for the type. For each virtual class, I keep a cache of vtables for each specialization of the generic type. The implementation of the cache should be swift a dictionary mapping a set of  metadata objects onto the cache. Unfortunately, the metadata object in swift doesn’t conform to the Hashable protocol. However, every metadata object (and class instance) in swift has a unique ObjectIdentifier, which does conform to Hashable, so I made a utility class in swift, `TypeCacheKey` in `typecachekey.swift` which handles the hashing. References to this are in `OverrideBuilder.cs` in `DefineVtableGetter` and `DefineVtableSetter`.


## Optionals

Optionals are syntax sugar onto compiler generated generic enumerations. Something akin to:

    public enum option<T> {
    case some(T)
    case none
    }

when I coded in support for optionals, there was no support for generics, so putting support in for optionals was not possible. Also, the generalized support for enums feels clunky because ultimately swift enums are discriminated unions which don’t exist in C#. Another option was to try to decompose the actual data within a swift optional myself, but even though it seems simple to try to extract the discriminator from the data type, but the rules for getting the information out of the union at runtime are baroque and require digging into the payload’s metadata object. It seemed simplest to make a SwiftOptional<T> type in C# with in-built methods to marshal back and forth to swift. The binary representation that gets passed back and forth is a [Medusa tuple](http://plinth.org/techtalk/?p=183). I may change this to use the native C# option with outside conversion methods.

## Exceptions

Swift exceptions are not exceptions. They can’t be because exceptions that didn’t interfere with ARC would be hugely expensive. Functions that are declared as throwing instead have an implicit extra return value. In a sane world, functions that throw would return a swift enum of the form:

    public enum error<T, U> {
    case thrown(T)
    case value(U)
    }

Or something like that that encompasses two possible values. Instead, errors are implemented in the ABI as having an implicit second return value in a non-ABI compliant register. What this means is that any swift function that throws can’t be called directly from C#. I solve this similarly to optional by using a [Medusa tuple](http://plinth.org/techtalk/?p=183) that holds either the exception or the return value but not both. If a swift function can throw, tom-swifty injects code that will decompose the return value and if it threw and exception, the error will be translated into an exception. If there exists an equivalent C# exception (generated by tom-swifty), you get that. If there is not, you will get a `SwiftException`.
If a swift virtual method in C# can throw and an overrider throws, then tom-swifty injects a try/catch block to catch exceptions thrown in C#. If the C# exception is SwiftException, then it will pass the SwiftError it contains to swift. If it is not a SwiftException, we construct a DotNetError (see `swifternumerror.swift`) to hand to swift.
In either case the return value or the swift error gets bundled up into a Medusa tuple which gets handed back to swift.
`MarshalEngine.cs` has the implementation of the C# return value marshaling for swift functions that can throw.
`MarshalEngineCSafeSwiftToCSharp.cs` and `NewClassCompiler.cs` have the virtual method code that catches the C# exceptions and translates them to swift errors.

# Runtime Libraries: What, Why, and How

There are two runtime libraries dependencies created by tom-swifty:

- SwiftRuntimeLibrary - contains all C# infrastructure for representing swift types and marshaling
- XamGlue - tools used by swift wrappers to make marshaling easier

These are necessary because there are some operations that done all the time and are either far to large or complicated to do inline in generated code.
XamGlue includes tools to do the following:

- Convert a values to and from an UnsafeRawPointer (`void *`)
- Convert marshals/exceptions to medusa tuples
- Adapt closures
- Accessors for dictionary methods that can be called directly from C#
- Accessors for array methods that can be called directly from C#
- Create a swift string from UTF16 encoded text
- Represent .NET exceptions as a swift error
- Provide a data type for caching vtables for generic classes

SwiftRuntimeLibrary has the following elements in it:

- Attributes for labeling “swiftable" types
- Interfaces for swift type representations - there is no real “root” type in swift. I chose to mirror this by using interfaces:
  - ISwiftObject
  - ISwiftNominalType
  - ISwiftEnum
  - ISwiftStruct
  - ISwiftProtocolImpl
  - ISwiftError
- Marshaling tools
- Hand-compiled adapters for arrays, dictionaries, optionals
- SwiftException related types
- P/Invokes into the libSwiftCore.dylib
- Object registry
# What the Heck is plist-swifty?

One of the things that is necessary in order to create a working framework for a swift module is to create an `info.plist` file. 

plist-swifty is a command line app that uses some code inside of tom-swifty that creates an info.plist file by analyzing a dylib file.

There is no open tool that I could find in Apple’s toolchain to do this. It appears to be done by the Xcode app and only the Xcode app. In the process of building tom-swifty, I need to build a swift runtime library (XamGlue) for each target platform as a framework. In order to do this, I either need an Xcode project or a makefile/script that can build the frameworks. I opted for the latter since it appears to work well enough and because I also need that code within tom-swifty since tom-swifty has to build wrapping frameworks to match input frameworks for target os/cpu.

It does this by first creating a default dictionary with minimal keys and values following the Apple documentation (such as it is).
It then adds in operating system specifics.
This is all done in `InfoPList.cs`.
`PLItems.cs` contains a simple hierarchy that represents the structures in an `info.plist` file with the appropriate infrastructure to write it out as reasonably well-formed XML.

I do not claim to be an expert on `info.plist` files nor do I claim that this code couldn’t be improved. I had a need and I could fill that need in a half day that I couldn’t find a decent other way to fill.

