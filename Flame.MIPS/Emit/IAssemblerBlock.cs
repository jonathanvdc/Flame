using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public interface IAssemblerBlock : ICodeBlock
    {
        IType Type { get; }

        IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context);
    }
}
