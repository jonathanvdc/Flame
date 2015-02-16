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
    /// <summary>
    /// Represents a simple analyzed block statement.
    /// This type emits its blocks directly, and does not perform any optimizations.
    /// It does not emit inialization statements.
    /// </summary>
    /// <remarks>
    /// It is recommended this type be used by another, higher-level type, as a way to emit sequences of blocks.
    /// </remarks>
    public class SimpleAnalyzedBlockStatement : AnalyzedExpressionBase<SimpleAnalyzedBlockStatement>, IAnalyzedStatement
    {
        public SimpleAnalyzedBlockStatement(ICodeGenerator CodeGenerator, params IAnalyzedBlock[] Blocks)
            : this(CodeGenerator, (IEnumerable<IAnalyzedBlock>)Blocks)
        {
        }
        public SimpleAnalyzedBlockStatement(ICodeGenerator CodeGenerator, IEnumerable<IAnalyzedBlock> Blocks)
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
                return new EmptyAnalyzedStatement(CodeGenerator);
            }
        }

        public IStatement ToStatement(VariableMetrics State)
        {
            var results = new LinkedList<IStatement>();
            var currentState = State;
            foreach (var item in Blocks.Cast<IAnalyzedStatement>())
            {
                results.AddLast(item.ToStatement(State));
                currentState = currentState.Pipe(item.Metrics);
            }
            return new BlockStatement(results);
        }

        public override IExpression ToExpression(VariableMetrics State)
        {
            var initStatements = Blocks.TakeWhile((item) => item is IAnalyzedStatement).Cast<IAnalyzedStatement>();
            var init = new BlockStatement(initStatements.ToStatements(State));
            var stateMetrics = State.PipeMetrics(initStatements);

            var exprBlock = Blocks.OfType<IAnalyzedExpression>().Single();
            var expr = exprBlock.ToExpression(stateMetrics);
            stateMetrics = State.PipeMetrics(exprBlock);

            var finalStatements = Blocks.SkipWhile((item) => item is IAnalyzedStatement).OfType<IAnalyzedStatement>();
            var final = new BlockStatement(finalStatements.ToStatements(stateMetrics));

            return new InitializedExpression(init, expr, final);
        }

        public override VariableMetrics Metrics
        {
            get { return Blocks.PipeMetrics(); }
        }

        public override bool Equals(SimpleAnalyzedBlockStatement Other)
        {
            return Blocks.SequenceEqual(Other.Blocks);
        }

        public override int GetHashCode()
        {
            return Blocks.Aggregate(0, (a, b) => a ^ b.GetHashCode());
        }
    }
}
