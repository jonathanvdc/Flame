using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRSignature
    {
        public IRSignature(UnqualifiedName Name, IEnumerable<INodeStructure<IAttribute>> AttributeNodes)
        {
            this.Name = Name;
            this.AttributeNodes = AttributeNodes;
            this.cachedAttrs = new Lazy<AttributeMap>(() => new AttributeMap(AttributeNodes.Select(item => item.Value)));
        }
        public IRSignature(UnqualifiedName Name)
        {
            this.Name = Name;
            this.AttributeNodes = Enumerable.Empty<INodeStructure<IAttribute>>();
            this.cachedAttrs = new Lazy<AttributeMap>(() => AttributeMap.Empty);
        }

        public UnqualifiedName Name { get; private set; }
        public IEnumerable<INodeStructure<IAttribute>> AttributeNodes { get; private set; }

        public const string MemberNodeName = "#member";

        public LNode Node
        {
            get
            {
                var args = new List<LNode>();
                args.Add(NodeFactory.IdOrLiteral(Name));
                args.AddRange(AttributeNodes.Select(item => item.Node));
                return NodeFactory.Call(MemberNodeName, args);
            }
        }

        private Lazy<AttributeMap> cachedAttrs;
        public AttributeMap Attributes
        {
            get { return cachedAttrs.Value; }
        }

        public static readonly IRSignature Empty = new IRSignature(new SimpleName(""), Enumerable.Empty<INodeStructure<IAttribute>>());
    }
}
