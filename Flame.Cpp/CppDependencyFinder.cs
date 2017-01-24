using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public sealed class CppDependencyFinder : TypeConverterBase<IEnumerable<IHeaderDependency>>
    {
        public CppDependencyFinder(IEnumerable<IMember> Exclude)
        {
            this.exclude = new HashSet<IMember>(Exclude);
        }

        private HashSet<IMember> exclude;

        protected override IEnumerable<IHeaderDependency> MakeArrayType(IEnumerable<IHeaderDependency> ElementType, int ArrayRank)
        {
            return ElementType;
        }

        protected override IEnumerable<IHeaderDependency> MakeGenericType(IEnumerable<IHeaderDependency> GenericDeclaration, IEnumerable<IEnumerable<IHeaderDependency>> TypeArguments)
        {
            return GenericDeclaration.MergeDependencies(TypeArguments.SelectMany(item => item));
        }

        protected override IEnumerable<IHeaderDependency> MakePointerType(IEnumerable<IHeaderDependency> ElementType, PointerKind Kind)
        {
            if (Kind.Equals(PointerKind.ReferencePointer))
            {
                return ElementType.MergeDependencies(new IHeaderDependency[] { StandardDependency.Memory });
            }
            else
            {
                return ElementType;
            }
        }

        protected override IEnumerable<IHeaderDependency> MakeVectorType(IEnumerable<IHeaderDependency> ElementType, IReadOnlyList<int> Dimensions)
        {
            return ElementType;
        }

        protected override IEnumerable<IHeaderDependency> ConvertTypeDefault(IType Type)
        {
            if (Type is ICppMember)
            {
                if (Type.DeclaringNamespace is IType)
                {
                    return Convert((IType)Type.DeclaringNamespace);
                }
                return new IHeaderDependency[] { new CppFile((ICppMember)Type) };
            }
            else
            {
                return Type.GetAttributeDependencies();
            }
        }

        protected override IEnumerable<IHeaderDependency> ConvertPrimitiveType(IType Type)
        {
            if (Type.IsEquivalent(PrimitiveTypes.String))
            {
                return new IHeaderDependency[] { StandardDependency.String };
            }
            else
            {
                return base.ConvertPrimitiveType(Type);
            }
        }

        protected override IEnumerable<IHeaderDependency> ConvertMethodType(MethodType Type)
        {
            var sig = Type.DelegateSignature;
            if (sig.GetIsAnonymous())
            {
                return Convert(sig.ReturnType).MergeDependencies(
                    new IHeaderDependency[] { StandardDependency.Functional }).Concat(
                        sig.Parameters.SelectMany(item => Convert(item.ParameterType)));
            }
            else
            {
                return new IHeaderDependency[0];
            }
        }

        protected override IEnumerable<IHeaderDependency> ConvertGenericInstance(IType Type)
        {
            return Enumerable.Empty<IHeaderDependency>();
        }

        protected override IEnumerable<IHeaderDependency> ConvertGenericParameter(IGenericParameter Type)
        {
            return Enumerable.Empty<IHeaderDependency>();
        }

        public override IEnumerable<IHeaderDependency> Convert(IType Value)
        {
            if (exclude.Contains(Value))
            {
                return new IHeaderDependency[0];
            }
            else
            {
                return base.Convert(Value);
            }
        }
    }
}
