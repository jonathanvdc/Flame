using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Compiler.Pipeline;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The inlining optimization: a transform that copies the function bodies
    /// of callees into the caller, replacing the call with its implementation.
    /// </summary>
    public class Inlining : Optimization
    {
        /// <summary>
        /// Creates an instance of the inlining optimization.
        /// </summary>
        protected Inlining()
        { }

        /// <summary>
        /// An instance of the inlining transformation.
        /// </summary>
        /// <value>A call-inlining transform.</value>
        public static readonly Inlining Instance
            = new Inlining();

        /// <inheritdoc/>
        public override bool IsCheckpoint => false;

        /// <inheritdoc/>
        public override async Task<MethodBody> ApplyAsync(MethodBody body, OptimizationState state)
        {
            var graph = body.Implementation.ToBuilder();

            // Find all inlinable instructions in the method body, map them
            // to their callees.
            var candidates = new Dictionary<InstructionBuilder, IMethod>();
            foreach (var instruction in graph.Instructions)
            {
                IMethod callee;
                if (TryGetCallee(instruction, out callee)
                    && ShouldConsider(callee, state.Method))
                {
                    candidates[instruction] = callee;
                }
            }

            // Request method bodies for all callees.
            var bodies = await state.GetBodiesAsync(candidates.Values);

            // Actually inline method bodies.
            foreach (var pair in candidates)
            {
                var instruction = pair.Key;
                var calleeBody = bodies[pair.Value];
                if (calleeBody == null || !CanInline(calleeBody, state.Method, graph.ImmutableGraph))
                {
                    continue;
                }

                TryInlineCall(instruction, calleeBody);
            }

            return body.WithImplementation(graph.ToImmutable());
        }

        private void TryInlineCall(InstructionBuilder instruction, MethodBody calleeBody)
        {
            var graph = instruction.Graph;
            var proto = instruction.Prototype;
            if (proto is CallPrototype)
            {
                if (ShouldInline(calleeBody, instruction.Arguments, graph.ImmutableGraph))
                {
                    instruction.ReplaceInstruction(calleeBody.Implementation, instruction.Arguments);
                }
            }
            else if (proto is NewObjectPrototype)
            {
                graph.TryForkAndMerge(builder =>
                {
                    // Synthesize a sequence of instructions that creates a zero-filled
                    // object of the right type.
                    var builderInsn = builder.GetInstruction(instruction);
                    var elementType = ((NewObjectPrototype)proto).Constructor.ParentType;
                    var defaultConst = builderInsn.InsertBefore(
                        Instruction.CreateConstant(DefaultConstant.Instance, elementType));
                    var objInstance = builderInsn.InsertBefore(
                        Instruction.CreateBox(elementType, defaultConst));

                    var allArgs = new ValueTag[] { objInstance }.Concat(builderInsn.Arguments).ToArray();
                    if (ShouldInline(calleeBody, allArgs, builder.ImmutableGraph))
                    {
                        // Actually inline the newobj call. To do so, we'll rewrite all 'return'
                        // flows in the callee to return the 'this' pointer and then inline that
                        // like a regular method call.
                        var modifiedImpl = calleeBody.Implementation.ToBuilder();
                        foreach (var block in modifiedImpl.BasicBlocks)
                        {
                            if (block.Flow is ReturnFlow)
                            {
                                block.Flow = new ReturnFlow(
                                    Instruction.CreateCopy(
                                        modifiedImpl.EntryPoint.Parameters[0].Type,
                                        modifiedImpl.EntryPoint.Parameters[0].Tag));
                            }
                        }
                        builderInsn.ReplaceInstruction(modifiedImpl.ToImmutable(), allArgs);
                        return true;
                    }
                    else
                    {
                        // We won't be inlining after all. Nix our changes.
                        return false;
                    }
                });
            }
        }

        private bool TryGetCallee(InstructionBuilder instruction, out IMethod callee)
        {
            var proto = instruction.Prototype;
            if (proto is CallPrototype)
            {
                var callProto = (CallPrototype)proto;
                if (callProto.Lookup == MethodLookup.Static)
                {
                    callee = callProto.Callee;
                    return true;
                }
            }
            else if (proto is NewObjectPrototype)
            {
                var newObjproto = (NewObjectPrototype)proto;
                callee = newObjproto.Constructor;
                return true;
            }

            callee = null;
            return false;
        }

        // The methods and properties below constitute the inlining heuristic to use.
        // To implement a custom inlining heuristic, inherit from this class and
        // override them.

        /// <summary>
        /// Tells if a method should be considered for inlining.
        /// </summary>
        /// <param name="callee">A method that might be inlined.</param>
        /// <param name="caller">
        /// A method that calls <paramref name="callee"/>; it's not sure if it
        /// should consider inlining <paramref name="callee"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if inlining may proceed; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool ShouldConsider(IMethod callee, IMethod caller)
        {
            // Don't inline recursive calls. We have the infrastructure to do
            // so, but inlining recursive calls if often pointless.
            callee = callee.GetRecursiveGenericDeclaration();
            if (callee == caller)
            {
                return false;
            }

            // By default, we don't want to inline methods across assemblies
            // because doing so anyway introduces dependencies on implementation
            // details in external dependencies that may be notice to change.
            var calleeAsm = callee.GetDefiningAssemblyOrNull();
            if (calleeAsm == null)
            {
                return true;
            }
            var callerAsm = caller.GetDefiningAssemblyOrNull();
            if (callerAsm == null)
            {
                return true;
            }
            else
            {
                return calleeAsm == callerAsm;
            }
        }

        /// <summary>
        /// Determines if a method body can be inlined, that is, if invalid
        /// code will be generated by inlining it.
        /// </summary>
        /// <param name="calleeBody">
        /// A method body that is an inlining candidate.
        /// </param>
        /// <param name="caller">
        /// The caller that wants to inline <paramref name="calleeBody"/>.
        /// </param>
        /// <param name="callerBody">
        /// The method body for <paramref name="caller"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if inlining <paramref name="calleeBody"/> into <paramref name="caller"/> is
        /// safe; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool CanInline(MethodBody calleeBody, IMethod caller, FlowGraph callerBody)
        {
            // To determine if we can inline a method, we essentially just need to
            // consider all members that show up in the method body and see if the
            // caller is allowed to access them.

            var rules = callerBody.GetAnalysisResult<AccessRules>();
            var members = calleeBody.Implementation
                .NamedInstructions.Select(insn => insn.Instruction)
                .Concat(calleeBody.Implementation.BasicBlocks.SelectMany(block => block.Flow.Instructions))
                .SelectMany(insn => insn.Prototype.Members);

            foreach (var item in members)
            {
                if (item is IType && !rules.CanAccess(caller, (IType)item))
                {
                    return false;
                }
                else if (item is ITypeMember && !rules.CanAccess(caller, (ITypeMember)item))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if a method body is worth inlining.
        /// </summary>
        /// <param name="body">
        /// The method body to inline.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to feed to <paramref name="body"/>.
        /// </param>
        /// <param name="caller">
        /// The control-flow graph of the caller, which defines the arguments.
        /// </param>
        /// <returns>
        /// <c>true</c> if the method body should be inlined; otherwise, <c>false,</c>.
        /// </returns>
        protected bool ShouldInline(
            MethodBody body,
            IReadOnlyList<ValueTag> arguments,
            FlowGraph caller)
        {
            return GetInlineCost(body) <= GetInlineGain(body, arguments, caller);
        }

        /// <summary>
        /// Gauges a particular method body's inline cost.
        /// </summary>
        /// <param name="body">A method body that might be inlined.</param>
        /// <returns>A number that represents the method body's inline cost.</returns>
        protected virtual int GetInlineCost(MethodBody body)
        {
            // The bigger a method body is, the less inclined we'll be to inline it.
            return body.Implementation.NamedInstructions.Count()
                + body.Implementation.BasicBlocks.Count();
        }

        /// <summary>
        /// Gauges a particular method body's inline gain.
        /// </summary>
        /// <param name="body">A method body that might be inlined.</param>
        /// <param name="arguments">
        /// The list of arguments to feed to <paramref name="body"/>.
        /// </param>
        /// <param name="caller">
        /// The control-flow graph of the caller, which defines the arguments.
        /// </param>
        /// <returns>
        /// A number that represents how much new information we expect to gain from inlining.
        /// </returns>
        protected virtual int GetInlineGain(
            MethodBody body,
            IReadOnlyList<ValueTag> arguments,
            FlowGraph caller)
        {
            // Inline method bodies containing up to ten instructions/blocks.
            // TODO: be smarter about arguments:
            //   * Inlining a method that takes a more derived type as argument
            //     may result in a direct call getting turned into an indirect
            //     call.
            int gain = 10;
            foreach (var arg in arguments)
            {
                NamedInstruction argInstruction;
                if (caller.TryGetInstruction(arg, out argInstruction))
                {
                    if (argInstruction.Prototype is AllocaPrototype)
                    {
                        // Inlining a method that takes an `alloca` argument may result
                        // in a level of memory indirection getting stripped away.
                        gain += 4;
                        continue;
                    }
                }

                var type = caller.GetValueType(arg);

                // Inlining means that we don't have to pass around big arguments.
                // Encourage inlining methods that take hefty arguments.
                gain += EstimateTypeSize(type) / 4;
            }
            gain += EstimateTypeSize(body.ReturnParameter.Type) / 4;
            return gain;
        }

        private static int EstimateTypeSize(IType type)
        {
            var intSpec = type.GetIntegerSpecOrNull();
            if (intSpec != null)
            {
                return intSpec.Size / 8;
            }
            else if (type is PointerType || type.IsSpecialType())
            {
                return 8;
            }
            else
            {
                return type.GetAllInstanceFields()
                    .Select(field => field.FieldType)
                    .Select(EstimateTypeSize)
                    .Sum();
            }
        }
    }
}
