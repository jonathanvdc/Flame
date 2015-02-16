using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedBlockStatement : AnalyzedExpressionBase<AnalyzedBlockStatement>, IAnalyzedStatement
    {
        public AnalyzedBlockStatement(ICodeGenerator CodeGenerator, params IAnalyzedBlock[] Blocks)
            : this(CodeGenerator, (IEnumerable<IAnalyzedBlock>)Blocks)
        {
        }
        public AnalyzedBlockStatement(ICodeGenerator CodeGenerator, IEnumerable<IAnalyzedBlock> Blocks)
            : base(CodeGenerator)
        {
            this.Blocks = Blocks;
        }

        public IEnumerable<IAnalyzedBlock> Blocks { get; private set; }

        private AnalyzedBlockSequenceProperties props;
        public AnalyzedBlockSequenceProperties BlockProperties
        {
            get
            {
                if (props == null)
                {
                    props = new AnalyzedBlockSequenceProperties(Blocks);
                }
                return props;
            }
        }

        public IStatementProperties StatementProperties
        {
            get { return BlockProperties; }
        }

        public override IExpressionProperties ExpressionProperties
        {
            get { return BlockProperties; }
        }

        public override IAnalyzedStatement InitializationStatement
        {
            get
            {
                return new AnalyzedBlockStatement(CodeGenerator, Blocks.Select((item) => item.InitializationStatement));
            }
        }

        public IStatement ToStatement(VariableMetrics State)
        {
            return new SimpleAnalyzedBlockStatement(CodeGenerator, Blocks).ToStatement(State);
        }

        public override IExpression ToExpression(VariableMetrics State)
        {
            return new SimpleAnalyzedBlockStatement(CodeGenerator, Blocks).ToExpression(State);
        }

        public override VariableMetrics Metrics
        {
            get { return Blocks.PipeMetrics(); }
        }

        public override bool Equals(AnalyzedBlockStatement Other)
        {
            return Blocks.SequenceEqual(Other.Blocks);
        }

        public override int GetHashCode()
        {
            return Blocks.Aggregate(0, (a, b) => a ^ b.GetHashCode());
        }
    }
}
