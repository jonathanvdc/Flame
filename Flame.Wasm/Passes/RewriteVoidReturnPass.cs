using System;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Native;
using Flame.Compiler.Visitors;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Compiler.Expressions;

namespace Flame.Wasm.Passes
{
    /// <summary>
    /// A visitor that rewrites void returns as break statements 
    /// from an outer tagged block.
    /// </summary>
    public sealed class RewriteVoidReturnVisitor : StatementVisitorBase
    {
        public RewriteVoidReturnVisitor(UniqueTag BreakTag)
        {
            this.BreakTag = BreakTag;
        }

        /// <summary>
        /// Gets the tag of the tagged block to break to.
        /// </summary>
        public UniqueTag BreakTag { get; private set; }

        public override bool Matches(IStatement Value)
        {
            return Value is ReturnStatement;
        }

        protected override IStatement Transform(IStatement Statement)
        {
            return new BreakStatement(BreakTag);
        }
    }

    /// <summary>
    /// A pass that rewrites void returns as break statements 
    /// from an outer tagged block.
    /// </summary>
    public sealed class RewriteVoidReturnPass : IPass<BodyPassArgument, IStatement>
    {
        private RewriteVoidReturnPass()
        { }

        public static readonly RewriteVoidReturnPass Instance = new RewriteVoidReturnPass();

        /// <summary>
        /// The name of the rewrite void return pass.
        /// </summary>
        public const string RewriteVoidReturnPassName = "rewrite-return-void";

        public IStatement Apply(BodyPassArgument Argument)
        {
            if (!Argument.DeclaringMethod.ReturnType.Equals(PrimitiveTypes.Void))
                return Argument.Body;

            var outerTag = new UniqueTag("outer");
            return new TaggedStatement(
                outerTag, new RewriteVoidReturnVisitor(outerTag).Visit(Argument.Body));
        }
    }
}

