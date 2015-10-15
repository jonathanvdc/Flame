using Flame.Compiler;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class PrimitiveNodeBlock : INodeBlock
    {
        public PrimitiveNodeBlock(ICodeGenerator CodeGenerator, LNode Node, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Node = Node;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public LNode Node { get; private set; }
        public IType Type { get; private set; }
    }
}
