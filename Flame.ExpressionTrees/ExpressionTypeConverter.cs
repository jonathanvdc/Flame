using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees
{
    public class ExpressionTypeConverter : TypeConverterBase<Type>
    {
        private ExpressionTypeConverter()
        {

        }

        static ExpressionTypeConverter()
        {
            Instance = new ExpressionTypeConverter();
        }

        public static ExpressionTypeConverter Instance { get; private set; }

        protected override Type ConvertTypeDefault(IType Type)
        {
            if (Type.Equals(PrimitiveTypes.Int8))
            {
                return typeof(sbyte);
            }
            else if (Type.Equals(PrimitiveTypes.Int16))
            {
                return typeof(short);
            }
            else if (Type.Equals(PrimitiveTypes.Int32))
            {
                return typeof(int);
            }
            else if (Type.Equals(PrimitiveTypes.Int64))
            {
                return typeof(long);
            }
            else if (Type.Equals(PrimitiveTypes.UInt8) || Type.Equals(PrimitiveTypes.Bit8))
            {
                return typeof(byte);
            }
            else if (Type.Equals(PrimitiveTypes.UInt16) || Type.Equals(PrimitiveTypes.Bit16))
            {
                return typeof(ushort);
            }
            else if (Type.Equals(PrimitiveTypes.UInt32) || Type.Equals(PrimitiveTypes.Bit32))
            {
                return typeof(uint);
            }
            else if (Type.Equals(PrimitiveTypes.UInt64) || Type.Equals(PrimitiveTypes.Bit64))
            {
                return typeof(ulong);
            }
            else if (Type.Equals(PrimitiveTypes.Float32))
            {
                return typeof(float);
            }
            else if (Type.Equals(PrimitiveTypes.Float64))
            {
                return typeof(double);
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return typeof(bool);
            }
            else if (Type.Equals(PrimitiveTypes.Char))
            {
                return typeof(char);
            }
            else if (Type.Equals(PrimitiveTypes.String))
            {
                return typeof(string);
            }
            else if (Type.Equals(PrimitiveTypes.Void))
            {
                return typeof(void);
            }
            return typeof(IBoundObject); // Default
        }

        protected override Type MakeArrayType(Type ElementType, int ArrayRank)
        {
            if (ArrayRank <= 1)
            {
                return ElementType.MakeArrayType();
            }
            else
            {
                return ElementType.MakeArrayType(ArrayRank);
            }
        }

        protected override Type MakeGenericType(Type GenericDeclaration, IEnumerable<Type> TypeArguments)
        {
            return GenericDeclaration.MakeGenericType(TypeArguments.ToArray());
        }

        protected override Type MakeGenericInstanceType(Type GenericDeclaration, Type GenericDeclaringTypeInstance)
        {
            throw new NotImplementedException();
        }

        protected override Type MakePointerType(Type ElementType, PointerKind Kind)
        {
            if (Kind.Equals(PointerKind.ReferencePointer))
            {
                return ElementType.MakeByRefType();
            }
            else
            {
                return ElementType.MakePointerType();
            }
        }

        protected override Type MakeVectorType(Type ElementType, IReadOnlyList<int> Dimensions)
        {
            return MakeArrayType(ElementType, Dimensions.Count);
        }
    }
}
