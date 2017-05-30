using System;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Native;
using Flame.Compiler.Visitors;

namespace Flame.Wasm.Passes
{
    public class CallLoweringPass : NodeVisitorBase, IPass<IStatement, IStatement>
    {
        public CallLoweringPass(IStackAbi Abi)
        {
            this.Abi = Abi;
        }

        /// <summary>
        /// The name of the call lowering pass.
        /// </summary>
        public const string CallLoweringPassName = "lower-call";

        /// <summary>
        /// Gets this pass' abi.
        /// </summary>
        public IStackAbi Abi { get; private set; }

        public override bool Matches(IExpression Value)
        {
            return Value is InvocationExpression;
        }

        public override bool Matches(IStatement Value)
        {
            return false;
        }

        protected override IExpression Transform(IExpression Expression)
        {
            var invExpr = ((InvocationExpression)Expression).Simplify();
            var target = invExpr.Target.GetEssentialExpression();
            if (target is GetMethodExpression)
            {
                var getMethodExpr = (GetMethodExpression)target;
                return Abi.CreateDirectCall(
                    getMethodExpr.Target, getMethodExpr.Caller, 
                    invExpr.Arguments).Accept(this);
            }
            throw new NotSupportedException("Indirect calls are not supported at this time.");
        }

        protected override IStatement Transform(IStatement Statement)
        {
            return Statement;
        }

        public IStatement Apply(IStatement Value)
        {
            return Visit(Value);
        }
    }
}

