using LLVMSharp;

namespace Flame.Llvm.Emit
{
    internal sealed class ModuleBuilder
    {
        public ModuleBuilder(LLVMModuleRef module)
        {
            this.Module = module;
        }

        public LLVMModuleRef Module { get; private set; }
    }
}
