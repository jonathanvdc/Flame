using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class ThisAddressOfExpression : IExpression
    {
        public ThisAddressOfExpression(IType Type)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public ICodeBlock Emit(ICodeGenerator Generator)
        {
            return ((IUnmanagedCodeGenerator)Generator).GetUnmanagedThis().EmitAddressOf();
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

        public IExpression Accept(INodeVisitor Visitor)
        {
            return this;
        }
    }
}
