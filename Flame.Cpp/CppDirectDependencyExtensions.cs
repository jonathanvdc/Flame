using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppDirectDependencyExtensions
    {
        public static IEnumerable<IType> GetDirectTypeDependencies(this IMethod Method)
        {
            return TypeDependencyConverter.Instance.GetAllDependencies(Method.GetParameters().GetTypes().With(Method.ReturnType));
        }

        public static IEnumerable<IType> GetDirectTypeDependencies(this IField Field)
        {
            return TypeDependencyConverter.Instance.Convert(Field.FieldType);
        }

        public static IEnumerable<IType> GetDirectTypeDependencies(this IProperty Property)
        {
            return TypeDependencyConverter.Instance.GetAllDependencies(Property.IndexerParameters.GetTypes().With(Property.PropertyType));
        }

        public static IEnumerable<IType> GetDirectTypeDependencies(this IType Type)
        {
            return Type.Methods.SelectMany(GetDirectTypeDependencies)
                .Concat(Type.Fields.SelectMany(GetDirectTypeDependencies))
                .Concat(Type.Properties.SelectMany(GetDirectTypeDependencies))
                .Distinct();
        }

        public static IEnumerable<IType> GetCyclicDependencies(this CppType Type)
        {
            return Type.Environment.DependencyCache.GetCyclicDependencies(Type).Except(Type.BaseTypes);
        }

        public static IEnumerable<IType> GetCyclicDependencies(this IEnumerable<CppType> Types)
        {
            return Types.SelectMany(GetCyclicDependencies).Distinct();
        }
    }
}
