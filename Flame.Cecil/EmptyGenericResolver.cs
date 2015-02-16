using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class EmptyGenericResolver : IGenericResolver
    {
        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return TypeParameter;
        }

        public IType ResolveType(IType Type)
        {
            return Type;
        }
    }
}
