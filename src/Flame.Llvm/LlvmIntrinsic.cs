using System;
using Flame.Llvm.Emit;

namespace Flame.Llvm
{
    /// <summary>
    /// A description of an LLVM intrinsic function.
    /// </summary>
    internal sealed unsafe class LlvmIntrinsic
    {
        /// <summary>
        /// Creates an LLVM intrinsic function description.
        /// </summary>
        /// <param name="name">The name of an intrinsic function.</param>
        /// <param name="define">A function that defines the intrinsic in a particular module.</param>
        public LlvmIntrinsic(string name, Func<LLVMModuleRef, LLVMValueRef> define)
        {
            this.Name = name;
            this.Define = define;
        }

        /// <summary>
        /// Creates an LLVM intrinsic function description.
        /// </summary>
        /// <param name="name">The name of an intrinsic function.</param>
        /// <param name="define">
        /// A function that creates the intrinsic's function prototype in a particular context.
        /// </param>
        public LlvmIntrinsic(string name, Func<LLVMContextRef, LLVMTypeRef> getPrototype)
            : this(name, module => module.AddFunction(name, getPrototype(module.Context)))
        { }

        /// <summary>
        /// Gets the name of the intrinsic.
        /// </summary>
        /// <value>An intrinsic function name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Defines the intrinsic in a particular module that does not yet
        /// have a definition for the intrinsic.
        /// </summary>
        /// <value>An intrinsic-defining function.</value>
        public Func<LLVMModuleRef, LLVMValueRef> Define { get; private set; }

        /// <summary>
        /// Gets the intrinsic as defined in a particular module. The
        /// intrinsic is defined if it is not yet defined.
        /// </summary>
        /// <param name="module">An LLVM module.</param>
        /// <returns>The intrinsic.</returns>
        public LLVMValueRef GetOrDefine(LLVMModuleRef module)
        {
            var fun = module.GetNamedFunction(Name);
            if (fun.Pointer() == IntPtr.Zero)
            {
                fun = Define(module);
            }
            return fun;
        }

        /// <summary>
        /// Gets the intrinsic as defined in a particular module. The
        /// intrinsic is defined if it is not yet defined.
        /// </summary>
        /// <param name="module">An LLVM module.</param>
        /// <returns>The intrinsic.</returns>
        public LLVMValueRef GetOrDefine(ModuleBuilder module)
        {
            return GetOrDefine(module.Module);
        }

        /// <summary>
        /// A version of the 'memmove' intrinsic that takes a 32-bit
        /// integer to describe the number of bytes that need to be copied.
        /// </summary>
        /// <value>A 'memmove' intrinsic.</value>
        public static readonly LlvmIntrinsic MemmoveInt32 =
            new LlvmIntrinsic(
                "llvm.memmove.p0i8.p0i8.i32",
                new Func<LLVMContextRef, LLVMTypeRef>(context => new LLVMTypeRef((IntPtr)LLVM.VoidTypeInContext(context)).CreateFunctionType(
                    new LLVMTypeRef[]
                    {
                        new LLVMTypeRef((IntPtr)LLVM.PointerType(LLVM.Int8TypeInContext(context), 0)),
                        new LLVMTypeRef((IntPtr)LLVM.PointerType(LLVM.Int8TypeInContext(context), 0)),
                        new LLVMTypeRef((IntPtr)LLVM.Int32TypeInContext(context)),
                        new LLVMTypeRef((IntPtr)LLVM.Int1TypeInContext(context))
                    },
                    false)));

        /// <summary>
        /// A version of the 'memcpy' intrinsic that takes a 32-bit
        /// integer to describe the number of bytes that need to be copied.
        /// </summary>
        /// <value>A 'memcpy' intrinsic.</value>
        public static readonly LlvmIntrinsic MemcpyInt32 =
            new LlvmIntrinsic(
                "llvm.memcpy.p0i8.p0i8.i32",
                new Func<LLVMContextRef, LLVMTypeRef>(context => new LLVMTypeRef((IntPtr)LLVM.VoidTypeInContext(context)).CreateFunctionType(
                    new LLVMTypeRef[]
                    {
                        new LLVMTypeRef((IntPtr)LLVM.PointerType(LLVM.Int8TypeInContext(context), 0)),
                        new LLVMTypeRef((IntPtr)LLVM.PointerType(LLVM.Int8TypeInContext(context), 0)),
                        new LLVMTypeRef((IntPtr)LLVM.Int32TypeInContext(context)),
                        new LLVMTypeRef((IntPtr)LLVM.Int1TypeInContext(context))
                    },
                    false)));
    }
}
