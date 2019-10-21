# Value Type Modeling
There are two broad types of value types in swift: those that can be modeled with scalars and those that can’t. Types that can’t be modeled with scalars are modeled as classes. This is not the only possible division. Types could be distinctly modeled based on blittable and non-blittable types or potentially non-blittable types, for example, but this approach was a headache and an unending source of bugs.


## Types That Are Modeled With Scalars

The following table lists the Swift types and the corresponding C# types.

| Swift Type                      | C# Type  |
| ------------------------------- | -------- |
| `Swift.Int64`                   | `long`   |
| `Swift.UInt64`                  | `ulong`  |
| `Swift.Int32`                   | `int`    |
| `Swift.UInt32`                  | `uint`   |
| `Swift.Int16`                   | `short`  |
| `Swift.UInt16`                  | `ushort` |
| `Swift.Int8`                    | `sbyte`  |
| `Swift.UInt8`                   | `byte`   |
| `Swift.UnsafeRawPointer`        | `IntPtr` |
| `Swift.UnsafeMutableRawPointer` | `IntPtr` |
| `Int`                           | `nint`   |
| `UInt`                          | `nuint`  |
| `Bool`                          | `bool`   |
| `Float`                         | `float`  |
| `Double`                        | `double` |

There are some swift enums that can be modeled directly as C# enums. I classify enums in a number of ways:
Integral - the enum has an integral raw type or every element has an integral payload.
Homogeneous - the enum’s payload types are all identical (say, all Int or all String)
Trivial - the enum has no inheritance and no raw type and none of its elements has a payload or the enum is both homogeneous and integral.

Trivial enums can be modeled with C# enums.


## Types That Are Modeled With Classes

All non-scalar structs and non-trivial enums are modeled with classes in C#. The modeled types are decorated with either `ISwiftEnum` or `ISwiftStruct`. Both of these interfaces inherit from `ISwiftNominalType`, which in turn inherits from `ISwiftDisposable` . `ISwiftNominalType` contains one property `SwiftData` which is the data payload for the type.

The reason why these types are `IDisposable` is that swift structs and enums behave differently from C# value types in that there are clear semantics for what happens to them when the come into and go out of scope which may include changes to contained data reference counts or destructors. As a result, at the very least we need to model this and `Disposable` is the way to go.

As a consequence, **it is almost never correct to directly copy the C#** `**SwiftData**`. If you do this and the type contains reference counted type(s), you will be creating a memory leak.

Each C# binding will get tagged with either `SwiftStructAttribute` or `SwiftEnumAttribute`, both of which inherit from `SwiftNominalTypeAttribute`. This attribute contains information about the swift type.

Given the following swift struct declaration:
```swift
    public struct BarInt {
        public var X:Int; 
        public init(x:Int) {
            X = x;
        }
    }
```

Binding Tools for Swift will generate the following C# (function bodies left empty):


```csharp    
    using System;
    using System.Runtime.InteropServices;
    using SwiftRuntimeLibrary;
    using SwiftRuntimeLibrary.SwiftMarshal;
    
    namespace NewClassCompilerTests
    {
        [SwiftStruct("libNewClassCompilerTests.dylib",
            "$s21NewClassCompilerTests6BarIntVMn", 
            "$s21NewClassCompilerTests6BarIntVN", "")]
        public class BarInt : ISwiftStruct
        {
            public BarInt(nint x)
            {
            }
            internal BarInt(SwiftNominalCtorArgument unused)
            {
            }
            public static SwiftMetatype GetSwiftMetatype()
            {
            }
            public void Dispose()
            {
            }
            void Dispose(bool disposing)
            {
            }
            ~BarInt()
            {
            }
            public byte[] SwiftData
            {
                get; set;
            }
            public nint X
            {
                get { }
                set { }
            }
        }
    }
```
You’ll note that there are two constructors. The first maps onto the `init` method inside the swift class. The second is an internal constructor that gets used to define an uninitialized type. This constructor gets used by the marshaler when a type needs to be allocated before it gets used, for example, as a return value. Swift semantics don’t allow programs to explicitly have variables in an uninitialized state. However, this happens implicitly. C# needs this ability. Be aware that a C# value type that is uninitialized is **invalid** in swift nor does swift have the notion of `default (T)` as in C#.

All properties bound in C# access the value through accessor functions. If a swift property (or a swift method) mutates the value, the contents of the C# `SwiftData` array will get changed as well.

Given this swift enum:

    public enum FooECTIA {
        case a(Int)
        case b(Double)
    }

Binding Tools for Swift will generate the following C# binding (empty method bodies):


    using System;
    using System.Runtime.InteropServices;
    using SwiftRuntimeLibrary;
    using SwiftRuntimeLibrary.SwiftMarshal;
    
    namespace NewClassCompilerTests
    {
        public enum FooECTIACases
        {
            A, B
        }
        [SwiftEnumType("libNewClassCompilerTests.dylib",
            "$s21NewClassCompilerTests8FooECTIAOMn", 
            "$s21NewClassCompilerTests8FooECTIAON", "")]
        public class FooECTIA : ISwiftEnum
        {
            public void Dispose()
            {
            }
            void Dispose(bool disposing)
            {
            }
            ~FooECTIA()
            {
            }
            public static FooECTIA NewA(nint value0)
            {
            }
            public static FooECTIA NewB(double value0)
            {
            }
            public byte[] SwiftData
            {
                get; set;
            }
            public nint ValueA
            {
                get { }
            }
            public double ValueB
            {
                get { }
            }
            public FooECTIACases Case
            {
                get { }
            }
        }
    }

First, there is an enum with elements for each case in the swift type. Then, like the struct example, the C# enum representation gets adorned with `SwiftEnumAttribute`. Unless there is an `init` method in the original swift type, there will be no constructors.
The `Case` property returns a `TypeCases` value that indicates the current case of the enum. For each case with a payload, there will be a corresponding `Value` property that returns the payload. Each `Value` property will do a check to ensure that that case of the enum matches the value case. If there is a mismatch, it will throw an `ArgumentOutOfRangeException`.

