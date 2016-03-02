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
            this.HasRewritten = false;
        }

        /// <summary>
        /// Gets the tag of the tagged block to break to.
        /// </summary>
        public UniqueTag BreakTag { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any return-void statements have been rewritten.
        /// </summary>
        /// <value><c>true</c> if any return-void statements have been rewritten; otherwise, <c>false</c>.</value>
        public bool HasRewritten { get; private set; }

        public override bool Matches(IStatement Value)
        {
            return Value is ReturnStatement;
        }

        protected override IStatement Transform(IStatement Statement)
        {
            var retStmt = (ReturnStatement)Statement;
            if (retStmt.Value == null || retStmt.Value.Type.Equals(PrimitiveTypes.Void))
            {
                HasRewritten = true;
                return new BreakStatement(BreakTag);
            }
            else
            {
                return Statement.Accept(this);
            }
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
            var outerTag = new UniqueTag("outer");
            var visitor = new RewriteVoidReturnVisitor(outerTag);
            var body = visitor.Visit(Argument.Body);
            if (visitor.HasRewritten)
                return new TaggedStatement(outerTag, body);
            else
                return body;
        }
    }
}

