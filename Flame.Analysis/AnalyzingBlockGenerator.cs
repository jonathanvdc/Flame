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
    public class AnalyzingBlockGenerator : IBlockGenerator, IAnalyzedStatement, IAnalyzedExpression
    {
        public AnalyzingBlockGenerator(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.blocks = new List<IAnalyzedBlock>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        private List<IAnalyzedBlock> blocks;

        private AnalyzedBlockStatement CreateEmitStatement()
        {
            return new AnalyzedBlockStatement(CodeGenerator, blocks);
        }

        public void EmitBlock(ICodeBlock Block)
        {
            blocks.Add((IAnalyzedBlock)Block);
            props = null;
        }

        public void EmitBreak()
        {
            EmitBlock(new LocalStatement(CodeGenerator, new BreakStatement()));
        }

        public void EmitContinue()
        {
            EmitBlock(new LocalStatement(CodeGenerator, new ContinueStatement()));
        }

        public void EmitPop(ICodeBlock Block)
        {
            EmitBlock(new PopBlock((IAnalyzedExpression)Block));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            EmitBlock(new ReturnBlock(CodeGenerator, Block as IAnalyzedExpression));
        }

        public virtual IStatement ToStatement(VariableMetrics State)
        {
            return CreateEmitStatement().ToStatement(State);
        }

        public virtual VariableMetrics Metrics
        {
            get { return blocks.PipeMetrics(); }
        }

        public virtual IExpression ToExpression(VariableMetrics State)
        {
            return CreateEmitStatement().ToExpression(State);
        }

        protected virtual IBlockProperties CreateProperties(IEnumerable<IAnalyzedBlock> Blocks)
        {
            return new AnalyzedBlockSequenceProperties(blocks);
        }

        private IBlockProperties props;
        public IBlockProperties Properties
        {
            get 
            {
                if (props == null)
                {
                    props = CreateProperties(blocks);
                }
                return props;
            }
        }

        public IStatementProperties StatementProperties
        {
            get { return (IStatementProperties)Properties; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return (IExpressionProperties)Properties; }
        }

        public virtual bool Equals(IAnalyzedBlock other)
        {
            if (other.GetType().Equals(this.GetType()))
            {
                return this.blocks.SequenceEqual(((AnalyzingBlockGenerator)other).blocks);
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is IAnalyzedBlock)
            {
                return this.Equals((IAnalyzedBlock)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return blocks.Aggregate(0, (a, b) => a ^ b.GetHashCode());
        }

        public IAnalyzedStatement InitializationStatement
        {
            get 
            {
                return new SimpleAnalyzedBlockStatement(CodeGenerator, blocks.Select((item) => item.InitializationStatement));
            }
        }
    }

    public class AnalyzedBlockSequenceProperties : IStatementProperties, IExpressionProperties
    {
        public AnalyzedBlockSequenceProperties(IEnumerable<IAnalyzedBlock> Blocks)
        {
            this.Blocks = Blocks;
        }

        public IEnumerable<IAnalyzedBlock> Blocks { get; private set; }

        private bool? isVolatile;
        public bool IsVolatile
        {
            get
            {
                if (isVolatile == null)
                {
                    isVolatile = Blocks.Any((item) => item.Properties.IsVolatile);
                }
                return isVolatile.Value;
            }
        }

        public bool Inline
        {
            get { return false; }
        }

        public IType Type
        {
            get { return Blocks.OfType<IAnalyzedExpression>().GetTypes().LastOrDefault((item) => !item.IsVoid()) ?? PrimitiveTypes.Void; }
        }
    }
}
