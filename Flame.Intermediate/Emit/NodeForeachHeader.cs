using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class NodeCollectionBlock : ICollectionBlock
    {
        public NodeCollectionBlock(IRCodeGenerator CodeGenerator, IVariableMember Member, LNode Collection)
        {
            this.CodeGenerator = CodeGenerator;
            this.Member = Member;
            this.Collection = Collection;
        }

        public IRCodeGenerator CodeGenerator { get; private set; }

        public IVariableMember Member { get; private set; }
        public LNode Collection { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }
    }


    public class NodeForeachHeader : IForeachBlockHeader
    {
        public NodeForeachHeader(LNode TagNode, IReadOnlyList<Tuple<string, NodeEmitVariable, NodeCollectionBlock>> Collections)
        {
            this.TagNode = TagNode;
            this.Collections = Collections;
        }

        public IReadOnlyList<Tuple<string, NodeEmitVariable, NodeCollectionBlock>> Collections { get; private set; }

        public IReadOnlyList<IEmitVariable> Elements
        {
            get { return Collections.Select(item => item.Item2).ToArray(); }
        }

        public LNode TagNode { get; private set; }
        public LNode CollectionsNode
        {
            get
            {
                return NodeFactory.Block(Collections.Select(item =>
                {
                    var sig = item.Item3.CodeGenerator.EmitSignature(item.Item3.Member);
                    return NodeFactory.Call(ExpressionParsers.CollectionNodeName, new LNode[] 
                    { 
                        NodeFactory.IdOrLiteral(item.Item1), 
                        sig.Node, 
                        item.Item3.CodeGenerator.Assembly.TypeTable.GetReference(item.Item3.Member.VariableType),
                        item.Item3.Collection 
                    });
                }));
            }
        }
    }
}
