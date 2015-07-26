using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public static class BlockExtensions
    {
        /// <summary>
        /// Tries to determine conservatively if this block is empty.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static bool IsEmpty(this ICecilBlock Block)
        {
            if (Block is EmptyBlock)
            {
                return true;
            }
            else if (Block is SequenceBlock)
            {
                var seq = (SequenceBlock)Block;
                return seq.First.IsEmpty() && seq.Second.IsEmpty();
            }
            else
            {
                return false;
            }
        }
    }
}
