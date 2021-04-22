# XML Reflection
In order to properly represent the Swift API in C#, it’s necessary to be able to reflect on that API in some way. Presently, there is no usable reflection in the swift language, although there are some data types compiled into a module that look promising (these are descriptor types: module descriptor, class descriptor, function descriptor, etc), but there are a number of problems with doing this:

1. very limited attributes come through
2. the view of the types is limited by the target platform/OS
3. the presence of the descriptors is controllable via a swift compiler switch which means they could be arbitrarily left out

New with swift 5.1 is a means of providing the front-facing API to the user via a text-only format. This text-only format is swift with no function contents, meaning that you need almost an entire swift parser to read it in. This is a sneaky hack on Apple’s part as they already have their own swift parser. We can do this and write a parser. I’d estimate that it’s a person year to do since swift is particularly tricky in that parsing of it requires some state-smart tricks since attributes have their own grammar that is unlike everything else. In addition, care needs to be taken to ensure that identifiers work properly with the Unicode subset that is allowable in swift. For the last part, I don’t know which parser engine would be appropriate. Maybe ANTLR (there exists a Swift 3.1 parser).
In our case, we hook into the swift compiler itself. This presents a number of benefits for us:

1. We end up consuming the private binary parse tree serialization present in `.swiftmodule` files
2. We take full advantage of the compiler infrastructure for module loading and resolution
3. We take full advantage of the compiler infrastructure for visiting the parse tree
4. In the future, we should be able to also consume the text serialization of the data
5. We have full access to attributes

This is not without its problems:

1. The build/test cycle is slow because any changes trigger a full rebuild of the compiler and all runtime libraries
2. The deliverables are huge. They can be trimmed down since we don’t need everything that gets built, but care needs to be taken so we don’t accidentally trim out something that should get shipped
3. This ties our builds to Apple’s builds since the compiler will hard fail on version mismatches between the compiler and `.swiftmodule` files

We are now adding in a separate reflection component. This is a parser that can consume `.swiftinterface` files and will generate the corresponding XML. I chose to generate XML rather than going directly to the `*Declaration` classes because the XML isn't going away anytime soon and it honestly keeps the code cleaner.

## Hooking Into the Compiler

To hook into the compiler, I added some code into argument checking in `swift/tools/driver/driver.cpp` to flag for reflection and then call the reflector. For the actual reflection, there is a new class called `ReflectionPrinter` which is modeled off the built-in tool to generate ObjC and lives in `swift/tools/driver/xamarinreflect_main.cpp`.

The Apple infrastructure follows a visitor pattern. To get reflection, we implement a number of methods named `visitFooDecl` where `Foo` is the particular language element that we care about. For the most part, these are all the top level declarations: class, struct, enum, protocol, extension, function, variable, name alias, bound generic types.

In the infrastructure, there is code to handle output indentation. `indent` increases the indentation level. `exdent` decreases the indentation level. `indents` prints 3 spaces per indent level.

## Implementing the Parser

The parser is generated from an ANTLR grammar that is based on a more-or-less complete swift language description provided in the sample grammars in ANTLR. It differs in that it corrects issues with nosebleed unicode as well as hacking out the language elements that just aren't used at all (code statements, for example). There are two passes that are made to an input file. The first is code to desugar the syntax which turns references to optional types into `Swift.Optional<>` and other similar things.

## XML Layout

The reflection output is meant to be compliant with standard XML, although there is no dtd (yet). It starts with the header:

    <?xml version="1.0" encoding="utf-8"?>
    <xamreflect version="1.0">
    <modulelist>

and will end with:

    </modulelist>
    </xamreflect>


## Module

A module list contains 0 or more `<module>` elements. A module will have the following attributes:

- name - a string that contains the name of the swift module
- swiftVersion - a string that prints the swift language version of the module

A `<module>` element will contain 0 or more declarations of the types:

- extension
- func
- type declaration
- property

## Type Aliases

Swift type aliases get aggregated into a list of `<typealias>` that live inside a `<typealiases>` tag.
Typealiases are only needed when typealiases are present and the types presented have not been flattened to their actual type.
The reflector flattens all aliased types, so it includes no typealiases. The `.swiftinterface` parser can't flatten typealiases, so it includes them.

A `<typealias>` tag will contain the following attributes:

- name - a string that represents the type being aliases including any generic part of its signature
- accessibility - one of `Public`, `Private`, `Internal`, `Open`
- type - a string that represents the target type to which name is aliased.
For example,
```Swift
public typealias Foo<T> = Swift.Array<T:Hashable, String>
```
should produce this:
```XML
<typealias name="Foo<T>" accessibility="Public" type="Swift.Array<T:Hashable, String>" />
```

## Type Declaration

A `<typedeclaration>` element has the following attributes:

