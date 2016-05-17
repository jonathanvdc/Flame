using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class GenericParameterTransformer : TypeTransformerBase
    {
        public GenericParameterTransformer(IEnumerable<IGenericParameter> GenericParameters)
        {
            this.GenericParameters = GenericParameters.ToDictionary(
                item => item.Name, item => item);
        }

        public IReadOnlyDictionary<UnqualifiedName, IGenericParameter> GenericParameters { get; private set; }

        protected override IType ConvertGenericParameter(IGenericParameter Type)
        {
            IGenericParameter result;
            if (GenericParameters.TryGetValue(Type.Name, out result))
            {
                return result;
            }
            else
            {
                return base.ConvertGenericParameter(Type);
            }
        }
    }
}
