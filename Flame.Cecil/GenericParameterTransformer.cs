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
            this.GenericParameters = GenericParameters.ToDictionary(item => item.Name,
                                                                    item => item,
                                                                    StringComparer.InvariantCulture);
        }

        public IReadOnlyDictionary<string, IGenericParameter> GenericParameters { get; private set; }

        protected override IType ConvertGenericParameter(IGenericParameter Type)
        {
            string name = Type.Name;
            if (GenericParameters.ContainsKey(name))
            {
                return GenericParameters[name];
            }
            else return base.ConvertGenericParameter(Type);
        }
    }
}