- kind - a string which is one of `class`, `struct`, `enum`, or `protocol`
- module - an optional name of the parent module if and only if the declaration has been unrooted from its module
- accessibility - one of `Public`, `Private`, `Internal`, `Open`
- isObjC - boolean
- isFinal - boolean
- isDeprecated - boolean
- isUnavailable - boolean

A `<typedeclaration>` element may contain 0 or 1 `<innerclasses>` elements which is a list of inner declarations of type class.
A `<typedeclaration>` element may contain 0 or 1 `<innerstructs>` elements which is a list of inner declarations of type struct.
A `<typedeclaration>` element may contain 0 or 1 `<innerenums>` elements which is a list of inner declarations of type enum.
A `<typedeclaration>` element may contain 0 or 1 `<members>` elements which is a list of inner declarations of member elements.
A `<typedeclaration>` element may contain 0 or 1 `<inherits>` elements which is a list of inheritance.

## Members

A `<members>` element contains 0 or more elements of type `<func>`, `<property>`, or `<typedeclaration>`.

## Functions

A `<func>` element represents a method or a top-level function. It contains the following attributes:

- name - a string representing the function’s name
- accessibility - one of `Public`, `Private`, `Internal`, `Open`
- returnType - a string which is a type specification (`TypeSpec`) that represents a type
- isProperty - boolean
- isStatic - boolean
- isFinal - boolean
- operatorKind - a string which is one of `None`, `Prefix`, `Postfix`, or `Infix`
- hasThrows - boolean
- isDeprecated - boolean
- isUnavailable - boolean
- isOptional - boolean
- objcSelector - a string representing the ObjC selector for the function
- isRequired - boolean
- isConvenienceInit - boolean

In addition, a `<func>` element may contain a `<parameterlists>` element.

## Parameter Lists

In swift 2.2 and earlier swift supported functions with multiple parameter lists. In swift 2.2, this was only used implicitly in instances methods by using one parameter list for the instance and one for the method’s parameters, deprecating explicitly declaring functions with multiple parameter lists. Starting in swift 4.0, the compiler no longer represented swift instance methods as having multiple parameter lists.  However, this usage was so heavy in our code that I elected to maintain the illusion of having multiple parameter lists.
`<parameterlists>` contains 0 or more elements of type `<parameterlist>`.
A `<parameterlist>` element contains 1 attribute:

- index - an integer representing 0 based index of the parameter list (in theory they should always come in order from the reflector, but just in case…)

A `<parameterlist>` contains 0 or more `<parameter>` elements.
A `<parameter>` element represents a parameter in a parameter list. It contains the following attributes:

- publicName - a string representing the publicly facing name of the parameter
- privateName - a string representing the private facing name of the parameter
- type - a string representing the type specification (`TypeSpec`) of the type
- isVariadic - boolean
## Inheritance

The `<inherits>` element contains 0 or more elements of type `<inherit>`. An `<inherit>` element contains the following attributes:

- type - a string representing the type specification (`TypeSpec`) of the type that is being inherited from
- inheritanceKind - one of `protocol` or `class`.
## Properties

A `<property>` element contains the following attributes:

- name - a string representing the property’s name
- accessibility - one of `Public`, `Private`, `Internal`, `Open`
- type - a string which is a type specification (`TypeSpec`) that represents the property type
- storage - one of `Addressed`, `AddressedWithObservers`, `AddressedWithTrivialAccessors`, `Computed`, `ComputedWithMutableAddress`, `Inherited`, `InheritedWithObservers`, `Stored`, `StoredWithObservers`, `StoredWithTrivialAccessors`, `Coroutine`, `MutableAddressor`.
- isStatic - boolean
- isDeprecated - boolean
- isUnavailable - boolean
- isOptional - boolean


## Extensions

An `<extension>` element contains the following attribute:

- onType - a string containing the fully-qualified name of the type being extended.

It contains the following elements:

- members - a list of members in the extension (see above)
- inherits - a list of inheritance (see above)

## Attributes

An `<attribute>` element will always appear as part of an `<attributes>` list.
An `<attribute>` element contains the following attribute:

- name - a string containing the name of the attribute. For example `@objc` has the name `"objc"`

An attribute may also contain an `<attributeparameterlist>` element which will contain one or more `<attributeparameter>` elements.

An `<attributeparameter>` element contains the following:

- kind - one of `Sublist`, `Label`, or `Literal`.
- value - if the kind is **not** `Sublist`, there will be a `value` attribute that contains the value of the element.

If the kind is `Sublist`, there will be an `<attributeparameterlist>` inside the attribute.

attribute parameters will contain any punctuation allowed (for example commas or quotation marks).
Sublists are generally set off when an attribute contains what the swift grammar calls "balanced tokens". So you might have something like this:
```Swift
@someAttribute(a, b: [a list, of things, to consider { or not }])
```
Attributes are not currently supported in the reflector (and may never be). They are going to be supported in the `.swiftinterface` parser.
Currently, attributes only appear on functions and nominal types, but in the future, they will be exposed on everything in the language that allows them.
