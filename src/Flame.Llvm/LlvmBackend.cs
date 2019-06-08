using Flame.Compiler.Pipeline;
using LLVMSharp;

namespace Flame.Llvm
{
    public static class LlvmBackend
    {
        public static LLVMModuleRef Compile(
            AssemblyContentDescription contents)
        {
            var module = LLVM.ModuleCreateWithName(contents.FullName.FullyUnqualifiedName.ToString());
            return module;
        }
    }
}
