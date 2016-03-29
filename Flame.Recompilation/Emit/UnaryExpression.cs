using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class UnaryExpression : IExpression
    {
        public UnaryExpression(IExpression Value, Operator Op)
        {
            this.Value = Value;
            this.Op = Op;
        }

        public IExpression Value { get; private set; }
        public Operator Op { get; private set; }

        public ICodeBlock Emit(ICodeGenerator Generator)
        {
            return Generator.EmitUnary(Value.Emit(Generator), Op);
        }

        public IBoundObject Evaluate()
        {
            return null;
        }

        public bool IsConstantNode
        {
            get { return true; }
        }

        public IExpression Optimize()
        {
            return this;
        }

        public IType Type
        {
            get
            {
                return Value.Type;
            }
        }

        public IExpression Accept(INodeVisitor Visitor)
        {
            var visitedUnary = Visitor.Visit(Value);

            if (visitedUnary == Value)
            {
                return this;
            }
            else
            {
                return new UnaryExpression(visitedUnary, Op);
            }
        }
    }
}
