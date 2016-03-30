using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Variables;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A recompilation pass based on a visitor.
    /// </summary>
    public class VisitorRecompilationPass : IPass<RecompilationPassArguments, INode>
    {
        private VisitorRecompilationPass()
        {

        }

        static VisitorRecompilationPass()
        {
            Instance = new VisitorRecompilationPass();
        }

        public static VisitorRecompilationPass Instance { get; private set; }

        public INode Apply(RecompilationPassArguments Value)
        {
            var visitor = new RecompilingVisitor(Value.Recompiler);

            return Value.Body is IExpression ? (INode)visitor.Visit((IExpression)Value.Body) : (INode)visitor.Visit((IStatement)Value.Body);
        }
    }

    public class RecompilingVisitor : NodeVisitorBase
    {
        public RecompilingVisitor(AssemblyRecompiler Recompiler)
        {
            this.Converter = new MemberConverter(new DelegateConverter<IType, IType>(Recompiler.GetType),
                                                 new DelegateConverter<IMethod, IMethod>(Recompiler.GetMethod),
                                                 new DelegateConverter<IField, IField>(Recompiler.GetField));
        }
        public RecompilingVisitor(MemberConverter Converter)
        {
            this.Converter = Converter;
        }

        public MemberConverter Converter { get; private set; }

        public override bool Matches(IExpression Value)
        {
            return Value is IMemberNode;
        }

        public override bool Matches(IStatement Value)
        {
            return Value is IMemberNode;
        }

        protected override IExpression Transform(IExpression Expression)
        {
            var memberNode = (IExpression)((IMemberNode)Expression).ConvertMembers(Converter);
            return memberNode.Accept(this);
        }

        protected override IStatement Transform(IStatement Statement)
        {
            var memberNode = (IStatement)((IMemberNode)Statement).ConvertMembers(Converter);
            return memberNode.Accept(this);
        }
    }
}
