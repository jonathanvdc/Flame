using LLVMSharp;

namespace Flame.Llvm
{
    public static class LlvmBackend
    {
        public static LLVMModuleRef Compile(
            IAssembly assembly)
        {
            var module = LLVM.ModuleCreateWithName(assembly.Name.ToString());
            return module;
        }
    }
}
