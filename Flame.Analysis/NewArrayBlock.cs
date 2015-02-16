using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class NewArrayBlock : IAnalyzedExpression
    {
        public NewArrayBlock(ICodeGenerator CodeGenerator, IType ElementType, IEnumerable<IAnalyzedExpression> Dimensions)
        {
            this.CodeGenerator = CodeGenerator;
            this.ElementType = ElementType;
            this.Dimensions = Dimensions;
        }

        public IType ElementType { get; private set; }
        public IEnumerable<IAnalyzedExpression> Dimensions { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IExpression ToExpression(VariableMetrics State)
        {
            return new NewArrayExpression(ElementType, Dimensions.ToExpressions(State));
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return new SimpleAnalyzedBlockStatement(CodeGenerator, Dimensions.GetInitializationStatement(CodeGenerator)); ; }
        }

        public VariableMetrics Metrics
        {
            get { return Dimensions.ToMetrics().Pipe(); }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return new LiteralExpressionProperties(ElementType.MakeArrayType(Dimensions.Count())); }
        }

        public IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
