# Closure Modeling
Swift has two different closure types: escaping and non-escaping. Escaping closures are closures that may continue to exist outside of the context that created them. For example, a closure parameter to a function that gets stored in a member variable in a class *must* be marked `@escaping` or you get an error in swift. Non-escaping closures must not be stored beyond the context in which they’re declared and may not refer to data outside the closure.

Binding Tools for Swift can model escaping closures, but can’t fully model non-escaping closures. There is a workaround, however. The Swift compiler has some magic that can turn a non-escaping closure into an escaping closure, with the caveat that it can't fully escape. The trick for this is use the swift function `withoutActuallyEscaping` which converts a non-escaping closure into an escaping one within the context of another closure only. 

The reason why is that C# can call very few swift functions or methods directly because of differences in the parameter passing conventions between swift and C#. Since swift closures expect the caller or the callee to match the calling conventions, Binding Tools for Swift has to change the closures to adapt.  For example, given the following swift closure:

```swift
    (String, Point?, Int) -> SomeProtocol
```
C# can’t call that directly since the calling conventions would be problematic, but C# could call a closure with this signature:
```swift
    (UnsafeMutablePointer<SomeProtocol>, OpaquePointer) -> ()
```
What happens is that for any closure of type `(args)->return`, Binding Tools for Swift transforms it into a closure of type `(UnsafeMutablePointer<return>, OpaquePointer)->()`. C# *can* call this closure since it is equivalent to `Action<IntPtr, IntPtr>`. Similarly, given the C# type `Func<return, args>`, Binding Tools for Swift can transform it into `Action<IntPtr, IntPtr>` and inject the appropriate marshaling to prepare the arguments.

Given the following swift code:
```swift
    public final class FooCCTDoubleFalse {
        public init() { }
        public func runIt(f:()->Double) -> Double
        {
            return f()
        }
    }
```
C# will generate the following code for the method `runIt`:
```csharp
    public  double RunIt(Func<double> f)
    {
        IntPtr thisIntPtr = StructMarshal.RetainSwiftObject(this);
        double retval;
        retval = NativeMethodsForFooCCTDoubleFalse.PIrunIt(thisIntPtr,
                    SwiftObjectRegistry.Registry.SwiftClosureForDelegate(f,
                        SwiftClosureRepresentation.FuncCallbackVoid,
                        new Type[] { }, typeof(double)));
                SwiftCore.Release(thisIntPtr);
        return retval;
    }
```
`PIrunIt` is a pinvoke into a swift wrapper function. It takes a closure of type `@escaping (UnsafeMutablePointer<Double>, OpaquePointer) -> ()` and returns a `Double`. The C# method `SwiftObjectRegistry.Registry.SwiftClosureForDelegate` adapts the delegate `f` into this type so it can be called from swift.

`SwiftClosureRepresentation` is a struct that represents a memory model for a swift closure. In addition, it contains a set of static methods that act as universal callback points for any swift closure. These methods identify the original C# delegate, marshal the arguments passed in from swift to C#, then calls the C# delegate. If there was a return value, it gets marshaled to swift.

For closures that throw, we treat them like functions that throw in that we change the return value from say, `TR` and turn it into a pointer to a medusa tuple of the form `UnsafeMutablePointer<(TR, Swift.Error, Bool)>`. This allows us to pass back to the caller the return value if an exception was not thrown and the Error if it was.
There are two types of closures from the C# point of view: `Func<T..., TR>` and `Action<T...>`. Actions, obviously, have no return value, but in the case of actions that throw, we still need to handle that so we can use a reduced medusa tuple of the form `(Swift.Error, Bool)`
For async closures, it looks like we'll have to hoist the async-ness into a `Swift.Task` return type.