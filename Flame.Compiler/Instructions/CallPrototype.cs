using System.Collections.Generic;
using Flame.Collections;

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
            // TODO: implement this
            throw new System.NotImplementedException();
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