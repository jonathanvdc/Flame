using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class ConversionBlock : IAnalyzedExpression
    {
        public ConversionBlock(IAnalyzedExpression Value, IType Type)
        {
            this.Value = Value;
            this.Type = Type;
        }

        public IType Type { get; private set; }
        public IAnalyzedExpression Value { get; private set; }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Value.InitializationStatement; }
        }

        public IExpression ToExpression(VariableMetrics Metrics)
        {
            return new ConversionExpression(Value.ToExpression(Metrics), Type);
        }

        public VariableMetrics Metrics
        {
            get { return Value.Metrics; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return Value.ExpressionProperties; }
        }

        public IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is ConversionBlock)
            {
                var otherCast = (ConversionBlock)other;
                return this.Type.Equals(otherCast.Type) && this.Value.Equals(otherCast.Value);
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
            return Type.GetHashCode() ^ Value.GetHashCode();
        }
    }
}
