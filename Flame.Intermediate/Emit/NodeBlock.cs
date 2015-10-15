using Flame.Compiler;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class NodeBlock : ICodeBlock
    {
        public NodeBlock(ICodeGenerator CodeGenerator, LNode Node)
        {
            this.CodeGenerator = CodeGenerator;
            this.Node = Node;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public LNode Node { get; private set; }
        
        public static LNode ToNode(ICodeBlock Block)
        {
            if (Block == null)
            {
                return NodeFactory.Id(IRParser.NullNodeName);
            }
            else
            {
                return ((NodeBlock)Block).Node;
            }
        }
    }
}
