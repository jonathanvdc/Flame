using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    /// <summary>
    /// Defines a member signature pass that applies two passes sequentially.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AggregateMemberSignaturePass<T> : IPass<MemberSignaturePassArgument<T>, MemberSignaturePassResult>
        where T : IMember
    {
        /// <summary>
        /// Creates a member signature pass that applies the given passes
        /// in sequence.
        /// </summary>
        /// <param name="First"></param>
        /// <param name="Second"></param>
        public AggregateMemberSignaturePass(
            IPass<MemberSignaturePassArgument<T>, MemberSignaturePassResult> First,
            IPass<MemberSignaturePassArgument<T>, MemberSignaturePassResult> Second)
        {
            this.First = First;
            this.Second = Second;
        }

        /// <summary>
        /// Gets the first pass that this aggregate member signature pass applies.
        /// </summary>
        public IPass<MemberSignaturePassArgument<T>, MemberSignaturePassResult> First { get; private set; }
        /// <summary>
        /// Gets the second pass that this aggregate member sognature pass applies.
        /// </summary>
        public IPass<MemberSignaturePassArgument<T>, MemberSignaturePassResult> Second { get; private set; }

        public MemberSignaturePassResult Apply(MemberSignaturePassArgument<T> Value)
        {
            return MemberSignaturePassResult.Combine(First.Apply(Value), Second.Apply(Value));
        }
    }
}
