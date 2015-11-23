using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public interface IAssemblerEmitContext
    {
        ICodeGenerator CodeGenerator { get; }

        void EmitComment(string Comment);
        void Emit(Instruction Instruction);
        void EmitReturn(IEnumerable<IStorageLocation> ReturnValues);
        IEnumerable<IStorageLocation> EmitInvoke(IMethod Method, IEnumerable<IStorageLocation> Arguments);
        IEnumerable<IStorageLocation> EmitInvoke(IRegister Target, ICallConvention CallConvention, IEnumerable<IStorageLocation> Arguments);

        Stack<IFlowControlStructure> FlowControl { get; }

        bool ApplyOptimization(IPeepholeOptimization Optimization);

        IInstructionArgument ToArgument(IRegister Register);
        IInstructionArgument ToArgument(long Offset, IRegister Register);
        IInstructionArgument ToArgument(long Immediate);
        IInstructionArgument ToArgument(IAssemblerLabel Label);

        /// <summary>
        /// Allocates a register of the specified type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        IRegister AllocateRegister(IType Type);
        /// <summary>
        /// Spills the register to a temporary storage location.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        IStorageLocation Spill(IRegister Source);
        /// <summary>
        /// Allocates a local storage location.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        IStorageLocation AllocateLocal(IType Type);
        /// <summary>
        /// Allocates an unmanaged local storage location.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        IUnmanagedStorageLocation AllocateUnmanagedLocal(IType Type);
        /// <summary>
        /// Gets a specific register.
        /// </summary>
        /// <param name="Kind"></param>
        /// <param name="Index"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        IRegister GetRegister(RegisterType Kind, int Index, IType Type, bool Acquire);
        /// <summary>
        /// Allocates a static storage location.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        IStorageLocation AllocateStatic(IType Type);
        /// <summary>
        /// Allocates a static storage location that contains the specified value.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        IStorageLocation AllocateStatic(IBoundObject Value);

        /// <summary>
        /// Gets the argument with the provided index.
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        IStorageLocation GetArgument(int Index);

        IAssemblerLabel DeclareLabel(string Name);
        void MarkLabel(IAssemblerLabel Label);
    }

    public interface IFlowControlStructure
    {
        UniqueTag Tag { get; }
        IAssemblerBlock EmitBreak();
        IAssemblerBlock EmitContinue();
    }
}
