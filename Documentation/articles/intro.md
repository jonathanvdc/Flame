# A brief introduction to Flame

This article is designed to be a quick introduction to the concepts that matter most to Flame. Some previous experience with the .NET ecosystem and optimizing compilers might be useful.

The two most important categories of data structures in Flame are its type system and its intermediate representation (IR). The former roughly corresponds to the type system of the .NET universe modulo some tweaks. The latter is in [static single assignment (SSA) form](https://en.wikipedia.org/wiki/Static_single_assignment_form), which is the state of the art in optimizing compilers.

## Flame's type system

Flame's type system is essentially a stripped-down version of the type system of the .NET universe. All types must implement the [`IType` interface](https://jonathanvdc.github.io/Flame/api/Flame.IType.html). Key points include:
  * Flame has first-class support for object-oriented programming constructs.
    - Types can define fields, methods and properties.
    - Types can define other types.
    - Types and methods can have generic parameters. Types, fields, methods and properties can be instantiate either directly if they define generic parameters and indirectly if an enclosing type defines generic parameters.
  * There are no built-in types in Flame. If you want to refer to, e.g., a 32-bit signed integer, then you should load `System.Int32` from a core library. This is easy in practice: the [`TypeEnvironment` class](https://jonathanvdc.github.io/Flame/api/Flame.TypeSystem.TypeEnvironment.html) offers convenient access to primitives types such as integers, floating-point numbers, Booleans, characters and strings.

    Removing built-in types was a deliberate decision based on experience with a previous iteration of Flame that did have built-in types for integers&mdash;library types like `System.Int32` implement various bits of functionality that cannot easily be modeled in built-in types. When Flame had built-in types, accessing that functionality was tremendously difficult and complicated the way the type system works.
  * Flame uses a single pointer type ([`PointerType`](https://jonathanvdc.github.io/Flame/api/Flame.TypeSystem.PointerType.html)) to unify many different kinds of pointers from the .NET world.
    - Regular pointers are represented as transient pointers ([`PointerKind.Transient`](https://jonathanvdc.github.io/Flame/api/Flame.TypeSystem.PointerKind.html#Flame_TypeSystem_PointerKind_Transient)).
    - References (`ref`/`out`/`byref`) are represented as reference pointers ([`PointerKind.Reference`](https://jonathanvdc.github.io/Flame/api/Flame.TypeSystem.PointerKind.html#Flame_TypeSystem_PointerKind_Reference)).
    - Instances of reference types (i.e., classes) and boxed values are represented as box pointers ([`PointerKind.Box`](https://jonathanvdc.github.io/Flame/api/Flame.TypeSystem.PointerKind.html#Flame_TypeSystem_PointerKind_Box)). This is a notable departure from the .NET type system, where instances of reference types are implicit pointers rather than explicit pointers. The rationale for this decision is that explicit pointers are easier to reason about in optimizations and analyses than implicit pointers&mdash;explicit pointers reduce the number of special cases there.

## Flame IR

Flame represents method bodies as control-flow graphs of instructions in SSA form. This representation makes it easier for analyses and transformations to reason about the computations performed by a method.
