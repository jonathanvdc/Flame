using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Collections;
using Flame.Compiler.Flow;
using Flame.TypeSystem;

namespace Flame.Compiler
{
    /// <summary>
    /// A method body: a method implementation represented as a
    /// control-flow graph along with a private copy of the return
    /// parameter, 'this' parameter and input parameters.
    /// </summary>
    public sealed class MethodBody
    {
        /// <summary>
        /// Creates a method implementation.
        /// </summary>
        /// <param name="returnParameter">
        /// The method implementation's return parameter.
        /// </param>
        /// <param name="thisParameter">
        /// The method implementation's 'this' parameter.
        /// </param>
        /// <param name="parameters">
        /// The method implementation's input parameters.
        /// </param>
        /// <param name="implementation">
        /// The method implementation, represented
        /// as a control-flow graph.
        /// </param>
        public MethodBody(
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            FlowGraph implementation)
        {
            this.ReturnParameter = returnParameter;
            this.ThisParameter = thisParameter;
            this.Parameters = parameters;
            this.Implementation = implementation;
        }

        /// <summary>
        /// Gets the method implementation's return parameter.
        /// </summary>
        /// <returns>The return parameter.</returns>
        public Parameter ReturnParameter { get; private set; }

        /// <summary>
        /// Gets the method implementation's 'this' parameter, if any.
        /// </summary>
        /// <returns>The 'this' parameter.</returns>
        public Parameter ThisParameter { get; private set; }

        /// <summary>
        /// Gets the method implementation's input parameter list.
        /// </summary>
        /// <returns>The parameter list.</returns>
        public IReadOnlyList<Parameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the control-flow graph that constitutes the method
        /// implementation.
        /// </summary>
        /// <returns>The method implementation.</returns>
        public FlowGraph Implementation { get; private set; }

        /// <summary>
        /// Applies a member mapping to this method body's implementation,
        /// 'this' parameter, parameter list and return parameter.
        /// The result is returned as a new method body.
        /// </summary>
        /// <param name="memberMapping">The member mapping to apply.</param>
        /// <returns>A new method body.</returns>
        public MethodBody Map(MemberMapping memberMapping)
        {
            var newRetParam = ReturnParameter.Map(memberMapping);
            var newThisParam = ThisParameter.Type == null
                ? ThisParameter
                : ThisParameter.Map(memberMapping);
            var newParamList = Parameter.MapAll(Parameters, memberMapping);
            var newImpl = Implementation.Map(memberMapping);
            return new MethodBody(newRetParam, newThisParam, newParamList, newImpl);
        }

        /// <summary>
        /// Gets a sequence that contains all members that appear in the method body.
        /// </summary>
        /// <value>A sequence of types, methods and fields.</value>
        public IEnumerable<IMember> Members
        {
            get
            {
                var results = new HashSet<IMember>();
                Map(
                    new MemberMapping(
                        t => { results.Add(t); return t; },
                        m => { results.Add(m); return m; },
                        f => { results.Add(f); return f; }));
                return results;
            }
        }

        /// <summary>
        /// Creates a method body based on this method body, but
        /// with a different implementation.
        /// </summary>
        /// <param name="newImplementation">
        /// The implementation of the method body to create.
        /// </param>
        /// <returns>
        /// A new method body.
        /// </returns>
        public MethodBody WithImplementation(FlowGraph newImplementation)
        {
            return new MethodBody(
                ReturnParameter,
                ThisParameter,
                Parameters,
                newImplementation);
        }

        /// <summary>
        /// Validates this method body and returns a list of error messages.
        /// </summary>
        /// <returns>A list of error messages.</returns>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            // Validate all instructions.
            foreach (var insnTag in Implementation.InstructionTags)
            {
                ValidateInstruction(
                    Implementation.GetInstruction(insnTag).Instruction,
                    errors);
            }

            // Validate control flow.
            foreach (var blockTag in Implementation.BasicBlockTags)
            {
                var block = Implementation.GetBasicBlock(blockTag);
                var flow = block.Flow;
                foreach (var innerInsn in flow.Instructions)
                {
                    ValidateInstruction(innerInsn, errors);
                }

                if (flow is TryFlow)
                {
                    var tryFlow = (TryFlow)flow;
                    ValidateBranch(
                        tryFlow.SuccessBranch,
                        BranchArgumentKind.TryResult,
                        flow,
                        errors);

                    ValidateBranch(
                        tryFlow.ExceptionBranch,
                        BranchArgumentKind.TryException,
                        flow,
                        errors);
                }
                else
                {
                    foreach (var branch in flow.Branches)
                    {
                        ValidateBranch(branch, BranchArgumentKind.Value, flow, errors);
                    }
                }
            }

            return errors;
        }

        private bool CheckArgsInGraph(Instruction instruction)
        {
            foreach (var arg in instruction.Arguments)
            {
                if (!Implementation.ContainsValue(arg))
                {
                    return false;
                }
            }
            return true;
        }

        private void ValidateInstruction(
            Instruction instruction, List<string> errors)
        {
            if (CheckArgsInGraph(instruction))
            {
                errors.AddRange(instruction.Validate(this));
            }
            else
            {
                errors.Add("Instruction argument not in graph.");
            }
        }

        private void ValidateBranch(
            Branch branch,
            BranchArgumentKind extraAllowedKind,
            BlockFlow flow,
            List<string> errors)
        {
            if (Implementation.ContainsBasicBlock(branch.Target))
            {
                var blockParams = Implementation.GetBasicBlock(branch.Target).Parameters;
                int blockParamCount = blockParams.Count;
                if (blockParamCount != branch.Arguments.Count)
                {
                    errors.Add(
                        string.Format(
                            "Branch argument count ('{0}') mismatches target " +
                            "block parameter count ('{1}').",
                            branch.Arguments.Count,
                            blockParamCount));
                }
                else
                {
                    for (int i = 0; i < blockParamCount; i++)
                    {
                        var arg = branch.Arguments[i];

                        if (arg.IsValue)
                        {
                            var argType = Implementation.GetValueType(arg.ValueOrNull);
                            if (!argType.Equals(blockParams[i].Type))
                            {
                                errors.Add(
                                    string.Format(
                                        "Branch argument type '{0}' mismatches target " +
                                        "block parameter type '{1}'.",
                                        branch.Arguments.Count,
                                        blockParams[i].Type));
                            }
                        }
                        else if (arg.Kind != extraAllowedKind)
                        {
                            errors.Add(
                                string.Format(
                                    "Branch argument kind '{0}' is not allowed in this " +
                                    "branch of '{1}' flow.",
                                    arg.Kind,
                                    flow));
                        }
                    }
                }
            }
            else
            {
                errors.Add("Branch to block outside of graph.");
            }
        }
    }
}