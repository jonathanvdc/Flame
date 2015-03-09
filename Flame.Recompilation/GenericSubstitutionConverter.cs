using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class GenericSubstitutionConverter : TypeTransformerBase
    {
        public GenericSubstitutionConverter(IType GenericDeclaration, IType GenericInstance)
        {
            this.GenericDeclaration = GenericDeclaration;
            this.GenericInstance = GenericInstance;
        }

        public IType GenericDeclaration { get; private set; }
        public IType GenericInstance { get; private set; }

        protected override IType ConvertGenericParameter(IGenericParameter Type)
        {
            foreach (var item in GenericInstance.GetGenericParameters()
                .Zip(GenericInstance.GetGenericArguments(), (a, b) => new KeyValuePair<IType, IType>(a, b)))
            {
                if (item.Key.Name == Type.Name)
                {
                    return item.Value;
                }
            }
            return Type;
        }
    }
}
