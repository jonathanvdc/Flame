using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// An instruction prototype for call instructions: instructions that
    /// call a delegate.
    /// </summary>
    public sealed class CallPrototype : InstructionPrototype
    {
        private CallPrototype(IMethod calleeSignature)
        {
            this.CalleeSignature = calleeSignature;
        }

        /// <summary>
        /// Gets the signature of the callee delegate.
        /// </summary>
        /// <returns>The callee's signature.</returns>
        public IMethod CalleeSignature { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType
            => CalleeSignature.ReturnParameter.Type;

        /// <inheritdoc/>
        public override int ParameterCount
            => (CalleeSignature.IsStatic ? 1 : 2) + CalleeSignature.Parameters.Count;

        /// <inheritdoc/>
        public override ExceptionSpecification ExceptionSpecification
            // TODO: use the callee signature's exception specification instead
            // of a throw-any exception specification.
            => ExceptionSpecification.ThrowAny;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            var errors = new List<string>();

            bool isStatic = CalleeSignature.IsStatic;
            if (!isStatic)
            {
                var thisType = body.Implementation
                    .GetValueType(GetThisArgument(instance))
                    as PointerType;

                if (thisType == null)
                {
                    errors.Add("Type of the 'this' argument must be a pointer type.");
                }
                else if (!thisType.Equals(CalleeSignature.ParentType))
                {
                    errors.Add(
                        string.Format(
                            "Pointee type of 'this' argument type '{0}' should " +
                            "have been parent type '{1}'.",
                            thisType.FullName,
                            CalleeSignature.ParentType.FullName));
                }
            }

            var parameters = CalleeSignature.Parameters;
            var argList = GetArgumentList(instance);
            int paramCount = parameters.Count;
            for (int i = 0; i < paramCount; i++)
            {
                var paramType = parameters[i].Type;
                var argType = body.Implementation.GetValueType(argList[i]);

                if (!paramType.Equals(argType))
                {
                    errors.Add(
                        string.Format(
                            "Argument of type '{0}' was provided where an " +
                            "argument of type '{1}' was expected.",
                            paramType.FullName,
                            argType.FullName));
                }
            }

            return errors;
        }

        /// <summary>
        /// Gets the callee delegate in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The callee delegate.</returns>
        public ValueTag GetCalleeDelegate(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return instruction.Arguments[0];
        }

        /// <summary>
        /// Gets the 'this' argument in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The 'this' argument.</returns>
        public ValueTag GetThisArgument(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            ContractHelpers.Assert(!CalleeSignature.IsStatic);
            return instruction.Arguments[1];
        }

        /// <summary>
        /// Gets the argument list argument in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The formal argument list.</returns>
        public ReadOnlySlice<ValueTag> GetArgumentList(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            int offset = CalleeSignature.IsStatic ? 1 : 2;
            return new ReadOnlySlice<ValueTag>(
                instruction.Arguments,
                offset,
                instruction.Arguments.Count - offset);
        }

        private static readonly InterningCache<CallPrototype> instanceCache
            = new InterningCache<CallPrototype>(
                new StructuralCallPrototypeComparer());

        /// <summary>
        /// Gets the call instruction prototype for a particular callee signature.
        /// </summary>
        /// <param name="calleeSignature">The signature of the callee delegate.</param>
        /// <returns>A call instruction prototype.</returns>
        public static CallPrototype Create(IMethod calleeSignature)
        {
            return instanceCache.Intern(new CallPrototype(calleeSignature));
        }
    }

    internal sealed class StructuralCallPrototypeComparer
        : IEqualityComparer<CallPrototype>
    {
        public bool Equals(CallPrototype x, CallPrototype y)
        {
            return object.Equals(x.CalleeSignature, y.CalleeSignature);
        }

        public int GetHashCode(CallPrototype obj)
        {
            return obj.CalleeSignature.GetHashCode();
        }
    }
}