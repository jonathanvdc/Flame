using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class MethodDelegateBlock : IAnalyzedExpression
    {
        public MethodDelegateBlock(ICodeGenerator CodeGenerator, IAnalyzedExpression Caller, IMethod Method)
        {
            this.CodeGenerator = CodeGenerator;
            this.Caller = Caller;
            this.Method = Method;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAnalyzedExpression Caller { get; private set; }
        public IMethod Method { get; private set; }

        public IExpression ToExpression(VariableMetrics State)
        {
            return new GetMethodExpression(Method, Caller.ToExpressionOrNull(State)); 
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Caller.InitializationStatement; }
        }

        public VariableMetrics Metrics
        {
            get { return Caller.GetMetricsOrDefault(); }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return Caller == null ? new LiteralExpressionProperties(MethodType.Create(Method)) : Caller.ExpressionProperties; }
        }

        public IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is MethodDelegateBlock)
            {
                var otherDelegate = (MethodDelegateBlock)other;
                return (object.ReferenceEquals(this.Caller, otherDelegate.Caller) || this.Caller.Equals(otherDelegate.Caller)) && this.Method.Equals(otherDelegate.Method);
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
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return Method.GetHashCode() ^ Caller.GetHashCode();
        }
    }
}
