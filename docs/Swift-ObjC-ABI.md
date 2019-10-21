# Swift ObjC ABI
Swift objects are very similar to ObjC objects in that they share a fair amount in the layout (first pointer is a like an ISA, for example) and swift objects will respond to a minimum of messages from ObjC (retain, release, respondsToSelector), but they do not have anything beyond the minimum.

When an object in Swift is adorned with `@objc` and inherits from `NSObject`, then the rules change. Swift treats the object nearly identically to an `NSObject`.  The thing is that if the compiler knows that the type is a swift type, then it will dispatch using a vtable entry instead of a selector. If it doesn’t know the type then it will go through selector dispatch.

Here’s a concrete example:

    import Foundation
    
    @objc
    public protocol Talky {
        func speak()
    }
    
    @objc
    public class Dog : NSObject, Talky {
        private var says: String
        public init (saying: String) {
            says = saying
        }
        public func speak() {
            print (says)
        }
    }
    
    let y = Dog (saying: "woof.")
    y.speak()

In this case, we get the following assembly language for the invocation of speak:

    0000000100001bd2         mov        rax, qword [__T010unitHelper1yAA3DogCv]
    0000000100001bd9         mov        rdx, qword [rax]
    0000000100001bdc         and        rdx, qword [rcx]
    0000000100001bdf         mov        r13, rax
    0000000100001be2         call       qword [rdx+0x70]

In the first line, we load up the value of y into rax. Then we grab the ISA pointer in rdx and mask it off with the value of a global called swift_isa_mask. We put the instance pointer in r13 then do a vtable dispatch to call speak. This does not use a selector.

If we change the declaration of y to `let y:Talky = Dog (saying: "woof.")`, so that it’s considered to be an ObjC protocol, then we get selector dispatch:

    0000000100001b3b         mov        rax, qword [__T010unitHelper1yAA5Talky_pv]
    0000000100001b42         mov        rsi, qword [0x1005b1350] // "speak"
    0000000100001b49         mov        rdi, rax
    0000000100001b4c         call       imp___stubs__objc_msgSend

In the first line we put the value of y into rax. The second line puts the selector for `speak` into rsi (which is argument 2 to `_objc_msgSend`), then it moves the instance into rdi (first argument to `_objc_msgSend`).

As a bit of fun swift syntax, you can call a method on a protocol by name, even if you don’t if the type of the object.

    let y:AnyObject = Dog (saying: "woof.")
    y.speak?()

Note that this doesn’t work for any arbitrary name after `y.` it *has* to be a member of a known protocol. In this case, the compiler injects a call to `respondsToSelector` and if it exists, it allocates an object of some kind (I would expect it to be an existential container, but it’s not) that holds a pointer to the instance and a pointer to partial application forwarder, which is a thunk that ends up calling the actual protocol method via `_objc_msgSend`.

This little bit of trivia doesn’t affect us as far as I can tell, but is interesting to see and to note that the `foo.selector?` syntax is very expensive.

This leaves us with an interesting choice. We could treat them like actual swift objects because the implementation is available to us and we can invoke methods without the msgSend and not bother with selectors. If the object inherits from a known ObjC object, we should have it in the type database and know how to marshal it.

In terms of Automatic Reference Counting, it appears to follow the same process as a typical swift object being passed to a function of calling retain in the caller and release in the callee. Likely, the same change will be in place for Swift 4.2 and later of having the caller do both acquire and release.

