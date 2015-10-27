using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class VersionNodeStructure : INodeStructure<Version>
    {
        public VersionNodeStructure(LNode Node)
        {
            this.Node = Node;
        }
        public VersionNodeStructure(Version Value)
        {
            this.Node = NodeFactory.Call(VersionNodeName,
                new LNode[] 
                {
                    NodeFactory.Literal(Value.Major), NodeFactory.Literal(Value.Minor),
                    NodeFactory.Literal(Value.Build), NodeFactory.Literal(Value.Revision)
                });
        }

        public const string VersionNodeName = "#version";

        public LNode Node { get; private set; }

        public Version Value
        {
            get
            {
                return new Version(
                    Convert.ToInt32(Node.Args[0].Value),
                    Convert.ToInt32(Node.Args[1].Value),
                    Convert.ToInt32(Node.Args[2].Value),
                    Convert.ToInt32(Node.Args[3].Value));
            }
        }
    }
}
