using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public interface IStatementBlock : ICodeBlock
    {
        IStatement GetStatement();
        IEnumerable<IType> ResultTypes { get; }
    }
}
