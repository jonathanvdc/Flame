using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    /// <summary>
    /// Defines a member lowering pass that applies two passes sequentially.
    /// </summary>
    public sealed class AggregateMemberLoweringPass : IPass<MemberLoweringPassArgument, MemberConverter>
    {
        /// <summary>
        /// Creates a member lowering pass that applies the given passes
        /// in sequence.
        /// </summary>
        /// <param name="First"></param>
        /// <param name="Second"></param>
        public AggregateMemberLoweringPass(
            IPass<MemberLoweringPassArgument, MemberConverter> First,
            IPass<MemberLoweringPassArgument, MemberConverter> Second)
        {
            this.First = First;
            this.Second = Second;
        }

        /// <summary>
        /// Gets the first pass that this aggregate member lowering pass applies.
        /// </summary>
        public IPass<MemberLoweringPassArgument, MemberConverter> First { get; private set; }

        /// <summary>
        /// Gets the second pass that this aggregate member lowering pass applies.
        /// </summary>
        public IPass<MemberLoweringPassArgument, MemberConverter> Second { get; private set; }

        public MemberConverter Apply(MemberLoweringPassArgument Value)
        {
            var firstConv = First.Apply(Value);
            var secondConv = Second.Apply(Value);
            return new MemberConverter(
                new CompositeConverter<IType, IType, IType>(
                    firstConv.TypeConverter,
                    secondConv.TypeConverter),
                new CompositeConverter<IMethod, IMethod, IMethod>(
                    firstConv.MethodConverter,
                    secondConv.MethodConverter),
                new CompositeConverter<IField, IField, IField>(
                    firstConv.FieldConverter,
                    secondConv.FieldConverter));
        }
    }
}
