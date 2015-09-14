using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class LiteralNode : Node
    {
        public LiteralNode(object Value)
        {
            this.val = Value;
        }

        private object val;

        public override Node Target
        {
            get { return null; }
        }

        public override IReadOnlyList<Node> Arguments
        {
            get { return new Node[0]; }
        }

        public override string Name
        {
            get { return null; }
        }

        public override object Value
        {
            get { return val; }
        }

        public override bool IsCall
        {
            get { return false; }
        }

        public override bool IsId
        {
            get { return false; }
        }

        public override bool IsLiteral
        {
            get { return true; }
        }
    }
}
