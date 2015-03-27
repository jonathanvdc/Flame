using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// Defines a block that consists of more than one block.
    /// </summary>
    public interface IMultiBlock : ICppBlock
    {
        /// <summary>
        /// Gets the multi-block's sub-blocks.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ICppBlock> GetBlocks();
    }
}
