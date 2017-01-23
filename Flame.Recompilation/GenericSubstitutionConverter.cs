using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class GenericSubstitutionConverter : GenericTypeTransformerBase
    {
        public GenericSubstitutionConverter(IType GenericDeclaration, IType GenericInstance)
        {
            this.GenericDeclaration = GenericDeclaration;
            this.GenericInstance = GenericInstance;
        }

        public IType GenericDeclaration { get; private set; }
        public IType GenericInstance { get; private set; }

        private Dictionary<IGenericParameter, IType> map;
        public IReadOnlyDictionary<IGenericParameter, IType> Mapping
        {
            get
            {
                if (map == null)
                {
                    map = GenericInstance.GetRecursiveGenericParameters()
                            .Zip(GenericInstance.GetRecursiveGenericArguments(), (a, b) => new KeyValuePair<IGenericParameter, IType>(a, b))
                            .ToDictionary<KeyValuePair<IGenericParameter, IType>, IGenericParameter, IType>(
                                item => item.Key,
                                item => item.Value,
                                TypeNameComparer.Instance);
                }
                return map;
            }
        }

        protected override IType ConvertGenericParameter(IGenericParameter Type)
        {
            if (Mapping.ContainsKey(Type))
            {
                return Mapping[Type];
            }
            return Type;
        }
    }
}
