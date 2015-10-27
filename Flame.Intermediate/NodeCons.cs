using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class NodeCons<T> : INodeStructure<IEnumerable<T>>
    {
        public NodeCons(INodeStructure<T> Head, INodeStructure<IEnumerable<T>> Tail)
        {
            this.Head = Head;
            this.Tail = Tail;
        }

        public INodeStructure<T> Head { get; private set; }
        public INodeStructure<IEnumerable<T>> Tail { get; private set; }

        public LNode Node
        {
            get 
            {
                var tailNode = Tail.Node;
                if (NodeFactory.IsBlock(tailNode))
                {
                    return NodeFactory.Block(tailNode.Args.Push(Head.Node));
                }
                else
                {
                    return NodeFactory.Block(new LNode[] { Head.Node, tailNode });
                }
            }
        }

        public IEnumerable<T> Value
        {
            get { return new T[] { Head.Value }.Concat(Tail.Value); }
        }
    }
}
