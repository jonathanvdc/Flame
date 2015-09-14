using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class CallNode : Node
    {
        public CallNode(string Target, IReadOnlyList<Node> Arguments)
            : this(new IdNode(Target), Arguments)
        {
        }
        public CallNode(Node Target, IReadOnlyList<Node> Arguments)
        {
            this.target = Target;
            this.args = Arguments;
        }

        private Node target;
        private IReadOnlyList<Node> args;

        public override Node Target { get { return target; } }
        public override IReadOnlyList<Node> Arguments { get { return args; } }

        public override string Name
        {
            get { return Target.IsId ? Target.Name : null; }
        }

        public override object Value
        {
            get { return null; }
        }

        public override bool IsCall
        {
            get { return true; }
        }

        public override bool IsId
        {
            get { return false; }
        }

        public override bool IsLiteral
        {
            get { return false; }
        }
    }
}
