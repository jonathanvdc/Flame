using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public interface IStorageLocation
    {
        /// <summary>
        /// Gets the storage location's type.
        /// </summary>
        IType Type { get; }
        /// <summary>
        /// Loads the value in the storage location into the specified register.
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        IAssemblerBlock EmitLoad(IRegister Target);
        /// <summary>
        /// Writes the value in the register to the storage location.
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        IAssemblerBlock EmitStore(IRegister Target);
        /// <summary>
        /// Releases the storage location.
        /// </summary>
        /// <returns></returns>
        IAssemblerBlock EmitRelease();
    }

    public interface IUnmanagedStorageLocation : IStorageLocation
    {
        /// <summary>
        /// Creates a block that loads the address of the storage location into the specified register.
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        IAssemblerBlock EmitLoadAddress(IRegister Target);
    }
}
