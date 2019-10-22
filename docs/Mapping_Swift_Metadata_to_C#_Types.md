# Mapping Swift Metadata to C# Types
There will times when we are passed a swift data type and its metadata and need to map it to a C# type. In most cases, we already know what C# type to expect (yay, static typing!), but in some cases we won’t. For example, if we’re passed an existential container, we will have no idea whatsoever what the actual type is supposed to be. We can, however, figure it out with some effort. The best way to do this is to classify the metadata and work from there:


## Non-Generic Nominal Types (structs, enums, classes, protocols)

Method 1:
Make a hash table that maps metadata → C# Type. This can be built on first access by looping over all loaded assemblies an finding all swift mapped types and adding calling `StructMarshal.Marshaler.Metadataof` on each. This is likely a slow call. Lookup will be fast, initial access will be slow. Subsequent access will be fast.

Method 2:
Make a hash table that maps swift type name → C# Type.  We can easily add a new attribute to all bound swift types, say `[SwiftTypeName(``"``module.class.inner``"``)]`. On first access, loop over all types looking for that attribute and hash the name → C# Type. Getting the swift type name at runtime is straight forward, but potentially slow. Encoding may be funny for Bockovers. Lookup will be fast. Initial access will be. Subsequent access will be fast, but slightly slower than method 1. This method will be more uniform for generics. Takes up more space, could tweak the garbage collector because type names are not stored contiguously in swift.


## Generic Nominal Types

Make a hash table that maps swift type name → C#. Built in the same way as method 2. Upon lookup, if the the metadata is generic, recurse on its generic specialization(s). Then call `Type.MakeGenericType` which the generic specializations.


## Tuple Types

Look up the C# type for each type in the tuple. Call `StructMarshal.Marshaler.MakeTupleType` to get the tuple type.


## Closure Types

Look up the C# types for each type in the argument list. Look up the C# type for the return type, if any.  Then call the appropriate flavor of `typeof (Action<,,>).MakeGenericType ()` or `typeof (Func<,,>).MakeGenericType()`.

All other types throw for now, but we’ll need to handle existential containers, metatypes (type types) and some of the other esoterica.


## Tentative Design

Since swift type metadata objects are singletons, it is most appropriate to use a single hash table that maps from `SwiftMetatype` → `Type`. If the key isn’t present, the tools should go through a series of steps to find it. First, check to see if the type is a generic nominal. If so, get the name of the swift host type from the `NominalTypeDescriptor` and look up the host type from a secondary cache. If present, use the C# type and `MakeGenericType ()` to make the final type. Add to the primary cache and return it. If the type isn’t present in the cache by name, look it up in all assemblies that have not already been scanned.
Foreach assembly, foreach type, if the type has on it `SwiftTypeNameAttribute`, add the type and name to the secondary cache, then `MakeGenericType ()` and return it.
If the type is non-generic and not present, scanning the assemblies can be done, but nothing should be added to the secondary cache. Instead, the metadata and type should be added directly to the main cache.
If the type is a tuple, the C# type will be built as above and put into the primary cache. If the type is a closure, it will be built as above and added to the primary cache.



