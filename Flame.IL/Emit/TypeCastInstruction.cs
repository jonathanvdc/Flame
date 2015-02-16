using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class TypeCastInstruction : ILInstruction
    {
        public TypeCastInstruction(ICodeGenerator CodeGenerator, ICodeBlock Value, IType TargetType)
            : base(CodeGenerator)
        {
            this.Value = (IInstruction)Value;
            this.TargetType = TargetType;
        }

        public IInstruction Value { get; private set; }
        public IType TargetType { get; private set; }

        #region EmitConversion

        private ICodeBlock EmitConversion(IType exprType)
        {
            var targetType = TargetType;
            bool exprIsPtr = exprType.get_IsPointer();
            bool targIsPtr = targetType.get_IsPointer();
            if (exprIsPtr && targIsPtr)
            {
                if (exprType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && !targetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
                {
                    return new ConversionInstruction(CodeGenerator, OpCodes.ConvertUnsignedPointer, targetType);
                }
                else
                {
                    return new EmptyInstruction(CodeGenerator);
                }
            }
            else if (targIsPtr)
            {
                if (!exprType.get_IsValueType() && targetType.AsContainerType().GetElementType().get_IsValueType())
                {
                    var block = CodeGenerator.CreateBlock();
                    block.EmitBlock(new ConversionInstruction(CodeGenerator, OpCodes.Unbox, targetType.AsContainerType().GetElementType()));
                    if (!targetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
                    {
                        block.EmitBlock(new ConversionInstruction(CodeGenerator, OpCodes.ConvertUnsignedPointer, targetType.AsContainerType().GetElementType()));
                    }
                    return block;
                }
                else if (exprType.get_IsPrimitive() && exprType.get_IsUnsignedInteger())
                {
                    return new ConversionInstruction(CodeGenerator, OpCodes.ConvertUnsignedPointer, targetType);
                }
                else
                {
                    return new ConversionInstruction(CodeGenerator, OpCodes.ConvertPointer, targetType);
                }
            }
            else if ((exprType.get_IsVector() || exprType.get_IsArray()) && (targetType.get_IsArray() || targetType.get_IsVector()))
            {
                if (!exprType.AsContainerType().GetElementType().Is(targetType.AsContainerType().GetElementType()))
                {
                    return new ConversionInstruction(CodeGenerator, OpCodes.Castclass, targetType);
                }
                else
                {
                    return new EmptyInstruction(CodeGenerator);
                }
            }
            else if (!exprType.get_IsValueType() && targetType.get_IsValueType()) // Unboxing first
            {
                //Emit(OpCodes.Unbox, targetType);
                return new ConversionInstruction(CodeGenerator, OpCodes.UnboxAny, targetType);
            }
            else if ((exprType.get_IsValueType() || (exprType.get_IsGenericParameter() && !exprType.get_IsReferenceType())) && !targetType.get_IsValueType()) // Boxing second
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.Box, exprType, targetType);
            }
            else if (exprType.get_IsPrimitive() && targetType.get_IsPrimitive()) // Primitive conversions
            {
                return EmitPrimitiveConversion(exprType, targetType);
            }
            else if (targetType.Equals(PrimitiveTypes.String)) // ToString()
            {
                return EmitToString(exprType);
            }
            else if (targetType.get_IsReferenceType() && exprType.get_IsReferenceType()) // Castclass, then
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.Castclass, targetType);
            }
            else  // Unbox.Any as last resort
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.UnboxAny, targetType);
            }
        }

        #endregion

        #region EmitPrimitiveConversion

        private ICodeBlock EmitPrimitiveConversion(IType Source, IType Target)
        {
            if (Source.get_IsBit())
            {
                if (Source.GetPrimitiveMagnitude() == Target.GetPrimitiveMagnitude())
                {
                    if (Target.get_IsFloatingPoint())
                    {
                        var block = new PushPointerInstruction(CodeGenerator);
                        return ((IUnmanagedCodeGenerator)CodeGenerator).EmitDereferencePointer(block);
                    }
                    else
                    {
                        return new EmptyInstruction(CodeGenerator);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (Target.get_IsBit())
            {
                if (Source.GetPrimitiveMagnitude() == Target.GetPrimitiveMagnitude())
                {
                    if (Source.get_IsFloatingPoint())
                    {
                        var block = new PushPointerInstruction(CodeGenerator);
                        return ((IUnmanagedCodeGenerator)CodeGenerator).EmitDereferencePointer(block);
                    }
                    else
                    {
                        return new EmptyInstruction(CodeGenerator);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (Target.Equals(PrimitiveTypes.Float64))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertFloat64, Target);
            }
            else if (Target.Equals(PrimitiveTypes.Float32))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertFloat32, Target);
            }
            else if (Target.Equals(PrimitiveTypes.Int8))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt8, Target);
            }
            else if (Target.Equals(PrimitiveTypes.Int16))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt16, Target);
            }
            else if (Target.Equals(PrimitiveTypes.Int32))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt32, Target);
            }
            else if (Target.Equals(PrimitiveTypes.Int64))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt64, Target);
            }
            else if (Target.Equals(PrimitiveTypes.UInt8))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt8, Target);
            }
            else if (Target.Equals(PrimitiveTypes.UInt16))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt16, Target);
            }
            else if (Target.Equals(PrimitiveTypes.UInt32))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt32, Target);
            }
            else if (Target.Equals(PrimitiveTypes.UInt64))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt64, Target);
            }
            else if (Target.Equals(PrimitiveTypes.Char))
            {
                return new ConversionInstruction(CodeGenerator, OpCodes.ConvertInt16, Target);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region EmitToString

        private ICodeBlock EmitToString(IType Type)
        {
            var info = Type.GetMethods().Where((item) => item.Name == "ToString").FilterByStatic(false).GetBestMethod(Type, new IType[0]);
            return new InvocationInstruction(CodeGenerator, info, new EmptyInstruction(CodeGenerator), new IInstruction[0]);
        }

        #endregion

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Value.Emit(Context, TypeStack);
            var instruction = (IInstruction)EmitPrimitiveConversion(TypeStack.Peek(), TargetType);
            instruction.Emit(Context, TypeStack);
        }
    }
}
