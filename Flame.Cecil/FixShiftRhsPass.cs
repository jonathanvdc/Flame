using System;
using Flame.Compiler.Visitors;
using Flame.Compiler;
using Flame.Compiler.Expressions;

namespace Flame.Cecil
{
    /// <summary>
    /// A pass that casts the right-hand sides of shift operators to int32
    /// if it is larger than int32.
    /// </summary>
    public sealed class FixShiftRhsPass : NodeVisitorBase, IPass<IStatement, IStatement>
    {
        private FixShiftRhsPass()
        { }

        /// <summary>
        /// Gets the one and only instance of this pass.
        /// </summary>
        public static readonly FixShiftRhsPass Instance = new FixShiftRhsPass();

        /// <summary>
        /// The name of the fix shift rhs pass.
        /// </summary>
        public const string FixShiftRhsPassName = "fix-shift-rhs";

        public override bool Matches(IExpression Value)
        {
            return Value is LeftShiftExpression
            || Value is RightShiftExpression;
        }

        public override bool Matches(IStatement Value)
        {
            return false;
        }

        protected override IExpression Transform(IExpression Expression)
        {
            var binOp = (BinaryExpression)Expression;
            var rhsSpec = binOp.RightOperand.Type.GetIntegerSpec();
            if (rhsSpec != null && rhsSpec.Size > 32)
            {
                var newRhs = new StaticCastExpression(
                    binOp.RightOperand,
                    rhsSpec.IsSigned
                    ? PrimitiveTypes.Int32
                    : PrimitiveTypes.UInt32).Simplify();

                if (binOp is LeftShiftExpression)
                    return new LeftShiftExpression(binOp.LeftOperand, newRhs);
                else
                    return new RightShiftExpression(binOp.LeftOperand, newRhs);
            }
            else
            {
                return Expression;
            }
        }

        protected override IStatement Transform(IStatement Statement)
        {
            return Statement.Accept(this);
        }

        public IStatement Apply(IStatement Statement)
        {
            return Visit(Statement);
        }
    }
}

