using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public interface INodeStructure<out T>
    {
        Node Node { get; }
        T Value { get; }
    }
}
