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
                int sourceMag = Source.GetPrimitiveSize();
                int targetMag = Target.GetPrimitiveSize();
                if (Target.GetIsBit())
                {
                    if (sourceMag < 8 && targetMag == 8)
                    {
                        Context.Emit(OpCodes.Conv_U8);
                    }
                    else if (sourceMag > 4 && targetMag == 4)
                    {
                        // Downcasting bit types is not cool, really. 
                        // I don't think the CLR back-end is the 
                        // right place to complain about conversions 
                        // like these, though. So let's just permit
                        // them and make the front-ends catch them.
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
                if (Source.GetPrimitiveBitSize() == Target.GetPrimitiveBitSize())
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
                    if (Source.GetPrimitiveBitSize() > 16)
                    {
                        // ushort and byte always fit in a float32, 
                        // so only perform this cast for uint and ulong
                        Context.Emit(OpCodes.Conv_R4);
                    }
                }
                else
                {
                    Context.Emit(OpCodes.Conv_R4);
                }
            }
            else if (CanElideIntegerCast(Source, Target))
            {
                // Do nothing.
            }
            else if (Target.Equals(PrimitiveTypes.Int8))
            {
                Context.Emit(OpCodes.Conv_I1);
            }
            else if (Target.Equals(PrimitiveTypes.Int16))
            {
                Context.Emit(OpCodes.Conv_I2);
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
            else if (Target.Equals(PrimitiveTypes.UInt16))
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
                Context.Emit(OpCodes.Conv_I2);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Determines if an integer cast from the first type to the
        /// second can be elided.
        /// </summary>
        /// <returns><c>true</c> if the integer cast can be elided; otherwise, <c>false</c>.</returns>
        /// <param name="From">The source integer type.</param>
        /// <param name="To">The target integer type.</param>
        private static bool CanElideIntegerCast(IType From, IType To)
        {
            // We can elide the following types of integer casts:
            //
            // *  Casts that convert an i_n to an i_m or a u_n to a u_m,
            //    where n <= m <= 4. This will simply chop off part 
            //    of the original value, and then sign/zero-extend 
            //    the result. However, the original values was _already_ 
            //    sign/zero-extended to an i4. So this is a nop.
            //
            // * Casts that convert an i_n to an i4/u4 or a u_n to an i4/u4.
            //   These casts don't do anything at all.
            //
            // We can reformulate this as: any integer-to-integer cast
            // can be elided, provided that the to-type is smaller than or
            // equal to an i4 in size, that the from-type is smaller
            // than or equal to the to-type, and that one of the following
            // holds:
            //
            // *  The to-type and from-type have the same signedness.
            // 
            // *  The to-type is either i4 or u4.
            //
            if ((From.GetIsInteger() || From.Equals(PrimitiveTypes.Char))
                && (To.GetIsInteger() || To.Equals(PrimitiveTypes.Char)))
            {
                var toBitSize = To.GetPrimitiveBitSize();
                if (toBitSize > 32)
                    return false;

                if (From.GetPrimitiveBitSize() > toBitSize)
                    return false;

                return (To.GetIsSignedInteger() && From.GetIsSignedInteger())
                || (To.GetIsUnsignedInteger() && From.GetIsUnsignedInteger())
                || (To.Equals(PrimitiveTypes.Int32) || To.Equals(PrimitiveTypes.UInt32));
            }
            else
            {
                return false;
            }
        }

        private void EmitToString(IType Type, IEmitContext Context)
        {
            if (!Type.Equals(PrimitiveTypes.Null))
            {
                var info = Type.GetMethod(new SimpleName("ToString"), false, PrimitiveTypes.String, new IType[0]);
                if (info == null)
                {
                    info = CodeGenerator.GetModule().Convert(Context.Processor.Body.Method.Module.TypeSystem.Object).GetMethod(new SimpleName("ToString"), false, PrimitiveTypes.String, new IType[0]);
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
                var callMethod = MethodType.GetMethod(Type);
                Context.Emit(OpCodes.Dup);
                Context.Emit(OpCodes.Ldvirtftn, callMethod);
                Context.Emit(OpCodes.Newobj, TargetType.GetConstructors().Single());
            }
        }

        #endregion

        #endregion

        public void Emit(IEmitContext Context)
        {
            var targetType = TargetType;
            bool targetIsDeleg = targetType.GetIsDelegate();

            if (Value is MethodBlock && targetIsDeleg)
            {
                if (TargetType is ICecilType)
                {
                    EmitRetypedMethodBlock((MethodBlock)Value, (ICecilType)targetType, Context);
                }
                else
                {
                    Value.Emit(Context);
                }
                return;
            }

            Value.Emit(Context);
            var exprType = Context.Stack.Pop();

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
            else if (exprType.GetIsPointer() && IsPrimitiveType(targetType))
            {
                EmitPrimitiveConversion(exprType, targetType, Context);
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
