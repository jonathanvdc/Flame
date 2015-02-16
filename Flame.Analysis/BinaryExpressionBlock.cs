using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class BinaryExpressionBlock : IAnalyzedExpression, IAssertionBlock
    {
        public BinaryExpressionBlock(IAnalyzedExpression Left, Operator Op, IAnalyzedExpression Right)
        {
            this.Left = Left;
            this.Op = Op;
            this.Right = Right;
        }

        public IAnalyzedExpression Left { get; private set; }
        public Operator Op { get; private set; }
        public IAnalyzedExpression Right { get; private set; }

        public VariableMetrics Metrics
        {
            get { return Left.Metrics.Pipe(Right.Metrics); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Left.CodeGenerator; }
        }

        public IBlockProperties BlockProperties
        {
            get { return ExpressionProperties; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return new BinaryBlockProperties(Left.ExpressionProperties, Op, Right.ExpressionProperties); }
        }

        public IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }

        public IExpression ToExpression(VariableMetrics State)
        {
            return new DirectBinaryExpression(Left.ToExpression(State), Op, Right.ToExpression(State));
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return new SimpleAnalyzedBlockStatement(CodeGenerator, new[] { Left.InitializationStatement, Right.InitializationStatement }); }
        }

        public IAssertion GetAssertion(VariableMetrics State)
        {
            if (Op.Equals(Operator.LogicalAnd) || (Op.Equals(Operator.And) && ExpressionProperties.Type.Equals(PrimitiveTypes.Boolean)))
            {
                return Left.GetAssertion(State).And(Right.GetAssertion(State));
            }
            else
            {
                return new EmptyAssertion();
            }
        }

        public IAnalyzedBlock ApplyAssertion(IAssertion Assertion, VariableMetrics State)
        {
            return new BinaryExpressionBlock(Left.ApplyAssertion(Assertion, State), Op, Right.ApplyAssertion(Assertion, State));
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is BinaryExpressionBlock)
            {
                var otherBinary = (BinaryExpressionBlock)other;
                return otherBinary.Op.Equals(this.Op) && this.Left.Equals(otherBinary.Left) && this.Right.Equals(otherBinary.Right);
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
            return Op.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode();
        }
    }
}
