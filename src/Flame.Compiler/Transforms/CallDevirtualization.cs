using Flame.Compiler.Instructions;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The call devirtualization optimization: a transform that takes iterates over virtual
    /// calls in a control-flow graph and tries to reduce them to a static call.
    /// </summary>
    public sealed class CallDevirtualization : IntraproceduralOptimization
    {
        private CallDevirtualization()
        { }

        /// <summary>
        /// An instance of the call devirtualization transform.
        /// </summary>
        public static readonly CallDevirtualization Instance = new CallDevirtualization();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            foreach (var instruction in builder.Instructions)
            {
                TrySimplify(instruction);
            }
            return builder.ToImmutable();
        }

        private static bool TrySimplify(InstructionBuilder instruction)
        {
            var proto = instruction.Prototype;
            if (proto is ConstrainedCallPrototype)
            {
                //
                //     constrained_call(f)(this_ref, args...)
                //
                // is equivalent to
                //
                //     call(impl(f), static)(this_ref, args...)  if this_ref == T ref* where T is a value type,
                //     call(f, virtual)(load(this_ref), args...) if this_ref == T any* ref*.

                var constrainedCallProto = (ConstrainedCallPrototype)proto;
                var thisArg = constrainedCallProto.GetThisArgument(instruction.Instruction);
                var thisRefType = instruction.Graph.GetValueType(thisArg) as PointerType;
                if (thisRefType == null)
                {
                    return false;
                }

                var thisValType = thisRefType.ElementType;
                if (thisValType is PointerType)
                {
                    instruction.Instruction = Instruction.CreateCall(
                        constrainedCallProto.Callee,
                        MethodLookup.Virtual,
                        instruction.InsertBefore(
                            Instruction.CreateLoad(thisValType, thisArg),
                            "this_value"),
                        constrainedCallProto.GetArgumentList(instruction.Instruction).ToArray());

                    TrySimplify(instruction);
                    return true;
                }
                else if (!(thisValType is IGenericParameter))
                {
                    var realCallee = thisValType.GetImplementationOf(constrainedCallProto.Callee);
                    if (realCallee != null && realCallee.ParentType == thisValType)
                    {
                        instruction.Instruction = Instruction.CreateCall(
                            realCallee,
                            MethodLookup.Static,
                            constrainedCallProto.GetThisArgument(instruction.Instruction),
                            constrainedCallProto.GetArgumentList(instruction.Instruction).ToArray());

                        return true;
                    }
                }
            }
            else if (proto is CallPrototype)
            {
                var callProto = (CallPrototype)proto;
                if (callProto.Callee.IsStatic)
                {
                    return false;
                }

                var thisType = GetActualType(
                    callProto.GetThisArgument(instruction.Instruction),
                    instruction.Graph.ImmutableGraph) as PointerType;

                if (thisType == null)
                {
                    return false;
                }

                var realCallee = thisType.ElementType.GetImplementationOf(callProto.Callee);
                if (realCallee == null || realCallee == callProto.Callee)
                {
                    return false;
                }

                instruction.Instruction = Instruction.CreateCall(
                    realCallee,
                    realCallee.IsVirtual() ? MethodLookup.Virtual : MethodLookup.Static,
                    instruction.InsertBefore(
                        Instruction.CreateReinterpretCast(
                            realCallee.ParentType.MakePointerType(thisType.Kind),
                            callProto.GetThisArgument(instruction.Instruction))),
                    callProto.GetArgumentList(instruction.Instruction).ToArray());

                return true;
            }
            return false;
        }

        private static IType GetActualType(ValueTag value, FlowGraph graph)
        {
            NamedInstruction insn;
            if (graph.TryGetInstruction(value, out insn))
            {
                var proto = insn.Prototype;
                if (proto is ReinterpretCastPrototype || proto is CopyPrototype)
                {
                    return GetActualType(insn.Arguments[0], graph);
                }
            }
            return graph.GetValueType(value);
        }
    }
}
