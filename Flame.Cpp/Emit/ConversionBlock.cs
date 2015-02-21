using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ConversionBlock : ICppBlock
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

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies.MergeDependencies(Type.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public bool UseImplicitCast(IType SourceType, IType TargetType)
        {
            if (SourceType.Is(TargetType))
            {
                return true;
            }
            else if (SourceType.get_IsPointer() && TargetType.get_IsPointer() && SourceType.AsContainerType().AsPointerType().PointerKind.Equals(TargetType.AsContainerType().AsPointerType().PointerKind))
            {
                return SourceType.AsContainerType().GetElementType().Is(TargetType.AsContainerType().GetElementType());
            }
            else if (TargetType.get_IsBit())
            {
                return SourceType.get_IsUnsignedInteger();
            }
            else if (SourceType.get_IsBit())
            {
                return TargetType.get_IsUnsignedInteger();
            }
            else if ((SourceType.get_IsSignedInteger() && TargetType.get_IsSignedInteger()) || (SourceType.get_IsUnsignedInteger() && TargetType.get_IsUnsignedInteger()) || (SourceType.get_IsBit() && TargetType.get_IsBit()) || (SourceType.get_IsFloatingPoint() && TargetType.get_IsFloatingPoint()))
            {
                return SourceType.GetPrimitiveMagnitude() == TargetType.GetPrimitiveMagnitude();
            }
            else if (SourceType.get_IsUnsignedInteger() && TargetType.get_IsSignedInteger())
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
            return (SourceType.get_IsBit() || TargetType.get_IsBit()) && SourceType.get_IsBit() != TargetType.get_IsBit() && SourceType.GetPrimitiveSize() != TargetType.GetPrimitiveSize();
        }

        public static bool UseConvertToSharedPtr(IType SourceType, IType TargetType)
        {
            return SourceType.get_IsPointer() && SourceType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer) && TargetType.get_IsPointer() && TargetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer);
        }

        public static bool UseConvertToTransientPtr(IType SourceType, IType TargetType)
        {
            return SourceType.get_IsPointer() && SourceType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && TargetType.get_IsPointer() && TargetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer);
        }

        public static bool UseBoxConversion(IType SourceType, IType TargetType)
        {
            return TargetType.get_IsPointer() && TargetType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && SourceType.Is(TargetType.AsContainerType().GetElementType());
        }

        public static bool UseDynamicCast(IType SourceType, IType TargetType)
        {
            return SourceType.get_IsPointer() && TargetType.get_IsPointer();
        }

        public static bool UseToStringCast(IType SourceType, IType TargetType)
        {
            return TargetType.Equals(PrimitiveTypes.String) && (SourceType.get_IsInteger() || SourceType.get_IsFloatingPoint());
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
                cb.Append(Value.GetCode());
                return cb;
            }
        }

        public static ICppBlock Cast(ICppBlock Block, IType Target)
        {
            if (Block == null)
            {
                return null;
            }
            var sType = Block.Type;
            if (sType.Equals(Target))
            {
                return Block;
            }
            else
            {
                return new ConversionBlock(Block.CodeGenerator, Block, Target);
            }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
