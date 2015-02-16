using Flame.Compiler;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public interface IAssemblerMethod : IMethod
    {
        /// <summary>
        /// Creates a new block that jumps to the method and returns afterward.
        /// </summary>
        IAssemblerBlock CreateCallBlock(ICodeGenerator CodeGenerator);

        /// <summary>
        /// Gets the method's call convention.
        /// </summary>
        ICallConvention CallConvention { get; }
    }

    public interface ISyscallMethod : IAssemblerMethod
    {
        int ServiceIndex { get; }
    }
}
