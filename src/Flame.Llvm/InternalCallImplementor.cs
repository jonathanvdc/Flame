using System;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// Responsible for implementing methods that have no method body and are
    /// marked as internal calls.
    /// </summary>
    public abstract class InternalCallImplementor
    {
        /// <summary>
        /// Implements a method by synthesizing an appropriate body for its
        /// LLVM function.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <param name="module">The module that defines <paramref name="method"/>.</param>
        public abstract void Implement(IMethod method, LLVMValueRef function, ModuleBuilder module);
    }

    /// <summary>
    /// An internal call implementor for the CLR.
    /// </summary>
    public class ClrInternalCallImplementor : InternalCallImplementor
    {
        /// <summary>
        /// Creates an instance of a CLR internal call implementor.
        /// </summary>
        protected ClrInternalCallImplementor()
        { }

        /// <summary>
        /// An instance of an internal call implementor for the CLR.
        /// </summary>
        /// <returns>A CLR internal call implementor.</returns>
        public static ClrInternalCallImplementor Instance =
            new ClrInternalCallImplementor();

        /// <inheritdoc/>
        public override void Implement(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            if (TryImplementInterlocked(method, function, module)
                || TryImplementThread(method, function, module)
                || TryImplementRuntimeImports(method, function, module)
                || TryImplementBuffer(method, function, module)
                || TryImplementString(method, function, module))
            {
                return;
            }
            throw new NotSupportedException(
                $"Method '{method.FullName}' is marked as \"internal call\" but " +
                "is not a known CLR internal call method.");
        }

        /// <summary>
        /// Tries to implement a method defined in the <see cref="System.String"/> class.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <returns><c>true</c> if <paramref name="method"/> was implemented; otherwise, <c>false</c>.</returns>
        private bool TryImplementString(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            if (!IsStaticMethodOf(method, "System.String"))
            {
                return false;
            }

            var name = method.Name.ToString();
            var paramCount = method.Parameters.Count;
            if (name == "FastAllocateString" && paramCount == 1)
            {
                var dataType = method.ParentType;
                var llvmType = module.ImportType(dataType);
                var fields = MethodBodyEmitter.DecomposeStringFields(dataType, module);

                var ep = function.AppendBasicBlock("entry");
                using (var builder = new IRBuilder(module.Context))
                {
                    builder.PositionBuilderAtEnd(ep);

                    var size = LLVM.SizeOf(llvmType.GetStructElementTypes()[fields.DataFieldIndex]);

                    var value = module.GC.EmitAllocObject(
                        dataType,
                        builder.CreateMul(
                            builder.CreateZExt(
                                function.GetParam(0),
                                size.TypeOf(),
                                ""),
                            size,
                            "bytecount"),
                        module,
                        builder,
                        "str");

                    var lengthPtr = fields.GetLengthPtr(value, builder);
                    builder.CreateStore(
                        builder.CreateTrunc(
                            function.GetParam(0),
                            lengthPtr.TypeOf().GetElementType(),
                            ""),
                        fields.GetLengthPtr(value, builder));
                    builder.CreateRet(value);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to implement a method defined in the <see cref="System.Runtime.RuntimeImports"/> class.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <returns><c>true</c> if <paramref name="method"/> was implemented; otherwise, <c>false</c>.</returns>
        private bool TryImplementRuntimeImports(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            if (!IsStaticMethodOf(method, "System.Runtime.RuntimeImports"))
            {
                return false;
            }

            var name = method.Name.ToString();
            var paramCount = method.Parameters.Count;

            if (name == "Memmove" && paramCount == 3)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder =>
                        builder.CreateCall(
                            LlvmIntrinsic.MemmoveInt32.GetOrDefine(module),
                            new[]
                            {
                                function.GetParam(0),
                                function.GetParam(1),
                                function.GetParam(2),
                                LLVM.ConstInt(LLVM.Int1TypeInContext(module.Context), 0, false)
                            },
                            ""));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to implement a method defined in the <see cref="System.Buffer"/> class.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <returns><c>true</c> if <paramref name="method"/> was implemented; otherwise, <c>false</c>.</returns>
        private bool TryImplementBuffer(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            if (!IsStaticMethodOf(method, "System.Buffer"))
            {
                return false;
            }

            var name = method.Name.ToString();
            var paramCount = method.Parameters.Count;

            if (name == "InternalMemcpy" && paramCount == 3)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder =>
                        builder.CreateCall(
                            LlvmIntrinsic.MemcpyInt32.GetOrDefine(module),
                            new[]
                            {
                                function.GetParam(0),
                                function.GetParam(1),
                                function.GetParam(2),
                                LLVM.ConstInt(LLVM.Int1TypeInContext(module.Context), 0, false)
                            },
                            ""));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to implement a method defined in the <see cref="System.Threading.Thread"/> class.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <returns><c>true</c> if <paramref name="method"/> was implemented; otherwise, <c>false</c>.</returns>
        private bool TryImplementThread(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            if (!IsStaticMethodOf(method, "System.Threading.Thread"))
            {
                return false;
            }

            var name = method.Name.ToString();
            var paramCount = method.Parameters.Count;

            if (name == nameof(System.Threading.Thread.VolatileRead) && paramCount == 1)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder =>
                    {
                        var load = builder.CreateLoad(function.GetParam(0), "");
                        load.SetVolatile(true);
                        return load;
                    });
                return true;
            }
            else if (name == nameof(System.Threading.Thread.VolatileWrite) && paramCount == 1)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder =>
                    {
                        var store = builder.CreateStore(function.GetParam(1), function.GetParam(0));
                        store.SetVolatile(true);
                        return store;
                    });
                return true;
            }
            else if (name == nameof(System.Threading.Thread.MemoryBarrier) && paramCount == 0)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder =>
                        builder.CreateFence(
                            LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                            false,
                            ""));
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsStaticMethodOf(IMethod method, string fullName)
        {
            var type = method.ParentType;
            return type.FullName.ToString() == fullName && method.IsStatic;
        }

        /// <summary>
        /// Tries to implement a method defined in the <see cref="System.Threading.Interlocked"/> class.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <returns><c>true</c> if <paramref name="method"/> was implemented; otherwise, <c>false</c>.</returns>
        private bool TryImplementInterlocked(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            if (!IsStaticMethodOf(method, "System.Threading.Interlocked"))
            {
                return false;
            }

            var name = method.Name.ToString();
            var paramCount = method.Parameters.Count;
            if (name == "Add" && paramCount == 2)
            {
                ImplementWithAtomicAdd(function, function.GetParam(1), module);
                return true;
            }
            if (name == "Exchange" && paramCount == 2)
            {
                ImplementWithAtomic(function, LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpXchg, function.GetParam(1), module);
                return true;
            }
            else if (name == nameof(System.Threading.Interlocked.CompareExchange) && paramCount == 3)
            {
                var ep = function.AppendBasicBlock("entry");
                var builder = LLVM.CreateBuilderInContext(module.Context);
                LLVM.PositionBuilderAtEnd(builder, ep);
                var cmp = LLVM.BuildAtomicCmpXchg(
                    builder,
                    function.GetParam(0),
                    function.GetParam(2),
                    function.GetParam(1),
                    LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                    LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                    false);
                LLVM.BuildRet(builder, LLVM.BuildExtractValue(builder, cmp, 0, ""));
                LLVM.DisposeBuilder(builder);
                return true;
            }
            else if (name == "Increment" && paramCount == 1)
            {
                ImplementWithAtomicAdd(method, function, 1, module);
                return true;
            }
            else if (name == "Decrement" && paramCount == 1)
            {
                ImplementWithAtomicAdd(method, function, -1, module);
                return true;
            }
            else if (name == "Read" && paramCount == 1)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder =>
                    {
                        var load = builder.CreateLoad(function.GetParam(0), "");
                        load.SetVolatile(true);
                        return load;
                    });
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ImplementWithAtomicAdd(IMethod method, LLVMValueRef function, int rhs, ModuleBuilder module)
        {
            ImplementWithAtomicAdd(
                function,
                LLVM.ConstInt(
                    module.ImportType(((PointerType)method.Parameters[0].Type).ElementType),
                    (ulong)rhs,
                    true),
                module);
        }

        private void ImplementWithAtomicAdd(LLVMValueRef function, LLVMValueRef rhs, ModuleBuilder module)
        {
            ImplementWithAtomic(function, LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpAdd, rhs, module);
        }

        private void ImplementWithAtomic(LLVMValueRef function, LLVMAtomicRMWBinOp op, LLVMValueRef rhs, ModuleBuilder module)
        {
            ImplementWithInstruction(
                function,
                module,
                builder => builder.CreateAtomicRMW(
                    op,
                    function.GetParam(0),
                    rhs,
                    LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                    false));
        }

        private void ImplementWithInstruction(
            LLVMValueRef function,
            ModuleBuilder module,
            Func<IRBuilder, LLVMValueRef> createInstruction)
        {
            var ep = function.AppendBasicBlock("entry");
            using (var builder = new IRBuilder(module.Context))
            {
                builder.PositionBuilderAtEnd(ep);
                var insn = createInstruction(builder);
                if (insn.TypeOf().TypeKind == LLVMTypeKind.LLVMVoidTypeKind)
                {
                    builder.CreateRetVoid();
                }
                else
                {
                    builder.CreateRet(insn);
                }
            }
        }
    }
}
