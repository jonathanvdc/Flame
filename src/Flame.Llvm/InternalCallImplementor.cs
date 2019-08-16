using System;
using System.Collections.Generic;
using Flame.Llvm.Emit;
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
            if (TryImplementInterlocked(method, function, module))
            {
                return;
            }
            throw new NotSupportedException(
                $"Method '{method.FullName}' is marked as \"internal call\" but " +
                "is not a known CLR internal call method.");
        }

        /// <summary>
        /// Tries to implement a method defined in the <see cref="System.Threading.Interlocked"/> class.
        /// </summary>
        /// <param name="method">An internal call method to implement.</param>
        /// <param name="function"><paramref name="method"/>'s corresponding LLVM function.</param>
        /// <returns><c>true</c> if <paramref name="method"/> was implemented; otherwise, <c>false</c>.</returns>
        private bool TryImplementInterlocked(IMethod method, LLVMValueRef function, ModuleBuilder module)
        {
            var type = method.ParentType;
            if (type.FullName.ToString() != "System.Threading.Interlocked"
                || !method.IsStatic)
            {
                return false;
            }

            var name = method.Name.ToString();
            if (name == "Add" && method.Parameters.Count == 2)
            {
                ImplementWithInstruction(
                    function,
                    module,
                    builder => builder.CreateAtomicRMW(
                        LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpAdd,
                        function.GetParam(0),
                        function.GetParam(1),
                        LLVMAtomicOrdering.LLVMAtomicOrderingAcquireRelease,
                        false));
                return true;
            }
            return false;
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
                builder.CreateRet(insn);
            }
        }
    }
}
