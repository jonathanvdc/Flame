using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    /// <summary>
    /// The frame manager handles setting up the frame and breaking it down, along with every frame transition in between.
    /// </summary>
    public interface IFrameManager
    {
        /// <summary>
        /// Binds the frame manager to the context.
        /// </summary>
        /// <param name="Context"></param>
        void Bind(IAssemblerEmitContext Context);

        /// <summary>
        /// Emits the instructions for a function return. This also breaks down the stack frame.
        /// </summary>
        /// <param name="ReturnValues"></param>
        /// <param name="Context"></param>
        void EmitReturnInstructions(IEnumerable<IStorageLocation> ReturnValues, IAssemblerEmitContext Context);

        /// <summary>
        /// Emits the necessary instructions for a function call.
        /// </summary>
        /// <param name="Method"></param>
        /// <param name="Arguments"></param>
        /// <param name="Context"></param>
        IEnumerable<IStorageLocation> EmitInvokeInstructions(IMethod Method, IEnumerable<IStorageLocation> Arguments, IAssemblerEmitContext Context);

        /// <summary>
        /// Emits the necessary instructions for a function call.
        /// </summary>
        /// <param name="Method"></param>
        /// <param name="Arguments"></param>
        /// <param name="Context"></param>
        IEnumerable<IStorageLocation> EmitInvokeInstructions(IRegister Method, ICallConvention CallConvention, IEnumerable<IStorageLocation> Arguments, IAssemblerEmitContext Context);

        /// <summary>
        /// Flags the given register as a preserved register.
        /// </summary>
        /// <param name="Register"></param>
        void PreserveRegister(RegisterData Register);

        /// <summary>
        /// Flags the given register as no longer preserved.
        /// </summary>
        /// <param name="Register"></param>
        void ReleaseRegister(RegisterData Register);

        /// <summary>
        /// Stack-allocates a variable.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        IUnmanagedStorageLocation StackAllocate(IType Type);

        /// <summary>
        /// Gets the storage location of the argument with the given index.
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        IStorageLocation GetArgument(IAssemblerEmitContext Context, int Index);

        /// <summary>
        /// Emits the instructions necessary for frame initialization.
        /// </summary>
        /// <param name="Context"></param>
        /// <remarks>This should be called after the method's body has been emitted.</remarks>
        void EmitInitializeFrame(IAssemblerEmitContext Context);
    }
}
