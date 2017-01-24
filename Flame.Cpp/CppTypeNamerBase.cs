using Flame.Build;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public abstract class CppTypeNamerBase : TypeNamerBase
    {
        public CppTypeNamerBase(INamespace CurrentNamespace)
        {
            this.CurrentNamespace = CurrentNamespace;
        }

        public INamespace CurrentNamespace { get; private set; }

        public abstract string NameInt(int PrimitiveBitSize);
        public abstract string NameUInt(int PrimitiveBitSize);
        public abstract string NameFloat(int PrimitiveBitSize);

        protected override string ConvertPrimitiveType(IType Type)
        {
            if (Type.Equals(PrimitiveTypes.Void))
            {
                return "void";
            }
            else if (Type.GetIsSignedInteger())
            {
                return NameInt(Type.GetPrimitiveBitSize());
            }
            else if (Type.GetIsUnsignedInteger() || Type.GetIsBit())
            {
                return NameUInt(Type.GetPrimitiveBitSize());
            }
            else if (Type.GetIsFloatingPoint())
            {
                return NameFloat(Type.GetPrimitiveBitSize());
            }
            else if (Type.Equals(PrimitiveTypes.String))
            {
                return "std::string";
            }
            else if (Type.Equals(PrimitiveTypes.Char))
            {
                return "char";
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return "bool";
            }
            else if (Type.Equals(PrimitiveTypes.Null))
            {
                return Convert(PrimitiveTypes.Void.MakePointerType(PointerKind.TransientPointer));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override string MakePointerType(string ElementType, PointerKind Kind)
        {
            if (Kind.Equals(PointerKind.ReferencePointer))
            {
                return MakeGenericType("std::shared_ptr", new string[] { ElementType });
            }
            else
            {
                return ElementType + Kind.Extension;
            }
        }

        protected override string ConvertArrayType(ArrayType Type)
        {
            if (Type.GetIsGenericInstance())
            {
                return ConvertGenericInstance(Type);
            }
            else
            {
                return base.ConvertArrayType(Type);
            }
        }

        protected override string ConvertVectorType(VectorType Type)
        {
            if (Type.GetIsGenericInstance())
            {
                return ConvertGenericInstance(Type);
            }
            else
            {
                return base.ConvertVectorType(Type);
            }
        }

        protected override string ConvertMethodType(MethodType Type)
        {
            return MakeGenericType("std::function", new string[] { base.ConvertMethodType(Type) });
        }

        protected override string ConvertGenericParameter(IGenericParameter Type)
        {
            return Type.Name.ToString();
        }

        protected override string ConvertTypeDeclaration(IType Type)
        {
            if (Type.DeclaringNamespace is IType)
            {
                return Convert((IType)Type.DeclaringNamespace) + "::" + Type.Name;
            }
            return base.ConvertTypeDeclaration(Type);
        }

        protected override string ConvertTypeDefault(IType Type)
        {
            if (Type.IsGlobalType())
            {
                return CppNameExtensions.RemoveRedundantScope(Type.DeclaringNamespace.FullName.ToString(), CurrentNamespace);
            }
            else
            {
                return CppNameExtensions.RemoveRedundantScope(Type.FullName.ToString(), CurrentNamespace);
            }
        }
    }
}
