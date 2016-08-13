using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class DynamicCastBlock : ICecilBlock
    {
        public DynamicCastBlock(ICecilBlock Value, IType TargetType)
        {
            this.Value = Value;
            this.TargetType = TargetType;
        }

        public ICecilBlock Value { get; private set; }
        public IType TargetType { get; private set; }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        private InvalidOperationException CreateException(
            IType SourceType, IType TargetType)
        {
            return new InvalidOperationException(
                "A value of type '" + SourceType.FullName + 
                "' could not be converted to type '" + TargetType + 
                "' by a dynamic_cast.");
        }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            var exprType = Context.Stack.Pop();
            var targetType = TargetType;
            bool exprIsPtr = exprType.GetIsPointer();
            bool targIsPtr = targetType.GetIsPointer();

            if (exprIsPtr && targIsPtr)
            {
                if (exprType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && !targetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
                {
                    Context.Emit(OpCodes.Conv_U);
                }
                // Else, do absolutely nothing
            }
            else if (exprType.GetIsReferenceType())
            {
                if (TargetType.GetIsReferenceType())
                {
                    if (!exprType.Is(targetType))
                    {
                        // castclass, then
                        Context.Emit(OpCodes.Castclass, targetType);
                    }
                }
                else if (targIsPtr && ILCodeGenerator.IsCLRValueType(TargetType.AsPointerType().ElementType))
                {
                    Context.Emit(OpCodes.Unbox);
                }
                else if (ILCodeGenerator.IsCLRValueType(targetType))
                {
                    // unbox.any can be used to convert reference types
                    // to value types.
                    Context.Emit(OpCodes.Unbox_Any, targetType);
                }
                else
                {
                    throw CreateException(exprType, targetType);
                }
            }
            else if (ILCodeGenerator.IsPossibleValueType(exprType) 
                && !ILCodeGenerator.IsCLRValueType(targetType))
            {
                Context.Emit(OpCodes.Box, exprType);
                if (!exprType.Is(targetType))
                {
                    Context.Emit(OpCodes.Castclass, targetType);
                }
            }

            Context.Stack.Push(TargetType);
        }

        public IType BlockType
        {
            get { return TargetType; }
        }
    }
}
