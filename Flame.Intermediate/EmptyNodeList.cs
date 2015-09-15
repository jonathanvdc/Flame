using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class EmptyNodeList<T> : INodeStructure<IReadOnlyList<T>>
    {
        private EmptyNodeList()
        { }
        
        static EmptyNodeList()
        {
            Instance = new EmptyNodeList<T>();
        }

        public static EmptyNodeList<T> Instance { get; private set; }

        public LNode Node { get { return NodeFactory.Block(new LNode[0]); } }

        public IReadOnlyList<T> Value
        {
            get { return new T[0]; }
        }
    }
}
