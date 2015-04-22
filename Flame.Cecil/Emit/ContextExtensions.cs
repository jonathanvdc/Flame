using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public static class ContextExtensions
    {
        #region EmitPushPointerCommands

        /// <summary>
        /// Emits the appropriate commands to push a pointer to the value on top of the stack.
        /// </summary>
        /// <param name="ElementType"></param>
        public static void EmitPushPointerCommands(this IEmitContext Context, IUnmanagedCodeGenerator CodeGenerator, IType ElementType, bool AllowTransientPointers)
        {
            var optimization = new PushPointerOptimization(AllowTransientPointers);

            if (!Context.ApplyOptimization(optimization))
            {
                var local = CodeGenerator.DeclareUnmanagedVariable(ElementType); // Create temporary
                ((ICecilBlock)local.EmitSet(CodeGenerator.EmitVoid())).Emit(Context); // Set temporary to value on top of stack
                ((ICecilBlock)local.EmitAddressOf()).Emit(Context); // Push address on stack
                ((ICecilBlock)local.EmitRelease()).Emit(Context); // Release temporary
            }
            else
            {
                Context.Stack.Push(Context.Stack.Pop().MakePointerType(PointerKind.ReferencePointer));
            }
        }

        #endregion
    }
}
