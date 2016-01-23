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

        public static NodeBlock Id(IRCodeGenerator CodeGenerator, string NodeName)
        {
            return new NodeBlock(CodeGenerator, NodeFactory.Id(NodeName));
        }

        public static NodeBlock Call(IRCodeGenerator CodeGenerator, string NodeName, params ICodeBlock[] Blocks)
        {
            return new NodeBlock(CodeGenerator, NodeFactory.Call(NodeName, Blocks.Select(ToNode)));
        }
        public static NodeBlock Call(IRCodeGenerator CodeGenerator, string NodeName, params LNode[] Nodes)
        {
            return new NodeBlock(CodeGenerator, NodeFactory.Call(NodeName, Nodes));
        }

		public static NodeBlock Block(IRCodeGenerator CodeGenerator, IEnumerable<ICodeBlock> Nodes)
		{
			return Block(CodeGenerator, Nodes.Select(ToNode));
		}
		public static NodeBlock Block(IRCodeGenerator CodeGenerator, IEnumerable<LNode> Nodes)
		{
			return new NodeBlock(CodeGenerator, NodeFactory.Block(Nodes));
		}
		public static NodeBlock Block(IRCodeGenerator CodeGenerator, params ICodeBlock[] Nodes)
		{
			return Block(CodeGenerator, (IEnumerable<ICodeBlock>)Nodes);
		}
		public static NodeBlock Block(IRCodeGenerator CodeGenerator, params LNode[] Nodes)
		{
			return Block(CodeGenerator, (IEnumerable<LNode>)Nodes);
		}
        
		/// <summary>
		/// Converts the given (optionally null) block
		/// to a node.
		/// </summary>
		/// <returns>The node.</returns>
		/// <param name="Block">Block.</param>
        public static LNode ToNode(ICodeBlock Block)
        {
            if (Block == null)
            {
                return NodeFactory.Literal(null);
            }
            else
            {
                return ((NodeBlock)Block).Node;
            }
        }
    }
}
