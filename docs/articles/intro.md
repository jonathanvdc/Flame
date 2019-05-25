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

Flame represents method bodies as control-flow graphs (CFGs) of instructions in SSA form. This representation makes it easier for analyses and transformations to reason about the computations performed by a method.

Flame IR's control-flow graphs are immutable data structures that have mutable wrappers for easy manipulation, so most concepts in Flame IR have both an immutable and mutable API. These APIs are usually very similar, with the former being geared towards analysis and the latter intended mainly for IR construction and transformation.

These are the main data structures in Flame IR:
  * A **control-flow graph** is a sets of basic blocks. Exactly one basic block in every control-flow graph is marked as the **entry point block**, which means that control is transferred to that block when the method is invoked for which the control-flow graph is its implementation.

    [Immutable API: `FlowGraph`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.FlowGraph.html). [Mutable API: `FlowGraphBuilder`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.FlowGraphBuilder.html).
  * Conceptually, a **basic block** is a straight-line sequence of instructions that ends in a control-flow instruction. In Flame IR, a basic block consists of four main components:
    1. A **unique tag** that identifies the basic block.
    2. A sequence of **block parameters**, which allow for arbitrary values to be passed from one block to another. This is primarily a means for overcoming the limitations imposed by SSA form in a disciplined manner. Flame's block parameters roughly correspond to the phi functions found in compiler literature.
    3. A sequence of **named instructions**: instructions that are each identified by a unique tag.
    4. Block flow: the block's outgoing control flow.

    [Immutable API: `BasicBlock`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.BasicBlock.html). [Mutable API: `BasicBlockBuilder`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.BasicBlockBuilder.html).
  * An **instruction** is an expression that accepts zero or more values and produces exactly one value. Every instruction consists of two components: a prototype and an argument list.
    * An instruction **prototype** describes the instruction's semantics. For example, instructions that produce constants have prototype [`ConstantPrototype`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Instructions.ConstantPrototype.html) whereas instructions that call a method have prototype [`CallPrototype`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Instructions.CallPrototype.html).
    Prototypes are never specific to the control-flow graph in which they occur.
    * An **argument list** is simply a list of values defined in the enclosing control-flow graph. For this reason, argument lists are always specific to the control-flow graph in which they occur.

    Instructions are *named* if they are defined directly by a basic block. Instructions are *anonymous* if they are defined by block flow, which in turn appears in a basic block. Named instructions can be used as values by other instructions. Anonymous instructions cannot; their values are consumed immediately by the block flow that defines them. Examples of anonymous instructions include the condition of `switch` flow, the result returned by `return` flow and the "dangerous" instruction wrapped by `try` flow.

    [Immutable API: `Instruction`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Instruction.html). [Mutable API: `InstructionBuilder`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.InstructionBuilder.html).

  * **Block flow** represents the control flow that terminates a basic block. Flame defines five types of control flow:
    1. **Jump** ([`JumpFlow`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Flow.JumpFlow.html)): an unconditional branch to some other block in the control-flow graph. Corresponds to a `goto` statement in C#.
    2. **Return** ([`ReturnFlow`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Flow.ReturnFlow.html)): returns exactly one value to the caller. A value of type `void` can be returned to indicate that no actual value should be returned.
    3. **Switch** ([`SwitchFlow`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Flow.SwitchFlow.html)): branches to one of many blocks by comparing a condition with a number of constants. Corresponds to a `switch` statement in C#, but is also used to represent `if` statements.
    4. **Try** ([`TryFlow`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Flow.TryFlow.html)): executes an instruction and checks if that instruction throws an exception. If it does, an exception-path branch is taken. Otherwise, a success-path branch is taken.
    5. **Unreachable** ([`UnreachableFlow`](https://jonathanvdc.github.io/Flame/api/Flame.Compiler.Flow.UnreachableFlow.html)): indicates that the end of a particular basic block is unreachable.

### Example: a factorial function

To illustrate what Flame IR looks like in practice, we'll consider the recursive factorial function below.

```csharp
public static int FactorialRecursive(int value, int accumulator)
{
    if (value > 1)
    {
        return FactorialRecursive(value - 1, value * accumulator);
    }
    else
    {
        return accumulator;
    }
}
```

After compiling this using `csc`, we get the following IL:

```
.method public static hidebysig default int32 FactorialRecursive (int32 'value', int32 accumulator) cil managed
{
    // Method begins at RVA 0x2076
    // Code size 18 (0x12)
    .maxstack 8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  ble.s IL_0010

    IL_0004:  ldarg.0
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  mul
    IL_000a:  call int32 class Program::FactorialRecursive(int32, int32)
    IL_000f:  ret
    IL_0010:  ldarg.1
    IL_0011:  ret
}
```

We now feed the `csc`-compiled `exe` to `ilopt` with the `--print-ir` option. `ilopt` reports the following optimized IR. If you look carefully, you'll see that Flame eliminated the recursive call to `FactorialRecursive`, replacing it with a branch to the entry point block.

```
{
    #entry_point(
        @entry-point.thunk,
        #(#param(System::Int32, @value.thunk), #param(System::Int32, @accumulator.thunk)),
        { },
        #goto(@entry-point(@value.thunk, @accumulator.thunk)));

    #block(
        @entry-point,
        #(#param(System::Int32, value), #param(System::Int32, accumulator)),
        {
            IL_0000_val_1 = const(1, System::Int32)();
            IL_0000_val_2 = intrinsic(@arith.gt, System::Boolean, #(System::Int32, System::Int32))(value, IL_0000_val_1);
        },
        #switch(
            copy(System::Boolean)(IL_0000_val_2), // <-- value to switch on
            IL_0009(), // <-- 'default' case
            {
                #case(#(@false), IL_001D()); // <-- 'case false'
            }));

    #block(
        IL_0009,
        #(),
        {
            @IL_0000_val_1.rff.IL_0009 = const(1, System::Int32)();
            IL_0009_val_2 = intrinsic(@arith.sub, System::Int32, #(System::Int32, System::Int32))(value, @IL_0000_val_1.rff.IL_0009);
            IL_0009_val_5 = intrinsic(@arith.mul, System::Int32, #(System::Int32, System::Int32))(value, accumulator);
        },
        #goto(@entry-point(IL_0009_val_2, IL_0009_val_5)));

    #block(
        IL_001D,
        #(),
        { },
        #return(copy(System::Int32)(accumulator)));
};
```

`ilopt` selects the following CIL for the snippet of Flame IR above.

```
.method public static hidebysig default int32 FactorialRecursive (int32 'value', int32 accumulator) cil managed
{
    // Method begins at RVA 0x2078
    // Code size 24 (0x18)
    .maxstack 3
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  starg 1

    IL_0006:  dup
    IL_0007:  starg 0

    IL_000b:  ldc.i4.1
    IL_000c:  ble.s IL_0016

    IL_000e:  ldarg.0
    IL_000f:  ldc.i4.1
    IL_0010:  sub
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  mul
    IL_0014:  br.s IL_0002

    IL_0016:  ldarg.1
    IL_0017:  ret
}
```
