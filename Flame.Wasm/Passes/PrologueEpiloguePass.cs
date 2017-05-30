using System;
using Flame.Compiler.Visitors;
using Flame.Compiler;
using Flame.Compiler.Native;
using Flame.Compiler.Statements;
using System.Collections.Generic;

namespace Flame.Wasm.Passes
{
    /// <summary>
    /// A visitor that converts return statements to 
    /// return/epilogue statements, for a specific ABI.
    /// </summary>
    public class ReturnEpilogueVisitor : NodeVisitorBase
    {
        public ReturnEpilogueVisitor(IStackAbi Abi, IMethod Method)
        {
            this.Abi = Abi;
            this.Method = Method;
        }

        /// <summary>
        /// Gets the ABI that is used to write return statements/epilogues.
        /// </summary>
        public IStackAbi Abi { get; private set; }

        /// <summary>
        /// Gets the method whose body is currently being processed.
        /// </summary>
        public IMethod Method { get; private set; }

        public override bool Matches(IExpression Value)
        {
            return false;
        }

        public override bool Matches(IStatement Value)
        {
            return Value is ReturnStatement;
        }

        protected override IExpression Transform(IExpression Expression)
        {
            return Expression;
        }

        protected override IStatement Transform(IStatement Statement)
        {
            var retStmt = (ReturnStatement)Statement;
            return Abi.CreateReturnEpilogue(Method, retStmt.Value == null ? null : Visit(retStmt.Value));
        }
    }

    /// <summary>
    /// A pass that prepends an ABI-specific prologues to method bodies.
    /// </summary>
    public class ProloguePass : IPass<BodyPassArgument, IStatement>
    {
        public ProloguePass(IStackAbi Abi)
        {
            this.Abi = Abi;
        }

        public const string ProloguePassName = "prologue";

        /// <summary>
        /// Gets the ABI that is used to append prologues.
        /// </summary>
        public IStackAbi Abi { get; private set; }

        public IStatement Apply(BodyPassArgument Arg)
        {
            var results = new List<IStatement>();
            results.Add(Abi.CreatePrologue(Arg.DeclaringMethod));
            results.Add(Arg.Body);
            return new BlockStatement(results).Simplify();
        }
    }

    /// <summary>
    /// A pass that converts return statements to 
    /// return/epilogue statements, for a specific ABI.
    /// </summary>
    public class EpiloguePass : IPass<BodyPassArgument, IStatement>
    {
        public EpiloguePass(IStackAbi Abi)
        {
            this.Abi = Abi;
        }

        public const string EpiloguePassName = "epilogue";

        /// <summary>
        /// Gets the ABI that is used to write return statements/epilogues.
        /// </summary>
        public IStackAbi Abi { get; private set; }

        public IStatement Apply(BodyPassArgument Arg)
        {
            return new ReturnEpilogueVisitor(Abi, Arg.DeclaringMethod).Visit(Arg.Body);
        }
    }
}

