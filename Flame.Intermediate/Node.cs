using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public abstract class Node
    {
        public abstract Node Target { get; }
        public abstract IReadOnlyList<Node> Arguments { get; }
        public abstract string Name { get; }
        public abstract object Value { get; }

        public abstract bool IsCall { get; }
        public abstract bool IsId { get; }
        public abstract bool IsLiteral { get; }
    }
}
