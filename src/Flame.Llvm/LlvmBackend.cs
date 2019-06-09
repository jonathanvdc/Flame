using System.Linq;
using Flame.Compiler.Pipeline;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm
{
    public static class LlvmBackend
    {
        public static LLVMModuleRef Compile(
            AssemblyContentDescription contents,
            TypeEnvironment typeSystem)
        {
            var module = LLVM.ModuleCreateWithName(contents.FullName.FullyUnqualifiedName.ToString());
            var builder = new ModuleBuilder(module, typeSystem);
            foreach (var method in contents.TypeMembers.OfType<IMethod>())
            {
                builder.DeclareMethod(method);
            }
            foreach (var pair in contents.MethodBodies)
            {
                builder.DefineMethod(pair.Key, pair.Value);
            }
            return module;
        }
    }
}
