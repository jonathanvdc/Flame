using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedIfElseBlockGenerator : AnalyzedStatementBase<AnalyzedIfElseBlockGenerator>, IIfElseBlockGenerator
    {
        public AnalyzedIfElseBlockGenerator(ICodeGenerator CodeGenerator, IAnalyzedExpression Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
            this.IfBlock = CodeGenerator.CreateBlock();
            this.ElseBlock = CodeGenerator.CreateBlock();
        }

        /// <summary>
        /// Gets an immutable version of this if-else block generator.
        /// </summary>
        /// <returns></returns>
        public IAnalyzedStatement ToImmutable()
        {
            return new AnalyzedIfElseStatement(CodeGenerator, Condition, (IAnalyzedStatement)IfBlock, (IAnalyzedStatement)ElseBlock);
        }

        public IAnalyzedExpression Condition { get; private set; }
        public IBlockGenerator IfBlock { get; private set; }
        public IBlockGenerator ElseBlock { get; private set; }

        public override VariableMetrics Metrics
        {
            get { return ToImmutable().Metrics; }
        }

        public override IStatementProperties StatementProperties
        {
            get { return ToImmutable().StatementProperties; }
        }

        public override IStatement ToStatement(VariableMetrics State)
        {
            return ToImmutable().ToStatement(State);
        }

        public override bool Equals(AnalyzedIfElseBlockGenerator Other)
        {
            return ToImmutable().Equals(Other.ToImmutable());
        }

        public override bool Equals(IAnalyzedBlock obj)
        {
            if (obj is AnalyzedIfElseBlockGenerator)
            {
                return Equals((AnalyzedIfElseBlockGenerator)obj);
            }
            else
            {
                return ToImmutable().Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return ToImmutable().GetHashCode();
        }
    }
}
