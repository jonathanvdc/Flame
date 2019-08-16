using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Pipeline;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// Provides a high-level interface to Flame's LLVM back-end.
    /// </summary>
    public static class LlvmBackend
    {
        /// <summary>
        /// Compiles an assembly content description to an LLVM module.
        /// </summary>
        /// <param name="contents">An assembly content description to compile.</param>
        /// <param name="typeSystem">A type system to use.</param>
        /// <returns>An LLVM module builder.</returns>
        public static ModuleBuilder Compile(
            AssemblyContentDescription contents,
            TypeEnvironment typeSystem)
        {
            return Compile(contents, typeSystem, new ItaniumMangler(typeSystem));
        }

        /// <summary>
        /// Compiles an assembly content description to an LLVM module.
        /// </summary>
        /// <param name="contents">An assembly content description to compile.</param>
        /// <param name="typeSystem">A type system to use.</param>
        /// <param name="mangler">A name mangler to use.</param>
        /// <returns>An LLVM module builder.</returns>
        public static ModuleBuilder Compile(
            AssemblyContentDescription contents,
            TypeEnvironment typeSystem,
            NameMangler mangler)
        {
            return Compile(contents, typeSystem, mangler, ClrInternalCallImplementor.Instance);
        }

        /// <summary>
        /// Compiles an assembly content description to an LLVM module.
        /// </summary>
        /// <param name="contents">An assembly content description to compile.</param>
        /// <param name="typeSystem">A type system to use.</param>
        /// <param name="mangler">A name mangler to use.</param>
        /// <param name="internalCallImplementor">An internal call implementor to use.</param>
        /// <returns>An LLVM module builder.</returns>
        public static ModuleBuilder Compile(
            AssemblyContentDescription contents,
            TypeEnvironment typeSystem,
            NameMangler mangler,
            InternalCallImplementor internalCallImplementor)
        {
            var module = LLVM.ModuleCreateWithName(contents.FullName.FullyUnqualifiedName.ToString());
            var builder = new ModuleBuilder(
                module,
                typeSystem,
                mangler,
                MallocInterface.Instance,
                new ClosedMetadataFormat(contents.Types, contents.TypeMembers));

            var icalls = new Dictionary<IMethod, LLVMValueRef>();
            foreach (var method in contents.TypeMembers.OfType<IMethod>())
            {
                var fun = builder.DeclareMethod(method);
                if (method.IsInternalCall())
                {
                    icalls[method] = fun;
                }
            }

            foreach (var pair in contents.MethodBodies)
            {
                builder.DefineMethod(pair.Key, pair.Value);
                icalls.Remove(pair.Key);
            }

            foreach (var pair in icalls)
            {
                internalCallImplementor.Implement(pair.Key, pair.Value, builder);
            }

            if (contents.EntryPoint != null)
            {
                // If there is an entry point, then we will synthesize a 'main' function that calls
                // said entry point.
                builder.SynthesizeMain(contents.EntryPoint);
            }

            return builder;
        }
    }
}
