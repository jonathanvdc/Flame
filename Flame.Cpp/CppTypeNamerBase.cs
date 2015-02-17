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

        public abstract string NameInt(int PrimitiveMagnitude);
        public abstract string NameUInt(int PrimitiveMagnitude);
        public abstract string NameFloat(int PrimitiveMagnitude);

        protected override string ConvertPrimitiveType(IType Type)
        {
            if (Type.Equals(PrimitiveTypes.Void))
            {
                return "void";
            }
            else if (Type.get_IsSignedInteger())
            {
                return NameInt(Type.GetPrimitiveMagnitude());
            }
            else if (Type.get_IsUnsignedInteger() || Type.get_IsBit())
            {
                return NameUInt(Type.GetPrimitiveMagnitude());
            }
            else if (Type.get_IsFloatingPoint())
            {
                return NameFloat(Type.GetPrimitiveMagnitude());
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
                return "std::shared_ptr<" + ElementType + ">";
            }
            else
            {
                return ElementType + "*";
            }
        }

        protected override string ConvertArrayType(IArrayType Type)
        {
            if (Type.get_IsGenericInstance())
            {
                return ConvertGenericInstance(Type);
            }
            else
            {
                return base.ConvertArrayType(Type);
            }
        }

        protected override string ConvertVectorType(IVectorType Type)
        {
            if (Type.get_IsGenericInstance())
            {
                return ConvertGenericInstance(Type);
            }
            else
            {
                return base.ConvertVectorType(Type);
            }
        }

        protected override string ConvertGenericParameter(IGenericParameter Type)
        {
            return Type.Name;
        }

        protected override string ConvertTypeDefault(IType Type)
        {
            if (Type.IsGlobalType())
            {
                return CppNameExtensions.RemoveRedundantScope(Type.DeclaringNamespace.FullName, CurrentNamespace);
            }
            else
            {
                return CppNameExtensions.RemoveRedundantScope(Type.FullName, CurrentNamespace);
            }
        }
    }
}
