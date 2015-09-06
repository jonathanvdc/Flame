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

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            var exprType = Context.Stack.Pop();
            var targetType = TargetType;
            bool exprIsPtr = exprType.get_IsPointer();
            bool targIsPtr = targetType.get_IsPointer();

            if (exprIsPtr && targIsPtr)
            {
                if (exprType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && !targetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
                {
                    Context.Emit(OpCodes.Conv_U);
                }
                // Else, do absolutely nothing
            }
            else if (TargetType.get_IsReferenceType() && exprType.get_IsReferenceType()) // Castclass, then
            {
                if (!exprType.Is(targetType))
                {
                    Context.Emit(OpCodes.Castclass, targetType);
                }
            }
            else
            {
                throw new InvalidOperationException("A value of type '" + exprType.FullName + "' could not be converted to type '" + targetType + "' by a dynamic_cast.");
            }

            Context.Stack.Push(TargetType);
        }

        public IType BlockType
        {
            get { return TargetType; }
        }
    }
}
