using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class NodeSignature
    {
        public NodeSignature(string Name, IEnumerable<INodeStructure<IAttribute>> AttributeNodes)
        {
            this.Name = Name;
            this.AttributeNodes = AttributeNodes;
            this.cachedAttrs = new Lazy<IAttribute[]>(() => AttributeNodes.Select(item => item.Value).ToArray());
        }

        public string Name { get; private set; }
        public IEnumerable<INodeStructure<IAttribute>> AttributeNodes { get; private set; }

        public const string MemberNodeName = "#member";

        public LNode Node
        {
            get
            {
                var args = new List<LNode>();
                args.Add(NodeFactory.Id(Name));
                args.AddRange(AttributeNodes.Select(item => item.Node));
                return NodeFactory.Call(MemberNodeName, args);
            }
        }

        private Lazy<IAttribute[]> cachedAttrs;
        public IEnumerable<IAttribute> Attributes
        {
            get { return cachedAttrs.Value; }
        }
    }
}
