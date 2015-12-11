using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ConversionBlock : ICecilBlock
    {
        public ConversionBlock(ICodeGenerator CodeGenerator, ICecilBlock Value, IType TargetType)
        {
            this.Value = Value;
            this.CodeGenerator = CodeGenerator;
            this.TargetType = TargetType;
        }

        public ICecilBlock Value { get; private set; }
        public IType TargetType { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        #region Conversions

        #region Helpers

        private static bool IsPrimitiveType(IType Type)
        {
            return Type.GetIsPrimitive();
        }

        private void EmitPrimitiveConversion(IType Source, IType Target, IEmitContext Context)
        {
            if (Source.GetIsBit())
            {
                int sourceMag = Source.GetPrimitiveMagnitude();
                int targetMag = Target.GetPrimitiveMagnitude();
                if (Target.GetIsBit())
                {
                    if (sourceMag < 4 && targetMag == 4)
                    {
                        Context.Emit(OpCodes.Conv_U8);
                    }
                    else if (sourceMag > 3 && targetMag == 3) // Downcasting bit types is not cool, really. I don't think the CLR back-end should be the one to complain about this, though.
                    {
                        Context.Emit(OpCodes.Conv_U4);
                    }
                    else if (sourceMag > 2 && targetMag == 2)
                    {
                        Context.Emit(OpCodes.Conv_U2);
                    }
                    else if (sourceMag > 1 && targetMag == 1)
                    {
                        Context.Emit(OpCodes.Conv_U1);
                    }
                }
                else if (sourceMag == targetMag)
                {
                    if (Target.GetIsFloatingPoint())
                    {
                        Context.Stack.Push(Source);
                        Context.EmitPushPointerCommands((IUnmanagedCodeGenerator)CodeGenerator, Source, true);
                        Context.Stack.Pop();
                        new DereferencePointerEmitter().Emit(Context, Target);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot convert between bit types and types of mismatched size.");
                }
            }
            else if (Target.GetIsBit())
            {
                if (Source.GetPrimitiveMagnitude() == Target.GetPrimitiveMagnitude())
                {
                    if (Source.GetIsFloatingPoint())
                    {
                        Context.Stack.Push(Source);
                        Context.EmitPushPointerCommands((IUnmanagedCodeGenerator)CodeGenerator, Source, true);
                        Context.Stack.Pop();
                        new DereferencePointerEmitter().Emit(Context, Target);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot convert between bit types and types of mismatched size.");
                }
            }
            else if (Target.Equals(PrimitiveTypes.Float64))
            {
                if (Source.GetIsUnsignedInteger())
                {
                    Context.Emit(OpCodes.Conv_R_Un);
                    Context.Emit(OpCodes.Conv_R8);
                }
                else
                {
                    Context.Emit(OpCodes.Conv_R8);
                }
            }
            else if (Target.Equals(PrimitiveTypes.Float32))
            {
                if (Source.GetIsUnsignedInteger())
                {
                    Context.Emit(OpCodes.Conv_R_Un);
                    if (Source.GetPrimitiveMagnitude() > 2) // ushort and byte always fit in a float32, so only perform this cast for uint and ulong 
                    {
                        Context.Emit(OpCodes.Conv_R4);
                    }
                }
                else
                {
                    Context.Emit(OpCodes.Conv_R4);
                }
            }
            else if (Target.Equals(PrimitiveTypes.Int8))
            {
                Context.Emit(OpCodes.Conv_I1);
            }
            else if (Target.Equals(PrimitiveTypes.Int16))
            {
                if (!Source.Equals(PrimitiveTypes.Char))
                {
                    Context.Emit(OpCodes.Conv_I2);
                }
            }
            else if (Target.Equals(PrimitiveTypes.Int32))
            {
                Context.Emit(OpCodes.Conv_I4);
            }
            else if (Target.Equals(PrimitiveTypes.Int64))
            {
                Context.Emit(OpCodes.Conv_I8);
            }
            else if (Target.Equals(PrimitiveTypes.UInt8))
            {
                Context.Emit(OpCodes.Conv_U1);
            }
            else if (Target.Equals(PrimitiveTypes.UInt16) && !Source.Equals(PrimitiveTypes.Char))
            {
                Context.Emit(OpCodes.Conv_U2);
            }
            else if (Target.Equals(PrimitiveTypes.UInt32))
            {
                Context.Emit(OpCodes.Conv_U4);
            }
            else if (Target.Equals(PrimitiveTypes.UInt64))
            {
                Context.Emit(OpCodes.Conv_U8);
            }
            else if (Target.Equals(PrimitiveTypes.Char))
            {
                if (Source.GetPrimitiveMagnitude() != 2)
                {
                    Context.Emit(OpCodes.Conv_I2);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void EmitToString(IType Type, IEmitContext Context)
        {
            if (!Type.Equals(PrimitiveTypes.Null))
            {
                var info = Type.GetMethod("ToString", false, PrimitiveTypes.String, new IType[0]);
                if (info == null)
                {
                    info = CodeGenerator.GetModule().Convert(Context.Processor.Body.Method.Module.TypeSystem.Object).GetMethod("ToString", false, PrimitiveTypes.String, new IType[0]);
                }
                var block = (ICecilBlock)CodeGenerator.EmitInvocation(info, new VirtualPushBlock(CodeGenerator, Type), new ICodeBlock[0]);
                block.Emit(Context);
                Context.Stack.Pop();
            }
        }

        private bool CanDowncastDelegate(IType Type, IType TargetType)
        {
            return Type.GetGenericDeclaration().Equals(TargetType.GetGenericDeclaration()) && Type.Is(TargetType);
        }

        private void EmitRetypedMethodBlock(MethodBlock Block, ICecilType TargetType, IEmitContext Context)
        {
            Block.ChangeDelegateType(TargetType).Emit(Context);
        }

        private void EmitDelegateConversion(ICecilType Type, ICecilType TargetType, IEmitContext Context)
        {
            if (!CanDowncastDelegate(Type, TargetType))
            {
                var callMethod = CecilDelegateType.GetInvokeMethod(Type);
                Context.Emit(OpCodes.Newobj, TargetType.GetConstructors().Single());
            }
        }

        #endregion

        #endregion

        public void Emit(IEmitContext Context)
        {
            bool targetIsDeleg = TargetType.GetIsDelegate();

            if (Value is MethodBlock && targetIsDeleg)
            {
                if (TargetType is ICecilType)
                {
                    EmitRetypedMethodBlock((MethodBlock)Value, (ICecilType)TargetType, Context); 
                }
                else
                {
                    Value.Emit(Context);
                }
                return;
            }

            Value.Emit(Context);
            var exprType = Context.Stack.Pop();
            var targetType = TargetType;

            if (targetType.GetIsGeneric() && targetType.GetIsGenericDeclaration())
            {
                throw new Exception("Type casts to open generic types are not allowed.");
            }

            if (targetType.GetIsPointer())
            {
                if (exprType.GetIsPointer())
                {
                    throw new InvalidOperationException("static_cast cannot be used to convert between pointer or reference types. Use reinterpret_cast or dynamic_cast instead.");
                }

                if (!exprType.GetIsValueType() && targetType.AsContainerType().AsPointerType().ElementType.GetIsValueType())
                {
                    Context.Emit(OpCodes.Unbox, targetType.AsContainerType().AsPointerType().ElementType);
                    if (!targetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
                    {
                        Context.Emit(OpCodes.Conv_U);
                    }
                }
                else if (exprType.GetIsUnsignedInteger())
                {
                    Context.Emit(OpCodes.Conv_U);
                }
                else
                {
                    Context.Emit(OpCodes.Conv_I);
                }
            }
            else if (exprType.GetIsVector() || exprType.GetIsArray())
            {
                if (targetType.GetIsArray() || targetType.GetIsVector())
                {
                    if (!exprType.AsContainerType().ElementType.Is(targetType.AsContainerType().ElementType))
                    {
                        Context.Emit(OpCodes.Castclass, targetType);
                    }
                }
                // Otherwise, do nothing. The target type is assumed to be a base type of the array type.
            }
            else if (!ILCodeGenerator.IsCLRValueType(exprType) && ILCodeGenerator.IsCLRValueType(targetType)) // Unboxing first
            {
                //Emit(OpCodes.Unbox, targetType);
                Context.Emit(OpCodes.Unbox_Any, targetType);
            }
            else if (TargetType.Equals(PrimitiveTypes.String)) // ToString()
            {
                EmitToString(exprType, Context);
            }
            else if (ILCodeGenerator.IsPossibleValueType(exprType) && !ILCodeGenerator.IsCLRValueType(targetType)) // Boxing second
            {
                Context.Emit(OpCodes.Box, exprType);
                if (!exprType.Is(targetType))
                {
                    Context.Emit(OpCodes.Castclass, targetType);
                }
            }
            else if (IsPrimitiveType(exprType) && IsPrimitiveType(targetType)) // Primitive conversions
            {
                EmitPrimitiveConversion(exprType, targetType, Context);
            }
            else if (targetIsDeleg && exprType.GetIsDelegate())
            {
                if (TargetType is ICecilType)
                {
                    EmitDelegateConversion((ICecilType)exprType, (ICecilType)targetType, Context);
                }
            }
            else  // Unbox.Any as last resort
            {
                if (!ILCodeGenerator.IsCLRValueType(exprType) || !ILCodeGenerator.IsCLRValueType(targetType)) // Do not use Unbox.Any if both types are value types
                {
                    Context.Emit(OpCodes.Unbox_Any, targetType);
                }
            }
            Context.Stack.Push(targetType);
        }

        public IType BlockType
        {
            get { return TargetType; }
        }
    }
}
