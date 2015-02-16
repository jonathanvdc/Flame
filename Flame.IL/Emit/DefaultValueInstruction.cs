using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class DefaultValueInstruction : ILInstruction
    {
        public DefaultValueInstruction(ICodeGenerator CodeGenerator, IType Type)
            : base(CodeGenerator)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            if (Type.get_IsSignedInteger() || Type.get_IsUnsignedInteger() || Type.get_IsBit())
            {
                if (Type.GetPrimitiveMagnitude() == 4)
                {
                    new PushInt64Instruction(CodeGenerator, 0, Type).Emit(Context, TypeStack);
                }
                else
                {
                    new PushInt32Instruction(CodeGenerator, 0, Type).Emit(Context, TypeStack);
                }
            }
            else if (Type.get_IsFloatingPoint())
            {
                if (Type.Equals(PrimitiveTypes.Float64))
                {
                    new PushFloat64Instruction(CodeGenerator, 0).Emit(Context, TypeStack);
                }
                else
                {
                    new PushFloat32Instruction(CodeGenerator, 0).Emit(Context, TypeStack);
                }
            }
            else
            {
                bool useGenericApproach;
                if (Type.get_IsValueType())
                {
                    useGenericApproach = true;
                }
                else if (Type.get_IsGenericParameter())
                {
                    useGenericApproach = !(Type as IGenericParameter).get_IsReferenceType();
                }
                else
                {
                    useGenericApproach = false;
                }
                if (useGenericApproach)
                {
                    var tempVar = ((IUnmanagedCodeGenerator)CodeGenerator).DeclareUnmanagedVariable(Type);

                    var block = CodeGenerator.CreateBlock();

                    block.EmitBlock(tempVar.CreateAddressOfExpression().Emit(CodeGenerator));
                    block.EmitBlock(new InitObjectInstruction(CodeGenerator, Type));
                    block.EmitBlock(tempVar.CreateGetExpression().Emit(CodeGenerator));

                    ((IInstruction)block).Emit(Context, TypeStack);
                }
                else
                {
                    new PushNullInstruction(CodeGenerator).Emit(Context, TypeStack);
                }
            }
        }
    }
}
