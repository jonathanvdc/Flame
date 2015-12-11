using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ConversionBlock : IOpBlock
    {
        public ConversionBlock(ICodeGenerator CodeGenerator, ICppBlock Value, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }
        public ICppBlock Value { get; private set; }
        public int Precedence { get { return 3; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies.MergeDependencies(Type.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public static bool UseImplicitCast(IType SourceType, IType TargetType)
        {
            if (SourceType.Is(TargetType))
            {
                return true;
            }
            else if (SourceType.GetIsPointer() && TargetType.GetIsPointer() && SourceType.AsContainerType().AsPointerType().PointerKind.Equals(TargetType.AsContainerType().AsPointerType().PointerKind))
            {
                return SourceType.AsContainerType().ElementType.Is(TargetType.AsContainerType().ElementType);
            }
            else if (TargetType.GetIsBit())
            {
                return SourceType.GetIsUnsignedInteger();
            }
            else if (SourceType.GetIsBit())
            {
                return TargetType.GetIsUnsignedInteger();
            }
            else if ((SourceType.GetIsSignedInteger() && TargetType.GetIsSignedInteger()) || (SourceType.GetIsUnsignedInteger() && TargetType.GetIsUnsignedInteger()) || (SourceType.GetIsBit() && TargetType.GetIsBit()) || (SourceType.GetIsFloatingPoint() && TargetType.GetIsFloatingPoint()))
            {
                return SourceType.GetPrimitiveMagnitude() <= TargetType.GetPrimitiveMagnitude();
            }
            else if (SourceType.GetIsUnsignedInteger() && TargetType.GetIsSignedInteger())
            {
                return SourceType.GetPrimitiveMagnitude() < TargetType.GetPrimitiveMagnitude();
            }
            else
            {
                return false;
            }
        }

        public static bool UseReinterpretBits(IType SourceType, IType TargetType)
        {
            return (SourceType.GetIsBit() || TargetType.GetIsBit()) && SourceType.GetIsBit() != TargetType.GetIsBit() && SourceType.GetPrimitiveSize() != TargetType.GetPrimitiveSize();
        }

        public static bool UseConvertToSharedPtr(IType SourceType, IType TargetType)
        {
            return SourceType.GetIsPointer() && SourceType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer) && TargetType.GetIsPointer() && TargetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer);
        }

        public static bool UseConvertToTransientPtr(IType SourceType, IType TargetType)
        {
            return SourceType.GetIsPointer() && SourceType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && TargetType.GetIsPointer() && TargetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer);
        }

        public static bool UseBoxConversion(IType SourceType, IType TargetType)
        {
            return TargetType.GetIsPointer() && TargetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && SourceType.Is(TargetType.AsContainerType().ElementType);
        }

        public static bool UseDynamicCast(IType SourceType, IType TargetType)
        {
            return SourceType.GetIsPointer() && TargetType.GetIsPointer();
        }

        public static bool UseToStringCast(IType SourceType, IType TargetType)
        {
            return TargetType.Equals(PrimitiveTypes.String) && (SourceType.GetIsInteger() || SourceType.GetIsFloatingPoint());
        }

        public CodeBuilder GetCode()
        {
            var sType = Value.Type.RemoveAtAddressPointers();
            var tType = Type.RemoveAtAddressPointers();
            if (UseImplicitCast(sType, tType))
            {
                return Value.GetCode();
            }
            if (sType.GetPointerDepth() == tType.GetPointerDepth() + 1)
            {
                return new DereferenceBlock(Value).GetCode();
            }
            else if (UseConvertToSharedPtr(sType, tType))
            {
                return new ToReferenceBlock(Value).GetCode();
            }
            else if (UseConvertToTransientPtr(sType, tType))
            {
                return new ToAddressBlock(Value).GetCode();
            }
            else if (UseBoxConversion(sType, tType))
            {
                return new BoxConversionBlock(Value).GetCode();
            }
            else if (UseDynamicCast(sType, tType))
            {
                return new DynamicCastBlock(Value, Type).GetCode();
            }
            else if (UseToStringCast(sType, tType))
            {
                return new InvocationBlock(CppPrimitives.GetToStringMethodBlock(sType, CodeGenerator), Value).GetCode();
            }
            else
            {
                CodeBuilder cb = new CodeBuilder();
                cb.Append('(');
                cb.Append(CodeGenerator.GetTypeNamer().Name(tType, CodeGenerator));
                cb.Append(')');
                cb.AppendAligned(Value.GetOperandCode(this));
                return cb;
            }
        }

        public static ICppBlock Cast(ICppBlock Block, IType Target)
        {
            if (Block == null)
            {
                return null;
            }
            var cg = Block.CodeGenerator;
            var tType = cg.GetEnvironment().TypeConverter.Convert(Target); 
            var sType = Block.Type;
            if (sType.Equals(tType))
            {
                return Block;
            }
            else
            {
                return new ConversionBlock(cg, Block, tType);
            }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
