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
                var block = CodeGenerator.CreateBlock();
                local.CreateSetStatement(new CodeBlockExpression(CodeGenerator.CreateBlock(), ElementType)).Emit(block); // Set temporary to value on top of stack
                block.EmitBlock(local.CreateAddressOfExpression().Emit(CodeGenerator)); // Push address on stack
                local.CreateReleaseStatement().Emit(block); // Release temporary
                ((ICecilBlock)block).Emit(Context);
            }
            else
            {
                Context.Stack.Push(Context.Stack.Pop().MakePointerType(PointerKind.ReferencePointer));
            }
        }

        #endregion
    }
}
