using Flame.Compiler.Instructions;
using Flame.Compiler.Transforms;
using Flame.Compiler;
using Flame.Collections;
using Flame.TypeSystem;
using System.Linq;
using Flame.Constants;

namespace Flame.Clr.Transforms
{
    /// <summary>
    /// An intraprocedural transform that turns Flame IR's dedicated
    /// delegate instructions into CIL idioms.
    /// </summary>
    public sealed class LowerDelegates : IntraproceduralOptimization
    {
        private LowerDelegates()
        { }

        /// <summary>
        /// An instance of the CIL delegate canonicalization transform.
        /// </summary>
        public static readonly LowerDelegates Instance = new LowerDelegates();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            foreach (var instruction in builder.Instructions)
            {
                var proto = instruction.Prototype;
                if (proto is IndirectCallPrototype)
                {
                    // Flame IR has dedicated instructions for delegate calls,
                    // but in CIL they are implemented by a virtual call to
                    // a magic 'Invoke' method.
                    var callProto = (IndirectCallPrototype)proto;
                    var calleeValue = callProto.GetCallee(instruction.Instruction);
                    var delegateType = graph.GetValueType(calleeValue);
                    IMethod invokeMethod;
                    if (TypeHelpers.TryGetDelegateInvokeMethod(delegateType, out invokeMethod))
                    {
                        instruction.Instruction = Instruction.CreateCall(
                            invokeMethod,
                            MethodLookup.Virtual,
                            calleeValue,
                            callProto.GetArgumentList(instruction.Instruction).ToArray());
                    }
                }
                else if (proto is NewDelegatePrototype)
                {
                    // CIL delegates are created by first loading a function pointer
                    // onto the stack (using either `ldftn` or `ldvirtftn`) and then
                    // constructing the actual delegate using a `newobj` opcode.

                    var newDelegateProto = (NewDelegatePrototype)proto;
                    var delegateType = newDelegateProto.ResultType;

                    IMethod invokeMethod;
                    if (!TypeHelpers.TryGetDelegateInvokeMethod(delegateType, out invokeMethod))
                    {
                        continue;
                    }

                    var constructor = delegateType.Methods.Single(method => method.IsConstructor);
                    bool isVirtual = newDelegateProto.Lookup == MethodLookup.Virtual;

                    // First create an instruction that loads the function pointer.
                    var functionPointer = instruction.InsertBefore(
                        Instruction.CreateNewDelegate(
                            constructor.Parameters[1].Type,
                            newDelegateProto.Callee,
                            isVirtual
                                ? newDelegateProto.GetThisArgument(instruction.Instruction)
                                : null,
                            newDelegateProto.Lookup),
                        instruction.Tag.Name + "_fptr");

                    // CLR delegate constructors always take two parameters: a function
                    // pointer and a 'this' argument (of type 'Object *box').
                    // The latter can be somewhat tricky to get right:
                    //
                    //   * if the delegate has a 'this' argument then that 'this' argument
                    //     should be reinterpreted as an instance of 'Object *box'.
                    //
                    //   * if the delegate does not have a 'this' argument, then we pass
                    //     a `null` constant as 'this' argument.

                    ValueTag thisArgument;
                    if (newDelegateProto.HasThisArgument)
                    {
                        thisArgument = newDelegateProto.GetThisArgument(instruction.Instruction);

                        var thisType = builder.GetValueType(thisArgument);
                        var expectedThisType = constructor.Parameters[0].Type;
                        
                        if (thisType != expectedThisType)
                        {
                            thisArgument = functionPointer.InsertBefore(
                                Instruction.CreateReinterpretCast(
                                    (PointerType)expectedThisType,
                                    thisArgument));
                        }
                    }
                    else
                    {
                        thisArgument = functionPointer.InsertBefore(
                            Instruction.CreateConstant(
                                NullConstant.Instance,
                                constructor.Parameters[0].Type));
                    }

                    // And then create the actual delegate. To do so we must use the
                    // delegate type's constructor. This constructor will always take
                    // two parameters: a bound object (of type System.Object) and a
                    // function pointer (of type natural int).
                    instruction.Instruction = Instruction.CreateNewObject(
                        constructor,
                        new[]
                        {
                            thisArgument,
                            functionPointer
                        });
                }
            }
            return builder.ToImmutable();
        }
    }
}
