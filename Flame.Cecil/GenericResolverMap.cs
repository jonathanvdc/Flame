using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class GenericResolverMap : IGenericResolver
    {
        public GenericResolverMap(IGenericResolver Resolver)
        {
            this.Resolver = Resolver;
            this.Mapping = new Dictionary<IGenericParameter, IType>();
        }

        public IGenericResolver Resolver { get; private set; }
        public Dictionary<IGenericParameter, IType> Mapping { get; private set; }

        public void Map(IGenericParameter Parameter, IType Type)
        {
            this.Mapping[Parameter] = Type;
        }
        public void Map(IEnumerable<IGenericParameter> Parameters, IEnumerable<IType> Types)
        {
            foreach (var item in Parameters.Zip(Types, (a, b) => new KeyValuePair<IGenericParameter, IType>(a, b)))
            {
                Map(item.Key, item.Value);
            }
        }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            if (Mapping.ContainsKey(TypeParameter))
            {
                return Mapping[TypeParameter];
            }
            else
            {
                return Resolver.ResolveTypeParameter(TypeParameter);
            }
        }
    }
}
