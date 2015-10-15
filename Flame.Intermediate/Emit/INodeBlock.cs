using Flame.Compiler;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public interface INodeBlock : ICodeBlock
    {
        IType Type { get; }
        LNode Node { get; }
    }
}
