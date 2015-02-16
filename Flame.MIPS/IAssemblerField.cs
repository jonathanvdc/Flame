using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public interface IAssemblerField : IField
    {
        /// <summary>
        /// Gets the field's offset from the object's base address.
        /// </summary>
        int Offset { get; }
        /// <summary>
        /// Gets the field's size.
        /// </summary>
        int Size { get; }
    }
}
