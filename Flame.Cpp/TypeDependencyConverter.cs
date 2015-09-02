using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class TypeDependencyConverter : TypeConverterBase<IEnumerable<IType>>
    {
        private TypeDependencyConverter()
        {
        }

        private static TypeDependencyConverter converter;
        public static TypeDependencyConverter Instance
        {
            get
            {
                if (converter == null)
                {
                    converter = new TypeDependencyConverter();
                }
                return converter;
            }
        }

        protected override IEnumerable<IType> ConvertTypeDefault(IType Type)
        {
            return new IType[] { Type };
        }

        protected override IEnumerable<IType> MakeArrayType(IEnumerable<IType> ElementType, int ArrayRank)
        {
            return ElementType;
        }

        protected override IEnumerable<IType> MakeGenericType(IEnumerable<IType> GenericDeclaration, IEnumerable<IEnumerable<IType>> TypeArguments)
        {
            return GenericDeclaration.Concat(TypeArguments.SelectMany(item => item));
        }

        protected override IEnumerable<IType> MakePointerType(IEnumerable<IType> ElementType, PointerKind Kind)
        {
            return ElementType;
        }

        protected override IEnumerable<IType> MakeVectorType(IEnumerable<IType> ElementType, IReadOnlyList<int> Dimensions)
        {
            return ElementType;
        }

        protected override IEnumerable<IType> ConvertGenericParameter(IGenericParameter Type)
        {
            return Enumerable.Empty<IType>();
        }

        public IEnumerable<IType> GetAllDependencies(IEnumerable<IType> Types)
        {
            return Convert(Types).Aggregate(Enumerable.Empty<IType>(), (a, b) => a.Union(b));
        }
    }
}
