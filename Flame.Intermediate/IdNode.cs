using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IdNode : Node
    {
        public IdNode(string Name)
        {
            this.name = Name;
        }

        private string name;

        public override string Name { get { return name; } }

        public override Node Target
        {
            get { return null; }
        }

        public override IReadOnlyList<Node> Arguments
        {
            get { return new Node[0]; }
        }

        public override object Value
        {
            get { return null; }
        }

        public override bool IsCall
        {
            get { return false; }
        }

        public override bool IsId
        {
            get { return true; }
        }

        public override bool IsLiteral
        {
            get { return false; }
        }
    }
}
